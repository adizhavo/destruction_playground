using System.Collections;
using UnityEngine;

public class DestroyDelay : MonoBehaviour
{
    public float waitTime = 8;
    
    private void Start()
    {
        Destroy(gameObject, waitTime);
    }
}

