using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic; 

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Master Arsenal (Drag all your child weapons here!)")]
    [Tooltip("Drag the actual weapon objects from the player's hands into these slots.")]
    public GameObject meleeWeapon; 
    public GameObject pistol;      
    public GameObject shotgun;
    public GameObject smg1; // <-- AGENT ZETA TACTIC: Arya - 9
    public GameObject smg2; // <-- AGENT ZETA TACTIC: Krizz Vector
    public GameObject assaultRifle;
    public GameObject sniper;
    public GameObject lmg;
    public GameObject railgun;

    [Header("Currently Equipped Loadout")]
    [Tooltip("Do NOT touch this! The script will automatically fill this based on your Gacha unlocks!")]
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
        // ==========================================
        // AGENT ZETA DYNAMIC ARSENAL BUILDER
        // ==========================================
        List<GameObject> dynamicLoadout = new List<GameObject>();

        if (meleeWeapon != null) dynamicLoadout.Add(meleeWeapon);
        if (pistol != null) dynamicLoadout.Add(pistol);
        
        if (PlayerPrefs.GetInt("Unlocked_Shotgun", 0) == 1 && shotgun != null) dynamicLoadout.Add(shotgun);
        
        // --- THE TWIN SMG LOGIC ---
        if (PlayerPrefs.GetInt("Unlocked_SMG", 0) == 1)
        {
            if (smg1 != null) dynamicLoadout.Add(smg1);
            if (smg2 != null) dynamicLoadout.Add(smg2);
        }
        // --------------------------

        if (PlayerPrefs.GetInt("Unlocked_AssaultRifle", 0) == 1 && assaultRifle != null) dynamicLoadout.Add(assaultRifle);
        if (PlayerPrefs.GetInt("Unlocked_Sniper", 0) == 1 && sniper != null) dynamicLoadout.Add(sniper);
        if (PlayerPrefs.GetInt("Unlocked_LMG", 0) == 1 && lmg != null) dynamicLoadout.Add(lmg);
        if (PlayerPrefs.GetInt("Unlocked_Railgun", 0) == 1 && railgun != null) dynamicLoadout.Add(railgun);

        equippedWeapons = dynamicLoadout.ToArray();
        // ==========================================

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        if (equippedWeapons != null && equippedWeapons.Length > 0)
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