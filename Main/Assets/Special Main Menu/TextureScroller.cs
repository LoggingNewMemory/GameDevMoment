using UnityEngine;
using System.Collections; // NEW: Required for smooth transitions over time

public class TextureScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeed = 0.5f;
    public bool moveVertically = true;

    [Header("Animation Sync")]
    public Animator playerAnimator;
    public string targetStateName = "Walk";

    [Header("Transition Settings")]
    [Tooltip("How many seconds it takes to rotate the player and fade the road.")]
    public float transitionDuration = 1.5f; 
    public float walkRotationX = -35f;

    private Renderer rend;
    private float currentOffset;
    private bool hasStartedWalking = false;

    void Start()
    {
        rend = GetComponent<Renderer>();

        if (playerAnimator != null)
        {
            rend.enabled = false; 
            SetMaterialAlpha(0f); // Make sure it's fully invisible at start
        }
    }

    void Update()
    {
        // 1. Wait for Walk animation to begin
        if (playerAnimator != null && !hasStartedWalking)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            
            if (stateInfo.IsName(targetStateName))
            {
                hasStartedWalking = true;
                // Start the smooth transition!
                StartCoroutine(SmoothTransitionRoutine()); 
            }
        }

        // 2. Only scroll the texture IF we have started walking
        if (hasStartedWalking && rend.enabled)
        {
            currentOffset += scrollSpeed * Time.deltaTime;

            if (moveVertically)
            {
                rend.material.SetTextureOffset("_BaseMap", new Vector2(0, currentOffset));
                rend.material.SetTextureOffset("_MainTex", new Vector2(0, currentOffset));
            }
            else
            {
                rend.material.SetTextureOffset("_BaseMap", new Vector2(currentOffset, 0)); 
                rend.material.SetTextureOffset("_MainTex", new Vector2(currentOffset, 0)); 
            }
        }
    }

    // ==========================================
    // THE MAGIC TRANSITION COROUTINE
    // ==========================================
    private IEnumerator SmoothTransitionRoutine()
    {
        rend.enabled = true; // Turn the object on (but it's currently invisible)

        float elapsed = 0f;
        Vector3 startRot = playerAnimator.transform.localEulerAngles;
        
        // Unity sometimes reads 0 degrees as 360. This prevents the character from doing a backflip!
        float startX = startRot.x > 180 ? startRot.x - 360 : startRot.x; 

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / transitionDuration; // Goes from 0.0 to 1.0

            // 1. Smoothly fade the material in
            SetMaterialAlpha(Mathf.Lerp(0f, 1f, percent));

            // 2. Smoothly tilt the player
            float currentX = Mathf.Lerp(startX, walkRotationX, percent);
            playerAnimator.transform.localRotation = Quaternion.Euler(currentX, startRot.y, startRot.z);

            yield return null; // Wait until the next frame
        }

        // Snap exactly to the final values at the very end to prevent math rounding errors
        SetMaterialAlpha(1f);
        playerAnimator.transform.localRotation = Quaternion.Euler(walkRotationX, startRot.y, startRot.z);
    }

    // Helper function to safely change material transparency
    private void SetMaterialAlpha(float alpha)
    {
        if (rend.material.HasProperty("_BaseColor")) // For URP
        {
            Color c = rend.material.GetColor("_BaseColor");
            c.a = alpha;
            rend.material.SetColor("_BaseColor", c);
        }
        else if (rend.material.HasProperty("_Color")) // For Standard Built-In
        {
            Color c = rend.material.GetColor("_Color");
            c.a = alpha;
            rend.material.SetColor("_Color", c);
        }
    }
}