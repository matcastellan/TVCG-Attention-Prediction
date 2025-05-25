using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckGazeIntersection : MonoBehaviour
{
    public bool gazeIntersected = false;

    private void OnTriggerEnter(Collider other)
    {
        gazeIntersected = true;
        //Debug.Log("Gaze intersected");
    }

    private void OnTriggerStay(Collider other)
    {
        gazeIntersected = true;
        //Debug.Log("Gaze intersected");
    }
}
