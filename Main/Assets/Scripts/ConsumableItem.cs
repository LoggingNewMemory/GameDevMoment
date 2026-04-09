using UnityEngine;

public enum ItemType { IndomieUdon, Extrajoss, VodkaRey, MacNCheese }

public class ConsumableItem : MonoBehaviour
{
    public ItemType typeOfItem;

    [Header("Audio")]
    public AudioClip pickupSound; 
    public AudioClip cheekiBreeki; 

    void Start()
    {
        // THE MAGIC FIX: Automatically create a massive invisible Trigger bubble via code!
        SphereCollider triggerBubble = gameObject.AddComponent<SphereCollider>();
        triggerBubble.isTrigger = true;
        
        // Because your Vodka is scaled to 0.07, but your Udon is 1.0, 
        // this math guarantees the bubble is always the exact same human-sized 
        // area in the world, preventing the player from just stepping over it!
        float scaleFix = Mathf.Abs(transform.localScale.x);
        if (scaleFix < 0.01f) scaleFix = 0.01f;
        triggerBubble.radius = 1.5f / scaleFix;
    }

    void OnTriggerEnter(Collider other)
    {
        CheckPickup(other.gameObject);
    }

    // Adding Stay just in case the item spawns directly inside the player!
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
            PlayerStats stats = playerObject.GetComponent<PlayerStats>();

            if (stats != null)
            {
                switch (typeOfItem)
                {
                    case ItemType.IndomieUdon:
                        stats.HealPercentage(10f);
                        break;
                    case ItemType.Extrajoss:
                        stats.HealPercentage(5f);
                        stats.TriggerUnlimitedEnergy(10f);
                        break;
                    case ItemType.VodkaRey:
                        stats.HealPercentage(2f);
                        stats.TriggerUnlimitedEnergy(7f);
                        stats.DrinkVodka(cheekiBreeki, 7f);
                        break;
                    case ItemType.MacNCheese:
                        stats.HealPercentage(20f);
                        break;
                }

                if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                
                Destroy(gameObject);
            }
        }
    }
}