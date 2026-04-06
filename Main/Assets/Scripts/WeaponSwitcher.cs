using UnityEngine;
using UnityEngine.InputSystem; // Needed for the new input system

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Your Weapons")]
    public GameObject smg;
    public GameObject ar;

    void Start()
    {
        // When the game starts, let's make sure the SMG is equipped by default
        EquipSMG();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Press '3' to equip the SMG
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            EquipSMG();
        }

        // Press '4' to equip the AR
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            EquipAR();
        }
    }

    void EquipSMG()
    {
        if (smg != null) smg.SetActive(true);
        if (ar != null) ar.SetActive(false);
    }

    void EquipAR()
    {
        if (smg != null) smg.SetActive(false);
        if (ar != null) ar.SetActive(true);
    }
}