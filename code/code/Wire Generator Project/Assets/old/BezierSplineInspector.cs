using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    private BezierSpline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const float directionScale = 0.5f;

    private const int stepsPerCurve = 10;

    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private float SagWeight = 2f;

    private int selectedIndex = -1;

    SerializedProperty radialSegments;
    SerializedProperty diameter;
    SerializedProperty weight;

    void OnEnable()
    {
        radialSegments = serializedObject.FindProperty("radialSegments");
        diameter = serializedObject.FindProperty("diameter");
        weight = serializedObject.FindProperty("weight");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        spline = target as BezierSpline;
        //serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            //spline.UpdateSpline();
            //serializedObject.Update();
            SagWeight = serializedObject.FindProperty("weight").floatValue;
            Debug.Log("sagWeight is " + SagWeight);
            spline.SagMode(SagWeight);
            spline.GenerateMesh();
            EditorUtility.SetDirty(spline);
        }
        if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
        {
            DrawSelectedPointInspector();
        }
        if (GUILayout.Button("Add Curve"))
        {
            Undo.RecordObject(spline, "Add Curve");
            spline.AddCurve();
            EditorUtility.SetDirty(spline);
            spline.GenerateMesh();
        }
        if(GUILayout.Button("Activate sag mode"))
        {
            Undo.RecordObject(spline, "Active sag mode");
            spline.SagMode(0.2f);
            EditorUtility.SetDirty(spline);
            spline.GenerateMesh();
        }
        if (GUILayout.Button("Activate straight mode"))
        {
            Undo.RecordObject(spline, "Activate straight mode");
            spline.StraightMode();
            EditorUtility.SetDirty(spline);
            spline.GenerateMesh();
        }
        if (GUILayout.Button("Apply"))
        {
            Undo.RecordObject(spline, "Apply");
            EditorUtility.SetDirty(spline);
            spline.UpdateSpline();
            spline.GenerateMesh();
        }
        spline.GenerateMesh();
    }

    private void OnSceneGUI()
    {
        spline = target as BezierSpline;
        EditorGUI.BeginChangeCheck();
        handleTransform = spline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        //Vector3 p0 = ShowPoint(0);
        /*for(int i = 1; i < spline.ControlPointCount; i += 3)
        {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i+1);
            Vector3 p3 = ShowPoint(i+2);
            
            Handles.color = Color.gray;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            Handles.DrawBezier(p0, p3, p1, p2, Color.black, null, 2f);
            p0 = p3;
        }
        */

        for(int i = 0; i < spline.ControlPointCount; i++)
        {
            ShowPoint(i);
        }

        //ShowDirections();
        if (EditorGUI.EndChangeCheck())
        {
            //spline.UpdateSpline();
            //spline.UpdateMesh();
            spline.GenerateMesh();
        }
    }

    private void DrawSelectedPointInspector()
    {
        GUILayout.Label("Selected Point");
        EditorGUI.BeginChangeCheck();
        Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Move Point");
            EditorUtility.SetDirty(spline);
            spline.SetControlPoint(selectedIndex, point);
            spline.GenerateMesh();
        }
    }
    private void ShowDirections()
    {
        Handles.color = Color.green;
        Vector3 point = spline.GetPoint(0f);
        Handles.DrawLine(point, point + spline.GetDirection(0f) * directionScale);
        int steps = stepsPerCurve * spline.CurveCount;
        for (int i = 1; i <= steps; i++)
        {
            point = spline.GetPoint(i / (float)steps);
            Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
        }
    }

    private Vector3 ShowPoint(int index)
    {
        Vector3 point = handleTransform.TransformPoint(spline.GetControlPoint(index));
        float size = HandleUtility.GetHandleSize(point);
        if (index == 0)
        {
            size *= 2f;
        }
        Handles.color = Color.white;
        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = index;
            Repaint();
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "MovePoint");
                EditorUtility.SetDirty(spline);
                spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
                spline.GenerateMesh();
            }
        }
        return point;
    }
}
