using UnityEngine;
using System.Collections;

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
    private UniversalMeleeAttack meleeScript; 
    private UniversalCharacterAudio audioScript;
    private UniversalLootDrop lootScript; // <-- UNIVERSAL

    void Start()
    {
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>(); 
        audioScript = GetComponent<UniversalCharacterAudio>();
        lootScript = GetComponent<UniversalLootDrop>(); // <-- UNIVERSAL

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
        
        lastTeleportTime = Time.time; 
    }

    void Update()
    {
        if (isDead || !isProvoked || playerTarget == null) return;

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
                AttackPlayer(attackDamage);
            }
        }
    }

    IEnumerator TeleportRoutine()
    {
        lastTeleportTime = Time.time;
        Vector3 teleportPos = playerTarget.position - (playerTarget.forward * 1.5f);
        teleportPos.y = transform.position.y; 
        transform.position = teleportPos;

        AttackPlayer(teleportDamage);
        yield return null;
    }

    void AttackPlayer(float damage)
    {
        lastAttackTime = Time.time;
        if (meleeScript != null) meleeScript.TriggerAttack(damage);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        isProvoked = true;
        health -= amount;

        if (health <= 0) Die();
        else 
        {
            anim.SetTrigger("Hit");
            if (audioScript != null) audioScript.PlayHitSound();
        }
    }

    void Die()
    {
        isDead = true;
        if (meleeScript != null) meleeScript.CancelAttack(); 
        if (audioScript != null) audioScript.PlayDeathSound();
        if (lootScript != null) lootScript.DropLoot(); // <-- UNIVERSAL DROP

        anim.SetBool("isChasing", false);
        anim.SetTrigger("Die");
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 3f);
    }
}