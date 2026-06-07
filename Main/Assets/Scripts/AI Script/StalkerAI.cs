using UnityEngine;
using System.Collections;

public class StalkerAI : MonoBehaviour
{
    [Header("Stalker AI Settings")]
    public float moveSpeed = 8f;
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float teleportDamage = 20f;
    public float attackCooldown = 1.2f;
    public float teleportCooldown = 10f;

    [Header("Agent Zeta's Whiskers")]
    public float obstacleCheckDistance = 2f; 
    private int dodgeDirection = 1; 
    private float nextDodgeChangeTime = 0f;

    [Header("Phantom Protocol (Anti-Stuck)")]
    public float stuckCheckInterval = 3f; 
    public float stuckDistanceThreshold = 1.0f; 

    private Transform playerTarget;
    private float lastAttackTime;
    private float lastTeleportTime;
    
    private Animator anim;
    private UniversalMeleeAttack meleeScript; 
    private UniversalHealth healthScript; 
    private Rigidbody rb; 

    private Vector3 lastCheckedPosition;
    private float nextStuckCheckTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>(); 
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        lastTeleportTime = Time.time; 
        lastCheckedPosition = transform.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
    }

    void FixedUpdate() 
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- AGENT ZETA: ANTI-STUCK MONITOR ---
        if (Time.time > nextStuckCheckTime)
        {
            if (distance > attackRange * 2f) 
            {
                float movedDist = Vector3.Distance(transform.position, lastCheckedPosition);
                if (movedDist < stuckDistanceThreshold) StartCoroutine(TeleportRoutine()); 
            }
            lastCheckedPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }
        // --------------------------------------

        if (Time.time >= lastTeleportTime + teleportCooldown && distance > 5f)
        {
            StartCoroutine(TeleportRoutine());
        }

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            
            if (rb != null)
            {
                Vector3 moveDir = (lookPos - transform.position).normalized;

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

                Vector3 targetPos = transform.position + moveDir * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(targetPos);
            }
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                if (meleeScript != null) meleeScript.TriggerAttack(attackDamage);
            }
        }
    }

    IEnumerator TeleportRoutine()
    {
        lastTeleportTime = Time.time;
        Vector3 bestPos = transform.position;

        // --- SAFE TELEPORT: Try up to 5 safe spots behind the player ---
        for (int i = 0; i < 5; i++)
        {
            Vector3 offsetDir = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * (-playerTarget.forward);
            Vector3 testPos = playerTarget.position + (offsetDir * 1.5f);
            testPos.y += 2f; 

            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit floorHit, 10f))
            {
                Vector3 finalPos = floorHit.point + (Vector3.up * 0.1f);
                if (!Physics.CheckSphere(finalPos + (Vector3.up * 1f), 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    bestPos = finalPos;
                    break;
                }
            }
        }
        
        if (rb != null) rb.position = bestPos;
        else transform.position = bestPos;
        lastCheckedPosition = bestPos;

        lastAttackTime = Time.time;
        if (meleeScript != null) meleeScript.TriggerAttack(teleportDamage);
        
        yield return null;
    }
}