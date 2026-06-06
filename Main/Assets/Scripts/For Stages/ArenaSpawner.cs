using UnityEngine;
using System.Collections;
using TMPro; 
using UnityEngine.SceneManagement; 

[System.Serializable]
public class LevelEnemy
{
    public string editorNote = "Enemy Name"; 
    public GameObject enemyPrefab;           
    
    [Range(1, 100)] 
    public int spawnWeight = 50;             
}

public class ArenaSpawner : MonoBehaviour
{
    [Header("Boss Fight Mode")]
    [Tooltip("Drag Shanna in here! The spawner will infinitely spawn normal enemies as supplies until she dies!")]
    public GameObject bossTargetToDefeat; 
    private bool isBossLevel = false;

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

        // AGENT ZETA TACTICS: If a boss is assigned, switch to Infinite Survival Mode!
        if (bossTargetToDefeat != null) isBossLevel = true;

        if (enemiesLeftText == null)
        {
            GameObject textObj = GameObject.Find("EnemiesLeftText"); 
            if (textObj != null) enemiesLeftText = textObj.GetComponent<TextMeshProUGUI>();
        }

        UpdateUI();
        StartCoroutine(SpawnRoutine());
    }

    void Update()
    {
        // If we are in a Boss Level, constantly check if the Boss is dead!
        if (isBossLevel && !stageCleared)
        {
            // If Shanna's GameObject is destroyed or disabled, the mission is complete!
            if (bossTargetToDefeat == null || !bossTargetToDefeat.activeInHierarchy)
            {
                TriggerVictory();
            }
        }
    }

    IEnumerator SpawnRoutine()
    {
        // If it's a Boss Level, spawn infinitely. Otherwise, stop at the normal limit.
        while (isBossLevel || enemiesSpawned < totalEnemiesToSpawn)
        {
            if (stageCleared) yield break; // Stop spawning immediately if we win!

            if (enemiesAlive < maxAliveAtOnce)
            {
                int enemiesLeftInTotal = isBossLevel ? 999 : (totalEnemiesToSpawn - enemiesSpawned);
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
                    
                    BasicChaserAI ai = newEnemy.GetComponent<BasicChaserAI>();
                    if (ai != null) ai.SetTarget(player);

                    enemiesSpawned++;
                    enemiesAlive++;
                    return; 
                }
            }
        }
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

        // Only trigger victory via kill count if we are NOT in a boss level
        if (!isBossLevel && enemiesKilled >= totalEnemiesToSpawn)
        {
            TriggerVictory();
        }
    }

    void TriggerVictory()
    {
        stageCleared = true;
        Debug.Log("<color=cyan>[Agent Zeta] TARGET ELIMINATED! BREACHING NEXT AREA...</color>");
        StartCoroutine(LoadNextLevelRoutine());
    }

    IEnumerator LoadNextLevelRoutine()
    {
        yield return new WaitForSeconds(timeBeforeNextLevel);
        
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutToScene(nextLevelName);
        }
        else
        {
            SceneManager.LoadScene(nextLevelName);
        }
    }

    void UpdateUI()
    {
        if (enemiesLeftText != null)
        {
            if (isBossLevel)
            {
                enemiesLeftText.text = "BOSS ENGAGED!";
            }
            else
            {
                int remaining = totalEnemiesToSpawn - enemiesKilled;
                enemiesLeftText.text = "Enemy Left\n" + remaining;
            }
        }
    }
}