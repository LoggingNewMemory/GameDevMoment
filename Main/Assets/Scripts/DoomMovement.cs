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

    [Header("Jumping & Gravity")]
    public float jumpHeight = 2f;
    public float gravity = -20f; 
    private Vector3 velocity; 

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
        if (isRunning && !isSliding) currentSpeed = runSpeed;

        // SLIDING
        if (Keyboard.current.cKey.wasPressedThisFrame && isRunning && !isSliding && controller.isGrounded)
        {
            if (playerAudio != null && slideSound != null)
            {
                playerAudio.PlayOneShot(slideSound);
            }
            StartCoroutine(SlideRoutine(inputDirection));
        }

        Vector3 move;
        if (isSliding)
        {
            currentSpeed = slideSpeed;
            move = slideDirection * currentSpeed; 
        }
        else
        {
            move = inputDirection * currentSpeed;
        }

        // --- 4. JUMPING ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded && !isSliding)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            if (playerAudio != null && jumpSound != null)
            {
                playerAudio.PlayOneShot(jumpSound);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move((move + velocity) * Time.deltaTime); 

        // --- 5. FOOTSTEP AUDIO ---
        if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isSliding)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstepSound();
                stepTimer = isRunning ? stepInterval * 0.7f : stepInterval; 
            }
        }
        else
        {
            stepTimer = 0f; 
        }
    }

    IEnumerator SlideRoutine(Vector3 startDirection)
    {
        isSliding = true;
        slideDirection = startDirection; 

        controller.height = slideHeight;
        
        Vector3 originalCamPos = playerCamera.localPosition;
        playerCamera.localPosition = new Vector3(originalCamPos.x, originalCamPos.y - (originalHeight - slideHeight) / 2f, originalCamPos.z);

        yield return new WaitForSeconds(slideDuration);

        controller.height = originalHeight;
        playerCamera.localPosition = originalCamPos;
        isSliding = false;
    }

    void PlayFootstepSound()
    {
        if (footstepSounds != null && footstepSounds.Length > 0 && playerAudio != null)
        {
            int randomIndex = Random.Range(0, footstepSounds.Length);
            playerAudio.PlayOneShot(footstepSounds[randomIndex]);
        }
    }
}