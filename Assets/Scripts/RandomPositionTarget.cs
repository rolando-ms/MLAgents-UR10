using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPositionTarget : MonoBehaviour
{
    public Transform robotBase;
    private float maxLength = 1.4f;
    private float minLength = 0.25f;
    private float maxSafeLength = 0.25f;
    public Vector3 startPosition = new Vector3(1f,1f,1f); //new Vector3(0f, 0.5f, 0.5f);//Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = robotBase.position;
        transform.rotation = new Quaternion(0,0,0,1);
        transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        SetRandomValidPosition();
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = robotBase.position + startPosition;
    }

    public void SetRandomValidPosition()
    {
        float randomX = Random.Range(-maxLength, maxLength);
        if(randomX < 0f && Mathf.Abs(randomX) < minLength) randomX = Random.Range(-maxLength, -minLength);
        if(randomX >= 0f && randomX < minLength) randomX = Random.Range(minLength, maxLength);
        
        float randomZ = Random.Range(-maxLength, maxLength);
        if(randomZ < 0f && Mathf.Abs(randomZ) < minLength) randomZ = Random.Range(-maxLength, -minLength);
        if(randomZ >= 0f && randomZ < minLength) randomZ = Random.Range(minLength, maxLength);
        
        float randomY = Random.Range(minLength, maxLength);
        
        Vector3 randomPosition = new Vector3(robotBase.position.x + randomX, robotBase.position.y + randomY, robotBase.position.z + randomZ);
        
        if(Vector3.Distance(randomPosition, robotBase.position) > maxLength)
        {
            Vector3 robotBaseToRdmPos = randomPosition - robotBase.position;
            randomPosition = (maxLength * robotBaseToRdmPos.normalized) + robotBase.position;
        }
        transform.position = randomPosition;
    }

    public void SetRandomSafeValidPosition()
    {
        // Creating random X position and cecking if it lies whithin the accepted threshold
        float randomX = Random.Range(-maxSafeLength, maxSafeLength);
        if(randomX < 0f && Mathf.Abs(randomX) < minLength) randomX = Random.Range(-maxSafeLength, -minLength);
        if(randomX >= 0f && randomX < minLength) randomX = Random.Range(minLength, maxSafeLength);
        
        // Creating random Z position and cecking if it lies whithin the accepted threshold
        float randomZ = Random.Range(-maxSafeLength, maxSafeLength);
        if(randomZ < 0f && Mathf.Abs(randomZ) < minLength) randomZ = Random.Range(-maxSafeLength, -minLength);
        if(randomZ >= 0f && randomZ < minLength) randomZ = Random.Range(minLength, maxSafeLength);
        
        // Creating random Y position
        float randomY = Random.Range(maxLength, maxLength + 0.2f);
        
        Vector3 randomPosition = new Vector3(robotBase.position.x + randomX, robotBase.position.y + randomY, robotBase.position.z + randomZ);
        
        // If distance to random point is larger than the maxLength, project point onto the sphere of maxLength
        if(Vector3.Distance(randomPosition, robotBase.position) > maxLength)
        {
            Vector3 robotBaseToRdmPos = randomPosition - robotBase.position;
            randomPosition = (maxLength * robotBaseToRdmPos.normalized) + robotBase.position;
        }
        transform.position = randomPosition;
    }

    public void SetFixedGoalPoint()
    {
        transform.position = startPosition;
    }
}