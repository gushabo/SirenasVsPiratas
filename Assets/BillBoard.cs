using System;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    public Transform lookAtTarget;
    public GameObject slider;


    private void Awake()
    {
        lookAtTarget = GameObject.FindGameObjectWithTag("Camera").transform;
    }

    private void Update()
    {
        if (lookAtTarget == null) return;
        
        slider.transform.SetParent(null);
        slider.transform.position = gameObject.transform.position;
        slider.transform.rotation = Quaternion.Euler(new Vector3(90, -98, 0));
        
    }
}
