using UnityEngine;
using System.Collections;

public class AnangFinalBossAI : MonoBehaviour
{
    [Header("Final Boss Stats (Balanced)")]
    public float walkSpeed = 7.5f; 
    public float attackRange = 3f;

    [Header("Evasive Protocol (Dodge)")]
    public float dodgeCooldown = 5f; 
    public float dodgeSpeed = 25f;
    public float dodgeDuration = 0.2f;

    [Header("Woke Protocol (Dash Knockdown)")]
    public float dashCooldown = 10f; 
    public float dashSpeed = 30f; 
    public float dashDuration = 0.25f;
    
    [Header("Stalker Protocol (Teleport)")]
    public float teleportCooldown = 11f;
    public float teleportDistance = 2f;

    [Header("Kaya Protocol (Flashbang Melee)")]
    public float meleeDamage = 20f;
    public float meleeCooldown = 1.2f;
    [Range(0f, 100f)] public float flashbangChance = 25f; 
    public float flashbangDuration = 1.5f;

    [Header("Railgun Protocol (Annihilation)")]
    public GameObject railgunBeamPrefab; 
    public float railgunDamage = 40f; 
    public float railgunCooldown = 16f; 
    public float aimTrackingTime = 1.5f; 
    public float aimLockTime = 0.5f;     

    // --- Internal State ---
    private Transform playerTarget;
    private Rigidbody rb;
    private Animator anim;
    private UniversalHealth healthScript;
    private UniversalMeleeAttack meleeScript;

    private float lastMeleeTime = 0f;
    private float lastRailgunTime = 0f;
    private float lastTeleportTime = 0f;
    private float lastDashTime = 0f;
    private float lastDodgeTime = 0f;
    
    private bool isBusy = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        meleeScript = GetComponent<UniversalMeleeAttack>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        lastRailgunTime = Time.time - (railgunCooldown / 2f); 
        lastTeleportTime = Time.time + 3f; 
    }

    void FixedUpdate()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null || isBusy) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        transform.LookAt(lookPos);

        // --- 1. THREAT PRIORITY: RAILGUN ---
        if (Time.time >= lastRailgunTime + railgunCooldown && distance <= 30f)
        {
            StartCoroutine(FireRailgunRoutine());
            return;
        }

        // --- 2. THREAT PRIORITY: STALKER TELEPORT ---
        if (Time.time >= lastTeleportTime + teleportCooldown && distance > 5f)
        {
            ExecuteTeleportStrike();
            return;
        }

        // --- 3. THREAT PRIORITY: WOKE DASH ---
        if (Time.time >= lastDashTime + dashCooldown && distance > attackRange && distance < 15f)
        {
            StartCoroutine(DashKnockdownRoutine());
            return;
        }

        // --- 4. COMBAT MOVEMENT & EVASION ---
        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            
            if (Time.time >= lastDodgeTime + dodgeCooldown)
            {
                StartCoroutine(EvasiveDodgeRoutine());
                return;
            }

            Vector3 direction = (lookPos - transform.position).normalized;
            rb.linearVelocity = new Vector3(direction.x * walkSpeed, rb.linearVelocity.y, direction.z * walkSpeed);
        }
        else
        {
            // --- 5. MELEE / FLASHBANG ATTACK ---
            if (anim != null) anim.SetBool("isChasing", false);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (Time.time >= lastMeleeTime + meleeCooldown)
            {
                ExecuteMeleeStrike();
            }
        }
    }

    IEnumerator EvasiveDodgeRoutine()
    {
        isBusy = true;
        lastDodgeTime = Time.time;

        Vector3 dodgeDir = (Random.value > 0.5f) ? transform.right : -transform.right;
        
        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            rb.linearVelocity = new Vector3(dodgeDir.x * dodgeSpeed, rb.linearVelocity.y, dodgeDir.z * dodgeSpeed);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector3.zero;
        isBusy = false;
    }

    void ExecuteMeleeStrike()
    {
        lastMeleeTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");
        if (meleeScript != null) meleeScript.TriggerAttack(meleeDamage);

        if (Random.Range(0f, 100f) <= flashbangChance)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TriggerFlashbang(flashbangDuration);
        }
    }

    void ExecuteTeleportStrike()
    {
        lastTeleportTime = Time.time;

        Vector3 teleportDir = -playerTarget.forward;
        Vector3 rayStart = playerTarget.position + Vector3.up * 1f; // Cast from chest height
        Vector3 targetBehindPos = playerTarget.position + (teleportDir * teleportDistance);

        // AGENT ZETA HACK: Raycast backwards to check for arena walls!
        // We use default layers, but it will hit anything with a collider.
        if (Physics.Raycast(rayStart, teleportDir, out RaycastHit hit, teleportDistance))
        {
            // If there is a wall, teleport slightly in front of it so his collider doesn't get stuck!
            targetBehindPos = hit.point - (teleportDir * 0.8f); 
        }

        targetBehindPos.y = transform.position.y; 
        
        transform.position = targetBehindPos;
        transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));

        ExecuteMeleeStrike();
    }

    IEnumerator DashKnockdownRoutine()
    {
        isBusy = true; 
        lastDashTime = Time.time;

        if (anim != null) anim.SetTrigger("Attack");

        Vector3 targetPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        Vector3 dashDir = (targetPos - transform.position).normalized;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, rb.linearVelocity.y, dashDir.z * dashSpeed);
            elapsed += Time.fixedDeltaTime;

            if (Vector3.Distance(transform.position, playerTarget.position) <= attackRange)
            {
                rb.linearVelocity = Vector3.zero;
                PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
                if (stats != null) stats.TakeDamage(meleeDamage * 1.5f, transform); 
                DoomMovement movement = playerTarget.GetComponent<DoomMovement>();
                if (movement != null) movement.TriggerKnockdown();
                break; 
            }
            yield return new WaitForFixedUpdate();
        }
        
        rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(0.5f); 
        isBusy = false; 
    }

    IEnumerator FireRailgunRoutine()
    {
        isBusy = true;
        lastRailgunTime = Time.time;

        rb.linearVelocity = Vector3.zero;
        if (anim != null) anim.SetBool("isChasing", false);
        
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

        if (anim != null) anim.SetTrigger("Attack"); 
        yield return new WaitForSeconds(aimLockTime); 

        if (railgunBeamPrefab != null)
        {
            Vector3 startPos = transform.position + Vector3.up * 1.2f; 
            Vector3 endPos = startPos + (transform.forward * 30f);

            GameObject beam = Instantiate(railgunBeamPrefab, startPos, Quaternion.identity);
            BeamFader fader = beam.GetComponent<BeamFader>();
            
            if (fader != null) fader.ActivateBeam(startPos, endPos);
        }

        if (playerTarget != null)
        {
            Vector3 toPlayer = (playerTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toPlayer);
            
            if (angle < 15f) 
            {
                PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();
                if (playerStats != null) playerStats.TakeDamage(railgunDamage, transform);
            }
        }

        yield return new WaitForSeconds(1f); 
        isBusy = false;
    }
}