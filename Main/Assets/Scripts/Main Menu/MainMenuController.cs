using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro; 

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
    [Tooltip("Make sure this matches your scene name EXACTLY!")]
    public string firstLevelName = "CUTSCENE Prolouge"; // <-- AGENT ZETA TACTIC: Updated destination!
    public GameObject resumeButton; 

    [Header("Tutorial & Graphics Settings")]
    public GameObject howToPlayPanel; 
    public TextMeshProUGUI graphicsText; 

    private int currentIndex = 0;
    private Image currentBg;
    private Image nextBg;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerPrefs.HasKey("SavedGraphics"))
        {
            int savedLevel = PlayerPrefs.GetInt("SavedGraphics");
            QualitySettings.SetQualityLevel(savedLevel);
        }

        if (graphicsText != null)
        {
            graphicsText.text = "Graphics: " + QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            resumeButton.SetActive(true);
        }
        else
        {
            resumeButton.SetActive(false);
        }

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
    
    public void ClickStartGame()
    {
        // Wipe the old save so they start fresh!
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        
        // Boot up the prologue!
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

    public void ClickGraphicsSettings()
    {
        int currentLevel = QualitySettings.GetQualityLevel();
        int nextLevel = (currentLevel + 1) % QualitySettings.names.Length;
        
        QualitySettings.SetQualityLevel(nextLevel);
        
        PlayerPrefs.SetInt("SavedGraphics", nextLevel);
        PlayerPrefs.Save();
        
        if (graphicsText != null)
        {
            graphicsText.text = "Graphics: " + QualitySettings.names[nextLevel];
        }
        
        Debug.Log("Graphics saved and changed to: " + QualitySettings.names[nextLevel]);
    }
}