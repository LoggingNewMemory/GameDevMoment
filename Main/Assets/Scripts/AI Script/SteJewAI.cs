using UnityEngine;
using UnityEngine.AI; // <-- AGENT ZETA REQUIRED!
using System.Collections; 

public class SteJewAI : MonoBehaviour
{
    [Header("Flee Settings")]
    public float moveSpeed = 12f; 
    public float fleeDistance = 15f; 

    [Header("Panic Settings (Almost Caught)")]
    public float panicDistance = 6f;   
    public float panicSpeed = 20f;     

    [Header("Global Combat Settings")]
    public float attackDamage = 5f;
    public float attackCooldown = 8f; 
    public float damageDelay = 1f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip attackCastSound;    
    public AudioClip stealReserveSound;  

    private Transform playerTarget;
    private float lastAttackTime = 0f;

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

        lastAttackTime = Time.time; 
        
        if (agent != null) agent.speed = moveSpeed;
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
            return;
        }

        if (playerTarget == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // FLEE LOGIC
        if (distance < fleeDistance && !isCasting)
        {
            agent.isStopped = false;
            agent.speed = (distance < panicDistance) ? panicSpeed : moveSpeed;

            // Calculate a point AWAY from the player!
            Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
            Vector3 targetFleePos = transform.position + (fleeDirection * 10f);
            
            // Check if that point is actually on the NavMesh
            if (NavMesh.SamplePosition(targetFleePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            
            if (anim != null) anim.SetBool("isChasing", true); 
        }
        else
        {
            agent.isStopped = true; // Safe distance reached, or currently casting!
            if (anim != null) anim.SetBool("isChasing", false);
        }

        // LOOK LOGIC
        if (isCasting || agent.isStopped)
        {
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
        }

        if (Time.time >= lastAttackTime + attackCooldown) AttackPlayer();
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        isCasting = true; 
        
        if (agent != null) agent.isStopped = true; // Stand still to cast!

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
        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        SimpleShoot activeGun = playerTarget.GetComponentInChildren<SimpleShoot>();

        if (distance >= 20f && distance <= 40f)
        {
            if (stats != null) stats.TakeDamage(attackDamage, null); 
        }
        else if (distance >= 0f && distance < 20f)
        {
            if (activeGun != null)
            {
                activeGun.StealReserveAmmo(2); 
                if (stealReserveSound != null) AudioSource.PlayClipAtPoint(stealReserveSound, playerTarget.position);
            }
        }
    }
}