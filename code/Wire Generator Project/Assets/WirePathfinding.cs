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

        public Transform startPoint;
        public Transform endPoint;

        float distanceX;
        float distanceY;
        float distanceZ;

        [SerializeField]
        float weight = 1f;
        [SerializeField]
        float sagOffset = 0f;

        Mesh mesh;
        MeshFilter meshFilter;
        public List<ControlPoint> points = new List<ControlPoint>() { new ControlPoint(new Vector3(0,0,0)), new ControlPoint(new Vector3(1, 0, 0))};
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
            //castRay();

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

        public void castRay()
        {
            Collider[] wall = new Collider[1];

            if (points[0].anchorTransform != null && points[points.Count - 1].anchorTransform != null)
            {
                startPoint = points[0].anchorTransform;
                endPoint = points[points.Count - 1].anchorTransform;
                this.transform.position = startPoint.position;
                float startToEndX = endPoint.position.x - startPoint.position.x;
                float startToEndY = endPoint.position.y - startPoint.position.y;
                float startToEndZ = endPoint.position.z - startPoint.position.z;
                Vector3 spacingZ = new Vector3(0, 0, 0.2f);

                wall = Physics.OverlapSphere(points[0].offset, 1f);
                points[0].offset = wall[0].ClosestPoint(points[0].offset) + spacingZ;

                wall = new Collider[0];

                float remainingDistanceX = startToEndX;
                while(wall.Length == 0)
                {
                    wall = Physics.OverlapSphere(points[0].offset + new Vector3(remainingDistanceX, 0, 0),1f);
                    if(wall.Length > 0)
                    {
                        points[1].offset = wall[0].ClosestPoint(points[0].offset + new Vector3(remainingDistanceX, 0, 0)) + spacingZ;
                        points[1].offset.z = spacingZ.z;
                        Debug.Log("wall.Length is :" + wall.Length);
                    }
                    else
                    {
                        remainingDistanceX = remainingDistanceX / 2;
                        Debug.Log("Halfing remaining Distance on X to: " + remainingDistanceX);
                    }
                }
                bool bodenlos = false;
                float tick = 0.1f;
                while(bodenlos == false)
                {
                    if(Physics.OverlapSphere(points[1].offset + new Vector3(tick,0,0),0.3f).Length > 0)
                    {
                        Debug.Log("tick is " + tick);
                        tick += 0.1f;
                    }
                    else
                    {
                        points[1].offset += new Vector3(tick, 0, 0);
                        bodenlos = true;
                    }
                }


                points.Add(new ControlPoint(points[1].offset));
                points[2].offset.z = endPoint.transform.position.z;

                points.Add(new ControlPoint(points[2].offset + new Vector3(0, startToEndY, 0)));

                //points.Add(new ControlPoint(points[3].offset + new Vector3(remainingDistanceX, 0, 0)));
                // off by .7 on x axis

                points.Add(new ControlPoint(endPoint.transform.position));
            }

            if (points[points.Count - 1].offset == endPoint.transform.position)
            {
                Debug.Log("Goal reached");
            }

            //float wallPosX = wall[0].transform.position.x;
            //float wallPosY = wall[0].transform.position.y;
            //float wallPosZ = wall[0].transform.position.z;

            //if (points.Count == 2)
            //{
            //    float PosX = points[1].position.x;
            //    float PosY = points[1].position.y;
            //    float PosZ = points[1].position.z;

            //    float distanceX = PosX - wallPosX;
            //    float distanceY = PosY - wallPosY;
            //    float distanceZ = PosZ - wallPosZ;
            //    //Debug.Log("PosX - wallPosX = " + distanceX + ", PosY - wallPosY = " + distanceY + ", PosZ - wallPosZ = " + distanceZ);

            //    if(PosX - wallPosX <= PosY - wallPosY &&
            //        PosX - wallPosX <= PosZ - wallPosZ)
            //    {
            //        points[1].offset.x += wallPosX;
            //    }
            //    else if (PosY - wallPosY <= PosX - wallPosX &&
            //        PosY - wallPosY <= PosZ - wallPosZ)
            //    {
            //        points[1].offset.y += wallPosY;
            //    }
            //    else
            //    {
            //        points[1].offset = new Vector3(0, 0, wallPosZ - 0.2f);
            //    }

            //    distanceX = points[points.Count - 1].offset.x + endPoint.transform.position.x;
            //    bool touchy = false;
            //    bool ende = false;
            //    while(ende==false)
            //    {
            //        while (touchy==false)
            //        {
            //            //Distance to next point on x axis
            //            Collider[] wallForNewPoint = Physics.OverlapSphere(points[points.Count - 1].offset + new Vector3(distanceX, 0, 0), 1f);
            //            //Debug.Log(wallForNewPoint.Length);
            //            if (wallForNewPoint.Length>0){
            //                //Debug.Log("Collider gefunden bei " + wallForNewPoint[0].transform.position);
            //                points.Add(new ControlPoint(points[points.Count-1].offset + new Vector3(distanceX, 0, 0)));
            //                touchy = true;
            //            }
            //            else
            //            {
            //                distanceX = distanceX / 2;
            //            }
            //        }
            //        if(points[points.Count-1].offset.y != endPoint.transform.position.y)
            //        {
            //            Collider[] wallForNewPoint = Physics.OverlapSphere(points[points.Count-1].offset + new Vector3(0, points[points.Count - 1].offset.y + endPoint.transform.position.y,0), 1f);
            //            if(wallForNewPoint.Length > 0)
            //            {
            //                points.Add(new ControlPoint(points[points.Count - 1].offset + new Vector3(0, points[points.Count - 1].offset.y + endPoint.transform.position.y, 0)));
            //            }
            //        }
            //        if(points[points.Count - 1].offset == endPoint.transform.position)
            //        {
            //            Debug.Log("Ziel erreicht");
            //        }
            //        if (points[points.Count - 1].offset.x != endPoint.transform.position.y)
            //        {
            //            Collider[] wallForNewPoint = new Collider[1];
            //            distanceX = endPoint.transform.position.x - points[points.Count - 1].offset.x;
            //            do
            //            {
            //                wallForNewPoint = Physics.OverlapSphere(points[points.Count - 1].offset + new Vector3(distanceX, 0, 0), 1f);
            //                Debug.Log("DistanceX is currently : " + distanceX);
            //                Debug.Log("Number of collided walls is " + wallForNewPoint.Length);
            //                if (wallForNewPoint.Length > 0)
            //                {
            //                    Debug.Log("Wall found");
            //                    points.Add(new ControlPoint(points[points.Count - 1].offset + new Vector3(distanceX, 0, 0)));
            //                }
            //                else
            //                {
            //                    distanceX = distanceX / 2;
            //                }
            //            }
            //            while (wallForNewPoint.Length > 0);
            //        }
            //        ende = true;
            //    }
            //}

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
