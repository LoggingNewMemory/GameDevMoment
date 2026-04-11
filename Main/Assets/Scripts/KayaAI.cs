using UnityEngine;
using System.Collections;

public class KayaAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f; 
    public float stoppingDistance = 2f; 

    [Header("Teleport Settings")]
    public float teleportCooldown = 7f; // How often she can warp behind you
    public float teleportDistance = 2f; // How far behind you she lands
    public AudioClip teleportSound;

    [Header("Combat Settings (Flashbang)")]
    public float attackRange = 2.5f;
    public float attackDamage = 5f; // Small damage, mostly just to stun!
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

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        // Randomize her first teleport so it catches the player off guard!
        nextTeleportTime = Time.time + Random.Range(2f, 5f); 
    }

    void Update()
    {
        // --- DEATH PHYSICS FIX ---
        if (healthScript != null && healthScript.isDead) 
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true; 
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
            return;
        }

        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- 1. TELEPORT LOGIC ---
        // If her cooldown is ready and she isn't already in your face
        if (Time.time >= nextTeleportTime && distance > attackRange)
        {
            TeleportBehindPlayer();
            return; 
        }

        // --- 2. ALWAYS CHASE ---
        if (distance > stoppingDistance)
        {
            Vector3 moveDir = (playerTarget.position - transform.position).normalized;
            moveDir.y = 0; 
            
            if (rb != null) rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
            else transform.position += moveDir * moveSpeed * Time.deltaTime;
            
            if (moveDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 10f);

            if (anim != null) anim.SetBool("isChasing", true);
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            
            // Keep looking at the player even when stopped
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        // --- 3. ATTACK (FLASHBANG) ---
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void TeleportBehindPlayer()
    {
        nextTeleportTime = Time.time + teleportCooldown;
        
        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);

        // Calculate the exact spot behind the player's back!
        Vector3 behindPos = playerTarget.position - (playerTarget.forward * teleportDistance);
        behindPos.y = transform.position.y; // Keep her flat on the floor

        transform.position = behindPos;

        // Snap her rotation so she is instantly looking at your back
        Vector3 lookDir = (playerTarget.position - transform.position).normalized;
        lookDir.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDir);

        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, transform.position);
        Debug.Log("Kaya teleported behind you!");
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
            stats.TriggerFlashbang(flashbangDuration); // Call the new blind effect!
        }
    }
}