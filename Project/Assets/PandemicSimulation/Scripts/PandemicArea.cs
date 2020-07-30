using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using Unity.MLAgents;
using UnityEngine;
/// <summary>
/// Manages spawning bots and market
/// </summary>
public class PandemicArea : MonoBehaviour
{
    [Tooltip("Range of the area")]
    public float range;

    [Header("DummyBots")]
    public GameObject dummyBot;
    public int healthyBotCount;
    public int infectedBotCount;

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

    /// <summary>
    /// Creates objects in random position at given amount
    /// </summary>
    /// <param name="obj">The object which will be initialized</param>
    /// <param name="num">Number of objects </param>
    public void CreateObjectAtRandomPosition(GameObject obj, int num)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
        }
    }
    /// <summary>
    /// Creates objets in random position with given amount of healthy and infected agents
    /// </summary>
    /// <param name="obj"> The object which will be instantiated</param>
    /// <param name="goodNum">The number of healthy agents</param>
    /// <param name="infectedNum">The number of infected agents</param>
    public void CreateObjectAtRandomPosition(GameObject obj, int healthyNum, int infectedNum)
    {
        //Add default healthy bots
        for (int i = 0; i < healthyNum; i++)
        {
            //Instantiate the dummyBot with choosenRandom,Position and choosenRandom rotation inside of the Pandemic Area Object
            GameObject f = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)), transform);
            f.GetComponent<SphereCollider>().radius = exposureRadius;
            f.GetComponent<DummyBot>().infectionCoeff = infectionCoeff;
            dummyBotList.Add(f);

        }
        //Add default starter infected bots
        for (int i = 0; i < infectedNum; i++)
        {
            //Instantiate the dummyBot with choosenRandom,Position and choosenRandom rotation inside of the Pandemic Area Object
            GameObject b = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)), transform);
            b.GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.INFECTED;
            b.GetComponent<DummyBot>().changeAgentStatus();
            b.GetComponent<SphereCollider>().radius = exposureRadius;
            dummyBotList.Add(b);
        }
    }
    public Vector3 ChooseRandomPosition()
    {
        return new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position;
    }
    /// <summary>
    /// A more complex version of ChooseRandomPosition(), User can set the wanted range.
    /// </summary>
    /// <param name="range">defines the square area that vector3 can be selected</param>
    /// <returns></returns>
    public Vector3 ChooseRandomPosition(float range)
    {
        return new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position;
    }

    public void ResetPandemicArea()
    {
        rewardCube.transform.position = ChooseRandomPosition(range/2); // We want reward near to middle.

        //Reset infectedCounter and healthyCounter
        infectedCounter = 0;
        HealthyCounter = healthyBotCount + infectedBotCount; //Count all of them and infected ones will be removed from DummyBot.cs
        recoveredCounter = 0;

        foreach (GameObject agent in agents)
        {
            //Restart the status of the agent
            agent.GetComponent<PandemicAgent>().m_InfectionStatus = PandemicAgent.agentStatus.HEALTHY;
            agent.GetComponent<PandemicAgent>().changeAgentStatus();
            agent.GetComponent<PandemicAgent>().infectionCoeff = infectionCoeff;
            agent.GetComponent<PandemicAgent>().recoverTime = recoverTime;
            agent.GetComponent<PandemicAgent>().starvingLevel = 100;
            //Randomly 
            agent.transform.position = ChooseRandomPosition();
            agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        }
        //If its first time then List should be empty, Check if it empty
        if (dummyBotList.Count == 0)
        {
            CreateObjectAtRandomPosition(dummyBot, healthyBotCount, infectedBotCount);
        }
        else
        {
            //Reset every dummyBot in the list
            for (int i = 0; i < dummyBotList.Count; i++)
            {
                dummyBotList[i].transform.position = ChooseRandomPosition();
                dummyBotList[i].GetComponent<DummyBot>().nextActionTime = -1f;
                dummyBotList[i].GetComponent<DummyBot>().recoverTime = recoverTime; //Reset the recoverTime also
                dummyBotList[i].GetComponent<DummyBot>().StartCoroutine(dummyBotList[i].GetComponent<DummyBot>().WaitAtStart(1f)); //Frezee bots at the start of the episode.
                if (i < healthyBotCount)
                {
                    dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.HEALTHY;

                }
                else
                {
                    dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.INFECTED;

                }
                dummyBotList[i].GetComponent<DummyBot>().changeAgentStatus();
            }
        }

    }
    

    public void Awake()
    {
        exportObj = GetComponentInChildren<ExportCsv>().gameObject;
        exportObj.GetComponent<ExportCsv>().addHeaders();

        //Find child agents of this pandemicArea
        foreach (PandemicAgent agentSript in GetComponentsInChildren<PandemicAgent>())
        {
            agents.Add(agentSript.gameObject);
        }

        ResetPandemicArea();
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPandemicArea();
        }
    }

}
