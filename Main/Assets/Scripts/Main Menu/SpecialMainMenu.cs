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
    public GameObject finalMenuUI; 
    public Animator characterAnimator; 

    private void Start()
    {
        // 1. Force the game time to unpause just in case it was frozen when the player died!
        Time.timeScale = 1f;

        if (finalMenuUI != null) finalMenuUI.SetActive(false);

        // FREEZE the animation so he stays dead on the floor while loading!
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
        if (waitTime > 0)
        {
            yield return new WaitForSecondsRealtime(waitTime);
        }

        // ==========================================
        // PHASE 2: CROSSFADE TO LOGIN & START LOADING
        // ==========================================
        yield return StartCoroutine(CrossfadeAudio(audioSourceA, audioSourceB, loginMusic));

        Application.backgroundLoadingPriority = ThreadPriority.Low;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(firstGameScene);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            if (loadingBar != null) loadingBar.value = asyncLoad.progress;
            yield return null; 
        }

        if (loadingBar != null) loadingBar.value = 1f;
        Application.backgroundLoadingPriority = ThreadPriority.Normal;

        // ==========================================
        // PHASE 3: MAIN MENU & ANIMATION
        // ==========================================
        
        // MOVED: UNFREEZE the animation IMMEDIATELY since loading just finished!
        if (characterAnimator != null) characterAnimator.speed = 1f;

        // Smoothly blend to Main Menu music (happens while he is rising up!)
        yield return StartCoroutine(CrossfadeAudio(audioSourceB, audioSourceA, mainMenuMusic));

        // Wait a few seconds for the rising animation to finish before showing the buttons
        yield return new WaitForSecondsRealtime(3.0f);

        if (finalMenuUI != null) finalMenuUI.SetActive(true);
    }
    
    // ==========================================
    // AUDIO CROSSFADE LOGIC
    // ==========================================
    private IEnumerator CrossfadeAudio(AudioSource fadingOut, AudioSource fadingIn, AudioClip nextClip)
    {
        fadingIn.clip = nextClip;
        fadingIn.volume = 0f;
        fadingIn.loop = true; // Ensure the tracks (like Login and Main Menu) loop properly
        fadingIn.Play();

        float timer = 0f;
        while (timer < crossfadeDuration)
        {
            // Use unscaledDeltaTime so the crossfade doesn't break if time gets paused elsewhere!
            timer += Time.unscaledDeltaTime; 
            
            fadingOut.volume = Mathf.Lerp(1f, 0f, timer / crossfadeDuration);
            fadingIn.volume = Mathf.Lerp(0f, 1f, timer / crossfadeDuration);
            
            yield return null;
        }

        // Lock in final exact values and completely stop the old track
        fadingOut.volume = 0f;
        fadingIn.volume = 1f;
        fadingOut.Stop();
    }
}