using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class BezierSpline : MonoBehaviour
    {
    [SerializeField]
    private Vector3[] points;
    public Vector3 start;
    public Vector3 end;
    public float length;

    public float pointsPerMeter = 0.3f;
    public int pointCount = 11;

    Mesh mesh;
    public MeshFilter meshFilter;

    public int radialSegments = 5;
    public float diameter = 0.1f;
    public float weight = 0;
    public float tension = 1;

    public float sagDepth;

    private void Awake()
    {
        UpdateSpline();
    }

    public void UpdateSpline()
    {
        points = new Vector3[pointCount];
        points[0].x = 0;
        points[pointCount-1].x = 10;

        //start and end could be connector points on other game objects
        start = points[0];
        end = points[pointCount - 1];
        length = Vector3.Distance(start, end);
        
        tension = weight * length;

        float sagAmount = tension + weight; //+ Offset?
        float lowestPoint = CalculateWireSag(sagAmount, 0.5f);
        sagDepth = lowestPoint;



        Vector3 pivot = Vector3.Lerp(start, end, 0.5f);
        pivot.y += lowestPoint;
        //gameObject.transform.position = pivot;
        
        for(int i = 0; i < pointCount; i++)
        {
            float wireSamplePoint = (float)i / (float)(pointCount-1);
            Debug.Log("wireSamplePoint: " + wireSamplePoint);
            Vector3 wirePoint = (end - start) * wireSamplePoint;
            wirePoint.y += CalculateWireSag(sagAmount, wireSamplePoint);
            points[i] = /*transform.InverseTransformPoint*/(start + wirePoint);
        }

        start = transform.position + points[0];
        end = transform.position + points[pointCount - 1];

        UpdateMesh();
    }

    public void UpdateMesh()
    {
        if (meshFilter == null)
        {

        }
        meshFilter = gameObject.AddComponent<MeshFilter>();
        //meshFilter.mesh = 
        GenerateMesh();
    }

    public int ControlPointCount
    {
        get
        {
            return points.Length;
        }
    }

    public Vector3 GetControlPoint(int index)
    {
        return points[index];
    }

    public void SetControlPoint(int index, Vector3 point)
    {
        points[index] = point;
    }

    public void AddCurve()
    {
        Vector3 point = points[pointCount - 1];
        Array.Resize(ref points, points.Length + 3);
        point.x += 1f;
        points[points.Length - 3] = point;
        point.x += 1f;
        points[points.Length - 2] = point;
        point.x += 1f;
        points[points.Length - 1] = point;
    }

    public static float CalculateWireSag(float gravity, float t)
    {
        Debug.Log("WireSag: " + gravity * -Mathf.Sin(t * Mathf.PI));
        return gravity * -Mathf.Sin(t * Mathf.PI);
    }

    public void SagMode()
    {
        Vector3 sagPoint = Vector3.Lerp(points[0], points[pointCount - 1], 0.5f);
        sagPoint.y -= CalculateWireSag(sagDepth, 0.5f);
        Debug.Log("sagPoint: " + sagPoint);

        int half = points.Length / 2;
        for(int i = 1; i < points.Length-1; i++)
        {
            points[i].x = points[i-1].x + (points[pointCount - 1].x / points.Length);
            Debug.Log("X Koordinate von Punkt " + i + " angepasst");
        }
            for(int j = 1; j < half; j++)
            {
                points[j].y += (j) * sagDepth;
            }
            for (int j = half; j >= 0; j--)
            {
                points[j + 1].y += (half - j + 1) * sagDepth;
            Debug.Log("Punkt " + j + ": " + points[j].y);
            }

        GenerateMesh();
    }

    public void StraightMode()
    {
        if(points[0].y > points[pointCount - 1].y)
        {
            for(int i = 1; i < points.Length-2; i++)
            {
                points[i].x = points[i-1].x + points[pointCount - 1].x / pointCount;
                points[i].y = points[i - 1].y - points[pointCount - 1].y / pointCount;
            }
        }
        else if(points[0].y < points[points.Length-1].y)
        {
            for (int i = 1; i < points.Length - 1; i++)
            {
                points[i].x = points[i-1].x + points[pointCount - 1].x / points.Length;
                points[i].y = points[0].y + points[i + 1].y / points.Length;
            }
        }
        else
        {
            for (int i = 1; i < points.Length - 2; i++)
            {
                points[i].x = points[i-1].x + points[pointCount - 1].x / pointCount - 1;
                points[i].y = points[0].y + points[pointCount - 1].y / pointCount - 1;
            }
        }
        GenerateMesh();
    }

        public int CurveCount
        {
            get
            {
                return (points.Length - 1) / 3;
            }
        }

        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetPoint(
                points[i], points[i + 1], points[i + 2], points[i + 3], t));
        }

        public Vector3 GetVelocity(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * CurveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(
                points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
        }

        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

    public int[] GenerateIndices()
    {
        // Two triangles and 3 vertices
        var indices = new int[points.Length * radialSegments * 2 * 3];

        var currentIndicesIndex = 0;
        for (int segment = 1; segment < points.Length; segment++)
        {
            for (int side = 0; side < radialSegments; side++)
            {
                var vertIndex = (segment * radialSegments + side);
                var prevVertIndex = vertIndex - radialSegments;

                // Triangle one
                indices[currentIndicesIndex++] = prevVertIndex;
                indices[currentIndicesIndex++] = (side == radialSegments - 1) ? (vertIndex - (radialSegments - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = vertIndex;

                // Triangle two
                indices[currentIndicesIndex++] = (side == radialSegments - 1) ? (prevVertIndex - (radialSegments - 1)) : (prevVertIndex + 1);
                indices[currentIndicesIndex++] = (side == radialSegments - 1) ? (vertIndex - (radialSegments - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = prevVertIndex;
            }
        }

        return indices;
    }

    public  Vector3[] VertexRing(int index)
    {
        var dirCount = 0;
        var forward = Vector3.zero;

        //If not first index
        if (index > 0)
        {
            forward += (points[index] - points[index - 1]).normalized;
            dirCount++;
        }

        //If not last index
        if (index < points.Length - 1)
        {
            forward += (points[index + 1] - points[index]).normalized;
            dirCount++;
        }

        //Forward is the average of the connecting edges directions
        forward = (forward / dirCount).normalized;
        var side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
        var up = Vector3.Cross(forward, side).normalized;

        var circle = new Vector3[radialSegments];
        var angle = 0f;
        var angleStep = (2 * Mathf.PI) / radialSegments;

        for (int i = 0; i < radialSegments; i++)
        {
            var x = Mathf.Cos(angle);
            var y = Mathf.Sin(angle);

            circle[i] = points[index] + side * x * diameter + up * y * diameter;

            angle += angleStep;
        }

        return circle;
    }

    public Mesh GenerateMesh()
    {
        if(mesh == null)
        {
            mesh = new Mesh();
        }

        var verticesLength = radialSegments * points.Length;
        Vector3[] vertices = new Vector3[verticesLength];
        Color[] colors = new Color[verticesLength];

        int[] indices = GenerateIndices();
        //Vector2[] uvs = GenerateUVs(spline);
        //colors = GenerateColors(spline);

        if (verticesLength > mesh.vertexCount)
        {
            mesh.vertices = vertices;
            mesh.triangles = indices;
            //spline.mesh.uv = uvs;
        }
        else
        {
            mesh.triangles = indices;
            mesh.vertices = vertices;
            //spline.mesh.uv = uvs;
        }

        int currentVertIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3[] circle = VertexRing(i);
            foreach (var vertex in circle)
            {
                vertices[currentVertIndex++] = vertex;
            }
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        //meshFilter.sharedMesh = mesh;

        return mesh;
    }

    public void Reset()
        {
        mesh = new Mesh { name = "Wire Mesh" };
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GenerateMesh();

            /*points = new Vector3[]
            {
            new Vector3(1f, 0f, 0f),
            new Vector3(2f, 0f, 0f)
            };
            */
        UpdateSpline();
        GenerateMesh();
        }
    }