using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

[System.Serializable]
public class MeleeSwingData
{
    public string swingName = "Swing";
    public float damage = 40f;          
    public float swingSpeed = 0.1f;     
    public float swingCooldown = 0.3f;  
    
    [Header("Procedural Animation")]
    public Vector3 targetRotation; 
    public Vector3 positionalOffset;
    
    [Header("Camera Kick")]
    public float cameraKickDown = -3f; 
    public float cameraKickLeft = -1f;
}

public class SimpleMelee : MonoBehaviour
{
    [Header("Combo System")]
    [Tooltip("How many seconds without clicking before the combo resets to Swing 1?")]
    public float comboResetTime = 1.0f; 
    
    // --- REDUCED TO 2-HIT COMBO ---
    public MeleeSwingData[] comboSwings = new MeleeSwingData[]
    {
        // Swing 1: Slash Right to Left
        new MeleeSwingData { swingName = "Slash Right to Left", targetRotation = new Vector3(65, -85, -40), positionalOffset = new Vector3(-0.3f, -0.5f, 0.6f), cameraKickLeft = -1f },
        
        // Swing 2: Slash Left to Right
        new MeleeSwingData { swingName = "Slash Left to Right", targetRotation = new Vector3(65, 45, 40), positionalOffset = new Vector3(0.3f, -0.5f, 0.6f), cameraKickLeft = 1f }
    };

    private int currentComboStep = 0;
    private float lastSwingEndTime = 0f;
    
    // Tracks the running combo so we can safely stop it!
    private Coroutine activeSwingCoroutine; 

    [Header("Weapon Base Stats")]
    public float range = 2.5f;          
    public Vector3 restingRotation = new Vector3(15, -20, 10);       
    public bool useHitStop = true;

    [Header("Weapon Sway/Drawing")]
    public Vector3 drawOffset = new Vector3(0, -1f, 0); 
    public float drawTime = 0.5f;
    private Vector3 originalPosition;
    public bool isDrawing = false;
    private bool isSwinging = false;

    [Header("Audio & FX")]
    public AudioSource weaponAudio;
    public AudioClip swingSound; 
    public AudioClip hitSound;   
    public GameObject hitEffectPrefab; 

    [Header("Visuals")]
    public Image crosshairDisplay;   
    public Sprite weaponCrosshair;

    private Camera fpsCamera;
    private DoomMovement playerMovement;

    void Awake()
    {
        fpsCamera = GetComponentInParent<Camera>();
        playerMovement = GetComponentInParent<DoomMovement>();
        originalPosition = transform.localPosition;

        if (crosshairDisplay == null)
        {
            GameObject crosshairObj = GameObject.Find("Crosshair");
            if (crosshairObj != null) 
            {
                crosshairDisplay = crosshairObj.GetComponent<Image>();
            }
        }
    }
    
    void OnEnable()
    {
        UpdateCrosshairUI();
        StartCoroutine(DrawWeaponRoutine());
        currentComboStep = 0; 
    }

    void Update()
    {
        if (Mouse.current == null || isDrawing || isSwinging) return;
        if (playerMovement != null && playerMovement.isKnockedDown) return;

        if (Time.unscaledTime - lastSwingEndTime > comboResetTime && currentComboStep > 0)
        {
            currentComboStep = 0;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (comboSwings == null || comboSwings.Length == 0) return;
            
            if (activeSwingCoroutine != null) StopCoroutine(activeSwingCoroutine);
            activeSwingCoroutine = StartCoroutine(SwingRoutine(comboSwings[currentComboStep]));
        }
    }

    void UpdateCrosshairUI() 
    { 
        if (crosshairDisplay != null && weaponCrosshair != null) 
            crosshairDisplay.sprite = weaponCrosshair; 
    }

    IEnumerator SwingRoutine(MeleeSwingData currentSwing)
    {
        isSwinging = true;

        if (weaponAudio != null && swingSound != null) weaponAudio.PlayOneShot(swingSound);

        if (playerMovement != null) 
            playerMovement.AddRecoil(0f, currentSwing.cameraKickDown); 

        float elapsed = 0f;
        Quaternion startRot = Quaternion.Euler(restingRotation);
        Quaternion targetRot = Quaternion.Euler(currentSwing.targetRotation);
        Vector3 targetPos = originalPosition + currentSwing.positionalOffset;

        bool hitConnected = DetectHit(currentSwing.damage); 

        // Safety check in case the Unity Inspector wiped the speed values to 0
        if (currentSwing.swingSpeed > 0f) 
        {
            while (elapsed < currentSwing.swingSpeed)
            {
                elapsed += Time.unscaledDeltaTime; 
                float t = elapsed / currentSwing.swingSpeed;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); 

                transform.localRotation = Quaternion.Slerp(startRot, targetRot, easeT);
                transform.localPosition = Vector3.Lerp(originalPosition, targetPos, easeT);
                yield return null;
            }
        }

        elapsed = 0f;
        float recoveryTime = currentSwing.swingCooldown - currentSwing.swingSpeed; 
        if (recoveryTime <= 0) recoveryTime = 0.1f;

        while (elapsed < recoveryTime)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / recoveryTime;
            float easeT = t * t * (3f - 2f * t); 

            transform.localRotation = Quaternion.Slerp(targetRot, startRot, easeT);
            transform.localPosition = Vector3.Lerp(targetPos, originalPosition, easeT);
            yield return null;
        }

        transform.localRotation = startRot;
        transform.localPosition = originalPosition;
        
        isSwinging = false;
        lastSwingEndTime = Time.unscaledTime; 

        currentComboStep++;
        if (currentComboStep >= comboSwings.Length) 
        {
            currentComboStep = 0; 
        }
    }

    bool DetectHit(float currentSwingDamage)
    {
        RaycastHit hit;
        float hitRadius = 0.5f; 
        bool hitFlesh = false;

        if (Physics.SphereCast(fpsCamera.transform.position, hitRadius, fpsCamera.transform.forward, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            
            if (target != null)
            {
                target.TakeDamage(currentSwingDamage); 
                hitFlesh = true;
                
                if (weaponAudio != null && hitSound != null) weaponAudio.PlayOneShot(hitSound);
                if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                
                if (useHitStop) StartCoroutine(HitStopRoutine());
            }
        }
        return hitFlesh;
    }

    IEnumerator HitStopRoutine()
    {
        float originalScale = Time.timeScale; 
        Time.timeScale = 0.05f; 
        yield return new WaitForSecondsRealtime(0.04f); 
        Time.timeScale = originalScale; 
    }

    public IEnumerator DrawWeaponRoutine()
    {
        isDrawing = true;
        isSwinging = false;
        transform.localRotation = Quaternion.Euler(restingRotation);
        transform.localPosition = originalPosition + drawOffset;
        float elapsed = 0f;
        while (elapsed < drawTime)
        {
            elapsed += Time.unscaledDeltaTime; 
            float easeT = elapsed / drawTime;
            transform.localPosition = Vector3.Lerp(originalPosition + drawOffset, originalPosition, easeT);
            yield return null;
        }
        transform.localPosition = originalPosition;
        isDrawing = false;
    }

    public IEnumerator HolsterWeaponRoutine()
    {
        isDrawing = true;
        
        if (activeSwingCoroutine != null) StopCoroutine(activeSwingCoroutine);
        
        transform.localRotation = Quaternion.Euler(restingRotation);
        float elapsed = 0f;
        while (elapsed < drawTime)
        {
            elapsed += Time.unscaledDeltaTime; 
            transform.localPosition = Vector3.Lerp(originalPosition, originalPosition + drawOffset, elapsed / drawTime);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public void InstantHide()
    {
        StopAllCoroutines(); 
        isDrawing = false; 
        isSwinging = false;
        transform.localPosition = originalPosition + drawOffset; 
    }
}