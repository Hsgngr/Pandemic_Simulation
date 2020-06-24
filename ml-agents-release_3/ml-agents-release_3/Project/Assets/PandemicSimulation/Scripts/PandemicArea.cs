using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public int healthyBotCount = 10;
    public int infectedBotCount = 1;

    [Space(10)]
    public List<GameObject> agents = new List<GameObject>();

    [Header("InfectionSettings")]
    [Tooltip("The maximum possible distance for exposure to occur aka radius")]
    public float exposureRadius = 8f;

    [Tooltip("Propability of getting infected is divided byinfectionCoeff. (1 is most infectious 100 is minimum infectious)")]
    [Range(500f, 1f)]
    public float infectionCoeff = 50f;

    //List of DummyBots
    public List<GameObject> dummyBotList = new List<GameObject>();






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
            GameObject f = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
            f.GetComponent<SphereCollider>().radius = exposureRadius;
            f.GetComponent<DummyBot>().infectionCoeff = infectionCoeff;
            dummyBotList.Add(f);

        }
        //Add default starter infected bots
        for (int i = 0; i < infectedNum; i++)
        {
            GameObject b = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
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


    public void ResetPandemicArea(List<GameObject> agents)
    {
        foreach (GameObject agent in agents)
        {
            Debug.Log("agent: " + agent.name);
            //Restart the status of the agent
            agent.GetComponent<PandemicAgent>().m_InfectionStatus = PandemicAgent.agentStatus.HEALTHY;
            agent.GetComponent<PandemicAgent>().changeAgentStatus();
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

                if (i < healthyBotCount)
                {
                    dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.HEALTHY;
                    dummyBotList[i].GetComponent<DummyBot>().changeAgentStatus();                  
                }
                else
                {

                    dummyBotList[i].GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.INFECTED;
                    dummyBotList[i].GetComponent<DummyBot>().changeAgentStatus();
                }

            }
        }

    }

    public void Awake()
    {
        ResetPandemicArea(agents);

    }
    public void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            ResetPandemicArea(agents);
        }
    }

}
