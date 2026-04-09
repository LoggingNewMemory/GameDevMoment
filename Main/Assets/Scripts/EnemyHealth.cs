using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float health = 60f; 
    private bool isDead = false; 

    [Header("AI & Combat")]
    public float moveSpeed = 5f;
    public float attackRange = 2f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    
    private Transform playerTarget;
    private bool isProvoked = false; 
    private float lastAttackTime = 0f;

    public Animator anim;        
    private UniversalMeleeAttack meleeScript;
    private UniversalCharacterAudio audioScript;
    private UniversalLootDrop lootScript; // <-- UNIVERSAL

    void Start()
    {
        meleeScript = GetComponent<UniversalMeleeAttack>();
        audioScript = GetComponent<UniversalCharacterAudio>();
        lootScript = GetComponent<UniversalLootDrop>(); // <-- UNIVERSAL

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        if (isDead || !isProvoked || playerTarget == null) return;

        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            if (anim != null) anim.SetBool("isChasing", true);
            transform.position = Vector3.MoveTowards(transform.position, lookPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
            if (Time.time >= lastAttackTime + attackCooldown) AttackPlayer();
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        isProvoked = true;
        health -= amount;

        if (health <= 0f) Die();
        else 
        {
            if (anim != null) anim.SetTrigger("Hit"); 
            if (audioScript != null) audioScript.PlayHitSound();
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (meleeScript != null) meleeScript.TriggerAttack(attackDamage);
    }

    void Die()
    {
        isDead = true;
        if (meleeScript != null) meleeScript.CancelAttack(); 
        if (audioScript != null) audioScript.PlayDeathSound();
        if (lootScript != null) lootScript.DropLoot(); // <-- UNIVERSAL DROP

        if (anim != null) 
        {
            anim.SetBool("isChasing", false);
            anim.SetTrigger("Die");   
        }

        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 3f);
    }
}