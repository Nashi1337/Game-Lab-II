using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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


        Vector3 spacingZ = new Vector3(0, 0, 0.19f);
        Vector3 spacingY = new Vector3(0, 0.19f, 0);
        Vector3 spacingX = new Vector3(0.19f, 0, 0);

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

        public Vector3 NextPoint(float endPoint, Vector3 currentPosition, Vector3 direction, Vector3 rayDirection, int point)
        {
            for(float tick = 0.4f; tick <= endPoint;tick += 0.2f)
            {
                Vector3 offset = tick * direction;
                currentPosition = points[point].offset +  offset;
                if(Physics.Raycast(transform.TransformPoint(currentPosition), direction, 0.2f))
                {
                    break;
                }
                else if (!Physics.Raycast(transform.TransformPoint(currentPosition), rayDirection, 0.5f))
                {
                    break;
                }
                Debug.DrawRay(transform.TransformPoint(currentPosition), rayDirection * 0.5f, UnityEngine.Color.red, 10, true);
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

        Vector3 GetLastPoint()
        {
            return points[points.Count-1].offset;
        }

        int GetLastPointIndex()
        {
            return points.Count-1;
        }

        void FoundObstacle(RaycastHit obstacle, Vector3 direction)
        {
            Collider collider = obstacle.collider;
            Vector3 center = collider.bounds.center;
            float distanceToTop = Mathf.Abs(center.y + collider.bounds.extents.y);
            float distanceToBottom = Mathf.Abs(center.y - collider.bounds.extents.y);
            Vector3 closestPoint;

            if (direction == Vector3.right)
            {
                points.Add(
                    new ControlPoint(
                        new Vector3(center.x - collider.bounds.extents.x - spacingX.x, 
                        GetLastPoint().y, 
                        GetLastPoint().z)));
                if (distanceToTop < distanceToBottom)
                {
                    closestPoint = new Vector3(GetLastPoint().x, center.y + collider.bounds.extents.y, GetLastPoint().z);
                    points.Add(new ControlPoint(closestPoint + spacingY));
                }
                else
                {
                    closestPoint = new Vector3(GetLastPoint().x, center.y - collider.bounds.extents.y, GetLastPoint().z);
                    points.Add(new ControlPoint(closestPoint - spacingY));
                }
                points.Add(new ControlPoint(GetLastPoint() + new Vector3(2*collider.bounds.extents.x, 0, 0) + 2 * spacingX));
                points.Add(new ControlPoint(GetLastPoint()));
                points[points.Count - 1].offset.y = points[points.Count - 4].offset.y;
            }
            else if (direction == Vector3.forward)
            {
                points.Add(
                    new ControlPoint(
                        new Vector3(GetLastPoint().x, 
                        GetLastPoint().y, 
                        center.z - spacingZ.z)));
                if (distanceToTop < distanceToBottom)
                {
                    closestPoint = new Vector3(GetLastPoint().x, center.y + collider.bounds.extents.y, GetLastPoint().z);
                    points.Add(new ControlPoint(closestPoint + spacingY));
                }
                else
                {
                    closestPoint = new Vector3(GetLastPoint().x, center.y - collider.bounds.extents.y, GetLastPoint().z);
                    points.Add(new ControlPoint(closestPoint - spacingY));
                }
                points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, 2*collider.bounds.extents.z) + 2 * spacingZ));
                points.Add(new ControlPoint(GetLastPoint()));
                points[points.Count - 1].offset.y = points[points.Count - 4].offset.y;
            }
            else
            {
                points.Add(
                    new ControlPoint(
                        new Vector3(GetLastPoint().x, 
                        center.y + collider.bounds.extents.y + spacingY.y, 
                        GetLastPoint().z)));

                points.Add(
                    new ControlPoint(
                        new Vector3(
                            GetLastPoint().x,
                            GetLastPoint().y,
                            GetLastPoint().z - collider.bounds.extents.z - spacingZ.z)));

                points.Add(
                    new ControlPoint(
                        GetLastPoint() - new Vector3(0, 2*collider.bounds.extents.y, 0) - 2 * spacingY));

                points.Add(
                    new ControlPoint(
                        GetLastPoint()));
                points[points.Count - 1].offset.z = points[points.Count - 4].offset.z;
            }

        }

        public void FindPath()
        {
            Collider[] wall;

            if (points[0].anchorTransform != null && points[1].anchorTransform != null)
            {
                startPoint = points[0].anchorTransform.position;
                endPoint = points[1].anchorTransform.position;
                this.transform.position = startPoint;

                //Finding the nearest wall from the starting point and setting the 2nd point 0.2f away from that wall
                wall = Physics.OverlapSphere(points[0].offset, 1f);
                points[1].offset = wall[0].ClosestPoint(points[0].offset) + spacingZ;

                //Checking if there is an obstacle to the right of the 2nd point
                var obstacle = CastRay(GetLastPoint(), Vector3.right);
                if(obstacle.collider != null ) 
                {
                    //If there is an obstacle, find a way above or underneath it
                    FoundObstacle(obstacle, Vector3.right);
                }
                
                //Create a new point as soon as there is no wall next to the wire
                points.Add(
                    new ControlPoint(
                        NextPoint(endPoint.x,points[1].offset,
                            Vector3.right,
                            Vector3.forward,
                            GetLastPointIndex())));

                //Go in Z direction and check wether there's an obstacle in the way
                obstacle = CastRay(GetLastPoint(), Vector3.forward);
                if (obstacle.collider != null && obstacle.collider.gameObject.tag != "wall")
                {
                    Debug.Log("Found an obstacle and it's called " + obstacle.collider.name);
                    FoundObstacle(obstacle, Vector3.forward);
                }
                //Create a new point when the wall next to the wire ends or there's another wall in the way
                points.Add(
                    new ControlPoint(
                        NextPoint(
                            CastRay(GetLastPoint(), Vector3.forward).point.z - 2*spacingZ.z,
                                GetLastPoint(), 
                                Vector3.forward,
                                Vector3.left,
                                GetLastPointIndex())));

                Debug.Log("I decided to create the " + GetLastPointIndex() + "th point at " + GetLastPoint());

                //Repeat process on x axis for remaining distance
                obstacle = CastRay(GetLastPoint(), Vector3.right);
                if (obstacle.collider != null)
                {
                    FoundObstacle(obstacle, Vector3.right);
                }
                else
                {
                    points.Add(
                        new ControlPoint(
                            NextPoint(
                                CastRay(GetLastPoint(),Vector3.right).point.x - 2 * spacingX.x,
                                    GetLastPoint(),
                                    Vector3.right,
                                    Vector3.forward,
                                    GetLastPointIndex())));
                }
                points.Add(
                    new ControlPoint(
                        new Vector3(
                            endPoint.x,GetLastPoint().y,GetLastPoint().z)));

                //Check if the endpoint is higher or lower than the last created point
                Vector3 upOrDown;
                if (endPoint.y > GetLastPoint().y)
                {
                    upOrDown = new Vector3(0, 1, 0);
                }
                else
                {
                    upOrDown = new Vector3(0, -1, 0);
                }

                //Check if there is an obstacle on the remaining distance on y-axis
                obstacle = CastRay(GetLastPoint(), upOrDown);
                if (obstacle.collider != null)
                {
                    FoundObstacle(obstacle, upOrDown);

                    points.Add(
                        new ControlPoint(
                            GetLastPoint()));
                    points[points.Count - 1].offset.y = endPoint.y;

                    points.Add(
                        new ControlPoint(
                            GetLastPoint()));
                    points[points.Count - 1].offset.z = endPoint.z+0.5f;
                }
                else
                {
                    points.Add(
                        new ControlPoint(
                            new Vector3(
                                GetLastPoint().x,
                                endPoint.y,
                                GetLastPoint().z)));
                    points.Add(
                        new ControlPoint(
                            new Vector3(
                                GetLastPoint().x,
                                GetLastPoint().y,
                                endPoint.z+0.5f)));
                }
            }
            else
            {
                FindStartEnd();
                FindPath();
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
