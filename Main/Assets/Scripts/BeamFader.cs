using UnityEngine;
using System.Collections;

public class BeamFader : MonoBehaviour
{
    public float fadeDuration = 0.2f; 
    private Material beamMatInstance;

    public void ActivateBeam(Vector3 startPoint, Vector3 endPoint)
    {
        gameObject.SetActive(true);
        
        // 1. Calculate the exact center and distance
        float distance = Vector3.Distance(startPoint, endPoint);
        Vector3 centerPosition = (startPoint + endPoint) / 2f;

        // 2. Position the cube exactly between the gun and the target
        transform.position = centerPosition;

        // 3. Make the cube point at the target
        transform.LookAt(endPoint);

        // 4. Stretch the cube to form a perfect laser beam! 
        // (0.1 thickness, and 'distance' for the length)
        transform.localScale = new Vector3(0.1f, 0.1f, distance);

        // 5. Clone the material to fade safely
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            beamMatInstance = new Material(rend.sharedMaterial);
            rend.material = beamMatInstance;
        }

        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

            // Change the physical material color safely
            if (beamMatInstance != null)
            {
                beamMatInstance.color = new Color(1f, 1f, 1f, currentAlpha);
            }

            yield return null; 
        }

        Destroy(gameObject); // Kamikaze cleanup!
    }

    void OnDestroy()
    {
        // Prevent memory leaks
        if (beamMatInstance != null)
        {
            Destroy(beamMatInstance);
        }
    }
}