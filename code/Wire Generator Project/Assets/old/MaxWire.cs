using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaxWire : MonoBehaviour
{
    [SerializeField]
    Mesh mesh;
    public int radialSegments = 5;

    public GameObject startConnection;
    public GameObject endConnection;

    public float length;
    public float sagDepth;
    public float sagOffset = 0f;
    public float tension;
    public float weight = 5f;
    public float diameter = 0.1f;
    public Vector3[] points;
    public Vector3 startPos;
    public Vector3 endPos;


    private void OnEnable()
    {
        GenerateMesh();
    }

    private void Update()
    {
        startPos = startConnection ? startConnection.transform.position : transform.position;
        endPos = endConnection ? endConnection.transform.position : transform.position;
        length = Vector3.Distance(startPos, endPos);

        float sagAmount = tension + weight + sagOffset;
        float lowestPoint = CalculateWireSag(sagAmount, 0.5f);
        int positionCount = Mathf.RoundToInt(length + sagDepth);
        points = new Vector3[positionCount];

        Vector3 pivot = Vector3.Lerp(startPos, endPos, 0.5f);
        pivot.y += lowestPoint;
        gameObject.transform.position = pivot;

        for(int i = 0; i < positionCount; i++)
        {
            float wireSamplePoint = (float)i / (float)(positionCount - 1);

            Vector3 wirePoint = (endPos - startPos) * wireSamplePoint;

            wirePoint.y += CalculateWireSag(sagAmount, wireSamplePoint);

            points[i] = transform.InverseTransformPoint(startPos + wirePoint);
        }
    }

    public float CalculateWireSag(float gravity, float t)
    {
        return gravity * -Mathf.Sin(t * Mathf.PI);
    }


    private Mesh GenerateMesh()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
        }

        mesh.name = name + " Mesh";

        var verticesLength = radialSegments * points.Length;
        Vector3[] vertices = new Vector3[verticesLength];
        //Color?

        int[] indices = GenerateIndices();
        //uv
        //color

        if(verticesLength > mesh.vertexCount)
        {
            mesh.vertices = vertices;
            mesh.triangles = indices;
            //mesh.uv = uvs;
        }
        else
        {
            mesh.triangles = indices;
            mesh.vertices = vertices;
            //mesh.uv = uvs;
        }

        int currentVertIndex = 0;

        for(int i = 0; i < points.Length; i++)
        {
            Vector3[] circle = VertexRing(i);
            foreach(var vertex in circle)
            {
                vertices[currentVertIndex++] = vertex;
            }
        }

        mesh.vertices = vertices;
        //color;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();


        return mesh;
    }

    private int[] GenerateIndices()
    {
        var indices = new int[points.Length * radialSegments * 2 * 3];

        var currentIndicesIndex = 0;

        for(int segment = 1; segment < points.Length; segment++)
        {
            for(int side = 0; side < radialSegments; side++)
            {
                var vertIndex = (segment * radialSegments + side);
                var prevVertIndex = vertIndex - radialSegments;

                indices[currentIndicesIndex++] = prevVertIndex;
                indices[currentIndicesIndex++] = (side == radialSegments - 1) ? (vertIndex - (radialSegments - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = vertIndex;

                indices[currentIndicesIndex++] = (side == radialSegments - 1) ? (prevVertIndex - (radialSegments - 1)) : (prevVertIndex + 1);
                indices[currentIndicesIndex++] = (side == radialSegments - 1) ? (vertIndex - (radialSegments - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = prevVertIndex;
            }
        }

        return indices;
    }

    private Vector3[] VertexRing(int index)
    {
        var dirCount = 0;
        var forward = Vector3.zero;

        if(index > 0)
        {
            forward += (points[index] - points[index - 1]).normalized;
            dirCount++;
        }

        if(index < points.Length - 1)
        {
            forward += (points[index + 1] - points[index]).normalized;
            dirCount++;
        }

        forward = (forward / dirCount).normalized;
        var side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
        var up = Vector3.Cross(forward, side).normalized;

        var circle = new Vector3[radialSegments];
        var angle = 0f;
        var angleStep = (2 * Mathf.PI) / radialSegments;

        for(int i = 0; i < radialSegments; i++)
        {
            var x = Mathf.Cos(angle);
            var y = Mathf.Sin(angle);

            circle[i] = points[index] + side * x * diameter + up * y * diameter;

            angle += angleStep;
        }

        return circle;
    }

}
