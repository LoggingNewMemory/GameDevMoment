using UnityEngine;
using System.Collections;

public class AmericaWokeAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 9f; 
    public float stoppingDistance = 1.5f; 

    [Header("Combat Settings")]
    public float attackRange = 3f;
    public float attackDamage = 30f;
    public float attackCooldown = 2f;
    
    [Header("Dash Knockdown Ability")]
    [Range(0f, 100f)]
    public float dashChance = 30f;
    public float dashSpeed = 35f; 

    private Transform playerTarget;
    private Animator anim;
    private UniversalHealth healthScript;
    private Rigidbody rb; // <-- NEW: Grabbing the physics body!
    
    private float lastAttackTime = 0f;
    private bool isDashing = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); // <-- NEW: Assigning the Rigidbody

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;
        if (isDashing) return; 

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // 1. ALWAYS CHASE
        if (distance > stoppingDistance)
        {
            Vector3 moveDir = (playerTarget.position - transform.position).normalized;
            moveDir.y = 0; 
            
            // --- NEW: Using Rigidbody to move so he can't pass through walls! ---
            if (rb != null)
            {
                rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
            
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);
            }

            if (anim != null) anim.SetBool("isChasing", true);
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
        }

        // 2. ATTACK ON THE RUN
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");

        float roll = Random.Range(0f, 100f);
        
        if (roll <= dashChance)
        {
            StartCoroutine(DashKnockdownRoutine());
        }
        else
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(attackDamage, transform); 
            }
        }
    }

    IEnumerator DashKnockdownRoutine()
    {
        isDashing = true; 
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTarget.position;
        Vector3 dashDir = (targetPos - startPos).normalized;
        dashDir.y = 0;

        float dashDuration = 0.2f; 
        float elapsed = 0f;

        // Physically launch him at the player using the Rigidbody!
        while (elapsed < dashDuration)
        {
            if (healthScript != null && healthScript.isDead) yield break;
            
            elapsed += Time.deltaTime;
            
            // --- NEW: Rigidbody movement for the high-speed dash ---
            if (rb != null)
            {
                rb.MovePosition(transform.position + dashDir * dashSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += dashDir * dashSpeed * Time.deltaTime;
            }
            
            yield return null;
        }

        if (Vector3.Distance(transform.position, playerTarget.position) <= attackRange + 1f)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(attackDamage, transform); 

            DoomMovement movement = playerTarget.GetComponent<DoomMovement>();
            if (movement != null)
            {
                movement.TriggerKnockdown();
                Debug.Log("AmericaWoke used Dash Knockdown!");
            }
        }

        isDashing = false; 
    }
}