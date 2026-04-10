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
                // SMART PICKUP (FOOD): If health is full, don't waste food
                if ((typeOfItem == ItemType.IndomieUdon || typeOfItem == ItemType.MacNCheese) && stats.currentHealth >= stats.maxHealth)
                {
                    return; 
                }

                // SMART PICKUP (DRINKS): If overheal is maxed, don't waste drinks
                if ((typeOfItem == ItemType.Extrajoss || typeOfItem == ItemType.VodkaRey) && stats.currentHealth >= stats.maxOverheal)
                {
                    return; 
                }

                switch (typeOfItem)
                {
                    case ItemType.IndomieUdon:
                        stats.HealPercentage(10f, false); 
                        break;
                    case ItemType.MacNCheese:
                        stats.HealPercentage(20f, false);
                        break;
                    case ItemType.Extrajoss:
                        stats.HealPercentage(25f, true); 
                        stats.TriggerUnlimitedEnergy(10f);
                        break;
                    case ItemType.VodkaRey:
                        stats.HealPercentage(15f, true); 
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