using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;
    public float currentHealth;
    public TextMeshProUGUI healthTextDisplay; // Drag a new UI Text here!

    [Header("Buffs")]
    public bool hasUnlimitedEnergy = false;
    public bool isDrunk = false;
    private int vodkaCount = 0;

    [Header("Audio")]
    public AudioSource bgmSource; // Needs an AudioSource to play Cheeki Breeki!

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void HealPercentage(float percent)
    {
        // Calculate how much health to add based on the percentage
        float healAmount = maxHealth * (percent / 100f);
        currentHealth += healAmount;

        // Cap it at 100%
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (healthTextDisplay != null)
        {
            healthTextDisplay.text = "HP: " + Mathf.RoundToInt(currentHealth);
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

        // Play the BGM
        if (bgmSource != null && cheekiBreeki != null)
        {
            bgmSource.clip = cheekiBreeki;
            bgmSource.Play();
            StartCoroutine(StopBGM(duration));
        }

        // Check if we hit the 3-drink threshold!
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