using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Your Weapons")]
    public GameObject[] weapons; 
    
    [Header("Settings")]
    public float switchDelay = 0.5f; 
    
    private int currentWeaponIndex = 0;
    private bool isSwitching = false;

    // Changed to Awake so it hides the unequipped guns before the camera even renders!
    void Awake()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(i == currentWeaponIndex);
            }
        }
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

        // --- NUMBER KEY SWITCHING (1-8) ---
        if (Keyboard.current.digit1Key.wasPressedThisFrame) currentWeaponIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1) currentWeaponIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Length > 2) currentWeaponIndex = 2;
        if (Keyboard.current.digit4Key.wasPressedThisFrame && weapons.Length > 3) currentWeaponIndex = 3;
        if (Keyboard.current.digit5Key.wasPressedThisFrame && weapons.Length > 4) currentWeaponIndex = 4;
        if (Keyboard.current.digit6Key.wasPressedThisFrame && weapons.Length > 5) currentWeaponIndex = 5;
        if (Keyboard.current.digit7Key.wasPressedThisFrame && weapons.Length > 6) currentWeaponIndex = 6;
        if (Keyboard.current.digit8Key.wasPressedThisFrame && weapons.Length > 7) currentWeaponIndex = 7;

        if (previousWeapon != currentWeaponIndex)
        {
            StartCoroutine(SwitchWeaponRoutine(previousWeapon, currentWeaponIndex));
        }
    }

    IEnumerator SwitchWeaponRoutine(int oldIndex, int newIndex)
    {
        isSwitching = true;

        SimpleShoot oldGun = weapons[oldIndex].GetComponent<SimpleShoot>();
        if (oldGun != null)
        {
            oldGun.StartCoroutine(oldGun.HolsterWeaponRoutine());
        }

        yield return new WaitForSeconds(switchDelay);

        weapons[oldIndex].SetActive(false);
        weapons[newIndex].SetActive(true);

        isSwitching = false;
    }
}