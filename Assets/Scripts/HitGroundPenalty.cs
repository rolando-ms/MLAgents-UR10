using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitGroundPenalty : MonoBehaviour
{
    public UR10Agent robotAgent;

    
    /// <summary>
    /// Called when the agent collides with something solid
    /// </summary>
    /// <param name="other">The collision info</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "robot")
        {
            Debug.Log("In trigger enter called.");
            robotAgent.HitGroundPenalty(other.name);
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
        if (collision.collider.tag == "robot")
        {
            Debug.Log("On collision enter called.");
            // If there is a collision, add negative reward
            robotAgent.HitGroundPenalty(collision.collider.name);
            //Debug.Log("Reward " + -1f);
        }
    }

    /// <summary>
    /// Called when the agent's collider stays in a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "robot")
        {
            robotAgent.HitGroundPenalty(other.name);
            // If there is a collision, add negative reward
            //robotAgent.AddReward(-1f);
            //Debug.Log("Reward " + -1f);
        }
    }
    

}
