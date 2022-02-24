using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(BaseEnemy))]
public class HospitalHunterBoss : MonoBehaviour
{
    public Animator animator;
    public Transform head;
    public List<GameObject> waypointObjects = new List<GameObject>();
    public AudioClip stepAudioClip;
    [Tooltip("Random chance of changing waypoint direction after attacking player")]
    [Range(0f, 1f)]
    public float changeDirectionChanceAfterAttacking = .5f;
    public int attackAfterXWaypoints = 10;
    [Range(0f, 10f)]
    public float waitBeforeAttack = 2f;
    [Tooltip("When the player distance is closer than this number, the boss will attack no matter what. !!! Not the same as the regular pattern attack !!!")]
    public float proximityForAttack = 3f;
    [Tooltip("Cannot attack player, no matter what, when has already hit player no longer than this number, ago")]
    public float cooldownBetweenAttacks = 5f;
    public float bufferForNavMeshStoppingDistance = 2f;


    private BaseEnemy baseEnemy;
    private AudioSource audioSource;
    private Vector3[] waypoints;
    private int direction = 1;
    private int currentWaypointIndex = 0;
    private Vector3 nextWaypoint;
    private int waypointToAttackCountdown;
    private bool isHeadingTowardsPlayer = false;
    private NavMeshAgent agent;
    private GameObject player;
    private float currentWaitPeriodBeforeAttack = 0f;
    private float currentCooldownForAttack = 0f;
    private bool isAttacking = false;
    private bool isRunning = false;
    private bool runAudioPlaying = false;
    private float switchedDirectionTimer = 0f;
    private Quaternion headInitialRotation;



    void Awake() {
        baseEnemy = GetComponent<BaseEnemy>();
    }



    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player");
        waypoints = waypointObjects.Select(obj => obj.transform.position).ToArray();
        nextWaypoint = waypoints[currentWaypointIndex];
        waypointToAttackCountdown = attackAfterXWaypoints;
        headInitialRotation = head.rotation;

        if (agent == null) { throw new System.Exception(this.name + ": NavMeshAgent does not exist"); }
        if (player == null) { throw new System.Exception(this.name + ": Player game object not found"); }
        if (waypoints.Length == 0) { throw new System.Exception(this.name + ": No waypoints found"); }
        if (animator == null) { throw new System.Exception(this.name + ": Animator not set"); }
        if (audioSource == null) { throw new System.Exception(this.name + ": Audio Source not found"); }
        if (stepAudioClip == null) { throw new System.Exception(this.name + ": Step audio clip not set"); }
        if (baseEnemy == null) { throw new System.Exception(this.name + ": BaseEnemy script not found"); }

        baseEnemy.SetRagdoll(false);

        StartRunning();

        FindNextWaypoint();
        agent.SetDestination(nextWaypoint);

        SetupEvents();
    }


    void SetupEvents() {
        baseEnemy.onHit.AddListener(() => GotHit(baseEnemy.IncomingDamage, false));
        baseEnemy.onCritical.AddListener(() => GotHit(baseEnemy.IncomingDamage, true));
    }


    void Update()
    {
        if (agent == null || !agent.enabled || player == null) {
            return;
        }


        if (isAttacking) {
            return;
        }


        // Begin attack by proximity
        if (Vector3.Distance(player.transform.position, transform.position) < proximityForAttack && !isHeadingTowardsPlayer && currentCooldownForAttack <= 0f) {
            BeginAttackPlayer(0f);
        }

        head.rotation = headInitialRotation;

        if (isHeadingTowardsPlayer) {
            if (currentWaitPeriodBeforeAttack <= 0f) {
                agent.updateRotation = true;
                agent.SetDestination(player.transform.position);
                if (!isRunning) {
                    StartRunning();
                }

                if (Vector3.Distance(agent.destination, transform.position) <= agent.stoppingDistance + bufferForNavMeshStoppingDistance) {
                    Attack();
                }

            } else {
                if (isRunning) {
                    agent.updateRotation = false;
                    StopRunning();
                    transform.LookAt(player.transform.position);
                }
            }
            head.LookAt(player.transform.position);
        } else {
            agent.updateRotation = true;
            agent.SetDestination(nextWaypoint);

            if (Vector3.Distance(nextWaypoint, transform.position) <= agent.stoppingDistance + bufferForNavMeshStoppingDistance) {
                if (waypointToAttackCountdown <= 0) {
                    BeginAttackPlayer(waitBeforeAttack);
                } else {
                    FindNextWaypoint();
                    agent.SetDestination(nextWaypoint);
                }
            }
        }


        if (currentWaitPeriodBeforeAttack > 0f) { currentWaitPeriodBeforeAttack -= Time.deltaTime; }
        if (currentCooldownForAttack > 0f) { currentCooldownForAttack -= Time.deltaTime; }
        if (switchedDirectionTimer > 0f) { switchedDirectionTimer -= Time.deltaTime; }
    }


    void SwitchDirection() {
        switchedDirectionTimer = 3f;
        direction *= -1;
        FindNextWaypoint();
    }


    void FindNextWaypoint() {
        if (waypointToAttackCountdown > 0) {
            waypointToAttackCountdown -= 1;
        }

        currentWaypointIndex = (currentWaypointIndex + direction) % waypoints.Length;
        if (currentWaypointIndex < 0) { currentWaypointIndex = waypoints.Length + currentWaypointIndex; }

        nextWaypoint = waypoints[currentWaypointIndex];
    }


    void BeginAttackPlayer(float waitPeriod) {
        if (currentCooldownForAttack > 0f) {
            return;
        }

        isHeadingTowardsPlayer = true;
        waypointToAttackCountdown = attackAfterXWaypoints;
        currentWaitPeriodBeforeAttack = waitPeriod;
    }


    void Attack() {
        if (isAttacking) { throw new System.Exception("Attack called while already attacking"); }

        StartCoroutine(AttackAsync());
    }


    IEnumerator AttackAsync() {
        isAttacking = true;
        animator.SetBool("attack", true);
        StopRunning();

        yield return new WaitForSeconds(1f);

        FinishAttack();
    }


    void FinishAttack() {
        StopCoroutine("AttackAsync");
        StartRunning();
        animator.SetBool("attack", false);
        currentCooldownForAttack = cooldownBetweenAttacks;
        isHeadingTowardsPlayer = false;
        isAttacking = false;

        if (changeDirectionChanceAfterAttacking > 0f && Random.Range(0, 1f) <= changeDirectionChanceAfterAttacking) {
            SwitchDirection();
        } else {
            FindNextWaypoint();
        }
    }


    void StartRunning() {
        if (isRunning) return;

        animator.SetFloat("speed", 1);
        animator.SetBool("hunting", true);
        isRunning = true;
        StartCoroutine("RunAudio");
    }

    IEnumerator RunAudio() {
        if (!runAudioPlaying) {
            runAudioPlaying = true;
            while (isRunning) {
                yield return new WaitForSeconds(2f / agent.speed);
                audioSource.PlayOneShot(stepAudioClip);
            }
            runAudioPlaying = false;
        }
    }

    void StopRunning() {
        if (!isRunning) return;

        isRunning = false;
        runAudioPlaying = false;
        animator.SetFloat("speed", 0);
        animator.SetBool("hunting", false);
    }


    void GotHit(float damage, bool isCritical) {
        if (isHeadingTowardsPlayer && isCritical) {
            FinishAttack();
        } else {
            if (switchedDirectionTimer <= 0f && Random.Range(0f, 1f) < .5f) {
                SwitchDirection();
            }
        }

        baseEnemy.health -= damage;
        if (baseEnemy.health <= 0f) {
            Die();
        }
    }


    void Die() {
        StopRunning();
        StopCoroutine("AttackAsync");
        agent.enabled = false;
        animator.enabled = false;
        GetComponent<Collider>().enabled = false;


        // RAGDOLL HERE
        baseEnemy.Die();
    }
}
