using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization;
using UnityEngine.Rendering;
using UnityEditor;

namespace WireGenerator
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class Wire : MonoBehaviour
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
        public float radius=6f;
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
        [SerializeField]
        public Material wireMaterial;
        [SerializeField]
        public Material cornerMaterial;
        GameObject[] allStartPoints;
        GameObject[] allEndPoints;
        public Vector3 startPos;
        public Vector3 endPos;
        GameObject generateStartPoint;
        GameObject generateEndPoint;
        public bool wireGenerated=false;
        private bool pointMoved = false;
        public int wireIndex=0;


        private void Awake()
        {
            Wire[] allWires = FindObjectsOfType<Wire>();
            foreach(Wire wire in allWires)
            {
                if (wire.wireIndex == wireIndex)
                {
                    wire.wireIndex = wireIndex;
                    wireIndex++;
                }
            }
            if (gameObject.transform.childCount > 0)
            {
                WireStartEnd[] startEndPoint = GetComponentsInChildren<WireStartEnd>();
                foreach(WireStartEnd startEnd in startEndPoint)
                {
                    startEnd.index = wireIndex;
                }
                FindPath();
            }
        }
        void Update()
        {
            if (transform.position != Vector3.zero)
            {
                transform.position = Vector3.zero;
            }
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
            startPos = startPointGO.transform.position;
            }
        }

        public void Reset()
        {
            wireGenerated = false;
            this.transform.position = Vector3.zero;

            if (GraphicsSettings.renderPipelineAsset != null)
            {
                UpdateMaterials();
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
            points = new List<ControlPoint>() { new ControlPoint(new Vector3(0, 0, 0)), new ControlPoint(new Vector3(1, 0, 0)) };

            mesh = new Mesh{name = "Wire"};
            meshFilter=GetComponent<MeshFilter>();

            SetMesh(GenerateMeshUsingPrefab());
        }
        public void FindStartEnd()
        {
            startPointGO = null;
            endPointGO = null;
            allStartPoints = GameObject.FindGameObjectsWithTag("startPoint");
            allEndPoints = GameObject.FindGameObjectsWithTag("endPoint");
            if (allStartPoints != null)
            {
                foreach(GameObject startPoint in allStartPoints)
                {
                    if(startPoint.GetComponent<WireStartEnd>().index == wireIndex)
                    {
                        startPointGO = startPoint;
                    }
                }
            }
            if (allEndPoints != null)
            {
                foreach(GameObject endPoint in allEndPoints)
                {
                    if(endPoint.GetComponent<WireStartEnd>().index == wireIndex)
                    {
                        endPointGO = endPoint;
                    }
                }
            }
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                UpdateMaterials();
            }
        }

        public RaycastHit CastRay(Vector3 start, Vector3 direction, float distance)
        {
            Ray ray = new Ray(transform.TransformPoint(start), transform.TransformPoint(direction));
            Physics.Raycast(ray, out RaycastHit hitData, distance);
            return hitData;
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
            FindStartEnd();
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

                points[0].position = startPointGO.transform.position;

                if (distanceY > 0)
                {
                    var obstacle = CastRay(points[0].position, Vector3.up, endPos.y - points[0].position.y);
                    if (obstacle.collider == null)
                    {
                        points[1].position = new Vector3(points[0].position.x, endPos.y, points[0].position.z);
                    }
                    else
                    {
                        points[1].position = new Vector3(points[0].position.x, obstacle.point.y, points[0].position.z) - spacingY;

                        Vector3 center = obstacle.collider.bounds.center;
                        float positiveDistance = Mathf.Abs(center.z + obstacle.collider.bounds.extents.z);
                        float negativeDistance = Mathf.Abs(center.z - obstacle.collider.bounds.extents.z);

                        if (positiveDistance < negativeDistance)
                        {
                            points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, positiveDistance) + spacingZ));
                        }
                        else
                        {
                            points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, negativeDistance) - spacingZ));
                        }
                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 2 * obstacle.collider.bounds.extents.y, 0) + (2 * spacingY)));

                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, points[GetLastPointIndex() - 2].position.z)));
                    }
                }
                else
                {
                    //if there is no difference on y axis, delete last point. then new points will be created
                    points.Remove(points[GetLastPointIndex()]);
                }
                if(distanceX > 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.right,Mathf.Abs(endPos.x)-GetLastPoint().x);
                    if (obstacle.collider != null)
                    {
                        points.Add(new ControlPoint(new Vector3(obstacle.point.x, GetLastPoint().y, GetLastPoint().z) - spacingX));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHitUp = center.y - obstacle.point.y;
                        float distanceCenterHitForward = center.z - obstacle.point.z;

                        float distanceToBorderUp = obstacle.collider.bounds.extents.y - Mathf.Abs(distanceCenterHitUp);
                        float distanceToBorderForward = Mathf.Abs(distanceCenterHitForward) + obstacle.collider.bounds.extents.z;

                        if(distanceToBorderUp < distanceToBorderForward)
                        {
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y + distanceToBorderUp, GetLastPoint().z)+spacingY));
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x + 2 * obstacle.collider.bounds.extents.x, GetLastPoint().y, GetLastPoint().z) + 2 * spacingX));
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x, points[GetLastPointIndex() - 2].position.y,GetLastPoint().z)));
                        }
                        else
                        {
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, GetLastPoint().z - distanceToBorderForward)-spacingZ));
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x + 2 * obstacle.collider.bounds.extents.x, GetLastPoint().y, GetLastPoint().z) + 2 * spacingX));
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, points[GetLastPointIndex() - 2].position.z)));
                        }
                    }
                    points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                }
                else if(distanceX < 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.left,Mathf.Abs(endPos.x-GetLastPoint().x));
                    if (obstacle.collider == null)
                    {
                        points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                    }
                    else
                    {
                        points.Add(new ControlPoint(new Vector3(obstacle.point.x, GetLastPoint().y, GetLastPoint().z) + spacingX));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHitUp = center.y - obstacle.point.y;
                        float distanceCenterHitForward = center.z - obstacle.point.z;
                        float distanceToBorderUp = Mathf.Abs(distanceCenterHitUp) + obstacle.collider.bounds.extents.y;
                        float distanceToBorderForward = Mathf.Abs(distanceCenterHitForward) + obstacle.collider.bounds.extents.z;

                        if (distanceToBorderUp < distanceToBorderForward)
                        {
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y + distanceToBorderUp, GetLastPoint().z)));
                        }
                        else
                        {
                            points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, GetLastPoint().z - distanceToBorderForward)));
                        }
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x - 2 * obstacle.collider.bounds.extents.x, GetLastPoint().y, GetLastPoint().z) - 2 * spacingX));
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, points[GetLastPointIndex() - 2].position.z)));
                    }
                    points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, GetLastPoint().z)));
                }

                if (distanceZ > 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.forward, Mathf.Abs(endPos.z) + Mathf.Abs(GetLastPoint().z));
                    if (obstacle.collider != null)
                    {
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, obstacle.point.z) - spacingZ));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHit = center.y - obstacle.point.y;

                        float distanceToBorder = obstacle.collider.bounds.extents.y - Mathf.Abs(distanceCenterHit);
                        Vector3 nextPoint;
                        if (distanceCenterHit < 0)
                        {
                            nextPoint = new Vector3(0, distanceToBorder+spacingY.y, 0);
                        }
                        else
                        {
                            nextPoint = new Vector3(0, -distanceToBorder-spacingY.y, 0);
                        }
                        points.Add(new ControlPoint(GetLastPoint() + nextPoint));

                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, 2*obstacle.collider.bounds.extents.z)+2*spacingZ));
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, points[GetLastPointIndex() -2].position.y, GetLastPoint().z)));
                    }
                    points.Add(new ControlPoint(new Vector3(endPos.x,GetLastPoint().y,endPos.z)));
                }
                else if(distanceZ < 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.back, Mathf.Abs(endPos.z) + Mathf.Abs(GetLastPoint().z));
                    if (obstacle.collider != null)
                    {
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, obstacle.point.z) + spacingZ));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHit = center.y - obstacle.point.y;

                        float distanceToBorder = Mathf.Abs(distanceCenterHit) + obstacle.collider.bounds.extents.y;

                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, distanceToBorder, 0) + spacingY));

                        points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, 2 * obstacle.collider.bounds.extents.z) - 2 * spacingZ));
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, points[GetLastPointIndex() - 2].position.y, GetLastPoint().z)));
                    }
                    points.Add(new ControlPoint(new Vector3(endPos.x, GetLastPoint().y, endPos.z)));
                }

                distanceY = endPos.y - GetLastPoint().y;

                if (distanceY > 0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.up,Mathf.Abs(endPos.y)-GetLastPoint().y);
                    if (obstacle.collider == null)
                    {
                        points.Add(new ControlPoint(endPos));
                    }
                    else
                    {
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, obstacle.point.y, GetLastPoint().z) - spacingY));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHit = center.z - obstacle.point.z;

                        float distanceToBorder = Mathf.Abs(distanceCenterHit) - obstacle.collider.bounds.extents.z;

                        if (distanceCenterHit<0)
                        {
                            points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, distanceToBorder) + spacingZ));
                        }
                        else
                        {
                            points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, distanceToBorder) - spacingZ));
                        }
                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 2 * obstacle.collider.bounds.extents.y, 0) + (2 * spacingY)));
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, endPos.z)));
                    }
                    points.Add(new ControlPoint(endPos));
                }
                else if(distanceY<0)
                {
                    var obstacle = CastRay(GetLastPoint(), Vector3.down, Mathf.Abs(endPos.y) - GetLastPoint().y);
                    if (obstacle.collider == null)
                    {
                        points.Add(new ControlPoint(endPos));
                    }
                    else
                    {
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, obstacle.point.y, GetLastPoint().z) - spacingY));

                        Vector3 center = obstacle.collider.bounds.center;
                        float distanceCenterHit = center.z - obstacle.point.z;

                        float distanceToBorder = Mathf.Abs(distanceCenterHit) - obstacle.collider.bounds.extents.z;

                        if (distanceCenterHit < 0)
                        {
                            points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, distanceToBorder) + spacingZ));
                        }
                        else
                        {
                            points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, distanceToBorder) - spacingZ));
                        }
                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 2 * obstacle.collider.bounds.extents.y, 0) + (2 * spacingY)));
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, endPos.z)));
                    }
                    points.Add(new ControlPoint(endPos));
                }

                wireGenerated = true;
                SetMesh(GenerateMeshUsingPrefab());
            }
            else
            {
                Debug.LogError("Start or End Point not found! Please add one to the scene!");
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
            Vector3 scaling = new Vector3(radius/100 / straightMesh.bounds.extents.x, radius/100 / straightMesh.bounds.extents.y, (end - beginning).magnitude / straightMesh.bounds.size.z);

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

                CombineInstance scaler = new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(radius/100 / curveMesh.bounds.extents.x, radius/100 / curveMesh.bounds.extents.y, 1)) };
                Mesh mesh2 = new Mesh();
                mesh2.CombineMeshes(new CombineInstance[] { scaler });

                float thisCurveSizePrev = Mathf.Min(curveSize, differencePrevious.magnitude / 2f);
                float thisCurveSizeNext = Mathf.Min(curveSize, differenceNext.magnitude / 2f);
                WireGenerator.WireMesh.DeformMeshUsingBezierCurve(mesh2, WireGenerator.WireMesh.Axis.Z, differencePrevious.normalized * thisCurveSizePrev, differencePrevious.normalized * thisCurveSizePrev / 2, differenceNext.normalized * thisCurveSizeNext / 2, differenceNext.normalized * thisCurveSizeNext);
                
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

        private void UpdateMaterials()
        {
            Material blueURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/blueURP.mat");
            Material redURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/redURP.mat");
            Material blackURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/blackURP.mat");
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/StartPoint.prefab").GetComponent<MeshRenderer>().material = blueURP;
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/EndPoint.prefab").GetComponent<MeshRenderer>().material = redURP;
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/Wire.prefab").GetComponent<MeshRenderer>().sharedMaterials[0] = blackURP;
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/Wire.prefab").GetComponent<MeshRenderer>().sharedMaterials[1] = blackURP;
            Material[] wireMaterials = this.GetComponent<MeshRenderer>().sharedMaterials;
            if (wireMaterial == null)
                wireMaterials[0] = blackURP;
            else
                wireMaterials[0] = wireMaterial;
            if (cornerMaterial == null)
                wireMaterials[1] = blackURP;
            else
                wireMaterials[1] = cornerMaterial;
            startPointGO.GetComponent<MeshRenderer>().sharedMaterial = blueURP;
            endPointGO.GetComponent<MeshRenderer>().sharedMaterial = redURP;
            this.GetComponent<MeshRenderer>().sharedMaterials = wireMaterials;
        }
    }

}
