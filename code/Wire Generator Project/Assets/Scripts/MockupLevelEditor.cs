using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MockupLevelEditor : MonoBehaviour
{
    public GameObject wirePrefab;

    public Text StartX;
    public Text StartY;
    public Text StartZ;

    public Text EndX;
    public Text EndY;
    public Text EndZ;

    GameObject startPoint;
    GameObject endPoint;
    GameObject wire;

    private void Update()
    {
        if(startPoint!=null && endPoint != null)
        {
            StartX.text = startPoint.transform.position.x.ToString();
            StartY.text = startPoint.transform.position.y.ToString();
            StartZ.text = startPoint.transform.position.z.ToString();
            EndX.text = endPoint.transform.position.x.ToString();
            EndY.text = endPoint.transform.position.y.ToString();
            EndZ.text = endPoint.transform.position.z.ToString();
        }
    }
    public void GenerateStartPoint()
    {
        startPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startPoint.transform.position = new Vector3(0, 0, 0);
        startPoint.name = "startPointGenerated";
        startPoint.tag = "startPoint";
    }
    public void GenerateEndPoint()
    {
        endPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endPoint.transform.position = new Vector3(2, 2, 2);
        endPoint.name = "endPointGenerated";
        endPoint.tag = "endPoint";
    }

    public void GenerateWire()
    {
        if (wirePrefab != null)
        {
            wire = Instantiate(wirePrefab);
            wire.transform.position = Vector3.zero;
            wire.name = "WireGenerated";
        }
    }
}
