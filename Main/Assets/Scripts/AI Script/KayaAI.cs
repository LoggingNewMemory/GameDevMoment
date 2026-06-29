using UnityEngine;
using UnityEngine.AI; // <-- AGENT ZETA REQUIRED!
using System.Collections;

public class KayaAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f; 

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
    private NavMeshAgent agent; 
    
    private float nextTeleportTime = 0f;
    private float lastAttackTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        agent = GetComponent<NavMeshAgent>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        nextTeleportTime = Time.time + Random.Range(2f, 5f); 
        
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange - 0.5f;
        }
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

        if (Time.time >= nextTeleportTime && distance > attackRange)
        {
            TeleportBehindPlayer();
            return; 
        }

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            if (anim != null) anim.SetBool("isChasing", true);
        }
        else
        {
            agent.isStopped = true;
            if (anim != null) anim.SetBool("isChasing", false);
            
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
    }

    void TeleportBehindPlayer()
    {
        nextTeleportTime = Time.time + teleportCooldown;
        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);
        
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;

        Vector3 bestPos = transform.position;
        
        for (int i = 0; i < 5; i++)
        {
            Vector3 offsetDir = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * (-playerTarget.forward);
            Vector3 testPos = playerTarget.position + (offsetDir * teleportDistance);
            testPos.y += 2f; 

            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit floorHit, 10f))
            {
                Vector3 finalPos = floorHit.point + (Vector3.up * 0.1f);
                
                if (NavMesh.SamplePosition(finalPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
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
        
        // --- SECURE AGENT WARP ---
        if (agent != null) agent.Warp(bestPos);
        else transform.position = bestPos;

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