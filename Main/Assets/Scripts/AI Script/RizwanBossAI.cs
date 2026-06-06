using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RizwanBossAI : MonoBehaviour
{
    [Header("Boss Stats (NIGHTMARE)")]
    public float walkSpeed = 12f; 
    public float attackRange = 3f;
    public float dashTriggerRange = 15f;

    [Header("Damage & Timers")]
    public float basicAttackCooldown = 0.8f;
    public float dashCooldown = 4f; 
    public float summonCooldown = 5f; 
    public float basicDamage = 25f;

    [Header("Assassin Protocol (Teleport)")]
    public float teleportCooldown = 6f; 
    public float teleportDistance = 10f; 

    [Header("Minion Protocol (SteJew)")]
    public GameObject steJewPrefab;
    public int maxSteJews = 6; 

    // Internal State
    private Transform playerTarget;
    private Rigidbody rb;
    private Animator anim;
    private UniversalHealth healthScript;
    private UniversalMeleeAttack meleeScript;

    private float lastBasicAttack = 0f;
    private float lastDashTime = 0f;
    private float lastSummonTime = 0f;
    private float lastTeleportTime = 0f;

    private bool isDashing = false;
    private bool isTeleporting = false;

    private List<GameObject> activeMinions = new List<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        meleeScript = GetComponent<UniversalMeleeAttack>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    void FixedUpdate()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null || isTeleporting) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        CleanMinionList();

        float sqrDistance = (transform.position - playerTarget.position).sqrMagnitude;

        // 1. Minion Summoning 
        if (Time.time >= lastSummonTime + summonCooldown && !isDashing)
        {
            lastSummonTime = Time.time;
            if (activeMinions.Count < maxSteJews) SummonMinions();
        }

        // 2. Relentless Teleport (Behind Player)
        if (Time.time >= lastTeleportTime + teleportCooldown && !isDashing)
        {
            if (sqrDistance > (teleportDistance * teleportDistance) || Random.value > 0.98f)
            {
                StartCoroutine(TeleportStrikeProtocol());
                return;
            }
        }

        // 3. Phantom Dash (Invisible Blink to Player)
        if (sqrDistance <= (dashTriggerRange * dashTriggerRange) && sqrDistance > (attackRange * attackRange))
        {
            if (Time.time >= lastDashTime + dashCooldown && !isDashing)
            {
                StartCoroutine(PhantomDashStrike());
                return;
            }
        }

        // 4. Movement & Attack Logic
        if (!isDashing)
        {
            if (sqrDistance > (attackRange * attackRange))
            {
                if (anim != null) anim.SetBool("isChasing", true);
                Vector3 direction = (lookPos - transform.position).normalized;
                rb.linearVelocity = new Vector3(direction.x * walkSpeed, rb.linearVelocity.y, direction.z * walkSpeed);
            }
            else
            {
                if (anim != null) anim.SetBool("isChasing", false);
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

                if (Time.time >= lastBasicAttack + basicAttackCooldown)
                {
                    lastBasicAttack = Time.time;
                    if (meleeScript != null) meleeScript.TriggerAttack(basicDamage);
                }
            }
        }
    }

    // ==========================================
    // NIGHTMARE BOSS MECHANICS
    // ==========================================

    IEnumerator PhantomDashStrike()
    {
        isDashing = true;
        lastDashTime = Time.time;

        if (playerTarget != null)
        {
            Vector3 inFrontPos = playerTarget.position + (transform.position - playerTarget.position).normalized * 1.5f;
            inFrontPos.y = transform.position.y;

            transform.position = inFrontPos;
            
            lastBasicAttack = Time.time;
            if (meleeScript != null) meleeScript.TriggerAttack(basicDamage);
        }

        yield return new WaitForSeconds(0.3f); 
        isDashing = false;
    }

    IEnumerator TeleportStrikeProtocol()
    {
        isTeleporting = true;
        lastTeleportTime = Time.time;

        rb.linearVelocity = Vector3.zero;
        if (anim != null) anim.SetBool("isChasing", false);

        yield return new WaitForSeconds(0.1f); 

        if (playerTarget != null)
        {
            Vector3 behindPos = playerTarget.position - (playerTarget.forward * 2f);
            behindPos.y = transform.position.y;

            transform.position = behindPos;
            transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));

            lastBasicAttack = Time.time;
            if (meleeScript != null) meleeScript.TriggerAttack(basicDamage * 1.5f);
        }

        yield return new WaitForSeconds(0.3f); 
        isTeleporting = false;
    }

    void SummonMinions()
    {
        int amountToSpawn = maxSteJews - activeMinions.Count;

        for (int i = 0; i < amountToSpawn; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * 4f;
            
            // AGENT ZETA HACK: Added 1.5f to the Y axis so they drop from slightly above the ground!
            Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 1.5f, randomCircle.y);

            if (steJewPrefab != null)
            {
                GameObject newMinion = Instantiate(steJewPrefab, spawnPos, Quaternion.identity);
                
                // AGENT ZETA HACK: Turn on their brains by injecting the player target!
                BasicChaserAI ai = newMinion.GetComponent<BasicChaserAI>();
                if (ai != null && playerTarget != null)
                {
                    ai.SetTarget(playerTarget);
                }

                activeMinions.Add(newMinion);
            }
        }
    }

    void CleanMinionList()
    {
        activeMinions.RemoveAll(item => item == null);
    }
}