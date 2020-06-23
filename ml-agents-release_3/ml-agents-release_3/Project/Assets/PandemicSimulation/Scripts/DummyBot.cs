using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a single DummyBot who acts randomly on the environment.
/// </summary>
public class DummyBot : MonoBehaviour
{
    [Tooltip("The material when the bot is healthy")]
    public Material healthyMaterial;

    [Tooltip("The material when the bot is infected")]
    public Material infectiousMaterial;

    [Tooltip("The exposure Collider")]
    public Collider ExposureSphere;

    [Tooltip("The maximum possible distance for exposure to occur aka radius")]
    public float exposureRadius = 8f;

    [Tooltip("The probability of exposure at that maximum distance")]
    [Range(0.0f, 0.001f)]
    public float probability;

    //The PandemicArea
    private PandemicArea pandemicArea;

    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;

    //Check if agent is frozen or not;
    public bool isFrozen = false;
    //Targeted Position
    private Vector3 targetPosition;
    private float nextActionTime = -1f;



    /// <summary>
    /// States for being healthy or infectious
    /// </summary>
    public enum agentStatus
    {
        HEALTHY,
        INFECTED
    }

    public agentStatus m_InfectionStatus = agentStatus.HEALTHY;

    public void changeAgentStatus()
    {
        switch (m_InfectionStatus)
        {
            case agentStatus.HEALTHY:
            GetComponentInChildren<Renderer>().material = healthyMaterial;
                break;
            case agentStatus.INFECTED:
                GetComponentInChildren<Renderer>().material = infectiousMaterial;
                //Add - reward here.
                break;
        }
    }

  

    private void Awake()
    {
        pandemicArea = FindObjectOfType<PandemicArea>();
        targetPosition = pandemicArea.ChooseRandomPosition();
      
    }

    private void FixedUpdate()
    {
        if (!isFrozen)
        {
            moveRandomTarget();
        }

    }
    /// <summary>
    /// Bot choose a random point in the map and moves towards this target until it reaches. 
    /// When it reaches it finds another target.
    /// </summary>
    private void moveRandomTarget()
    {
        if (Time.fixedTime >= nextActionTime)
        {
            // Pick a random target
            targetPosition = pandemicArea.ChooseRandomPosition();

            // Rotate toward the target
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);

            // Calculate the time to get there
            float timeToGetThere = Vector3.Distance(transform.position, targetPosition) / moveSpeed;
            nextActionTime = Time.fixedTime + timeToGetThere;
        }
        else
        {
            // Make sure that the fish does not swim past the target
            Vector3 moveVector = moveSpeed * transform.forward * Time.fixedDeltaTime;
            if (moveVector.magnitude <= Vector3.Distance(transform.position, targetPosition))
            {
                transform.position += moveVector;
            }
            else
            {
                transform.position = targetPosition;
                nextActionTime = Time.fixedTime;
            }
        }
    }


    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Called when the agent's collider stays in a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Handles when the agen'ts collider enters or stays in a trigger collider
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        //Check if its an agent
        // Değilse bişi yapma infected ise distance'ı al. distance 0 a eşit demek maximum exposure demek distance radius'a eşit demek minimum exposure demek. arasını linear arttır.
        if (collider.CompareTag("agent"))
        {
            if(collider.gameObject.GetComponent<DummyBot>().m_InfectionStatus == agentStatus.INFECTED) {

                //Distance between two agents
                float distance = Vector3.Distance(collider.gameObject.transform.position, transform.position);
                probability = Mathf.InverseLerp(exposureRadius, 0, distance) /100;

                //Debug.Log("Probability of getting infected is: " + probability);

                if (Random.Range(0f, 1f) < probability)
                {
                    // Debug.Log("You got infected");
                    m_InfectionStatus = agentStatus.INFECTED;
                    changeAgentStatus();
                }

            }
            
        }
    }
}
