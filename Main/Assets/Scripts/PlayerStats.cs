using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;         // Normal Cap for Food
    public float overhealMax = 125f;       // Absolute Buff Cap for Drinks!
    public float currentHealth;
    public TextMeshProUGUI healthTextDisplay;

    [Header("Buffs")]
    public bool hasUnlimitedEnergy = false;
    public bool isDrunk = false;
    private int vodkaCount = 0;

    [Header("Audio")]
    public AudioSource bgmSource;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // NEW: Added a "canOverheal" toggle!
    public void HealPercentage(float percent, bool canOverheal = false)
    {
        // If we are already at or above 100, and this is normal food, don't heal!
        if (!canOverheal && currentHealth >= maxHealth) return;

        float healAmount = maxHealth * (percent / 100f);
        currentHealth += healAmount;

        // Enforce the caps!
        if (!canOverheal && currentHealth > maxHealth)
        {
            currentHealth = maxHealth; // Cap normal food at 100
        }
        else if (canOverheal && currentHealth > overhealMax)
        {
            currentHealth = overhealMax; // Cap energy drinks at 125
        }
        
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = "HP: " + Mathf.RoundToInt(currentHealth);

            // UI MAGIC: Turn the text Cyan if we have overheal buffer!
            if (currentHealth > maxHealth)
            {
                healthTextDisplay.color = Color.cyan;
            }
            else
            {
                healthTextDisplay.color = Color.white;
            }
        }
    }

    // --- BUFF: EXTRAJOSS & VODKA ENERGY ---
    public void TriggerUnlimitedEnergy(float duration)
    {
        StartCoroutine(EnergyRoutine(duration));
    }

    private IEnumerator EnergyRoutine(float duration)
    {
        hasUnlimitedEnergy = true;
        yield return new WaitForSeconds(duration);
        hasUnlimitedEnergy = false;
    }

    // --- BUFF: REY'S VODKA ---
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
            Debug.Log("PLAYER IS DIZZY! Damage +20%");
        }
    }

    private IEnumerator StopBGM(float duration)
    {
        yield return new WaitForSeconds(duration);
        bgmSource.Stop();
    }
}