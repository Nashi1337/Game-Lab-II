using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WireGenerator
{  
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
public class MaxWire2 : MonoBehaviour
    {
        [Serializable]
        public class ControlPoint
        {
            public Vector3 offset;
            public bool useAnchor;
            public Transform anchorTransform;

            public ControlPoint(Vector3 offset)
            {
                this.offset = offset;
                useAnchor = false;
                anchorTransform = null;
            }
        }

        [SerializeField]
        float weight = 1f;
        [SerializeField]
        float sagOffset = 0f;
        [Tooltip("Controls the distance between edge loops along the length of the wire. Wires always have a minimum of 5 points")]
        public float pointsPerMeter = 0.3f;

        public Vector3[] schmoints;

        Mesh mesh;
        MeshFilter meshFilter;
        public List<ControlPoint> points = new List<ControlPoint>() {
            new ControlPoint(new Vector3(0,0,0)),
            new ControlPoint(new Vector3(0,0,1))};
        float lengthFactor = 1f;
        public float radius = 0.02f;
        public int corners = 6;
        public float sagDepth;

        private void Awake()
        {
            Reset();
        }

        private void Update()
        {
            float tension = weight * lengthFactor;
            float sag = tension + weight + sagOffset;

            int positionCount = Mathf.RoundToInt(pointsPerMeter * lengthFactor + sagDepth);
            positionCount = Mathf.Clamp(positionCount, 6, 50);
            schmoints = new Vector3[positionCount];

            float sagAmount = tension + weight + sagOffset;
            float lowestPoint = CalculateWireSag(sag, 0.5f);
            sagDepth = lowestPoint;

            //number of points?

            Vector3 pivot = Vector3.Lerp(points[0].offset, points[points.Count - 1].offset, 0.5f);
            pivot.y += lowestPoint;
            gameObject.transform.position = pivot;

            for (int i = 0; i < positionCount; i++)
            {
                //Sample point along wire length as 0-1 value
                float wireSamplePoint = (float)i / (float)(positionCount - 1);

                //Current position along wire
                Vector3 wirePoint = (points[0].offset - points[points.Count - 1].offset) * wireSamplePoint;

                //Offset at Y-axis by sagging amount
                wirePoint.y += CalculateWireSag(sagAmount, wireSamplePoint);

                //Transform position to local-space
                points[i].offset = transform.InverseTransformPoint(points[0].offset + wirePoint);
            }

            points[0].offset = transform.position + schmoints[0];
            points[points.Count - 1].offset = transform.position + schmoints[schmoints.Length - 1];


            GenerateMesh();
        }

        private void Reset()
        {
            mesh = new Mesh { name = "Wire Mesh" };
            meshFilter = GetComponent<MeshFilter>();

            float tension = weight * lengthFactor;
            float sag = tension + weight + sagOffset;

            int positionCount = Mathf.RoundToInt(pointsPerMeter * lengthFactor + sagDepth);
            positionCount = Mathf.Clamp(positionCount, 6, 50);
            schmoints = new Vector3[positionCount];

            float lowestPoint= CalculateWireSag(sag, 0.5f);
            sagDepth = lowestPoint;

            //number of points?

            Vector3 pivot = Vector3.Lerp(points[0].offset, points[points.Count - 1].offset, 0.5f);
            pivot.y += lowestPoint;
            gameObject.transform.position = pivot;

            for (int i = 0; i < positionCount; i++)
            {
                //Sample point along wire length as 0-1 value
                float wireSamplePoint = (float)i / (float)(positionCount - 1);

                //Current position along wire
                Vector3 wirePoint = (points[0].offset - points[points.Count - 1].offset) * wireSamplePoint;

                //Offset at Y-axis by sagging amount
                wirePoint.y += CalculateWireSag(sag, wireSamplePoint);

                //Transform position to local-space
                points[i].offset = transform.InverseTransformPoint(points[0].offset + wirePoint);
            }

            points[0].offset = transform.position + schmoints[0];
            points[points.Count - 1].offset = transform.position + schmoints[schmoints.Length - 1];

            Debug.Log("lowest Point is: " + lowestPoint);
            GenerateMesh();
        }

        public static float CalculateWireSag(float gravity, float t)
        {
            return gravity * -Mathf.Sin(t * Mathf.PI);
        }

        public Vector3 GetPosition(int i)
        {
            return (points[i].useAnchor && points[i].anchorTransform ? points[i].anchorTransform : transform).position + points[i].offset;
        }
        public void SetPosition(int i, Vector3 position)
        {
            points[i].offset = position - (points[i].useAnchor && points[i].anchorTransform ? points[i].anchorTransform : transform).position;
        }

        public void GenerateMesh()
        {
            var tempVertices = new Vector3[corners * points.Count];
            var tempNormals = new Vector3[corners * points.Count];

            for (int controlPointId = 0; controlPointId < points.Count; controlPointId++)
            {

                //calculate vector from one end to the other
                Vector3 tangent;

                if (controlPointId == 0)
                {
                    tangent = -(GetPosition(controlPointId) - GetPosition(1)).normalized;
                }
                else if (controlPointId == points.Count - 1)
                {
                    tangent = (GetPosition(controlPointId) - GetPosition(controlPointId - 1)).normalized;
                }
                else
                {
                    tangent = ((GetPosition(controlPointId) - GetPosition(controlPointId - 1)).normalized - (GetPosition(controlPointId) - GetPosition(controlPointId + 1)).normalized).normalized;
                }
                Debug.Log(tangent);

                Vector3 startpointVertice;

                if (tangent.y == 1)
                {
                    startpointVertice = Vector3.right;
                }
                else if (tangent.y == -1)
                {
                    startpointVertice = Vector3.right;
                }
                else
                {
                    //calculate vector perpendicular tangent
                    var helpVector = Quaternion.Euler(0, -90, 0) * tangent;
                    //cross returns vector perpendicular to two vectors
                    startpointVertice = Vector3.Cross(tangent, helpVector).normalized;
                }

                startpointVertice *= radius;


                var offsetCircle = Quaternion.AngleAxis(360f / corners, tangent);

                for (int i = 0; i < corners; i++)
                {
                    offsetCircle = Quaternion.AngleAxis((360f / corners) * i, tangent);
                    tempVertices[i + corners * controlPointId] = (offsetCircle * startpointVertice) + (points[controlPointId].useAnchor ? GetPosition(controlPointId) - transform.position : points[controlPointId].offset);
                    tempNormals[i + corners * controlPointId] = offsetCircle * startpointVertice;
                }
            }


            mesh.vertices = tempVertices;
            mesh.normals = tempNormals;

            var tempTriangles = new int[corners * 6 * (points.Count - 1)];
            for (int row = 0; row < points.Count - 1; row++)
            {
                for (int i = 0; i < corners; i++)
                {
                    int baseLine = row * corners * 6 + i * 6;
                    tempTriangles[baseLine + 0] = i + corners * row;
                    tempTriangles[baseLine + 1] = (i + 1) % corners + corners * row;
                    tempTriangles[baseLine + 2] = corners + i + corners * row;
                    tempTriangles[baseLine + 3] = (i + 1) % corners + corners * row;
                    tempTriangles[baseLine + 4] = corners + (i + 1) % corners + corners * row;
                    tempTriangles[baseLine + 5] = corners + i + corners * row;
                }
            }

            mesh.triangles = tempTriangles;
            meshFilter.sharedMesh = mesh;
        }
    }
}
