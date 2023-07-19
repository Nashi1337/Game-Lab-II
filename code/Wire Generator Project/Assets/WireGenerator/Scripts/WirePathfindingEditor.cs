using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Linq;
using System;
using UnityEditor.SceneManagement;

namespace WireGenerator
{
    [CustomEditor(typeof(Wire))]
    public class WireEditor : Editor
    {
        SerializedProperty radius;
        SerializedProperty corners;
        SerializedProperty points;


        SerializedProperty straightMesh;
        SerializedProperty curveMesh;
        SerializedProperty sizePerStraightMesh;
        SerializedProperty numberPerStraightSegment;
        SerializedProperty straightPartMeshGenerationMode;
        SerializedProperty curveSize;

        SerializedProperty startPointGO;
        SerializedProperty endPointGO;
        SerializedProperty wireMat;
        SerializedProperty cornerMat;

        SerializedProperty wireIndex;

        private void UndoCallbackGenerateMesh()
        {
            //Refreshes MeshRenderer visual state, not sure if there is an easier way to do it
            Wire wire = target as Wire;

            MeshFilter meshFilter = wire.GetComponent<MeshFilter>();
            Mesh sharedMesh = meshFilter.sharedMesh;
            meshFilter.sharedMesh = null;
            meshFilter.sharedMesh = sharedMesh;
        }

        void OnEnable()
        {
            points = serializedObject.FindProperty("points");
            radius = serializedObject.FindProperty("radius");
            corners = serializedObject.FindProperty("corners");

            straightMesh = serializedObject.FindProperty("straightMesh");
            curveMesh = serializedObject.FindProperty("curveMesh");
            sizePerStraightMesh = serializedObject.FindProperty("sizePerStraightMesh");
            numberPerStraightSegment = serializedObject.FindProperty("numberPerStraightSegment");
            straightPartMeshGenerationMode = serializedObject.FindProperty("straightPartMeshGenerationMode");
            curveSize = serializedObject.FindProperty("curveSize");

            startPointGO = serializedObject.FindProperty("startPointGO");
            endPointGO = serializedObject.FindProperty("endPointGO");

            wireMat = serializedObject.FindProperty("wireMaterial");
            cornerMat = serializedObject.FindProperty("cornerMaterial");

            wireIndex = serializedObject.FindProperty("wireIndex");

            Undo.undoRedoPerformed += UndoCallbackGenerateMesh;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoCallbackGenerateMesh;
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
            EditorGUI.BeginChangeCheck();

            if(GUILayout.Button("Create Wire"))
            {
                Undo.RecordObject(wire, "Find Path");
                wire.FindPath();
            }

            if (GUILayout.Button("Reset"))
            {
                Undo.RecordObject(wire, "Reset");
                wire.Reset();
            }


            EditorGUILayout.PropertyField(points);
            EditorGUILayout.PropertyField(startPointGO);
            EditorGUILayout.PropertyField(endPointGO);
            EditorGUILayout.PropertyField(wireMat);
            EditorGUILayout.PropertyField(cornerMat);
            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(corners);
            EditorGUILayout.PropertyField(straightMesh);
            EditorGUILayout.PropertyField(curveMesh);
            EditorGUILayout.PropertyField(sizePerStraightMesh);
            EditorGUILayout.PropertyField(numberPerStraightSegment);
            EditorGUILayout.PropertyField(straightPartMeshGenerationMode);
            EditorGUILayout.PropertyField(curveSize);
            EditorGUILayout.PropertyField(wireIndex);

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();

                Undo.RecordObject(wire.GetComponent<MeshFilter>(), "Remove mesh from mesh filter");
                wire.GetComponent<MeshFilter>().sharedMesh = null;
                Mesh newMesh = wire.GenerateMeshUsingPrefab();
                Undo.RegisterCreatedObjectUndo(newMesh, "Create Mesh");
                Undo.RecordObject(wire, "Change Mesh");
                wire.SetMesh(newMesh);
                Undo.SetCurrentGroupName("Generate Mesh");
            }
        }
    }

    [EditorTool("Wire Tool", typeof(Wire))]
    class WireTool: EditorTool, IDrawSelectedHandles
    {

        public override void OnToolGUI(EditorWindow window)
        {
            Wire wire = target as Wire;
            Handles.BeginGUI();
            Rect buttonRect = new Rect(SceneView.lastActiveSceneView.position.width-150f, SceneView.lastActiveSceneView.position.height-150f, 130f, 30f);
            if (GUI.Button(buttonRect, "Create Wire"))
            {
                wire.FindPath();
            }
            Handles.EndGUI();
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < wire.points.Count;i++)
            {
                wire.SetPosition(i,Handles.PositionHandle(wire.GetPosition(i), Quaternion.identity));
                Undo.RecordObject(wire, "Change Control Point Position");
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(wire.GetComponent<MeshFilter>(), "Remove mesh from mesh filter");
                wire.GetComponent<MeshFilter>().sharedMesh = null;
                Mesh newMesh = wire.GenerateMeshUsingPrefab();
                Undo.RegisterCreatedObjectUndo(newMesh, "Create Mesh");
                Undo.RecordObject(wire, "Change Mesh");
                wire.SetMesh(newMesh);
                Undo.SetCurrentGroupName("Generate Mesh");
            }
        }

        public void OnDrawHandles()
        {
        }
    }
}
