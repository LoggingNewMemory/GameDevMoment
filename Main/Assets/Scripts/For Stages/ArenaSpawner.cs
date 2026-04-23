using UnityEngine;
using System.Collections;
using TMPro; 
using UnityEngine.SceneManagement; 

[System.Serializable]
public class LevelEnemy
{
    public string editorNote = "Enemy Name"; 
    public GameObject enemyPrefab;           
    
    [Tooltip("Higher number = spawns more often! (e.g., 80 Jambret, 20 Kaya)")]
    [Range(1, 100)] 
    public int spawnWeight = 50;             
}

public class ArenaSpawner : MonoBehaviour
{
    [Header("GDD: Level Enemy Pool")]
    [Tooltip("Set exactly which enemies belong in THIS specific level!")]
    public LevelEnemy[] enemiesToSpawn;      
    
    [Header("Wave Settings")]
    public int totalEnemiesToSpawn = 250;  
    public int maxAliveAtOnce = 20;        
    
    // --- NEW: BURST SPAWNING SETTINGS ---
    [Header("Burst Settings")]
    public int minSpawnAtOnce = 2;         
    public int maxSpawnAtOnce = 10;        
    public float timeBetweenBursts = 3f; // Replaced spawnDelay to act as a wave delay!

    [Header("Level Transition")]
    public string nextLevelName = "Level_2"; 
    public float timeBeforeNextLevel = 3f;   

    [Header("Spawn Area (Around Player)")]
    public float minSpawnDistance = 12f;   
    public float maxSpawnDistance = 30f;   
    
    public LayerMask floorLayer; 

    [Header("UI & Tracking")]
    public TextMeshProUGUI enemiesLeftText; 
    
    private Transform player;
    private int enemiesSpawned = 0;
    public int enemiesAlive = 0;
    private int enemiesKilled = 0;
    private bool stageCleared = false;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (enemiesLeftText == null)
        {
            GameObject textObj = GameObject.Find("EnemiesLeftText"); 
            if (textObj != null) enemiesLeftText = textObj.GetComponent<TextMeshProUGUI>();
        }

        UpdateUI();
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (enemiesSpawned < totalEnemiesToSpawn)
        {
            if (enemiesAlive < maxAliveAtOnce)
            {
                int enemiesLeftInTotal = totalEnemiesToSpawn - enemiesSpawned;
                int burstAmount = 0;

                // --- THE ULTIMATE FIX: Summon all remaining! ---
                if (enemiesLeftInTotal <= minSpawnAtOnce)
                {
                    // Ignore the map limits and just dump the rest of the wave!
                    burstAmount = enemiesLeftInTotal;
                }
                else
                {
                    // Otherwise, calculate a normal wave
                    int spaceLeftOnMap = maxAliveAtOnce - enemiesAlive;
                    int currentMaxSpawn = Mathf.Min(maxSpawnAtOnce, enemiesLeftInTotal);
                    burstAmount = Random.Range(minSpawnAtOnce, currentMaxSpawn + 1);
                    
                    // Only apply map limits to standard waves
                    burstAmount = Mathf.Min(burstAmount, spaceLeftOnMap);
                }

                // Spawn the burst!
                for (int i = 0; i < burstAmount; i++)
                {
                    SpawnEnemy();
                }

                // Wait before the next wave
                yield return new WaitForSeconds(timeBetweenBursts);
            }
            else
            {
                // Map is completely full! Wait a short moment and check again.
                yield return new WaitForSeconds(1f); 
            }
        }
    }

    void SpawnEnemy()
    {
        if (player == null || enemiesToSpawn.Length == 0) return;

        // --- NEW FIX: Try up to 10 times to find a safe spot! ---
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * distance;

            // 1. Raycast horizontally from the player's chest to check for walls
            Vector3 rayStart = player.position + Vector3.up * 1f; 
            Vector3 rayDir = spawnOffset.normalized;

            // Check if there is a wall blocking the path
            if (Physics.Raycast(rayStart, rayDir, out RaycastHit wallHit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // We hit a wall! Pull the spawn distance back by 1.5 meters (enemy thickness)
                distance = wallHit.distance - 1.5f;
                
                // If pulling it back puts them too close to the player, cancel and try a new random spot
                if (distance < minSpawnDistance) continue; 
                
                spawnOffset = rayDir * distance;
            }

            // 2. Now that we know the X/Z position is safely inside the room, cast DOWN to find the floor
            Vector3 spawnPos = player.position + spawnOffset;
            spawnPos.y += 15f; 

            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 30f, floorLayer))
            {
                GameObject chosenEnemy = PickRandomEnemyBasedOnWeight();
                
                if (chosenEnemy != null)
                {
                    Instantiate(chosenEnemy, hit.point, Quaternion.identity);
                    enemiesSpawned++;
                    enemiesAlive++;
                    return; // Success! Exit the function.
                }
            }
        }
        
        Debug.LogWarning("Spawner couldn't find a safe spot after 10 tries. Room might be too crowded!");
    }

    // ==========================================
    // WEIGHTED RANDOM SPAWN LOGIC
    // ==========================================
    GameObject PickRandomEnemyBasedOnWeight()
    {
        int totalWeight = 0;
        foreach (var enemy in enemiesToSpawn)
        {
            totalWeight += enemy.spawnWeight;
        }

        int randomValue = Random.Range(0, totalWeight);

        foreach (var enemy in enemiesToSpawn)
        {
            if (randomValue < enemy.spawnWeight)
            {
                return enemy.enemyPrefab;
            }
            randomValue -= enemy.spawnWeight;
        }

        return enemiesToSpawn[0].enemyPrefab;
    }

    public void EnemyDefeated()
    {
        if (stageCleared) return;

        enemiesAlive--;
        enemiesKilled++;
        UpdateUI();

        if (enemiesKilled >= totalEnemiesToSpawn)
        {
            stageCleared = true;
            Debug.Log("STAGE CLEARED! LOADING NEXT LEVEL...");
            StartCoroutine(LoadNextLevelRoutine());
        }
    }

    IEnumerator LoadNextLevelRoutine()
    {
        yield return new WaitForSeconds(timeBeforeNextLevel);
        SceneManager.LoadScene(nextLevelName);
    }

    void UpdateUI()
    {
        if (enemiesLeftText != null)
        {
            int remaining = totalEnemiesToSpawn - enemiesKilled;
            enemiesLeftText.text = "Enemy: " + remaining;
        }
    }
}