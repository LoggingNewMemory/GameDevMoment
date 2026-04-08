using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Position Bobbing (Walking/Running)")]
    public float walkBobSpeed = 12f;
    public float runBobSpeed = 18f;
    public float bobAmountX = 0.05f; // How far left/right it bounces
    public float bobAmountY = 0.05f; // How far up/down it bounces

    [Header("Rotation Sway (Mouse Look)")]
    public float swayMultiplier = 3f;
    public float maxSway = 4f;       // Prevents the gun from rotating completely sideways

    [Header("Smoothing")]
    public float smoothStep = 10f;   // How snappy the gun feels

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float timer = 0f;

    void Start()
    {
        // Remember exactly where the WeaponHolder started!
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // ==========================================
        // 1. ROTATION SWAY (Looking around with mouse)
        // ==========================================
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        // Invert the mouse input so the gun drags BEHIND the camera
        float mouseX = -mouseDelta.x * swayMultiplier * Time.deltaTime;
        float mouseY = -mouseDelta.y * swayMultiplier * Time.deltaTime;

        // Clamp it so the gun doesn't flip upside down if you flick your mouse super fast
        mouseX = Mathf.Clamp(mouseX, -maxSway, maxSway);
        mouseY = Mathf.Clamp(mouseY, -maxSway, maxSway);

        Quaternion targetRotation = startRotation * Quaternion.Euler(mouseY, mouseX, 0f);
        
        // Apply the rotation smoothly
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smoothStep * Time.deltaTime);


        // ==========================================
        // 2. POSITION BOBBING (Walking with WASD)
        // ==========================================
        float xInput = 0f;
        float zInput = 0f;

        if (Keyboard.current.wKey.isPressed) zInput += 1f;
        if (Keyboard.current.sKey.isPressed) zInput -= 1f;
        if (Keyboard.current.dKey.isPressed) xInput += 1f;
        if (Keyboard.current.aKey.isPressed) xInput -= 1f;

        Vector3 inputVector = new Vector3(xInput, 0f, zInput);

        // Are we moving?
        if (inputVector.magnitude > 0.1f)
        {
            // Are we running? (Holding Shift and moving forward)
            bool isRunning = Keyboard.current.leftShiftKey.isPressed && zInput > 0;
            float currentBobSpeed = isRunning ? runBobSpeed : walkBobSpeed;

            // Increase the timer based on our speed
            timer += Time.deltaTime * currentBobSpeed;

            // Math magic! Cosine and Sine create a perfect "Figure 8" walking pattern
            float bobX = Mathf.Cos(timer / 2) * bobAmountX; 
            float bobY = Mathf.Sin(timer) * bobAmountY;

            Vector3 targetPosition = startPosition + new Vector3(bobX, bobY, 0f);
            
            // Apply the position smoothly
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, smoothStep * Time.deltaTime);
        }
        else
        {
            // If we stop moving, smoothly return the gun to the dead center
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPosition, smoothStep * Time.deltaTime);
        }
    }
}