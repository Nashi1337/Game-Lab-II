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
        /// <summary>
        /// Each point of the points list is derived from the ControlPoint class
        /// </summary>
        [Serializable]
        public class ControlPoint
        {
            public Vector3 position;
            public ControlPoint(Vector3 offset) {
                this.position = offset;
            }
        }

        //spacing vectors for obstacle circumvention for padding
        Vector3 spacingZ = new Vector3(0, 0, 0.19f);
        Vector3 spacingY = new Vector3(0, 0.19f, 0);
        Vector3 spacingX = new Vector3(0.19f, 0, 0);

        MeshFilter meshFilter;
        //Initialize the points list with two points for better visibility
        public List<ControlPoint> points = new List<ControlPoint>() { new ControlPoint(new Vector3(0,0,0)), new ControlPoint(new Vector3(1, 0, 0))};
        public float radius=6f;

        //Assumes mesh for straight parts is aligned with z-Axis
        [SerializeField] private Mesh straightMesh;
        [SerializeField] public float sizePerStraightMesh = 0.2f;
        [SerializeField] public int numberPerStraightSegment = 1;

        [SerializeField] private float curveSize = 0.1f;
        [SerializeField] private Mesh curveMesh;

        [SerializeField] StraightPartMeshGenerationMode straightPartMeshGenerationMode = StraightPartMeshGenerationMode.RoundAndScaleLast;

        //in case start and endpoint cannot be found, the user can manually assign them
        [SerializeField]
        public GameObject startPointGO;
        [SerializeField]
        public GameObject endPointGO;

        GameObject[] allStartPoints;
        GameObject[] allEndPoints;
        public Vector3 startPos;
        public Vector3 endPos;
        GameObject generateStartPoint;
        GameObject generateEndPoint;
        public bool wireGenerated=false;
        private bool pointMoved = false;
        public int wireIndex=0;

        /// <summary>
        /// When the script is loaded it searches the scene for all Wire objects and adjusts it's own index so that it is n+1.
        /// Then if start and end are children of the wire it sets their index to the same as the wire
        /// </summary>
        private void Awake()
        {
            Wire[] allWires = FindObjectsOfType<Wire>();
            foreach(Wire wire in allWires)
            {
                if (wire.wireIndex == wireIndex)
                {
                    if (wire != this)
                    {
                        wire.wireIndex = wireIndex;
                        wireIndex++;
                    }
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

        /// <summary>
        /// The Update Method checks every frame update, if the start or endpoint was moved.
        /// If yes, it immediately recalculates the path and generates the mesh.
        /// The result is a smooth user experience.
        /// Since we enabled "ExecuteInEditMode" it also works in the scene view.
        /// </summary>
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

        /// <summary>
        /// Our custom Reset method keeps fixed information and reinitializes other information.
        /// It also empties the Control Point list.
        /// </summary>
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

            meshFilter=GetComponent<MeshFilter>();

            SetMesh(GenerateMeshUsingPrefab());
        }

        /// <summary>
        /// Searches the scene for all start and end points, then keeps a reference to the ones with the same index as the wire.
        /// </summary>
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

        /// <summary>
        /// Custom RayCast method that shoots a ray only a given distance
        /// </summary>
        /// <param name="start">Origin of the ray</param>
        /// <param name="direction">Direction of where the ray is going</param>
        /// <param name="distance">max distance how far the ray should be cast</param>
        /// <returns></returns>
        public RaycastHit CastRay(Vector3 start, Vector3 direction, float distance)
        {
            Ray ray = new Ray(transform.TransformPoint(start), transform.TransformPoint(direction));
            Physics.Raycast(ray, out RaycastHit hitData, distance);
            return hitData;
        }
        /// <summary>
        /// Returns the position of the last point added to the list
        /// </summary>
        /// <returns>Vector3 of the last point</returns>
        Vector3 GetLastPoint()
        {
            return points[points.Count-1].position;
        }

        /// <summary>
        /// Returns the index of the last point added to the list
        /// </summary>
        /// <returns>int that is the length of the points list -1</returns>
        int GetLastPointIndex()
        {
            return points.Count-1;
        }

        /// <summary>
        /// Pathfinding function of the wire. Is executed automatically or manually by button press
        /// </summary>
        public void FindPath()
        {
            //reinitialize start and end
            FindStartEnd();

            //only execute if the wire has a start and end
            if (startPointGO != null && endPointGO != null)
            {
                //if the wire was already generated, reset it first before proceeding (prevents creating duplicate control points)
                if (wireGenerated)
                {
                    wireGenerated = false;
                    Reset();
                }

                startPos = startPointGO.transform.position;
                endPos = endPointGO.transform.position;

                //remaining distances between start and end
                float distanceX = endPos.x - startPos.x;
                float distanceY = endPos.y - startPos.y;
                float distanceZ = endPos.z - startPos.z;

                //the first point of the list is always inside the startPoint object
                points[0].position = startPointGO.transform.position;

                //first check the distance on Y axis. Is the end point above, below or on the same level as the start point?
                if (distanceY > 0)
                {
                    //check if there is an obstacle on the Y axis between start and end
                    var obstacle = CastRay(points[0].position, Vector3.up, endPos.y - points[0].position.y);
                    if (obstacle.collider == null)
                    {
                        //if not, just set the second point in the list which already exists to y=endPos.y
                        points[1].position = new Vector3(points[0].position.x, endPos.y, points[0].position.z);
                    }
                    else
                    {
                        //if there is an obstacle, set the second point shortly before the obstacle
                        points[1].position = new Vector3(points[0].position.x, obstacle.point.y, points[0].position.z) - spacingY;

                        Vector3 center = obstacle.collider.bounds.center;

                        //calculate the distance from the center of the obstacle to the point where the ray hit
                        float distanceCenterHit = center.z - obstacle.point.z;

                        //calculate the distance to the next border
                        float distanceToBorder = Mathf.Abs(distanceCenterHit) - obstacle.collider.bounds.extents.z;

                        //if the distance is negative, the next point will be created in front of the obstacle. Otherwise in the front
                        if (distanceCenterHit < 0)
                        {
                            points.Add(new ControlPoint(GetLastPoint() - new Vector3(0, 0, distanceToBorder) + spacingZ));
                        }
                        else
                        {
                            points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 0, distanceToBorder) - spacingZ));
                        }
                        //the next point will be created above the obstacle
                        points.Add(new ControlPoint(GetLastPoint() + new Vector3(0, 2 * obstacle.collider.bounds.extents.y, 0) + (2 * spacingY)));

                        //this point then hast the same z-coordinate than two points prior, before the obstacle
                        points.Add(new ControlPoint(new Vector3(GetLastPoint().x, GetLastPoint().y, points[GetLastPointIndex() - 2].position.z)));
                    }
                }
                else
                {
                    //if there is no difference on y axis, delete last point. then new points will be created
                    points.Remove(points[GetLastPointIndex()]);
                }
                //repeat same process for X-Axis
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
                //if the end point is to the left of the start point, this will be executed
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

                //next comes the z-axis
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
                //update the remaining distance on the y-axis
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
                //when all points are created, the mesh will be generated
                SetMesh(GenerateMeshUsingPrefab());
            }
            else
            {
                Debug.LogError("Start or End Point not found! Please add one to the scene!");
            }
        }

        /// <summary>
        /// sets the mesh of the wire object to the generated mesh
        /// </summary>
        /// <param name="mesh"></param>
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

        /// <summary>
        /// returns the position of a given point from the list
        /// </summary>
        /// <param name="i">index of desired point</param>
        /// <returns>position of point from list</returns>
        public Vector3 GetPosition(int i) {
            return (transform.position + points[i].position);
        }

        /// <summary>
        /// updates the position of a given point
        /// </summary>
        /// <param name="i">index of desired point</param>
        /// <param name="position">new position of given point</param>
        public void SetPosition(int i, Vector3 position)
        {
            points[i].position = position - transform.position;
        }

        /// <summary>
        /// in case the active render pipeline differs from the default, URP materials are provided
        /// </summary>
        private void UpdateMaterials()
        {
            Material blueURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/blueURP.mat");
            Material redURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/redURP.mat");
            Material blackURP = AssetDatabase.LoadAssetAtPath<Material>("Assets/WireGenerator/Materials/blackURP.mat");
            //AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/StartPoint.prefab").GetComponent<MeshRenderer>().material = blueURP;
            //AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/EndPoint.prefab").GetComponent<MeshRenderer>().material = redURP;
            //AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/Wire.prefab").GetComponent<MeshRenderer>().sharedMaterials[0] = blackURP;
            //AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WireGenerator/Prefabs/Wire.prefab").GetComponent<MeshRenderer>().sharedMaterials[1] = blackURP;
            Material[] wireMaterials = this.GetComponent<MeshRenderer>().sharedMaterials;

            wireMaterials[0] = blackURP;
            wireMaterials[1] = blackURP;

            startPointGO.GetComponent<MeshRenderer>().sharedMaterial = blueURP;
            endPointGO.GetComponent<MeshRenderer>().sharedMaterial = redURP;
            this.GetComponent<MeshRenderer>().sharedMaterials = wireMaterials;
        }
    }

}
