using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Position Bobbing (Walking/Running)")]
    public float walkBobSpeed = 12f;
    public float runBobSpeed = 18f;
    public float bobAmountX = 0.05f; 
    public float bobAmountY = 0.05f; 

    [Header("Rotation Sway (Mouse Look)")]
    public float swayMultiplier = 3f;
    public float maxSway = 4f;       

    [Header("Shooting / Steadying")]
    // Drops the sway down to 20% of its normal movement while firing
    public float steadyMultiplier = 0.2f; 
    // How fast the character braces and un-braces the weapon
    public float steadyTransitionSpeed = 15f; 
    
    [Header("Smoothing")]
    public float smoothStep = 10f;   

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timer = 0f;
    
    // Tracks the current multiplier (transitions between 1.0 and steadyMultiplier)
    private float currentSwayMultiplier = 1f;

    void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // ==========================================
        // 0. SHOOTING / BRACING CHECK
        // ==========================================
        // If the left mouse button is pressed, target the steady multiplier. Otherwise, target 1.0 (normal).
        bool isShooting = Mouse.current.leftButton.isPressed;
        float targetMultiplier = isShooting ? steadyMultiplier : 1f;
        
        // Smoothly glide the multiplier up or down
        currentSwayMultiplier = Mathf.Lerp(currentSwayMultiplier, targetMultiplier, Time.deltaTime * steadyTransitionSpeed);


        // ==========================================
        // 1. ROTATION SWAY (Mouse Look)
        // ==========================================
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        // Apply the currentSwayMultiplier to the mouse sway!
        float mouseX = -mouseDelta.x * swayMultiplier * currentSwayMultiplier * Time.deltaTime;
        float mouseY = -mouseDelta.y * swayMultiplier * currentSwayMultiplier * Time.deltaTime;

        mouseX = Mathf.Clamp(mouseX, -maxSway, maxSway);
        mouseY = Mathf.Clamp(mouseY, -maxSway, maxSway);

        Quaternion targetRotation = startRotation * Quaternion.Euler(mouseY, mouseX, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smoothStep * Time.deltaTime);


        // ==========================================
        // 2. POSITION BOBBING (Walking)
        // ==========================================
        float xInput = 0f;
        float zInput = 0f;

        if (Keyboard.current.wKey.isPressed) zInput += 1f;
        if (Keyboard.current.sKey.isPressed) zInput -= 1f;
        if (Keyboard.current.dKey.isPressed) xInput += 1f;
        if (Keyboard.current.aKey.isPressed) xInput -= 1f;

        Vector3 inputVector = new Vector3(xInput, 0f, zInput);

        if (inputVector.magnitude > 0.1f)
        {
            bool isRunning = Keyboard.current.leftShiftKey.isPressed && zInput > 0;
            float currentBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;

            timer += Time.deltaTime * currentBobSpeed;

            // Apply the currentSwayMultiplier to the walking bob!
            float bobX = Mathf.Cos(timer / 2) * bobAmountX * currentSwayMultiplier; 
            float bobY = Mathf.Sin(timer) * bobAmountY * currentSwayMultiplier;

            Vector3 targetPosition = startPosition + new Vector3(bobX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, smoothStep * Time.deltaTime);
        }
        else
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPosition, smoothStep * Time.deltaTime);
        }
    }
}