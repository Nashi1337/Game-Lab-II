using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace WireGenerator
{
    [CustomEditor(typeof(Wire))]
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
            Wire wire = target as Wire;
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
            Wire wire = target as Wire;

            EditorGUILayout.LabelField("Select the Wire Tool in the toolbar to edit control points in Scene View");

            //pointsDetails =EditorGUILayout.BeginFoldoutHeaderGroup(pointsDetails, "Control Points Details");
            EditorGUI.BeginChangeCheck();

            /*if (pointsDetails)
            {

                for(int i=0; i < wire.points.Count)
                {
                    if (i != 0)
                    {
                        EditorGUILayout.Space();
                    }
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(offsetPoint1);
                    //EditorGUILayout.PropertyField(useAnchor1);
                    wire.points[i].useAnchor = EditorGUILayout.BeginToggleGroup("Use Anchor", wire..points[i].useAnchor);
                    EditorGUILayout.PropertyField(anchorTransform1);
                    EditorGUILayout.EndToggleGroup();
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            */

            EditorGUILayout.PropertyField(points);

            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(corners);

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                wire.GenerateMesh();
            }
        }
    }

    [EditorTool("Wire Tool", typeof(Wire))]
    class WireTool: EditorTool, IDrawSelectedHandles
    {
        public override void OnToolGUI(EditorWindow window)
        {
            Wire wire = target as Wire;
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
