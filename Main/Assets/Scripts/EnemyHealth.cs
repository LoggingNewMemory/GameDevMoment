using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public float health = 60f; 
    private bool isDead = false; // Prevents him from dying twice or dropping double loot!

    [Header("Animation")]
    public Animator anim;        // <-- NEW: Drag Jambret's Animator component here!

    [Header("Audio")]
    public AudioClip hitSound;   
    public AudioClip deathSound; 

    [Header("Loot Drops")]
    public GameObject indomiePrefab;    
    public GameObject macNCheesePrefab; 
    public GameObject ammoBoxPrefab;    

    public void TakeDamage(float amount)
    {
        // If he's already dead, ignore the bullets!
        if (isDead) return;

        health -= amount;

        if (health <= 0f)
        {
            Die();
        }
        else 
        {
            // Play the flinch animation and sound!
            if (anim != null) anim.SetTrigger("Hit");
            if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    void Die()
    {
        isDead = true;

        // --- THE FIX ---
        if (anim != null) 
        {
            anim.ResetTrigger("Hit"); // Cancel any flinching!
            anim.SetTrigger("Die");   // Drop dead!
        }

        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // Turn off his collider so the player can walk over his body
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // --- LOOT DROP LOGIC ---
        int roll = Random.Range(1, 101);

        if (roll <= 30 && indomiePrefab != null) Instantiate(indomiePrefab, transform.position + Vector3.up, Quaternion.identity);
        else if (roll > 30 && roll <= 50 && macNCheesePrefab != null) Instantiate(macNCheesePrefab, transform.position + Vector3.up, Quaternion.identity);
        else if (roll > 50 && roll <= 100 && ammoBoxPrefab != null) Instantiate(ammoBoxPrefab, transform.position + Vector3.up, Quaternion.identity);

        // DELAYED DESTROY: Wait 3 seconds so the animation finishes, then delete the body!
        Destroy(gameObject, 3f);
    }
}