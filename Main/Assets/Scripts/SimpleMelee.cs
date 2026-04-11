using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class SimpleMelee : MonoBehaviour
{
    [Header("Melee Stats")]
    public float damage = 40f;          
    public float range = 2.5f;          
    public float swingSpeed = 0.1f;     // Ultra fast snap!
    public float swingCooldown = 0.4f;  

    [Header("Procedural Animation (Angles & Position)")]
    public Vector3 restingRotation = new Vector3(15, -20, 10);       
    public Vector3 swingTargetRotation = new Vector3(65, -85, -40); 
    
    [Tooltip("How far the sword lunges forward and down during the swing")]
    public Vector3 swingPositionalOffset = new Vector3(-0.3f, -0.5f, 0.6f);

    [Header("Camera & Juice (The AAA Feel)")]
    [Tooltip("Violently yanks the camera down/left to match the swing momentum")]
    public float cameraSwingKickDown = -3f; 
    public float cameraSwingKickLeft = -1f;
    [Tooltip("Freezes the game for a microsecond on flesh impacts!")]
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

        // --- NEW: AUTO-ASSIGN CROSSHAIR ---
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
    }

    void Update()
    {
        if (Mouse.current == null || isDrawing || isSwinging) return;
        if (playerMovement != null && playerMovement.isKnockedDown) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(SwingRoutine());
        }
    }

    void UpdateCrosshairUI() 
    { 
        if (crosshairDisplay != null && weaponCrosshair != null) 
            crosshairDisplay.sprite = weaponCrosshair; 
    }

    IEnumerator SwingRoutine()
    {
        isSwinging = true;

        if (weaponAudio != null && swingSound != null) weaponAudio.PlayOneShot(swingSound);

        if (playerMovement != null) 
            playerMovement.AddRecoil(0f, cameraSwingKickDown);

        float elapsed = 0f;
        Quaternion startRot = Quaternion.Euler(restingRotation);
        Quaternion targetRot = Quaternion.Euler(swingTargetRotation);
        Vector3 targetPos = originalPosition + swingPositionalOffset;

        bool hitConnected = DetectHit();

        while (elapsed < swingSpeed)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / swingSpeed;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            transform.localRotation = Quaternion.Slerp(startRot, targetRot, easeT);
            transform.localPosition = Vector3.Lerp(originalPosition, targetPos, easeT);
            yield return null;
        }

        elapsed = 0f;
        float recoveryTime = swingCooldown - swingSpeed; 
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
    }

    bool DetectHit()
    {
        RaycastHit hit;
        float hitRadius = 0.5f; 
        bool hitFlesh = false;

        if (Physics.SphereCast(fpsCamera.transform.position, hitRadius, fpsCamera.transform.forward, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            
            if (target != null)
            {
                target.TakeDamage(damage);
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
        StopCoroutine("SwingRoutine");
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