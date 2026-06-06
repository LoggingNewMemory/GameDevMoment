using UnityEngine;
using UnityEngine.InputSystem;

public class AgentEMPCheat : MonoBehaviour
{
    void Update()
    {
        // PRESS 'K' TO NUKE THE ENTIRE MAP AND END THE WAVE!
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("<color=red>[Agent Zeta] EMP DETONATED! Hacking the Spawner...</color>");
            
            // 1. Hack the Spawner so it thinks the wave is over!
            ArenaSpawner spawner = Object.FindAnyObjectByType<ArenaSpawner>();
            if (spawner != null)
            {
                // Empty the dropship reserves!
                spawner.totalEnemiesToSpawn = 0; 
            }

            // 2. Wipe the current active hostiles on the map
            UniversalHealth[] allEntities = Object.FindObjectsByType<UniversalHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            foreach (UniversalHealth entity in allEntities)
            {
                // Instantly flatline them, as long as it isn't Pria Sigma 1!
                if (!entity.gameObject.CompareTag("Player"))
                {
                    entity.TakeDamage(9999f);
                }
            }
        }
    }
}