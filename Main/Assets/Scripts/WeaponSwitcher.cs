using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Your Weapons")]
    public GameObject[] weapons; 
    
    [Header("Settings")]
    public float switchDelay = 0.5f; 

    [Header("UI Elements")]
    public GameObject ammoCounterUI; // <-- Slot for your Canvas Ammo Counter
    
    private int currentWeaponIndex = 0;
    private bool isSwitching = false;

    void Awake()
    {
        // --- NEW: AUTO-ASSIGN AMMO COUNTER UI ---
        if (ammoCounterUI == null)
        {
            ammoCounterUI = GameObject.Find("AmmoCounter");
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(i == currentWeaponIndex);
            }
        }
    }

    void Start()
    {
        // Make sure the UI is correct the exact second the game starts!
        UpdateAmmoUIState(currentWeaponIndex);
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;
        
        if (isSwitching || weapons.Length <= 1) return;

        int previousWeapon = currentWeaponIndex;

        // --- SCROLL WHEEL SWITCHING ---
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0f)
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= weapons.Length) currentWeaponIndex = 0;
        }
        else if (scroll < 0f)
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0) currentWeaponIndex = weapons.Length - 1;
        }

        // --- NUMBER KEY SWITCHING (1-9) ---
        if (Keyboard.current.digit1Key.wasPressedThisFrame) currentWeaponIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1) currentWeaponIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Length > 2) currentWeaponIndex = 2;
        if (Keyboard.current.digit4Key.wasPressedThisFrame && weapons.Length > 3) currentWeaponIndex = 3;
        if (Keyboard.current.digit5Key.wasPressedThisFrame && weapons.Length > 4) currentWeaponIndex = 4;
        if (Keyboard.current.digit6Key.wasPressedThisFrame && weapons.Length > 5) currentWeaponIndex = 5;
        if (Keyboard.current.digit7Key.wasPressedThisFrame && weapons.Length > 6) currentWeaponIndex = 6;
        if (Keyboard.current.digit8Key.wasPressedThisFrame && weapons.Length > 7) currentWeaponIndex = 7;
        if (Keyboard.current.digit9Key.wasPressedThisFrame && weapons.Length > 8) currentWeaponIndex = 8;

        if (previousWeapon != currentWeaponIndex)
        {
            StartCoroutine(SwitchWeaponRoutine(previousWeapon, currentWeaponIndex));
        }
    }

    IEnumerator SwitchWeaponRoutine(int oldIndex, int newIndex)
    {
        isSwitching = true;

        // 1. Check if the old weapon was a GUN
        SimpleShoot oldGun = weapons[oldIndex].GetComponent<SimpleShoot>();
        if (oldGun != null)
        {
            yield return StartCoroutine(oldGun.HolsterWeaponRoutine());
        }

        // 2. Check if the old weapon was the SWORD
        SimpleMelee oldMelee = weapons[oldIndex].GetComponent<SimpleMelee>();
        if (oldMelee != null)
        {
            yield return StartCoroutine(oldMelee.HolsterWeaponRoutine());
        }

        // 3. Fallback wait if it has neither script
        if (oldGun == null && oldMelee == null)
        {
            yield return new WaitForSeconds(switchDelay);
        }

        weapons[oldIndex].SetActive(false);
        weapons[newIndex].SetActive(true);

        // --- Update the Ammo UI Visibility! ---
        UpdateAmmoUIState(newIndex);

        isSwitching = false;
    }

    void UpdateAmmoUIState(int index)
    {
        if (ammoCounterUI != null && weapons[index] != null)
        {
            // If the active weapon has a SimpleShoot script, it's a gun! Turn UI ON.
            // If it doesn't (like the Sword), turn UI OFF.
            bool isGun = weapons[index].GetComponent<SimpleShoot>() != null;
            ammoCounterUI.SetActive(isGun);
        }
    }
}