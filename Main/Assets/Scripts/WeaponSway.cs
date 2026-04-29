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
    public float steadyMultiplier = 0.2f; 
    public float steadyTransitionSpeed = 15f; 
    
    [Header("Smoothing")]
    public float smoothStep = 10f;   

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timer = 0f;
    
    private float currentSwayMultiplier = 1f;

    void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Mouse.current == null || Keyboard.current == null) return;

        // ==========================================
        // 0. SHOOTING / BRACING CHECK
        // ==========================================
        bool isShooting = Mouse.current.leftButton.isPressed;
        float targetMultiplier = isShooting ? steadyMultiplier : 1f;

        // <-- FIXED: Unscaled Time
        currentSwayMultiplier = Mathf.Lerp(currentSwayMultiplier, targetMultiplier, Time.unscaledDeltaTime * steadyTransitionSpeed);


        // ==========================================
        // 1. ROTATION SWAY (Mouse Look)
        // ==========================================
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        // <-- FIXED: Unscaled Time
        float mouseX = -mouseDelta.x * swayMultiplier * currentSwayMultiplier * Time.unscaledDeltaTime;
        float mouseY = -mouseDelta.y * swayMultiplier * currentSwayMultiplier * Time.unscaledDeltaTime;

        mouseX = Mathf.Clamp(mouseX, -maxSway, maxSway);
        mouseY = Mathf.Clamp(mouseY, -maxSway, maxSway);

        Quaternion targetRotation = startRotation * Quaternion.Euler(mouseY, mouseX, 0f);
        // <-- FIXED: Unscaled Time
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smoothStep * Time.unscaledDeltaTime);


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

            // <-- FIXED: Unscaled Time
            timer += Time.unscaledDeltaTime * currentBobSpeed;

            float bobX = Mathf.Cos(timer / 2) * bobAmountX * currentSwayMultiplier; 
            float bobY = Mathf.Sin(timer) * bobAmountY * currentSwayMultiplier;

            Vector3 targetPosition = startPosition + new Vector3(bobX, bobY, 0f);
            // <-- FIXED: Unscaled Time
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, smoothStep * Time.unscaledDeltaTime);
        }
        else
        {
            timer = 0f;
            // <-- FIXED: Unscaled Time
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPosition, smoothStep * Time.unscaledDeltaTime);
        }
    }
}