using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WireStartEnd : MonoBehaviour
{
    public int index;
    public bool invisibleOnPlay;
    private void Start()
    {
        //makes start and end point invisible upon play because no one wants to see 
        if (Application.isPlaying&&invisibleOnPlay)
        {
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
