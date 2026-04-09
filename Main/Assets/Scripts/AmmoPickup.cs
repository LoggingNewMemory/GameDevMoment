using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30; // How many bullets this box gives

    [Header("Audio")]
    public AudioClip pickupSound; // Optional: The sound of grabbing ammo

    void OnTriggerEnter(Collider other)
    {
        // 1. Did the player walk into this box?
        if (other.CompareTag("Player"))
        {
            // 2. Because inactive weapons are turned OFF, this will naturally 
            // only grab the script of the weapon you currently have in your hands!
            SimpleShoot activeGun = other.GetComponentInChildren<SimpleShoot>();

            if (activeGun != null)
            {
                // 3. If our pockets are completely full, ignore the box so we don't waste it!
                if (activeGun.reserveAmmo >= activeGun.maxReserveAmmo) return;

                // 4. Give the gun the ammo!
                activeGun.AddAmmo(ammoAmount);

                // 5. Play the pickup sound
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                // 6. Delete the ammo box
                Destroy(gameObject);
            }
        }
    }
}