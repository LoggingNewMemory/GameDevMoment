using UnityEngine;
using System.Collections;
using TMPro; // For the UI Counter

public class ArenaSpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject jambretPrefab;       // Drag your "Jambret 1" prefab here!
    public int totalEnemiesToSpawn = 250;  // Total for the stage
    public int maxAliveAtOnce = 20;        // How many can swarm you at once (Keeps FPS high!)
    public float spawnDelay = 0.5f;        // Time between individual spawns

    [Header("Spawn Area (Around Player)")]
    public float minSpawnDistance = 12f;   // So they don't spawn directly on your head
    public float maxSpawnDistance = 30f;   // So they don't spawn miles away

    [Header("UI & Tracking")]
    public TextMeshProUGUI enemiesLeftText; // Drag a UI text here to show the countdown
    
    private Transform player;
    private int enemiesSpawned = 0;
    public int enemiesAlive = 0;
    private int enemiesKilled = 0;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        UpdateUI();
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // Keep looping until we have pumped 250 enemies into the map
        while (enemiesSpawned < totalEnemiesToSpawn)
        {
            // Only spawn a new one if we haven't hit the screen limit
            if (enemiesAlive < maxAliveAtOnce)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }
            else
            {
                // Wait for the player to kill some before trying again
                yield return new WaitForSeconds(0.5f); 
            }
        }
    }

    void SpawnEnemy()
    {
        if (player == null) return;

        // 1. Pick a random angle (0 to 360 degrees around the player)
        float angle = Random.Range(0f, 360f);
        
        // 2. Pick a random distance between the Min and Max radius
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        // 3. Do the math to find that exact X/Z coordinate
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
        Vector3 spawnPos = player.position + spawnOffset;
        
        // 4. Drop them from the sky to find the floor!
        // We push the start point 10 meters up, and shoot a laser down to find the exact floor height.
        spawnPos.y += 10f; 

        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 30f))
        {
            // Spawn the Jambret exactly where the laser hit the floor!
            Instantiate(jambretPrefab, hit.point + Vector3.up * 0.5f, Quaternion.identity);
            
            enemiesSpawned++;
            enemiesAlive++;
        }
    }

    // --- The enemies will call this method when they die! ---
    public void EnemyDefeated()
    {
        enemiesAlive--;
        enemiesKilled++;
        UpdateUI();

        if (enemiesKilled >= totalEnemiesToSpawn)
        {
            Debug.Log("STAGE 1 CLEARED! ALL 250 DEFEATED!");
            // Later we can add a Door Opening or Level Load script right here!
        }
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