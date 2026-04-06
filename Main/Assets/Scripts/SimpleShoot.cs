using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SimpleShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 100f;
    public float damage = 20f;
    
    [Header("Auto Fire Settings")]
    public float fireRate = 10f; // How many bullets per second
    private float nextTimeToFire = 0f; // Internal timer
    
    [Header("Visuals")]
    public GameObject muzzleFlashObject; 
    public GameObject impactEffectPrefab;

    void Update()
    {
        if (Mouse.current == null) return;

        // CHANGED: 'isPressed' checks if the button is held down.
        // We also check if enough time has passed since the last shot.
        if (Mouse.current.leftButton.isPressed && Time.time >= nextTimeToFire)
        {
            // Reset the timer for the next bullet
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (muzzleFlashObject != null)
        {
            StartCoroutine(FlashMuzzle());
        }

        RaycastHit hit;
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            EnemyHealth target = hit.collider.GetComponentInParent<EnemyHealth>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    IEnumerator FlashMuzzle()
    {
        muzzleFlashObject.SetActive(true);
        // Made the flash slightly faster so it looks good on high fire rates
        yield return new WaitForSeconds(0.03f); 
        muzzleFlashObject.SetActive(false);
    }
}