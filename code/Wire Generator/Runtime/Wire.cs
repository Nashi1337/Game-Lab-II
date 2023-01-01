using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WireGenerator
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class Wire : MonoBehaviour
    {
        public Vector3 offsetPoint1 = new Vector3(0, 0, 0);
        public Vector3 offsetPoint2 = new Vector3(0, 0, 1);
        public Vector3 point1 {
            get { return (useAnchor1 && anchorTransform1 ? anchorTransform1 : transform).position + offsetPoint1; }
            set { offsetPoint1 = value - (useAnchor1 && anchorTransform1 ? anchorTransform1 : transform).position; }
        }
        public Vector3 point2 {
            get { return (useAnchor2 && anchorTransform2 ? anchorTransform2 : transform).position + offsetPoint2; }
            set { offsetPoint2 = value - (useAnchor2 && anchorTransform1 ? anchorTransform2:transform).position; }
        }

        public bool useAnchor1=false;
        public bool useAnchor2=false;

        public Transform anchorTransform1;
        public Transform anchorTransform2;

        float lengthFactor = 1f;

        public float radius=0.02f;

        Mesh mesh;

        public int corners=6;

        MeshFilter meshFilter;

        private void Reset()
        {
            mesh = new Mesh{name = "Wire"};
            meshFilter=GetComponent<MeshFilter>();
            GenerateMesh();
        }
        public void GenerateMesh()
        {
            //calculate vector from one end to the other
            var tangent = (offsetPoint2-offsetPoint1).normalized;

            Vector3 startpointVertice;
            
            if (tangent.y != 1)
            {
                //calculate vector perpendicular tangent
                var helpVector = Quaternion.Euler(0, -90, 0) * tangent;
                //cross returns vector perpendicular to two vectors

                startpointVertice = Vector3.Cross(tangent, helpVector).normalized;
            }
            else {
                startpointVertice = Vector3.right;
            }

            startpointVertice *= radius;


            var offsetCircle=Quaternion.AngleAxis(360f / corners, tangent);

            var tempVertices = new Vector3[corners*2];
            var tempNormals = new Vector3[corners * 2];
            for (int controlPoint = 0; controlPoint < 2; controlPoint++) {
                for (int i = 0; i < corners; i++)
                {
                    offsetCircle = Quaternion.AngleAxis((360f / corners) * i, tangent);
                    tempVertices[i + corners * controlPoint] = (offsetCircle * startpointVertice) + (controlPoint==0?offsetPoint1:offsetPoint2);
                    tempNormals[i + corners * controlPoint] = offsetCircle * startpointVertice;
                }
            }
            mesh.vertices = tempVertices;
            mesh.normals = tempNormals;
            var tempTriangles = new int[corners*6];
            for (int i = 0; i < corners; i++) {
                tempTriangles[i * 6 + 0] = i;
                tempTriangles[i * 6 + 1] = (i + 1) % corners;
                tempTriangles[i * 6 + 2] = corners + i;
                tempTriangles[i * 6 + 3] = (i + 1) % corners;
                tempTriangles[i * 6 + 4] = corners + (i + 1) % corners;
                tempTriangles[i * 6 + 5] = corners + i;
            }
            mesh.triangles=tempTriangles;
            meshFilter.sharedMesh = mesh;
        }
    }
}
