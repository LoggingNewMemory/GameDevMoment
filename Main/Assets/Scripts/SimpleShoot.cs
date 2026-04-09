using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; 
using TMPro; 
using UnityEngine.UI;

public class SimpleShoot : MonoBehaviour
{
    public Camera fpsCamera;
    public float range = 100f;
    public float damage = 20f;
    
    [Header("Weapon Settings")]
    public bool isAutomatic = false; 
    public bool isBoltAction = false; 
    public float fireRate = 10f; 
    private float nextTimeToFire = 0f; 

    [Header("Equip Settings (Switching Guns)")]
    public float drawDuration = 0.5f; 
    public Vector3 drawOffset = new Vector3(0.5f, -0.8f, 0f); 
    private bool isDrawing = false; 
    
    [Header("Chambering Settings (Bolt/Pump)")]
    public float chamberHideDistance = 0.3f; 
    public float chamberDuration = 1.5f;     

    [Header("Reload & Reserve Settings")]
    public int magSize = 30;          
    public int reserveAmmo = 90;       
    public int maxReserveAmmo = 180;   
    
    private int currentAmmo;          
    private bool isReloading = false; 
    private bool isChambering = false; 
    public float hideDistance = 1.2f; 
    public float slideDuration = 0.3f; 

    [Header("Audio: Mag Reloads (AR/SMG)")]
    public AudioClip reloadSound;     
    
    [Header("Audio: Shotgun Reloads")]
    public bool isShotgunReload = false; 
    public AudioClip insertShellSound;   
    public AudioClip pumpSound;          

    [Header("Audio: Shooting")]
    public AudioSource weaponAudio;
    public AudioClip fireSound; 
    public AudioClip equipSound; 

    [Header("Visuals & UI")]
    public GameObject impactEffectPrefab;
    public TextMeshProUGUI ammoTextDisplay; 
    public Image crosshairDisplay;   
    public Sprite weaponCrosshair;

    [Header("Railgun Visuals")]
    public Transform muzzlePoint;     // <-- NEW: Drag the tip of your gun here!
    public GameObject beamEffectPrefab; // <-- NEW: Drag RailgunBeamEffect Prefab here!
    private BeamFader spawnedBeamFader; // Reference to the script in the spawned beam

    private Vector3 originalPosition; 
    private bool hasStarted = false;

    void Awake()
    {
        originalPosition = transform.localPosition;
        currentAmmo = magSize; 
        hasStarted = true;
    }

    void Start()
    {
        UpdateAmmoUI(); 
        UpdateCrosshairUI();
    }

    void OnEnable()
    {
        isReloading = false;
        isChambering = false; 
        isDrawing = false;
        
        if (hasStarted) 
        {
            transform.localPosition = originalPosition + drawOffset;
            UpdateAmmoUI(); 
            UpdateCrosshairUI();
            StartCoroutine(DrawWeaponRoutine());
        }
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;
        if (isReloading || isChambering || isDrawing) return;

        // --- RELOAD INPUT ---
        if (currentAmmo <= 0 || Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo < magSize && reserveAmmo > 0) 
            {
                if (isShotgunReload) StartCoroutine(ShotgunReloadRoutine());
                else StartCoroutine(SmoothReloadRoutine());
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

    void UpdateAmmoUI()
    {
        if (ammoTextDisplay != null) ammoTextDisplay.text = currentAmmo + " / " + reserveAmmo;
    }

    void UpdateCrosshairUI()
    {
        if (crosshairDisplay != null && weaponCrosshair != null) crosshairDisplay.sprite = weaponCrosshair;
    }

    IEnumerator DrawWeaponRoutine()
    {
        isDrawing = true;
        if (weaponAudio != null && equipSound != null) weaponAudio.PlayOneShot(equipSound);
        Vector3 startPos = originalPosition + drawOffset;
        float elapsedTime = 0f;
        while (elapsedTime < drawDuration)
        {
            transform.localPosition = Vector3.Lerp(startPos, originalPosition, elapsedTime / drawDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = originalPosition;
        isDrawing = false;
    }

    IEnumerator SmoothReloadRoutine()
    {
        isReloading = true;
        if (weaponAudio != null && reloadSound != null) weaponAudio.PlayOneShot(reloadSound);
        Vector3 targetHiddenPosition = originalPosition - new Vector3(0f, hideDistance, 0f);
        float audioLength = reloadSound != null ? reloadSound.length : 1.5f;
        float waitTimeAtBottom = audioLength - (slideDuration * 2f);
        if (waitTimeAtBottom < 0) waitTimeAtBottom = 0.1f; 
        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetHiddenPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = targetHiddenPosition; 
        yield return new WaitForSeconds(waitTimeAtBottom);
        elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(targetHiddenPosition, originalPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = originalPosition; 
        
        int ammoNeeded = magSize - currentAmmo;
        if (reserveAmmo >= ammoNeeded) { currentAmmo += ammoNeeded; reserveAmmo -= ammoNeeded; }
        else { currentAmmo += reserveAmmo; reserveAmmo = 0; }
        UpdateAmmoUI(); 
        isReloading = false;
    }

    IEnumerator ShotgunReloadRoutine()
    {
        isReloading = true;
        Vector3 targetHiddenPosition = originalPosition - new Vector3(0f, hideDistance, 0f);
        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetHiddenPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = targetHiddenPosition;
        int shellsNeeded = magSize - currentAmmo;
        for (int i = 0; i < shellsNeeded; i++)
        {
            if (reserveAmmo <= 0) break;
            if (weaponAudio != null && insertShellSound != null) { weaponAudio.PlayOneShot(insertShellSound); yield return new WaitForSeconds(insertShellSound.length); }
            else yield return new WaitForSeconds(0.4f); 
            currentAmmo++; reserveAmmo--; UpdateAmmoUI(); 
        }
        if (weaponAudio != null && pumpSound != null) { weaponAudio.PlayOneShot(pumpSound); yield return new WaitForSeconds(pumpSound.length); }
        elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(targetHiddenPosition, originalPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = originalPosition;
        isReloading = false;
    }

    void Shoot()
    {
        currentAmmo--; 
        UpdateAmmoUI(); 
        if (weaponAudio != null && fireSound != null) weaponAudio.PlayOneShot(fireSound);

        RaycastHit hit;
        // Start the ray from the camera as usual for aiming
        Vector3 rayOrigin = fpsCamera.transform.position;
        Vector3 rayDirection = fpsCamera.transform.forward;

        // Where the beam visually ENDS
        Vector3 visualEndPoint; 

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            EnemyHealth target = hit.collider.GetComponentInParent<EnemyHealth>();
            if (target != null)
            {
                float finalDamage = damage;
                PlayerStats stats = GetComponentInParent<PlayerStats>();
                if (stats != null && stats.isDrunk) finalDamage *= 1.2f;
                target.TakeDamage(finalDamage);
            }
            if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            // Set the visual end point to the hit point
            visualEndPoint = hit.point;
        }
        else
        {
            // If we hit nothing, the beam goes to the maximum range
            visualEndPoint = rayOrigin + (rayDirection * range);
        }

        // --- NEW: TRIGGER RAILGUN VISUALS ---
        if (muzzlePoint != null && beamEffectPrefab != null)
        {
            HandleBeamVisuals(visualEndPoint);
        }

        if (isBoltAction) StartCoroutine(ChamberRoundRoutine());
    }

    // Function to manage visual beam objects efficiently
    void HandleBeamVisuals(Vector3 endPoint)
    {
        // For performance, we create ONE beam effect object and reuse it!
        if (spawnedBeamFader == null)
        {
            // Spawn the prefab
            GameObject newBeam = Instantiate(beamEffectPrefab, Vector3.zero, Quaternion.identity);
            // Grab its fader script
            spawnedBeamFader = newBeam.GetComponent<BeamFader>();
        }

        // Tell the fader to draw and fade the line from the gun tip to the hit point
        spawnedBeamFader.ActivateBeam(muzzlePoint.position, endPoint);
    }

    IEnumerator ChamberRoundRoutine()
    {
        isChambering = true; 
        Vector3 targetDipPosition = originalPosition - new Vector3(0f, chamberHideDistance, 0f);
        float waitTimeAtBottom = chamberDuration - (slideDuration * 2f);
        if (waitTimeAtBottom < 0) waitTimeAtBottom = 0.05f;
        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetDipPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = targetDipPosition;
        yield return new WaitForSeconds(waitTimeAtBottom);
        elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            transform.localPosition = Vector3.Lerp(targetDipPosition, originalPosition, elapsedTime / slideDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = originalPosition;
        isChambering = false; 
    }

    public IEnumerator HolsterWeaponRoutine()
    {
        isDrawing = true; isReloading = false; isChambering = false;
        Vector3 currentPos = transform.localPosition; 
        Vector3 targetPos = originalPosition + drawOffset;
        float elapsedTime = 0f;
        while (elapsedTime < drawDuration)
        {
            transform.localPosition = Vector3.Lerp(currentPos, targetPos, elapsedTime / drawDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        transform.localPosition = targetPos;
    }

    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
        
        // Don't let the player carry a million bullets!
        if (reserveAmmo > maxReserveAmmo)
        {
            reserveAmmo = maxReserveAmmo;
        }
        
        UpdateAmmoUI();
    }
}