using UnityEngine;
using System.Collections;

public class KayaAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f; 
    public float stoppingDistance = 2f; 

    [Header("Agent Zeta's Whiskers")]
    public float obstacleCheckDistance = 2f; 
    private int dodgeDirection = 1; 
    private float nextDodgeChangeTime = 0f;

    [Header("Phantom Protocol (Anti-Stuck)")]
    public float stuckCheckInterval = 3f; 
    public float stuckDistanceThreshold = 1.0f; 

    [Header("Teleport Settings")]
    public float teleportCooldown = 7f; 
    public float teleportDistance = 2f; 
    public AudioClip teleportSound;

    [Header("Combat Settings (Flashbang)")]
    public float attackRange = 2.5f;
    public float attackDamage = 5f; 
    public float attackCooldown = 4f;
    public float flashbangDuration = 1f;
    public AudioClip flashbangSound;

    private Transform playerTarget;
    private Animator anim;
    private UniversalHealth healthScript;
    private Rigidbody rb; 
    
    private float nextTeleportTime = 0f;
    private float lastAttackTime = 0f;

    private Vector3 lastCheckedPosition;
    private float nextStuckCheckTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        nextTeleportTime = Time.time + Random.Range(2f, 5f); 
        lastCheckedPosition = transform.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero; 
                rb.isKinematic = true; 
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
            return;
        }

        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- AGENT ZETA: ANTI-STUCK MONITOR ---
        if (Time.time > nextStuckCheckTime)
        {
            if (distance > attackRange * 2f) 
            {
                float movedDist = Vector3.Distance(transform.position, lastCheckedPosition);
                if (movedDist < stuckDistanceThreshold) TeleportBehindPlayer(); // Just use her normal combat teleport if stuck!
            }
            lastCheckedPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }
        // --------------------------------------

        if (Time.time >= nextTeleportTime && distance > attackRange)
        {
            TeleportBehindPlayer();
            return; 
        }

        if (distance > stoppingDistance)
        {
            Vector3 targetPos = playerTarget.position;
            targetPos.y = transform.position.y;
            Vector3 moveDir = (targetPos - transform.position).normalized;
            
            Vector3 chestPos = transform.position + Vector3.up * 1f;
            if (Physics.Raycast(chestPos, transform.forward, out RaycastHit hit, obstacleCheckDistance))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    if (Time.time > nextDodgeChangeTime)
                    {
                        dodgeDirection = Random.Range(0, 2) == 0 ? 1 : -1;
                        nextDodgeChangeTime = Time.time + 1.5f; 
                    }
                    moveDir = transform.right * dodgeDirection;
                }
            }
            
            if (rb != null) rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);
            if (moveDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            if (anim != null) anim.SetBool("isChasing", true);
        }
        else
        {
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (anim != null) anim.SetBool("isChasing", false);
            
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void TeleportBehindPlayer()
    {
        nextTeleportTime = Time.time + teleportCooldown;
        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);

        Vector3 bestPos = transform.position;
        
        for (int i = 0; i < 5; i++)
        {
            Vector3 offsetDir = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * (-playerTarget.forward);
            Vector3 testPos = playerTarget.position + (offsetDir * teleportDistance);
            testPos.y += 2f; 

            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit floorHit, 10f))
            {
                Vector3 finalPos = floorHit.point + (Vector3.up * 0.1f);
                
                // --- AGENT ZETA: NAVMESH SECURITY SCAN ---
                if (UnityEngine.AI.NavMesh.SamplePosition(finalPos, out UnityEngine.AI.NavMeshHit navHit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    finalPos = navHit.position;

                    if (!Physics.CheckSphere(finalPos + (Vector3.up * 1f), 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        bestPos = finalPos;
                        break;
                    }
                }
            }
        }
        
        if (rb != null) rb.position = bestPos;
        else transform.position = bestPos;
        lastCheckedPosition = bestPos;

        Vector3 lookDir = (playerTarget.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");
        if (flashbangSound != null) AudioSource.PlayClipAtPoint(flashbangSound, transform.position);

        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(attackDamage, transform); 
            stats.TriggerFlashbang(flashbangDuration); 
        }
    }
}