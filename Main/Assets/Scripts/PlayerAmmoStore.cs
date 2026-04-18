using UnityEngine;
using TMPro; // Needed to talk to your TextMeshPro UI!
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
    [Header("Ammo Backpack (Current Reserve)")]
    public int pistolAmmo = 60;
    public int shotgunAmmo = 16;
    public int smgAmmo = 120;
    public int arAmmo = 90;
    public int sniperOrLmgAmmo = 30;
    public int railgunAmmo = 5;

    [Header("UI Notification Settings")]
    public TextMeshProUGUI notificationText;
    public float showDuration = 2f; // How long the text stays solid
    public float fadeDuration = 1f; // How long it takes to slowly fade out
    
    private Coroutine activeNotification;

    void Start()
    {
        // Automatically find your "NotificationAmmo" UI exactly like we did for Health!
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null && notificationText == null)
        {
            Transform notifObj = mainCanvas.transform.Find("NotificationAmmo");
            if (notifObj != null) 
            {
                notificationText = notifObj.GetComponent<TextMeshProUGUI>();
                
                // Hide it at the very start of the game
                Color c = notificationText.color;
                c.a = 0f;
                notificationText.color = c;
            }
        }
    }

    public void AddAmmo(AmmoType type, int amount)
    {
        string ammoName = "";

        switch (type)
        {
            case AmmoType.Pistol:
                pistolAmmo += amount;
                ammoName = "Pistol Bullets";
                break;
            case AmmoType.Shotgun:
                shotgunAmmo += amount;
                ammoName = "Shotgun Shells";
                break;
            case AmmoType.SMG:
                smgAmmo += amount;
                ammoName = "SMG Bullets";
                break;
            case AmmoType.AssaultRifle:
                arAmmo += amount;
                ammoName = "AR Bullets";
                break;
            case AmmoType.SniperOrLMG:
                sniperOrLmgAmmo += amount;
                ammoName = "Heavy Ammo";
                break;
            case AmmoType.Railgun:
                railgunAmmo += amount;
                ammoName = "Railgun Batteries";
                break;
        }

        // Trigger the popup!
        TriggerNotification($"You Got: {amount} {ammoName}");
    }

    private void TriggerNotification(string message)
    {
        if (notificationText == null) return;

        // If a message is already fading out, stop it so we can pop the new one instantly!
        if (activeNotification != null) StopCoroutine(activeNotification);
        
        activeNotification = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        // 1. Set the text and make it 100% visible immediately
        notificationText.text = message;
        Color c = notificationText.color;
        c.a = 1f;
        notificationText.color = c;

        // 2. Wait a few seconds so the player can read it
        yield return new WaitForSeconds(showDuration);

        // 3. Smoothly fade the text into invisibility
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            notificationText.color = c;
            yield return null;
        }

        // Guarantee it is perfectly invisible at the end
        c.a = 0f;
        notificationText.color = c;
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