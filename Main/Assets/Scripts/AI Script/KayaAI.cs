using UnityEngine;
using System.Collections;

public class KayaAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f; 
    public float stoppingDistance = 2f; 

    [Header("Teleport Settings")]
    public float teleportCooldown = 7f; 
    public float teleportDistance = 2f; 
    public AudioClip teleportSound;

    [Header("Combat Settings (Flashbang)")]
    public float attackRange = 2.5f;
    public float attackDamage = 5f; 
    public float attackCooldown = 4f;
    public float flashbangDuration = 1f;
    public AudioClip flashbangSound;

    private Transform playerTarget;
    private Animator anim;
    private UniversalHealth healthScript;
    private Rigidbody rb; 
    
    private float nextTeleportTime = 0f;
    private float lastAttackTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 

        // --- THE TRUTH REVEALER ---
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) 
        {
            playerTarget = p.transform;
            Debug.Log("<color=red>" + gameObject.name + " is currently chasing: " + p.name + "</color>");
        }

        nextTeleportTime = Time.time + Random.Range(2f, 5f); 
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

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // 1. TELEPORT LOGIC 
        if (Time.time >= nextTeleportTime && distance > attackRange)
        {
            TeleportBehindPlayer();
            return; 
        }

        // 2. MOVEMENT & CHASING 
        if (distance > stoppingDistance)
        {
            // Flatten the target position so she doesn't try to fly up or dig down
            Vector3 targetPos = playerTarget.position;
            targetPos.y = transform.position.y;
            
            Vector3 moveDir = (targetPos - transform.position).normalized;
            
            // --- NEW: Using Velocity makes physics 100x smoother! ---
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
            // --- NEW: Hit the brakes when she reaches you! ---
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            
            if (anim != null) anim.SetBool("isChasing", false);
            
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        // 3. ATTACK (FLASHBANG) 
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void TeleportBehindPlayer()
    {
        nextTeleportTime = Time.time + teleportCooldown;
        
        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);

        Vector3 behindPos = playerTarget.position - (playerTarget.forward * teleportDistance);
        behindPos.y = transform.position.y; 

        transform.position = behindPos;

        Vector3 lookDir = (playerTarget.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");

        if (flashbangSound != null) AudioSource.PlayClipAtPoint(flashbangSound, transform.position);

        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(attackDamage, transform); 
            stats.TriggerFlashbang(flashbangDuration); 
        }
    }
}