using UnityEngine;
using System.Collections;

public class AmericaWokeAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 9f; // <-- UPDATED: Now much faster!
    public float stoppingDistance = 1.5f; // Gets right up in your face before stopping his feet

    [Header("Combat Settings")]
    public float attackRange = 3f;
    public float attackDamage = 30f;
    public float attackCooldown = 2f;
    
    [Header("Dash Knockdown Ability")]
    [Range(0f, 100f)]
    public float dashChance = 30f;
    public float dashSpeed = 35f; // The physical speed he flies at you during the dash

    private Transform playerTarget;
    private Animator anim;
    private UniversalHealth healthScript;
    
    private float lastAttackTime = 0f;
    private bool isDashing = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        // Stop thinking if dead or currently flying through the air
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;
        if (isDashing) return; 

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // 1. ALWAYS CHASE (Unless physically bumping into the player)
        if (distance > stoppingDistance)
        {
            Vector3 moveDir = (playerTarget.position - transform.position).normalized;
            moveDir.y = 0; 
            
            transform.position += moveDir * moveSpeed * Time.deltaTime;
            
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

        // 2. ATTACK ON THE RUN (No stopping and waiting!)
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
            // Instantly launch into the Knockdown Dash!
            StartCoroutine(DashKnockdownRoutine());
        }
        else
        {
            // Instant normal attack while continuing to walk!
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(attackDamage, transform); 
            }
        }
    }

    IEnumerator DashKnockdownRoutine()
    {
        isDashing = true; // Temporarily stop standard walking so the dash physics take over
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTarget.position;
        Vector3 dashDir = (targetPos - startPos).normalized;
        dashDir.y = 0;

        float dashDuration = 0.2f; // Fast, violent lunge
        float elapsed = 0f;

        // Physically launch him at the player
        while (elapsed < dashDuration)
        {
            if (healthScript != null && healthScript.isDead) yield break;
            
            elapsed += Time.deltaTime;
            transform.position += dashDir * dashSpeed * Time.deltaTime;
            yield return null;
        }

        // Did the dash connect, or did the player dodge?
        if (Vector3.Distance(transform.position, playerTarget.position) <= attackRange + 1f)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(attackDamage, transform); 

            DoomMovement movement = playerTarget.GetComponent<DoomMovement>();
            if (movement != null)
            {
                // Triggers the violent camera fall and weapon drop
                movement.TriggerKnockdown();
                Debug.Log("AmericaWoke used Dash Knockdown!");
            }
        }

        isDashing = false; // Go back to normal relentless chasing
    }
}