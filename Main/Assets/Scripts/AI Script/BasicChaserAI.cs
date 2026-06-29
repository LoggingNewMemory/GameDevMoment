using UnityEngine;

public class BasicChaserAI : MonoBehaviour
{
    [Header("AI & Combat")]
    public float moveSpeed = 8f;
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    [Header("Agent Zeta's Wall Slider")]
    [Tooltip("How far ahead the enemy looks for walls.")]
    public float obstacleCheckDistance = 1.5f; 
    [Tooltip("How thick the enemy is (prevents shoulder clipping).")]
    public float bodyRadius = 0.4f; 

    [Header("Phantom Protocol (Anti-Stuck)")]
    public float stuckCheckInterval = 3f; 
    public float stuckDistanceThreshold = 1.0f; 

    private Transform playerTarget;
    private float lastAttackTime = 0f;

    private Animator anim;
    private UniversalMeleeAttack meleeScript;
    private UniversalHealth healthScript; 
    private Rigidbody rb; 

    // Anti-stuck trackers
    private Vector3 lastCheckedPosition;
    private float nextStuckCheckTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        lastCheckedPosition = transform.position;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    void FixedUpdate() 
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        float sqrDistance = (transform.position - playerTarget.position).sqrMagnitude;

        // --- AGENT ZETA: ANTI-STUCK MONITOR ---
        if (Time.time > nextStuckCheckTime)
        {
            // Only check if they are safely outside of their attack range!
            if (sqrDistance > (attackRange * attackRange * 4f)) 
            {
                float movedDist = Vector3.Distance(transform.position, lastCheckedPosition);
                
                if (movedDist < stuckDistanceThreshold)
                {
                    // THEY ARE STUCK! INITIATE SAFE TELEPORT!
                    SafeTeleportToPlayer();
                }
            }
            
            // Reset trackers for the next 3 seconds
            lastCheckedPosition = transform.position;
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }
        // --------------------------------------

        if (sqrDistance > (attackRange * attackRange))
        {
            if (anim != null) anim.SetBool("isChasing", true);
            
            if (rb != null)
            {
                Vector3 direction = (lookPos - transform.position).normalized;

                // Wall Slider Math
                Vector3 chestPos = transform.position + Vector3.up * 1f;
                
                if (Physics.SphereCast(chestPos, bodyRadius, direction, out RaycastHit hit, obstacleCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.collider.CompareTag("Player"))
                    {
                        direction = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
                    }
                }

                rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
            }
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                if (meleeScript != null) meleeScript.TriggerAttack(attackDamage);
            }
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
}