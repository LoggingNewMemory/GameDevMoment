using UnityEngine;

public class RandomAmmoBox : MonoBehaviour
{
    [Header("Loot Settings")]
    public bool isCompletelyRandom = true;
    public AmmoType myAmmoType;
    public int ammoInside = 0;

    [Header("Audio")]
    public AudioClip pickupSound;

    void Start()
    {
        if (isCompletelyRandom)
        {
            myAmmoType = (AmmoType)Random.Range(0, 6);
        }

        switch (myAmmoType)
        {
            case AmmoType.Pistol: ammoInside = Random.Range(10, 20); break;
            case AmmoType.Shotgun: ammoInside = Random.Range(4, 9); break;
            case AmmoType.SMG: ammoInside = Random.Range(30, 50); break;
            case AmmoType.AssaultRifle: ammoInside = Random.Range(20, 31); break;
            case AmmoType.SniperOrLMG: ammoInside = Random.Range(10, 20); break;
            case AmmoType.Railgun: ammoInside = Random.Range(1, 4); break;
        }

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
                // Backpack firmly decides if we pick it up based on ownership
                bool wasPickedUp = ammoStore.AddAmmo(myAmmoType, ammoInside);
                
                if (wasPickedUp)
                {
                    if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                    Destroy(gameObject);
                }
            }
        }
    }
}