using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Needed for the flash timer

public class SimpleShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 100f;
    public float damage = 20f;
    
    [Header("Visuals")]
    public GameObject muzzleFlashObject; // Changed to GameObject for the hack
    public GameObject impactEffectPrefab; // The sparks prefab

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // 1. Play muzzle flash hack
        if (muzzleFlashObject != null)
        {
            StartCoroutine(FlashMuzzle());
        }

        // 2. The Raycast
        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);
            
            // 3. Spawn impact sparks facing away from the surface
            if (impactEffectPrefab != null)
            {
                // Instantiate the sparks, rotating them to align with the wall's normal
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    // A coroutine that turns the flash object on, waits 0.05 seconds, then turns it off
    IEnumerator FlashMuzzle()
    {
        muzzleFlashObject.SetActive(true);
        yield return new WaitForSeconds(0.05f); 
        muzzleFlashObject.SetActive(false);
    }
}