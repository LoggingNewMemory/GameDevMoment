using UnityEngine;
using UnityEngine.AI; // <-- AGENT ZETA REQUIRED!
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
    
    private NavMeshAgent agent; // <-- THE GPS
    
    private float lastAttackTime = 0f;
    private bool isDashing = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        agent = GetComponent<NavMeshAgent>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = stoppingDistance;
        }
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) 
        {
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
            return;
        }

        if (playerTarget == null || agent == null || isDashing) return; 

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            if (anim != null) anim.SetBool("isChasing", true);
        }
        else
        {
            agent.isStopped = true;
            if (anim != null) anim.SetBool("isChasing", false);
            
            // Look at player
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
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
        
        // Temporarily turn him into a rocket!
        float originalSpeed = agent.speed;
        float originalAccel = agent.acceleration;
        agent.speed = dashSpeed;
        agent.acceleration = 500f; // INSTANT SPEED
        agent.isStopped = false;
        agent.SetDestination(playerTarget.position);

        yield return new WaitForSeconds(0.3f); // Dash duration
        
        // Hit the brakes
        agent.isStopped = true;
        agent.speed = originalSpeed;
        agent.acceleration = originalAccel;

        if (Vector3.Distance(transform.position, playerTarget.position) <= hitTrackingRange)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(attackDamage, transform); 

            DoomMovement movement = playerTarget.GetComponent<DoomMovement>();
            if (movement != null) movement.TriggerKnockdown();

            if (anim != null) anim.SetBool("isChasing", false);
            yield return new WaitForSeconds(1.5f); // Rest after missing/hitting
        }
        isDashing = false; 
    }
}