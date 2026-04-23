using UnityEngine;

public class BasicChaserAI : MonoBehaviour
{
    [Header("AI & Combat")]
    // --- FIXED: Default values matched to your perfect Inspector settings! ---
    public float moveSpeed = 8f;
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    private Transform playerTarget;
    private float lastAttackTime = 0f;

    private Animator anim;
    private UniversalMeleeAttack meleeScript;
    private UniversalHealth healthScript; 
    private Rigidbody rb; 

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void FixedUpdate() 
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            
            // --- FIXED: Using Velocity instead of MovePosition stops the "running away" bounce-back glitch! ---
            if (rb != null)
            {
                // Find the exact direction to the player
                Vector3 direction = (lookPos - transform.position).normalized;
                
                // Set the speed in that direction, but KEEP the current Y velocity so gravity still works!
                rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
            }
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            
            // --- FIXED: Slam the brakes so they don't slide around like they are on ice! ---
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
}