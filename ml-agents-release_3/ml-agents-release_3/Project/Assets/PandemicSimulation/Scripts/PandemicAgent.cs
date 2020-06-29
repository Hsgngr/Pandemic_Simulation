using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// A Pandemic Simulation Machine Learning Agent
/// </summary>
public class PandemicAgent : Agent
{
    [Tooltip("The material when the agent is healthy")]
    public Material healthyMaterial;

    [Tooltip("The material when the agent is infected")]
    public Material infectiousMaterial;

    [Tooltip("The material when the bot is infected")]
    public Material recoveredMaterial;

    [Tooltip("The exposure Collider")]
    [HideInInspector]
    public Collider ExposureSphere;

    [Tooltip("The maximum possible distance for exposure to occur aka radius")]
    [HideInInspector]
    public float exposureRadius;

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

    //Rigidbody of the agent
    private Rigidbody rb;

    //The list of n-number agents' directions and distance to this agent inside of the exposure radius.
    List<KeyValuePair<Vector3, float>> directions = new List<KeyValuePair<Vector3, float>>(); //This might be not the correct way so it may be deleted.

    //Starving level; 
    private float starvingLevel = 100f; //again this will not included in MVP

    //Market distance
    private float marketDistance; //It not implemented yet.


    /// <summary>
    /// States for being healthy or infectious
    /// </summary>
    public enum agentStatus
    {
        HEALTHY,
        INFECTED,
        RECOVERED
    }

    [Tooltip("Recovery time after the infection starts")]
    public float recoverTime=50f;

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
                pandemicAreaObj.GetComponent<PandemicArea>().healthyCounter--;
                pandemicAreaObj.GetComponent<PandemicArea>().infectedCounter++;               
                //Add - reward here.
                break;
            case agentStatus.RECOVERED:
                GetComponentInChildren<Renderer>().material = recoveredMaterial;
                pandemicAreaObj.GetComponent<PandemicArea>().infectedCounter--;
                pandemicAreaObj.GetComponent<PandemicArea>().recoveredCounter++;
                break;
        }
    }
    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        //Get the PandemicArea and its settings
        pandemicArea = GetComponentInParent<PandemicArea>();

        pandemicAreaObj = pandemicArea.gameObject;
        GetComponent<SphereCollider>().radius = pandemicArea.exposureRadius;
        recoverTime = pandemicArea.recoverTime;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        
            var localVelocity = transform.InverseTransformDirection(rb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);
            sensor.AddObservation(System.Convert.ToInt32(m_InfectionStatus));                
    }

    /// <summary>
    /// Reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        pandemicArea.ResetPandemicArea();

        //Zero out velocities so that movement stops before a new episode begins
        rb.velocity = Vector3.zero;
    }
    public void MoveAgent(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var forwardAxis = (int)act[0];
        var rightAxis = (int)act[1];
        var rotateAxis = (int)act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward;
                break;
            case 2:
                dirToGo = -transform.forward;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right;
                break;
            case 2:
                dirToGo = -transform.right;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = -transform.up;
                break;
            case 2:
                rotateDir = transform.up;
                break;
        }
        rb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        if (rb.velocity.sqrMagnitude > 25f) // slow it down
        {
            rb.velocity *= 0.95f;
        }
    }
    /// <summary>
    /// Called when an action is received from either the player input or the neural network
    /// 
    /// VectorAction[i] represents:
    /// Index 0: move forward or backward
    /// Index 1: move to right or left
    /// Index 2: Rotate clockwise or counterclockwise
    /// 
    /// To see these actions look at the MoveAgent
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        MoveAgent(vectorAction);
    }
    public override void Heuristic(float[] actionsOut)
    {
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[2] = 2f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[0] = 1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            actionsOut[2] = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            actionsOut[0] = 2f;
        }
        actionsOut[3] = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
    }

    private void UpdateDirectionList() { 
        // I will not do this yet.
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
                if (collider.gameObject.GetComponent<DummyBot>().m_InfectionStatus == DummyBot.agentStatus.INFECTED)
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

    //Moved to public override void Initialize()
    //private void Awake()
    //{
    //    //Get the PandemicArea
    //    pandemicArea = GetComponentInParent<PandemicArea>();
    //    pandemicAreaObj = pandemicArea.gameObject;

    //    GetComponent<SphereCollider>().radius = pandemicArea.exposureRadius;
    //    recoverTime = pandemicArea.recoverTime;
    //}

    private void FixedUpdate()
    {
        //Debug.Log("I'm now infected and time left for my recovery: " + recoverTime);
        if (m_InfectionStatus == agentStatus.INFECTED)
        {
           // Debug.Log("I'm now infected and time left for my recovery: " + recoverTime);
            if (recoverTime <= 0)
            {
                m_InfectionStatus = agentStatus.RECOVERED;
                changeAgentStatus();
            }
            else
            {
                recoverTime -= Time.fixedDeltaTime;
            }
        }
        //Debug.Log("agentStatus: " + System.Convert.ToInt32(m_InfectionStatus));
    }
}
