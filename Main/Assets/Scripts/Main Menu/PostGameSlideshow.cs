using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PostGameSlideshow : MonoBehaviour
{
    [Header("Slideshow Images")]
    public Sprite[] backgroundImages; 
    public Image currentBg; // We only need ONE image layer now!
    
    [Header("Movement Settings")]
    public float timePerImage = 5f;   
    public float zoomSpeed = 0.02f;   
    public float panSpeed = 15f; // How fast it drifts sideways
    
    [Header("Endfield Aesthetic")]
    [Range(0f, 1f)] public float maxOpacity = 1f;

    private int currentIndex = 0;

    void Start()
    {
        if (backgroundImages.Length > 0)
        {
            currentBg.sprite = backgroundImages[0];
            currentBg.color = new Color(1f, 1f, 1f, maxOpacity);
            StartCoroutine(SlideshowRoutine());
        }
    }

    void Update()
    {
        if (currentBg != null)
        {
            // 1. Slowly zoom the image in
            currentBg.rectTransform.localScale += Vector3.one * (zoomSpeed * Time.unscaledDeltaTime);
            
            // 2. Slowly pan the image sideways
            currentBg.rectTransform.anchoredPosition += new Vector2(panSpeed * Time.unscaledDeltaTime, 0);
        }
    }

    IEnumerator SlideshowRoutine()
    {
        while (true)
        {
            // Wait for the duration of the current image
            yield return new WaitForSecondsRealtime(timePerImage);

            // INSTANT HARD CUT to the next image
            currentIndex = (currentIndex + 1) % backgroundImages.Length;
            currentBg.sprite = backgroundImages[currentIndex];
            
            // Reset the scale and position so the new image starts centered
            currentBg.rectTransform.localScale = Vector3.one; 
            currentBg.rectTransform.anchoredPosition = Vector2.zero; 
        }
    }
}