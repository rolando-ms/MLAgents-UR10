using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Assertions;

namespace RosSharp.Control
{
    /// <summary>
    /// A UR10 robot Machine Learning Agent
    /// <summary>
    public class UR10Controller : Agent
    {
    private ArticulationBody[] articulationChain;
        private Color[] prevColor;
        private int previousJointIndex;

        public ControlType control = ControlType.PositionControl;
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

        private int currentJointIndex;
        private List<string> jointNames;
        private Dictionary<int, int> jointIndexMapping;
        private int urdfJointIndex;
        private int controllableJointIndex;
        public string baseLinkName = "base_link";
        public string toolCenterPointName = "tool0";
        public int jointsToControl = 8;

        private List<Color[]> colorList;
        private float[] defaultJointPos = new float[] {0,-90,90,-90,-90,-90,0,0,0,0,0,0};
        private float[] currentJointPos = new float[] {0,0,0,0,0,0,0,0,0,0,0,0};

        void Awake()
        {
            this.gameObject.AddComponent<FKRobot>();
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
        }

        private void Start()
        {
        }

        private void ResetToDefaultPos()
        {
            //ResetRobot();
            for(int i=0; i < jointsToControl; i++)
            {
                ResetJoint(jointNames[i], defaultJointPos[i] * Mathf.Deg2Rad);
            }
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

        // Update is called once per frame
        void Update()
        {
            SelectJoint();
            MoveJointWithKeys(jointNames[currentJointIndex]);

        }

        public float[] GetCurrentJointPositions()
        {
            for(int i=0; i<jointNames.Count; i++)
            {
                GameObject jointObject = GameObject.Find(jointNames[i]);
                Assert.IsNotNull(jointObject);
                if(jointObject != null)
                {
                    ArticulationBody joint = jointObject.GetComponent<ArticulationBody>();
                    ArticulationReducedSpace position = joint.jointPosition;
                    currentJointPos[i] = position[0];
                }
                else
                {
                    Debug.Log("Joint " + jointNames[i] + " not found");
                }
            }
            return currentJointPos;
        }

        /// <summary>
        /// Select and highlight a joint of the robot.
        /// </summary>
        public void SelectJoint()
        {
            if(Input.GetKey(KeyCode.A)) currentJointIndex -= 1;
            if(Input.GetKey(KeyCode.D)) currentJointIndex += 1;
            //Mathf.Clamp(currentJointIndex, 0f, jointsToControl);
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
        /// Lets to move a joint using W and S keys.
        /// </summary>
        /// <param name="jointName">String name of the target joint</param>
        public void MoveJointWithKeys(string jointName)
        {
            GameObject jointObject = GameObject.Find(jointName);
            Assert.IsNotNull(jointObject);
            if(jointObject != null)
            {
                ArticulationBody joint = jointObject.GetComponent<ArticulationBody>();
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
            GameObject jointObject = GameObject.Find(jointName);
            Assert.IsNotNull(jointObject);
            if(jointObject != null)
            {
                ArticulationBody joint = jointObject.GetComponent<ArticulationBody>();
                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                currentDrive.target = target;
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
            GameObject jointObject = GameObject.Find(JointName);
            Assert.IsNotNull(jointObject);
            if(jointObject != null)
            {
                ArticulationBody joint = jointObject.GetComponent<ArticulationBody>();
                ArticulationReducedSpace jointPos = joint.jointPosition;
                jointPos[0] = target;
                joint.jointPosition = jointPos;
            }
            else
            {
                Debug.Log("Joint " + jointName + " not found.");
            }
        }

        /// <summary>
        /// Resets the given joint to the given target.
        /// </summary>
        public void ResetRobot()
        {
            string jointName = jointNames[0];
            GameObject jointObject = GameObject.Find(jointName);
            Assert.IsNotNull(jointObject);
            if(jointObject != null)
            {
                ArticulationBody joint = jointObject.GetComponent<ArticulationBody>();
                ArticulationDrive currentDrive = joint.xDrive;
                joint.SetJointPositions(new List<float>{0,(Mathf.Deg2Rad)-90,(Mathf.Deg2Rad)-270,(Mathf.Deg2Rad)-90,(Mathf.Deg2Rad)-90,(Mathf.Deg2Rad)-90,0,0,0,0,0,0});
            }
            else
            {
                Debug.Log("Joint " + jointName + " not found.");
            }
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

    }

}
