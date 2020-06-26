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

    [Tooltip("The material when the bot is infected")]
    public Material recoveredMaterial;

    [Tooltip("The maximum possible distance for exposure to occur aka radius")]
    [HideInInspector]
    public float exposureRadius = 8f;

    [Tooltip("Infection Coefficient")]
    [HideInInspector]
    public float infectionCoeff;

    [Tooltip("The probability of exposure at that maximum distance")]
    [Range(0.0f, 0.001f)]
    public float probability;

    //The PandemicArea
    private PandemicArea pandemicArea;

    //The gameObject of the Pandemic Area
    private GameObject pandemicAreaObj;

    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;

    //Check if agent is frozen or not;
    public bool isFrozen = false;
    //Targeted Position
   
    //When a target selected Distance is divided by velocity (moveSpeed) and it give nextActionTime
    [HideInInspector]
    public float nextActionTime = -1f;
    private Vector3 targetPosition;


    /// <summary>
    /// States for being healthy or infectious
    /// </summary>
    public enum agentStatus
    {
        HEALTHY,
        INFECTED,
        RECOVERED
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
                pandemicAreaObj.GetComponent<PandemicArea>().infectedCounter++;
                //Add - reward here.
                break;
            case agentStatus.RECOVERED:
                GetComponentInChildren<Renderer>().material = recoveredMaterial;
                pandemicAreaObj.GetComponent<PandemicArea>().recoveredCounter++;               
                break;
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
        
        //Check if our agent is healthy, otherwise there is nothing like reinfection
        if (m_InfectionStatus == agentStatus.HEALTHY)
        {
           
            //Check if its a dummyBot   
            if (collider.CompareTag("dummyBot"))
            {
                //If it is infected 
                if (collider.gameObject.GetComponent<DummyBot>().m_InfectionStatus == agentStatus.INFECTED)
                {
                    exposeInfection(collider.gameObject);
                }
            }
            //Check if it is an agent
            else if (collider.CompareTag("agent"))
            {
                //Check if it is infected
                if (collider.gameObject.GetComponent<PandemicAgent>().m_InfectionStatus == PandemicAgent.agentStatus.INFECTED)
                {
                    exposeInfection(collider.gameObject);
                }
            }
        }

    }
    /// <summary>
    /// Gets the distance between agents and expose with infenction probability.
    /// There is an inverse proportion between distance and infection probability.
    /// </summary>
    /// <param name="infector">The agent who is inside of the collider</param>
    private void exposeInfection(GameObject infector)
    {
        //Distance between two agents
        float distance = Vector3.Distance(infector.transform.position, transform.position);
        probability = Mathf.InverseLerp(exposureRadius, 0, distance) / infectionCoeff;

        //Debug.Log("Probability of getting infected is: " + probability);

        if (Random.Range(0f, 1f) < probability)
        {
            // Debug.Log("You got infected");
            m_InfectionStatus = agentStatus.INFECTED;
            changeAgentStatus();
        }
    }

    private void Awake()
    {
        pandemicArea = FindObjectOfType<PandemicArea>();
        pandemicAreaObj = pandemicArea.gameObject;
        targetPosition = pandemicArea.ChooseRandomPosition();
        GetComponent<SphereCollider>().radius = exposureRadius;

    }
    private void FixedUpdate()
    {
        if (!isFrozen)
        {
            moveRandomTarget();
        }
        if(m_InfectionStatus == agentStatus.INFECTED)
        {

        }

    }
}
