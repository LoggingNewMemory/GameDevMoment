using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 60f; 

    [Header("Audio")]
    public AudioClip hitSound;   
    public AudioClip deathSound; 

    [Header("Loot Drops")]
    public GameObject indomiePrefab;    
    public GameObject macNCheesePrefab; 
    public GameObject ammoBoxPrefab;    

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0f)
        {
            Die();
        }
        else if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    void Die()
    {
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // --- LOOT DROP LOGIC ---
        // Pick a random number between 1 and 100
        int roll = Random.Range(1, 101);

        // 1 to 30 = Indomie (30% chance)
        if (roll <= 30 && indomiePrefab != null)
        {
            Instantiate(indomiePrefab, transform.position + Vector3.up, Quaternion.identity);
        }
        // 31 to 50 = Mac N Cheese (20% chance)
        else if (roll > 30 && roll <= 50 && macNCheesePrefab != null)
        {
            Instantiate(macNCheesePrefab, transform.position + Vector3.up, Quaternion.identity);
        }
        // 51 to 100 = Ammo Box (50% chance!)
        else if (roll > 50 && roll <= 100 && ammoBoxPrefab != null)
        {
            Instantiate(ammoBoxPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}