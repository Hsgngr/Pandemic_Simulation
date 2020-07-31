using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reward : MonoBehaviour
{
    private GameObject pandemicAreaObj;
    //private bool isTaken = false;


    private void Awake()
    {
        pandemicAreaObj = GetComponentInParent<PandemicArea>().gameObject;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("agent"))
        {
            
        }
        
    }
    private IEnumerator WaitAndDeploy(float waitTime)
    {
        while (true)
        {
            gameObject.SetActive(false);
            yield return new WaitForSeconds(waitTime);
            print("WaitAndPrint " + Time.time);
        }
    }

}
