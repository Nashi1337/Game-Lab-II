using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WireConnector))]
public class WireConnectorInspector : Editor
{

    private WireConnector connector;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        connector = target as WireConnector;
        if (GUILayout.Button("Generate Wire"))
        {
            New();            
        }
    }

    public void New()
    {
        Material blackMat = (Material)Resources.Load("black");
        GameObject newWire = new GameObject();
        newWire.transform.position = connector.transform.position;
        newWire.name = "new Wire";
        BezierSpline spline = newWire.AddComponent<BezierSpline>();
        spline.UpdateMesh(connector.material);
        spline.CreateFromObject(connector.transform);
        //spline.UpdateMesh(blackMat);
        //spline.Reset();
    }
}
