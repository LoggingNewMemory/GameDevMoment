using UnityEngine;
using System.Collections;
using System.Collections.Generic; 
using TMPro; 
using UnityEngine.SceneManagement; 

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

    [HideInInspector] 
    public List<GameObject> activeInstances = new List<GameObject>();
}

public class ArenaSpawner : MonoBehaviour
{
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
    
    // --- AGENT ZETA FIX 1: Look for PlayerStats instead of UniversalHealth! ---
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

    [Header("Level Transition")]
    public string nextLevelName = "Level_2"; 
    public float timeBeforeNextLevel = 3f;   

    [Header("Spawn Area (Around Player)")]
    public float minSpawnDistance = 12f;   
    public float maxSpawnDistance = 30f;   
    
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

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) 
        {
            player = p.transform;
            // --- AGENT ZETA FIX 1 APPLIED ---
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
        // --- AGENT ZETA FIX 2: ALWAYS check if the player is dead, even if there is no boss! ---
        if (!stageCleared && !playerDefeated)
        {
            if (playerStatsScript != null && playerStatsScript.isDead)
            {
                TriggerDefeat();
                return;
            }
        }

        // Only check boss death if it's actually a boss level
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

        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * distance;

            Vector3 rayStart = player.position + Vector3.up * 0.2f; 
            Vector3 rayDir = spawnOffset.normalized;

            if (Physics.Raycast(rayStart, rayDir, out RaycastHit wallHit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                distance = wallHit.distance - 1.5f;
                if (distance < minSpawnDistance) continue; 
                spawnOffset = rayDir * distance;
            }

            Vector3 spawnPos = player.position + spawnOffset;
            spawnPos.y += 2f; 

            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 10f, floorLayer))
            {
                GameObject newEnemy = Instantiate(chosenEnemyData.enemyPrefab, hit.point + (Vector3.up * 0.1f), Quaternion.identity);
                chosenEnemyData.activeInstances.Add(newEnemy);

                BasicChaserAI ai = newEnemy.GetComponent<BasicChaserAI>();
                if (ai != null) ai.SetTarget(player);
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

            if (enemy.maxActiveAtOnce <= 0 || enemy.activeInstances.Count < enemy.maxActiveAtOnce)
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

        if (isBossLevel)
        {
            if (skillToUnlock == BossSkillReward.RageOfCS)
            {
                PlayerPrefs.SetInt("Unlocked_RageOfCS", 1);
                gachaMessage = "BOSS DEFEATED!\n<color=yellow>UNLOCKED: RAGE OF CS</color>";
            }
            else if (skillToUnlock == BossSkillReward.HaluOfCS)
            {
                PlayerPrefs.SetInt("Unlocked_HaluOfCS", 1);
                gachaMessage = "BOSS DEFEATED!\n<color=yellow>UNLOCKED: HALU OF CS</color>";
            }
            else if (skillToUnlock == BossSkillReward.TimeForCoding)
            {
                PlayerPrefs.SetInt("Unlocked_TimeForCoding", 1);
                PlayerPrefs.SetInt("Unlocked_Railgun", 1); 
                gachaMessage = "FINAL BOSS DEFEATED!\n<color=yellow>UNLOCKED: TIME FOR CODING & ANANG RAILGUN</color>";
            }
            else gachaMessage = "BOSS DEFEATED!\n<color=yellow>AREA CLEARED</color>";
        }
        else if (disableGachaReward)
        {
            gachaMessage = "WAVE CLEARED!\n<color=yellow>AREA SECURED</color>";
            Debug.Log("<color=cyan>[Agent Zeta] Tutorial level cleared! No gacha reward issued.</color>");
        }
        else
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

                gachaMessage = $"WAVE CLEARED!\n<color=yellow>UNLOCKED: {displayWeaponName}</color>";
                Debug.Log($"<color=magenta>[Agent Zeta] Gacha Pull: {pulledWeapon} ({displayWeaponName})!</color>");
            }
            else
            {
                gachaMessage = "WAVE CLEARED!\n<color=yellow>ARSENAL MAXED OUT</color>";
                Debug.Log("<color=cyan>[Agent Zeta] Arsenal is full! No new weapons to drop.</color>");
            }
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