using UnityEngine;
using UnityEngine.AI; // <-- AGENT ZETA REQUIRED: The GPS Library!

public class BasicChaserAI : MonoBehaviour
{
    [Header("AI & Combat")]
    public float moveSpeed = 8f;
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    private Transform playerTarget;
    private float lastAttackTime = 0f;

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
        
        // Grab the GPS component!
        agent = GetComponent<NavMeshAgent>(); 
        
        if (agent != null)
        {
            // Sync the agent's speed with your custom variable
            agent.speed = moveSpeed; 
            // Prevent the agent from stopping too early
            agent.stoppingDistance = attackRange - 0.5f; 
        }
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    // Agent Zeta Tip: NavMeshAgents work best in normal Update!
    void Update() 
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
            return;
        }
        
        if (playerTarget == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            
            // --- AGENT ZETA PATHFINDING ---
            agent.isStopped = false;
            // Tell the GPS where Pria Sigma 1 is, and it will automatically steer around walls!
            agent.SetDestination(playerTarget.position); 
            // ------------------------------
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            
            // Stop moving so we can swing!
            agent.isStopped = true;

            // Look directly at the player's face
            Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
            transform.LookAt(lookPos);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                if (meleeScript != null) meleeScript.TriggerAttack(attackDamage);
            }
        }
    }
}