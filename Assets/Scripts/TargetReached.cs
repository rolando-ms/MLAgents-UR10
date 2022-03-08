using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetReached : MonoBehaviour
{
    public UR10Agent robotAgent;
    public GameObject EndEffector;

    
    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="other">The collision info</param>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collider = " + other.name);
        if (other.name == EndEffector.name)
        {
            Debug.Log("In trigger enter called.");
            robotAgent.GoalReward();
            // If there is a collision, add negative reward
            //robotAgent.AddReward(-1f);
            //Debug.Log("Reward " + -1f);
        }
    }

    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name == EndEffector.name)
        {
            Debug.Log("On collision enter called.");
            // If there is a collision, add negative reward
            robotAgent.GoalReward();
            //Debug.Log("Reward " + -1f);
        }
    }

    /// <summary>
    /// Called when the agent's collider stays in a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        if (other.name == EndEffector.name)
        {
            robotAgent.GoalReward();
            // If there is a collision, add negative reward
            //robotAgent.AddReward(-1f);
            //Debug.Log("Reward " + -1f);
        }
    }
}
