using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // <-- KOBO ADDED THIS! Required for TextMeshPro magic! ✨

public class SpecialMainMenu : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource audioSourceA;
    public AudioSource audioSourceB;

    [Header("Audio Clips")]
    public AudioClip stressMusic;    
    public AudioClip loginMusic;     
    public AudioClip mainMenuMusic;  

    [Header("Settings")]
    public float crossfadeDuration = 2.0f;
    public string firstGameScene = "Level_0"; 
    
    [Header("UI & Animation")]
    public Slider loadingBar; 
    public CanvasGroup finalMenuUI; 
    public Animator characterAnimator; 
    public float uiFadeDuration = 1.5f; 
    public GameObject resumeButton; 
    
    public TextMeshProUGUI graphicsText; // <-- Drag your Graphics Text here!

    // --- AGENT ZETA RESET SYSTEM ---
    [Header("Reset System")]
    public TextMeshProUGUI resetButtonText; 
    private bool isConfirmingReset = false;
    // -------------------------------

    // We store the background load here so the "Start" button can trigger it instantly!
    private AsyncOperation pendingLoad; 

    private void Start()
    {   
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        // --- NEW: LOAD SAVED GRAPHICS FROM HARD DRIVE ---
        if (PlayerPrefs.HasKey("SavedGraphics"))
        {
            int savedLevel = PlayerPrefs.GetInt("SavedGraphics");
            QualitySettings.SetQualityLevel(savedLevel);
        }

        // --- INITIALIZE GRAPHICS TEXT ON BOOTUP ---
        if (graphicsText != null)
        {
            graphicsText.text = "Graphics: " + QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        // --- SAVE SYSTEM CHECK ---
        // Check the hard drive to see if a save file called "SavedLevel" exists
        if (resumeButton != null)
        {
            if (PlayerPrefs.HasKey("SavedLevel"))
                resumeButton.SetActive(true); // Turn button ON
            else
                resumeButton.SetActive(false); // Turn button OFF
        }

        // Hide UI and disable clicking at the start
        if (finalMenuUI != null) 
        {
            finalMenuUI.alpha = 0f;
            finalMenuUI.interactable = false;
            finalMenuUI.blocksRaycasts = false;
        }

        if (characterAnimator != null) characterAnimator.speed = 0f;

        StartCoroutine(CinematicSequence());
    }

    private IEnumerator CinematicSequence()
    {
        // ==========================================
        // PHASE 1: STRESS ONLY (No loading yet)
        // ==========================================
        audioSourceA.clip = stressMusic;
        audioSourceA.volume = 1f;
        audioSourceA.Play();

        float waitTime = stressMusic.length - crossfadeDuration;
        if (waitTime > 0) yield return new WaitForSecondsRealtime(waitTime);

        // ==========================================
        // PHASE 2: CROSSFADE TO LOGIN & START LOADING
        // ==========================================
        yield return StartCoroutine(CrossfadeAudio(audioSourceA, audioSourceB, loginMusic));

        Application.backgroundLoadingPriority = ThreadPriority.Low;
        
        // We save the load operation to 'pendingLoad' so the Start button can use it!
        pendingLoad = SceneManager.LoadSceneAsync(firstGameScene);
        pendingLoad.allowSceneActivation = false;

        while (pendingLoad.progress < 0.9f)
        {
            if (loadingBar != null) loadingBar.value = pendingLoad.progress;
            yield return null; 
        }

        if (loadingBar != null) loadingBar.value = 1f;
        Application.backgroundLoadingPriority = ThreadPriority.Normal;

        // ==========================================
        // PHASE 3: MAIN MENU, ANIMATION, & UI FADE
        // ==========================================
        if (characterAnimator != null) characterAnimator.speed = 1f;

        if (finalMenuUI != null) StartCoroutine(FadeInUI());

        yield return StartCoroutine(CrossfadeAudio(audioSourceB, audioSourceA, mainMenuMusic));
    }
    
    // ==========================================
    // AUDIO & UI COROUTINES
    // ==========================================
    private IEnumerator CrossfadeAudio(AudioSource fadingOut, AudioSource fadingIn, AudioClip nextClip)
    {
        fadingIn.clip = nextClip;
        fadingIn.volume = 0f;
        fadingIn.loop = true; 
        fadingIn.Play();

        float timer = 0f;
        while (timer < crossfadeDuration)
        {
            timer += Time.unscaledDeltaTime; 
            fadingOut.volume = Mathf.Lerp(1f, 0f, timer / crossfadeDuration);
            fadingIn.volume = Mathf.Lerp(0f, 1f, timer / crossfadeDuration);
            yield return null;
        }

        fadingOut.volume = 0f;
        fadingIn.volume = 1f;
        fadingOut.Stop();
    }

    private IEnumerator FadeInUI()
    {
        float timer = 0f;
        while (timer < uiFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            finalMenuUI.alpha = Mathf.Lerp(0f, 1f, timer / uiFadeDuration);
            yield return null;
        }

        finalMenuUI.alpha = 1f;
        finalMenuUI.interactable = true;
        finalMenuUI.blocksRaycasts = true;
    }

    // ==========================================
    // BUTTON FUNCTIONS
    // ==========================================
    
    public void ClickStartGame()
    {
        // --- AGENT ZETA: NEW GAME PROTOCOL ---
        // 1. Reset the level location
        PlayerPrefs.DeleteKey("SavedLevel");
        
        // 2. Confiscate all standard weapons and power-ups!
        PlayerPrefs.DeleteKey("Unlocked_Shotgun");
        PlayerPrefs.DeleteKey("Unlocked_SMG");
        PlayerPrefs.DeleteKey("Unlocked_AssaultRifle");
        PlayerPrefs.DeleteKey("Unlocked_Sniper");
        PlayerPrefs.DeleteKey("Unlocked_LMG");
        PlayerPrefs.DeleteKey("Unlocked_RageOfCS");
        PlayerPrefs.DeleteKey("Unlocked_HaluOfCS");
        
        // Note: We avoid using DeleteAll() here so we don't accidentally delete 
        // their Graphics Settings or their VIP Menu Unlock flag!
        
        PlayerPrefs.Save();
        
        // Boot up the prologue!
        SceneManager.LoadScene(firstLevelName);
    }
    public void ClickResumeGame()
    {
        // Check the hard drive for the saved level name, and load it
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            string levelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneManager.LoadScene(levelToLoad);
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

    // --- NEW: NUCLEAR RESET FUNCTION ---
    public void ClickResetGame()
    {
        if (!isConfirmingReset)
        {
            // First click: Warn the player!
            isConfirmingReset = true;
            if (resetButtonText != null)
            {
                resetButtonText.text = "Press Again";
            }
            Debug.Log("<color=red>[Agent Zeta] RESET PENDING: Player is holding the detonator...</color>");
        }
        else
        {
            // Second click: Wipe everything and exit!
            Debug.Log("<color=red>[Agent Zeta] WIPING DATA: All PlayerPrefs deleted!</color>");
            
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }

    // --- NEW: GRAPHICS SETTINGS CYCLER ---
    public void ClickGraphicsSettings()
    {
        // Get the current graphics level (0 = Lowest, 1 = Medium, 2 = High, etc.)
        int currentLevel = QualitySettings.GetQualityLevel();
        
        // Calculate the next level. The '%' makes it loop back to 0 if it hits the maximum!
        int nextLevel = (currentLevel + 1) % QualitySettings.names.Length;
        
        // Apply the new graphics setting
        QualitySettings.SetQualityLevel(nextLevel);
        
        // --- NEW: SAVE IT TO THE HARD DRIVE! ---
        PlayerPrefs.SetInt("SavedGraphics", nextLevel);
        PlayerPrefs.Save();
        
        // --- UPDATE THE VISIBLE TEXT! ---
        if (graphicsText != null)
        {
            graphicsText.text = "Graphics: " + QualitySettings.names[nextLevel];
        }
        
        Debug.Log("Graphics saved and changed to: " + QualitySettings.names[nextLevel]);
    }
}