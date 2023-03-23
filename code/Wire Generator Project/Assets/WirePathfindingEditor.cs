using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Linq;

namespace WireGeneratorPathfinding
{
    [CustomEditor(typeof(WirePathfinding))]
    public class WireEditorPathfinding : Editor
    {
        SerializedProperty radius;
        SerializedProperty corners;
        SerializedProperty points;

        bool pointsDetails;

        void OnEnable()
        {
            points = serializedObject.FindProperty("points");
            radius = serializedObject.FindProperty("radius");
            corners = serializedObject.FindProperty("corners");
        }

        public void OnSceneGUI()
        {
            WirePathfinding wire = target as WirePathfinding;
            Handles.color = new Color(1.00f, 0.498f, 0.314f);
            for (int i = 0; i < wire.points.Count;i++)
            {
                if (i != 0)
                {
                    Handles.DrawLine(wire.GetPosition(i), wire.GetPosition(i-1));
                }
                Handles.SphereHandleCap(0, wire.GetPosition(i), Quaternion.identity, 0.1f, EventType.Repaint);
            }
        }
        public override void OnInspectorGUI()
        {
            WirePathfinding wire = target as WirePathfinding;

            EditorGUILayout.LabelField("Select the Wire Tool in the toolbar to edit control points in Scene View");

            EditorGUI.BeginChangeCheck();

            if(GUILayout.Button("Find Start and End Points"))
            {
                wire.points[0].anchorTransform = GameObject.FindGameObjectWithTag("startPoint").transform;
                wire.points[wire.points.Count() - 1].anchorTransform = GameObject.FindGameObjectWithTag("endPoint").transform;
                //Debug.Log("Found Start point at " + wire.startPoint.transform.position);
            }

            if (GUILayout.Button("Find Path"))
            {
                Undo.RecordObject(wire, "Find Path");
                Debug.Log("Start Point is: " + wire.points[0].anchorTransform.position);
                Debug.Log("End Point is: " + wire.points[wire.points.Count() - 1].anchorTransform.position);
                wire.FindPath();
            }
            if(GUILayout.Button("Find Path Along Wall"))
            {
                Undo.RecordObject(wire, "Find Path Along Wall");
                wire.castRay();
            }

            EditorGUILayout.PropertyField(points);

            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(corners);

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                wire.GenerateMesh();
            }
        }
    }

    [EditorTool("Wire Tool", typeof(WirePathfinding))]
    class WireTool: EditorTool, IDrawSelectedHandles
    {
        public override void OnToolGUI(EditorWindow window)
        {
            WirePathfinding wire = target as WirePathfinding;
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < wire.points.Count;i++)
            {
                wire.SetPosition(i,Handles.PositionHandle(wire.GetPosition(i), Quaternion.identity));
            }
            if (EditorGUI.EndChangeCheck())
            {
                wire.GenerateMesh();
            }
        }

        public void OnDrawHandles()
        {
        }
    }
}
