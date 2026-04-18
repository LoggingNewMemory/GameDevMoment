using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Currently Equipped Loadout")]
    public GameObject[] equippedWeapons; 
    
    [Header("Settings")]
    public float switchDelay = 0.5f; 

    [Header("UI Elements")]
    public GameObject ammoCounterUI; 
    
    private int currentWeaponIndex = 0;
    private bool isSwitching = false;

    void Awake()
    {
        if (ammoCounterUI == null)
        {
            ammoCounterUI = GameObject.Find("AmmoCounter");
        }
    }

    void Start()
    {
        // --- THE LIFECYCLE FIX ---
        // By moving this to Start(), we guarantee every gun has already run its Awake() 
        // and perfectly memorized its position before we start turning them invisible!
        
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        // Now, safely turn on ONLY the weapons in our chosen loadout
        if (equippedWeapons != null)
        {
            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] != null)
                {
                    equippedWeapons[i].SetActive(i == currentWeaponIndex);
                }
            }
        }

        UpdateAmmoUIState(currentWeaponIndex);
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;
        
        if (isSwitching || equippedWeapons == null || equippedWeapons.Length <= 1) return;

        int previousWeapon = currentWeaponIndex;

        // --- SCROLL WHEEL SWITCHING ---
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll > 0f)
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= equippedWeapons.Length) currentWeaponIndex = 0;
        }
        else if (scroll < 0f)
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0) currentWeaponIndex = equippedWeapons.Length - 1;
        }

        // --- NUMBER KEY SWITCHING ---
        if (Keyboard.current.digit1Key.wasPressedThisFrame && equippedWeapons.Length > 0) currentWeaponIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && equippedWeapons.Length > 1) currentWeaponIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && equippedWeapons.Length > 2) currentWeaponIndex = 2;
        if (Keyboard.current.digit4Key.wasPressedThisFrame && equippedWeapons.Length > 3) currentWeaponIndex = 3;
        if (Keyboard.current.digit5Key.wasPressedThisFrame && equippedWeapons.Length > 4) currentWeaponIndex = 4;
        if (Keyboard.current.digit6Key.wasPressedThisFrame && equippedWeapons.Length > 5) currentWeaponIndex = 5;
        if (Keyboard.current.digit7Key.wasPressedThisFrame && equippedWeapons.Length > 6) currentWeaponIndex = 6;
        if (Keyboard.current.digit8Key.wasPressedThisFrame && equippedWeapons.Length > 7) currentWeaponIndex = 7;
        if (Keyboard.current.digit9Key.wasPressedThisFrame && equippedWeapons.Length > 8) currentWeaponIndex = 8;

        if (previousWeapon != currentWeaponIndex)
        {
            StartCoroutine(SwitchWeaponRoutine(previousWeapon, currentWeaponIndex));
        }
    }

    IEnumerator SwitchWeaponRoutine(int oldIndex, int newIndex)
    {
        isSwitching = true;

        if (equippedWeapons[oldIndex] != null)
        {
            SimpleShoot oldGun = equippedWeapons[oldIndex].GetComponent<SimpleShoot>();
            if (oldGun != null) yield return StartCoroutine(oldGun.HolsterWeaponRoutine());

            SimpleMelee oldMelee = equippedWeapons[oldIndex].GetComponent<SimpleMelee>();
            if (oldMelee != null) yield return StartCoroutine(oldMelee.HolsterWeaponRoutine());

            if (oldGun == null && oldMelee == null) yield return new WaitForSeconds(switchDelay);

            equippedWeapons[oldIndex].SetActive(false);
        }

        if (equippedWeapons[newIndex] != null)
        {
            equippedWeapons[newIndex].SetActive(true);
        }

        UpdateAmmoUIState(newIndex);
        isSwitching = false;
    }

    void UpdateAmmoUIState(int index)
    {
        if (ammoCounterUI != null && equippedWeapons != null && equippedWeapons.Length > index && equippedWeapons[index] != null)
        {
            bool isGun = equippedWeapons[index].GetComponent<SimpleShoot>() != null;
            ammoCounterUI.SetActive(isGun);
        }
    }

    public void SetNewLoadout(GameObject[] newWeapons)
    {
        if (equippedWeapons != null && equippedWeapons.Length > 0 && equippedWeapons[currentWeaponIndex] != null)
        {
            equippedWeapons[currentWeaponIndex].SetActive(false);
        }

        equippedWeapons = newWeapons;
        currentWeaponIndex = 0;

        if (equippedWeapons != null && equippedWeapons.Length > 0 && equippedWeapons[0] != null)
        {
            equippedWeapons[0].SetActive(true);
            UpdateAmmoUIState(0);
        }
    }
}