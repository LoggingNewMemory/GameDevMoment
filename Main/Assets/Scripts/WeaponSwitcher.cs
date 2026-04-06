using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Your Weapons")]
    public GameObject pistol;
    public GameObject smg;
    public GameObject ar;

    void Start()
    {
        EquipPistol(); // Start with the pistol equipped
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit2Key.wasPressedThisFrame) EquipPistol();
        if (Keyboard.current.digit3Key.wasPressedThisFrame) EquipSMG();
        if (Keyboard.current.digit4Key.wasPressedThisFrame) EquipAR();
    }

    void EquipPistol()
    {
        if (pistol != null) pistol.SetActive(true);
        if (smg != null) smg.SetActive(false);
        if (ar != null) ar.SetActive(false);
    }

    void EquipSMG()
    {
        if (pistol != null) pistol.SetActive(false);
        if (smg != null) smg.SetActive(true);
        if (ar != null) ar.SetActive(false);
    }

    void EquipAR()
    {
        if (pistol != null) pistol.SetActive(false);
        if (smg != null) smg.SetActive(false);
        if (ar != null) ar.SetActive(true);
    }
}