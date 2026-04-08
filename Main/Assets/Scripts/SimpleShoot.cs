using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; 

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
    public int magSize = 30;          
    private int currentAmmo;          
    private bool isReloading = false; 
    
    // NEW: How far down the gun drops (make this bigger for the LMG!)
    public float hideDistance = 1.2f; 
    // NEW: How fast the gun slides down and up (in seconds)
    public float slideDuration = 0.3f; 

    [Header("Audio")]
    public AudioSource weaponAudio;
    public AudioClip fireSound;
    public AudioClip reloadSound;     
    
    [Header("Visuals")]
    public GameObject impactEffectPrefab;

    private Vector3 originalPosition; 
    private bool hasStarted = false;

    void Start()
    {
        originalPosition = transform.localPosition;
        currentAmmo = magSize; 
        hasStarted = true;
    }

    void OnEnable()
    {
        // If we pull the gun out, make sure it is fully loaded and in the right spot!
        isReloading = false;
        if (hasStarted) transform.localPosition = originalPosition;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        if (isReloading) return;

        // --- RELOAD INPUT ---
        if (currentAmmo <= 0 || Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo < magSize) 
            {
                StartCoroutine(SmoothReloadRoutine());
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

    IEnumerator SmoothReloadRoutine()
    {
        isReloading = true;
        
        if (weaponAudio != null && reloadSound != null)
        {
            weaponAudio.PlayOneShot(reloadSound);
        }

        Vector3 targetHiddenPosition = originalPosition - new Vector3(0f, hideDistance, 0f);
        
        // Calculate how long we need to wait at the bottom 
        // (Audio length minus the time it takes to slide down AND slide back up)
        float audioLength = reloadSound != null ? reloadSound.length : 1.5f;
        float waitTimeAtBottom = audioLength - (slideDuration * 2f);
        if (waitTimeAtBottom < 0) waitTimeAtBottom = 0.1f; // Failsafe in case the audio is super short

        float elapsedTime = 0f;

        // 1. SLIDE DOWN SMOOTHLY
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetHiddenPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait until next frame
        }
        transform.localPosition = targetHiddenPosition; // Ensure it reaches the exact bottom

        // 2. WAIT FOR AUDIO
        yield return new WaitForSeconds(waitTimeAtBottom);

        // 3. SLIDE BACK UP SMOOTHLY
        elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(targetHiddenPosition, originalPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = originalPosition; // Ensure it snaps exactly to the proper aim point
        
        currentAmmo = magSize;
        isReloading = false;
    }

    void Shoot()
    {
        currentAmmo--; 

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