using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Random Ammo Settings")]
    public int minAmmoAmount = 15; 
    public int maxAmmoAmount = 45; 

    [Header("Audio")]
    public AudioClip pickupSound; 

    void Start()
    {
        // THE MAGIC FIX: Auto-create a massive invisible Trigger bubble for the ammo!
        SphereCollider triggerBubble = gameObject.AddComponent<SphereCollider>();
        triggerBubble.isTrigger = true;
        
        // This handles your AmmoBox's tiny 0.0005 scale perfectly
        float scaleFix = Mathf.Abs(transform.localScale.x);
        if (scaleFix < 0.0001f) scaleFix = 0.0001f;
        triggerBubble.radius = 1.5f / scaleFix;
    }

    void OnTriggerEnter(Collider other)
    {
        CheckPickup(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        CheckPickup(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckPickup(collision.gameObject);
    }

    void CheckPickup(GameObject playerObject)
    {
        if (playerObject.CompareTag("Player"))
        {
            SimpleShoot activeGun = playerObject.GetComponentInChildren<SimpleShoot>();

            if (activeGun != null)
            {
                if (activeGun.reserveAmmo >= activeGun.maxReserveAmmo) return;

                int randomAmmo = Random.Range(minAmmoAmount, maxAmmoAmount + 1);
                activeGun.AddAmmo(randomAmmo);

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
        }
    }
}