using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Linq;
using System;
using UnityEditor.SceneManagement;

namespace WireGeneratorPathfinding
{
    [CustomEditor(typeof(WirePathfinding))]
    public class WireEditorPathfinding : Editor
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

        SerializedProperty cornerPart;
        SerializedProperty pipePart;
        SerializedProperty startPointGO;
        SerializedProperty endPointGO;
        GameObject startPoint;
        GameObject endPoint;


        bool showWire;

        private void UndoCallbackGenerateMesh()
        {
            //Refreshes MeshRenderer visual state, not sure if there is an easier way to do it
            WirePathfinding wire = target as WirePathfinding;

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
            startPoint = startPointGO.serializedObject.targetObject as GameObject;
            endPoint = endPointGO.serializedObject.targetObject as GameObject;



            showWire = true;

            Undo.undoRedoPerformed += UndoCallbackGenerateMesh;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoCallbackGenerateMesh;
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

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(startPointGO);
            EditorGUILayout.PropertyField(endPointGO);
            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("something changed");
            }

        }
        public override void OnInspectorGUI()
        {
            WirePathfinding wire = target as WirePathfinding;

            EditorGUILayout.LabelField("Select the Wire Tool in the toolbar to edit control points in Scene View");
            showWire = EditorGUILayout.Toggle("Show wire", showWire);
            EditorGUI.BeginChangeCheck();

            if(GUILayout.Button("Find Start and End Points"))
            {
                Undo.RecordObject(wire, "Find Start and End Points");
                wire.FindStartEnd();
            }

            if(GUILayout.Button("Find Path Along Wall"))
            {
                Undo.RecordObject(wire, "Find Path Along Wall");
                wire.FindPath();
            }

            if (GUILayout.Button("Generate Mesh (Trucy)"))
            {
                Undo.IncrementCurrentGroup();
                Mesh sharedMesh = wire.GetComponent<MeshFilter>().sharedMesh;
                try
                {
                    Undo.DestroyObjectImmediate(sharedMesh);
                }
                catch (ArgumentNullException)
                {

                }
                Undo.RecordObject(wire.GetComponent<MeshFilter>(), "Remove mesh from mesh filter");
                wire.GetComponent<MeshFilter>().sharedMesh = null;
                Mesh newMesh = wire.GenerateMeshUsingPrefab();
                Undo.RegisterCreatedObjectUndo(newMesh, "Create Mesh");
                Undo.RecordObject(wire, "Change Mesh");
                wire.SetMesh(newMesh);  
                Undo.SetCurrentGroupName("Generate Mesh");
            }

            if (GUILayout.Button("Create Pipe(Nashi)"))
            {
                Undo.RecordObject(wire, "Create Pipe");
                wire.CreatePipe();
            }
            if (GUILayout.Button("Delete Pipe(Nashi)"))
            {
                Undo.RecordObject(wire, "Delete Pipe");
                wire.DeletePipe();
            }


            if (GUILayout.Button("Reset"))
            {
                Undo.RecordObject(wire, "Reset");
                wire.Reset();
            }


            wire.ShowWire(showWire);

            EditorGUILayout.PropertyField(points);
            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(corners);
            EditorGUILayout.PropertyField(straightMesh);
            EditorGUILayout.PropertyField(curveMesh);
            EditorGUILayout.PropertyField(sizePerStraightMesh);
            EditorGUILayout.PropertyField(numberPerStraightSegment);
            EditorGUILayout.PropertyField(straightPartMeshGenerationMode);
            EditorGUILayout.PropertyField(curveSize);


            if (EditorGUI.EndChangeCheck()) {
                wire.ShowWire(showWire);
                serializedObject.ApplyModifiedProperties();
                //wire.GenerateMesh();

                //GameObject startPoint = startPointGO.serializedObject.targetObject as GameObject;
                var go = startPointGO.serializedObject.targetObject as GameObject;
                //Debug.Log(go);
                //Debug.Log(startPoint);
                //Debug.Log(startPointGO.serializedObject.GetType());
                //wire.startPointGO.transform.position;
                if (wire.wireGenerated)
                {
                    if (wire.startPointGO.transform.position != wire.startPos)
                    {
                        Debug.Log("start point moved (I'm OnInspectorGUI");
                    }
                }
                if (startPointGO.serializedObject.targetObject as GameObject)
                {
                    Debug.Log("hallo");
                    //wire.FindPath();
                }
                //if (endPointGO.transform.hasChanged)
                //{
                //    Debug.Log("hallllo");
                //}
                //wire.UpdatePoints();
                
                
                //wire.GenerateMesh();

            }
        }
    }

    //adds a position handle to each control point
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
                Undo.RecordObject(wire, "Change Control Point Position");
            }
            if (EditorGUI.EndChangeCheck())
            {
                //wire.GenerateMesh();
            }
        }

        public void OnDrawHandles()
        {
        }
    }
}
