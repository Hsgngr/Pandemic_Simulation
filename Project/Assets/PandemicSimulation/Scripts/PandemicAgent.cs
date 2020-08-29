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

    //StatRecorder
    private statRecorder statRecorder;

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

    //Environment Reset Parameters
    public EnvironmentParameters m_ResetParams;

    [Tooltip("Recovery time after the infection starts")]
    public float recoverTime = 50f;

    [Tooltip("Number of infected bots at start")]
    public int infectedCount;

    [Tooltip("Number of healthy bots at start")]
    public int healthyCount;

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
                pandemicAreaObj.GetComponent<PandemicArea>().InfectedCounter++;
                //Add - reward here.
                break;
            case agentStatus.RECOVERED:
                GetComponentInChildren<Renderer>().material = recoveredMaterial;
                pandemicAreaObj.GetComponent<PandemicArea>().InfectedCounter--;
                pandemicAreaObj.GetComponent<PandemicArea>().RecoveredCounter++;
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
        statRecorder = pandemicAreaObj.GetComponentInChildren<statRecorder>();

        //define parameters as environment parameters for randomization and curriculum learning
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        float distance = Vector3.Distance(transform.position, pandemicAreaObj.GetComponent<PandemicArea>().rewardCube.transform.position);
        Vector3 direction = transform.position - pandemicAreaObj.GetComponent<PandemicArea>().rewardCube.transform.position;
        var localVelocity = transform.InverseTransformDirection(rb.velocity);

        //sensor.AddObservation(starvingLevel/100); // Dividing with 100 for normalization
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
        sensor.AddOneHotObservation((int)m_InfectionStatus, NUM_ITEM_TYPES); //A shortcut for one-hot-style observations.

        //Observations for getting reward easily
        //sensor.AddObservation(distance);
        //sensor.AddObservation(direction.normalized);

        //Infection sayısının healthy saysına oranı vs verilebilir but not yet.
        //sensor.AddObservation(pandemicArea.infectedBotCount);

    }

    /// <summary>
    /// Reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        pandemicArea.ResetPandemicArea();
        SetResetParameters();

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
            actionsOut[0] = 0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            actionsOut[0] = 1f;
        }
        else
        {
            actionsOut[0] = 2f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[1] = 0f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            actionsOut[1] = 1f;
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

        var direction = (int)act[1];

        switch (rotateAxis)
        {
            case 0:
                rotateDir = -transform.up;
                break;
            case 1:
                rotateDir = transform.up;
                break;
            case 2:
                rotateDir = Vector3.zero;
                break;
        }
        switch (direction)
        {
            case 0:
                dirToGo = transform.forward;
                break;
            case 1:
                dirToGo = -transform.forward;
                break;
        }

        rb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        if (rb.velocity.sqrMagnitude > 25f) // slow it down
        {
            rb.velocity *= 0.95f;
        }
    }

    public void SetResetParameters()
    {
        exposureRadius = m_ResetParams.GetWithDefault("exposureRadius", pandemicArea.exposureRadius);
        GetComponent<SphereCollider>().radius = exposureRadius;
        recoverTime = m_ResetParams.GetWithDefault("recoverTime", pandemicArea.recoverTime);
        infectionCoeff = m_ResetParams.GetWithDefault("infectionCoeff", pandemicArea.infectionCoeff);

        //healthyCount = (int) m_ResetParams.GetWithDefault("healthyCount", pandemicArea.healthyBotCount);
        //infectedCount =(int)m_ResetParams.GetWithDefault("infectedCount", pandemicArea.infectedBotCount);

        //pandemicArea.healthyBotCount = healthyCount;
        //pandemicArea.infectedBotCount = infectedCount;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("target"))
        {
            float tempReward = 1 - (starvingLevel / 100);
            //AddReward(1- tempReward);
            AddReward(1f);
            //Add in TotalScore
            statRecorder.totalScore += 1;

            collision.gameObject.transform.position = pandemicArea.ChooseRandomPosition();
            starvingLevel = 100f;
        }
        if (m_InfectionStatus == agentStatus.HEALTHY)
        {
            if (collision.gameObject.CompareTag("agent"))
            {
                //Each agent will count this therefore its half.
                statRecorder.collisionCounts += 0.5f;
            }
            else if (collision.gameObject.CompareTag("dummyBot"))
            {
                statRecorder.collisionCounts += 1f;
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
                if (collider.gameObject.GetComponent<DummyBot>().m_InfectionStatus == DummyBot.agentStatus.INFECTED && collider.gameObject.GetComponent<DummyBot>().isFrozen == false)
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
            SetReward(-1f);
            statRecorder.infectedCounts += 1;

            //EndEpisode();  
            //If infector is an agent then also penalize it too for infecting someone. Shame!
            if (infector.GetComponent<PandemicAgent>())
            {
                infector.GetComponent<PandemicAgent>().AddReward(-1f);
                statRecorder.totalScore += -1;

            }

        }
    }

    //Moved to public override void Initialize()
    private void Awake()
    {
        Initialize();
    }

    private void FixedUpdate()
    {
        if (starvingLevel <= 0f)
        {
            //AddReward(-1f);
            //EndEpisode();
        }
        else
        {
            starvingLevel = starvingLevel - 0.05f;
        }
        if (m_InfectionStatus == agentStatus.HEALTHY)
        {
            //Survive Bonus
            SetReward(0.01f);

            //Add to totalScore also
            statRecorder.totalScore += 0.01f;
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
