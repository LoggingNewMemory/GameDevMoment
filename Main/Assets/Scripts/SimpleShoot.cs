using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 100f;
    public float damage = 20f;
    
    [Header("Weapon Settings")]
    public bool isAutomatic = false; 
    public float fireRate = 10f; 
    private float nextTimeToFire = 0f; 
    
    [Header("Visuals")]
    public GameObject impactEffectPrefab;

    void Update()
    {
        if (Mouse.current == null) return;

        // AUTOMATIC FIRE (Hold left click)
        if (isAutomatic)
        {
            if (Mouse.current.leftButton.isPressed && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
        // SINGLE FIRE (Tap left click)
        else
        {
            // wasPressedThisFrame forces you to release and click again!
            if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
    }

    void Shoot()
    {
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
}