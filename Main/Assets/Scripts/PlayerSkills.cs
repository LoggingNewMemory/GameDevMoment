using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro; // REQUIRED FOR TEXTMESHPRO! ✨

public class PlayerSkills : MonoBehaviour
{
    private DoomMovement movement;
    private PlayerStats stats;

    [Header("UI Reference")]
    public TextMeshProUGUI powerStatusText; // Drag "Power Status" text here!

    [Header("Unlocked Skills (Progression)")]
    public bool hasRageOfCS = false;
    public bool hasHaluOfCS = false;
    public bool hasTimeForCoding = false;

    [Header("Rage of CS (Q)")]
    public float rageDuration = 15f;
    public float rageSpeedMultiplier = 1.5f;
    public float rageCooldownMax = 30f;
    [HideInInspector] public bool isRageActive = false;
    private float rageCDTimer = 0f;
    private float rageActiveTimer = 0f;

    [Header("Halu of CS (E)")]
    public float haluBaseDuration = 10f;
    public float haluKillBonus = 0.2f;
    public float haluCooldownMax = 25f;
    [HideInInspector] public bool isHaluActive = false;
    private float haluCDTimer = 0f;
    private float haluTimer = 0f;

    [Header("Time for Coding (F)")]
    public float timeForCodingDuration = 5f; 
    public float slowMotionScale = 0.2f;     
    public float timeCodingCooldownMax = 20f;
    [HideInInspector] public bool isTimeCodingActive = false;
    private float timeCodingCDTimer = 0f;
    private float timeCodingActiveTimer = 0f;

    void Start()
    {
        movement = GetComponent<DoomMovement>();
        stats = GetComponent<PlayerStats>();
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
        // Reductions use unscaledTime so they don't lag during slow-mo!
        if (rageCDTimer > 0) rageCDTimer -= Time.unscaledDeltaTime;
        if (haluCDTimer > 0) haluCDTimer -= Time.unscaledDeltaTime;
        if (timeCodingCDTimer > 0) timeCodingCDTimer -= Time.unscaledDeltaTime;

        // Active countdowns
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

        if (isTimeCodingActive)
        {
            timeCodingActiveTimer -= Time.unscaledDeltaTime;
            if (timeCodingActiveTimer <= 0) EndTimeForCoding();
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
        
        if (hasTimeForCoding && Keyboard.current.fKey.wasPressedThisFrame && !isTimeCodingActive && timeCodingCDTimer <= 0)
        {
            StartTimeForCoding();
        }
    }

    // ==========================================
    // SKILL 1: RAGE OF CS
    // ==========================================
    void StartRageOfCS()
    {
        isRageActive = true;
        rageActiveTimer = rageDuration;
        rageCDTimer = rageCooldownMax; // Cooldown starts instantly upon cast
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
        haluCDTimer = haluCooldownMax; // Cooldown starts after usage ends
        Debug.Log("HALU OF CS ENDED!");
    }

    // ==========================================
    // SKILL 3: TIME FOR CODING
    // ==========================================
    void StartTimeForCoding()
    {
        isTimeCodingActive = true;
        timeCodingActiveTimer = timeForCodingDuration;
        timeCodingCDTimer = timeCodingCooldownMax;

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log("TIME FOR CODING ACTIVATED!");
    }

    void EndTimeForCoding()
    {
        isTimeCodingActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("TIME FOR CODING ENDED!");
    }

    // ==========================================
    // DYNAMIC UI OVERLAY GENERATOR
    // ==========================================
    void UpdateUIIndicator()
    {
        if (powerStatusText == null) return;

        string uiOutput = "";

        // Skill 1 Status Builder
        if (hasRageOfCS)
        {
            uiOutput += "[Q] RAGE OF CS: ";
            if (isRageActive) uiOutput += $"<color=orange>ACTIVE ({rageActiveTimer:F1}s)</color>\n";
            else if (rageCDTimer > 0) uiOutput += $"<color=red>CD ({rageCDTimer:F1}s)</color>\n";
            else uiOutput += "<color=green>READY</color>\n";
        }

        // Skill 2 Status Builder
        if (hasHaluOfCS)
        {
            uiOutput += "[E] HALU OF CS: ";
            if (isHaluActive) uiOutput += $"<color=#00FFFF>ACTIVE ({haluTimer:F1}s)</color>\n";
            else if (haluCDTimer > 0) uiOutput += $"<color=red>CD ({haluCDTimer:F1}s)</color>\n";
            else uiOutput += "<color=green>READY</color>\n";
        }

        // Skill 3 Status Builder
        if (hasTimeForCoding)
        {
            uiOutput += "[F] TIME FOR CODING: ";
            if (isTimeCodingActive) uiOutput += $"<color=yellow>SLOW-MO ({timeCodingActiveTimer:F1}s)</color>\n";
            else if (timeCodingCDTimer > 0) uiOutput += $"<color=red>CD ({timeCodingCDTimer:F1}s)</color>\n";
            else uiOutput += "<color=green>READY</color>\n";
        }

        powerStatusText.text = uiOutput;
    }

    // Unlock logic overrides remain intact...
    public void UnlockRageOfCS() { hasRageOfCS = true; }
    public void UnlockHaluOfCS() { hasHaluOfCS = true; }
    public void UnlockTimeForCoding() { hasTimeForCoding = true; }
}