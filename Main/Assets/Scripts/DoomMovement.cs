using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; 

public class DoomMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform playerCamera;
    
    [Header("Movement Speeds")]
    public float walkSpeed = 10f; 
    public float runSpeed = 18f; 
    public float slideSpeed = 25f; 

    [Header("Dashing")]
    public float dashSpeed = 40f;      
    public float dashDuration = 0.2f;  
    public float dashCooldown = 1f;    
    private bool isDashing = false;
    private float lastDashTime = -100f; 
    private Vector3 dashDirection;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 2f;
    public float gravity = -20f; 
    private Vector3 velocity; 

    [Header("Knockback & Falling")]
    public float knockbackDecay = 10f; 
    private Vector3 knockbackVelocity = Vector3.zero;
    public bool isKnockedDown = false; 
    private float stumbleTimer = 0f; 

    [Header("Looking")]
    public float mouseSensitivity = 0.1f; 
    private float xRotation = 0f;

    [Header("Sliding")]
    public float slideDuration = 0.8f;
    public float slideHeight = 1f; 
    private float originalHeight;
    private bool isSliding = false;
    private Vector3 slideDirection;

    [Header("Audio")]
    public AudioSource playerAudio;
    public AudioClip[] footstepSounds; 
    public float stepInterval = 0.4f;  
    private float stepTimer;
    public AudioClip jumpSound;  
    public AudioClip slideSound; 
    public AudioClip dashSound; 

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        originalHeight = controller.height; 
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f; 
        velocity.y += gravity * Time.deltaTime;

        Vector3 move = Vector3.zero;

        if (!isKnockedDown)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            xRotation -= mouseDelta.y * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); 

            // --- DIZZY CAMERA SWAY & RANDOM STUMBLING ---
            float dizzyRoll = 0f;
            PlayerStats stats = GetComponent<PlayerStats>();
            
            if (stats != null && stats.dizzyStacks > 0)
            {
                // Roll the camera based on how many stacks you have
                dizzyRoll = Mathf.Sin(Time.time * (2f + stats.dizzyStacks)) * (2f * stats.dizzyStacks);

                // Randomly trigger the stumble effect so the player trips!
                if (Random.Range(0f, 100f) < (0.5f * stats.dizzyStacks)) 
                {
                    stumbleTimer = 0.4f; 
                }
            }

            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, dizzyRoll);
            transform.Rotate(Vector3.up * (mouseDelta.x * mouseSensitivity));
            // ----------------------------------------------

            float x = 0f; float z = 0f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;

            Vector3 inputDirection = transform.right * x + transform.forward * z;
            if (inputDirection.magnitude > 1f) inputDirection.Normalize();

            float currentSpeed = walkSpeed;
            bool isRunning = Keyboard.current.leftShiftKey.isPressed && z > 0;
            if (isRunning && !isSliding && !isDashing) currentSpeed = runSpeed;

            // Apply Stumble slow-down
            if (stumbleTimer > 0f)
            {
                stumbleTimer -= Time.deltaTime;
                currentSpeed *= 0.1f; 
            }

            if (Keyboard.current.cKey.wasPressedThisFrame && isRunning && !isSliding && !isDashing && controller.isGrounded)
            {
                if (playerAudio != null && slideSound != null) playerAudio.PlayOneShot(slideSound);
                StartCoroutine(SlideRoutine(inputDirection));
            }

            if (Mouse.current.rightButton.wasPressedThisFrame && !isDashing && !isSliding)
            {
                if (Time.time >= lastDashTime + dashCooldown || (stats != null && stats.hasUnlimitedEnergy))
                    StartCoroutine(DashRoutine(inputDirection));
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded && !isSliding && !isDashing && stumbleTimer <= 0f)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
            }

            if (isDashing) move = dashDirection * dashSpeed;
            else if (isSliding) move = slideDirection * slideSpeed;
            else move = inputDirection * currentSpeed;

            if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isSliding && !isDashing)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { PlayFootstepSound(); stepTimer = isRunning ? stepInterval * 0.7f : stepInterval; }
            }
            else stepTimer = 0f; 
        }

        controller.Move((move + velocity + knockbackVelocity) * Time.deltaTime); 
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    public void ApplyPunchKnockback(Vector3 direction, float force)
    {
        if (!isKnockedDown)
        {
            knockbackVelocity += direction * force;
            stumbleTimer = 0.35f; 
            StartCoroutine(SmoothCameraPunchRoutine(12f, 0.08f)); 
        }
    }

    IEnumerator SmoothCameraPunchRoutine(float kickAmount, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isKnockedDown) yield break; 
            
            float step = (kickAmount / duration) * Time.deltaTime;
            xRotation -= step;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void TriggerKnockdown()
    {
        if (!isKnockedDown) StartCoroutine(KnockdownRoutine());
    }

    IEnumerator KnockdownRoutine()
    {
        isKnockedDown = true;
        
        SimpleShoot activeGun = GetComponentInChildren<SimpleShoot>();
        SimpleMelee activeMelee = GetComponentInChildren<SimpleMelee>();

        if (activeGun != null) activeGun.InstantHide();
        if (activeMelee != null) activeMelee.InstantHide();

        knockbackVelocity = -transform.forward * 25f; 

        float fallDuration = 0.15f; 
        float elapsed = 0f;
        Vector3 normalCamPos = playerCamera.localPosition;
        Vector3 fallenCamPos = new Vector3(normalCamPos.x, -0.8f, normalCamPos.z); 
        float startXRot = xRotation;

        while(elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            playerCamera.localPosition = Vector3.Lerp(normalCamPos, fallenCamPos, elapsed / fallDuration);
            xRotation = Mathf.Lerp(startXRot, 75f, elapsed / fallDuration); 
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, Mathf.Lerp(0f, 65f, elapsed / fallDuration)); 
            yield return null;
        }

        yield return new WaitForSeconds(1.5f); 

        elapsed = 0f;
        float riseDuration = 0.5f; 
        while(elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            playerCamera.localPosition = Vector3.Lerp(fallenCamPos, normalCamPos, elapsed / riseDuration);
            xRotation = Mathf.Lerp(75f, 0f, elapsed / riseDuration);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, Mathf.Lerp(65f, 0f, elapsed / riseDuration));
            yield return null;
        }

        playerCamera.localPosition = normalCamPos;
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        isKnockedDown = false; 

        if (activeGun != null) StartCoroutine(activeGun.DrawWeaponRoutine());
        if (activeMelee != null) StartCoroutine(activeMelee.DrawWeaponRoutine());
    }

    public void AddRecoil(float pushBackForce, float cameraKickUp) { if (isKnockedDown) return; knockbackVelocity -= playerCamera.forward * pushBackForce; xRotation -= cameraKickUp; xRotation = Mathf.Clamp(xRotation, -90f, 90f); }
    IEnumerator SlideRoutine(Vector3 startDirection) { isSliding = true; slideDirection = startDirection; controller.height = slideHeight; Vector3 originalCamPos = playerCamera.localPosition; playerCamera.localPosition = new Vector3(originalCamPos.x, originalCamPos.y - (originalHeight - slideHeight) / 2f, originalCamPos.z); yield return new WaitForSeconds(slideDuration); controller.height = originalHeight; playerCamera.localPosition = originalCamPos; isSliding = false; }
    IEnumerator DashRoutine(Vector3 currentInputDirection) { isDashing = true; lastDashTime = Time.time; if (playerAudio != null && dashSound != null) playerAudio.PlayOneShot(dashSound); if (currentInputDirection.magnitude == 0) dashDirection = transform.forward; else dashDirection = currentInputDirection; yield return new WaitForSeconds(dashDuration); isDashing = false; }
    void PlayFootstepSound() { if (footstepSounds != null && footstepSounds.Length > 0 && playerAudio != null) { int randomIndex = Random.Range(0, footstepSounds.Length); playerAudio.PlayOneShot(footstepSounds[randomIndex]); } }
}