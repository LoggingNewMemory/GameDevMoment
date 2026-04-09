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
    private bool isProvoked = false; // Waits until you shoot him!
    private float lastAttackTime = 0f;

    [Header("Animation & Audio")]
    public Animator anim;        
    public AudioClip hitSound;   
    public AudioClip deathSound; 
    public AudioClip attackSound; // (Optional) A whoosh or grunt sound

    [Header("Loot Drops")]
    public GameObject indomiePrefab;    
    public GameObject macNCheesePrefab; 
    public GameObject ammoBoxPrefab;    

    void Start()
    {
        // Automatically find the player when the game starts
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        // Don't do anything if he's dead, hasn't been shot yet, or can't find the player
        if (isDead || !isProvoked || playerTarget == null) return;

        // Face the player (Ignoring Y axis so he doesn't tilt upward if you jump)
        Vector3 lookPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(lookPos);

        // Check how far away the player is
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > attackRange)
        {
            // CHASE THE PLAYER!
            if (anim != null) anim.SetBool("isChasing", true);
            transform.position = Vector3.MoveTowards(transform.position, lookPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            // IN RANGE: STOP AND ATTACK!
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

        // THE WAKE UP CALL: He gets mad and starts chasing you the moment he gets shot!
        isProvoked = true;

        health -= amount;

        if (health <= 0f)
        {
            Die();
        }
        else 
        {
            if (anim != null) anim.SetTrigger("Hit");
            if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        
        // Trigger the punch animation
        if (anim != null) anim.SetTrigger("Attack");
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        // Deal damage to the player
        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(attackDamage, transform);
        }
    }

    void Die()
    {
        isDead = true;

        if (anim != null) 
        {
            anim.ResetTrigger("Hit"); 
            anim.ResetTrigger("Attack"); // Cancel any punches he was throwing
            anim.SetBool("isChasing", false);
            anim.SetTrigger("Die");   
        }

        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // --- LOOT DROP ---
        int roll = Random.Range(1, 101);
        if (roll <= 30 && indomiePrefab != null) Instantiate(indomiePrefab, transform.position + Vector3.up, Quaternion.identity);
        else if (roll > 30 && roll <= 50 && macNCheesePrefab != null) Instantiate(macNCheesePrefab, transform.position + Vector3.up, Quaternion.identity);
        else if (roll > 50 && roll <= 100 && ammoBoxPrefab != null) Instantiate(ammoBoxPrefab, transform.position + Vector3.up, Quaternion.identity);

        Destroy(gameObject, 3f);
    }
}