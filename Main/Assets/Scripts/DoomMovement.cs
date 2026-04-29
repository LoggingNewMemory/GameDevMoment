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

    [Header("Wall Jumping")]
    public float wallJumpUpForce = 8f;     
    public float wallJumpSideForce = 15f;  
    [Tooltip("Increased distance makes it easier to register a wall near you!")]
    public float wallCheckDistance = 1.5f;   
    private bool wallLeft = false;
    private bool wallRight = false;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    [Header("Knockback & Falling")]
    public float knockbackDecay = 10f; 
    private Vector3 knockbackVelocity = Vector3.zero;
    public bool isKnockedDown = false; 
    public bool isDead = false; 
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

    // --- NEW: HEAD BOBBING MERGED IN! ---
    [Header("Head Bobbing")]
    public float walkBobSpeed = 14f;
    public float runBobSpeed = 18f;
    public float bobAmountX = 0.04f;
    public float bobAmountY = 0.04f;
    private float bobTimer = 0f;
    private Vector3 defaultCameraPos;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        originalHeight = controller.height; 
        
        // Memorize the exact resting spot of the camera!
        defaultCameraPos = playerCamera.localPosition;
    }

    void Update()
    {
        if (isDead || Mouse.current == null || Keyboard.current == null) return;

        CheckForWalls();

        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f; 

        velocity.y += gravity * Time.unscaledDeltaTime;

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

            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentDizzyRoll);
            transform.Rotate(Vector3.up * (mouseDelta.x * mouseSensitivity));

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

            currentSpeed *= speedMultiplier;

            if (stumbleTimer > 0f)
            {
                stumbleTimer -= Time.unscaledDeltaTime;
                currentSpeed *= 0.1f; 
            }

            // --- THE MERGED BOB LOGIC! ---
            // Only bob the head if we are safely on the ground and not dodging or sliding!
            if (controller.isGrounded && !isSliding && !isDashing)
            {
                if (inputDirection.magnitude > 0.1f)
                {
                    float currentBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
                    bobTimer += Time.unscaledDeltaTime * currentBobSpeed;

                    float bobX = Mathf.Cos(bobTimer / 2f) * bobAmountX; 
                    float bobY = Mathf.Sin(bobTimer) * bobAmountY;

                    Vector3 targetPos = defaultCameraPos + new Vector3(bobX, bobY, 0f);
                    playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetPos, 10f * Time.unscaledDeltaTime);
                }
                else
                {
                    // Smoothly recenter when we stop!
                    bobTimer = 0f;
                    playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, defaultCameraPos, 10f * Time.unscaledDeltaTime);
                }
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

            if (Keyboard.current.spaceKey.wasPressedThisFrame && stumbleTimer <= 0f)
            {
                if (controller.isGrounded && !isSliding && !isDashing)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
                }
                else if (!controller.isGrounded && (wallLeft || wallRight))
                {
                    WallBounceJump();
                }
            }

            if (isDashing) move = dashDirection * dashSpeed * speedMultiplier;
            else if (isSliding) move = slideDirection * slideSpeed * speedMultiplier;
            else move = inputDirection * currentSpeed;

            if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isSliding && !isDashing)
            {
                stepTimer -= Time.unscaledDeltaTime;
                if (stepTimer <= 0f) { PlayFootstepSound(); stepTimer = isRunning ? stepInterval * 0.7f : stepInterval; }
            }
            else stepTimer = 0f; 
        }

        controller.Move((move + velocity + knockbackVelocity) * Time.unscaledDeltaTime); 
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.unscaledDeltaTime);
    }

    private void CheckForWalls()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance);
    }

    private void WallBounceJump()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        velocity.y = wallJumpUpForce;
        knockbackVelocity = wallNormal * wallJumpSideForce; 
        if (playerAudio != null && jumpSound != null) playerAudio.PlayOneShot(jumpSound);
    }

    public void ApplyPunchKnockback(Vector3 direction, float force)
    {
        if (!isKnockedDown && !isDead)
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
            if (isKnockedDown || isDead) yield break; 
            float step = (kickAmount / duration) * Time.unscaledDeltaTime;
            xRotation -= step;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void TriggerKnockdown() { if (!isKnockedDown && !isDead) StartCoroutine(KnockdownRoutine()); }

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
        
        // Use our default camera pos so it doesn't get messed up!
        Vector3 normalCamPos = defaultCameraPos;
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

        if (isDead) yield break; 

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

    public void TriggerDeath()
    {
        if (!isDead) StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;
        isKnockedDown = true; 

        SimpleShoot activeGun = GetComponentInChildren<SimpleShoot>();
        SimpleMelee activeMelee = GetComponentInChildren<SimpleMelee>();

        if (activeGun != null) activeGun.InstantHide();
        if (activeMelee != null) activeMelee.InstantHide();

        float fallDuration = 0.5f; 
        float elapsed = 0f;
        Vector3 normalCamPos = defaultCameraPos;
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

        PauseMenuManager pauseManager = FindObjectOfType<PauseMenuManager>();
        if (pauseManager != null)
        {
            pauseManager.TriggerGameOver();
        }
    }

    public void AddRecoil(float pushBackForce, float cameraKickUp) { if (isKnockedDown || isDead) return; knockbackVelocity -= playerCamera.forward * pushBackForce; xRotation -= cameraKickUp; xRotation = Mathf.Clamp(xRotation, -90f, 90f); }
    
    IEnumerator SlideRoutine(Vector3 startDirection) 
    { 
        isSliding = true; 
        slideDirection = startDirection; 
        controller.height = slideHeight; 
        playerCamera.localPosition = new Vector3(defaultCameraPos.x, defaultCameraPos.y - (originalHeight - slideHeight) / 2f, defaultCameraPos.z); 
        yield return new WaitForSecondsRealtime(slideDuration); 
        controller.height = originalHeight; 
        playerCamera.localPosition = defaultCameraPos; 
        isSliding = false; 
    }
    
    IEnumerator DashRoutine(Vector3 currentInputDirection) { isDashing = true; lastDashTime = Time.unscaledTime; if (playerAudio != null && dashSound != null) playerAudio.PlayOneShot(dashSound); if (currentInputDirection.magnitude == 0) dashDirection = transform.forward; else dashDirection = currentInputDirection; yield return new WaitForSecondsRealtime(dashDuration); isDashing = false; }
    void PlayFootstepSound() { if (footstepSounds != null && footstepSounds.Length > 0 && playerAudio != null) { int randomIndex = Random.Range(0, footstepSounds.Length); playerAudio.PlayOneShot(footstepSounds[randomIndex]); } }
}