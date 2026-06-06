using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Header("Fader Settings")]
    public Image fadeImage;
    public float fadeSpeed = 1.5f;

    void Awake()
    {
        // Secret Agent Singleton: Makes sure only one fader exists!
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeInRoutine());
        }
    }

    IEnumerator FadeInRoutine()
    {
        fadeImage.color = new Color(0, 0, 0, 1); // Start pitch black
        
        while (fadeImage.color.a > 0)
        {
            fadeImage.color = new Color(0, 0, 0, fadeImage.color.a - (Time.deltaTime * fadeSpeed));
            yield return null;
        }
        
        fadeImage.gameObject.SetActive(false); // Turn off so it doesn't block clicks!
    }

    // Call this from other scripts to trigger the exit fade!
    public void FadeOutToScene(string sceneName)
    {
        StartCoroutine(FadeOutRoutine(sceneName));
    }

    IEnumerator FadeOutRoutine(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 0); // Start clear
        
        while (fadeImage.color.a < 1)
        {
            fadeImage.color = new Color(0, 0, 0, fadeImage.color.a + (Time.deltaTime * fadeSpeed));
            yield return null;
        }
        
        // Breach into the next level once the screen is totally black!
        SceneManager.LoadScene(sceneName);
    }
}