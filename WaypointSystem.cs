using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaypointSystem : MonoBehaviour
{
    public Transform[] PatrollingNodes;
    Animator model;
    public float TimeToStayInPosition;
    float TimeToStay;
    NavMeshAgent agent;
    // Start is called before the first frame update
    void Start()
    {
        model = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        //foreach (var agent in agents)
        //{
            foreach (var node in PatrollingNodes)   //Patrol around the nodes defined in hierarchy
            {
                    switch (agent.destination != null && agent)
                    {
                        case true:
                            SwapNodeTimer(node, agent);
                        break;

                        case false:
                            TimeToStay = TimeToStayInPosition;
                            SwapNodeTimer(node, agent);
                        break;
                    }
                
            }
        //}
    }

    public void SwapNodeTimer(Transform node, NavMeshAgent ag) {
        //foreach (var model in models)
        //{
        if(ag.remainingDistance <= ag.stoppingDistance) {
                                model.SetFloat("speed" , -0.5f);
                                //Debug.Log("idle");
                                TimeToStay -= Time.deltaTime;
                                if(TimeToStay <= 0) {
                                    TimeToStay = TimeToStayInPosition;
                                ag.destination = node.position;
                                }
                            } else {

                                model.SetFloat("speed" , 0.5f);
                                //Debug.Log("walk");
                            }
        //}
    }
}
