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

    [Header("Knockback Physics")]
    public float knockbackDecay = 10f; // How fast the pushback stops
    private Vector3 knockbackVelocity = Vector3.zero;

    [Header("Looking")]
    public float mouseSensitivity = 0.1f; 
    private float xRotation = 0f;

    [Header("Sliding")]
    public float slideDuration = 0.8f;
    public float slideHeight = 1f; 
    private float originalHeight;
    private bool isSliding = false;
    private Vector3 slideDirection;

    [Header("Footstep Audio")]
    public AudioSource playerAudio;
    public AudioClip[] footstepSounds; 
    public float stepInterval = 0.4f;  
    private float stepTimer;

    [Header("Action Audio")]
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

        // --- 1. LOOKING AROUND ---
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);


        // --- 2. GRAVITY & GROUND CHECK ---
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }


        // --- 3. MOVEMENT INPUT ---
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;

        Vector3 inputDirection = transform.right * x + transform.forward * z;
        if (inputDirection.magnitude > 1f) inputDirection.Normalize();

        float currentSpeed = walkSpeed;

        bool isRunning = Keyboard.current.leftShiftKey.isPressed && z > 0;
        if (isRunning && !isSliding && !isDashing) currentSpeed = runSpeed;

        // --- SLIDING ---
        if (Keyboard.current.cKey.wasPressedThisFrame && isRunning && !isSliding && !isDashing && controller.isGrounded)
        {
            if (playerAudio != null && slideSound != null) playerAudio.PlayOneShot(slideSound);
            StartCoroutine(SlideRoutine(inputDirection));
        }

        // --- DASHING ---
        if (Mouse.current.rightButton.wasPressedThisFrame && !isDashing && !isSliding)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            bool hasEnergy = (stats != null && stats.hasUnlimitedEnergy);

            if (Time.time >= lastDashTime + dashCooldown || hasEnergy)
            {
                StartCoroutine(DashRoutine(inputDirection));
            }
        }

        // --- APPLY MOVEMENT ---
        Vector3 move;
        if (isDashing) move = dashDirection * dashSpeed;
        else if (isSliding) { currentSpeed = slideSpeed; move = slideDirection * currentSpeed; }
        else move = inputDirection * currentSpeed;

        // --- 4. JUMPING ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded && !isSliding && !isDashing)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
        }

        velocity.y += gravity * Time.deltaTime;

        // NEW: Add the knockback velocity to our movement!
        controller.Move((move + velocity + knockbackVelocity) * Time.deltaTime); 

        // NEW: Rapidly slow down the knockback force so we don't slide forever
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);

        // --- 5. FOOTSTEP AUDIO ---
        if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isSliding && !isDashing)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f) { PlayFootstepSound(); stepTimer = isRunning ? stepInterval * 0.7f : stepInterval; }
        }
        else stepTimer = 0f; 
    }

    IEnumerator SlideRoutine(Vector3 startDirection)
    {
        isSliding = true; slideDirection = startDirection; 
        controller.height = slideHeight;
        Vector3 originalCamPos = playerCamera.localPosition;
        playerCamera.localPosition = new Vector3(originalCamPos.x, originalCamPos.y - (originalHeight - slideHeight) / 2f, originalCamPos.z);
        yield return new WaitForSeconds(slideDuration);
        controller.height = originalHeight; playerCamera.localPosition = originalCamPos;
        isSliding = false;
    }

    IEnumerator DashRoutine(Vector3 currentInputDirection)
    {
        isDashing = true; lastDashTime = Time.time; 
        if (playerAudio != null && dashSound != null) playerAudio.PlayOneShot(dashSound);
        if (currentInputDirection.magnitude == 0) dashDirection = transform.forward;
        else dashDirection = currentInputDirection;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    void PlayFootstepSound()
    {
        if (footstepSounds != null && footstepSounds.Length > 0 && playerAudio != null)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            playerAudio.PlayOneShot(footstepSounds[randomIndex]);
        }
    }

    // ==========================================
    // CALLED BY THE WEAPON SCRIPT WHEN SHOOTING!
    // ==========================================
    public void AddRecoil(float pushBackForce, float cameraKickUp)
    {
        // 1. Shove the player physically backwards (opposite of where the camera is looking)
        knockbackVelocity -= playerCamera.forward * pushBackForce;
        
        // 2. Jerk the camera upwards
        xRotation -= cameraKickUp;
        
        // Make sure the camera doesn't snap past looking straight up
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
    }
}