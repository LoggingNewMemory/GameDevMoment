using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI; // <-- NEW: Required for the Crosshair!

public class SimpleMelee : MonoBehaviour
{
    [Header("Melee Stats")]
    public float damage = 40f;          
    public float range = 2.5f;          
    public float swingSpeed = 0.15f;    
    public float swingCooldown = 0.6f;  

    [Header("Procedural Animation (Angles)")]
    public Vector3 restingRotation = new Vector3(0, 0, 0);       
    public Vector3 swingTargetRotation = new Vector3(45, -60, -20); 

    [Header("Weapon Sway/Drawing (Matches Guns)")]
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

    // --- NEW: CROSSHAIR VISUALS ---
    [Header("Visuals")]
    public Image crosshairDisplay;   
    public Sprite weaponCrosshair;
    // ------------------------------

    private Camera fpsCamera;
    private DoomMovement playerMovement;

    void Awake()
    {
        fpsCamera = GetComponentInParent<Camera>();
        playerMovement = GetComponentInParent<DoomMovement>();
        originalPosition = transform.localPosition;
    }

    void OnEnable()
    {
        UpdateCrosshairUI(); // <-- NEW: Update the crosshair when drawing the sword!
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

    // --- NEW: CROSSHAIR FUNCTION ---
    void UpdateCrosshairUI() 
    { 
        if (crosshairDisplay != null && weaponCrosshair != null) 
        {
            crosshairDisplay.sprite = weaponCrosshair; 
        }
    }

    IEnumerator SwingRoutine()
    {
        isSwinging = true;

        if (weaponAudio != null && swingSound != null) weaponAudio.PlayOneShot(swingSound);

        float elapsed = 0f;
        Quaternion startRot = Quaternion.Euler(restingRotation);
        Quaternion targetRot = Quaternion.Euler(swingTargetRotation);

        DetectHit();

        while (elapsed < swingSpeed)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / swingSpeed);
            yield return null;
        }

        elapsed = 0f;
        float recoveryTime = swingCooldown - swingSpeed; 
        if (recoveryTime <= 0) recoveryTime = 0.1f;

        while (elapsed < recoveryTime)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(targetRot, startRot, elapsed / recoveryTime);
            yield return null;
        }

        transform.localRotation = startRot;
        isSwinging = false;
    }

    void DetectHit()
    {
        RaycastHit hit;
        float hitRadius = 0.5f; 

        if (Physics.SphereCast(fpsCamera.transform.position, hitRadius, fpsCamera.transform.forward, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            
            if (target != null)
            {
                target.TakeDamage(damage);
                
                if (weaponAudio != null && hitSound != null) weaponAudio.PlayOneShot(hitSound);
                if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
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
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(originalPosition + drawOffset, originalPosition, elapsed / drawTime);
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
            elapsed += Time.deltaTime;
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