using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro; // <-- KOBO ADDED THIS! For TextMeshPro UI ✨

public class MainMenuController : MonoBehaviour
{
    [Header("Slideshow Settings")]
    public Sprite[] backgroundImages; 
    public Image bgLayer1;            
    public Image bgLayer2;            
    
    public float timePerImage = 5f;   
    public float fadeDuration = 1.5f; 
    public float zoomSpeed = 0.02f;   

    [Header("Level & Save Settings")]
    public string firstLevelName = "Level_0"; 
    public GameObject resumeButton; 

    [Header("Tutorial & Graphics Settings")]
    public GameObject howToPlayPanel; 
    public TextMeshProUGUI graphicsText; // <-- KOBO ADDED THIS! Drag your Graphics Text here!

    private int currentIndex = 0;
    private Image currentBg;
    private Image nextBg;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- NEW: LOAD SAVED GRAPHICS FROM HARD DRIVE ---
        if (PlayerPrefs.HasKey("SavedGraphics"))
        {
            int savedLevel = PlayerPrefs.GetInt("SavedGraphics");
            QualitySettings.SetQualityLevel(savedLevel);
        }

        // --- NEW: INITIALIZE GRAPHICS TEXT ON BOOTUP ---
        if (graphicsText != null)
        {
            graphicsText.text = "Graphics: " + QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        // --- Save System Check ---
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            resumeButton.SetActive(true);
        }
        else
        {
            resumeButton.SetActive(false);
        }

        // --- Safety Check: Ensure Panel is OFF at start ---
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }

        if (backgroundImages.Length > 0)
        {
            currentBg = bgLayer1;
            nextBg = bgLayer2;

            currentBg.sprite = backgroundImages[0];
            currentBg.color = Color.white;
            nextBg.color = new Color(1f, 1f, 1f, 0f); 

            StartCoroutine(SlideshowRoutine());
        }
    }

    void Update()
    {
        if (currentBg != null && currentBg.color.a > 0)
        {
            currentBg.rectTransform.localScale += Vector3.one * (zoomSpeed * Time.unscaledDeltaTime);
        }
        
        if (nextBg != null && nextBg.color.a > 0)
        {
            nextBg.rectTransform.localScale += Vector3.one * (zoomSpeed * Time.unscaledDeltaTime);
        }

        // --- How to Play ESC listener (NEW INPUT SYSTEM FIX) ---
        if (howToPlayPanel != null && howToPlayPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                howToPlayPanel.SetActive(false);
            }
        }
    }

    IEnumerator SlideshowRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(timePerImage);

            float timer = 0f;
            while (timer < fadeDuration / 2f) 
            {
                timer += Time.unscaledDeltaTime;
                float alpha = 1f - (timer / (fadeDuration / 2f));
                currentBg.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            
            currentBg.color = new Color(1f, 1f, 1f, 0f);

            int nextIndex = (currentIndex + 1) % backgroundImages.Length;
            currentBg.sprite = backgroundImages[nextIndex];
            currentBg.rectTransform.localScale = Vector3.one; 

            timer = 0f;
            while (timer < fadeDuration / 2f) 
            {
                timer += Time.unscaledDeltaTime;
                float alpha = timer / (fadeDuration / 2f);
                currentBg.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            currentBg.color = Color.white;
            currentIndex = nextIndex;
        }
    }

    // ==========================================
    // BUTTON FUNCTIONS
    // ==========================================
    
    public void ClickStartGame()
    {
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        SceneManager.LoadScene(firstLevelName);
    }

    public void ClickResumeGame()
    {
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            string levelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneManager.LoadScene(levelToLoad);
        }
    }

    public void ClickHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    public void ClickExitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // --- NEW: GRAPHICS SETTINGS CYCLER ---
    public void ClickGraphicsSettings()
    {
        int currentLevel = QualitySettings.GetQualityLevel();
        int nextLevel = (currentLevel + 1) % QualitySettings.names.Length;
        
        QualitySettings.SetQualityLevel(nextLevel);
        
        // Save to hard drive
        PlayerPrefs.SetInt("SavedGraphics", nextLevel);
        PlayerPrefs.Save();
        
        // Update visible text
        if (graphicsText != null)
        {
            graphicsText.text = "Graphics: " + QualitySettings.names[nextLevel];
        }
        
        Debug.Log("Graphics saved and changed to: " + QualitySettings.names[nextLevel]);
    }
}