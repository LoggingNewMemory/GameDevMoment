using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerSkills : MonoBehaviour
{
    private DoomMovement movement;
    private PlayerStats stats;

    // --- NEW: SKILL UNLOCK SYSTEM ---
    [Header("Unlocked Skills (Progression)")]
    public bool hasRageOfCS = false;
    public bool hasHaluOfCS = false;
    public bool hasTimeForCoding = false;

    [Header("Rage of CS (Q)")]
    public float rageDuration = 15f;
    public float rageSpeedMultiplier = 1.5f;
    public bool isRageActive = false;

    [Header("Halu of CS (E)")]
    public float haluBaseDuration = 10f;
    public float haluKillBonus = 0.2f;
    private float haluTimer = 0f;
    public bool isHaluActive = false;

    [Header("Time for Coding (F)")]
    public float timeForCodingDuration = 5f; // How long real-time the slow motion lasts
    public float slowMotionScale = 0.2f;     // 0.2 means the world runs at 20% speed
    public bool isTimeCodingActive = false;

    void Start()
    {
        movement = GetComponent<DoomMovement>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // --- SKILL ACTIVATIONS (Now checks if you actually own them!) ---
        if (hasRageOfCS && Keyboard.current.qKey.wasPressedThisFrame && !isRageActive)
        {
            StartCoroutine(RageOfCSRoutine());
        }
        
        if (hasHaluOfCS && Keyboard.current.eKey.wasPressedThisFrame && !isHaluActive)
        {
            StartHaluOfCS();
        }
        
        if (hasTimeForCoding && Keyboard.current.fKey.wasPressedThisFrame && !isTimeCodingActive)
        {
            StartCoroutine(TimeForCodingRoutine());
        }

        // --- HALU OF CS DRAIN ---
        // Uses unscaled time so it drains correctly even during Sandevistan!
        if (isHaluActive)
        {
            haluTimer -= Time.unscaledDeltaTime;
            if (haluTimer <= 0)
            {
                CancelHaluOfCS();
            }
        }
    }

    // ==========================================
    // SKILL 1: RAGE OF CS (SPEED & RELOAD)
    // ==========================================
    IEnumerator RageOfCSRoutine()
    {
        isRageActive = true;
        Debug.Log("RAGE OF CS ACTIVATED! Speed & Reload Increased!");

        if (movement != null) movement.speedMultiplier = rageSpeedMultiplier;
        
        yield return new WaitForSecondsRealtime(rageDuration);

        if (movement != null) movement.speedMultiplier = 1f;
        isRageActive = false;
        
        Debug.Log("RAGE OF CS ENDED!");
    }

    // ==========================================
    // SKILL 2: HALU OF CS (DUAL WIELD)
    // ==========================================
    public void StartHaluOfCS()
    {
        isHaluActive = true;
        haluTimer = haluBaseDuration;
        Debug.Log("HALU OF CS ACTIVATED! Dual Wielding!");
    }

    public void AddHaluKillBonus()
    {
        if (isHaluActive)
        {
            haluTimer += haluKillBonus;
            Debug.Log("Halu extended! Time left: " + haluTimer);
        }
    }

    public void CancelHaluOfCS()
    {
        if (!isHaluActive) return;
        isHaluActive = false;
        Debug.Log("HALU OF CS ENDED/CANCELED!");
    }

    // ==========================================
    // SKILL 3: TIME FOR CODING (SANDEVISTAN)
    // ==========================================
    IEnumerator TimeForCodingRoutine()
    {
        isTimeCodingActive = true;
        Debug.Log("TIME FOR CODING ACTIVATED! The world slows down...");

        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(timeForCodingDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        isTimeCodingActive = false;
        Debug.Log("TIME FOR CODING ENDED!");
    }

    // ==========================================
    // PUBLIC UNLOCK FUNCTIONS (Call these from items or level managers!)
    // ==========================================
    public void UnlockRageOfCS()
    {
        hasRageOfCS = true;
        Debug.Log("Player unlocked: RAGE OF CS!");
    }

    public void UnlockHaluOfCS()
    {
        hasHaluOfCS = true;
        Debug.Log("Player unlocked: HALU OF CS!");
    }

    public void UnlockTimeForCoding()
    {
        hasTimeForCoding = true;
        Debug.Log("Player unlocked: TIME FOR CODING!");
    }
}