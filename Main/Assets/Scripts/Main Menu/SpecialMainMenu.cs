using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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
    public GameObject resumeButton; // NEW: Drag your Resume Button here!

    // NEW: We store the background load here so the "Start" button can trigger it instantly!
    private AsyncOperation pendingLoad; 

    private void Start()
    {
        Time.timeScale = 1f;

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
        // Delete the old save so they start fresh
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        
        // Because we already loaded the scene in the background, this makes it pop up instantly!
        if (pendingLoad != null)
        {
            pendingLoad.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(firstGameScene);
        }
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
}