using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization;

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
        [Range(0, 5)]
        public int steps;

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

        private void Awake()
        {
            Reset();
            FindShortestPath();
        }
        void Update()
        {
            if (wireGenerated)
            {
                if(steps <= 2)
                {
                    noWalls = true;
                }
                if (noWalls)
                {
                    if (steps != points.Count - 2)
                    {
                        Debug.Log("Step Count should be " + steps + " but is " + (points.Count - 2));
                        Reset();
                        FindShortestPath();
                    }
                }
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
                        if (noWalls)
                            FindShortestPath();
                        else
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
                        if (noWalls)
                            FindShortestPath();
                        else
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
            GenerateMesh();
        }
        public void FindStartEnd()
        {
            startPointGO = GameObject.FindGameObjectWithTag("startPoint");
            endPointGO = GameObject.FindGameObjectWithTag("endPoint");
        }

        public Vector3 NextPoint(Vector3 endPoint, Vector3 currentPosition, Vector3 direction, Vector3 rayDirection, int point)
        {
            float distance = Vector3.Distance(endPoint,currentPosition);
            for(float tick = 1f; tick <= distance;tick += 0.2f)
            {
                Vector3 offset = tick * direction;
                currentPosition = points[point].position +  offset;
                if(Physics.Raycast(transform.TransformPoint(currentPosition), direction, 0.2f))
                {
                    break;
                }
                else if (!Physics.Raycast(transform.TransformPoint(currentPosition), rayDirection, 1f))
                {
                    break;
                }
                else if (direction == Vector3.right || direction == Vector3.left)
                {
                    if (currentPosition.x >= endPoint.x)
                    {
                        currentPosition.x = endPoint.x;
                        break;
                    }
                }
                else if (direction == Vector3.up || direction == Vector3.down)
                {
                    if (currentPosition.y >= endPoint.y)
                    {
                        currentPosition.y = endPoint.y;
                        break;
                    }
                }
                else if (direction == Vector3.back || direction == Vector3.forward)
                {
                    Debug.Log(currentPosition);
                    if (Mathf.Round(currentPosition.z) >= Mathf.Round(endPoint.z))
                    {
                        Debug.Log("bla");
                        currentPosition.z = endPoint.z;
                        break;
                    }
                }
                Debug.DrawRay(currentPosition, rayDirection * 0.5f, Color.red, 10, true);
            }
            return new Vector3(
                //Mathf.Round
                (currentPosition.x),
                //Mathf.Round
                (currentPosition.y),
                //Mathf.Round
                (currentPosition.z));
        }

        public RaycastHit CastRay(Vector3 start, Vector3 direction)
        {
            Ray ray = new Ray(transform.TransformPoint(start), direction);
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
                points[points.Count - 1].position.y = points[points.Count - 4].position.y;
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
                points[points.Count - 1].position.y = points[points.Count - 4].position.y;
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
                points[points.Count - 1].position.z = points[points.Count - 4].position.z;
            }

            GetComponent<MeshFilter>().sharedMesh = null;
            Mesh newMesh = GenerateMeshUsingPrefab();
            SetMesh(newMesh);
        }

        public void FindShortestPath()
        {
            if (startPointGO != null && endPointGO != null)
            {
                if (wireGenerated)
                {
                    wireGenerated = false;
                    Reset();
                    FindShortestPath();
                }

                //POINT 0
                points[0].position = startPos;

                //POINT 1
                if(steps == 0)
                    points[1].position = endPos;
                //else
                //    points[1].position = new Vector3(
                //        endPos.x,
                //        points[0].position.y,
                //        points[0].position.z);

                if (steps >= 1)
                {
                    //POINT 1
                    points[1].position = new Vector3(
                        endPos.x,
                        startPos.y,
                        endPos.z);
                    //POINT 2
                    if (startPos.y != endPos.y)
                    {
                        points.Add(new ControlPoint(endPos));
                    }
                }
                if(steps >= 2)
                {
                    //POINT 1
                    points[1].position = new Vector3(
                        endPos.x,
                        startPos.y,
                        startPos.z);
                    //POINT 2
                    points[2].position = new Vector3(
                        endPos.x,
                        endPos.y,
                        startPos.z);
                    //POINT 3
                    if (startPos.z != endPos.z)
                    {
                        points.Add(new ControlPoint(endPos));
                    }
                }
                if(steps >= 3)
                {
                    //POINT 4
                    points.Add(new ControlPoint(GetLastPoint()));
                    points[3].position = points[2].position;
                    points[2].position = points[1].position;
                    points[1].position = new Vector3(
                        points[0].position.x + Vector3.Distance(points[0].position,points[1].position)/2,
                        points[1].position.y,
                        points[1].position.z);
                }
                if(steps >= 4)
                {
                    //POINT 5
                    points.Add(new ControlPoint(GetLastPoint()));
                    points[4].position = points[3].position;
                    points[3].position = new Vector3(
                        points[3].position.x,
                        points[2].position.y + Vector3.Distance(points[2].position, points[3].position) / 2,
                        points[3].position.z);
                }
                if(steps == 5)
                {
                    //POINT 6
                    points.Add(new ControlPoint(GetLastPoint()));
                    points[5].position = new Vector3(
                        points[4].position.x,
                        points[4].position.y,
                        points[4].position.z + Vector3.Distance(points[6].position, points[4].position) / 2);
                }
                wireGenerated = true;
                GetComponent<MeshFilter>().sharedMesh = null;
                Mesh newMesh = GenerateMeshUsingPrefab();
                SetMesh(newMesh);
            }
            else
            {
                FindStartEnd();
                FindPath();
            }
        }
        public void FindPath()
        {
            if (startPointGO != null && endPointGO != null)
            {
                if (wireGenerated)
                {
                    wireGenerated = false;
                    Reset();
                    FindPath();
                }

                startPos = startPointGO.transform.position;
                endPos = endPointGO.transform.position;
                //POINT 0
                points[0].position = startPointGO.transform.position;

                //Finding the nearest wall from the starting point and setting the 2nd point 0.2f away from that wall
                //POINT 1
                points[1].position = FindClosestPoint(points[0].position);

                //Checking if there is an obstacle to the right of the 2nd point
                var obstacle = CastRay(GetLastPoint(), Vector3.right);
                if(obstacle.collider != null ) 
                {
                    //If there is an obstacle, find a way above or underneath it
                    FoundObstacle(obstacle, Vector3.right);
                }
                
                //POINT 2
                //Create a new point as soon as there is no wall next to the wire
                if(endPos.x != GetLastPoint().x)
                    points.Add(
                        new ControlPoint(
                            NextPoint(
                                endPos,
                                points[1].position,
                                Vector3.right,
                                Vector3.forward,
                                GetLastPointIndex())));

                //Go in Z direction and check wether there's an obstacle in the way
                obstacle = CastRay(GetLastPoint(), Vector3.forward);
                if (obstacle.collider != null && obstacle.collider.gameObject.tag != "wall")
                {
                    //Debug.Log("Found an obstacle and it's called " + obstacle.collider.name);
                    FoundObstacle(obstacle, Vector3.forward);
                }
                //Create a new point when the wall next to the wire ends or there's another wall in the way
                //POINT 3
                if (endPos.x != GetLastPoint().x)
                {
                    points.Add(
                        new ControlPoint(
                            NextPoint(
                                CastRay(GetLastPoint(), Vector3.forward).point - 2*spacingZ,
                                GetLastPoint(), 
                                Vector3.forward,
                                Vector3.left,
                                GetLastPointIndex())));
                }


                //Repeat process on x axis for remaining distance
                obstacle = CastRay(GetLastPoint(), Vector3.right);
                if (obstacle.collider != null)
                {
                    FoundObstacle(obstacle, Vector3.right);
                }
                else
                {
                    if (endPos.x != GetLastPoint().x)
                    {
                        points.Add(
                        new ControlPoint(
                            NextPoint(
                                endPos,
                                GetLastPoint(),
                                Vector3.right,
                                Vector3.forward,
                                GetLastPointIndex())));
                    }

                }
                //POINT 4
                if (endPos.x != GetLastPoint().x)
                {
                    points.Add(
                    new ControlPoint(
                        new Vector3(
                            endPos.x,GetLastPoint().y,GetLastPoint().z)));
                }


                //Check if the endpoint is higher or lower than the last created point
                Vector3 upOrDown;
                if (endPos.y > startPos.y)
                {
                    upOrDown = Vector3.up;
                }
                else
                {
                    upOrDown = Vector3.down;
                }


                //Check if there is an obstacle on the remaining distance on y-axis
                obstacle = CastRay(GetLastPoint(), upOrDown);
                if (obstacle.collider != null)
                {
                    FoundObstacle(obstacle, upOrDown);

                    points.Add(
                        new ControlPoint(
                            GetLastPoint()));
                    points[points.Count - 1].position.y = endPos.y;

                    points.Add(
                        new ControlPoint(
                            GetLastPoint()));
                    points[points.Count - 1].position.z = endPos.z+0.5f;
                }
                else
                {
                    if (endPos.y != GetLastPoint().y)
                    {
                        points.Add(
                            new ControlPoint(
                                new Vector3(
                                    GetLastPoint().x,
                                    endPos.y,
                                    GetLastPoint().z)));
                    }
                    if(!noWalls)
                        points.Add(
                            new ControlPoint(
                                new Vector3(
                                    GetLastPoint().x,
                                    GetLastPoint().y,
                                    endPos.z+0.5f)));
                }

                if (steps <= 2)
                {
                    noWalls = true;
                    Reset();
                    FindShortestPath();
                }

                wireGenerated = true;
                GenerateMesh();
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
    }

}
