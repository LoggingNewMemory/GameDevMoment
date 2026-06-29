using UnityEngine;
using UnityEngine.AI; 
using System.Collections; 

public enum ThiefState { Hunting, Fleeing }

public class SteJewAI : MonoBehaviour
{
    [Header("Hit & Run Settings")]
    public ThiefState currentState = ThiefState.Hunting;
    public float huntSpeed = 12f; 
    public float fleeSpeed = 22f; 
    public float fleeDuration = 6f; // How many seconds he runs away after an attempt
    private float fleeTimer = 0f;

    [Header("Teleport Settings")]
    [Range(0f, 100f)]
    public float teleportChance = 20f; 
    
    [Header("Steal Combat Settings")]
    public float attackRange = 2.5f;
    public float attackDamage = 5f;
    public float damageDelay = 0.5f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip attackCastSound;    
    public AudioClip stealReserveSound;  

    private Transform playerTarget;
    private Animator anim;
    private UniversalHealth healthScript;
    private NavMeshAgent agent; 
    
    private bool isCasting = false; 

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        agent = GetComponent<NavMeshAgent>();
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        // Roll the dice for a jump scare on the very first encounter!
        currentState = ThiefState.Hunting;
        RollForTeleport();
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
            return;
        }

        if (playerTarget == null || agent == null) return;

        // If he is currently doing the steal animation, freeze him in place and look at player!
        if (isCasting)
        {
            agent.isStopped = true;
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // ==========================================
        // STATE 1: HUNTING (Trying to steal)
        // ==========================================
        if (currentState == ThiefState.Hunting)
        {
            agent.speed = huntSpeed;

            if (distance <= attackRange)
            {
                AttackPlayer();
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(playerTarget.position);
                if (anim != null) anim.SetBool("isChasing", true);
            }
        }
        // ==========================================
        // STATE 2: FLEEING (Running away after attempt)
        // ==========================================
        else if (currentState == ThiefState.Fleeing)
        {
            fleeTimer -= Time.deltaTime;

            if (fleeTimer <= 0)
            {
                // Cooldown finished! Time to hunt again!
                currentState = ThiefState.Hunting;
                RollForTeleport();
            }
            else
            {
                agent.isStopped = false;
                agent.speed = fleeSpeed; 

                // Calculate a point AWAY from the player!
                Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
                Vector3 targetFleePos = transform.position + (fleeDirection * 10f);
                
                if (NavMesh.SamplePosition(targetFleePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                
                if (anim != null) anim.SetBool("isChasing", true); 

                // Look where he is running
                if (agent.velocity != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(agent.velocity.normalized), Time.deltaTime * 10f);
                }
            }
        }
    }

    void RollForTeleport()
    {
        if (Random.Range(0f, 100f) <= teleportChance)
        {
            // Teleport directly behind the player!
            Vector3 offsetDir = Quaternion.Euler(0, Random.Range(-180f, 180f), 0) * playerTarget.forward;
            Vector3 testPos = playerTarget.position + (offsetDir * 2f);
            testPos.y += 2f; 

            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit floorHit, 10f))
            {
                if (NavMesh.SamplePosition(floorHit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    agent.Warp(navHit.position);
                    Debug.Log("<color=magenta>[Agent Zeta] SteJew activated Teleport Ambush!</color>");
                }
            }
        }
    }

    void AttackPlayer()
    {
        isCasting = true; 
        if (agent != null) agent.isStopped = true; 

        if (anim != null) anim.SetTrigger("Attack");
        if (audioSource != null && attackCastSound != null) audioSource.PlayOneShot(attackCastSound);

        StartCoroutine(MagicHitRoutine());
    }

    IEnumerator MagicHitRoutine()
    {
        yield return new WaitForSeconds(damageDelay);
        isCasting = false; 

        if (healthScript != null && healthScript.isDead) yield break;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Check if the player dodged the pickpocket attempt!
        if (distance <= attackRange + 1f) 
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            SimpleShoot activeGun = playerTarget.GetComponentInChildren<SimpleShoot>();

            if (stats != null) stats.TakeDamage(attackDamage, transform); 
            
            if (activeGun != null)
            {
                activeGun.StealReserveAmmo(2); 
                if (stealReserveSound != null) AudioSource.PlayClipAtPoint(stealReserveSound, playerTarget.position);
            }
            Debug.Log("<color=red>[Agent Zeta] SteJew successfully stole ammo!</color>");
        }
        else
        {
            Debug.Log("<color=green>[Agent Zeta] Player dodged SteJew's steal attempt!</color>");
        }

        // Win or lose, he immediately runs away!
        currentState = ThiefState.Fleeing;
        fleeTimer = fleeDuration;
    }
}