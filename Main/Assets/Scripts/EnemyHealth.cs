using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 60f; // Takes 3 shots from a 20-damage gun

    public void TakeDamage(float amount)
    {
        health -= amount;
        
        // Optional: Print to console so we know it's working
        Debug.Log(gameObject.name + " took damage! Health left: " + health);

        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        // For now, the ultimate hack: just Thanos-snap him out of existence
        Destroy(gameObject);
    }
}