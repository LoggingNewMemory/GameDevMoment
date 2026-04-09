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
        SphereCollider triggerBubble = gameObject.AddComponent<SphereCollider>();
        triggerBubble.isTrigger = true;
        
        float scaleFix = Mathf.Abs(transform.localScale.x);
        if (scaleFix < 0.01f) scaleFix = 0.01f;
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
            PlayerStats stats = playerObject.GetComponent<PlayerStats>();

            if (stats != null)
            {
                // SMART PICKUP: If health is full, don't let them accidentally eat food and waste it!
                if ((typeOfItem == ItemType.IndomieUdon || typeOfItem == ItemType.MacNCheese) && stats.currentHealth >= stats.maxHealth)
                {
                    return; // Abort pickup. Leave it on the floor!
                }

                switch (typeOfItem)
                {
                    // Food: FALSE (Caps at 100)
                    case ItemType.IndomieUdon:
                        stats.HealPercentage(10f, false); 
                        break;
                    case ItemType.MacNCheese:
                        stats.HealPercentage(20f, false);
                        break;
                    
                    // Drinks: TRUE (Allows Overheal up to 125!)
                    case ItemType.Extrajoss:
                        stats.HealPercentage(5f, true);
                        stats.TriggerUnlimitedEnergy(10f);
                        break;
                    case ItemType.VodkaRey:
                        stats.HealPercentage(2f, true);
                        stats.TriggerUnlimitedEnergy(7f);
                        stats.DrinkVodka(cheekiBreeki, 7f);
                        break;
                }

                if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                
                Destroy(gameObject);
            }
        }
    }
}