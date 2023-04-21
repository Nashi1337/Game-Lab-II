using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
            public bool useAnchor;
            public Transform anchorTransform;

            public ControlPoint(Vector3 offset) {
                this.offset = offset;
                useAnchor = false;
                anchorTransform = null;
            }
        }

        public Vector3 startPoint;
        public Vector3 endPoint;

        float distanceX;
        float distanceY;
        float distanceZ;
        Vector3 spacingZ = new Vector3(0, 0, 0.2f);

        [SerializeField]
        float weight = 1f;
        [SerializeField]
        float sagOffset = 0f;

        Mesh mesh;
        MeshFilter meshFilter;
        public List<ControlPoint> points = new List<ControlPoint>() { new ControlPoint(new Vector3(0,0,0)), new ControlPoint(new Vector3(1, 0, 0))};
        float lengthFactor = 1f;
        public float radius=2f;
        public int corners=6;
        float CalculateWireSag(float gravity, float t)
        {
            return gravity * -Mathf.Sin(t * Mathf.PI);
        }

        public void Reset()
        {
            points = new List<ControlPoint>() { new ControlPoint(new Vector3(0, 0, 0)), new ControlPoint(new Vector3(1, 0, 0)) };

            mesh = new Mesh{name = "Wire"};
            meshFilter=GetComponent<MeshFilter>();

            float tension = weight * lengthFactor;
            float sag = tension + weight + sagOffset;
            float minimum = CalculateWireSag(sag, 0.5f);
            GenerateMesh();
        }
        public void FindStartEnd()
        {
            points[0].anchorTransform = GameObject.FindGameObjectWithTag("startPoint").transform;
            points[1].anchorTransform = GameObject.FindGameObjectWithTag("endPoint").transform;
        }

        //public Vector3 CalculateStartToEnd(Vector3 start, Vector3 end)
        //{
        //    return end - start;
        //}

        public Vector3 NextPoint(float endPoint, Vector3 currentPosition, Vector3 direction, Vector3 rayDirection, int point)
        {
            for(float tick = 0.2f; tick <= endPoint;tick += 0.2f)
            {
                Vector3 offset = tick * direction;
                currentPosition = points[point].offset +  offset;
                if (!Physics.Raycast(transform.TransformPoint(currentPosition), rayDirection, 0.5f))
                {
                    break;
                }
                Debug.DrawRay(transform.TransformPoint(currentPosition), rayDirection * 0.5f, Color.red, 10, true);
            }
            return currentPosition;
        }

        public RaycastHit CastRay(Vector3 start, Vector3 direction)
        {
            Ray ray = new Ray(transform.TransformPoint(start), direction);
            Physics.Raycast(ray, out RaycastHit hitData, Mathf.Infinity);
            return hitData;
        }

        public float CalculateDifference(float endPoint, float startPoint)
        {
            return endPoint - startPoint;
        }
        public void FindPath()
        {
            Collider[] wall;

            if (points[0].anchorTransform != null && points[1].anchorTransform != null)
            {
                startPoint = points[0].anchorTransform.position;
                endPoint = points[1].anchorTransform.position;
                this.transform.position = startPoint;

                wall = Physics.OverlapSphere(points[0].offset, 1f);
                points[1].offset = wall[0].ClosestPoint(points[0].offset) - spacingZ;

                Debug.DrawRay(transform.TransformPoint(points[1].offset), transform.right*100f, Color.blue, 10, true);
                var obstacle = CastRay(points[1].offset, Vector3.right);
                //Debug.Log(obstacle);

                if(obstacle.point != null ) 
                {
                    Debug.Log(CalculateDifference(endPoint.y, points[1].offset.y));
                    Debug.Log(obstacle.transform.gameObject.GetComponent<Renderer>().bounds.size);
                }

                points.Add(new ControlPoint(NextPoint(endPoint.x,points[1].offset,Vector3.right,Vector3.forward,1)));


                Ray ray = new Ray(points[2].offset, transform.forward);
                Physics.Raycast(ray, out RaycastHit hitData);
                Vector3 hitPosition = hitData.point;
                //Debug.DrawRay(transform.TransformPoint(points[2].offset), Vector3.forward*10f, Color.red, 10, true);
                points.Add(
                    new ControlPoint(
                        NextPoint(
                            CastRay(points[2].offset,Vector3.forward).point.z-2*spacingZ.z,
                            points[2].offset, 
                            Vector3.forward,Vector3.left,2)));

                if (CastRay(points[3].offset, Vector3.right).point!=Vector3.zero)
                {
                    Debug.Log("Found obstacle");
                    for(float tick = 0.2f; tick <= CastRay(points[3].offset, Vector3.right).point.x; tick+=0.2f)
                    {
                        //TODO: Was passiert wenn ein Hindernis auf dem Weg liegt?
                    }
                }
                else
                {
                    points.Add(new ControlPoint(points[3].offset + new Vector3(endPoint.x - transform.TransformPoint(points[3].offset).x, 0, 0)));
                }

                if(CastRay(points[4].offset, Vector3.up).point != Vector3.zero)
                {
                    Debug.Log("Found obstacle");
                    for(float tick = 0.2f; tick <= CastRay(points[4].offset, Vector3.up).point.x; tick+= 0.2f)
                    {
                        //TODO: Was passiert wenn ein hindernis auf dem Weg liegt?
                    }
                }
                else
                {
                    points.Add(new ControlPoint(points[4].offset + new Vector3(0, endPoint.y - transform.TransformPoint(points[4].offset).y, 0)));
                }

                if(CastRay(points[5].offset, Vector3.back).point!=Vector3.zero)
                {
                    Debug.Log("Found obstacle");
                    for (float tick = 0.2f; tick <= CastRay(points[5].offset, Vector3.back).point.x; tick += 0.2f)
                    {
                        //TODO: Was passiert wenn ein hindernis auf dem Weg liegt?
                    }
                }
                else
                {
                    points.Add(new ControlPoint(points[5].offset + new Vector3(0, 0, endPoint.z - transform.TransformPoint(points[5].offset).z)));
                }

            }
            else
            {
                FindStartEnd();
                FindPath();
            }
            if (points[points.Count - 1].offset == endPoint)
            {
                Debug.Log("Goal reached");
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

                startpointVertice *= radius/100;


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
