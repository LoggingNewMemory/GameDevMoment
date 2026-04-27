using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UVScroller : MonoBehaviour
{
    [Header("Scroll Direction & Speed")]
    // Adjust these to make it scroll left/right or up/down
    public Vector2 scrollSpeed = new Vector2(0.5f, 0f); 
    
    private RawImage rawImage;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        // This shifts the image coordinates infinitely without moving the actual UI object
        Rect uvRect = rawImage.uvRect;
        uvRect.position += scrollSpeed * Time.deltaTime;
        rawImage.uvRect = uvRect;
    }
}