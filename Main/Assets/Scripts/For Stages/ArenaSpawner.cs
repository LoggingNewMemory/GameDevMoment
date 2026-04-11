using UnityEngine;
using System.Collections;
using TMPro; 
using UnityEngine.SceneManagement; 

// --- NEW: This creates a beautiful custom menu in the Unity Inspector! ---
[System.Serializable]
public class LevelEnemy
{
    public string editorNote = "Enemy Name"; // Just for your GDD organization!
    public GameObject enemyPrefab;           // Drag the enemy here
    
    [Tooltip("Higher number = spawns more often! (e.g., 80 Jambret, 20 Kaya)")]
    [Range(1, 100)] 
    public int spawnWeight = 50;             // The chance of this enemy spawning
}

public class ArenaSpawner : MonoBehaviour
{
    [Header("GDD: Level Enemy Pool")]
    [Tooltip("Set exactly which enemies belong in THIS specific level!")]
    public LevelEnemy[] enemiesToSpawn;      
    
    [Header("Wave Settings")]
    public int totalEnemiesToSpawn = 250;  
    public int maxAliveAtOnce = 20;        
    public float spawnDelay = 0.5f;        

    [Header("Level Transition")]
    public string nextLevelName = "Level_2"; 
    public float timeBeforeNextLevel = 3f;   

    [Header("Spawn Area (Around Player)")]
    public float minSpawnDistance = 12f;   
    public float maxSpawnDistance = 30f;   

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
                SpawnEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.5f); 
            }
        }
    }

    void SpawnEnemy()
    {
        if (player == null || enemiesToSpawn.Length == 0) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * distance;
        Vector3 spawnPos = player.position + spawnOffset;
        spawnPos.y += 15f; 

        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 30f))
        {
            GameObject chosenEnemy = PickRandomEnemyBasedOnWeight();
            
            if (chosenEnemy != null)
            {
                // --- FIXED: Removed the +0.5f height boost! Now they spawn exactly on the floor! ---
                Instantiate(chosenEnemy, hit.point, Quaternion.identity);
                
                enemiesSpawned++;
                enemiesAlive++;
            }
        }
    }

    // ==========================================
    // NEW: WEIGHTED RANDOM SPAWN LOGIC
    // ==========================================
    GameObject PickRandomEnemyBasedOnWeight()
    {
        int totalWeight = 0;
        // 1. Calculate the total "tickets" in the lottery
        foreach (var enemy in enemiesToSpawn)
        {
            totalWeight += enemy.spawnWeight;
        }

        // 2. Spin the wheel!
        int randomValue = Random.Range(0, totalWeight);

        // 3. See who won the spawn lottery
        foreach (var enemy in enemiesToSpawn)
        {
            if (randomValue < enemy.spawnWeight)
            {
                return enemy.enemyPrefab;
            }
            randomValue -= enemy.spawnWeight;
        }

        // Fallback just in case
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
            enemiesLeftText.text = "REMAINING: " + remaining;
        }
    }
}