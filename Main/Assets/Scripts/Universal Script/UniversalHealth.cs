using UnityEngine;

public class UniversalHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float health; 
    
    // Other scripts can look at this to see if the enemy is dead, but they cannot change it!
    public bool isDead { get; private set; } = false;

    private Animator anim;
    private UniversalMeleeAttack meleeScript;
    private UniversalCharacterAudio audioScript;
    private UniversalLootDrop lootScript;

    void Awake()
    {
        // Set health to full when the enemy spawns
        health = maxHealth;
        
        anim = GetComponent<Animator>();
        meleeScript = GetComponent<UniversalMeleeAttack>();
        audioScript = GetComponent<UniversalCharacterAudio>();
        lootScript = GetComponent<UniversalLootDrop>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

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

    void Die()
    {
        isDead = true;

        if (meleeScript != null) meleeScript.CancelAttack();
        if (audioScript != null) audioScript.PlayDeathSound();
        if (lootScript != null) lootScript.DropLoot();

        // --- HALU OF CS KILL BONUS ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerSkills skills = player.GetComponent<PlayerSkills>();
            if (skills != null) skills.AddHaluKillBonus();
        }

        // --- NEW: TELL THE ARENA SPAWNER AN ENEMY DIED! ---
        ArenaSpawner spawner = FindFirstObjectByType<ArenaSpawner>();
        if (spawner != null) spawner.EnemyDefeated();
        // --------------------------------------------------

        if (anim != null)
        {
            anim.ResetTrigger("Hit");
            anim.ResetTrigger("Attack");
            anim.SetBool("isChasing", false);
            anim.SetTrigger("Die");
        }

        // 1. Turn off the collider so the player doesn't trip over the body
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. THIS IS THE MISSING LINE! Turn off physics so they don't slide or fall through the floor!
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Destroy(gameObject, 3f);
    }
}