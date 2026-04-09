using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; 

public class PlayerStats : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;         
    public float overhealMax = 125f;       
    public float currentHealth;
    public TextMeshProUGUI healthTextDisplay;

    [Header("Damage UI (Hits)")]
    public Image bloodScreen;          
    public float normalHitAlpha = 0.4f;  // Semi-transparent for normal punches
    public float knockdownAlpha = 0.85f; // Almost solid red for getting knocked down!
    public float flashDuration = 0.5f;   

    [Header("Low Health UI (Pulse)")]
    public float lowHealthThreshold = 30f; // HP where the pulsing starts
    public float pulseSpeed = 2f;          // How fast the heartbeat pulses
    public float maxPulseAlpha = 0.5f;     // How dark the pulse gets

    private float currentFlashAlpha = 0f;  // Tracks the sudden hit flashes

    [Header("Hit Reactions")]
    public float hitResetTime = 3f; 
    private int consecutiveHits = 0;
    private float lastHitTime = 0f;
    private DoomMovement playerMovement;

    [Header("Buffs")]
    public bool hasUnlimitedEnergy = false;
    public bool isDrunk = false;
    private int vodkaCount = 0;

    [Header("Audio")]
    public AudioSource bgmSource;

    void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<DoomMovement>();
        UpdateHealthUI();

        if (bloodScreen != null)
        {
            Color c = bloodScreen.color;
            c.a = 0f;
            bloodScreen.color = c;
        }
    }

    void Update()
    {
        // 1. Reset the hit combo if you evaded for a while
        if (consecutiveHits > 0 && Time.time > lastHitTime + hitResetTime)
        {
            consecutiveHits = 0;
        }

        // 2. --- NEW: DYNAMIC BLOOD SCREEN BLENDING ---
        if (bloodScreen != null)
        {
            float finalAlpha = currentFlashAlpha; // Start with the hit flash

            // If health is dangerously low, calculate a heartbeat pulse!
            if (currentHealth <= lowHealthThreshold && currentHealth > 0)
            {
                // PingPong creates a smooth wave that goes up and down over time
                float pulse = Mathf.PingPong(Time.time * pulseSpeed, maxPulseAlpha);
                
                // Keep whichever is higher: The sudden hit flash, or the heartbeat pulse
                finalAlpha = Mathf.Max(currentFlashAlpha, pulse);
            }

            // Apply it to the screen
            Color c = bloodScreen.color;
            c.a = finalAlpha;
            bloodScreen.color = c;
        }
    }

    public void TakeDamage(float damageAmount, Transform attacker = null)
    {
        currentHealth -= damageAmount;
        UpdateHealthUI();

        lastHitTime = Time.time;
        consecutiveHits++;

        if (playerMovement != null)
        {
            if (consecutiveHits >= 3)
            {
                playerMovement.TriggerKnockdown();
                consecutiveHits = 0; 
                
                // --- NEW: MASSIVE FLASH FOR KNOCKDOWN ---
                if (bloodScreen != null)
                {
                    StopCoroutine("FlashBloodScreen"); 
                    StartCoroutine(FlashBloodScreen(knockdownAlpha, 1.5f)); // Dark red, lasts longer!
                }
            }
            else if (attacker != null)
            {
                Vector3 pushDirection = (transform.position - attacker.position).normalized;
                pushDirection.y = 0; 
                playerMovement.ApplyPunchKnockback(pushDirection, 15f); 
                
                // --- NEW: NORMAL FLASH FOR PUNCH ---
                if (bloodScreen != null)
                {
                    StopCoroutine("FlashBloodScreen"); 
                    StartCoroutine(FlashBloodScreen(normalHitAlpha, flashDuration)); 
                }
            }
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("PLAYER IS DEAD!");
            currentFlashAlpha = 1f; // Screen goes pitch red when dead!
        }
    }

    // --- REVISION: CUSTOMIZABLE FADE OUT ---
    IEnumerator FlashBloodScreen(float targetAlpha, float duration)
    {
        currentFlashAlpha = targetAlpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentFlashAlpha = Mathf.Lerp(targetAlpha, 0f, elapsed / duration);
            yield return null;
        }

        currentFlashAlpha = 0f;
    }

    public void HealPercentage(float percent, bool canOverheal = false)
    {
        if (!canOverheal && currentHealth >= maxHealth) return;
        float healAmount = maxHealth * (percent / 100f);
        currentHealth += healAmount;
        if (!canOverheal && currentHealth > maxHealth) currentHealth = maxHealth; 
        else if (canOverheal && currentHealth > overhealMax) currentHealth = overhealMax; 
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = "HP: " + Mathf.RoundToInt(currentHealth);
            if (currentHealth > maxHealth) healthTextDisplay.color = Color.cyan;
            else if (currentHealth <= lowHealthThreshold) healthTextDisplay.color = Color.red; // Text turns red too!
            else healthTextDisplay.color = Color.white;
        }
    }

    public void TriggerUnlimitedEnergy(float duration) { StartCoroutine(EnergyRoutine(duration)); }
    private IEnumerator EnergyRoutine(float duration) { hasUnlimitedEnergy = true; yield return new WaitForSeconds(duration); hasUnlimitedEnergy = false; }
    public void DrinkVodka(AudioClip cheekiBreeki, float duration) { vodkaCount++; if (bgmSource != null && cheekiBreeki != null) { bgmSource.clip = cheekiBreeki; bgmSource.Play(); StartCoroutine(StopBGM(duration)); } if (vodkaCount >= 3) { isDrunk = true; Debug.Log("PLAYER IS DIZZY! Damage +20%"); } }
    private IEnumerator StopBGM(float duration) { yield return new WaitForSeconds(duration); bgmSource.Stop(); }
}