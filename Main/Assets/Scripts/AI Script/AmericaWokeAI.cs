using UnityEngine;
using System.Collections;

public class AmericaWokeAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 9f; 
    public float stoppingDistance = 1.5f; 

    [Header("Agent Zeta's Wall Slider")]
    public float obstacleCheckDistance = 1.5f; 
    public float bodyRadius = 0.4f; 

    [Header("Phantom Protocol (Anti-Stuck)")]
    public float stuckCheckInterval = 3f; 
    public float stuckDistanceThreshold = 1.0f; 

    [Header("Combat Settings")]
    public float attackRange = 3f;
    public float attackDamage = 30f;
    public float attackCooldown = 2f;

    [Header("Universal Attack Settings")]
    public AudioClip normalAttackSound;
    public AudioClip knockdownAttackSound;
    public float damageDelay = 0.3f;
    public float hitTrackingRange = 3f; 
    
    [Header("Dash Knockdown Ability")]
    [Range(0f, 100f)]
    public float dashChance = 30f;
    public float dashSpeed = 35f; 

    private Transform playerTarget;
    private Animator anim;
    private UniversalHealth healthScript;
    private Rigidbody rb; 
    
    private float lastAttackTime = 0f;
    private bool isDashing = false;

    // Anti-stuck trackers
    private Vector3 lastCheckedPosition;
    private float nextStuckCheckTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

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
        if (isDashing) return; 

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- AGENT ZETA: ANTI-STUCK MONITOR ---
        if (Time.time > nextStuckCheckTime)
        {
            if (distance > attackRange * 2f) 
            {
                float movedDist = Vector3.Distance(transform.position, lastCheckedPosition);
                if (movedDist < stuckDistanceThreshold) SafeTeleportToPlayer();
            }
            lastCheckedPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }
        // --------------------------------------

        if (distance > stoppingDistance)
        {
            Vector3 targetPos = playerTarget.position;
            targetPos.y = transform.position.y;
            Vector3 moveDir = (targetPos - transform.position).normalized;
            
            Vector3 chestPos = transform.position + Vector3.up * 1f;
            if (Physics.SphereCast(chestPos, bodyRadius, moveDir, out RaycastHit hit, obstacleCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    moveDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
                }
            }

            if (rb != null)
            {
                rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);
            }

            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            }

            if (anim != null) anim.SetBool("isChasing", true);
        }
        else
        {
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (anim != null) anim.SetBool("isChasing", false);
        }

        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void SafeTeleportToPlayer()
    {
        for (int i = 0; i < 10; i++) 
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(6f, 15f); 
            Vector3 testPos = playerTarget.position + new Vector3(randomDir.x, 2f, randomDir.y) * dist;

            if (Physics.Raycast(testPos, Vector3.down, out RaycastHit floorHit, 15f))
            {
                Vector3 finalPos = floorHit.point + (Vector3.up * 0.1f);

                // --- AGENT ZETA: NAVMESH SECURITY SCAN ---
                if (UnityEngine.AI.NavMesh.SamplePosition(finalPos, out UnityEngine.AI.NavMeshHit navHit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    finalPos = navHit.position; // Snap to the blue grid!

                    if (!Physics.CheckSphere(finalPos + (Vector3.up * 1f), bodyRadius * 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (rb != null) rb.position = finalPos;
                        else transform.position = finalPos;
                        
                        lastCheckedPosition = finalPos;
                        return;
                    }
                }
            }
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");

        float roll = Random.Range(0f, 100f);
        
        if (roll <= dashChance)
        {
            if (knockdownAttackSound != null) AudioSource.PlayClipAtPoint(knockdownAttackSound, transform.position);
            StartCoroutine(DashKnockdownRoutine());
        }
        else
        {
            if (normalAttackSound != null) AudioSource.PlayClipAtPoint(normalAttackSound, transform.position);
            StartCoroutine(NormalAttackRoutine());
        }
    }

    IEnumerator NormalAttackRoutine()
    {
        yield return new WaitForSeconds(damageDelay);
        if (healthScript != null && healthScript.isDead) yield break;
        if (Vector3.Distance(transform.position, playerTarget.position) <= hitTrackingRange)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(attackDamage, transform); 
        }
    }

    IEnumerator DashKnockdownRoutine()
    {
        isDashing = true; 
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTarget.position;
        targetPos.y = transform.position.y; 
        
        Vector3 dashDir = (targetPos - startPos).normalized;
        float dashDuration = 0.2f; 
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            if (healthScript != null && healthScript.isDead) yield break;
            elapsed += Time.deltaTime;
            
            if (Vector3.Distance(transform.position, playerTarget.position) > stoppingDistance)
            {
                if (rb != null) rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);
            }
            else
            {
                if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
            yield return null;
        }
        
        if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        if (Vector3.Distance(transform.position, playerTarget.position) <= hitTrackingRange)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(attackDamage, transform); 

            DoomMovement movement = playerTarget.GetComponent<DoomMovement>();
            if (movement != null) movement.TriggerKnockdown();

            if (anim != null) anim.SetBool("isChasing", false);
            yield return new WaitForSeconds(1.5f);
        }
        isDashing = false; 
    }
}