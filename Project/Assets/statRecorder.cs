using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class statRecorder : MonoBehaviour
{
    public PandemicArea pandemicArea;

    StatsRecorder m_Recorder;
    public float totalScore;
    public int infectedCounts;
    public float collisionCounts;
    public Text scoreText;

    void Start()
    {
        pandemicArea = GetComponentInParent<PandemicArea>();
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = $"Score: {infectedCounts}";
        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0)
        {
            m_Recorder.Add("TotalScore", totalScore);
            m_Recorder.Add("InfectedCounts", infectedCounts);
            m_Recorder.Add("CollisionCounts", collisionCounts/2);
        }
    }
}
