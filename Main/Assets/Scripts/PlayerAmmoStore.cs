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
        if (notificationText == null || !notificationText.gameObject.scene.IsValid())
        {
            notificationText = null; 
            
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

        if (notificationText != null)
        {
            Color c = notificationText.color;
            c.a = 0f;
            notificationText.color = c;
            notificationText.gameObject.SetActive(false); 
        }
    }

    // --- CHANGED: Added 'forcePickup' parameter ---
    public bool AddAmmo(AmmoType type, int amount, bool forcePickup = false)
    {
        string ammoName = "";

        switch (type)
        {
            case AmmoType.Pistol:
                if (!hasPistol && !forcePickup) return false; 
                pistolAmmo += amount;
                ammoName = "Pistol Bullets";
                break;
            case AmmoType.Shotgun:
                if (!hasShotgun && !forcePickup) return false; 
                shotgunAmmo += amount;
                ammoName = "Shotgun Shells";
                break;
            case AmmoType.SMG:
                if (!hasSMG && !forcePickup) return false; 
                smgAmmo += amount;
                ammoName = "SMG Bullets";
                break;
            case AmmoType.AssaultRifle:
                if (!hasAssaultRifle && !forcePickup) return false; 
                arAmmo += amount;
                ammoName = "AR Bullets";
                break;
            case AmmoType.SniperOrLMG:
                if (!hasSniperOrLMG && !forcePickup) return false; 
                sniperOrLmgAmmo += amount;
                ammoName = "Heavy Ammo";
                break;
            case AmmoType.Railgun:
                if (!hasRailgun && !forcePickup) return false; 
                railgunAmmo += amount;
                ammoName = "Railgun Batteries";
                break;
        }

        TriggerNotification($"You Got: {amount} {ammoName}");

        // --- THE UI SYNC FIX ---
        // Find whatever gun is currently active in your hands
        SimpleShoot activeGun = GetComponentInChildren<SimpleShoot>();
        if (activeGun != null)
        {
            // Instantly push the bullets into the gun so the UI updates!
            activeGun.AddAmmo(amount);
        }

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
        notificationText.gameObject.SetActive(true);
        notificationText.text = message;
        
        Color c = notificationText.color;
        c.a = 1f;
        notificationText.color = c;

        yield return new WaitForSeconds(showDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            notificationText.color = c;
            yield return null;
        }
        
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