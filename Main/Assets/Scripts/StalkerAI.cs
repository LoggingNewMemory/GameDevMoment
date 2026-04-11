using UnityEngine;
using System.Collections;

public class StalkerAI : MonoBehaviour
{
    [Header("Stalker AI Settings")]
    public float moveSpeed = 8f;
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float teleportDamage = 20f;
    public float attackCooldown = 1.2f;
    public float teleportCooldown = 10f;

    private Transform playerTarget;
    private float lastAttackTime;
    private float lastTeleportTime;
    
    private Animator anim;
    private UniversalMeleeAttack meleeScript; 
    private UniversalHealth healthScript; 

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>(); 
        healthScript = GetComponent<UniversalHealth>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        lastTeleportTime = Time.time; 
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) return;

        // --- THE FIX: Removed the "isProvoked" wait check entirely! ---
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (Time.time >= lastTeleportTime + teleportCooldown && distance > 5f)
        {
            StartCoroutine(TeleportRoutine());
        }

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
                lastAttackTime = Time.time;
                if (meleeScript != null) meleeScript.TriggerAttack(attackDamage);
            }
        }
    }

    IEnumerator TeleportRoutine()
    {
        lastTeleportTime = Time.time;
        Vector3 teleportPos = playerTarget.position - (playerTarget.forward * 1.5f);
        teleportPos.y = transform.position.y; 
        transform.position = teleportPos;

        lastAttackTime = Time.time;
        if (meleeScript != null) meleeScript.TriggerAttack(teleportDamage);
        
        yield return null;
    }
}