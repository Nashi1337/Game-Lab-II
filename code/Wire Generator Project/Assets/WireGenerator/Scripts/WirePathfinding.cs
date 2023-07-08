using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization;
using UnityEngine.Rendering;
using UnityEditor;

namespace WireGeneratorPathfinding
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class WirePathfinding : MonoBehaviour
    {
        [Serializable]
        public class ControlPoint
        {
            public Vector3 position;
            public ControlPoint(Vector3 offset) {
                this.position = offset;
            }
        }

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

        //Assumes mesh for straight parts is aligned with z-Axis
        [SerializeField] private Mesh straightMesh;
        [SerializeField] public float sizePerStraightMesh = 0.2f;
        [SerializeField] public int numberPerStraightSegment = 1;

        [SerializeField] private float curveSize = 0.1f;
        [SerializeField] private Mesh curveMesh;

        [SerializeField] StraightPartMeshGenerationMode straightPartMeshGenerationMode = StraightPartMeshGenerationMode.RoundAndScaleLast;

        [SerializeField]
        public GameObject startPointGO;
        [SerializeField]
        public GameObject endPointGO;
        public Vector3 startPos;
        public Vector3 endPos;
        GameObject generateStartPoint;
        GameObject generateEndPoint;
        public bool wireGenerated=false;
        private bool pointMoved = false;
        bool noWalls;
        bool useURP;

        private void Awake()
        {
            Reset();
            FindPath();
        }
        void Update()
        {
            if (wireGenerated)
            {
                if (startPos != startPointGO.transform.position)
                {
                    if (!pointMoved)
                    {
                        pointMoved = true;
                    }
                }
                else
                {
                    if (pointMoved)
                    {
                        pointMoved = false;
                        FindPath();
                    }
                }
                if (endPos != endPointGO.transform.position)
                {
                    if (!pointMoved)
                    {
                        pointMoved = true;
                    }
                }
                else
                {
                    if (pointMoved)
                    {
                        pointMoved = false;
                        FindPath();
                    }
                }
            }
            startPos = startPointGO.transform.position;
        }

        float CalculateWireSag(float gravity, float t)
        {
            return gravity * -Mathf.Sin(t * Mathf.PI);
        }

        public void Reset()
        {
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                Material blueURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/blueURP.mat");
                Material redURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/redURP.mat");
                Material blackURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/blackURP.mat");
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/StartPoint.prefab").GetComponent<MeshRenderer>().material = blueURP;
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/EndPoint.prefab").GetComponent<MeshRenderer>().material = redURP;
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/Wire.prefab").GetComponent<MeshRenderer>().sharedMaterials[0] = blackURP;
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/Wire.prefab").GetComponent<MeshRenderer>().sharedMaterials[1] = blackURP;
                GameObject.FindWithTag("startPoint").gameObject.GetComponent<MeshRenderer>().sharedMaterial = blueURP;
                GameObject.FindWithTag("endPoint").gameObject.GetComponent<MeshRenderer>().sharedMaterial = redURP;
                Material[] wireMaterials = this.GetComponent<MeshRenderer>().sharedMaterials;
                wireMaterials[0] = blackURP;
                wireMaterials[1] = blackURP;
                this.GetComponent<MeshRenderer>().sharedMaterials = wireMaterials;
            }
            if (startPointGO == null || endPointGO == null)
            {
                FindStartEnd();
            }
            if (generateEndPoint == null || generateStartPoint == null)
            {
                generateEndPoint = GameObject.Find("GenerateEndPoint");
                generateStartPoint = GameObject.Find("GenerateStartPoint");
            }
            startPos = startPointGO.transform.position;
            endPos = endPointGO.transform.position;
            wireGenerated = false;
            points = new List<ControlPoint>() { new ControlPoint(new Vector3(0, 0, 0)), new ControlPoint(new Vector3(1, 0, 0)) };

            mesh = new Mesh{name = "Wire"};
            meshFilter=GetComponent<MeshFilter>();

            float tension = weight * lengthFactor;
            float sag = tension + weight + sagOffset;
            float minimum = CalculateWireSag(sag, 0.5f);
            SetMesh(GenerateMeshUsingPrefab());
        }
        public void FindStartEnd()
        {
            startPointGO = GameObject.FindGameObjectWithTag("startPoint");
            endPointGO = GameObject.FindGameObjectWithTag("endPoint");
        }

        public RaycastHit CastRay(Vector3 start, Vector3 direction)
        {
            Ray ray = new Ray(transform.TransformPoint(start), transform.TransformPoint(direction));
            Physics.Raycast(ray, out RaycastHit hitData, Mathf.Infinity);
            return hitData;
        }
        public Vector3 FindClosestPoint(Vector3 origin)
        {
            float spacing = 0.2f;

            Vector3 closestPoint = Vector3.positiveInfinity;
            float bestDistance = Mathf.Infinity;

            Vector3[] directons = {
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.right,
                Vector3.back,
                Vector3.forward
            };

            foreach (Vector3 dir in directons)
            {
                Ray ray = new Ray(origin, dir);
                Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity);
                if ((hit.transform == null) || (!hit.transform.CompareTag("wall")))
                {
                    continue;
                }
                float distance = hit.distance - spacing;
                if (bestDistance <= distance)
                {
                    continue;
                }
                bestDistance = distance;
                closestPoint = origin + dir * distance;
            }

            if (bestDistance == Mathf.Infinity)
            {
                Debug.Log("I think there is no wall anywhere :(");
                noWalls = true;
                return endPos;
            }
            else
            {
                noWalls = false;
                return new Vector3(
                    //Mathf.Round
                    (closestPoint.x),
                    //Mathf.Round
                    (closestPoint.y),
                    //Mathf.Round
                    (closestPoint.z));
            }
        }
        Vector3 GetLastPoint()
        {
            return points[points.Count-1].position;
        }

        int GetLastPointIndex()
        {
            return points.Count-1;
        }

        public void FindPath()
        {
            if (startPointGO != null && endPointGO != null)
            {
                if (wireGenerated)
                {
                    wireGenerated = false;
                    Reset();
                }

                startPos = startPointGO.transform.position;
                endPos = endPointGO.transform.position;

                float distanceX = endPos.x - startPos.x;
                float distanceY = endPos.y - startPos.y;
                float distanceZ = endPos.z - startPos.z;
                
                //POINT 0
                points[0].position = startPointGO.transform.position;

                if (distanceY > 0)
                {

                    var obstacle = CastRay(points[0].position, Vector3.up);
                    if(obstacle.collider == null)
                    {
                        //POINT 1 with no obstacle
                        points[1].position = new Vector3(points[0].position.x,endPos.y,points[0].position.z);
                        Debug.Log("no obstacle in y direction found");
                    }
                    else
                    {
                        //POINT 1 with obstacle
                        points[1].position = new Vector3(points[0].position.x,obstacle.point.y,points[0].position.z)-spacingY;

                        Vector3 center = obstacle.collider.bounds.center;
                        float positiveDistance = Mathf.Abs(center.z + obstacle.collider.bounds.extents.z);
                        float negativeDistance = Mathf.Abs(center.z - obstacle.collider.bounds.extents.z);

                        //POINT 2
                        if(positiveDistance < negativeDistance)
                        {
                            points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, positiveDistance) + spacingZ));
                        }
                        else
                        {
                            points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, negativeDistance) - spacingZ));
                        }
                        //POINT 3
                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0,2*obstacle.collider.bounds.extents.y,0) + (2 * spacingY)));
                        //POINT 4
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x,GetLastPoint().y,endPos.z)));
                    }

                }
                else
                {
                    //if there is no difference on y axis, delete last point. then new points will be created
                    points.Remove(points[GetLastPointIndex()]);
                }
                Debug.Log(distanceX);
                if(distanceX > 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.right);
                    if (obstacle.collider == null)
                    {
                        //POINT 2 or 5 with no obstacle
                        points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                        Debug.Log("no obstacle in x direction found");
                    }
                    else
                    {
                        //POINT 2 or 5 with obstacle
                        points.Add(new ControlPoint(new Vector3(obstacle.point.x, GetLastPoint().y, GetLastPoint().z) - spacingX));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHit = center.y - obstacle.point.y;

                        float distanceToBorder = Mathf.Abs(distanceCenterHit) + obstacle.collider.bounds.extents.y;

                        //POINT 3 or 6
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y + distanceToBorder, GetLastPoint().z)));
                        //POINT 4 or 7
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x + 2 * obstacle.collider.bounds.extents.x, GetLastPoint().y, GetLastPoint().z) + 2 * spacingX));
                        //POINT 5 or 8
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, points[GetLastPointIndex() - 2].position.y, GetLastPoint().z)));
                    }
                    //POINT 6 or 9
                    points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                }
                else
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.left);
                    if (obstacle.collider == null)
                    {
                        //POINT 2 or 5 with no obstacle
                        points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                        Debug.Log("no obstacle in x direction found");
                    }
                    else
                    {
                        //POINT 2 or 5 with obstacle
                        points.Add(new ControlPoint(new Vector3(obstacle.point.x, GetLastPoint().y, GetLastPoint().z) + spacingX));

                        Vector3 center = obstacle.collider.bounds.center;

                        //POINT 3 or 6
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x,GetLastPoint().y + Mathf.Abs(center.y + obstacle.collider.bounds.extents.y), GetLastPoint().z)));
                        //POINT 4 or 7
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x - 2*obstacle.collider.bounds.extents.x,GetLastPoint().y,GetLastPoint().z)-2*spacingX));
                        //POINT 5 or 8
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, points[GetLastPointIndex() - 2].position.y, GetLastPoint().z)));
                    }
                    //POINT 6 or 9
                    points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                }

                if (distanceY > 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.up);
                    if (obstacle.collider == null)
                    {
                        //POINT 3 or 7 or 10 with no obstacle
                        points.Add(new ControlPoint(endPos));
                        Debug.Log("no obstacle in y direction found");
                    }
                    else
                    {
                        //POINT 3 or 7 or 10 with obstacle
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, obstacle.point.y, GetLastPoint().z) - spacingY));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHit = center.z - obstacle.point.z;

                        float distanceToBorder = Mathf.Abs(distanceCenterHit) - obstacle.collider.bounds.extents.z;

                        //POINT 4 or 8 or 11
                        if (distanceCenterHit<0)
                        {
                            points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, distanceToBorder) + spacingZ));
                        }
                        else
                        {
                            points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, distanceToBorder) - spacingZ));
                        }
                        //POINT 5 or 9 or 12
                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 2 * obstacle.collider.bounds.extents.y, 0) + (2 * spacingY)));
                        //POINT 6 or 10 or 13
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, endPos.z)));
                    }
                    points.Add(new ControlPoint(endPos));
                }          

                wireGenerated = true;
                SetMesh(GenerateMeshUsingPrefab());
            }
            else
            {
                FindStartEnd();
                FindPath();
            }
        }

        public void SetMesh(Mesh mesh)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        }

        public Mesh GenerateMeshUsingPrefab()
        {
            //Assumes mesh for straight parts is aligned with z-Axis
            //Curve Parts(90)
            //Assumes Open end point toward +Z and +Y and quadratic bounds for curves
            CombineInstance[] combineInstances = new CombineInstance[2];

            combineInstances[0] = new CombineInstance() { mesh = GenerateStraightPartMesh(), transform = Matrix4x4.identity };

            combineInstances[1] = new CombineInstance() { mesh = GenerateCurvePartMesh(), transform = Matrix4x4.identity };
            //combineInstances[1] = new CombineInstance() { mesh = WireGenerator.WireMesh.DeformMeshUsingBezierCurve(Instantiate<Mesh>(straightMesh)) }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combineInstances, false);
            return mesh;
        }

        public Mesh GenerateStraightPartMesh()
        {
            Mesh mesh = new Mesh();
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            combineInstances.AddRange(GenerateStraightParts());
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combineInstances.ToArray());
            return mesh;
        }

        public Mesh GenerateCurvePartMesh()
        {
            Mesh mesh = new Mesh();
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            combineInstances.AddRange(GenerateCurveParts());
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combineInstances.ToArray());
            return mesh;
        }

        enum StraightPartMeshGenerationMode
        {
            FixedNumber, RoundAndScaleAll, RoundAndScaleLast
        }
        private List<CombineInstance> GenerateStraightParts()
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();


            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 difference = GetPosition(i + 1) - GetPosition(i);

                if (curveSize > difference.magnitude / 2)
                {
                    continue;
                }

                Vector3 beginning = GetPosition(i) - transform.position + curveSize * difference.normalized;
                Vector3 end = GetPosition(i + 1) - transform.position - curveSize * difference.normalized;



                int numberOfSectionDivisions = 0;
                switch (straightPartMeshGenerationMode)
                {
                    case StraightPartMeshGenerationMode.FixedNumber:
                        numberOfSectionDivisions = numberPerStraightSegment;
                        break;
                    case StraightPartMeshGenerationMode.RoundAndScaleAll:
                        numberOfSectionDivisions = Math.Max(1, Mathf.RoundToInt((end - beginning).magnitude / sizePerStraightMesh));
                        break;
                    case StraightPartMeshGenerationMode.RoundAndScaleLast:
                        numberOfSectionDivisions = Math.Max(1, Mathf.RoundToInt((end - beginning).magnitude / sizePerStraightMesh)) - 1;
                        end = beginning + numberOfSectionDivisions * sizePerStraightMesh * (end - beginning).normalized;
                        break;
                    //default: throw new UnexpectedEnumValueException<StraightPartMeshGenerationMode>(straightPartMeshGenerationMode);
                }

                combineInstances.AddRange(GenerateStraightSection(beginning, end, numberOfSectionDivisions));
                if (straightPartMeshGenerationMode == StraightPartMeshGenerationMode.RoundAndScaleLast)
                {
                    combineInstances.Add(GenerateStraightPart(end, GetPosition(i + 1) - transform.position - curveSize * difference.normalized));
                }
            }

            return combineInstances;
        }

        private List<CombineInstance> GenerateStraightSection(Vector3 sectionBeginning, Vector3 sectionEnd, int sectionDivisions)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            for (int i = 0; i < sectionDivisions; i++)
            {
                Vector3 difference = sectionEnd - sectionBeginning;
                Vector3 partSize = difference / sectionDivisions;

                Vector3 partBeginning = sectionBeginning + partSize * i;
                Vector3 partEnd = sectionBeginning + partSize * (i + 1);
                combineInstances.Add(GenerateStraightPart(partBeginning, partEnd));
            }
            return combineInstances;
        }

        private CombineInstance GenerateStraightPart(Vector3 beginning, Vector3 end)
        {
            Vector3 difference = end - beginning;

            Vector3 translation = beginning + difference / 2;
            Quaternion rotation = Quaternion.LookRotation(difference);
            Vector3 scaling = new Vector3(radius / straightMesh.bounds.extents.x, radius / straightMesh.bounds.extents.y, (end - beginning).magnitude / straightMesh.bounds.size.z);

            Matrix4x4 transformMatrix = Matrix4x4.TRS(translation, rotation, scaling);
            return new CombineInstance() { mesh = straightMesh, transform = transformMatrix };
        }

        private List<CombineInstance> GenerateCurveParts()
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 differenceNext = -GetPosition(i) + GetPosition(i + 1);
                Vector3 differencePrevious = -GetPosition(i) + GetPosition(i - 1);

                Vector3 position = GetPosition(i) - transform.position;
                //Quaternion rotation = Quaternion.LookRotation(differenceNext, differencePrevious);
                //Vector3 scale = new Vector3(radius / curveMesh.bounds.extents.x, curveSize / curveMesh.bounds.extents.y, curveSize / curveMesh.bounds.extents.z) ;

                Mesh mesh = Instantiate<Mesh>(curveMesh);

                CombineInstance scaler = new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(radius / curveMesh.bounds.extents.x, radius / curveMesh.bounds.extents.y, 1)) };
                Mesh mesh2 = new Mesh();
                mesh2.CombineMeshes(new CombineInstance[] { scaler });

                float thisCurveSizePrev = Mathf.Min(curveSize, differencePrevious.magnitude / 2f);
                float thisCurveSizeNext = Mathf.Min(curveSize, differenceNext.magnitude / 2f);
                WireGeneratorPathfinding.WireMesh.DeformMeshUsingBezierCurve(mesh2, WireGeneratorPathfinding.WireMesh.Axis.Z, differencePrevious.normalized * thisCurveSizePrev, differencePrevious.normalized * thisCurveSizePrev / 2, differenceNext.normalized * thisCurveSizeNext / 2, differenceNext.normalized * thisCurveSizeNext);
                
                Matrix4x4 transformMatrix = Matrix4x4.TRS(position, Quaternion.identity/*rotation*/, Vector3.one/*scale*/);

                combineInstances.Add(new CombineInstance
                {
                    mesh = mesh2,
                    transform = transformMatrix
                }); ;
            }

            return combineInstances;
        }




        public Vector3 GetPosition(int i) {
            return (transform.position + points[i].position);
        }
        public void SetPosition(int i, Vector3 position)
        {
            points[i].position = position - transform.position;
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
                    tempVertices[i + corners * controlPointId] = (offsetCircle * startpointVertice) + points[controlPointId].position;
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
        public void ShowWire(bool showWire)
        {
            if (startPointGO != null && endPointGO != null)
            {
                if (showWire)
                {
                    startPointGO.gameObject.GetComponent<MeshRenderer>().enabled = true;
                    endPointGO.gameObject.GetComponent<MeshRenderer>().enabled = true;
                    this.gameObject.GetComponent<MeshRenderer>().enabled = true;
                }
                else
                {
                    startPointGO.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    endPointGO.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    this.gameObject.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
    }

}
