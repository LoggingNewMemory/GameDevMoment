using UnityEngine;
using System.Collections;
using System.Collections.Generic; 
using TMPro; 
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public enum BossSkillReward { None, RageOfCS, HaluOfCS, TimeForCoding }

[System.Serializable]
public class LevelEnemy
{
    public string editorNote = "Enemy Name"; 
    public GameObject enemyPrefab;           
    
    [Range(1, 100)] 
    public int spawnWeight = 50;             

    [Header("Difficulty Balance")]
    [Tooltip("Maximum amount of THIS specific enemy alive at once. Set to 0 for unlimited!")]
    public int maxActiveAtOnce = 5; 

    // --- AGENT ZETA TIME-LOCK PROTOCOL ---
    [Tooltip("How many seconds must pass from the start of the level before this enemy is allowed to spawn?")]
    public float spawnDelaySeconds = 0f; 
    // -------------------------------------

    [HideInInspector] 
    public List<GameObject> activeInstances = new List<GameObject>();
}

public class ArenaSpawner : MonoBehaviour
{
    [Header("Agent Zeta: Tactical Spawn Nodes")]
    [Tooltip("Drag your invisible spawn point GameObjects here!")]
    public Transform[] spawnNodes;

    [Header("Boss Fight Mode")]
    public GameObject bossTargetToDefeat; 
    public BossSkillReward skillToUnlock = BossSkillReward.None; 

    [Header("Gacha Settings")]
    [Tooltip("Check this box for Tutorial/Intro levels where you don't want to give a random weapon!")]
    public bool disableGachaReward = false;

    [Header("Defeat Transition (For Bad Endings)")]
    [Tooltip("If the player dies in this boss room, load this scene! Leave empty for normal levels.")]
    public string badEndingSceneName = ""; 
    private bool playerDefeated = false;
    
    private PlayerStats playerStatsScript;

    private bool isBossLevel = false;
    private UniversalHealth bossHealthScript; 

    [Header("GDD: Level Enemy Pool")]
    public LevelEnemy[] enemiesToSpawn;      
    
    [Header("Wave Settings")]
    public int totalEnemiesToSpawn = 250;  
    public int maxAliveAtOnce = 20;        
    
    [Header("Burst Settings")]
    public int minSpawnAtOnce = 2;         
    public int maxSpawnAtOnce = 10;        
    public float timeBetweenBursts = 3f; 

    [Header("Spawn Area (Around Player)")]
    public float minSpawnDistance = 5f;    
    public float maxSpawnDistance = 15f;   

    [Header("Level Transition")]
    public string nextLevelName = "Level_2"; 
    public float timeBeforeNextLevel = 3f;   
    
    public LayerMask floorLayer; 

    [Header("UI & Tracking")]
    public TextMeshProUGUI enemiesLeftText; 
    public GameObject gachaScreen;    
    public TextMeshProUGUI gachaText; 
    
    private Transform player;
    private int enemiesSpawned = 0;
    public int enemiesAlive = 0;
    private int enemiesKilled = 0;
    private bool stageCleared = false;

    // --- AGENT ZETA STOPWATCH ---
    private float levelStartTime;
    // ----------------------------

    void Start()
    {   
        // --- START THE STOPWATCH ---
        levelStartTime = Time.time;
        // ---------------------------

        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedLevel", currentScene);
        PlayerPrefs.Save();
        Debug.Log($"<color=green>[Agent Zeta] Progress auto-saved to: {currentScene}!</color>");
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) 
        {
            player = p.transform;
            playerStatsScript = player.GetComponent<PlayerStats>();
        }

        if (bossTargetToDefeat != null) 
        {
            isBossLevel = true;
            bossHealthScript = bossTargetToDefeat.GetComponent<UniversalHealth>();
        }

        if (enemiesLeftText == null)
        {
            GameObject textObj = GameObject.Find("EnemiesLeftText"); 
            if (textObj != null) enemiesLeftText = textObj.GetComponent<TextMeshProUGUI>();
        }

        if (gachaScreen != null) gachaScreen.SetActive(false);

        UpdateUI();
        StartCoroutine(SpawnRoutine());
    }

    void Update()
    {
        if (!stageCleared && !playerDefeated)
        {
            if (playerStatsScript != null && playerStatsScript.isDead)
            {
                TriggerDefeat();
                return;
            }
        }

        if (isBossLevel && !stageCleared && !playerDefeated)
        {
            bool isBossDead = false;
            if (bossTargetToDefeat == null || !bossTargetToDefeat.activeInHierarchy) isBossDead = true;
            else if (bossHealthScript != null && bossHealthScript.isDead) isBossDead = true;

            if (isBossDead) TriggerVictory();
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (isBossLevel || enemiesSpawned < totalEnemiesToSpawn)
        {
            if (stageCleared || playerDefeated) yield break; 

            if (enemiesAlive < maxAliveAtOnce)
            {
                int enemiesLeftInTotal = isBossLevel ? 999 : (totalEnemiesToSpawn - enemiesSpawned);
                int burstAmount = 0;

                if (enemiesLeftInTotal <= minSpawnAtOnce) burstAmount = enemiesLeftInTotal;
                else
                {
                    int spaceLeftOnMap = maxAliveAtOnce - enemiesAlive;
                    int currentMaxSpawn = Mathf.Min(maxSpawnAtOnce, enemiesLeftInTotal);
                    burstAmount = Random.Range(minSpawnAtOnce, currentMaxSpawn + 1);
                    burstAmount = Mathf.Min(burstAmount, spaceLeftOnMap);
                }

                for (int i = 0; i < burstAmount; i++) SpawnEnemy();
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

        LevelEnemy chosenEnemyData = PickRandomEnemyBasedOnWeight();
        if (chosenEnemyData == null || chosenEnemyData.enemyPrefab == null) return; 

        // =========================================================
        // AGENT ZETA: FOOLPROOF NODE SPAWNING PROTOCOL
        // =========================================================
        if (spawnNodes != null && spawnNodes.Length > 0)
        {
            List<Transform> safeNodes = new List<Transform>();

            // Filter out nodes that are too close or too far from Pria Sigma 1
            foreach (Transform node in spawnNodes)
            {
                float dist = Vector3.Distance(player.position, node.position);
                if (dist >= minSpawnDistance && dist <= maxSpawnDistance)
                {
                    safeNodes.Add(node);
                }
            }

            Transform selectedNode = null;

            // If we found perfect nodes, pick a random one!
            if (safeNodes.Count > 0)
            {
                selectedNode = safeNodes[Random.Range(0, safeNodes.Count)];
            }
            else 
            {
                // Fallback: If player is running wild and no nodes are at the perfect distance, 
                // just pick any node so the wave doesn't get soft-locked!
                selectedNode = spawnNodes[Random.Range(0, spawnNodes.Length)];
            }

            // Spawn directly at the exact coordinates of the invisible node!
            GameObject nodeEnemy = Instantiate(chosenEnemyData.enemyPrefab, selectedNode.position, Quaternion.identity);
            chosenEnemyData.activeInstances.Add(nodeEnemy);

            BasicChaserAI nodeAI = nodeEnemy.GetComponent<BasicChaserAI>();
            if (nodeAI != null) nodeAI.SetTarget(player);
            
            enemiesSpawned++;
            enemiesAlive++;
            return;
        }

        // =========================================================
        // FALLBACK: OLD RANDOM MATH (Only runs if you forget to add nodes!)
        // =========================================================
        for (int attempts = 0; attempts < 30; attempts++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * distance;

            Vector3 simpleSpawnPos = player.position + spawnOffset;
            simpleSpawnPos.y += 2f; 

            if (Physics.Raycast(simpleSpawnPos, Vector3.down, out RaycastHit hitFloor, 15f, floorLayer))
            {
                Vector3 finalSpawnPoint = hitFloor.point + (Vector3.up * 0.1f);

                // Check 1.5: Is this spot actually inside the baked playable area?
                if (!NavMesh.SamplePosition(finalSpawnPoint, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    continue; 
                }
                
                finalSpawnPoint = navHit.position;

                if (Physics.CheckSphere(finalSpawnPoint + (Vector3.up * 1f), 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue; 
                }

                Vector3 playerChest = player.position + (Vector3.up * 1f);
                Vector3 enemyChest = finalSpawnPoint + (Vector3.up * 1f);
                if (Physics.Linecast(playerChest, enemyChest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue; 
                }

                GameObject simpleEnemy = Instantiate(chosenEnemyData.enemyPrefab, finalSpawnPoint, Quaternion.identity);
                chosenEnemyData.activeInstances.Add(simpleEnemy);

                BasicChaserAI simpleAI = simpleEnemy.GetComponent<BasicChaserAI>();
                if (simpleAI != null) simpleAI.SetTarget(player);
                enemiesSpawned++;
                enemiesAlive++;
                return; 
            }
        }
    }

    LevelEnemy PickRandomEnemyBasedOnWeight()
    {
        int totalWeight = 0;
        List<LevelEnemy> validEnemies = new List<LevelEnemy>();

        foreach (var enemy in enemiesToSpawn)
        {
            enemy.activeInstances.RemoveAll(item => item == null || !item.activeInHierarchy);

            // --- AGENT ZETA TIME CHECK ---
            bool isTimeValid = Time.time >= (levelStartTime + enemy.spawnDelaySeconds);

            if (isTimeValid && (enemy.maxActiveAtOnce <= 0 || enemy.activeInstances.Count < enemy.maxActiveAtOnce))
            {
                validEnemies.Add(enemy);
                totalWeight += enemy.spawnWeight;
            }
        }

        if (validEnemies.Count == 0) return null;

        int randomValue = Random.Range(0, totalWeight);

        foreach (var enemy in validEnemies)
        {
            if (randomValue < enemy.spawnWeight) return enemy;
            randomValue -= enemy.spawnWeight;
        }
        
        return validEnemies[0];
    }

    public void EnemyDefeated()
    {
        if (stageCleared || playerDefeated) return;

        enemiesAlive--;
        enemiesKilled++;
        UpdateUI();

        if (!isBossLevel && enemiesKilled >= totalEnemiesToSpawn) TriggerVictory();
    }

    void TriggerDefeat()
    {
        playerDefeated = true;
        Debug.Log("<color=red>[Agent Zeta] AGENT DOWN! EXTRACTING TO BAD ENDING...</color>");
        
        if (!string.IsNullOrEmpty(badEndingSceneName))
        {
            StartCoroutine(LoadDefeatRoutine());
        }
    }

    IEnumerator LoadDefeatRoutine()
    {
        yield return new WaitForSeconds(timeBeforeNextLevel);
        
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeOutToScene(badEndingSceneName);
        else SceneManager.LoadScene(badEndingSceneName);
    }

    public void TriggerVictory()
    {
        stageCleared = true;
        string gachaMessage = "";
        string bossMessage = "";
        string weaponMessage = "";

        // --- 1. BOSS REWARD LOGIC ---
        if (isBossLevel)
        {
            if (skillToUnlock == BossSkillReward.RageOfCS)
            {
                PlayerPrefs.SetInt("Unlocked_RageOfCS", 1);
                bossMessage = "BOSS DEFEATED!\n<color=yellow>UNLOCKED: RAGE OF CS</color>";
            }
            else if (skillToUnlock == BossSkillReward.HaluOfCS)
            {
                PlayerPrefs.SetInt("Unlocked_HaluOfCS", 1);
                bossMessage = "BOSS DEFEATED!\n<color=yellow>UNLOCKED: HALU OF CS</color>";
            }
            else if (skillToUnlock == BossSkillReward.TimeForCoding)
            {
                PlayerPrefs.SetInt("Unlocked_TimeForCoding", 1);
                PlayerPrefs.SetInt("Unlocked_Railgun", 1); 
                bossMessage = "FINAL BOSS DEFEATED!\n<color=yellow>UNLOCKED: TIME FOR CODING & ANANG RAILGUN</color>";
            }
            else bossMessage = "BOSS DEFEATED!\n<color=yellow>AREA CLEARED</color>";
        }

        // --- 2. WEAPON GACHA LOGIC (Runs independently!) ---
        if (!disableGachaReward)
        {
            System.Collections.Generic.List<string> availablePool = new System.Collections.Generic.List<string>();
            
            if (PlayerPrefs.GetInt("Unlocked_Shotgun", 0) == 0) availablePool.Add("Unlocked_Shotgun");
            if (PlayerPrefs.GetInt("Unlocked_SMG", 0) == 0) availablePool.Add("Unlocked_SMG");
            if (PlayerPrefs.GetInt("Unlocked_AssaultRifle", 0) == 0) availablePool.Add("Unlocked_AssaultRifle");
            if (PlayerPrefs.GetInt("Unlocked_Sniper", 0) == 0) availablePool.Add("Unlocked_Sniper");
            if (PlayerPrefs.GetInt("Unlocked_LMG", 0) == 0) availablePool.Add("Unlocked_LMG");

            if (availablePool.Count > 0)
            {
                string pulledWeapon = availablePool[Random.Range(0, availablePool.Count)];
                PlayerPrefs.SetInt(pulledWeapon, 1);
                
                string displayWeaponName = "";
                switch (pulledWeapon)
                {
                    case "Unlocked_Shotgun": displayWeaponName = "FAUZAN SHOTGUN"; break;
                    case "Unlocked_SMG": displayWeaponName = "ARYA - 9 & KRIZZ VECTOR"; break;
                    case "Unlocked_AssaultRifle": displayWeaponName = "SAWUNGGA M4"; break;
                    case "Unlocked_Sniper": displayWeaponName = "PAKCIK KAR-98"; break;
                    case "Unlocked_LMG": displayWeaponName = "KANGKUNG MG-48"; break;
                    default: displayWeaponName = "MYSTERY WEAPON"; break;
                }

                weaponMessage = $"<color=yellow>WEAPON ACQUIRED: {displayWeaponName}</color>";
                Debug.Log($"<color=magenta>[Agent Zeta] Gacha Pull: {pulledWeapon} ({displayWeaponName})!</color>");
            }
            else
            {
                weaponMessage = "<color=yellow>ARSENAL MAXED OUT</color>";
                Debug.Log("<color=cyan>[Agent Zeta] Arsenal is full! No new weapons to drop.</color>");
            }
        }
        else if (!isBossLevel) 
        {
            weaponMessage = "<color=yellow>AREA SECURED</color>";
        }

        // --- 3. MERGE THE MESSAGES ---
        if (isBossLevel)
        {
            gachaMessage = bossMessage;
            if (!disableGachaReward && weaponMessage != "")
            {
                gachaMessage += "\n" + weaponMessage;
            }
        }
        else
        {
            gachaMessage = "WAVE CLEARED!\n" + weaponMessage;
        }
        
        PlayerPrefs.Save(); 

        if (enemiesLeftText != null) enemiesLeftText.gameObject.SetActive(false);
        if (gachaScreen != null) gachaScreen.SetActive(true);
        if (gachaText != null) gachaText.text = gachaMessage;

        StartCoroutine(LoadNextLevelRoutine());
    }

    IEnumerator LoadNextLevelRoutine()
    {
        yield return new WaitForSeconds(timeBeforeNextLevel);
        
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeOutToScene(nextLevelName);
        else SceneManager.LoadScene(nextLevelName);
    }

    void UpdateUI()
    {
        if (enemiesLeftText != null)
        {
            if (isBossLevel) enemiesLeftText.text = "BOSS ENGAGED!";
            else enemiesLeftText.text = "Enemy Left\n" + (totalEnemiesToSpawn - enemiesKilled);
        }
    }
}