using UnityEngine;
using System.Collections;

// The class name HERE must perfectly match the file name (StalkerAI)
public class StalkerAI : MonoBehaviour, IDamageable 
{
    public float health = 70f; 
    public float moveSpeed = 8f;
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float teleportDamage = 20f;
    public float attackCooldown = 1.2f;
    public float teleportCooldown = 10f;

    private Transform playerTarget;
    private bool isProvoked = false;
    private bool isDead = false;
    private float lastAttackTime;
    private float lastTeleportTime;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        // Start teleport off cooldown so he can't spam it immediately
        lastTeleportTime = Time.time; 
    }

    void Update()
    {
        if (isDead || !isProvoked || playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- TELEPORT LOGIC ---
        if (Time.time >= lastTeleportTime + teleportCooldown && distance > 5f)
        {
            StartCoroutine(TeleportRoutine());
        }

        // --- MOVEMENT & ATTACK ---
        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        if (distance > attackRange)
        {
            anim.SetBool("isChasing", true);
            transform.position = Vector3.MoveTowards(transform.position, lookPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            anim.SetBool("isChasing", false);
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer(attackDamage);
            }
        }
    }

    IEnumerator TeleportRoutine()
    {
        lastTeleportTime = Time.time;

        // Teleport slightly behind the player
        Vector3 teleportPos = playerTarget.position - (playerTarget.forward * 1.5f);
        
        // Keep him from floating in the air if you jump!
        teleportPos.y = transform.position.y; 
        
        transform.position = teleportPos;

        // Instant Attack after teleporting
        AttackPlayer(teleportDamage);
        
        yield return null;
    }

    void AttackPlayer(float damage)
    {
        lastAttackTime = Time.time;
        anim.SetTrigger("Attack");
        
        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null) stats.TakeDamage(damage, transform);
    }

    // --- FULFILLING THE IDAMAGEABLE CONTRACT ---
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        isProvoked = true;
        health -= amount;

        if (health <= 0) Die();
        else anim.SetTrigger("Hit");
    }

    void Die()
    {
        isDead = true;
        
        anim.ResetTrigger("Hit");
        anim.ResetTrigger("Attack");
        anim.SetBool("isChasing", false);
        anim.SetTrigger("Die");
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        Destroy(gameObject, 3f);
    }
}