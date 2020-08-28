using Grpc.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.Barracuda;
using Unity.MLAgents;
using UnityEngine;
/// <summary>
/// Manages spawning bots and market
/// </summary>
public class PandemicArea : MonoBehaviour
{
    [Tooltip("Is this training")]
    public bool isTraining;
    public int agentCount;
    public GameObject agent;

    [Tooltip("Range of the area")]
    public float range;

    [Header("DummyBots")]
    public GameObject dummyBot;
    public int healthyBotCount;
    public int infectedBotCount;

    [Tooltip("The total number of allowed bots which can be spawn in pandemicArea.")]
    public int botLimit;

    //List of DummyBots
    public List<GameObject> dummyBotList = new List<GameObject>();
    [Space(10)]
    //List of Agents
    public List<GameObject> agents = new List<GameObject>();


    [Header("InfectionSettings")]
    [Tooltip("The maximum possible distance for exposure to occur aka radius (Default 8f)")]
    public float exposureRadius = 8f;

    [Tooltip("Propability of getting infected is divided byinfectionCoeff. (1 is most infectious 100 is minimum infectious)")]
    [Range(500f, 1f)]
    public float infectionCoeff = 50f;

    [Tooltip("Recovery time after the infection starts")]
    public float recoverTime = 50f;

    [Header("SIR Model")]
    private GameObject exportObj;
    [System.NonSerialized]
    public int healthyCounter;
    [System.NonSerialized]
    public int infectedCounter = 0;

    [System.NonSerialized]
    public int recoveredCounter = 0;

    public int HealthyCounter
    {
        get => healthyCounter;
        set
        {
            healthyCounter = value;
            exportObj.GetComponent<ExportCsv>().record();
        }
    }
    public int InfectedCounter
    {
        get => infectedCounter;
        set
        {
            infectedCounter = value;
            exportObj.GetComponent<ExportCsv>().record();
        }
    }

    public int RecoveredCounter
    {
        get => recoveredCounter;
        set
        {
            recoveredCounter = value;
            exportObj.GetComponent<ExportCsv>().record();
        }
    }
    //reward cube
    public GameObject rewardCube;

    //Environment Reset Parameters
    public EnvironmentParameters m_ResetParams;

    //Is there any agent in the environment
    private bool isAgentExist;

    /// <summary>
    /// Instantiates dummybots.
    /// </summary>
    /// <param name="botCount">This is the maximum amount of bots that you can use. You need to define this before running the simulation.</param>
    public void CreateBotAtRandomPosition(int botCount)
    {
        for (int i = 0; i < botCount; i++)
        {
            //Instantiate the dummyBot with choosenRandom,Position and choosenRandom rotation inside of the Pandemic Area Object
            GameObject f = Instantiate(dummyBot, Vector3.zero, Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)), transform);
            dummyBotList.Add(f);
        }
        resetDummyBots();
    }
    /// <summary>
    /// Empty function for choosing random position inside of the area. DummyBots are using as they decide their next target.
    /// </summary>
    /// <returns></returns>
    public Vector3 ChooseRandomPosition()
    {
        return new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position;
    }
    /// <summary>
    /// A more complex version of ChooseRandomPosition(), User can set the wanted range.
    /// </summary>
    /// <param name="range">defines the square area that vector3 can be selected</param>
    /// <returns>a random position for object</returns>
    public Vector3 ChooseRandomPosition(float range)
    {
        return new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position;
    }
    /// <summary>
    /// This version of the function gets 2 different values which are max and min ranges. 
    /// It divides the square to an inner and outer square.
    /// Agents will be spawned in the outer square, bots and reward will be spawned in inner square.
    /// </summary>
    /// <param name="maxRange">maximum range that it can take. Mostly this will be equal to range itself.</param>
    /// <param name="minRange">minimum range, this will help to seperate areas between squares.</param>
    /// <returns>a random position for object </returns>
    public Vector3 ChooseRandomPosition(float maxRange, float minRange)
    {
        float randNum = Random.value;
        if (randNum >= 0.5)
        {
            return new Vector3(Random.Range(minRange, maxRange), 1f,
                Random.Range(-range, range)) + transform.position;
        }
        else
        {
            return new Vector3(Random.Range(-maxRange, -minRange), 1f,
                Random.Range(-range, range)) + transform.position;
        }
    }
    /// <summary>
    /// Core method for resetting the simulation area. Every end of the episode area reset itself.
    /// </summary>
    public void ResetPandemicArea()
    {
        //Environment Parameters (This is required for Curriculum Learning)
        if (isAgentExist)
        {
            healthyBotCount = (int)m_ResetParams.GetWithDefault("healthyCount", healthyBotCount);
            infectedBotCount = (int)m_ResetParams.GetWithDefault("infectedCount", infectedBotCount);
        }
        rewardCube.transform.position = ChooseRandomPosition(range * 2 / 5); // We want reward close to middle

        //Reset infectedCounter and healthyCounter
        infectedCounter = 0;
        healthyCounter = healthyBotCount + infectedBotCount + agents.Count; //Count all of them and infected ones will be removed from DummyBot.cs
        recoveredCounter = 0;

        if (isAgentExist)
        {
            foreach (GameObject agent in agents)
            {
                //Restart the status of the agent
                if (agent.GetComponent<PandemicAgent>().m_InfectionStatus != PandemicAgent.agentStatus.HEALTHY)
                {
                    agent.GetComponent<PandemicAgent>().m_InfectionStatus = PandemicAgent.agentStatus.HEALTHY;
                    agent.GetComponent<PandemicAgent>().changeAgentStatus();
                }
                agent.GetComponent<PandemicAgent>().infectionCoeff = infectionCoeff;
                agent.GetComponent<PandemicAgent>().recoverTime = recoverTime;
                agent.GetComponent<PandemicAgent>().starvingLevel = 100;
                //Randomly
                if (!isTraining)
                {
                    agent.transform.position = ChooseRandomPosition(range);
                }
                else
                {
                    agent.transform.position = ChooseRandomPosition(range, range / 2);
                }

                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }
        //If its first time then List should be empty, Check if it empty
        if (dummyBotList.Count == 0)
        {
            CreateBotAtRandomPosition(botLimit);
        }
        if (healthyBotCount + infectedBotCount > botLimit)
        {
            Debug.LogWarning(" Total bot number cannot exceed botLimit. In order to increase the total number of bots, increase the bot limit and restart again.");
        }
        //Set Active bots as they needed, not more than that.
        for (int i = 0; i < dummyBotList.Count; i++)
        {
            if (i < infectedBotCount + healthyBotCount)
            {
                dummyBotList[i].SetActive(true);
            }
            else
            {
                dummyBotList[i].SetActive(false);
            }
        }
        resetDummyBots();

        healthyCounter = healthyBotCount + agents.Count;
        infectedCounter = infectedBotCount;
    }
    /// <summary>
    /// Divides bots to healthy and infected.
    /// </summary>
    public void resetDummyBots()
    {
        //Reset every dummyBot in the list
        for (int i = 0; i < dummyBotList.Count; i++)
        {
            if (dummyBotList[i].activeSelf)
            {
                if (!isTraining)
                {
                    dummyBotList[i].transform.position = ChooseRandomPosition(range);
                }
                else
                {
                    dummyBotList[i].transform.position = ChooseRandomPosition(range / 2);
                }

                dummyBotList[i].GetComponent<DummyBot>().nextActionTime = -1f;
                dummyBotList[i].GetComponent<DummyBot>().recoverTime = recoverTime; //Reset the recoverTime also
                dummyBotList[i].GetComponent<DummyBot>().StartCoroutine(dummyBotList[i].GetComponent<DummyBot>().WaitAtStart(1f)); //Frezee bots at the start of the episode.
                dummyBotList[i].GetComponent<SphereCollider>().radius = exposureRadius;
                dummyBotList[i].GetComponent<DummyBot>().infectionCoeff = infectionCoeff;
                if (i < healthyBotCount)
                {
                    //If its not already has the healthyStatus, change it.
                    if (dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus != DummyBot.agentStatus.HEALTHY)
                    {
                        dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.HEALTHY;
                        dummyBotList[i].GetComponent<DummyBot>().changeAgentStatus();
                    }
                }
                else if (i < infectedBotCount + healthyBotCount)
                {
                    //If its not already has the infectedStatus, change the status.
                    if (dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus != DummyBot.agentStatus.INFECTED)
                    {

                        dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.INFECTED;
                        dummyBotList[i].GetComponent<DummyBot>().changeAgentStatus();
                    }
                }
            }
        }


    }


    public void Awake()
    {
        exportObj = GetComponentInChildren<ExportCsv>().gameObject;
        exportObj.GetComponent<ExportCsv>().addHeaders();

        if (GetComponentInChildren<PandemicAgent>())
            isAgentExist = true;
        else isAgentExist = false;

        //Find child agents of this pandemicArea
        if (!isTraining)
        {
            for (int i = 0; i < agentCount; i++)
            {
                GameObject f = Instantiate(agent, ChooseRandomPosition(range), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)), transform);
                if (i < agentCount / 2)
                {
                    f.GetComponent<SphereCollider>().radius = exposureRadius / 2;

                }
                else
                {
                    f.GetComponent<SphereCollider>().radius = exposureRadius;
                }
                f.GetComponent<SphereCollider>().radius = exposureRadius;

                agents.Add(f);
            }
        }
        else
        {
            if (isAgentExist)
            {
                foreach (PandemicAgent agentSript in GetComponentsInChildren<PandemicAgent>())
                {
                    agents.Add(agentSript.gameObject);
                }
            }
        }

    }
    public void Start()
    {
        if (isAgentExist)
        {
            m_ResetParams = agents[0].GetComponent<PandemicAgent>().m_ResetParams;
            healthyBotCount = (int)m_ResetParams.GetWithDefault("healthyCount", healthyBotCount);
            infectedBotCount = (int)m_ResetParams.GetWithDefault("infectedCount", infectedBotCount);
        }
        else //If an agent exist in the environment it will call the ResetPandemicArea() function in OnEpisodeBegin() anyway. So dont call twice.
        {
            if (!isTraining){
                ResetPandemicArea();
            }
        }


    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPandemicArea();
            if (isAgentExist)
            {
                foreach (GameObject agent in agents)
                {
                    agent.GetComponent<PandemicAgent>().SetResetParameters();
                }
            }
            //When restart simulation restart also values
            //Actually useless in a way we will not use Restart key during the simulation.
            exportObj.GetComponent<ExportCsv>().record();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            foreach (GameObject agent in agents)
            {
                agent.GetComponent<SphereCollider>().radius = exposureRadius * 4;
            }
            foreach (GameObject bot in dummyBotList)
            {
                bot.GetComponent<SphereCollider>().radius = exposureRadius * 4;
            }
        }
    }

}
