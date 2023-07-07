using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WireGeneratorPathfinding.WirePathfinding;

namespace WireGeneratorPathfinding
{
    public class WireMesh
    {
        public enum Axis
        {
            X,Y,Z
        }

        public static void DeformMeshUsingBezierCurve(Mesh mesh, Axis axis , Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Bounds bounds = mesh.bounds;
            Vector3[] vertices = mesh.vertices;
            for(int i=0; i<mesh.vertices.Length;i++)
            {
                float t = (mesh.vertices[i][(int)axis]-bounds.min[(int)axis])/bounds.size[(int)axis];
                //TODO respect axis;
                Vector3 difference = new Vector3 (mesh.vertices[i].x, mesh.vertices[i].y,0);

                //Calculate using Berstein Polynomial
                Vector3 bezierCurvePoint = p0 * (-t * t * t + 3 * t * t - 3 * t + 1)
                                         + p1 * (3 * t * t * t - 6 * t * t + 3 * t)
                                         + p2 * (-3 * t * t * t + 3 * t * t)
                                         + p3 * (t * t * t);
                Vector3 derivative = p0 * (-3 * t * t + 6 * t - 3)
                                   + p1 * (9 * t * t - 12 * t + 3)
                                   + p2 * (-9 * t * t + 6 * t)
                                   + p3 * (3 * t * t);
                Vector3 tangent = derivative.normalized;

                Quaternion q = Quaternion.FromToRotation(Vector3.forward,tangent);

                Vector3 newDifference = q * difference;
                vertices[i] = bezierCurvePoint+newDifference;


            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
