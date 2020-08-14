using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class rewardCounter : MonoBehaviour
{
    private Text text;
    private PandemicAgent agent;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
        agent = transform.parent.parent.GetComponentInChildren<PandemicAgent>();
    }
    void LateUpdate()
    {
        decimal cumulativeReward = Decimal.Round((decimal)agent.GetCumulativeReward(), 2);
        text.text = "Cumulative Reward: " + cumulativeReward;

    }
}
