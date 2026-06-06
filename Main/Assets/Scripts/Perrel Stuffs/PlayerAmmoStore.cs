using UnityEngine;
using TMPro; 
using System.Collections;

public enum AmmoType { Pistol, Shotgun, SMG, AssaultRifle, Sniper, LMG, Railgun }

public class PlayerAmmoStore : MonoBehaviour
{
    [Header("Owned Weapons")]
    public bool hasPistol = true;
    public bool hasShotgun = false;
    public bool hasSMG = false;
    public bool hasAssaultRifle = false;
    public bool hasSniper = false;
    public bool hasLMG = false;
    public bool hasRailgun = false;

    [Header("Ammo Backpack (Current Reserve)")]
    public int pistolAmmo = 30;    
    public int shotgunAmmo = 8;    
    public int smgAmmo = 60;       
    public int arAmmo = 45;        
    public int sniperAmmo = 15;    
    public int lmgAmmo = 150;      
    public int railgunAmmo = 2;    

    [Header("Max Ammo Capacity")]
    public int maxPistolAmmo = 60;
    public int maxShotgunAmmo = 16;
    public int maxSmgAmmo = 120;
    public int maxArAmmo = 90;
    public int maxSniperAmmo = 30; 
    public int maxLmgAmmo = 300;   
    public int maxRailgunAmmo = 5;

    [Header("UI Notification Settings")]
    public TextMeshProUGUI notificationText;
    public float showDuration = 2f; 
    public float fadeDuration = 1f; 
    
    private Coroutine activeNotification;

    void Start()
    {
        // --- AGENT ZETA WEAPON RETRIEVAL ---
        // Load the saved gacha results into the player's inventory!
        hasPistol = true; 
        if (PlayerPrefs.GetInt("Unlocked_Shotgun", 0) == 1) hasShotgun = true;
        if (PlayerPrefs.GetInt("Unlocked_SMG", 0) == 1) hasSMG = true; 
        if (PlayerPrefs.GetInt("Unlocked_AssaultRifle", 0) == 1) hasAssaultRifle = true;
        if (PlayerPrefs.GetInt("Unlocked_Sniper", 0) == 1) hasSniper = true;
        if (PlayerPrefs.GetInt("Unlocked_LMG", 0) == 1) hasLMG = true;
        if (PlayerPrefs.GetInt("Unlocked_Railgun", 0) == 1) hasRailgun = true;
        // -----------------------------------

        if (notificationText == null || !notificationText.gameObject.scene.IsValid())
        {
            notificationText = null; 
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                Transform notifObj = mainCanvas.transform.Find("NotificationAmmo");
                if (notifObj != null) notificationText = notifObj.GetComponent<TextMeshProUGUI>();
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

    public bool AddAmmo(AmmoType type, int amount)
    {
        string ammoName = "";

        switch (type)
        {
            case AmmoType.Pistol:
                if (!hasPistol || pistolAmmo >= maxPistolAmmo) return false; 
                pistolAmmo += amount; 
                if (pistolAmmo > maxPistolAmmo) pistolAmmo = maxPistolAmmo; 
                ammoName = "Pistol Bullets"; 
                break;

            case AmmoType.Shotgun:
                if (!hasShotgun || shotgunAmmo >= maxShotgunAmmo) return false; 
                shotgunAmmo += amount; 
                if (shotgunAmmo > maxShotgunAmmo) shotgunAmmo = maxShotgunAmmo;
                ammoName = "Shotgun Shells"; 
                break;

            case AmmoType.SMG:
                if (!hasSMG || smgAmmo >= maxSmgAmmo) return false; 
                smgAmmo += amount; 
                if (smgAmmo > maxSmgAmmo) smgAmmo = maxSmgAmmo;
                ammoName = "SMG Bullets"; 
                break;

            case AmmoType.AssaultRifle:
                if (!hasAssaultRifle || arAmmo >= maxArAmmo) return false; 
                arAmmo += amount; 
                if (arAmmo > maxArAmmo) arAmmo = maxArAmmo;
                ammoName = "AR Bullets"; 
                break;

            case AmmoType.Sniper:
                if (!hasSniper || sniperAmmo >= maxSniperAmmo) return false; 
                sniperAmmo += amount; 
                if (sniperAmmo > maxSniperAmmo) sniperAmmo = maxSniperAmmo;
                ammoName = "Sniper Rounds"; 
                break;

            case AmmoType.LMG:
                if (!hasLMG || lmgAmmo >= maxLmgAmmo) return false; 
                lmgAmmo += amount; 
                if (lmgAmmo > maxLmgAmmo) lmgAmmo = maxLmgAmmo;
                ammoName = "LMG Belt"; 
                break;

            case AmmoType.Railgun:
                if (!hasRailgun || railgunAmmo >= maxRailgunAmmo) return false; 
                railgunAmmo += amount; 
                if (railgunAmmo > maxRailgunAmmo) railgunAmmo = maxRailgunAmmo;
                ammoName = "Railgun Batteries"; 
                break;
        }

        TriggerNotification($"You Got: {amount} {ammoName}");

        SimpleShoot activeGun = GetComponentInChildren<SimpleShoot>();
        if (activeGun != null && activeGun.weaponAmmoType == type)
        {
            activeGun.UpdateAmmoUI();
        }

        return true; 
    }

    public int GetAmmoCount(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol: return pistolAmmo;
            case AmmoType.Shotgun: return shotgunAmmo;
            case AmmoType.SMG: return smgAmmo;
            case AmmoType.AssaultRifle: return arAmmo;
            case AmmoType.Sniper: return sniperAmmo;
            case AmmoType.LMG: return lmgAmmo;
            case AmmoType.Railgun: return railgunAmmo;
            default: return 0;
        }
    }

    public void SetAmmoCount(AmmoType type, int amount)
    {
        switch (type)
        {
            case AmmoType.Pistol: pistolAmmo = amount; break;
            case AmmoType.Shotgun: shotgunAmmo = amount; break;
            case AmmoType.SMG: smgAmmo = amount; break;
            case AmmoType.AssaultRifle: arAmmo = amount; break;
            case AmmoType.Sniper: sniperAmmo = amount; break;
            case AmmoType.LMG: lmgAmmo = amount; break;
            case AmmoType.Railgun: railgunAmmo = amount; break;
        }
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
}