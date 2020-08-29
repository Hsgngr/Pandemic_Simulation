using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class house : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Collider;
    public bool isLocked;

    public void Lock()
    {
        Collider.SetActive(true);
        isLocked = true;
    }
    public void Unlock()
    {
        Collider.SetActive(false);
        isLocked = false;
    }
    public void resetHouse()
    {
        Collider.SetActive(false);
        isLocked = false;
    }
    public void Start()
    {
        resetHouse();
    }
}
