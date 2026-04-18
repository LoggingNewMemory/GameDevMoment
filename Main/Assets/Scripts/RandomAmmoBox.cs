using UnityEngine;

public class RandomAmmoBox : MonoBehaviour
{
    [Header("Loot Settings")]
    public AmmoType myAmmoType;
    public int ammoInside = 0;

    [Header("Audio")]
    public AudioClip pickupSound;

    void Start()
    {
        myAmmoType = (AmmoType)Random.Range(0, 6);

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
        if (other.CompareTag("Player"))
        {
            PlayerAmmoStore ammoStore = other.GetComponentInParent<PlayerAmmoStore>();
            
            if (ammoStore != null)
            {
                // --- CHANGED: Ask the backpack if it wants the ammo! ---
                bool wasPickedUp = ammoStore.AddAmmo(myAmmoType, ammoInside);
                
                // If the backpack said TRUE (we own the gun), eat the box!
                if (wasPickedUp)
                {
                    if (pickupSound != null)
                    {
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                    }
                    Destroy(gameObject);
                }
                // If the backpack said FALSE, we do absolutely nothing. The box stays on the floor!
            }
        }
    }
}