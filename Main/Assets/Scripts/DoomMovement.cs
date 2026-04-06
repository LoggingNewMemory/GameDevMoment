using UnityEngine;
using UnityEngine.InputSystem; // <-- Required for the new input system!

public class DoomMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform playerCamera;
    
    public float speed = 15f; 
    
    // NOTE: The New Input System reads raw mouse pixels, so the numbers are huge. 
    // Sensitivity needs to be set much lower than the legacy system!
    public float mouseSensitivity = 0.1f; 

    private float xRotation = 0f;

    void Start()
    {
        // Lock the mouse cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Safety check in case a keyboard or mouse isn't plugged in
        if (Mouse.current == null || Keyboard.current == null) return;

        // --- LOOKING AROUND ---
        // Read raw pixel delta from the mouse
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // Look up and down (Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Look left and right (Yaw - rotates the whole body)
        transform.Rotate(Vector3.up * mouseX);


        // --- SNAPPY MOVEMENT ---
        float x = 0f;
        float z = 0f;

        // Read raw keyboard keys for instant on/off movement (No sliding)
        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;

        Vector3 move = transform.right * x + transform.forward * z;
        
        // Prevent moving faster when running diagonally (a classic Doom bug, but usually annoying)
        if (move.magnitude > 1f) move.Normalize();

        // SimpleMove automatically applies basic gravity!
        controller.SimpleMove(move * speed);
    }
}