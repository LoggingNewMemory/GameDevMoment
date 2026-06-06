using UnityEngine;
using System.Collections; 

public class SteJewAI : MonoBehaviour
{
    [Header("Flee Settings")]
    public float moveSpeed = 22f; 
    public float fleeDistance = 15f; 
    
    [Header("Panic Settings (Almost Caught)")]
    public float panicDistance = 6f;   
    public float panicSpeed = 38f;     

    [Header("Wall Bounce Settings")]
    public float wallCheckDistance = 3f; 
    public float bounceDuration = 0.5f;  
    private Vector3 bounceDir;
    private float bounceTimer = 0f;

    [Header("Global Combat Settings")]
    public float attackDamage = 5f;
    public float attackCooldown = 8f; 
    public float damageDelay = 1f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip attackCastSound;    
    public AudioClip emptyMagSound;      
    public AudioClip stealReserveSound;  

    private Transform playerTarget;
    private float lastAttackTime = 0f;

    private Animator anim;
    private UniversalHealth healthScript;
    private Rigidbody rb; 
    
    private bool isCasting = false; 

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        rb = GetComponent<Rigidbody>(); 
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) 
        {
            playerTarget = p.transform;
            Debug.Log("<color=yellow>" + gameObject.name + " is currently avoiding: " + p.name + "</color>");
        }

        lastAttackTime = Time.time; 
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero; 
                rb.isKinematic = true; 
                
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
            return;
        }

        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        Vector3 currentMoveDir = Vector3.zero;

        // --- 1. FLEE, PANIC & BOUNCE LOGIC ---
        if (distance < fleeDistance)
        {
            float currentSpeed = moveSpeed;
            if (distance < panicDistance)
            {
                currentSpeed = panicSpeed;
            }

            if (bounceTimer > 0)
            {
                bounceTimer -= Time.deltaTime;
                currentMoveDir = bounceDir;
            }
            else
            {
                currentMoveDir = (transform.position - playerTarget.position).normalized;
                currentMoveDir.y = 0; 
            }

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, currentMoveDir, out RaycastHit hit, wallCheckDistance))
            {
                if (!hit.transform.CompareTag("Player") && Mathf.Abs(hit.normal.y) < 0.2f)
                {
                    bounceDir = Vector3.Reflect(currentMoveDir, hit.normal).normalized;
                    bounceDir.y = 0;
                    
                    bounceTimer = bounceDuration;
                    currentMoveDir = bounceDir;
                }
            }

            // --- ANTI-SKYROCKET VELOCITY ---
            if (rb != null)
            {
                float safeY = rb.linearVelocity.y;
                // If Unity tries to launch him upward faster than 2f, instantly kill the upward momentum!
                if (safeY > 2f) safeY = -2f; 

                rb.linearVelocity = new Vector3(currentMoveDir.x * currentSpeed, safeY, currentMoveDir.z * currentSpeed);
            }
            else
            {
                transform.position += currentMoveDir * currentSpeed * Time.deltaTime;
            }

            if (anim != null) anim.SetBool("isChasing", true); 
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            
            // --- ANTI-SKYROCKET BRAKES ---
            if (rb != null) 
            {
                float safeY = rb.linearVelocity.y;
                if (safeY > 2f) safeY = -2f;
                rb.linearVelocity = new Vector3(0, safeY, 0);
            }
        }

        // --- 2. ROTATION LOGIC ---
        if (isCasting)
        {
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }
        }
        else if (currentMoveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentMoveDir), Time.deltaTime * 10f);
        }
        else 
        {
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }
        }

        // --- 3. GLOBAL ATTACK LOGIC ---
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        isCasting = true; 

        if (anim != null) anim.SetTrigger("Attack");

        if (audioSource != null && attackCastSound != null)
        {
            audioSource.PlayOneShot(attackCastSound);
        }

        StartCoroutine(MagicHitRoutine());
    }

    IEnumerator MagicHitRoutine()
    {
        yield return new WaitForSeconds(damageDelay);
        
        isCasting = false; 

        if (healthScript != null && healthScript.isDead) yield break;

        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(attackDamage, null); 
            stats.AddDizzyStack();
        }

        float roll = Random.Range(0f, 100f);
        SimpleShoot activeGun = playerTarget.GetComponentInChildren<SimpleShoot>();

        if (roll <= 60f) 
        {
            Debug.Log("SteJew just did a normal magic attack!");
        }
        else if (roll > 60f && roll <= 85f) 
        {
            if (activeGun != null)
            {
                activeGun.StealReserveAmmo(30); 
                if (stealReserveSound != null) AudioSource.PlayClipAtPoint(stealReserveSound, playerTarget.position);
                Debug.Log("SteJew used Global Magic to steal 30 Reserve Ammo!");
            }
        }
        else 
        {
            if (activeGun != null)
            {
                activeGun.EmptyMagazine();
                if (emptyMagSound != null) AudioSource.PlayClipAtPoint(emptyMagSound, playerTarget.position);
                Debug.Log("SteJew used Global Magic to empty your magazine!");
            }
        }
    }
}