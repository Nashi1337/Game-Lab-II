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
    public Transform wireStartObject;
    public Transform wireEndObject;
    public Vector3 start;
    public Vector3 end;
    private Vector3 pos;
    public float length;

    public float pointsPerMeter = 0.3f;
    public int pointCount = 11;

    Mesh mesh;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public int radialSegments = 5;
    public float diameter = 0.1f;
    public float weight = 0;
    public float tension = 1;
    public float tiling = 3f;

    public float sagDepth;

    public void CreateFromObject(Transform connector)
    {
        wireStartObject = connector.transform;
        CreateWire();
        StraightMode();
    }

    public void CreateWire()
    {
        /*if (wireEndObject != null && wireStartObject != null)
        {
            start = wireStartObject ? wireStartObject.transform.position : transform.position;
            end = wireEndObject ? wireEndObject.transform.position : transform.position;
        }
        else
        {*/
        points = new Vector3[pointCount];
        pos = transform.position;
        if(wireStartObject != null)
        {
            if(wireStartObject.localScale == new Vector3(1, 1, 1))
            {
                transform.position = wireStartObject.transform.position;
            }
            else
            {
                pos.x = wireStartObject.transform.position.x + wireStartObject.localScale.x;
                //pos.y = wireStartObject.transform.position.y + wireStartObject.localScale.y;
            }

            transform.position = pos;
            points[0] = /*(wireStartObject.transform.position) + */new Vector3(1,0,0);
        }
        else
        {
            points[0].x = 1;
        }
        points[pointCount - 1].x = points[0].x + 10;
        start = points[0];
        end = points[pointCount - 1];
        //}

        length = Vector3.Distance(start, end);

        tension = weight * length;

        float sagAmount = tension + weight; //+ Offset?
        float lowestPoint = CalculateWireSag(sagAmount, 0.5f);
        sagDepth = lowestPoint;

        //pointCount = Mathf.RoundToInt(pointsPerMeter * length + sagDepth);
        //pointCount = Mathf.Clamp(pointCount, 6, 50);
        //points = new Vector3[pointCount];

        Vector3 pivot = Vector3.Lerp(start, end, 0.5f);
        pivot.y += lowestPoint;
        //gameObject.transform.position = pivot;

        //Vector3 forward = (end - start).normalized;
        //if (forward != Vector3.zero) gameObject.transform.forward = forward;


        for (int i = 0; i < pointCount; i++)
        {
            float wireSamplePoint = (float)i / (float)(pointCount - 1);
            //Debug.Log("wireSamplePoint: " + wireSamplePoint);
            Vector3 wirePoint = (end - start) * wireSamplePoint;
            wirePoint.y += CalculateWireSag(sagAmount, wireSamplePoint);
            if (i == pointCount - 1)
            {
                //wirePoint.y = points[0].y;
            }
            points[i] = /*transform.InverseTransformPoint*/(start + wirePoint);

        }


        start = points[0];
        end = points[pointCount - 1];

        //UpdateMesh();
        GenerateMesh();

    }

    public void UpdateSpline()
    {
        //points = new Vector3[pointCount];
        //points[0].x = 0;
        //points[pointCount - 1].x = pointCount;

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

        //Vector3 forward = (end - start).normalized;
        //if (forward != Vector3.zero) gameObject.transform.forward = forward;

        
        for(int i = 0; i < pointCount; i++)
        {
            float wireSamplePoint = (float)i / (float)(pointCount-1);
            //Debug.Log("wireSamplePoint: " + wireSamplePoint);
            Vector3 wirePoint = (end - start) * wireSamplePoint;
            wirePoint.y += CalculateWireSag(sagAmount, wireSamplePoint);
            if(i == pointCount - 1)
            {
                //wirePoint.y = points[0].y;
            }
            points[i] = /*transform.InverseTransformPoint*/(start + wirePoint);
        }
        
        //start = transform.position + points[0];
        //end = transform.position + points[points.Length - 1];

        //UpdateMesh();
        GenerateMesh();
    }

    public void UpdateMesh(Material mat)
    {
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;
        //GenerateMesh();
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
        Array.Resize(ref points, pointCount + 3);
        point.x += 1f;
        points[pointCount - 3] = point;
        point.x += 1f;
        points[pointCount - 2] = point;
        point.x += 1f;
        points[pointCount - 1] = point;
    }

    public static float CalculateWireSag(float gravity, float t)
    {
        //Debug.Log("WireSag: " + gravity * -Mathf.Sin(t * Mathf.PI));
        return gravity * -Mathf.Sin(t * Mathf.PI);
    }

    public void SagMode(float sagWeight)
    {
        /*Vector3 sagPoint = Vector3.Lerp(points[0], points[pointCount - 1], 0.5f);
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
        */
        weight = sagWeight;
        UpdateSpline();
        //GenerateMesh();
    }

    public void StraightMode()
    {
        /*if(points[0].y > points[pointCount - 1].y)
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
        }*/
        weight = 0;
        UpdateSpline();
    }

    public int CurveCount
    {
        get
        {
            return (pointCount - 1) / 3;
        }
    }

        public Vector3 GetPoint(float t)
        {
            int i;
            if (t >= 1f)
            {
                t = 1f;
                i = pointCount - 4;
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
                i = pointCount - 4;
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
        var indices = new int[pointCount * radialSegments * 2 * 3];

        var currentIndicesIndex = 0;
        for (int segment = 1; segment < pointCount; segment++)
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
        if (index < pointCount - 1)
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

        mesh.name = name + " Mesh";

        var verticesLength = radialSegments * pointCount; //replaced points.length
        Vector3[] vertices = new Vector3[verticesLength];
        //Color[] colors = new Color[verticesLength];

        int[] indices = GenerateIndices();
        //Vector2[] uvs = GenerateUVs();
        //colors = GenerateColors();

        if (verticesLength > mesh.vertexCount)
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

        for (int i = 0; i < pointCount; i++)
        {
            Vector3[] circle = VertexRing(i);
            foreach (var vertex in circle)
            {
                vertices[currentVertIndex++] = vertex;
            }
        }

        mesh.vertices = vertices;
        //mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }

    public Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[pointCount * radialSegments];
        for(int segment = 0; segment < pointCount; segment++)
        {
            for(int side = 0; side < radialSegments; side++)
            {
                int vertIndex = (segment * radialSegments + side);
                float u = side / (radialSegments - 1);
                float v = (segment / (pointCount - 1f)) * (tiling * length);

                uvs[vertIndex] = new Vector2(v,u);
            }
        }
        return uvs;
    }

    public Color[] GenerateColors()
    {
        Color[] colors = new Color[pointCount * radialSegments];

        float wireSamplePoint = 0;
        for(int segment = 0; segment < pointCount; segment++)
        {
            wireSamplePoint = (float)segment / (float)(pointCount - 1);

            for(int side = 0; side < radialSegments; side++)
            {
                int vertIndex = (segment * radialSegments + side);
                //colors[vertIndex] = windData.Evaluate(wireSamplePoint);
            }
        }

        return colors;
    }
    public void Reset()
        {
        if(mesh == null)
        {
            mesh = new Mesh { name = "Wire Mesh" };

        }
        if(meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        CreateWire();
        meshFilter.mesh = GenerateMesh();

        //GenerateMesh();
        }
    }