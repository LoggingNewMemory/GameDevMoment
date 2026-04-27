using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PostGameMenu : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource audioSourceA;
    public AudioSource audioSourceB;

    [Header("Audio Clips")]
    public AudioClip stressMusic;    // Drag Stress.m4a here
    public AudioClip loginMusic;     // Drag Login.wav here
    public AudioClip mainMenuMusic;  // Drag Main Menu.wav here

    [Header("Settings")]
    public float crossfadeDuration = 2.0f;
    public string firstGameScene = "Level_1"; 
    
    [Header("UI Elements")]
    public Slider loadingBar; // Optional: To show load progress
    public GameObject finalMenuUI; // The buttons that appear at the end

    private void Start()
    {
        // Hide the final menu UI at the start
        if (finalMenuUI != null) finalMenuUI.SetActive(false);

        // 1. Play Stress.m4a immediately while the character "Rises"
        audioSourceA.clip = stressMusic;
        audioSourceA.volume = 1f;
        audioSourceA.Play();

        // Start the grand sequence!
        StartCoroutine(CinematicSequence());
    }

    private IEnumerator CinematicSequence()
    {
        // 1. Wait for Stress.m4a to finish its exact audio duration
        yield return new WaitForSeconds(stressMusic.length);

        // 2. Immediately play Login.wav (Hard cut, no crossfade gap)
        audioSourceA.clip = loginMusic;
        audioSourceA.Play();

        // 3. Begin loading all required game files asynchronously
        yield return StartCoroutine(LoadGameDataAsync());

        // 4. Once loading hits 90%, crossfade the audio to Main Menu.wav
        yield return StartCoroutine(CrossfadeAudio(audioSourceA, audioSourceB, mainMenuMusic));

        // 5. Show the final clickable menu UI
        if (finalMenuUI != null) finalMenuUI.SetActive(true);
    }

    // ==========================================
    // ASYNC LOADING LOGIC
    // ==========================================
    private IEnumerator LoadGameDataAsync()
    {
        // This begins loading your heavy game scene in the background WITHOUT switching to it yet.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(firstGameScene);
        
        // Stop the scene from automatically switching when it hits 100%
        asyncLoad.allowSceneActivation = false;

        // Wait until it is fully loaded into the computer's RAM (Unity stops at 0.9 when allowSceneActivation is false)
        while (asyncLoad.progress < 0.9f)
        {
            if (loadingBar != null)
            {
                loadingBar.value = asyncLoad.progress;
            }
            yield return null;
        }

        if (loadingBar != null) loadingBar.value = 1f;
        
        // NOTE: We leave allowSceneActivation = false! 
        // When the player eventually clicks your "Start" button on the final menu UI, 
        // you just set asyncLoad.allowSceneActivation = true; and the game will start INSTANTLY.
    }

    // ==========================================
    // SEAMLESS DOUBLE-AUDIO CROSSFADE
    // ==========================================
    private IEnumerator CrossfadeAudio(AudioSource fadingOut, AudioSource fadingIn, AudioClip nextClip)
    {
        fadingIn.clip = nextClip;
        fadingIn.volume = 0f;
        fadingIn.Play();

        float timer = 0f;

        while (timer < crossfadeDuration)
        {
            timer += Time.deltaTime;
            
            // Gradually shift volumes
            fadingOut.volume = Mathf.Lerp(1f, 0f, timer / crossfadeDuration);
            fadingIn.volume = Mathf.Lerp(0f, 1f, timer / crossfadeDuration);
            
            yield return null;
        }

        // Ensure perfect values at the end and stop the old track
        fadingOut.volume = 0f;
        fadingIn.volume = 1f;
        fadingOut.Stop();
    }
}