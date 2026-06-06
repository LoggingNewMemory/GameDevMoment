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

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero; // Stop moving instantly when dead
                rb.isKinematic = true; 
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
            return;
        }

        if (playerTarget == null) return;
        if (isDashing) return; 

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // 1. ALWAYS CHASE
        if (distance > stoppingDistance)
        {
            Vector3 targetPos = playerTarget.position;
            targetPos.y = transform.position.y;
            Vector3 moveDir = (targetPos - transform.position).normalized;
            
            // --- NEW: Physics Velocity Movement ---
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
            // --- NEW: Hit the brakes! ---
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
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
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);
                }
            }
            else
            {
                // Brake early if we hit the player!
                if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }
            
            yield return null;
        }
        
        // Ensure velocity resets to 0 when the dash finishes
        if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        if (Vector3.Distance(transform.position, playerTarget.position) <= hitTrackingRange)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(attackDamage, transform); 

            DoomMovement movement = playerTarget.GetComponent<DoomMovement>();
            if (movement != null)
            {
                movement.TriggerKnockdown();
                Debug.Log("AmericaWoke used Dash Knockdown!");
            }

            if (anim != null) anim.SetBool("isChasing", false);
            yield return new WaitForSeconds(1.5f);
        }

        isDashing = false; 
    }
}