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

    [Header("Animation")]
    public Animator anim;        

    [Header("Loot Drops")]
    public GameObject indomiePrefab;    
    public GameObject macNCheesePrefab; 
    public GameObject ammoBoxPrefab;    

    private UniversalMeleeAttack meleeScript;
    private UniversalCharacterAudio audioScript;

    void Start()
    {
        meleeScript = GetComponent<UniversalMeleeAttack>();
        audioScript = GetComponent<UniversalCharacterAudio>();

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

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        isProvoked = true;
        health -= amount;

        if (health <= 0f) 
        {
            Die();
        }
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

        if (anim != null) 
        {
            anim.ResetTrigger("Hit"); 
            anim.ResetTrigger("Attack"); 
            anim.SetBool("isChasing", false);
            anim.SetTrigger("Die");   
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        int roll = Random.Range(1, 101);
        if (roll <= 30 && indomiePrefab != null) Instantiate(indomiePrefab, transform.position + Vector3.up, Quaternion.identity);
        else if (roll > 30 && roll <= 50 && macNCheesePrefab != null) Instantiate(macNCheesePrefab, transform.position + Vector3.up, Quaternion.identity);
        else if (roll > 50 && roll <= 100 && ammoBoxPrefab != null) Instantiate(ammoBoxPrefab, transform.position + Vector3.up, Quaternion.identity);

        Destroy(gameObject, 3f);
    }
}