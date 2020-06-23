using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfectionMechanism : MonoBehaviour
{
    [Tooltip("The maximum possible distance for exposure to occur aka radius")]
    public float exposureRadius;

    [Tooltip("The maximum possible distance for exposure to occur aka radius")]
    public Collider ExposureSphere;

    [Tooltip("The probability of exposure at that maximum distance")]
    public float exposureProbability;

}
