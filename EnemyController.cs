using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using SensorToolkit;


[RequireComponent(typeof(FOVCollider))]
[RequireComponent(typeof(TriggerSensor))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(RangeSensor))]

public class EnemyController : MonoBehaviour
{
    public static HashSet<EnemyController> enemiesHuntingPlayer = new HashSet<EnemyController>();

    public bool canHear = true;
    Animator EnemyAnimator;
    NavMeshAgent Enemy;
    [HideInInspector] public Transform TargetToSeek;
    [HideInInspector] public bool IsHunting;
    [HideInInspector] public Transform SoundHeard;
    float DistanceToTarget;
    public AudioSource EnemyAudio;
    public AudioClip WalkingClip;
    public AudioClip RunningClip;
    bool AudioIsPlaying;
    public bool canOpenDoors = false;
    public bool canHitDoors = false;
    public float hitTimer = 2f;
    public bool isHittingDoor { get; private set; } = false;
    private float currentHitTimer = 0f;
    public LayerMask doorLayerMask;
    public float playerCamouflagedDetectionDistance = 5f;

    public float BurnTime = 5f;
    [HideInInspector]public float fireTimer;

    // private List<Activatable> activatables = new List<Activatable>();
    // private List<PickupItemAction> pickupItemActions = new List<PickupItemAction>();


    [Header("Attributes")]
    public float HealthPoints;
    public float Damage;
    [HideInInspector]public float dmg;
    [HideInInspector]public bool onFire = false;
    GameObject fireClone = null;
    public float WalkSpeed;
    float Wspeed;
    public float RunSpeed;
    float Rspeed;

    [Header("Hunger System")]
    public float HungerLossPerTick = 1f;
    public float HungerLossRate = 30f;
    public float MaxHunger = 100f;
    public float CurrentHunger;
    public bool HasHunger;
    bool IsStarving = false;

    [Header("Sound System")]
    [Range(0f, 1f)]
    public float AudioSourceVolume = .8f;
    public AudioSource IdleSoundController;
    public AudioClip IdleClip;
    public AudioSource Screams;
    public AudioClip[] ScreamClip;


    // Start is called before the first frame update
    void Awake() {
        Collider[] Hitboxes = GetComponentsInChildren<Collider>();
        foreach (var col in Hitboxes)
        {
            if(col.tag == "Untagged") {
                col.tag = "Hitbox";
            }
        }
    }

    void Start()
    {
        SetRagdollState(false);
        dmg = Damage;
        Wspeed = WalkSpeed;
        Rspeed = RunSpeed;

        if(HasHunger) {
            InvokeRepeating("Hungry", 0f, HungerLossRate);
        }

        CurrentHunger = MaxHunger;
        if (IdleSoundController != null && IdleClip != null) {
            IdleSoundController.volume = AudioSourceVolume;
            IdleSoundController.clip = IdleClip;
            IdleSoundController.Play();
        }
        fireTimer = BurnTime;
        EnemyAnimator = gameObject.GetComponentInChildren<Animator>();
        Enemy = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if(onFire) { //calls the burning function once it has been set on fire
            Burning(null);
        }

        // If is hitting door, do nothing else
        if (isHittingDoor) return;

        if (canOpenDoors || canHitDoors) {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 2f, doorLayerMask)) {
                if (hit.transform.tag.Equals("Activatable")) {
                    DoorAction door = hit.transform.GetComponent<DoorAction>();
                    if (door != null && door.isHittableByEnemy && !door.IsCurrentlyOpen()) {
                        if (canOpenDoors && hit.transform.GetComponent<Activatable>().enabled) {
                            door.Toggle();
                        } else if (!door.isBeingHitByEnemy) {
                            StartCoroutine(BangOnDoor(door));
                        }
                    }
                }
            }
        }

        if (!Enemy.enabled || !Enemy.isOnNavMesh) return;

        if(TargetToSeek) {
            Enemy.destination = TargetToSeek.position;
            switch (EnemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Run")) { //checks if run animation exists
            case true:
                    Enemy.speed = Rspeed;
            break;

            case false:
                    Enemy.speed = Wspeed;
            break;
        }
        }

        if(!IsHunting && SoundHeard && canHear) {
            Enemy.destination = SoundHeard.position;
            SoundHeard = null;
        }

        if(EnemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("GetHit") && EnemyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f) { //hit recovery
            EnemyAnimator.SetBool("IsHit", false);
            Enemy.isStopped = false;
            if (EnemyAudio != null) EnemyAudio.UnPause();
            if (IdleSoundController != null) IdleSoundController.Pause();
        }


        switch (!isHittingDoor && Enemy.remainingDistance <= Enemy.stoppingDistance)
        {
            case false:
                if(!AudioIsPlaying) {
                    if (EnemyAudio != null) {
                        EnemyAudio.volume = AudioSourceVolume;
                        if (WalkingClip != null) EnemyAudio.clip = WalkingClip;
                        if (IsHunting && RunningClip != null) {
                            EnemyAudio.clip = RunningClip;
                        }
                        EnemyAudio.loop = true;
                        EnemyAudio.Play();
                    }
                    AudioIsPlaying = true;
                }

                EnemyAnimator.SetFloat("speed", 0.5f);
                EnemyAnimator.SetBool("attack", false);
                break;

            case true:
                if (AudioIsPlaying) {
                    if (EnemyAudio != null) EnemyAudio.Stop();
                    AudioIsPlaying = false;
                }
                EnemyAnimator.SetFloat("speed", -0.5f);
                if (IsHunting) {
                    Vector3 dir = TargetToSeek.position - transform.position;
                    Quaternion lookRot = Quaternion.LookRotation(dir);
                    Vector3 rot = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * 10f).eulerAngles;
                    transform.rotation = Quaternion.Euler(0f, rot.y, 0f);
                    EnemyAnimator.SetBool("attack", true);
                }
                break;
        }
    }

    public void Detection() {
        Transform target = GameObject.FindGameObjectWithTag("Player").transform;
        FreeRoam(false);
        if (target.tag.Equals("Player")) {
            bool isCamouflaged = false;

            playercontroller pc = target.GetComponent<playercontroller>();
            if (pc != null && pc.isCamouflaged) isCamouflaged = true;

            float distance = Vector3.Distance(transform.position, target.position);

            if (isCamouflaged && distance > playerCamouflagedDetectionDistance) {
                return;
            }
        }
        Scream();
        EnemyAnimator.SetBool("hunting", true);
        TargetToSeek = target;
        IsHunting = true;
        enemiesHuntingPlayer.Add(this);
    }

    public void LostDetection() {
        FreeRoam(true);
        if(IsHunting) {
            Scream();
        }
        EnemyAnimator.SetBool("hunting", false);
        Enemy.speed = Wspeed;
        TargetToSeek = null;
        IsHunting = false;
        enemiesHuntingPlayer.Remove(this);
        EnemyAnimator.SetFloat("speed", -0.5f);
    }

    void FreeRoam(bool state) {
        if(this.gameObject.GetComponent<RandomNavMeshWalk>()) {
        this.gameObject.GetComponent<RandomNavMeshWalk>().enabled = state;
        }
        if(this.gameObject.GetComponent<WaypointSystem>()) {
        this.gameObject.GetComponent<WaypointSystem>().enabled = state;
        }
    }

    public void HitAHitbox(float dmg) { //Used in a SendMessage by weapons to stun the enemies or knock them back
        Scream();
        if(!TargetToSeek) Detection();
        if (IdleSoundController != null) IdleSoundController.Pause();
        if (EnemyAudio != null) EnemyAudio.Pause();
        if (Enemy.enabled) Enemy.isStopped = true;
        EnemyAnimator.SetBool("IsHit", true);

        HealthPoints -= dmg;
        if(HealthPoints <= 0f) {
            Death();
        }
    }

    public void Death() { //Used in a sendMessage by the guns to kill the Enemy prefab
        FreeRoam(false);
        Scream();
        enemiesHuntingPlayer.Remove(this);

        if (IdleSoundController != null) IdleSoundController.Stop();
        if (EnemyAudio != null) EnemyAudio.Stop();

        Collider[] hitboxes = EnemyAnimator.gameObject.GetComponentsInChildren<Collider>();

        foreach (var hb in hitboxes)
        {
            hb.isTrigger = false;
        }

        SetRagdollState(true);
        StartCoroutine(EnableActivatable());
        this.tag = "dead";
        this.enabled = false;
        /*Scream();
        if (IdleSoundController != null) IdleSoundController.Stop();
        if (EnemyAudio != null) EnemyAudio.Stop();
        EnemyAnimator.SetBool("dead",true);
        EnemyAnimator.SetBool("IsHit", false);
        Enemy.isStopped = true;
        GetComponent<EnemyController>().enabled = false;
        GetComponent<CapsuleCollider>().enabled = false;

        foreach (BoxCollider Child in transform.GetComponentsInChildren<BoxCollider>())
        {
            Child.enabled = false;
        }

        Destroy(this.gameObject, 4f);*/
    }

    void SetRagdollState(bool state) { //true enables the ragdoll and false disables it
        GetComponent<CapsuleCollider>().enabled = !state;
        GetComponentInChildren<Animator>().enabled = !state;
        GetComponent<NavMeshAgent>().enabled = !state;
        GetComponent<MeshCollider>().enabled = !state;
        FreeRoam(!state);

        Rigidbody[] rb = GetComponentsInChildren<Rigidbody>();

        foreach (var r in rb)
        {
            r.isKinematic = !state;
        }
    }

    public void Burning(GameObject fire) { //this function is used in a SendMessage to Burn the enemy gameObject for a time window equal to the BurnTime float assigned in hierarchy

        if(fireTimer == BurnTime) {
            if(fire != null) {
                fireClone = (GameObject)Instantiate(fire, transform.position, transform.rotation);
                fireClone.transform.SetParent(transform.GetChild(0).GetChild(0));
            }
            onFire = true;
        }

        if(fireTimer <= 0f) {
            onFire = false;
            Death();
            Destroy(fireClone, 5f);
            fireTimer = BurnTime;

        } else if(fireTimer > 0f){
            fireTimer -= Time.deltaTime;
        }
    }

    void Scream() { //Call the method to play a scream, usually used in detection, detection lost, hit, death and attack
        if(Screams.enabled && Screams != null && !Screams.isPlaying && ScreamClip.Length > 0 && Enemy.enabled == true) {
            int RandomSound = Random.Range(0 , ScreamClip.Length - 1);
            Screams.volume = AudioSourceVolume;
            Screams.clip = ScreamClip[RandomSound];
            Screams.Play();
        }
    }

    void Hungry() {
        CurrentHunger -= HungerLossPerTick;

        if (CurrentHunger > 50f) {
            dmg = Damage;
            Wspeed = WalkSpeed;
            Rspeed = RunSpeed;
            IsStarving = false;

        } else if (CurrentHunger <= MaxHunger/2 && !IsStarving) {
            dmg = Damage * 1.2f;
            Wspeed = WalkSpeed * 1.3f;
            Rspeed = RunSpeed * 1.2f;
            IsStarving = true;

        } else if (CurrentHunger <= 0 && !IsStarving) {
            dmg = Damage * 1.7f;
            Wspeed = WalkSpeed * 1.6f;
            Rspeed = RunSpeed * 1.5f;
            IsStarving = true;
        }
    }

    IEnumerator BangOnDoor(DoorAction door) {
        isHittingDoor = true;
        door.isBeingHitByEnemy = true;
        Enemy.enabled = false;

        // EnemyAnimator.SetBool("attack", true);

        while (!door.IsCurrentlyOpen()) {
            currentHitTimer -= Time.deltaTime;

            if (currentHitTimer <= 0f) {
                currentHitTimer = hitTimer;
                door.Hit();
                EnemyAnimator.SetBool("attack", true);
            }

            yield return null;
        }

        EnemyAnimator.SetBool("attack", false);

        Enemy.enabled = true;
        door.isBeingHitByEnemy = false;
        isHittingDoor = false;
    }

    IEnumerator EnableActivatable() {
        var activatables = new List<Activatable>();
        var actions = new List<PickupItemAction>();
        Collider[] hitboxes = EnemyAnimator.gameObject.GetComponentsInChildren<Collider>();

        foreach (var hitbox in hitboxes) {
            var activatable = hitbox.gameObject.AddComponent<Activatable>();
            var action = hitbox.gameObject.AddComponent<PickupItemAction>();

            activatable.enabled = false;
            action.enabled = false;

            activatables.Add(activatable);
            actions.Add(action);

            yield return null;
        }

        yield return new WaitForSeconds(5f);

        for (int i = 0; i < activatables.Count; i++) {
            activatables[i].actionText = "Camouflage";
            activatables[i].events.AddListener(() => {
                actions[0].Camouflage();
                foreach (var a in activatables) {
                    a.enabled = false;
                }
                foreach (var a in actions) {
                    a.enabled = false;
                }
            });

            activatables[i].enabled = true;
            actions[i].enabled = true;

            yield return null;
        }
    }

}