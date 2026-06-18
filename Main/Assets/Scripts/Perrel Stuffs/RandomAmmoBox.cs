using UnityEngine;
using System.Collections.Generic;

public class RandomAmmoBox : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip pickupSound;

    void Start()
    {
        // Agent Zeta Cleanup: No more hardcoding the ammo type at Start!
        // We just set up the physical trigger bubble.
        SphereCollider triggerBubble = gameObject.AddComponent<SphereCollider>();
        triggerBubble.isTrigger = true;
        
        float scaleFix = Mathf.Abs(transform.localScale.x);
        if (scaleFix < 0.0001f) scaleFix = 0.0001f;
        triggerBubble.radius = 1.5f / scaleFix;
    }

    void OnTriggerEnter(Collider other) { TryPickup(other.gameObject); }
    void OnCollisionEnter(Collision collision) { TryPickup(collision.gameObject); }

    void TryPickup(GameObject playerObj)
    {
        if (playerObj.CompareTag("Player"))
        {
            PlayerAmmoStore ammoStore = playerObj.GetComponentInParent<PlayerAmmoStore>();
            
            if (ammoStore != null)
            {
                // --- AGENT ZETA DYNAMIC AMMO LOGIC ---
                // 1. Build a list of only the weapons Pria Sigma 1 currently owns!
                List<AmmoType> ownedWeapons = new List<AmmoType>();
                
                ownedWeapons.Add(AmmoType.Pistol); 
                if (PlayerPrefs.GetInt("Unlocked_Shotgun", 0) == 1) ownedWeapons.Add(AmmoType.Shotgun);
                if (PlayerPrefs.GetInt("Unlocked_SMG", 0) == 1) ownedWeapons.Add(AmmoType.SMG);
                if (PlayerPrefs.GetInt("Unlocked_AssaultRifle", 0) == 1) ownedWeapons.Add(AmmoType.AssaultRifle);
                if (PlayerPrefs.GetInt("Unlocked_Sniper", 0) == 1) ownedWeapons.Add(AmmoType.Sniper);
                if (PlayerPrefs.GetInt("Unlocked_LMG", 0) == 1) ownedWeapons.Add(AmmoType.LMG);
                if (PlayerPrefs.GetInt("Unlocked_Railgun", 0) == 1) ownedWeapons.Add(AmmoType.Railgun);

                // 2. Shuffle the list so the ammo type chosen is completely RANDOM!
                for (int i = 0; i < ownedWeapons.Count; i++)
                {
                    AmmoType temp = ownedWeapons[i];
                    int randomIndex = Random.Range(i, ownedWeapons.Count);
                    ownedWeapons[i] = ownedWeapons[randomIndex];
                    ownedWeapons[randomIndex] = temp;
                }

                bool wasPickedUp = false;

                // 3. Loop through the randomized list. 
                foreach (AmmoType randomAmmo in ownedWeapons)
                {
                    int ammoInside = GetRandomAmountFor(randomAmmo);
                    
                    // The Backpack checks if it's full. If it's NOT full, it takes the ammo and returns true!
                    if (ammoStore.AddAmmo(randomAmmo, ammoInside))
                    {
                        wasPickedUp = true;
                        Debug.Log($"<color=cyan>[Agent Zeta] AmmoBox dynamically injected {ammoInside} {randomAmmo} rounds!</color>");
                        break; // Stop immediately! We gave them ammo for one weapon!
                    }
                }

                // 4. If we successfully gave them ammo (wasPickedUp == true), destroy the box!
                // If it's false, they are 100% maxed out on everything, so leave the box on the ground!
                if (wasPickedUp)
                {
                    if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                    Destroy(gameObject);
                }
            }
        }
    }

    // Helper function to keep the random bullet amount logic clean!
    int GetRandomAmountFor(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol: return Random.Range(10, 20);
            case AmmoType.Shotgun: return Random.Range(4, 9);
            case AmmoType.SMG: return Random.Range(30, 50);
            case AmmoType.AssaultRifle: return Random.Range(20, 31);
            case AmmoType.Sniper: return Random.Range(5, 10);
            case AmmoType.LMG: return Random.Range(40, 80);
            case AmmoType.Railgun: return Random.Range(1, 4);
            default: return 10;
        }
    }
}