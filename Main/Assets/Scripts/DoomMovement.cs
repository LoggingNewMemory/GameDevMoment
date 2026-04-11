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
    public float speedMultiplier = 1f; 

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

    [Header("Wall Running (Apex / Titanfall)")]
    public float maxWallRunTime = 3f;
    public float wallRunSpeed = 20f;
    public float wallJumpUpForce = 8f;     // Upward boost off the wall
    public float wallJumpSideForce = 15f;  // Push away from the wall
    public float wallCheckDistance = 1f;   // How far to look for a wall
    public float wallRunCameraTilt = 15f;  // AAA camera juice!
    private bool isWallRunning = false;
    private float wallRunTimer = 0f;
    private float currentWallTilt = 0f;
    private bool wallLeft = false;
    private bool wallRight = false;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    [Header("Knockback & Falling")]
    public float knockbackDecay = 10f; 
    private Vector3 knockbackVelocity = Vector3.zero;
    public bool isKnockedDown = false; 
    private float stumbleTimer = 0f; 
    private float currentDizzyRoll = 0f; 

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

        CheckForWalls();

        // Use UNSCALED time so player ignores Sandevistan
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f; 

        // --- NEW: Gravity is paused while Wall Running! ---
        if (!isWallRunning) 
        {
            velocity.y += gravity * Time.unscaledDeltaTime;
        }

        Vector3 move = Vector3.zero;

        if (!isKnockedDown)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            xRotation -= mouseDelta.y * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); 

            PlayerStats stats = GetComponent<PlayerStats>();
            
            if (stats != null && stats.dizzyStacks > 0)
            {
                currentDizzyRoll = Mathf.Sin(Time.unscaledTime * (2f + stats.dizzyStacks)) * (2f * stats.dizzyStacks);

                if (Random.Range(0f, 100f) < (0.5f * stats.dizzyStacks)) 
                {
                    stumbleTimer = 0.4f; 
                }
            }
            else
            {
                currentDizzyRoll = Mathf.Lerp(currentDizzyRoll, 0f, Time.unscaledDeltaTime * 5f);
            }

            // --- NEW: Calculate Wall Run Camera Tilt ---
            float targetTilt = 0f;
            if (isWallRunning) targetTilt = wallLeft ? -wallRunCameraTilt : wallRunCameraTilt;
            currentWallTilt = Mathf.Lerp(currentWallTilt, targetTilt, Time.unscaledDeltaTime * 10f);

            // Combine Dizzy Roll and Wall Run Tilt
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentDizzyRoll + currentWallTilt);
            transform.Rotate(Vector3.up * (mouseDelta.x * mouseSensitivity));

            float x = 0f; float z = 0f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;

            Vector3 inputDirection = transform.right * x + transform.forward * z;
            if (inputDirection.magnitude > 1f) inputDirection.Normalize();

            // --- NEW: WALL RUN STATE MACHINE ---
            bool isMovingForward = z > 0;
            if ((wallLeft || wallRight) && isMovingForward && !controller.isGrounded && !isSliding)
            {
                if (!isWallRunning) StartWallRun();
            }
            else
            {
                if (isWallRunning) StopWallRun();
            }

            // Update the 3-second wall run timer
            if (isWallRunning)
            {
                wallRunTimer -= Time.unscaledDeltaTime;
                if (wallRunTimer <= 0f) StopWallRun();
            }

            // Calculate Base Speed
            float currentSpeed = walkSpeed;
            bool isRunning = Keyboard.current.leftShiftKey.isPressed && z > 0;
            
            if (isWallRunning) currentSpeed = wallRunSpeed;
            else if (isRunning && !isSliding && !isDashing) currentSpeed = runSpeed;

            // Apply Rage of CS Boost
            currentSpeed *= speedMultiplier;

            if (stumbleTimer > 0f)
            {
                stumbleTimer -= Time.unscaledDeltaTime;
                currentSpeed *= 0.1f; 
            }

            if (Keyboard.current.cKey.wasPressedThisFrame && isRunning && !isSliding && !isDashing && controller.isGrounded)
            {
                if (playerAudio != null && slideSound != null) playerAudio.PlayOneShot(slideSound);
                StartCoroutine(SlideRoutine(inputDirection));
            }

            if (Mouse.current.rightButton.wasPressedThisFrame && !isDashing && !isSliding)
            {
                if (Time.unscaledTime >= lastDashTime + dashCooldown || (stats != null && stats.hasUnlimitedEnergy))
                    StartCoroutine(DashRoutine(inputDirection));
            }

            // --- JUMP LOGIC (Standard & Wall Bounce) ---
            if (Keyboard.current.spaceKey.wasPressedThisFrame && stumbleTimer <= 0f)
            {
                if (controller.isGrounded && !isSliding && !isDashing)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
                }
                else if (isWallRunning)
                {
                    WallBounceJump();
                }
            }

            // Apply Movement
            if (isDashing) move = dashDirection * dashSpeed * speedMultiplier;
            else if (isSliding) move = slideDirection * slideSpeed * speedMultiplier;
            else move = inputDirection * currentSpeed;

            if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isSliding && !isDashing && !isWallRunning)
            {
                stepTimer -= Time.unscaledDeltaTime;
                if (stepTimer <= 0f) { PlayFootstepSound(); stepTimer = isRunning ? stepInterval * 0.7f : stepInterval; }
            }
            else stepTimer = 0f; 
        }

        controller.Move((move + velocity + knockbackVelocity) * Time.unscaledDeltaTime); 
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.unscaledDeltaTime);
    }

    // ==========================================
    // WALL RUN LOGIC
    // ==========================================
    private void CheckForWalls()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance);
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = maxWallRunTime; // Set the 3-second limit
        velocity.y = 0f;               // Stop falling!
    }

    private void StopWallRun()
    {
        isWallRunning = false;
    }

    private void WallBounceJump()
    {
        // 1. Get the normal (angle) of the wall we are touching
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        // 2. Add an upward boost
        velocity.y = wallJumpUpForce;

        // 3. Use our existing knockback system to violently shove the player away from the wall!
        knockbackVelocity += wallNormal * wallJumpSideForce; 

        if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
        StopWallRun();
    }

    // ==========================================
    // EXISTING MECHANICS
    // ==========================================
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
            float step = (kickAmount / duration) * Time.unscaledDeltaTime;
            xRotation -= step;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void TriggerKnockdown() { if (!isKnockedDown) StartCoroutine(KnockdownRoutine()); }

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
            elapsed += Time.unscaledDeltaTime;
            playerCamera.localPosition = Vector3.Lerp(normalCamPos, fallenCamPos, elapsed / fallDuration);
            xRotation = Mathf.Lerp(startXRot, 75f, elapsed / fallDuration); 
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, Mathf.Lerp(0f, 65f, elapsed / fallDuration)); 
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.5f); 

        elapsed = 0f;
        float riseDuration = 0.5f; 
        while(elapsed < riseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
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
    IEnumerator SlideRoutine(Vector3 startDirection) { isSliding = true; slideDirection = startDirection; controller.height = slideHeight; Vector3 originalCamPos = playerCamera.localPosition; playerCamera.localPosition = new Vector3(originalCamPos.x, originalCamPos.y - (originalHeight - slideHeight) / 2f, originalCamPos.z); yield return new WaitForSecondsRealtime(slideDuration); controller.height = originalHeight; playerCamera.localPosition = originalCamPos; isSliding = false; }
    IEnumerator DashRoutine(Vector3 currentInputDirection) { isDashing = true; lastDashTime = Time.unscaledTime; if (playerAudio != null && dashSound != null) playerAudio.PlayOneShot(dashSound); if (currentInputDirection.magnitude == 0) dashDirection = transform.forward; else dashDirection = currentInputDirection; yield return new WaitForSecondsRealtime(dashDuration); isDashing = false; }
    void PlayFootstepSound() { if (footstepSounds != null && footstepSounds.Length > 0 && playerAudio != null) { int randomIndex = Random.Range(0, footstepSounds.Length); playerAudio.PlayOneShot(footstepSounds[randomIndex]); } }
}