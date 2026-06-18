using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AgentZetaBalancer
{
    // Creates a new button in the Tools menu!
    [MenuItem("Tools/Agent Zeta/Auto-Balance Enemy Spawners")]
    public static void BalanceSpawners()
    {
        // 1. Find all Arena Spawners in the current level
        ArenaSpawner[] allSpawners = Object.FindObjectsByType<ArenaSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        int spawnerCount = 0;

        // 2. Loop through every spawner and hack the enemy stats based on their names
        foreach (ArenaSpawner spawner in allSpawners)
        {
            if (spawner.enemiesToSpawn == null) continue;

            foreach (LevelEnemy enemy in spawner.enemiesToSpawn)
            {
                // Grab the name from either the Editor Note or the Prefab Name to make sure we find it
                string nameToCheck = enemy.editorNote.ToLower();
                if (enemy.enemyPrefab != null) nameToCheck += enemy.enemyPrefab.name.ToLower();

                // --- AGENT ZETA BALANCE INJECTIONS ---
                if (nameToCheck.Contains("jambret"))
                {
                    enemy.spawnWeight = 60;
                    enemy.maxActiveAtOnce = 15;
                    enemy.spawnDelaySeconds = 0f;
                }
                else if (nameToCheck.Contains("black"))
                {
                    enemy.spawnWeight = 25;
                    enemy.maxActiveAtOnce = 5;
                    enemy.spawnDelaySeconds = 15f;
                }
                else if (nameToCheck.Contains("kaya"))
                {
                    enemy.spawnWeight = 10;
                    enemy.maxActiveAtOnce = 2;
                    enemy.spawnDelaySeconds = 30f;
                }
                else if (nameToCheck.Contains("stejew"))
                {
                    enemy.spawnWeight = 5;
                    enemy.maxActiveAtOnce = 2;
                    enemy.spawnDelaySeconds = 45f;
                }
                else if (nameToCheck.Contains("america") || nameToCheck.Contains("woke"))
                {
                    enemy.spawnWeight = 5;
                    enemy.maxActiveAtOnce = 1;
                    enemy.spawnDelaySeconds = 60f;
                }
            }
            
            // Tell Unity we modified the spawner so it saves
            EditorUtility.SetDirty(spawner);
            spawnerCount++;
        }

        // Force the scene to recognize the unsaved changes
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        Debug.Log($"<color=cyan>[Agent Zeta] BALANCING COMPLETE! {spawnerCount} spawners auto-configured to the Matrix parameters!</color>");
    }
}