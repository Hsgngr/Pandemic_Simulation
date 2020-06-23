using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Manages spawning bots and market
/// </summary>
public class PandemicArea : MonoBehaviour
{
    //Range of the area
    public float range;

    //DummyBots
    public GameObject dummyBot;
    public int healthyBotCount=10;
    public int infectedBotCount = 1;

    public GameObject[] agents;



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
        for (int i = 0; i < healthyNum; i++)
        {
            GameObject f = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
        }
        for (int i = 0; i < infectedNum; i++)
        {
            GameObject b = Instantiate(obj, ChooseRandomPosition(), Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
            b.GetComponent<DummyBot>().m_InfectionStatus = DummyBot.agentStatus.INFECTED;
            b.GetComponent<DummyBot>().changeAgentStatus();
        }
    }
    public Vector3 ChooseRandomPosition()
    {
        return new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position;
    }

    
    public void ResetPandemicArea(GameObject[] agents) {
        foreach (GameObject agent in agents)
        {
            if (agent.transform.parent == gameObject.transform)
            {
                agent.transform.position = new Vector3(Random.Range(-range, range), 2f,
                    Random.Range(-range, range))
                    + transform.position;
                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }
        CreateObjectAtRandomPosition(dummyBot,healthyBotCount,infectedBotCount);
    }

    public void Awake()
    {      
        ResetPandemicArea(agents);
        
    }

}
