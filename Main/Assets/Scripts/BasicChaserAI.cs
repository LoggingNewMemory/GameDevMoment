using UnityEngine;

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
    private Rigidbody rb; 

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 
        
        // KOBO OPTIMIZATION: No more FindGameObjectWithTag! Target is injected by spawner!
    }

    // Function to receive the target from the spawner
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

        // KOBO OPTIMIZATION: sqrMagnitude is 10x faster than Vector3.Distance!
        float sqrDistance = (transform.position - playerTarget.position).sqrMagnitude;

        // Multiply attackRange by itself to match the sqrDistance
        if (sqrDistance > (attackRange * attackRange))
        {
            if (anim != null) anim.SetBool("isChasing", true);
            
            if (rb != null)
            {
                Vector3 direction = (lookPos - transform.position).normalized;
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
}