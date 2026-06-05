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
    public LevelEnemy[] enemiesToSpawn;      
    
    [Header("Wave Settings")]
    public int totalEnemiesToSpawn = 250;  
    public int maxAliveAtOnce = 20;        
    
    [Header("Burst Settings")]
    public int minSpawnAtOnce = 2;         
    public int maxSpawnAtOnce = 10;        
    public float timeBetweenBursts = 3f; 

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

                if (enemiesLeftInTotal <= minSpawnAtOnce)
                {
                    burstAmount = enemiesLeftInTotal;
                }
                else
                {
                    int spaceLeftOnMap = maxAliveAtOnce - enemiesAlive;
                    int currentMaxSpawn = Mathf.Min(maxSpawnAtOnce, enemiesLeftInTotal);
                    burstAmount = Random.Range(minSpawnAtOnce, currentMaxSpawn + 1);
                    burstAmount = Mathf.Min(burstAmount, spaceLeftOnMap);
                }

                for (int i = 0; i < burstAmount; i++)
                {
                    SpawnEnemy();
                }

                yield return new WaitForSeconds(timeBetweenBursts);
            }
            else
            {
                yield return new WaitForSeconds(1f); 
            }
        }
    }

    void SpawnEnemy()
    {
        if (player == null || enemiesToSpawn.Length == 0) return;

        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * distance;

            Vector3 rayStart = player.position + Vector3.up * 1f; 
            Vector3 rayDir = spawnOffset.normalized;

            if (Physics.Raycast(rayStart, rayDir, out RaycastHit wallHit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                distance = wallHit.distance - 1.5f;
                if (distance < minSpawnDistance) continue; 
                spawnOffset = rayDir * distance;
            }

            Vector3 spawnPos = player.position + spawnOffset;
            spawnPos.y += 15f; 

            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 30f, floorLayer))
            {
                GameObject chosenEnemy = PickRandomEnemyBasedOnWeight();
                
                if (chosenEnemy != null)
                {
                    GameObject newEnemy = Instantiate(chosenEnemy, hit.point, Quaternion.identity);
                    
                    // KOBO OPTIMIZATION: Inject the player target instantly!
                    BasicChaserAI ai = newEnemy.GetComponent<BasicChaserAI>();
                    if (ai != null) ai.SetTarget(player);

                    enemiesSpawned++;
                    enemiesAlive++;
                    return; 
                }
            }
        }
        
        Debug.LogWarning("Spawner couldn't find a safe spot after 10 tries. Room might be too crowded!");
    }

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
            enemiesLeftText.text = "Enemy Left\n" + remaining;
        }
    }
}