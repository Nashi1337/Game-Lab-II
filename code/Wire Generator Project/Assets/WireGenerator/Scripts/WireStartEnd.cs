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
        if (Application.isPlaying&&invisibleOnPlay)
        {
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
