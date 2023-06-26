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

    public GameObject startPoint;
    public GameObject endPoint;

    public Slider Corners;
    GameObject wire;

    bool showWire;

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
        GameObject startPointGenerated = Instantiate(startPoint);
        startPointGenerated.transform.position = new Vector3(0, 0, 0);
        startPointGenerated.name = "startPointGenerated";
    }
    public void GenerateEndPoint()
    {
        GameObject endPointGenerated = Instantiate(endPoint);
        endPointGenerated.transform.position = new Vector3(2, 2, 2);
        endPointGenerated.name = "endPointGenerated";
    }

    public void GenerateWire()
    {
        if (wirePrefab != null)
        {
            wire = Instantiate(wirePrefab);
            wire.transform.position = Vector3.zero;
            wire.name = "WireGenerated";
            wire.gameObject.GetComponent<WireGeneratorPathfinding.WirePathfinding>().steps = 2;
        }
    }

    public void GenerateEverything()
    {
        GenerateStartPoint();
        GenerateEndPoint();
        GenerateWire();
    }

    public void HideWire()
    {
        wire.gameObject.GetComponent<WireGeneratorPathfinding.WirePathfinding>().ShowWire(showWire);
        showWire = !showWire;
    }

    public void SetCorners()
    {
        wire.gameObject.GetComponent<WireGeneratorPathfinding.WirePathfinding>().corners = (int)Corners.value;
        wire.gameObject.GetComponent<WireGeneratorPathfinding.WirePathfinding>().Reset();
        wire.gameObject.GetComponent<WireGeneratorPathfinding.WirePathfinding>().FindShortestPath();
        //wire.gameObject.GetComponent<WireGeneratorPathfinding.WirePathfinding>().GenerateMesh();
    }
}
