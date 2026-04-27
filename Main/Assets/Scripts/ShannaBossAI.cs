using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ShannaBossAI : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 12f;      

    [Header("Combat")]
    public float attackRange = 2.5f;
    public float baseDamage = 10f;
    public float teleportDamage = 15f; 
    public float attackCooldown = 0.8f;   
    public float attackFreezeTime = 0.3f; 

    [Header("Special Moves")]
    public float teleportCooldown = 3.0f; 
    public float teleportDistanceBehind = 1.5f; 
    public float gapCloseDistance = 15f; 
    
    private Transform playerTarget;
    private Rigidbody rb;
    private Animator anim;
    private UniversalHealth healthScript;
    private UniversalMeleeAttack meleeScript; 

    private float lastAttackTime = 0f;
    private float lastTeleportTime = 0f;
    private bool isAttacking = false; 
    private bool hasTeleportDamageBoost = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        meleeScript = GetComponent<UniversalMeleeAttack>(); 

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void FixedUpdate()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        
        // --- NEW FIX: Force her completely still during the attack so she doesn't slide! ---
        if (isAttacking) 
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            transform.LookAt(lookPos); 
            return;
        }

        transform.LookAt(lookPos); 
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- ATTACK LOGIC ---
        if (distance <= attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                // --- NEW FIX: Only creep forward if she is further than 1.5m away. 
                // Otherwise, stand perfectly still so the colliders don't grind together! ---
                if (distance > 1.5f)
                {
                    Vector3 dir = (lookPos - transform.position).normalized;
                    rb.linearVelocity = new Vector3(dir.x * (runSpeed * 0.5f), rb.linearVelocity.y, dir.z * (runSpeed * 0.5f));
                }
                else
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }
            }
        }
        // --- CHASE LOGIC ---
        else
        {
            if (anim != null) anim.SetBool("isChasing", true);

            bool shouldTeleport = Time.time >= lastTeleportTime + teleportCooldown;
            
            if (distance > gapCloseDistance)
            {
                shouldTeleport = true;
            }

            if (shouldTeleport)
            {
                TeleportBehindPlayer();
            }
            else
            {
                // Normal Running
                Vector3 dir = (lookPos - transform.position).normalized;
                rb.linearVelocity = new Vector3(dir.x * runSpeed, rb.linearVelocity.y, dir.z * runSpeed);
            }
        }
    }

    void TeleportBehindPlayer()
    {
        lastTeleportTime = Time.time;

        Vector3 behindPos = playerTarget.position - (playerTarget.forward * teleportDistanceBehind);
        behindPos.y = transform.position.y; 
        
        transform.position = behindPos;
        transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));
        
        hasTeleportDamageBoost = true; 
        lastAttackTime = 0f; 
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        float damageToDeal = hasTeleportDamageBoost ? teleportDamage : baseDamage;
        hasTeleportDamageBoost = false; 

        if (anim != null) anim.SetTrigger("Attack"); 
        
        if (meleeScript != null)
        {
            meleeScript.TriggerAttack(damageToDeal);
        }

        yield return new WaitForSeconds(attackFreezeTime);
        
        isAttacking = false;
    }
}