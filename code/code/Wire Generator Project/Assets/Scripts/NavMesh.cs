using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NavMesh : MonoBehaviour
{
    [SerializeField]
    private Transform movePositionTransform;
    private LineRenderer line;
    private Rigidbody rb;
    private List<Vector3> point;

    private NavMeshAgent navMeshAgent;
    // Start is called before the first frame update
    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        line = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonUp(0))
        //{
        //    RaycastHit hit;
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    if(Physics.Raycast(ray, out hit))
        //    {
        //        navMeshAgent.SetDestination(hit.point);
        //    }
        //}
        navMeshAgent.destination = movePositionTransform.position;
        DisplayLineDestination();
    }

    private void DisplayLineDestination()
    {
        if (navMeshAgent.path.corners.Length < 2) return;
        int i = 1;
        while (i < navMeshAgent.path.corners.Length)
        {
            line.positionCount = navMeshAgent.path.corners.Length;
            point = navMeshAgent.path.corners.ToList();
            for(int j = 0; j < point.Count; j++)
            {
                line.SetPosition(j, point[j]);
            }

            i++;
        }
    }
}
