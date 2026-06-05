using UnityEngine;
using System.Collections;

public class AnangFinalBossAI : MonoBehaviour
{
    [Header("Final Boss Stats")]
    public float walkSpeed = 8f;
    public float attackRange = 3f;
    public float railgunRange = 30f; // Can shoot from across the map!

    [Header("Melee Combat")]
    public float meleeDamage = 20f;
    public float meleeCooldown = 1.2f;

    [Header("Railgun Protocol (Annihilation)")]
    public GameObject railgunBeamEffect; // Drag the RailgunBeamEffect here!
    public float railgunDamage = 50f;
    public float railgunCooldown = 7f;
    public float aimTrackingTime = 1.0f; // Time he follows your movement
    public float aimLockTime = 0.5f;     // Time he freezes before firing (DODGE NOW!)
    public float blastDuration = 0.5f;   // How long the beam stays on screen

    private Transform playerTarget;
    private Rigidbody rb;
    private Animator anim;
    private UniversalHealth healthScript;
    private UniversalMeleeAttack meleeScript;

    private float lastMeleeTime = 0f;
    private float lastRailgunTime = 0f;
    private bool isCastingRailgun = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        meleeScript = GetComponent<UniversalMeleeAttack>();

        // Ensure the beam is hidden at the start!
        if (railgunBeamEffect != null) railgunBeamEffect.SetActive(false);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        // Give the player a few seconds before the first railgun blast
        lastRailgunTime = Time.time - (railgunCooldown / 2f); 
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    void FixedUpdate()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null || isCastingRailgun) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // 1. RAILGUN ANNIHILATION CHECK
        if (Time.time >= lastRailgunTime + railgunCooldown && distance <= railgunRange)
        {
            StartCoroutine(FireRailgunRoutine());
            return;
        }

        // 2. MELEE AND CHASING
        transform.LookAt(lookPos);

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            Vector3 direction = (lookPos - transform.position).normalized;
            rb.linearVelocity = new Vector3(direction.x * walkSpeed, rb.linearVelocity.y, direction.z * walkSpeed);
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (Time.time >= lastMeleeTime + meleeCooldown)
            {
                lastMeleeTime = Time.time;
                if (meleeScript != null) meleeScript.TriggerAttack(meleeDamage);
            }
        }
    }

    // ==========================================
    // ANNIHILATION PROTOCOL
    // ==========================================

    IEnumerator FireRailgunRoutine()
    {
        isCastingRailgun = true;
        lastRailgunTime = Time.time;

        // Hit the brakes!
        rb.linearVelocity = Vector3.zero;
        if (anim != null) anim.SetBool("isChasing", false);
        
        // PHASE 1: TRACKING (He follows your movement)
        float timer = 0;
        while (timer < aimTrackingTime)
        {
            if (playerTarget != null)
            {
                Vector3 aimPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
                transform.LookAt(aimPos);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // PHASE 2: LOCK ON (He stops rotating. THIS IS THE DODGE WINDOW!)
        if (anim != null) anim.SetTrigger("chargeDash"); // Warning animation!
        yield return new WaitForSeconds(aimLockTime);

        // PHASE 3: FIRE!
        if (railgunBeamEffect != null) railgunBeamEffect.SetActive(true);
        if (anim != null) anim.SetTrigger("Attack");

        // Calculate if the player actually dodged!
        if (playerTarget != null)
        {
            Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toPlayer);
            
            // If the player is still within 15 degrees of the front, they get hit!
            if (angle < 15f) 
            {
                UniversalHealth playerHealth = playerTarget.GetComponent<UniversalHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(railgunDamage);
                    Debug.Log("ANANG HIT PLAYER WITH RAILGUN FOR 50 DAMAGE!");
                }
            }
            else
            {
                Debug.Log("Player successfully dodged the Railgun!");
            }
        }

        // Keep the beam visible for a moment
        yield return new WaitForSeconds(blastDuration);

        // Turn off beam and recover
        if (railgunBeamEffect != null) railgunBeamEffect.SetActive(false);
        yield return new WaitForSeconds(1f);

        isCastingRailgun = false;
    }
}