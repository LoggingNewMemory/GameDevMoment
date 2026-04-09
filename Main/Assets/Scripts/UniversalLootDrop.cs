using UnityEngine;

public class UniversalLootDrop : MonoBehaviour
{
    [Header("Loot Prefabs (Just drag and drop!)")]
    public GameObject udonPrefab;
    public GameObject macNCheesePrefab;
    public GameObject ammoBoxPrefab;
    public GameObject vodkaPrefab;
    public GameObject extraJossPrefab;

    public void DropLoot()
    {
        // Roll a number between 1 and 100
        int roll = Random.Range(1, 101);

        GameObject itemToDrop = null;

        // --- NEW PROBABILITIES ---
        if (roll <= 10) 
        {
            itemToDrop = udonPrefab;         // 10% chance (Rolls 1 to 10)
        }
        else if (roll <= 25) 
        {
            itemToDrop = macNCheesePrefab;   // 15% chance (Rolls 11 to 25)
        }
        else if (roll <= 75) 
        {
            itemToDrop = ammoBoxPrefab;      // 50% chance (Rolls 26 to 75) <-- MASSIVE BOOST
        }
        else if (roll <= 85) 
        {
            itemToDrop = vodkaPrefab;        // 10% chance (Rolls 76 to 85)
        }
        else 
        {
            itemToDrop = extraJossPrefab;    // 15% chance (Rolls 86 to 100)
        }

        // Spawn the item if the slot isn't empty
        if (itemToDrop != null)
        {
            Instantiate(itemToDrop, transform.position + Vector3.up, Quaternion.identity);
        }
    }
}