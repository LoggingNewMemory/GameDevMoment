using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Random Ammo Settings")]
    public int minAmmoAmount = 15; // Minimum bullets this box can give
    public int maxAmmoAmount = 45; // Maximum bullets this box can give

    [Header("Audio")]
    public AudioClip pickupSound; 

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SimpleShoot activeGun = other.GetComponentInChildren<SimpleShoot>();

            if (activeGun != null)
            {
                // If our pockets are already completely full, don't grab the box!
                if (activeGun.reserveAmmo >= activeGun.maxReserveAmmo) return;

                // Pick a random number between Min and Max!
                // (Note: Unity's Random.Range for integers needs +1 on the max to actually include it)
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