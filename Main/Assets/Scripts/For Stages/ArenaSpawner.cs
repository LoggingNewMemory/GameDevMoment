using UnityEngine;
using System.Collections;
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
}

public class ArenaSpawner : MonoBehaviour
{
    [Header("Boss Fight Mode")]
    public GameObject bossTargetToDefeat; 
    public BossSkillReward skillToUnlock = BossSkillReward.None; 

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
        if (p != null) player = p.transform;

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
        if (isBossLevel && !stageCleared)
        {
            bool isBossDead = false;

            if (bossTargetToDefeat == null || !bossTargetToDefeat.activeInHierarchy)
            {
                isBossDead = true;
            }
            else if (bossHealthScript != null && bossHealthScript.isDead)
            {
                isBossDead = true;
            }

            if (isBossDead)
            {
                TriggerVictory();
            }
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (isBossLevel || enemiesSpawned < totalEnemiesToSpawn)
        {
            if (stageCleared) yield break; 

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

        if (!isBossLevel && enemiesKilled >= totalEnemiesToSpawn)
        {
            TriggerVictory();
        }
    }

    void TriggerVictory()
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
                gachaMessage = "BOSS DEFEATED!\n<color=yellow>UNLOCKED: TIME FOR CODING</color>";
            }
            else
            {
                gachaMessage = "BOSS DEFEATED!\n<color=yellow>AREA CLEARED</color>";
            }
            
            Debug.Log($"<color=yellow>[Agent Zeta] Boss Defeated! Reward: {skillToUnlock}</color>");
        }
        else
        {
            string[] gachaPool = { "Unlocked_Shotgun", "Unlocked_SMG", "Unlocked_AssaultRifle", "Unlocked_Sniper", "Unlocked_LMG" };
            string pulledWeapon = gachaPool[Random.Range(0, gachaPool.Length)];
            
            PlayerPrefs.SetInt(pulledWeapon, 1);
            
            // --- AGENT ZETA TACTICS: CUSTOM WEAPON NAMES! ---
            string displayWeaponName = "";
            switch (pulledWeapon)
            {
                case "Unlocked_Shotgun": displayWeaponName = "FAUZAN SHOTGUN"; break;
                case "Unlocked_SMG": displayWeaponName = "ARYA - 9 & KRIZZ VECTOR"; break;
                case "Unlocked_AssaultRifle": displayWeaponName = "SAWUNGGA M4"; break;
                case "Unlocked_Sniper": displayWeaponName = "PAKCIK KAR-98"; break;
                case "Unlocked_LMG": displayWeaponName = "KANGKUNG DP-28"; break;
                default: displayWeaponName = "MYSTERY WEAPON"; break;
            }

            gachaMessage = $"WAVE CLEARED!\n<color=yellow>UNLOCKED: {displayWeaponName}</color>";
            // ------------------------------------------------

            Debug.Log($"<color=magenta>[Agent Zeta] Gacha Pull: {pulledWeapon} ({displayWeaponName})!</color>");
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
            if (isBossLevel) enemiesLeftText.text = "BOSS ENGAGED!";
            else enemiesLeftText.text = "Enemy Left\n" + (totalEnemiesToSpawn - enemiesKilled);
        }
    }
}