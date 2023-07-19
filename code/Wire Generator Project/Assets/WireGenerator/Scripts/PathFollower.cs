using System.Collections.Generic;
using UnityEngine;
using static WireGenerator.Wire;

namespace WireGenerator
{
    public class PathFollower : MonoBehaviour
    {
        List<ControlPoint> points;
        public GameObject agent;
        GameObject walker;
        public float moveSpeed=5f;
        public bool playAnimation;

        int currentStep;

        public enum FollowerMode
        {
            Repeat,
            PingPong
        }

        public FollowerMode mode;

        private int moveDirection = 1;
        private bool useOwnAgent;
        private void Start()
        {
            points = GetComponent<Wire>().points;
            if (agent == null)
                walker = GameObject.CreatePrimitive(PrimitiveType.Sphere); 
            else
                walker = Instantiate(agent);
            walker.transform.position = points[0].position;
            walker.transform.rotation = Quaternion.identity;
            walker.transform.SetParent(this.gameObject.transform);
        }

        private void Update()
        {
            walker.SetActive(playAnimation);
            if (playAnimation)
            {
                if (agent != null&&!useOwnAgent)
                {
                    useOwnAgent = true;
                    Destroy(walker);
                    walker = Instantiate(agent);
                }
                if (mode == FollowerMode.Repeat)
                {
                    if (points.Count == 0)
                        return;

                    // Move towards the current step
                    walker.transform.position = Vector3.MoveTowards(walker.transform.position, points[currentStep].position, moveSpeed * Time.deltaTime);

                    // Check if the walker has reached the current step
                    if (walker.transform.position == points[currentStep].position)
                    {
                        // Move to the next step
                        currentStep++;

                        // Check if we've reached the end of the step list
                        if (currentStep >= points.Count)
                        {
                            // If we reached the end, loop back to the first step
                            currentStep = 0;
                            walker.transform.position = points[currentStep].position;
                        }
                    }
                }
                if (mode == FollowerMode.PingPong)
                {
                    if (points.Count == 0)
                        return;

                    walker.transform.position = Vector3.MoveTowards(walker.transform.position, points[currentStep].position, moveSpeed * Time.deltaTime);
                    if (walker.transform.position == points[currentStep].position)
                    {
                        currentStep += moveDirection;
                        if (currentStep >= points.Count - 1 || currentStep < 0)
                        {
                            moveDirection *= -1;
                            currentStep = Mathf.Clamp(currentStep, 0, points.Count - 1);
                        }
                    }
                
                }
            }
        }
    }
}
