using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class environmentParameters : MonoBehaviour
{

    public int healthyBotCount;
    public int infectedBotCount;

    //Environment Reset Parameters
    public EnvironmentParameters m_ResetParams;

    private void Awake()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

}
