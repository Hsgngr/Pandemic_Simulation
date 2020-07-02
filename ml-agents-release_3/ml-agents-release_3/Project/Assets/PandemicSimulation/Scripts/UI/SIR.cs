using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SIR : MonoBehaviour
{
    private Text text;
    private PandemicArea pandemicArea;
    
    // Start is called before the first frame update
    void Start()
    {
        pandemicArea = GetComponentInParent<PandemicArea>();
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        text.text = "Total Healthy Agents = " + pandemicArea.healthyCounter + "\n" +
                    "Total Infected Agents =" + pandemicArea.infectedCounter;
    }
}
