using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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
    public GameObject resumeButton; // Drag your Resume Button object here!

    private int currentIndex = 0;
    private Image currentBg;
    private Image nextBg;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- NEW: Save System Check ---
        // Check the hard drive to see if a save file called "SavedLevel" exists
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            // A save file exists! Turn the button ON.
            resumeButton.SetActive(true);
        }
        else
        {
            // No save file. Turn the button OFF.
            resumeButton.SetActive(false);
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
        // If they click "Start" (New Game), we delete the old save so they start fresh!
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(firstLevelName);
    }

    public void ClickResumeGame()
    {
        // Check the hard drive for the saved level name, and load it instantly
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            string levelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneManager.LoadScene(levelToLoad);
        }
    }

public void ClickExitGame()
    {
        Debug.Log("Exiting Game...");
        
        // This actually closes the game when you build it into a real application
        Application.Quit();

        // This magically stops the play button while you are testing inside the Unity Editor!
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}