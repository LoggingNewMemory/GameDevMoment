using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; // <-- NEW: Required to talk to UI Images!

public class PlayerStats : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;         
    public float overhealMax = 125f;       
    public float currentHealth;
    public TextMeshProUGUI healthTextDisplay;

    [Header("Damage UI")]
    public Image bloodScreen;          // The red image we just made
    public float flashDuration = 0.5f; // How long it takes to fade away
    public float maxAlpha = 0.5f;      // How dark the red gets (0.5 is semi-transparent)

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

        // Make sure the screen is clear when the game starts
        if (bloodScreen != null)
        {
            Color c = bloodScreen.color;
            c.a = 0f;
            bloodScreen.color = c;
        }
    }

    void Update()
    {
        if (consecutiveHits > 0 && Time.time > lastHitTime + hitResetTime)
        {
            consecutiveHits = 0;
        }
    }

    public void TakeDamage(float damageAmount, Transform attacker = null)
    {
        currentHealth -= damageAmount;
        UpdateHealthUI();

        lastHitTime = Time.time;
        consecutiveHits++;

        // --- NEW: TRIGGER THE BLOOD FLASH! ---
        if (bloodScreen != null)
        {
            StopCoroutine("FlashBloodScreen"); // Stop any current flash
            StartCoroutine("FlashBloodScreen"); // Start a fresh one
        }

        if (playerMovement != null)
        {
            if (consecutiveHits >= 3)
            {
                playerMovement.TriggerKnockdown();
                consecutiveHits = 0; 
            }
            else if (attacker != null)
            {
                Vector3 pushDirection = (transform.position - attacker.position).normalized;
                pushDirection.y = 0; 
                playerMovement.ApplyPunchKnockback(pushDirection, 15f); 
            }
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("PLAYER IS DEAD!");
        }
    }

    // --- NEW: THE FADE OUT ROUTINE ---
    IEnumerator FlashBloodScreen()
    {
        // 1. Instantly set it to bright red
        Color c = bloodScreen.color;
        c.a = maxAlpha;
        bloodScreen.color = c;

        float elapsed = 0f;

        // 2. Smoothly fade the alpha back to 0
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(maxAlpha, 0f, elapsed / flashDuration);
            bloodScreen.color = c;
            yield return null;
        }

        // 3. Guarantee it is completely invisible at the end
        c.a = 0f;
        bloodScreen.color = c;
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
            else healthTextDisplay.color = Color.white;
        }
    }

    public void TriggerUnlimitedEnergy(float duration) { StartCoroutine(EnergyRoutine(duration)); }
    private IEnumerator EnergyRoutine(float duration) { hasUnlimitedEnergy = true; yield return new WaitForSeconds(duration); hasUnlimitedEnergy = false; }
    public void DrinkVodka(AudioClip cheekiBreeki, float duration) { vodkaCount++; if (bgmSource != null && cheekiBreeki != null) { bgmSource.clip = cheekiBreeki; bgmSource.Play(); StartCoroutine(StopBGM(duration)); } if (vodkaCount >= 3) { isDrunk = true; Debug.Log("PLAYER IS DIZZY! Damage +20%"); } }
    private IEnumerator StopBGM(float duration) { yield return new WaitForSeconds(duration); bgmSource.Stop(); }
}