using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace WireGenerator
{
    [CustomEditor(typeof(Wire))]
    public class WireEditor : Editor
    {
        SerializedProperty offsetPoint1;
        SerializedProperty offsetPoint2;

        SerializedProperty anchorTransform1;
        SerializedProperty anchorTransform2;

        SerializedProperty radius;
        SerializedProperty corners;

        bool pointsDetails;

        void OnEnable()
        {
            offsetPoint1 = serializedObject.FindProperty("offsetPoint1");
            anchorTransform1 = serializedObject.FindProperty("anchorTransform1");
            offsetPoint2 = serializedObject.FindProperty("offsetPoint2");
            anchorTransform2 = serializedObject.FindProperty("anchorTransform2");
            radius = serializedObject.FindProperty("radius");
            corners = serializedObject.FindProperty("corners");
        }

        public void OnSceneGUI()
        {
            Wire wire = target as Wire;
            Handles.color = new Color(1.00f, 0.498f, 0.314f);
            Handles.DrawLine(wire.point1, wire.point2);
            Handles.SphereHandleCap(0, wire.point1, Quaternion.identity, 0.1f, EventType.Repaint);
            Handles.SphereHandleCap(0, wire.point2, Quaternion.identity, 0.1f, EventType.Repaint);

            /*
            var tempVector = (wire.point2 - wire.point1).normalized;
            var cross = Vector3.Cross(Vector3.forward,tempVector);

            var normal = cross.normalized == Vector3.zero ?Vector3.up:cross.normalized;
            
            var rotation = new Quaternion(cross.x, cross.y, cross.z, 1 + Vector3.Dot(Vector3.forward, tempVector));

            

            rotation.Normalize();
            Handles.CircleHandleCap(0,wire.point1, rotation, 0.2f, EventType.Repaint);


            var startpointVerticeQ = new Quaternion(0, 1, 0, 0) * rotation;
            var startpointVertice = new Vector3(startpointVerticeQ.x, startpointVerticeQ.y, startpointVerticeQ.z);
            startpointVertice.Normalize();



            Handles.DrawLine(wire.point1, wire.point1 + startpointVertice);
            Handles.DrawLine(wire.point2, wire.point2 + startpointVertice);
            Handles.DrawLine(wire.point1+startpointVertice, wire.point2 + startpointVertice);
        */
        }
        public override void OnInspectorGUI()
        {
            Wire wire = target as Wire;

            EditorGUILayout.LabelField("Select the Wire Tool in the toolbar to edit control points in Scene View");

            pointsDetails =EditorGUILayout.BeginFoldoutHeaderGroup(pointsDetails, "Control Points Details");
            EditorGUI.BeginChangeCheck();
            if (pointsDetails)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(offsetPoint1);
                //EditorGUILayout.PropertyField(useAnchor1);
                wire.useAnchor1 = EditorGUILayout.BeginToggleGroup("Use Anchor", wire.useAnchor1);
                EditorGUILayout.PropertyField(anchorTransform1);
                EditorGUILayout.EndToggleGroup();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(offsetPoint2);
                wire.useAnchor2 = EditorGUILayout.BeginToggleGroup("Use Anchor", wire.useAnchor2);
                EditorGUILayout.PropertyField(anchorTransform2);
                EditorGUILayout.EndToggleGroup();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(corners);

            if (EditorGUI.EndChangeCheck())
            {
                wire.GenerateMesh();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [EditorTool("Wire Tool", typeof(Wire))]
    class WireTool: EditorTool, IDrawSelectedHandles
    {
        public override void OnToolGUI(EditorWindow window)
        {
            Wire wire = target as Wire;
            EditorGUI.BeginChangeCheck();
            wire.point1 = Handles.PositionHandle(wire.point1, Quaternion.identity);
            wire.point2 = Handles.PositionHandle(wire.point2, Quaternion.identity);
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
