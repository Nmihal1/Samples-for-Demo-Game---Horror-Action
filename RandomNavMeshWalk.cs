using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RandomNavMeshWalk : MonoBehaviour {

    public bool randomYRotation = true;
    public float minDistance = 5f;
    public float maxDistance = 25f;
    public float minStopTime = 0.5f;
    public float maxStopTime = 5f;
    public LayerMask walkableLayer;
    public Animator animator = null;
    public bool drawNavPath = false;

    private float counter = 0f;
    private NavMeshAgent agent = null;
    private LineRenderer lineRenderer = null;
    private bool isMoving = false;


    void Awake() {
        if (randomYRotation) {
            transform.Rotate(0f, Random.Range(0f, 360f), 0f);
        }
    }

    void Start() {
        RandomCounter();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update() {
        if (!agent.enabled) return;


        if (!isMoving) {
            CalculateCounter();
        }

        if (agent.remainingDistance <= agent.stoppingDistance) {
            if (animator) animator.SetFloat("speed", -0.5f);
            isMoving = false;
        } else {
            isMoving = true;
        }

        if (drawNavPath) {
            if (lineRenderer == null) {
                lineRenderer = gameObject.AddComponent(typeof(LineRenderer)) as LineRenderer;
                lineRenderer.startWidth = .2f;
                lineRenderer.endWidth = .2f;
            }

            Vector3[] path = agent.path.corners;
            if (path != null && path.Length > 1) {
                lineRenderer.positionCount = path.Length + 1;
                for (int i = 0; i < path.Length; i++) {
                    lineRenderer.SetPosition(i, path[i]);
                }
                lineRenderer.SetPosition(path.Length, agent.destination);
            }
        }
    }


    void CalculateCounter() {
        if (counter > 0f) {
            counter -= Time.deltaTime;
        }

        if (counter <= 0f) {
            agent.SetDestination(RandomNavSphere());
            RandomCounter();
        }
    }


    void RandomCounter() {
        counter = Random.Range(minStopTime, maxStopTime);
    }


    Vector3 RandomNavSphere() {
        isMoving = true;
        if (animator) animator.SetFloat("speed", 0.5f);

        float distance = Random.Range(minDistance, maxDistance);

        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * distance;

        randomDirection += transform.position;

        NavMeshHit navHit;

        NavMesh.SamplePosition (randomDirection, out navHit, distance, walkableLayer.value);

        return navHit.position;
    }

}
