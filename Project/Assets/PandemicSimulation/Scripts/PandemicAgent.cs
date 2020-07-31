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

    private Vector3 rewardDistance;
    //Starving level;
    [Range(0f, 100f)]
    public float starvingLevel = 100f; //again this will not included in MVP

    //Market distance
    private float marketDistance; //It not implemented yet.

    [Tooltip("Recovery time after the infection starts")]
    public float recoverTime = 50f;
    /// <summary>
    /// States for being healthy or infectious
    /// </summary>
    public enum agentStatus
    {
        HEALTHY,
        INFECTED,
        RECOVERED
    }

    const int NUM_ITEM_TYPES = (int)agentStatus.RECOVERED; //The last state in the enum (returns 2)

    public agentStatus m_InfectionStatus = agentStatus.HEALTHY;

    /// <summary>
    /// When agentStatus changes call this function. 
    /// There may be a better way to do this which combines set status and 
    /// call this function automatically but for now its like this.
    /// </summary>
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

        exposureRadius = pandemicArea.exposureRadius;
        GetComponent<SphereCollider>().radius = exposureRadius;
        recoverTime = pandemicArea.recoverTime;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        float distance = Vector3.Distance(transform.position, pandemicAreaObj.GetComponent<PandemicArea>().rewardCube.transform.position);
        Vector3 direction = transform.position - pandemicAreaObj.GetComponent<PandemicArea>().rewardCube.transform.position;
        var localVelocity = transform.InverseTransformDirection(rb.velocity);

        sensor.AddObservation(starvingLevel/100); // Dividing with 100 for normalization
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        sensor.AddOneHotObservation((int)m_InfectionStatus, NUM_ITEM_TYPES); //A shortcut for one-hot-style observations.
        sensor.AddObservation(distance);
        sensor.AddObservation(direction.normalized);
        
        //Infection sayısının healthy saysına oranı vs verilebilir but not yet.
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

    /// <summary>
    /// When Behaviour Type is set to  "Heuristic Only" on the agent's Behaviour Parameters,
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(float[])"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut"> output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        if (Input.GetKey(KeyCode.A))
        {
            actionsOut[0] = 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[0] = 2f;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            actionsOut[0] = 0f;
        }
    }
    /// <summary>
    /// moves the agent
    /// </summary>
    /// <param name="act">there is 3 type of action which are forward-backward,
    /// left-right and rotate</param>
    public void MoveAgent(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var rotateAxis = (int)act[0];

        dirToGo = transform.forward;

        switch (rotateAxis)
        {
            case 0:
                rotateDir = Vector3.zero;
                break;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("target"))
        {
            float tempReward = 1 - (starvingLevel / 100);
            AddReward(1- tempReward);
            collision.gameObject.transform.position = pandemicArea.ChooseRandomPosition();
            starvingLevel = 100f;
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
                if (collider.gameObject.GetComponent<DummyBot>().m_InfectionStatus == DummyBot.agentStatus.INFECTED && collider.gameObject.GetComponent<DummyBot>().isFrozen== false)
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

        //Debug.Log("exposureRadius: " + exposureRadius);
        //Debug.Log("Distance: " + distance);
        //Debug.Log("InfectionCoeff : " + infectionCoeff);
        //Debug.Log("Probability of getting infected is: " + probability);

        if (Random.Range(0f, 1f) < probability)
        {
            //Debug.Log("You got infected");
            m_InfectionStatus = agentStatus.INFECTED;
            changeAgentStatus();
            AddReward(-1f);
            EndEpisode();
        }
    }

    //Moved to public override void Initialize()
    private void Awake()
    {
        ////Get the PandemicArea
        //pandemicArea = GetComponentInParent<PandemicArea>();
        //pandemicAreaObj = pandemicArea.gameObject;

        //GetComponent<SphereCollider>().radius = pandemicArea.exposureRadius;
        //recoverTime = pandemicArea.recoverTime;
        Initialize();
    }

    private void FixedUpdate()
    {
        //Survive Bonus
        AddReward(0.001f);
        
        if(starvingLevel <= 0f)
        {
            AddReward(-1f);
            EndEpisode();
        }
        else
        {
            starvingLevel = starvingLevel - 0.05f;
        }
        if (m_InfectionStatus == agentStatus.HEALTHY)
        {
            //Debug.Log("reward: " + reward);
            
        }
        //Debug.Log("I'm now infected and time left for my recovery: " + recoverTime);
        else if (m_InfectionStatus == agentStatus.INFECTED)
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
    }
}
