﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Pandemic Simulation Machine Learning Agent
/// </summary>
public class PandemicAgent : MonoBehaviour
{
    [Tooltip("The material when the agent is healthy")]
    public Material healthyMaterial;

    [Tooltip("The material when the agent is infected")]
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
        //Check if its a dummyBot   
        if (collider.CompareTag("dummyBot"))
        {
            //If it is infected 
            if (collider.gameObject.GetComponent<DummyBot>().m_InfectionStatus == DummyBot.agentStatus.INFECTED)
            {
                //Distance between two agents
                float distance = Vector3.Distance(collider.gameObject.transform.position, transform.position);
                probability = Mathf.InverseLerp(exposureRadius, 0, distance) / 100;

                //Debug.Log("Probability of getting infected is: " + probability);

                if (Random.Range(0f, 1f) < probability)
                {
                    // Debug.Log("You got infected");
                    m_InfectionStatus = agentStatus.INFECTED;
                    changeAgentStatus();
                }
            }
        }
        //Check if it is an agent
        else if (collider.CompareTag("agent"))
        {
            //Check if it is infected
            if (collider.gameObject.GetComponent<PandemicAgent>().m_InfectionStatus ==  agentStatus.INFECTED)
            {             
                //Distance between two agents
                float distance = Vector3.Distance(collider.gameObject.transform.position, transform.position);
                probability = Mathf.InverseLerp(exposureRadius, 0, distance) / 100;

                //Debug.Log("Probability of getting infected is: " + probability);

                if (Random.Range(0f, 1f) < probability)
                {
                    Debug.Log("Agent got infected");
                    m_InfectionStatus = agentStatus.INFECTED;
                    changeAgentStatus();
                }

            }
        }



    }
}
