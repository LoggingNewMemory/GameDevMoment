using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 60f; 

    [Header("Audio")]
    public AudioClip hitSound;   // <-- NEW: Slot for the pain grunt or bullet impact
    public AudioClip deathSound; 

    public void TakeDamage(float amount)
    {
        health -= amount;
        
        Debug.Log(gameObject.name + " took damage! Health left: " + health);

        if (health <= 0f)
        {
            Die();
        }
        else
        {
            // If they are still alive, play the hit sound!
            if (hitSound != null)
            {
                // You can add a 3rd number here for volume, like (hitSound, transform.position, 0.8f)
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }
        }
    }

    void Die()
    {
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        Destroy(gameObject);
    }
}