using UnityEngine;

public class BasicChaserAI : MonoBehaviour
{
    [Header("AI & Combat")]
    public float moveSpeed = 5f;
    public float attackRange = 2f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;

    private Transform playerTarget;
    private bool isProvoked = false;
    private float lastAttackTime = 0f;

    private Animator anim;
    private UniversalMeleeAttack meleeScript;
    private UniversalHealth healthScript; // <-- Talks to the new Universal script

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>();
        healthScript = GetComponent<UniversalHealth>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        // 1. Stop thinking if we are dead
        if (healthScript != null && healthScript.isDead) return;

        // 2. If we took damage, get mad and start chasing!
        if (healthScript != null && healthScript.health < healthScript.maxHealth) 
        {
            isProvoked = true;
        }

        // 3. Stand still if unprovoked
        if (!isProvoked || playerTarget == null) return;

        // --- MOVEMENT & ATTACKING ---
        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            transform.position = Vector3.MoveTowards(transform.position, lookPos, moveSpeed * Time.deltaTime);
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
}