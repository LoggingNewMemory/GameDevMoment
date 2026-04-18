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

    [Header("Weapon Ammo Type")]
    [Tooltip("Select the matching ammo type so the gun can grab bullets from the backpack!")]
    public AmmoType weaponAmmoType;
    
    [Header("Weapon Settings")]
    public bool isAutomatic = false; 
    public bool isBoltAction = false; 
    public float fireRate = 10f; 
    private float nextTimeToFire = 0f; 

    [Header("Shotgun Settings")]
    public bool isShotgun = false;
    public int pelletCount = 8;
    public float spreadAngle = 3f;

    [Header("Railgun Settings")]
    public bool isRailgun = false;

    [Header("Recoil & Knockback Settings")]
    public float playerKnockbackForce = 0f; 
    public float cameraKickForce = 1f;      
    public float weaponVisualKick = 0.1f;   
    public float weaponRecoverySpeed = 10f; 
    private DoomMovement playerMovement;    

    [Header("Equip Settings")]
    public float drawDuration = 0.5f; 
    public Vector3 drawOffset = new Vector3(0.5f, -0.8f, 0f); 
    private bool isDrawing = false; 
    
    [Header("Chambering Settings")]
    public float chamberHideDistance = 0.3f; 
    public float chamberDuration = 1.5f;     

    [Header("Reload & Reserve")]
    public int magSize = 30;          
    public int maxReserveAmmo = 180;   
    private int currentAmmo;          
    private bool isReloading = false; 
    private bool isChambering = false; 
    public float hideDistance = 1.2f; 
    public float slideDuration = 0.3f; 

    [Header("Audio")]
    public AudioClip reloadSound;     
    public bool isShotgunReload = false; 
    public AudioClip insertShellSound;   
    public AudioClip pumpSound;          
    public AudioSource weaponAudio;
    public AudioClip fireSound; 
    public AudioClip equipSound; 

    [Header("Visuals")]
    public GameObject impactEffectPrefab;
    public GameObject muzzleFlashPrefab; 
    public Transform muzzlePoint;        
    public TextMeshProUGUI ammoTextDisplay; 
    public Image crosshairDisplay;   
    public Sprite weaponCrosshair;

    [Header("Railgun Specific Effects")]
    public GameObject beamEffectPrefab; 
    private BeamFader spawnedBeamFader; 
    public bool useScreenFlash = false; 
    public Image screenFlashImage; 
    public Color flashColor = new Color(1f, 1f, 1f, 1f); 
    public float flashFadeDuration = 0.5f; 

    private Vector3 baseStartPos;    
    private Vector3 originalPosition; 
    private bool hasStarted = false;

    private PlayerSkills playerSkills;
    private bool wasHaluActive = false;
    
    private GameObject leftGunInstance;
    private Transform leftGunMuzzle;
    private BeamFader leftSpawnedBeamFader; 

    // --- THE FIX: The gun now strictly uses the Backpack for its ammo math! ---
    private PlayerAmmoStore ammoStore; 
    public int reserveAmmo
    {
        get { return ammoStore != null ? ammoStore.GetAmmoCount(weaponAmmoType) : 0; }
        set { if (ammoStore != null) ammoStore.SetAmmoCount(weaponAmmoType, value); }
    }

    void Awake()
    {
        // Find the backpack exactly once when the gun wakes up
        ammoStore = GetComponentInParent<PlayerAmmoStore>();
        
        baseStartPos = transform.localPosition; 
        originalPosition = baseStartPos;        
        
        currentAmmo = magSize; 
        hasStarted = true;
        
        playerMovement = GetComponentInParent<DoomMovement>();
        playerSkills = GetComponentInParent<PlayerSkills>(); 
    }

    void Start()
    {
        if (ammoTextDisplay == null)
        {
            GameObject ammoObj = GameObject.Find("AmmoCounter");
            if (ammoObj != null) ammoTextDisplay = ammoObj.GetComponent<TextMeshProUGUI>();
        }

        if (crosshairDisplay == null)
        {
            GameObject crosshairObj = GameObject.Find("Crosshair");
            if (crosshairObj != null) crosshairDisplay = crosshairObj.GetComponent<Image>();
        }

        if (isRailgun && useScreenFlash && screenFlashImage == null)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                Transform flashObj = mainCanvas.transform.Find("RailGun");
                if (flashObj != null) screenFlashImage = flashObj.GetComponent<Image>();
            }
        }

        UpdateAmmoUI(); 
        UpdateCrosshairUI();
    }

    void OnEnable()
    {
        isReloading = false; isChambering = false; isDrawing = false;
        if (hasStarted) 
        {
            transform.localPosition = originalPosition + drawOffset;
            UpdateAmmoUI(); UpdateCrosshairUI();
            StartCoroutine(DrawWeaponRoutine());
        }
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null || (playerMovement != null && playerMovement.isKnockedDown)) return;
        
        if (playerSkills != null)
        {
            if (playerSkills.isHaluActive && !wasHaluActive)
            {
                EnableDualWield();
                wasHaluActive = true;
            }
            else if (!playerSkills.isHaluActive && wasHaluActive)
            {
                DisableDualWield();
                wasHaluActive = false;
            }
        }

        if (!isReloading && !isChambering && !isDrawing)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, weaponRecoverySpeed * Time.unscaledDeltaTime);
        }

        if (isReloading || isChambering || isDrawing) return;

        if (currentAmmo <= 0 || Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo < magSize && reserveAmmo > 0) 
            {
                if (isShotgunReload) StartCoroutine(ShotgunReloadRoutine());
                else StartCoroutine(SmoothReloadRoutine());
                return;
            }
        }

        if (isAutomatic)
        {
            if (Mouse.current.leftButton.isPressed && Time.unscaledTime >= nextTimeToFire && currentAmmo > 0)
            {
                nextTimeToFire = Time.unscaledTime + 1f / fireRate; Shoot();
            }
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && Time.unscaledTime >= nextTimeToFire && currentAmmo > 0)
            {
                nextTimeToFire = Time.unscaledTime + 1f / fireRate; Shoot();
            }
        }
    }

    void LateUpdate()
    {
        if (leftGunInstance != null)
        {
            Vector3 targetPos = transform.localPosition;
            targetPos.x = -targetPos.x; 
            leftGunInstance.transform.localPosition = targetPos;
            leftGunInstance.transform.localRotation = transform.localRotation;
        }
    }

    void EnableDualWield()
    {
        leftGunInstance = Instantiate(gameObject, transform.parent);
        
        Destroy(leftGunInstance.GetComponent<SimpleShoot>()); 
        if (leftGunInstance.GetComponent("WeaponSway") != null) Destroy(leftGunInstance.GetComponent("WeaponSway"));
        if (leftGunInstance.GetComponent("WeaponSwitcher") != null) Destroy(leftGunInstance.GetComponent("WeaponSwitcher"));

        if (Mathf.Abs(baseStartPos.x) < 0.15f)
        {
            originalPosition = baseStartPos + new Vector3(0.4f, 0f, 0f);
        }

        if (muzzlePoint != null)
        {
            foreach (Transform child in leftGunInstance.GetComponentsInChildren<Transform>())
            {
                if (child.name == muzzlePoint.name)
                {
                    leftGunMuzzle = child;
                    break;
                }
            }
        }
    }

    void DisableDualWield()
    {
        if (leftGunInstance != null)
        {
            Destroy(leftGunInstance);
            leftGunInstance = null;
            leftGunMuzzle = null;
        }
        originalPosition = baseStartPos;
    }

    public void UpdateAmmoUI() { if (ammoTextDisplay != null) ammoTextDisplay.text = currentAmmo + " / " + reserveAmmo; }
    void UpdateCrosshairUI() { if (crosshairDisplay != null && weaponCrosshair != null) crosshairDisplay.sprite = weaponCrosshair; }

    public IEnumerator DrawWeaponRoutine()
    {
        isDrawing = true;
        if (weaponAudio != null && equipSound != null) weaponAudio.PlayOneShot(equipSound);
        Vector3 startPos = originalPosition + drawOffset;
        float elapsedTime = 0f;
        while (elapsedTime < drawDuration)
        {
            transform.localPosition = Vector3.Lerp(startPos, originalPosition, elapsedTime / drawDuration);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = originalPosition; isDrawing = false;
    }

    IEnumerator SmoothReloadRoutine()
    {
        isReloading = true;
        if (weaponAudio != null && reloadSound != null) weaponAudio.PlayOneShot(reloadSound);
        
        float speedMod = (playerSkills != null && playerSkills.isRageActive) ? playerSkills.rageSpeedMultiplier : 1f;
        float currentSlide = slideDuration / speedMod;

        Vector3 targetHiddenPosition = originalPosition - new Vector3(0f, hideDistance, 0f);
        float audioLength = reloadSound != null ? reloadSound.length : 1.5f;
        float waitTimeAtBottom = (audioLength / speedMod) - (currentSlide * 2f);
        if (waitTimeAtBottom < 0) waitTimeAtBottom = 0.1f; 

        float elapsedTime = 0f;
        while (elapsedTime < currentSlide)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetHiddenPosition, elapsedTime / currentSlide);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = targetHiddenPosition; 
        
        yield return new WaitForSecondsRealtime(waitTimeAtBottom);
        
        elapsedTime = 0f;
        while (elapsedTime < currentSlide)
        {
            transform.localPosition = Vector3.Lerp(targetHiddenPosition, originalPosition, elapsedTime / currentSlide);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = originalPosition; 
        
        int ammoNeeded = magSize - currentAmmo;
        if (reserveAmmo >= ammoNeeded) { currentAmmo += ammoNeeded; reserveAmmo -= ammoNeeded; }
        else { currentAmmo += reserveAmmo; reserveAmmo = 0; }
        UpdateAmmoUI(); isReloading = false;
    }

    IEnumerator ShotgunReloadRoutine()
    {
        isReloading = true;
        
        float speedMod = (playerSkills != null && playerSkills.isRageActive) ? playerSkills.rageSpeedMultiplier : 1f;
        float currentSlide = slideDuration / speedMod;

        Vector3 targetHiddenPosition = originalPosition - new Vector3(0f, hideDistance, 0f);
        float elapsedTime = 0f;
        while (elapsedTime < currentSlide)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetHiddenPosition, elapsedTime / currentSlide);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = targetHiddenPosition;
        
        int shellsNeeded = magSize - currentAmmo;
        for (int i = 0; i < shellsNeeded; i++)
        {
            if (reserveAmmo <= 0) break;
            
            if (weaponAudio != null && insertShellSound != null) 
            { 
                weaponAudio.PlayOneShot(insertShellSound); 
                yield return new WaitForSecondsRealtime(insertShellSound.length / speedMod); 
            }
            else yield return new WaitForSecondsRealtime(0.4f / speedMod); 
            
            currentAmmo++; reserveAmmo--; UpdateAmmoUI(); 
        }
        
        if (weaponAudio != null && pumpSound != null) 
        { 
            weaponAudio.PlayOneShot(pumpSound); 
            yield return new WaitForSecondsRealtime(pumpSound.length / speedMod); 
        }
        
        elapsedTime = 0f;
        while (elapsedTime < currentSlide)
        {
            transform.localPosition = Vector3.Lerp(targetHiddenPosition, originalPosition, elapsedTime / currentSlide);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = originalPosition; isReloading = false;
    }

    void Shoot()
    {
        currentAmmo--; UpdateAmmoUI(); 
        if (weaponAudio != null && fireSound != null) weaponAudio.PlayOneShot(fireSound);

        if (playerMovement != null) playerMovement.AddRecoil(playerKnockbackForce, cameraKickForce);
        transform.localPosition = originalPosition - new Vector3(0f, 0f, weaponVisualKick);

        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.SetParent(muzzlePoint);
            Destroy(flash, 0.05f); 
        }

        if (wasHaluActive && muzzleFlashPrefab != null && leftGunMuzzle != null)
        {
            GameObject flashL = Instantiate(muzzleFlashPrefab, leftGunMuzzle.position, leftGunMuzzle.rotation);
            flashL.transform.SetParent(leftGunMuzzle);
            Destroy(flashL, 0.05f); 
        }

        if (isRailgun && useScreenFlash && screenFlashImage != null)
        {
            StartCoroutine(ScreenFlashRoutine());
        }

        Vector3 rayOrigin = fpsCamera.transform.position;
        int gunsToFire = wasHaluActive ? 2 : 1;

        for (int g = 0; g < gunsToFire; g++)
        {
            Vector3 visualEndPoint = rayOrigin + (fpsCamera.transform.forward * range);
            int shotsToFire = isShotgun ? pelletCount : 1;

            for (int i = 0; i < shotsToFire; i++)
            {
                Vector3 rayDirection = fpsCamera.transform.forward;

                if (isShotgun)
                {
                    float spreadX = Random.Range(-spreadAngle, spreadAngle);
                    float spreadY = Random.Range(-spreadAngle, spreadAngle);
                    rayDirection = Quaternion.Euler(spreadX, spreadY, 0) * rayDirection;
                }

                float finalDamage = damage;
                PlayerStats stats = GetComponentInParent<PlayerStats>();
                if (stats != null && stats.isDrunk) finalDamage *= 1.2f;

                if (isRailgun)
                {
                    RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    foreach (RaycastHit hit in hits)
                    {
                        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                        if (target != null) target.TakeDamage(finalDamage);
                        if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                }
                else 
                {
                    if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                        if (target != null) target.TakeDamage(finalDamage);
                        if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        if (!isShotgun) visualEndPoint = hit.point;
                    }
                }
            }

            if (!isShotgun && beamEffectPrefab != null) 
            {
                if (g == 0 && muzzlePoint != null) HandleBeamVisuals(visualEndPoint, ref spawnedBeamFader, muzzlePoint);
                if (g == 1 && leftGunMuzzle != null) HandleBeamVisuals(visualEndPoint, ref leftSpawnedBeamFader, leftGunMuzzle);
            }
        }
        
        if (isBoltAction) StartCoroutine(ChamberRoundRoutine());
    }

    void HandleBeamVisuals(Vector3 endPoint, ref BeamFader fader, Transform muzzle)
    {
        if (fader == null)
        {
            GameObject newBeam = Instantiate(beamEffectPrefab, Vector3.zero, Quaternion.identity);
            fader = newBeam.GetComponent<BeamFader>();
        }
        fader.ActivateBeam(muzzle.position, endPoint);
    }

    IEnumerator ScreenFlashRoutine()
    {
        screenFlashImage.gameObject.SetActive(true);
        screenFlashImage.color = flashColor;
        float elapsed = 0f;
        while(elapsed < flashFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / flashFadeDuration);
            screenFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        screenFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        screenFlashImage.gameObject.SetActive(false); 
    }

    IEnumerator ChamberRoundRoutine()
    {
        isChambering = true; 
        
        float speedMod = (playerSkills != null && playerSkills.isRageActive) ? playerSkills.rageSpeedMultiplier : 1f;
        float currentSlide = slideDuration / speedMod;

        Vector3 targetDipPosition = originalPosition - new Vector3(0f, chamberHideDistance, 0f);
        float waitTimeAtBottom = (chamberDuration / speedMod) - (currentSlide * 2f);
        if (waitTimeAtBottom < 0) waitTimeAtBottom = 0.05f;

        float elapsedTime = 0f;
        while (elapsedTime < currentSlide)
        {
            transform.localPosition = Vector3.Lerp(originalPosition, targetDipPosition, elapsedTime / currentSlide);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = targetDipPosition;
        
        yield return new WaitForSecondsRealtime(waitTimeAtBottom);
        
        elapsedTime = 0f;
        while (elapsedTime < currentSlide)
        {
            transform.localPosition = Vector3.Lerp(targetDipPosition, originalPosition, elapsedTime / currentSlide);
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = originalPosition; isChambering = false; 
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
            elapsedTime += Time.unscaledDeltaTime; yield return null; 
        }
        transform.localPosition = targetPos;
    }

    public void InstantHide()
    {
        StopAllCoroutines(); 
        isDrawing = false; 
        isReloading = false; 
        isChambering = false;
        transform.localPosition = originalPosition + drawOffset; 
    }

    public void AddAmmo(int amount)
    {
        reserveAmmo += amount;
        if (reserveAmmo > maxReserveAmmo) reserveAmmo = maxReserveAmmo;
        UpdateAmmoUI();
    }

    public void EmptyMagazine()
    {
        currentAmmo = 0;
        UpdateAmmoUI();
    }

    public void StealReserveAmmo(int amount)
    {
        reserveAmmo -= amount;
        if (reserveAmmo < 0) reserveAmmo = 0;
        UpdateAmmoUI();
    }
}