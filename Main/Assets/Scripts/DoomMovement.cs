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
    public bool isKnockedDown = false; // Prevents moving/shooting while on the floor!

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

        // Gravity always applies so you drop if punched off a ledge
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f; 
        velocity.y += gravity * Time.deltaTime;

        Vector3 move = Vector3.zero;

        // --- ONLY ALLOW CONTROL IF NOT ON THE FLOOR! ---
        if (!isKnockedDown)
        {
            // LOOKING
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            xRotation -= mouseDelta.y * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * (mouseDelta.x * mouseSensitivity));

            // MOVEMENT
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

            // SLIDE & DASH
            if (Keyboard.current.cKey.wasPressedThisFrame && isRunning && !isSliding && !isDashing && controller.isGrounded)
            {
                if (playerAudio != null && slideSound != null) playerAudio.PlayOneShot(slideSound);
                StartCoroutine(SlideRoutine(inputDirection));
            }

            if (Mouse.current.rightButton.wasPressedThisFrame && !isDashing && !isSliding)
            {
                PlayerStats stats = GetComponent<PlayerStats>();
                if (Time.time >= lastDashTime + dashCooldown || (stats != null && stats.hasUnlimitedEnergy))
                    StartCoroutine(DashRoutine(inputDirection));
            }

            // JUMP
            if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded && !isSliding && !isDashing)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
            }

            // ASSIGN MOVEMENT
            if (isDashing) move = dashDirection * dashSpeed;
            else if (isSliding) move = slideDirection * slideSpeed;
            else move = inputDirection * currentSpeed;

            // FOOTSTEPS
            if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isSliding && !isDashing)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f) { PlayFootstepSound(); stepTimer = isRunning ? stepInterval * 0.7f : stepInterval; }
            }
            else stepTimer = 0f; 
        }

        // Apply movement + physics (This runs even while knocked down so you slide across the floor!)
        controller.Move((move + velocity + knockbackVelocity) * Time.deltaTime); 
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    // --- NEW: PUNCH PHYSICS ---
    public void ApplyPunchKnockback(Vector3 direction, float force)
    {
        if (!isKnockedDown)
        {
            knockbackVelocity += direction * force;
            xRotation -= 8f; // Jerk the camera up so you feel the hit!
        }
    }

    public void TriggerKnockdown()
    {
        if (!isKnockedDown) StartCoroutine(KnockdownRoutine());
    }

    IEnumerator KnockdownRoutine()
    {
        isKnockedDown = true;
        
        // Massive shove backward when you fall
        knockbackVelocity = -transform.forward * 20f; 

        // Animate the camera crashing to the floor
        float fallDuration = 0.3f;
        float elapsed = 0f;
        Vector3 normalCamPos = playerCamera.localPosition;
        Vector3 fallenCamPos = new Vector3(normalCamPos.x, -0.6f, normalCamPos.z); // Drop head
        float startXRot = xRotation;

        while(elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            playerCamera.localPosition = Vector3.Lerp(normalCamPos, fallenCamPos, elapsed / fallDuration);
            xRotation = Mathf.Lerp(startXRot, 70f, elapsed / fallDuration); // Force look down
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 45f); // Tilt head sideways (Dutch angle!)
            yield return null;
        }

        // Lay on the ground helpless!
        yield return new WaitForSeconds(2f);

        // Stand back up
        elapsed = 0f;
        float riseDuration = 0.6f;
        while(elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            playerCamera.localPosition = Vector3.Lerp(fallenCamPos, normalCamPos, elapsed / riseDuration);
            xRotation = Mathf.Lerp(70f, 0f, elapsed / riseDuration);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, Mathf.Lerp(45f, 0f, elapsed / riseDuration));
            yield return null;
        }

        playerCamera.localPosition = normalCamPos;
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        isKnockedDown = false; // Restore control!
    }

    public void AddRecoil(float pushBackForce, float cameraKickUp)
    {
        if (isKnockedDown) return;
        knockbackVelocity -= playerCamera.forward * pushBackForce;
        xRotation -= cameraKickUp;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
    }

    IEnumerator SlideRoutine(Vector3 startDirection) { /* Untouched */ isSliding = true; slideDirection = startDirection; controller.height = slideHeight; Vector3 originalCamPos = playerCamera.localPosition; playerCamera.localPosition = new Vector3(originalCamPos.x, originalCamPos.y - (originalHeight - slideHeight) / 2f, originalCamPos.z); yield return new WaitForSeconds(slideDuration); controller.height = originalHeight; playerCamera.localPosition = originalCamPos; isSliding = false; }
    IEnumerator DashRoutine(Vector3 currentInputDirection) { /* Untouched */ isDashing = true; lastDashTime = Time.time; if (playerAudio != null && dashSound != null) playerAudio.PlayOneShot(dashSound); if (currentInputDirection.magnitude == 0) dashDirection = transform.forward; else dashDirection = currentInputDirection; yield return new WaitForSeconds(dashDuration); isDashing = false; }
    void PlayFootstepSound() { /* Untouched */ if (footstepSounds != null && footstepSounds.Length > 0 && playerAudio != null) { int randomIndex = Random.Range(0, footstepSounds.Length); playerAudio.PlayOneShot(footstepSounds[randomIndex]); } }
}