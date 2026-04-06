using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapon Arsenal")]
    // This creates a neat list in the Inspector instead of individual slots!
    public GameObject[] weapons; 
    
    private int currentWeaponIndex = 0;

    void Start()
    {
        // Start by equipping the first weapon in the list
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
            if (currentWeaponIndex < 0) currentWeaponIndex = weapons.Length - 1; // Wrap around to the end
        }
        else if (scrollDelta < 0) // Scrolled Down
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= weapons.Length) currentWeaponIndex = 0; // Wrap around to the start
        }

        // --- NUMBER KEYS ---
        // (1 is index 0, 2 is index 1, 3 is index 2, etc.)
        if (Keyboard.current.digit1Key.wasPressedThisFrame && weapons.Length > 0) currentWeaponIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1) currentWeaponIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Length > 2) currentWeaponIndex = 2;

        // If the index changed this frame, swap the weapons!
        if (previousWeapon != currentWeaponIndex)
        {
            EquipWeapon(currentWeaponIndex);
        }
    }

    void EquipWeapon(int targetIndex)
    {
        // Loop through all our weapons. 
        // Turn ON the one that matches our target index, turn OFF the rest.
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(i == targetIndex);
            }
        }
    }
}