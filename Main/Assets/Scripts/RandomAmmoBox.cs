using UnityEngine;

public class RandomAmmoBox : MonoBehaviour
{
    [Header("Loot Settings")]
    [Tooltip("The script decides this randomly when it spawns!")]
    public AmmoType myAmmoType;
    public int ammoInside = 0;

    [Header("Audio")]
    public AudioClip pickupSound;

    void Start()
    {
        // 1. Roll a dice between 0 and 5 to pick one of the 6 ammo types!
        myAmmoType = (AmmoType)Random.Range(0, 6);

        // 2. Adjust the amount based on the gun! (We don't want 30 Railgun shots!)
        switch (myAmmoType)
        {
            case AmmoType.Pistol: ammoInside = Random.Range(10, 20); break;
            case AmmoType.Shotgun: ammoInside = Random.Range(4, 9); break;
            case AmmoType.SMG: ammoInside = Random.Range(30, 50); break;
            case AmmoType.AssaultRifle: ammoInside = Random.Range(20, 31); break;
            case AmmoType.SniperOrLMG: ammoInside = Random.Range(10, 20); break;
            case AmmoType.Railgun: ammoInside = Random.Range(1, 4); break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // If the player walks over this box
        if (other.CompareTag("Player"))
        {
            // Look for the backpack script on the player
            PlayerAmmoStore ammoStore = other.GetComponentInParent<PlayerAmmoStore>();
            
            if (ammoStore != null)
            {
                // Send the randomized ammo to the backpack!
                ammoStore.AddAmmo(myAmmoType, ammoInside);
                
                // Play a sound if you have one
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                // Destroy the box so it can't be picked up twice
                Destroy(gameObject);
            }
        }
    }
}