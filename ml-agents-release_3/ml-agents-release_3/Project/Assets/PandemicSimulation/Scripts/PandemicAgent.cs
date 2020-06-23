using System.Collections;
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
}
