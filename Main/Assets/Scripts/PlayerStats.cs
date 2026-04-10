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

    [Header("Damage UI (Hits)")]
    public Image bloodScreen;          
    public float normalHitAlpha = 0.4f;  
    public float knockdownAlpha = 0.85f; 
    public float flashDuration = 0.5f;   

    [Header("Low Health UI (Pulse)")]
    public float lowHealthThreshold = 30f; 
    public float pulseSpeed = 2f;          
    public float maxPulseAlpha = 0.5f;     

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
    }

    void Update()
    {
        if (consecutiveHits > 0 && Time.time > lastHitTime + hitResetTime)
        {
            consecutiveHits = 0;
        }

        // --- DIZZY DECAY ---
        if (dizzyStacks > 0)
        {
            dizzyDecayTimer -= Time.deltaTime;
            if (dizzyDecayTimer <= 0f)
            {
                dizzyStacks--;
                if (dizzyStacks > 0) dizzyDecayTimer = 4f; 
            }
            
            // If we are fully sober, turn off the drunk debuff!
            if (dizzyStacks == 0) isDrunk = false;
        }

        if (bloodScreen != null)
        {
            float finalAlpha = currentFlashAlpha; 

            if (currentHealth <= lowHealthThreshold && currentHealth > 0)
            {
                float pulse = Mathf.PingPong(Time.time * pulseSpeed, maxPulseAlpha);
                finalAlpha = Mathf.Max(currentFlashAlpha, pulse);
            }

            Color c = bloodScreen.color;
            c.a = finalAlpha;
            bloodScreen.color = c;
        }
    }

    public void TakeDamage(float damageAmount, Transform attacker = null)
    {
        bool hadOverheal = currentHealth > maxHealth;

        currentHealth -= damageAmount;

        if (hadOverheal && currentHealth <= maxHealth)
        {
            if (sfxSource != null && overhealBreakSound != null)
            {
                sfxSource.PlayOneShot(overhealBreakSound);
            }
        }

        UpdateHealthUI();

        lastHitTime = Time.time;
        consecutiveHits++;

        if (playerMovement != null)
        {
            if (consecutiveHits >= 3)
            {
                playerMovement.TriggerKnockdown();
                consecutiveHits = 0; 
                
                if (bloodScreen != null)
                {
                    StopCoroutine("FlashBloodScreen"); 
                    StartCoroutine(FlashBloodScreen(knockdownAlpha, 1.5f)); 
                }
            }
            else if (attacker != null)
            {
                Vector3 pushDirection = (transform.position - attacker.position).normalized;
                pushDirection.y = 0; 
                playerMovement.ApplyPunchKnockback(pushDirection, 15f); 
                
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
            currentFlashAlpha = 1f; 
        }
    }

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
        bool hadOverheal = currentHealth > maxHealth;

        float healAmount = maxHealth * (percent / 100f);
        currentHealth += healAmount;
        
        float cap = canOverheal ? maxOverheal : maxHealth;
        if (currentHealth > cap) currentHealth = cap;

        if (!hadOverheal && currentHealth > maxHealth)
        {
            if (sfxSource != null && overhealGainSound != null)
            {
                sfxSource.PlayOneShot(overhealGainSound);
            }
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
        if (dizzyStacks < maxDizzyStacks) 
        {
            dizzyStacks++;
        }
        
        isDrunk = true; 
        dizzyDecayTimer = 4f; 
    }

    public void TriggerUnlimitedEnergy(float duration) { StartCoroutine(EnergyRoutine(duration)); }
    private IEnumerator EnergyRoutine(float duration) { hasUnlimitedEnergy = true; yield return new WaitForSeconds(duration); hasUnlimitedEnergy = false; }
    
    // --- VODKA NOW TRIGGERS THE FULL DIZZY EFFECT ---
    public void DrinkVodka(AudioClip cheekiBreeki, float duration) 
    { 
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
            
            // Instantly max out the dizziness!
            dizzyStacks = maxDizzyStacks;
            // Takes longer to wear off because it's alcohol, not a magic attack!
            dizzyDecayTimer = 8f; 
            
            Debug.Log("PLAYER IS DIZZY FROM VODKA! Damage +20%"); 
        } 
    }
    
    private IEnumerator StopBGM(float duration) { yield return new WaitForSeconds(duration); bgmSource.Stop(); }
}