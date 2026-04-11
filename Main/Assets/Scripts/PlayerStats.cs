using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; 

public class PlayerStats : MonoBehaviour
{
    [Header("Health & Overheal System")]
    public float maxHealth = 100f;         
    public float maxOverheal = 150f;       
    public float currentHealth;
    public TextMeshProUGUI healthTextDisplay;

    [Header("Overheal Audio")]
    public AudioSource sfxSource;          
    public AudioClip overhealGainSound;      
    public AudioClip overhealBreakSound;      

    [Header("Player VOs (Voice/SFX)")]
    public AudioClip playerHitSound;         
    public AudioClip playerKnockoutSound;    
    public AudioClip playerDeathSound;       
    public bool isDead { get; private set; } = false;             

    [Header("Damage UI (Hits)")]
    public Image bloodScreen;          
    public float normalHitAlpha = 0.4f;  
    public float knockdownAlpha = 0.85f; 
    public float flashDuration = 0.5f;   

    [Header("Low Health UI (Pulse)")]
    public float lowHealthThreshold = 30f; 
    public float pulseSpeed = 2f;          
    public float maxPulseAlpha = 0.5f;     

    [Header("Death UI")]
    public Image deadScreenImage;          
    public float deadScreenFadeDuration = 2f; 

    [Header("Flashbang UI")] 
    public Image flashbangScreenImage;     

    private float currentFlashAlpha = 0f;  

    [Header("Hit Reactions")]
    public float hitResetTime = 3f; 
    private int consecutiveHits = 0;
    private float lastHitTime = 0f;
    private DoomMovement playerMovement;

    [Header("Buffs")]
    public bool hasUnlimitedEnergy = false;
    public bool isDrunk = false;
    private int vodkaCount = 0;

    [Header("Dizzy Effect")]
    public int dizzyStacks = 0;
    public int maxDizzyStacks = 5;
    private float dizzyDecayTimer = 0f;

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

        if (deadScreenImage != null)
        {
            Color c = deadScreenImage.color;
            c.a = 0f;
            deadScreenImage.color = c;
            deadScreenImage.gameObject.SetActive(false);
        }

        // --- Ensure the flashbang screen is invisible when the game starts! ---
        if (flashbangScreenImage != null)
        {
            Color c = flashbangScreenImage.color;
            c.a = 0f;
            flashbangScreenImage.color = c;
            flashbangScreenImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isDead) return; 

        if (consecutiveHits > 0 && Time.unscaledTime > lastHitTime + hitResetTime)
        {
            consecutiveHits = 0;
        }

        if (dizzyStacks > 0)
        {
            dizzyDecayTimer -= Time.unscaledDeltaTime;
            if (dizzyDecayTimer <= 0f)
            {
                dizzyStacks = 0;
                isDrunk = false; 
            }
        }

        if (bloodScreen != null)
        {
            float finalAlpha = currentFlashAlpha; 

            if (currentHealth <= lowHealthThreshold && currentHealth > 0)
            {
                float pulse = Mathf.PingPong(Time.unscaledTime * pulseSpeed, maxPulseAlpha);
                finalAlpha = Mathf.Max(currentFlashAlpha, pulse);
            }

            Color c = bloodScreen.color;
            c.a = finalAlpha;
            bloodScreen.color = c;
        }
    }

    public void TakeDamage(float damageAmount, Transform attacker = null)
    {
        if (isDead) return;

        PlayerSkills skills = GetComponent<PlayerSkills>();
        if (skills != null) skills.CancelHaluOfCS();

        bool hadOverheal = currentHealth > maxHealth;
        currentHealth -= damageAmount;

        if (hadOverheal && currentHealth <= maxHealth)
        {
            if (sfxSource != null && overhealBreakSound != null) sfxSource.PlayOneShot(overhealBreakSound);
        }

        UpdateHealthUI();

        lastHitTime = Time.unscaledTime;
        consecutiveHits++;

        bool isKnockout = false; 

        if (playerMovement != null)
        {
            if (consecutiveHits >= 3)
            {
                isKnockout = true;
                playerMovement.TriggerKnockdown();
                consecutiveHits = 0; 
                
                if (bloodScreen != null)
                {
                    StopCoroutine("FlashBloodScreen"); 
                    StartCoroutine(FlashBloodScreen(knockdownAlpha, 1.5f)); 
                }
            }
            else 
            {
                if (attacker != null)
                {
                    Vector3 pushDirection = (transform.position - attacker.position).normalized;
                    pushDirection.y = 0; 
                    playerMovement.ApplyPunchKnockback(pushDirection, 15f); 
                }
                
                if (bloodScreen != null)
                {
                    StopCoroutine("FlashBloodScreen"); 
                    StartCoroutine(FlashBloodScreen(normalHitAlpha, flashDuration)); 
                }
            }
        }

        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            isDead = true;
            
            if (sfxSource != null && playerDeathSound != null) sfxSource.PlayOneShot(playerDeathSound);
            
            if (playerMovement != null) playerMovement.TriggerDeath();

            if (deadScreenImage != null)
            {
                deadScreenImage.gameObject.SetActive(true);
                StartCoroutine(FadeInDeadScreenRoutine());
            }
        }
        else if (isKnockout)
        {
            if (sfxSource != null && playerKnockoutSound != null) sfxSource.PlayOneShot(playerKnockoutSound);
        }
        else if (!isDead)
        {
            if (sfxSource != null && playerHitSound != null) sfxSource.PlayOneShot(playerHitSound);
        }
    }

    private IEnumerator FadeInDeadScreenRoutine()
    {
        float elapsed = 0f;
        Color c = deadScreenImage.color;
        
        while (elapsed < deadScreenFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / deadScreenFadeDuration);
            deadScreenImage.color = c;
            yield return null;
        }
        
        c.a = 1f;
        deadScreenImage.color = c;
    }

    // --- NEW: Kaya's Flashbang Call ---
    public void TriggerFlashbang(float duration)
    {
        if (isDead || flashbangScreenImage == null) return;
        
        StopCoroutine("FlashbangRoutine");
        StartCoroutine(FlashbangRoutine(duration));
    }

    // --- NEW: Smoothly fades the white screen away ---
    private IEnumerator FlashbangRoutine(float duration)
    {
        // 1. Instantly snap to pure white
        Color c = flashbangScreenImage.color;
        c.a = 1f;
        flashbangScreenImage.color = c;
        flashbangScreenImage.gameObject.SetActive(true);

        float elapsed = 0f;

        // 2. Smoothly fade the white screen away!
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            flashbangScreenImage.color = c;
            yield return null;
        }

        c.a = 0f;
        flashbangScreenImage.color = c;
        flashbangScreenImage.gameObject.SetActive(false);
    }

    IEnumerator FlashBloodScreen(float targetAlpha, float duration)
    {
        currentFlashAlpha = targetAlpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            currentFlashAlpha = Mathf.Lerp(targetAlpha, 0f, elapsed / duration);
            yield return null;
        }
        currentFlashAlpha = 0f;
    }

    public void HealPercentage(float percent, bool canOverheal = false)
    {
        if (isDead) return; 

        bool hadOverheal = currentHealth > maxHealth;

        float healAmount = maxHealth * (percent / 100f);
        currentHealth += healAmount;
        
        float cap = canOverheal ? maxOverheal : maxHealth;
        if (currentHealth > cap) currentHealth = cap;

        if (!hadOverheal && currentHealth > maxHealth)
        {
            if (sfxSource != null && overhealGainSound != null) sfxSource.PlayOneShot(overhealGainSound);
        }

        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = "HP: " + Mathf.RoundToInt(currentHealth);
            
            if (currentHealth > maxHealth) healthTextDisplay.color = new Color(0.2f, 0.8f, 1f); 
            else if (currentHealth <= lowHealthThreshold) healthTextDisplay.color = Color.red; 
            else healthTextDisplay.color = Color.white;
        }
    }

    public void AddDizzyStack()
    {
        if (isDead) return;
        if (dizzyStacks < maxDizzyStacks) dizzyStacks++;
        isDrunk = true; 
        dizzyDecayTimer = 2f; 
    }

    public void TriggerUnlimitedEnergy(float duration) { StartCoroutine(EnergyRoutine(duration)); }
    private IEnumerator EnergyRoutine(float duration) { hasUnlimitedEnergy = true; yield return new WaitForSeconds(duration); hasUnlimitedEnergy = false; }
    
    public void DrinkVodka(AudioClip cheekiBreeki, float duration) 
    { 
        if (isDead) return;
        vodkaCount++; 
        if (bgmSource != null && cheekiBreeki != null) 
        { 
            bgmSource.clip = cheekiBreeki; 
            bgmSource.Play(); 
            StartCoroutine(StopBGM(duration)); 
        } 
        if (vodkaCount >= 3) 
        { 
            isDrunk = true; 
            dizzyStacks = maxDizzyStacks;
            dizzyDecayTimer = 2f; 
        } 
    }
    
    private IEnumerator StopBGM(float duration) { yield return new WaitForSeconds(duration); bgmSource.Stop(); }
}