using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WireGeneratorPathfinding
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class WirePathfinding : MonoBehaviour
    {
        [Serializable]
        public class ControlPoint
        {
            public Vector3 offset;
            public Vector3 position;
            public bool useAnchor;
            public Transform anchorTransform;

            public ControlPoint(Vector3 offset) {
                this.offset = offset;
                useAnchor = false;
                anchorTransform = null;
            }
        }

        Transform startPoint;
        Transform endPoint;

        float distanceX;
        float distanceY;
        float distanceZ;

        [SerializeField]
        float weight = 1f;
        [SerializeField]
        float sagOffset = 0f;

        Mesh mesh;
        MeshFilter meshFilter;
        public List<ControlPoint> points = new List<ControlPoint>() { new ControlPoint(new Vector3(0,0,0)), new ControlPoint(new Vector3(0, 0, 1))};
        float lengthFactor = 1f;
        public float radius=0.02f;
        public int corners=6;
        float CalculateWireSag(float gravity, float t)
        {
            return gravity * -Mathf.Sin(t * Mathf.PI);
        }

        private void Reset()
        {
            mesh = new Mesh{name = "Wire"};
            meshFilter=GetComponent<MeshFilter>();

            float tension = weight * lengthFactor;
            float sag = tension + weight + sagOffset;
            float minimum = CalculateWireSag(sag, 0.5f);
            GenerateMesh();
        }

        public void FindPath()
        {
            startPoint = points[0].anchorTransform;
            endPoint = points[points.Count - 1].anchorTransform;
            points[0].position = startPoint.position;
            points[points.Count - 1].position = endPoint.position;
            if (startPoint != endPoint)
            {
                distanceX = endPoint.position.x - startPoint.position.x;
                distanceY = endPoint.position.y - startPoint.position.y;
                distanceZ = endPoint.position.z - startPoint.position.z;
                Debug.Log("Distanz auf X Achse ist: " + distanceX);
                Debug.Log("Distanz auf Y Achse ist: " + distanceY);
                Debug.Log("Distanz auf Z Achse ist: " + distanceZ);
                points[0].offset.x = this.transform.position.x - distanceX;


                if (distanceX > 0)
                {
                    //points.Add(new ControlPoint(points[0].offset + new Vector3(distanceX,0,0)));
                    points[points.Count - 1].offset = new Vector3 (points[0].offset.x + distanceX, 0, 0);
                }
                else if(distanceX < 0)
                {

                }
                else
                {
                    foreach (var point in points)
                    {
                        point.offset.x = 0;
                    }
                }

                if(distanceY > 0)
                {
                    points.Add(new ControlPoint(points[points.Count - 1].offset + new Vector3(0, distanceY, 0)));
                }
                else if(distanceY < 0)
                {

                }
                else
                {
                    foreach (var point in points)
                    {
                        point.offset.y = 0;
                    }
                }

                if(distanceZ > 0)
                {
                    points.Add(new ControlPoint(points[points.Count - 1].offset + new Vector3(0, 0, distanceZ)));
                }
                else if(distanceZ < 0)
                {

                }
                else
                {
                    foreach(var point in points)
                    {
                        point.offset.z = 0;
                    }
                }
            }
        }

        public Vector3 GetPosition(int i) {
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

                if (controlPointId == 0) {
                    tangent = -(GetPosition(controlPointId) - GetPosition(1)).normalized;
                }
                else if (controlPointId==points.Count-1)
                {
                    tangent = (GetPosition(controlPointId) - GetPosition(controlPointId - 1)).normalized;
                }
                else
                {
                    tangent = ((GetPosition(controlPointId) - GetPosition(controlPointId - 1)).normalized - (GetPosition(controlPointId) - GetPosition(controlPointId + 1)).normalized).normalized;
                }
                //Debug.Log(tangent);

                Vector3 startpointVertice;
            
                if (tangent.y == 1)
                {
                    startpointVertice = Vector3.right;
                }
                else if (tangent.y == -1)
                {
                    startpointVertice = Vector3.right;
                }
                else {
                    //calculate vector perpendicular tangent
                    var helpVector = Quaternion.Euler(0, -90, 0) * tangent;
                    //cross returns vector perpendicular to two vectors
                    startpointVertice = Vector3.Cross(tangent, helpVector).normalized;
                }

                startpointVertice *= radius;


                var offsetCircle=Quaternion.AngleAxis(360f / corners, tangent);

                for (int i = 0; i < corners; i++)
                {
                    offsetCircle = Quaternion.AngleAxis((360f / corners) * i, tangent);
                    tempVertices[i + corners * controlPointId] = (offsetCircle * startpointVertice) + (points[controlPointId].useAnchor ? GetPosition(controlPointId) - transform.position : points[controlPointId].offset);
                    tempNormals[i + corners * controlPointId] = offsetCircle * startpointVertice;
                }
            }


            mesh.vertices = tempVertices;
            mesh.normals = tempNormals;

            var tempTriangles = new int[corners*6*(points.Count-1)];
            for (int row = 0; row < points.Count-1; row++)
            {
                    for (int i = 0; i < corners; i++)
                {
                    int baseLine = row * corners * 6 + i * 6;
                    tempTriangles[baseLine + 0] = i                             + corners * row;
                    tempTriangles[baseLine + 1] = (i + 1) % corners             + corners * row;
                    tempTriangles[baseLine + 2] = corners + i                   + corners * row;
                    tempTriangles[baseLine + 3] = (i + 1) % corners             + corners * row;
                    tempTriangles[baseLine + 4] = corners + (i + 1) % corners   + corners * row;
                    tempTriangles[baseLine + 5] = corners + i                   + corners * row;
                }
            }

            mesh.triangles=tempTriangles;
            meshFilter.sharedMesh = mesh;
        }
    }
}
