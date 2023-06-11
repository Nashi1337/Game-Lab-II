using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

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
            //public bool useAnchor;
            //public Transform anchorTransform;

            public ControlPoint(Vector3 offset) {
                this.position = offset;
                //useAnchor = false;
                //anchorTransform = null;
            }
        }

        public Vector3 startPoint;
        public Vector3 endPoint;


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
        [SerializeField] public float sizePerStraightMesh=0.2f;
        [SerializeField] public int numberPerStraightSegment = 1;

        [SerializeField] private float curveSize = 0.1f;
        [SerializeField] private Mesh curveMesh;

        [SerializeField]StraightPartMeshGenerationMode straightPartMeshGenerationMode = StraightPartMeshGenerationMode.RoundAndScaleLast;
        public GameObject cornerPart;
        public GameObject pipePart;
        [SerializeField]
        public GameObject pipeParent;
        List<GameObject> pipeParts = new List<GameObject>();
        [SerializeField]
        public GameObject startPointGO;
        [SerializeField]
        public GameObject endPointGO;
        public Vector3 startPos;
        public Vector3 endPos;
        public bool wireGenerated=false;
        private bool pointMoved = false;

        void Update()
        {
            //if(endPointGO != null && startPointGO != null)
            //{
            //    if (endPointGO.transform.position != endPos)
            //    {
            //        //Reset();
            //        FindPath();
            //    }
            //    if(startPointGO.transform.position != startPos)
            //    {
            //        //Reset();
            //        FindPath();
            //    }
            //}
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
            wireGenerated = false;
            pipeParent = this.gameObject;
            points = new List<ControlPoint>() { new ControlPoint(new Vector3(0, 0, 0)), new ControlPoint(new Vector3(1, 0, 0)) };

            mesh = new Mesh{name = "Wire"};
            meshFilter=GetComponent<MeshFilter>();

            float tension = weight * lengthFactor;
            float sag = tension + weight + sagOffset;
            float minimum = CalculateWireSag(sag, 0.5f);
            //GenerateMesh();
        }
        public void FindStartEnd()
        {
            //startPoint = GameObject.FindGameObjectWithTag("startPoint").transform.position;
            //endPoint = GameObject.FindGameObjectWithTag("endPoint").transform.position;
            startPointGO = GameObject.FindGameObjectWithTag("startPoint");
            endPointGO = GameObject.FindGameObjectWithTag("endPoint");
            //startPos = startPointGO.transform.position;
            //endPos = endPointGO.transform.position;
        }

        public Vector3 NextPoint(float endPoint, Vector3 currentPosition, Vector3 direction, Vector3 rayDirection, int point)
        {
            for(float tick = 0.4f; tick <= endPoint;tick += 0.2f)
            {
                Vector3 offset = tick * direction;
                currentPosition = points[point].position +  offset;
                if(Physics.Raycast(transform.TransformPoint(currentPosition), direction, 0.2f))
                {
                    break;
                }
                else if (!Physics.Raycast(transform.TransformPoint(currentPosition), rayDirection, 0.5f))
                {
                    break;
                }
                Debug.DrawRay(transform.TransformPoint(currentPosition), rayDirection * 0.5f, UnityEngine.Color.red, 10, true);
            }
            return currentPosition;
        }

        public RaycastHit CastRay(Vector3 start, Vector3 direction)
        {
            Ray ray = new Ray(transform.TransformPoint(start), direction);
            Physics.Raycast(ray, out RaycastHit hitData, Mathf.Infinity);
            return hitData;
        }
        public Vector3 FindClosestPoint(Vector3 origin)
        {
            Vector3 closestPoint = Vector3.positiveInfinity;
            Vector3[] distances = new Vector3[6];
            Ray ray = new Ray(origin, Vector3.up);
            Physics.Raycast(ray, out RaycastHit hitUp, Mathf.Infinity);
            if(hitUp.transform != null) if (hitUp.transform.tag == "wall") distances[0] = hitUp.point - origin - spacingY;

            ray = new Ray(transform.TransformPoint(origin), Vector3.down);
            Physics.Raycast(ray, out RaycastHit hitDown, Mathf.Infinity);
            if (hitDown.transform != null) if (hitDown.transform.tag == "wall") distances[1] = origin - hitDown.point + spacingY;

            ray = new Ray(transform.TransformPoint(origin), Vector3.left);
            Physics.Raycast(ray, out RaycastHit hitLeft, Mathf.Infinity);
            if (hitLeft.transform != null) if (hitLeft.transform.tag == "wall") distances[2] = origin - hitLeft.point - spacingX;

            ray = new Ray(transform.TransformPoint(origin), Vector3.right);
            Physics.Raycast(ray, out RaycastHit hitRight, Mathf.Infinity);
            if (hitRight.transform != null) if (hitRight.transform.tag == "wall") distances[3] = hitRight.point - origin + spacingX;

            ray = new Ray(transform.TransformPoint(origin), Vector3.back);
            Physics.Raycast(ray, out RaycastHit hitBack, Mathf.Infinity);
            if (hitBack.transform != null) if (hitBack.transform.tag == "wall") distances[4] = origin - hitBack.point - spacingZ;

            ray = new Ray(transform.TransformPoint(origin), Vector3.forward);
            Physics.Raycast(ray, out RaycastHit hitForward, Mathf.Infinity);
            if (hitForward.transform != null) if (hitForward.transform.tag == "wall") distances[5] = hitForward.point - origin + spacingZ;

            foreach (Vector3 vector in distances)
            {
                //Debug.Log(vector + " " + closestPoint);
                closestPoint = (vector.magnitude < closestPoint.magnitude) ? vector : closestPoint;
            }

            return closestPoint;
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

        }

        public void FindPath()
        {
            Collider[] wall;
            Debug.Log(startPointGO + ", " + endPointGO + ", " + wireGenerated);
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
                //startPoint = points[0].anchorTransform.position;
                //endPoint = points[1].anchorTransform.position;
                //this.transform.position = startPoint;
                points[0].position = startPointGO.transform.position;
                //Finding the nearest wall from the starting point and setting the 2nd point 0.2f away from that wall
                points[1].position = FindClosestPoint(points[0].position);
                wall = Physics.OverlapSphere(points[0].position, 1f);
                //Debug.Log(points[1].position);
                //points[1].position = wall[0].ClosestPoint(points[0].position) + spacingZ;
                //Debug.Log(points[1].position);

                //Checking if there is an obstacle to the right of the 2nd point
                var obstacle = CastRay(GetLastPoint(), Vector3.right);
                if(obstacle.collider != null ) 
                {
                    //If there is an obstacle, find a way above or underneath it
                    FoundObstacle(obstacle, Vector3.right);
                }
                
                //Create a new point as soon as there is no wall next to the wire
                points.Add(
                    new ControlPoint(
                        NextPoint(endPoint.x,points[1].position,
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
                points.Add(
                    new ControlPoint(
                        NextPoint(
                            CastRay(GetLastPoint(), Vector3.forward).point.z - 2*spacingZ.z,
                                GetLastPoint(), 
                                Vector3.forward,
                                Vector3.left,
                                GetLastPointIndex())));

                //Repeat process on x axis for remaining distance
                obstacle = CastRay(GetLastPoint(), Vector3.right);
                if (obstacle.collider != null)
                {
                    FoundObstacle(obstacle, Vector3.right);
                }
                else
                {
                    points.Add(
                        new ControlPoint(
                            NextPoint(
                                CastRay(GetLastPoint(),Vector3.right).point.x - 2 * spacingX.x,
                                    GetLastPoint(),
                                    Vector3.right,
                                    Vector3.forward,
                                    GetLastPointIndex())));
                }
                points.Add(
                    new ControlPoint(
                        new Vector3(
                            endPoint.x,GetLastPoint().y,GetLastPoint().z)));

                //Check if the endpoint is higher or lower than the last created point
                Vector3 upOrDown;
                if (endPoint.y > startPoint.y)
                {
                    upOrDown = new Vector3(0, 1, 0);
                }
                else
                {
                    upOrDown = new Vector3(0, -1, 0);
                }


                //Check if there is an obstacle on the remaining distance on y-axis
                obstacle = CastRay(GetLastPoint(), upOrDown);
                if (obstacle.collider != null)
                {
                    FoundObstacle(obstacle, upOrDown);

                    points.Add(
                        new ControlPoint(
                            GetLastPoint()));
                    points[points.Count - 1].position.y = endPoint.y;

                    points.Add(
                        new ControlPoint(
                            GetLastPoint()));
                    points[points.Count - 1].position.z = endPoint.z+0.5f;
                }
                else
                {
                    points.Add(
                        new ControlPoint(
                            new Vector3(
                                GetLastPoint().x,
                                endPoint.y,
                                GetLastPoint().z)));
                    points.Add(
                        new ControlPoint(
                            new Vector3(
                                GetLastPoint().x,
                                GetLastPoint().y,
                                endPoint.z+0.5f)));
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
        public Vector3 GetPosition(int i) {
            return (
                //points[i].useAnchor && points[i].anchorTransform ?
                //points[i].anchorTransform :
                //transform)
                //.position + points[i].offset;
                transform.position + points[i].position);
        }
        public void SetPosition(int i, Vector3 position)
        {
            points[i].position = position -
                //(points[i].useAnchor && points[i].anchorTransform ? points[i].anchorTransform : transform).position;
                transform.position;
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
                    tempVertices[i + corners * controlPointId] = (offsetCircle * startpointVertice) + 
                        //(points[controlPointId].useAnchor ? GetPosition(controlPointId) - transform.position :
                        points[controlPointId].position
                        //)
                        ;
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
            if (startPoint != null && endPoint != null)
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

        public void SetMesh(Mesh mesh)
        {
            meshFilter.sharedMesh = mesh;
        }
        public Mesh GenerateMeshUsingPrefab()
        {
            //Assumes mesh for straight parts is aligned with z-Axis
            //Curve Parts(90°)
            //Assumes Open end point toward +Z and +Y and quadratic bounds for curves
            CombineInstance[] combineInstances= new CombineInstance[2];

            combineInstances[0] = new CombineInstance() { mesh = GenerateStraightPartMesh(), transform=Matrix4x4.identity };
            combineInstances[1] = new CombineInstance() { mesh = GenerateCurvePartMesh(), transform=Matrix4x4.identity};


            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combineInstances,false);
            return mesh;
        }

        public Mesh GenerateStraightPartMesh()
        {
            Mesh mesh = new Mesh();
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            combineInstances.AddRange(GenerateStraightParts());
            mesh.CombineMeshes(combineInstances.ToArray());
            return mesh;
        }

        public Mesh GenerateCurvePartMesh()
        {
            Mesh mesh = new Mesh();
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            combineInstances.AddRange(GenerateCurveParts());
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
                Vector3 difference= GetPosition(i + 1) - GetPosition(i);

                Vector3 beginning = GetPosition(i) - transform.position + curveSize*difference.normalized;
                Vector3 end = GetPosition(i + 1) - transform.position - curveSize*difference.normalized;



                int numberOfSectionDivisions;
                switch(straightPartMeshGenerationMode)
                {
                    case StraightPartMeshGenerationMode.FixedNumber:
                        numberOfSectionDivisions = numberPerStraightSegment;
                        break;
                    case StraightPartMeshGenerationMode.RoundAndScaleAll:
                        numberOfSectionDivisions = Math.Max(1,Mathf.RoundToInt((end-beginning).magnitude/sizePerStraightMesh));
                        break;
                    case StraightPartMeshGenerationMode.RoundAndScaleLast:
                        numberOfSectionDivisions = Math.Max(1, Mathf.RoundToInt((end - beginning).magnitude / sizePerStraightMesh)) - 1;
                        end = beginning+numberOfSectionDivisions*sizePerStraightMesh*(end-beginning).normalized;
                        break;
                    default: throw new UnexpectedEnumValueException<StraightPartMeshGenerationMode>(straightPartMeshGenerationMode);
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

                Vector3 partBeginning = sectionBeginning + partSize*i;
                Vector3 partEnd = sectionBeginning + partSize * (i+1);
                combineInstances.Add(GenerateStraightPart(partBeginning, partEnd));
            }
            return combineInstances;
        }

        private CombineInstance GenerateStraightPart(Vector3 beginning, Vector3 end)
        {
            Vector3 difference = end-beginning;

            Vector3 translation = beginning+difference/2;
            Quaternion rotation = Quaternion.LookRotation(difference);
            Vector3 scaling = new Vector3(radius / straightMesh.bounds.extents.x, radius / straightMesh.bounds.extents.y, (end-beginning).magnitude / straightMesh.bounds.size.z);

            Matrix4x4 transformMatrix = Matrix4x4.TRS(translation, rotation, scaling);
            return new CombineInstance() { mesh = straightMesh, transform=transformMatrix };
        }

        private List<CombineInstance> GenerateCurveParts()
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();

            //Curve Parts(90°)
            //Assumes Open end point toward +Z and +Y and quadratic bounds
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector3 differenceNext = -GetPosition(i) + GetPosition(i + 1);
                Vector3 differencePrevious = -GetPosition(i) + GetPosition(i - 1);

                Vector3 position = GetPosition(i) - transform.position;
                Quaternion rotation = Quaternion.LookRotation(differenceNext, differencePrevious);
                Vector3 scale = new Vector3(radius / curveMesh.bounds.extents.x, curveSize / curveMesh.bounds.extents.y, curveSize / curveMesh.bounds.extents.z) ;

                Matrix4x4 transformMatrix = Matrix4x4.TRS(position, rotation, scale);

                combineInstances.Add(new CombineInstance
                {
                    mesh = curveMesh,
                    transform = transformMatrix
                }); ;
            }

            return combineInstances;
        }
        public void CreatePipe()
        {
            for(int i = 1; i < points.Count-1; i++)
            {
                if(pipeParts.Count == 0)
                {
                    Debug.Log("Pipe Parts is empty");
                }
                Vector3 rotation = CalculateRotation(points[i].position, points[i - 1].position);
                GameObject part = Instantiate(pipePart, transform.TransformPoint((points[i - 1].position + points[i].position) / 2), Quaternion.Euler(rotation*90), pipeParent.transform);
                if(part == null)
                {
                    Debug.Log("part is empty");
                }
                part.transform.localScale = (scaleFactor(points[i].position, points[i - 1].position));
                pipeParts.Add(part);
                rotation = CalculateRotation(points[i-1].position, points[i].position, points[i + 1].position);
                part = Instantiate(cornerPart, transform.TransformPoint(points[i].position), Quaternion.Euler(rotation ), pipeParent.transform);
                pipeParts.Add(part);
            }
            GameObject lastPart = Instantiate(pipePart, transform.TransformPoint((points[points.Count-2].position + GetLastPoint()) / 2), Quaternion.Euler(90, 0, 0), pipeParent.transform);
            lastPart.transform.localScale = scaleFactor(points[GetLastPointIndex()].position, points[GetLastPointIndex() - 1].position);
            pipeParts.Add(lastPart);
        }
        public void DeletePipe()
        {
            foreach (GameObject pipePart in pipeParts)
            {
                DestroyImmediate(pipePart);
            }
        }

        public Vector3 CalculateRotation(Vector3 point1, Vector3 point2)
        {
            Vector3 distance = point1 - point2;
            Vector3 rotation = new Vector3(0,0,0);
            if(distance.x > 0)
            {
                rotation = new Vector3(0, 0, 1);
            }
            else if(distance.y > 0)
            {
                rotation = new Vector3(0, 1, 0);
            }
            else if(distance.z > 0)
            {
                rotation = new Vector3 (1, 0, 0);
            }
            else if(distance.x < 0)
            {
                rotation = new Vector3(0, 0, 1);
            }
            else if(distance.y < 0)
            {
                rotation = new Vector3(0, 0, 0);
            }
            else if(distance.z < 0)
            {
                rotation = new Vector3(1, 0, 0);
            }

            return rotation;
        }
        public Vector3 CalculateRotation(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            Vector3 distance1 = point2 - point1;
            Vector3 distance2 = point3 - point2;
            Vector3 rotation = new Vector3(0, 0, 0);
            if (distance1.x > 0)
            {
                if(distance2.y > 0)
                {
                    rotation = new Vector3(0, 0, 180);
                }
                else if(distance2.y < 0)
                {
                    rotation = new Vector3(0, 0, 270);
                }
                else if(distance2.z > 0)
                {
                    rotation = new Vector3(90, 0, 180);
                }
                else if(distance2.z < 0)
                {
                    rotation = new Vector3(-90, 0, 180);
                }
            }
            else if (distance1.y > 0)
            {
                if (distance2.x > 0)
                {
                    rotation = new Vector3(0, 0, 0);
                }
                else if (distance2.x < 0)
                {
                    rotation = new Vector3(0, 180, 0);
                }
                else if (distance2.z > 0)
                {
                    rotation = new Vector3(0, 270, 0);
                }
                else if (distance2.z < 0)
                {
                    rotation = new Vector3(0, 90, 0);
                }
            }
            else if (distance1.y < 0)
            {
                if (distance2.x > 0)
                {
                    rotation = new Vector3(0, 0, 90);
                }
                else if (distance2.x < 0)
                {
                    rotation = new Vector3(0, 180, 0);
                }
                else if (distance2.z > 0)
                {
                    rotation = new Vector3(0, 270, 0);
                }
                else if (distance2.z < 0)
                {
                    rotation = new Vector3(0, 90, 90);
                }
            }
            else if (distance1.z > 0)
            {
                if (distance2.x > 0)
                {
                    rotation = new Vector3(90, 0, 0);
                }
                else if (distance2.x < 0)
                {
                    rotation = new Vector3(90, 90, 0);
                }
                else if (distance2.y > 0)
                {
                    rotation = new Vector3(180, 90, 0);
                }
                else if (distance2.y < 0)
                {
                    rotation = new Vector3(0, 90, 0);
                }
            }
            else if (distance1.z < 0)
            {
                if (distance2.x > 0)
                {
                    rotation = new Vector3(90, 270, 0);
                }
                else if (distance2.x < 0)
                {
                    rotation = new Vector3(270, 270, 0);
                }
                else if (distance2.y > 0)
                {
                    rotation = new Vector3(180, 270, 0);
                }
                else if (distance2.y < 0)
                {
                    rotation = new Vector3(0, 270, 0);
                }
            }
            return rotation;
        }

        public Vector3 scaleFactor(Vector3 point1, Vector3 point2)
        {
            Vector3 distance = point1 - point2;
            if (distance.x > 0)
            {
                return new Vector3(0.2f, distance.x / 2, 0.2f);
            }
            else if (distance.y > 0)
            {
                return new Vector3(0.2f, distance.y / 2, 0.2f);
            }
            else if (distance.z > 0)
            {
                return new Vector3(0.2f, distance.z / 2, 0.2f);
            }
            else if (distance.x < 0)
            {
                return new Vector3(0.2f, Mathf.Abs(distance.x) / 2, 0.2f);
            }
            else if (distance.y < 0)
            {
                return new Vector3(0.2f, Mathf.Abs(distance.y) / 2, 0.2f);
            }
            else if (distance.z < 0)
            {
                return new Vector3(0.2f, Mathf.Abs(distance.z) / 2, 0.2f);
            }
            else return new Vector3(0.2f, 0.2f, 0.2f);
        }
        public void UpdatePoints()
        {
            if (startPoint != null && endPoint != null)
            {
                if (startPointGO.transform.hasChanged)
                {
                    Debug.Log("startpoint has changed");
                }
                else if (endPointGO.transform.hasChanged)
                {
                    Debug.Log("endpoint has changed");
                }
            FindPath();
            }
        }
    }

}
