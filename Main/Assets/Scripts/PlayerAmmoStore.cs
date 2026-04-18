using UnityEngine;
using TMPro; 
using System.Collections;

public enum AmmoType
{
    Pistol,
    Shotgun,
    SMG,
    AssaultRifle,
    SniperOrLMG,
    Railgun
}

public class PlayerAmmoStore : MonoBehaviour
{
    [Header("Owned Weapons (Check what player has!)")]
    [Tooltip("If unchecked, the player will ignore ammo boxes for this weapon type!")]
    public bool hasPistol = true;
    public bool hasShotgun = false;
    public bool hasSMG = false;
    public bool hasAssaultRifle = false;
    public bool hasSniperOrLMG = false;
    public bool hasRailgun = false;

    [Header("Ammo Backpack (Current Reserve)")]
    public int pistolAmmo = 60;
    public int shotgunAmmo = 16;
    public int smgAmmo = 120;
    public int arAmmo = 90;
    public int sniperOrLmgAmmo = 30;
    public int railgunAmmo = 5;

    [Header("UI Notification Settings")]
    public TextMeshProUGUI notificationText;
    public float showDuration = 2f; 
    public float fadeDuration = 1f; 
    
    private Coroutine activeNotification;

    void Start()
    {
        // --- THE BULLETPROOF UI FIX ---
        // 1. If the slot is empty, OR if a blue Prefab file was accidentally dragged in:
        if (notificationText == null || !notificationText.gameObject.scene.IsValid())
        {
            notificationText = null; // Erase the bad file link!
            
            // Search the physical screen for the real text
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                Transform notifObj = mainCanvas.transform.Find("NotificationAmmo");
                if (notifObj != null) 
                {
                    notificationText = notifObj.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // 2. Safely and completely turn off the real screen text when the game starts
        if (notificationText != null)
        {
            Color c = notificationText.color;
            c.a = 0f;
            notificationText.color = c;
            notificationText.gameObject.SetActive(false); // Physically turn the object off!
        }
    }

    public bool AddAmmo(AmmoType type, int amount)
    {
        string ammoName = "";

        switch (type)
        {
            case AmmoType.Pistol:
                if (!hasPistol) return false; 
                pistolAmmo += amount;
                ammoName = "Pistol Bullets";
                break;
            case AmmoType.Shotgun:
                if (!hasShotgun) return false; 
                shotgunAmmo += amount;
                ammoName = "Shotgun Shells";
                break;
            case AmmoType.SMG:
                if (!hasSMG) return false; 
                smgAmmo += amount;
                ammoName = "SMG Bullets";
                break;
            case AmmoType.AssaultRifle:
                if (!hasAssaultRifle) return false; 
                arAmmo += amount;
                ammoName = "AR Bullets";
                break;
            case AmmoType.SniperOrLMG:
                if (!hasSniperOrLMG) return false; 
                sniperOrLmgAmmo += amount;
                ammoName = "Heavy Ammo";
                break;
            case AmmoType.Railgun:
                if (!hasRailgun) return false; 
                railgunAmmo += amount;
                ammoName = "Railgun Batteries";
                break;
        }

        TriggerNotification($"You Got: {amount} {ammoName}");
        return true; 
    }

    private void TriggerNotification(string message)
    {
        if (notificationText == null) return;
        if (activeNotification != null) StopCoroutine(activeNotification);
        activeNotification = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        // 1. Wake the UI up!
        notificationText.gameObject.SetActive(true);
        notificationText.text = message;
        
        Color c = notificationText.color;
        c.a = 1f;
        notificationText.color = c;

        // 2. Wait so the player can read it
        yield return new WaitForSeconds(showDuration);

        // 3. Smooth fade out
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            notificationText.color = c;
            yield return null;
        }
        
        // 4. Clean up and put it completely to sleep
        c.a = 0f;
        notificationText.color = c;
        notificationText.gameObject.SetActive(false); 
    }

    public bool TryConsumeAmmo(AmmoType type, int amountNeeded)
    {
        switch (type)
        {
            case AmmoType.Pistol: if (pistolAmmo >= amountNeeded) { pistolAmmo -= amountNeeded; return true; } break;
            case AmmoType.Shotgun: if (shotgunAmmo >= amountNeeded) { shotgunAmmo -= amountNeeded; return true; } break;
            case AmmoType.SMG: if (smgAmmo >= amountNeeded) { smgAmmo -= amountNeeded; return true; } break;
            case AmmoType.AssaultRifle: if (arAmmo >= amountNeeded) { arAmmo -= amountNeeded; return true; } break;
            case AmmoType.SniperOrLMG: if (sniperOrLmgAmmo >= amountNeeded) { sniperOrLmgAmmo -= amountNeeded; return true; } break;
            case AmmoType.Railgun: if (railgunAmmo >= amountNeeded) { railgunAmmo -= amountNeeded; return true; } break;
        }
        return false; 
    }
}