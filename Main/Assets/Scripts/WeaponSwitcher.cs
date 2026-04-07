using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapon Arsenal")]
    public GameObject[] weapons; 
    
    private int currentWeaponIndex = 0;

    void Start()
    {
        EquipWeapon(currentWeaponIndex); 
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        int previousWeapon = currentWeaponIndex;

        // --- MOUSE SCROLL WHEEL ---
        float scrollDelta = Mouse.current.scroll.ReadValue().y;
        
        if (scrollDelta > 0) // Scrolled Up
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0) currentWeaponIndex = weapons.Length - 1;
        }
        else if (scrollDelta < 0) // Scrolled Down
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= weapons.Length) currentWeaponIndex = 0;
        }

        // --- NUMBER KEYS ---
        // Light Arsenal
        if (Keyboard.current.digit1Key.wasPressedThisFrame && weapons.Length > 0) currentWeaponIndex = 0; // Pistol
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1) currentWeaponIndex = 1; // SMG_F
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Length > 2) currentWeaponIndex = 2; // AR_A_1
        if (Keyboard.current.digit4Key.wasPressedThisFrame && weapons.Length > 3) currentWeaponIndex = 3; // NEW: SMG_J
        
        // Heavy Arsenal
        if (Keyboard.current.digit5Key.wasPressedThisFrame && weapons.Length > 4) currentWeaponIndex = 4; // Shotgun
        if (Keyboard.current.digit6Key.wasPressedThisFrame && weapons.Length > 5) currentWeaponIndex = 5; // LMG
        if (Keyboard.current.digit7Key.wasPressedThisFrame && weapons.Length > 6) currentWeaponIndex = 6; // Recon
        if (Keyboard.current.digit8Key.wasPressedThisFrame && weapons.Length > 7) currentWeaponIndex = 7; // Railgun

        if (previousWeapon != currentWeaponIndex)
        {
            EquipWeapon(currentWeaponIndex);
        }
    }

    void EquipWeapon(int targetIndex)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(i == targetIndex);
            }
        }
    }
}