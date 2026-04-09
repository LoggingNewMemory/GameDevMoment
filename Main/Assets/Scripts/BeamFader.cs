using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BeamFader : MonoBehaviour
{
    private LineRenderer lr;
    public float fadeDuration = 0.2f; // Keep it super fast for a railgun!

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    public void ActivateBeam(Vector3 startPoint, Vector3 endPoint)
    {
        // Cancel any previous fading in case we spam-shot
        StopAllCoroutines(); 

        // 1. Set the points in world space
        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);

        // 2. Turn the beam on
        lr.enabled = true;

        // 3. Start the fade routine
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        float elapsedTime = 0f;
        
        // Start completely white/opaque
        lr.startColor = Color.white;
        lr.endColor = Color.white;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Calculate a new alpha value (1 to 0)
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

            // Create a faded color
            Color fadedColor = new Color(1f, 1f, 1f, newAlpha);
            
            // Apply it to the Line Renderer
            lr.startColor = fadedColor;
            lr.endColor = fadedColor;

            yield return null; 
        }

        // 4. Finally turn the beam back off
        lr.enabled = false;
    }
}