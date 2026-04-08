using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // <-- Brought this back for the timer!

public class SimpleShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 100f;
    public float damage = 20f;
    
    [Header("Weapon Settings")]
    public bool isAutomatic = false; 
    public float fireRate = 10f; 
    private float nextTimeToFire = 0f; 

    [Header("Ammo & Reloading")]
    public int magSize = 30;          // How many bullets per magazine
    private int currentAmmo;          // How many bullets we currently have
    private bool isReloading = false; 

    [Header("Audio")]
    public AudioSource weaponAudio;
    public AudioClip fireSound;
    public AudioClip reloadSound;     // <-- New Reload Sound!
    
    [Header("Visuals")]
    public GameObject impactEffectPrefab;

    private Vector3 originalPosition; 
    private bool hasStarted = false;

    void Start()
    {
        // Save exactly where the gun is supposed to be held
        originalPosition = transform.localPosition;
        currentAmmo = magSize; // Start with a full clip
        hasStarted = true;
    }

    // SAFETY NET: If we switch weapons while reloading, this resets the gun 
    // so it isn't stuck off-screen the next time we pull it out!
    void OnEnable()
    {
        isReloading = false;
        if (hasStarted) transform.localPosition = originalPosition;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // If we are currently reloading, stop the script here. No shooting allowed!
        if (isReloading) return;

        // --- RELOAD INPUT ---
        // Reload if we press 'R' OR if we try to shoot with 0 bullets
        if (currentAmmo <= 0 || Keyboard.current.rKey.wasPressedThisFrame)
        {
            // Only reload if we aren't already at max ammo
            if (currentAmmo < magSize) 
            {
                StartCoroutine(ReloadRoutine());
                return;
            }
        }

        // --- SHOOTING LOGIC ---
        if (isAutomatic)
        {
            if (Mouse.current.leftButton.isPressed && Time.time >= nextTimeToFire && currentAmmo > 0)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextTimeToFire && currentAmmo > 0)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        
        // 1. Play the reload sound
        if (weaponAudio != null && reloadSound != null)
        {
            weaponAudio.PlayOneShot(reloadSound);
        }

        // 2. Hide the gun by dropping it straight down by 0.5 meters
        transform.localPosition = originalPosition - new Vector3(0f, 0.5f, 0f);

        // 3. Wait for the exact length of the audio file! 
        // (If there is no audio file, wait 1.5 seconds by default)
        float reloadTime = reloadSound != null ? reloadSound.length : 1.5f;
        yield return new WaitForSeconds(reloadTime);

        // 4. Snap the gun back up to its normal position
        transform.localPosition = originalPosition;
        
        // 5. Refill ammo and allow shooting again
        currentAmmo = magSize;
        isReloading = false;
    }

    void Shoot()
    {
        currentAmmo--; // Subtract a bullet!

        if (weaponAudio != null && fireSound != null)
        {
            weaponAudio.PlayOneShot(fireSound);
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
}