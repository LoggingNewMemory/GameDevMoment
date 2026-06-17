using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [Header("Laser Settings")]
    public float speed = 40f;          
    public float lifetime = 3f;        
    public float damage = 20f;         

    public GameObject hitEffect;       

    void Start()
    {
        // Destroy this bullet after 'lifetime' seconds if it misses everything!
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Fly straight forward!
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Don't let the player shoot themselves!
        if (other.CompareTag("Player")) return;

        // Try to find the UniversalHealth script on the enemy we just hit
        UniversalHealth enemyHealth = other.GetComponent<UniversalHealth>();
        
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage); 
        }

        // Spawn a cool spark effect where the laser hit
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation);
        }

        // Destroy the laser bullet
        Destroy(gameObject);
    }
}