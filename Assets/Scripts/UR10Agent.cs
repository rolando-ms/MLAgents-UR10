using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;

//namespace RosSharp.Control
//{
    /// <summary>
    /// A UR10 robot Machine Learning Agent
    /// <summary>
    public class UR10Agent : Agent
    {
    private ArticulationBody[] articulationChain;
        private Color[] prevColor;
        private int previousJointIndex;

        //public ControlType control = ControlType.PositionControl;
        public int selectedIndex;
        public int previousIndex;
        public string jointName;
        public float stiffness = 100000f; //
        public float damping = 10000f; //
        public float forceLimit = 10000f; //
        public float R, G, B, Alpha;
        public float speed = 5f; // Units: degree/s
        public float torque = 100f; // Units: Nm or N
        public float acceleration = 5f;// Units: m/s^2 / degree/s^2

        //[Tooltip("Force to apply when moving")]
        //public float moveForce = 2f;

        private int currentJointIndex;
        //private string[] jointNames;
        private List<string> jointNames;
        private Dictionary<int, int> jointIndexMapping;
        private int urdfJointIndex;
        private int controllableJointIndex;
        public string baseLinkName = "base_link";
        //public string toolCenterPointName = "tool0";
        public GameObject EndEffector;
        private Vector3 EndEffectorStartPointOffset = new Vector3(-0.2f, 0.7f, 0.5f);
        private Vector3 EndEffectorStartPoint = Vector3.zero;
        public int jointsToControl = 8;
        public int numOfActions = 6;

        public RandomPositionTarget target;

        private List<Color[]> colorList;
        private float[] defaultJointPos = new float[] {0,-90,-90,-90, 0, 0,0,0,0,0,0,0}; //{0,-90,0,-90,0,0,0,0,0,0,0,0};
        private float[] currentJointPos = new float[] {0,0,0,0,0,0,0,0,0,0,0,0};
        private float[] currentJointVel = new float[] {0,0,0,0,0,0,0,0,0,0,0,0};
        private bool robotReady = false;
        public bool trainingMode = false;

        private float startDistanceToGoal;
        private float distanceToGoal;
        private float previousBestDistanceToGoal;
        private float stepPenalty = 0.0001f;
        private float goalDistanceThreshold = 0.05f;

        public bool MQTTPublish = false;
        [HideInInspector] public float[] MQTTMessage = new float[] {0,0,0,0,0,0};

        [SerializeField] private GameObject robot;
        [SerializeField] private Material winMaterial;
        [SerializeField] private Material loseMaterial;
        [SerializeField] private MeshRenderer groundMeshRenderer;
        

        /// <summary>
        /// Initialize the agent
        /// </summary>
        public override void Initialize()
        {
            //this.gameObject.AddComponent<FKRobot>();
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            int defDyanmicVal = 10;
            urdfJointIndex = 0;
            controllableJointIndex = 0;
            previousJointIndex = currentJointIndex = 1;
            foreach (ArticulationBody joint in articulationChain)
            {
                bool jointSelected = false;
                joint.gameObject.AddComponent<JointControl>();
                joint.jointFriction = defDyanmicVal;
                joint.angularDamping = defDyanmicVal;
                //Debug.Log("Articulation name = " + joint.name);
                //Debug.Log("Joint type = " + joint.jointType);
                if(joint.jointType != ArticulationJointType.FixedJoint)
                {
                    if(jointNames == null)
                    {
                        jointNames = new List<string> {joint.name};
                        jointSelected = true;
                    }
                    else if(!jointNames.Contains(joint.name))
                    {
                        jointNames.Add(joint.name);
                        jointSelected = true;
                    }
                }
                if(jointSelected)
                {
                    if(jointIndexMapping == null) jointIndexMapping = new Dictionary<int, int>();
                    Debug.Log("Articulation name = " + joint.name);
                    jointIndexMapping.Add(controllableJointIndex, urdfJointIndex);
                    controllableJointIndex += 1;
                }
                urdfJointIndex += 1;
            }
            Debug.Log("Number of joints = " + jointNames.Count);
            colorList = new List<Color[]>();
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            int counter = 0;
            foreach(ArticulationBody joint in articulationChain)
            {
                SaveOriginalColors(counter);
                counter ++;
            }

            R = G = 0;
            Alpha = B = 1;
            ResetToDefaultPos();
            Highlight(jointIndexMapping[currentJointIndex]);

            // If not training mode, no max step, play forever
            if (!trainingMode) MaxStep = 0;

        }

        /// <summary>
        /// Reset the agent when an episode begins
        /// </summary>
        public override void OnEpisodeBegin()
        {
            ResetEnvironment();
            ArticulationBody EndEffectorJoint = FindArticulationJoint("tool0");
            if(EndEffectorJoint == null) startDistanceToGoal = Vector3.Distance(Vector3.zero, target.transform.position);
            else startDistanceToGoal = Vector3.Distance(EndEffectorJoint.transform.position, target.transform.position);
            EndEffectorStartPoint = GetLocalEnvironmentOrigin() + EndEffectorStartPointOffset;
            startDistanceToGoal = 100f;
            previousBestDistanceToGoal = startDistanceToGoal;
        }

        /// <summary>
        /// Resets the current environment
        /// </summary>
        public void ResetEnvironment()
        {
            ResetToDefaultPos();
            //target.SetRandomValidPosition();
            //target.SetRandomSafeValidPosition();
            target.SetFixedGoalPoint();
            Debug.Log(target.transform.position);
        }

        /// <summary>
        /// Called when and action is received from either the player input or the neural network
        /// 
        /// vectorAction[i] represents:
        /// +1 = right, -1 = left, 0 = no motion, for every joint
        /// </summary>
        /// <param name="vectorAction">The actions to take</param>
        public override void OnActionReceived(float[] vectorAction)
        {
            float[] jointPositions = GetCurrentJointPositions();
            if(robotReady)
            {
                //jointPositions.PrintArray();
                float targetWeight = 0.25f;
                for(int i=0; i<vectorAction.Length; i++)
                {
                    float action = 0f;
                    // Discrete output to action mapping
                    if(vectorAction[i] == 0f) action = -1f;
                    if(vectorAction[i] == 1f) action = 0f;
                    if(vectorAction[i] == 2f) action = 1f;
                    float target = targetWeight * action;
                    MoveJointToTarget(jointNames[i], target);
                }

                for(int i= 0; i < 6; i++)
                {
                    //Clamping value joint before publishing
                    float limit = 2*3.13f;
                    if(jointPositions[i] >= 0)
                    {
                        if(jointPositions[i] > limit)
                        {
                            vectorAction[i] = limit;
                        }
                        else vectorAction[i] = jointPositions[i];
                    }
                    else
                    {
                        if(jointPositions[i] < -limit)
                        {
                            vectorAction[i] = -limit;
                        }
                        else vectorAction[i] = jointPositions[i];
                    }

                }
                EndEffector.transform.position.DrawTransformPosition();
                distanceToGoal = Vector3.Distance(EndEffector.transform.position, target.transform.position);
                float distanceChange = startDistanceToGoal - distanceToGoal;

                if(distanceToGoal < goalDistanceThreshold)
                {
                    GoalReward();
                }
                AddReward(-distanceToGoal * 0.1f);
            }
            AddReward(-stepPenalty);
        }

        /// <summary>
        /// Collect vector observations from the environment
        /// </summary>
        /// <param name="sensor">The vector sensor</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            // Target position (3 observations)
            //sensor.AddObservation(cokeBottle.GetTargetPosition());
            sensor.AddObservation(target.transform.position);
            
            // End-Effector position (3 observations)
            sensor.AddObservation(EndEffector.transform.position);

            // Target vector (3 observations)
            //sensor.AddObservation(cokeBottle.GetTargetPosition() - EndEffector.transform.position);
            sensor.AddObservation(target.transform.position - EndEffector.transform.position);

        }

        private void ResetToDefaultPos()
        {
            for(int i=0; i < jointsToControl; i++)
            {
                ResetJoint(jointNames[i], defaultJointPos[i] * Mathf.Deg2Rad);
            }
            robotReady = true;
        }

        /// <summary>
        /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
        /// this function will be called. Its return values will be fed into
        /// <see cref="OnActionReceived(float[])"/> instead of using the neural network
        /// </summary>

        /// <param name="actionsOut">And output action array</param>
        public override void Heuristic(float[] actionsOut)
        {
            if(Input.GetKey(KeyCode.A)) SelectJoint(-1, numOfActions);
            else if(Input.GetKey(KeyCode.D)) SelectJoint(1, numOfActions);
            else if (Input.GetKey(KeyCode.W)) actionsOut[currentJointIndex] = 1;
            else if (Input.GetKey(KeyCode.S)) actionsOut[currentJointIndex] = -1;
            else actionsOut[currentJointIndex] = 0;
            
        }

        private void GetJointNames(Transform childTransform)
        {
            if(childTransform.GetComponent<ArticulationBody>() != null)
            {
                if(childTransform.GetComponent<ArticulationBody>().jointType != ArticulationJointType.FixedJoint)
                {
                    string childTransformName = childTransform.name;
                    if(jointNames == null)
                    {
                        jointNames = new List<string> {childTransformName};
                    }
                    else if(!jointNames.Contains(childTransformName))
                    {
                        jointNames.Add(childTransformName);
                    }
                }
            }
            if(childTransform.childCount > 0)
            {
                for(int i = 0; i < childTransform.childCount; i++)
                {
                    GetJointNames(childTransform.GetChild(i));
                }
            }
        }

        public float[] GetCurrentJointPositions()
        {
            int index = 0;
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            ArticulationBody joint = null;
            foreach(ArticulationBody articulation in articulationChain)
            {
                if(index >= jointNames.Count) break;
                if(articulation.name == jointNames[index])
                {
                    joint = articulation;
                    ArticulationReducedSpace position = joint.jointPosition;
                    currentJointPos[index] = position[0];
                    index++;
                }
                else
                {
                    //Debug.Log("Joint " + jointNames[index] + " not found");
                }
            }
            return currentJointPos;
        }

        public float[] GetCurrentJointVelocities()
        {
            int index = 0;
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            ArticulationBody joint = null;
            foreach(ArticulationBody articulation in articulationChain)
            {
                if(index >= jointNames.Count) break;
                if(articulation.name == jointNames[index])
                {
                    joint = articulation;
                    ArticulationReducedSpace velocity = joint.jointVelocity;
                    currentJointVel[index] = velocity[0];
                    index++;
                }
                else
                {
                    //Debug.Log("Joint " + jointNames[index] + " not found");
                }
            }
            return currentJointVel;
            
        }

        /// <summary>
        /// Select and highlight a joint of the robot.
        /// </summary>
        public void SelectJoint()
        {
            if(Input.GetKeyDown(KeyCode.A)) currentJointIndex -= 1;
            if(Input.GetKeyDown(KeyCode.D)) currentJointIndex += 1;
            if(currentJointIndex < 0)
            {
                currentJointIndex = jointsToControl;
            }
            if(currentJointIndex > jointsToControl)
            {
                currentJointIndex = 0;
            }
            Highlight(jointIndexMapping[currentJointIndex]);
        }

        /// <summary>
        /// Select and highlight a joint of the robot.
        /// </summary>
        /// <param name="numLimit">Number of joints to control</param>
        public void SelectJoint(int nextJoint, int numLimit)
        {
            currentJointIndex += nextJoint;
            if(currentJointIndex < 0)
            {
                currentJointIndex = numLimit;
            }
            if(currentJointIndex > numLimit)
            {
                currentJointIndex = 0;
            }
            Highlight(jointIndexMapping[currentJointIndex]);
        }

        /// <summary>
        /// Lets to move a joint using W and S keys.
        /// </summary>
        /// <param name="jointName">String name of the target joint</param>
        public void MoveJointWithKeys(string jointName)
        {
            ArticulationBody joint = FindArticulationJoint(jointName);
            if(joint != null)
            {
                //joint = jointObject.GetComponent<ArticulationBody>();
                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                if(Input.GetKey(KeyCode.W)) currentDrive.target += 1f;
                if(Input.GetKey(KeyCode.S)) currentDrive.target -= 1f;
                joint.xDrive = currentDrive;
            }
            else
            {
                Debug.Log("Joint " + jointName + " not found.");
            }
        }

        /// <summary>
        /// Moves the given joint to the given target.
        /// </summary>
        /// <param name="jointName">String name of the target joint </param>
        /// <param name="target">Target value to move to </param>
        public void MoveJointToTarget(string jointName, float target)
        {
            ArticulationBody joint = FindArticulationJoint(jointName);
            if(joint != null)
            {
                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                currentDrive.damping = damping;
                currentDrive.stiffness = stiffness;
                currentDrive.target += target;
                joint.xDrive = currentDrive;
            }
            else
            {
                Debug.Log("Joint " + jointName + " not found.");
            }
        }

        /// <summary>
        /// Resets the given joint to the given target.
        /// </summary>
        public void ResetJoint(string JointName, float target)
        {
            ArticulationBody joint = FindArticulationJoint(JointName);

            //Assert.IsNotNull(jointObject);
            if(joint != null)
            {
                //ArticulationBody joint = jointObject.GetComponent<ArticulationBody>();
                ArticulationReducedSpace jointPos = joint.jointPosition;

                jointPos[0] = target; // Position is in Rad
                joint.jointPosition = jointPos;

                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                currentDrive.damping = damping;
                currentDrive.stiffness = stiffness;
                currentDrive.target = target * Mathf.Rad2Deg; // xDrive Target is in Deg
                joint.xDrive = currentDrive;

                joint.jointPosition = new ArticulationReducedSpace(target);
                joint.jointAcceleration = new ArticulationReducedSpace(0f);
                joint.jointForce = new ArticulationReducedSpace(0f);
                joint.jointVelocity = new ArticulationReducedSpace(0f);
            }
            else
            {
                Debug.Log("Joint " + jointName + " not found.");
            }
            //if(previousJointIndex != jointIndex) previousJointIndex = jointIndex;
        }


        /// <summary>
        /// Highlights the color of the robot by changing the color of the part to a color set by the user in the inspector window
        /// </summary>
        /// <param name="index">Index of the link selected in the Articulation Chain</param>
        private void Highlight(int index)
        {
            if(index == previousJointIndex)
            {
                return;
            }

            RestoreColor(previousJointIndex);

            Renderer[] materialList = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();

            foreach (var mesh in materialList)
            {
                Color tempColor = new Color(R, G, B, Alpha);
                mesh.material.color = tempColor;
            }

            previousJointIndex = index;

        }

        /// <summary>
        /// Saves the original colors of the mesh of the whole robot in colorList for future query
        /// </summary>
        /// <param name="index">Index of the link selected in the Articulation Chain</param>
        private void SaveOriginalColors(int index)
        {
            Renderer[] jointMaterials = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();   
            //}
            Color[] jointColors = new Color[jointMaterials.Length];
            for (int counter = 0; counter < jointMaterials.Length; counter++)
            {
                jointColors[counter] = jointMaterials[counter].sharedMaterial.GetColor("_Color");
            }
            colorList.Add(jointColors);
        }

        /// <summary>
        /// Queries the original color from colorList and replaces it into the selected joint according to index
        /// </summary>
        /// <param name="index">Index of the link selected in the Articulation Chain</param>
        private void RestoreColor(int index)
        {
            Renderer[] materialList = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();

            int counter = 0;
            foreach (var mesh in materialList)
            {
                mesh.material.color = colorList[index][counter];
                counter ++;
            }
        }

        /// <summary>
        /// Finds a joint by name in the articulation chain of the robot
        /// </summary>
        /// <param name="jointName">Name of the joint in the Articulation Chain</param>
        private ArticulationBody FindArticulationJoint(string jointName)
        {
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            ArticulationBody joint = null;
            foreach(ArticulationBody articulation in articulationChain)
            {
                if(articulation.name == jointName)
                {
                    joint = articulation;
                    break;
                }
            }
            return joint;
        }

        /// <summary>
        /// Returns the position of the origin of the current environment
        /// </summary>
        private Vector3 GetLocalEnvironmentOrigin()
        {
            Transform environmentTransform = GetLocalEnvironmentOrigin(this.transform);
            return environmentTransform.position;
        }

        /// <summary>
        /// Recursively finds parent transform that contains "Environment" in the name
        /// </summary>
        /// <param name="objectTrasform">Transform of a game object</param>
        private Transform GetLocalEnvironmentOrigin(Transform objectTransform)
        {
            Transform parentTransform = objectTransform.parent;
            if(parentTransform.name.Contains("Environment")) return parentTransform;
            else return GetLocalEnvironmentOrigin(parentTransform);
        }


        /// <summary>
        /// Negative reward for hitting the ground or table. It automatically ends the Episode.
        /// </summary>
        public void HitGroundPenalty(string colliderName)
        {
            AddReward(-1f);
            Debug.Log("Ground Penalty added.");
            Debug.Log("Collision on " + colliderName);
            groundMeshRenderer.material = loseMaterial;
        }

        /// <summary>
        /// Positive reward for reaching the goal. It automatically ends the episode.
        /// </summary>
        public void GoalReward()
        {
            AddReward(1f);
            Debug.Log("Goal Reached!!!!!!");
            groundMeshRenderer.material = winMaterial;
        }

    }

//}