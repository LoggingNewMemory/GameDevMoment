using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro; 

public class PlayerSkills : MonoBehaviour
{
    private DoomMovement movement;
    private PlayerStats stats;

    [Header("UI Reference")]
    public TextMeshProUGUI powerStatusText; 

    [Header("Unlocked Skills (Progression)")]
    public bool hasRageOfCS = false;
    public bool hasHaluOfCS = false;
    public bool hasTimeForCoding = false;

    [Header("Rage of CS (Q)")]
    public float rageDuration = 5f;          // Sui-chan nerfed this from 8f!
    public float rageSpeedMultiplier = 1.5f;
    public float rageCooldownMax = 40f;      // Sui-chan extended this from 30f!
    [HideInInspector] public bool isRageActive = false;
    private float rageCDTimer = 0f;
    private float rageActiveTimer = 0f;

    [Header("Halu of CS (E)")]
    public float haluBaseDuration = 8f;      // Sui-chan nerfed this from 12f!
    public float haluKillBonus = 0.2f;
    public float haluCooldownMax = 35f;      // Sui-chan extended this from 25f!
    [HideInInspector] public bool isHaluActive = false;
    private float haluCDTimer = 0f;
    private float haluTimer = 0f;

    [Header("Time for Coding / Sandevistan Mode (F)")]
    public float timeForCodingDuration = 10f; // Max capacity reduced from 17f!
    public float slowMotionScale = 0.2f;     
    public float timeCodingCooldownMax = 55f; // Time to fully recharge extended from 40f!
    [HideInInspector] public bool isTimeCodingActive = false;
    private float timeCodingActiveTimer = 0f; // Current remaining charge pool
    public float minChargeToActivate = 2f;    // Minimum charge required to flip it back on

    void Start()
    {
        movement = GetComponent<DoomMovement>();
        stats = GetComponent<PlayerStats>();
        
        // --- AGENT ZETA SKILL RETRIEVAL ---
        if (PlayerPrefs.GetInt("Unlocked_RageOfCS", 0) == 1) hasRageOfCS = true; 
        if (PlayerPrefs.GetInt("Unlocked_HaluOfCS", 0) == 1) hasHaluOfCS = true; 
        if (PlayerPrefs.GetInt("Unlocked_TimeForCoding", 0) == 1) hasTimeForCoding = true; 
        // ----------------------------------

        // Initialize Sandevistan charge pool to maximum capacity
        timeCodingActiveTimer = timeForCodingDuration;
    }

    void Update()
    {
        HandleCooldownTimers();
        HandleSkillInputs();
        UpdateUIIndicator();
    }

    // --- COOLDOWN & DURATION MANAGEMENT ---
    void HandleCooldownTimers()
    {
        if (rageCDTimer > 0) rageCDTimer -= Time.unscaledDeltaTime;
        if (haluCDTimer > 0) haluCDTimer -= Time.unscaledDeltaTime;

        if (isRageActive)
        {
            rageActiveTimer -= Time.unscaledDeltaTime;
            if (rageActiveTimer <= 0) EndRageOfCS();
        }

        if (isHaluActive)
        {
            haluTimer -= Time.unscaledDeltaTime;
            if (haluTimer <= 0) CancelHaluOfCS();
        }

        // --- SANDEVISTAN RESOURCE POOL TICKER ---
        if (isTimeCodingActive)
        {
            // Drain the pool while executing code in slow-mo!
            timeCodingActiveTimer -= Time.unscaledDeltaTime;
            if (timeCodingActiveTimer <= 0)
            {
                timeCodingActiveTimer = 0;
                EndTimeForCoding();
            }
        }
        else
        {
            // Recharge the pool naturally when deactivated
            if (timeCodingActiveTimer < timeForCodingDuration)
            {
                float rechargeRate = timeForCodingDuration / timeCodingCooldownMax;
                timeCodingActiveTimer += Time.unscaledDeltaTime * rechargeRate;
                if (timeCodingActiveTimer > timeForCodingDuration) 
                    timeCodingActiveTimer = timeForCodingDuration;
            }
        }
    }

    // --- INPUT ROUTER ---
    void HandleSkillInputs()
    {
        if (Keyboard.current == null) return;

        if (hasRageOfCS && Keyboard.current.qKey.wasPressedThisFrame && !isRageActive && rageCDTimer <= 0)
        {
            StartRageOfCS();
        }
        
        if (hasHaluOfCS && Keyboard.current.eKey.wasPressedThisFrame && !isHaluActive && haluCDTimer <= 0)
        {
            StartHaluOfCS();
        }
        
        // --- TOGGLEABLE SANDEVISTAN TRIGGER ---
        if (hasTimeForCoding && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (isTimeCodingActive)
            {
                // Active? Shut it down early to preserve the remaining pool!
                EndTimeForCoding();
            }
            else if (timeCodingActiveTimer >= minChargeToActivate)
            {
                // Inactive? Turn it back on as long as you have met the minimum charge requirement!
                StartTimeForCoding();
            }
        }
    }

    // ==========================================
    // SKILL 1: RAGE OF CS
    // ==========================================
    void StartRageOfCS()
    {
        isRageActive = true;
        rageActiveTimer = rageDuration;
        rageCDTimer = rageCooldownMax; 
        if (movement != null) movement.speedMultiplier = rageSpeedMultiplier;
        Debug.Log("RAGE OF CS ACTIVATED!");
    }

    void EndRageOfCS()
    {
        isRageActive = false;
        if (movement != null) movement.speedMultiplier = 1f;
        Debug.Log("RAGE OF CS ENDED!");
    }

    // ==========================================
    // SKILL 2: HALU OF CS
    // ==========================================
    public void StartHaluOfCS()
    {
        isHaluActive = true;
        haluTimer = haluBaseDuration;
        Debug.Log("HALU OF CS ACTIVATED!");
    }

    public void AddHaluKillBonus()
    {
        if (isHaluActive)
        {
            haluTimer += haluKillBonus;
        }
    }

    public void CancelHaluOfCS()
    {
        if (!isHaluActive) return;
        isHaluActive = false;
        haluCDTimer = haluCooldownMax; 
        Debug.Log("HALU OF CS ENDED!");
    }

    // ==========================================
    // SKILL 3: TIME FOR CODING (SANDEVISTAN OVERCLOCK)
    // ==========================================
    void StartTimeForCoding()
    {
        isTimeCodingActive = true;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log("SANDEVISTAN ACTIVATED: TIME FOR CODING!");
    }

    void EndTimeForCoding()
    {
        isTimeCodingActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("SANDEVISTAN DEACTIVATED: RETURN TO NORMAL TIME!");
    }

    // ==========================================
    // SUI-CHAN'S KILL REWARD PROTOCOL (RELOADED)
    // ==========================================
    public void ApplyKillCooldownReduction()
    {
        float cdReduction = 0.01f; // Brutally nerfed from 0.2f to 0.01f! 🪓
        float sandevistanBatteryRefill = 1.5f; // Getting kills gives you instant Sandevistan charge! 

        // Slash those standard cooldowns (just a tiny scratch now!)
        if (rageCDTimer > 0) rageCDTimer = Mathf.Max(0, rageCDTimer - cdReduction);
        if (haluCDTimer > 0) haluCDTimer = Mathf.Max(0, haluCDTimer - cdReduction);
        
        // Refill your coding energy pool for being a clean executioner!
        if (!isTimeCodingActive)
        {
            timeCodingActiveTimer = Mathf.Min(timeForCodingDuration, timeCodingActiveTimer + sandevistanBatteryRefill);
        }
        
        // Keep your original duration bonus for Halu active too!
        AddHaluKillBonus();
    }

    // ==========================================
    // DYNAMIC UI OVERLAY GENERATOR
    // ==========================================
    void UpdateUIIndicator()
    {
        if (powerStatusText == null) return;

        string uiOutput = "";

        if (hasRageOfCS)
        {
            uiOutput += "[Q] RAGE OF CS: ";
            if (isRageActive) uiOutput += $"<color=orange>ACTIVE ({rageActiveTimer:F1}s)</color>\n";
            else if (rageCDTimer > 0) uiOutput += $"<color=red>CD ({rageCDTimer:F1}s)</color>\n";
            else uiOutput += "<color=green>READY</color>\n";
        }

        if (hasHaluOfCS)
        {
            uiOutput += "[E] HALU OF CS: ";
            if (isHaluActive) uiOutput += $"<color=#00FFFF>ACTIVE ({haluTimer:F1}s)</color>\n";
            else if (haluCDTimer > 0) uiOutput += $"<color=red>CD ({haluCDTimer:F1}s)</color>\n";
            else uiOutput += "<color=green>READY</color>\n";
        }

        if (hasTimeForCoding)
        {
            uiOutput += "[F] TIME FOR CODING: ";
            float chargePercent = (timeCodingActiveTimer / timeForCodingDuration) * 100f;

            if (isTimeCodingActive)
            {
                uiOutput += $"<color=yellow>BURNING ({timeCodingActiveTimer:F1}s / {chargePercent:F0}%)</color>\n";
            }
            else
            {
                if (timeCodingActiveTimer >= minChargeToActivate)
                {
                    uiOutput += $"<color=green>READY ({timeCodingActiveTimer:F1}s / {chargePercent:F0}%)</color>\n";
                }
                else
                {
                    uiOutput += $"<color=red>CHARGING ({timeCodingActiveTimer:F1}s / {chargePercent:F0}%)</color>\n";
                }
            }
        }

        powerStatusText.text = uiOutput;
    }

    public void UnlockRageOfCS() { hasRageOfCS = true; }
    public void UnlockHaluOfCS() { hasHaluOfCS = true; }
    public void UnlockTimeForCoding() { hasTimeForCoding = true; }
}