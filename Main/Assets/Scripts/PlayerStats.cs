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
        // Remember if we had overheal BEFORE taking the hit
        bool hadOverheal = currentHealth > maxHealth;

        currentHealth -= damageAmount;

        // If we had overheal, but the hit dropped us to 100 or below, play the break sound!
        if (hadOverheal && currentHealth <= maxHealth)
        {
            if (sfxSource != null && overhealBreakSound != null)
            {
                sfxSource.PlayOneShot(overhealBreakSound);
            }
        }

        UpdateHealthUI();

        // Hit Reactions
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
            Debug.Log("PLAYER IS DEAD!");
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
        // Remember if we had overheal BEFORE drinking
        bool hadOverheal = currentHealth > maxHealth;

        float healAmount = maxHealth * (percent / 100f);
        currentHealth += healAmount;
        
        float cap = canOverheal ? maxOverheal : maxHealth;
        if (currentHealth > cap) currentHealth = cap;

        // If we DID NOT have overheal, but the drink pushed us over 100, play the gain sound!
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
            
            // Turn the health text cyan if we are overhealed!
            if (currentHealth > maxHealth) healthTextDisplay.color = new Color(0.2f, 0.8f, 1f); 
            else if (currentHealth <= lowHealthThreshold) healthTextDisplay.color = Color.red; 
            else healthTextDisplay.color = Color.white;
        }
    }

    public void TriggerUnlimitedEnergy(float duration) { StartCoroutine(EnergyRoutine(duration)); }
    private IEnumerator EnergyRoutine(float duration) { hasUnlimitedEnergy = true; yield return new WaitForSeconds(duration); hasUnlimitedEnergy = false; }
    public void DrinkVodka(AudioClip cheekiBreeki, float duration) { vodkaCount++; if (bgmSource != null && cheekiBreeki != null) { bgmSource.clip = cheekiBreeki; bgmSource.Play(); StartCoroutine(StopBGM(duration)); } if (vodkaCount >= 3) { isDrunk = true; Debug.Log("PLAYER IS DIZZY! Damage +20%"); } }
    private IEnumerator StopBGM(float duration) { yield return new WaitForSeconds(duration); bgmSource.Stop(); }
}