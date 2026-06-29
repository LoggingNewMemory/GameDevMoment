using UnityEngine;
using UnityEngine.AI; // <-- AGENT ZETA REQUIRED: GPS Library!
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

    private Transform playerTarget;
    private float lastAttackTime;
    private float lastTeleportTime;
    
    private Animator anim;
    private UniversalMeleeAttack meleeScript; 
    private UniversalHealth healthScript; 
    
    // --- AGENT ZETA: THE GPS DEVICE ---
    private NavMeshAgent agent; 

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>(); 
        healthScript = GetComponent<UniversalHealth>();
        
        agent = GetComponent<NavMeshAgent>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        lastTeleportTime = Time.time; 
        
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange - 0.2f;
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

        // Check for teleport BEFORE regular movement
        if (Time.time >= lastTeleportTime + teleportCooldown && distance > 5f)
        {
            StartCoroutine(TeleportRoutine());
            return; 
        }

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            agent.isStopped = false;
            // Tell the GPS where to go!
            agent.SetDestination(playerTarget.position);
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            agent.isStopped = true; // Hit the brakes!

            Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
            transform.LookAt(lookPos);

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
        
        // Pause the GPS so he doesn't slide while warping!
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;

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
                
                // NAVMESH SECURITY SCAN
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
        
        // --- AGENT ZETA WARP COMMAND ---
        if (agent != null) agent.Warp(bestPos);
        else transform.position = bestPos;
        // -------------------------------

        lastAttackTime = Time.time;
        if (meleeScript != null) meleeScript.TriggerAttack(teleportDamage);
        
        yield return null;
    }
}