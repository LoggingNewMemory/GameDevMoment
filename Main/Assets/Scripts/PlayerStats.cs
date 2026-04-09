using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;         
    public float overhealMax = 125f;       
    public float currentHealth;
    public TextMeshProUGUI healthTextDisplay;

    [Header("Hit Reactions (NEW)")]
    public float hitResetTime = 3f; // How long to wait before combo resets
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
    }

    void Update()
    {
        // Reset the hit combo if you haven't been punched in a while!
        if (consecutiveHits > 0 && Time.time > lastHitTime + hitResetTime)
        {
            consecutiveHits = 0;
        }
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

    // NEW: We added "attacker" so we know which way to shove the player!
    public void TakeDamage(float damageAmount, Transform attacker = null)
    {
        currentHealth -= damageAmount;
        UpdateHealthUI();

        // Track the hits
        lastHitTime = Time.time;
        consecutiveHits++;

        if (playerMovement != null)
        {
            if (consecutiveHits >= 3)
            {
                // YOU GOT KNOCKED OUT!
                playerMovement.TriggerKnockdown();
                consecutiveHits = 0; // Reset counter
            }
            else if (attacker != null)
            {
                // NORMAL PUNCH: Shove the player backward!
                Vector3 pushDirection = (transform.position - attacker.position).normalized;
                pushDirection.y = 0; // Don't launch them into space
                playerMovement.ApplyPunchKnockback(pushDirection, 15f); // 15 is a strong shove!
            }
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("PLAYER IS DEAD!");
        }
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

    public void DrinkVodka(AudioClip cheekiBreeki, float duration)
    {
        vodkaCount++;
        if (bgmSource != null && cheekiBreeki != null)
        {
            bgmSource.clip = cheekiBreeki; bgmSource.Play(); StartCoroutine(StopBGM(duration));
        }
        if (vodkaCount >= 3) { isDrunk = true; Debug.Log("PLAYER IS DIZZY! Damage +20%"); }
    }
    private IEnumerator StopBGM(float duration) { yield return new WaitForSeconds(duration); bgmSource.Stop(); }
}