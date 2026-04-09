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

        // --- FIXED PROBABILITIES ---
        // Currently set to exactly 20% each. 
        if (roll <= 20) 
        {
            itemToDrop = udonPrefab;         // 20% chance (1-20)
        }
        else if (roll <= 40) 
        {
            itemToDrop = macNCheesePrefab;   // 20% chance (21-40)
        }
        else if (roll <= 60) 
        {
            itemToDrop = ammoBoxPrefab;      // 20% chance (41-60)
        }
        else if (roll <= 80) 
        {
            itemToDrop = vodkaPrefab;        // 20% chance (61-80)
        }
        else 
        {
            itemToDrop = extraJossPrefab;    // 20% chance (81-100)
        }

        // Spawn the item if the slot isn't empty
        if (itemToDrop != null)
        {
            Instantiate(itemToDrop, transform.position + Vector3.up, Quaternion.identity);
        }
    }
}