using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LineInspectorTest))]
public class LineInspectorTest : Editor
{
    private void OnSceneGUI()
    {
        LineTest line = target as LineTest;

        Handles.color = Color.white;
        Handles.DrawLine(line.p0, line.p1);

        Debug.Log("Hallo");
    }
}
