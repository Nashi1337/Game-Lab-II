using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace WireGenerator
{
    [CustomEditor(typeof(MaxWire2))]
    public class MaxWireEditor: Editor
    {
        SerializedProperty radius;
        SerializedProperty corners;
        SerializedProperty points;

        bool pointsDetails;

        private void OnEnable()
        {
            points = serializedObject.FindProperty("points");
            radius = serializedObject.FindProperty("radius");
            corners = serializedObject.FindProperty("corners");
        }

        public void OnSceneGUI()
        {
            MaxWire2 wire = target as MaxWire2;
            Handles.color = new Color(1.00f, 0.498f, 0.314f);
            for (int i = 0; i < wire.points.Count; i++)
            {
                if(i != 0)
                {
                    Handles.DrawLine(wire.GetPosition(i), wire.GetPosition(i - 1));
                }
                Handles.SphereHandleCap(0, wire.GetPosition(i), Quaternion.identity, 0.1f, EventType.Repaint);
            }
        }

        public override void OnInspectorGUI()
        {
            MaxWire2 wire = target as MaxWire2;

            EditorGUILayout.LabelField("Select the Wire Tool in the toolbar to edit control points in Scene View");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(points);
            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(corners);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                wire.GenerateMesh();
            }
        }
    }

    [EditorTool("Max Wire Tool", typeof(MaxWire2))]
    class WireTool: EditorTool, IDrawSelectedHandles
    {
        public override void OnToolGUI(EditorWindow window)
        {
            MaxWire2 wire = target as MaxWire2;
            EditorGUI.BeginChangeCheck();
            for(int i = 0; i < wire.points.Count; i++)
            {
                wire.SetPosition(i, Handles.PositionHandle(wire.GetPosition(i), Quaternion.identity));
            }
            if (EditorGUI.EndChangeCheck())
            {
                wire.GenerateMesh();
                Handles.SphereHandleCap(0, Vector3.Lerp(wire.points[0].offset, wire.points[wire.points.Count - 1].offset, 0.5f), Quaternion.identity, 0.1f, EventType.Repaint);
                
            }
        }

        public void OnDrawHandles()
        {

        }
    }
}