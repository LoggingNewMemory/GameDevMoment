using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class CutsceneLoader : MonoBehaviour
{
    [Header("Scene Routing Intel")]
    [Tooltip("The EXACT name of the next scene (e.g., 'Level 0' or 'special_main_menu')")]
    public string nextSceneName;

    [Header("UI & Video Links")]
    public VideoPlayer videoPlayer;
    public TextMeshProUGUI skipPromptText;

    private AsyncOperation asyncLoad;
    private bool isSceneReady = false;

    void Start()
    {
        // 1. Hide the skip text at the start so they watch the movie!
        if (skipPromptText != null) skipPromptText.gameObject.SetActive(false);

        if (videoPlayer != null)
        {
            // Hook into the video player to know when the movie naturally finishes
            videoPlayer.loopPointReached += OnVideoFinished;
            
            // AGENT ZETA TACTICS: Wait for the video to stabilize BEFORE loading!
            StartCoroutine(WaitForVideoThenLoad());
        }
        else
        {
            // Fallback just in case you ever use this script without a video
            StartCoroutine(LoadNextLevelAsync());
        }
    }

    IEnumerator WaitForVideoThenLoad()
    {
        // 1. Wait until the video has successfully buffered and started playing
        while (!videoPlayer.isPlaying)
        {
            yield return null;
        }

        // 2. Give the CPU and SSD 1 extra second of breathing room
        yield return new WaitForSeconds(1f);

        // 3. NOW initiate the heavy background loading!
        StartCoroutine(LoadNextLevelAsync());
    }

    IEnumerator LoadNextLevelAsync()
    {
        // --- AGENT ZETA HACK: THROTTLE THE CPU ---
        // Force Unity to load the background level using the absolute lowest CPU priority
        // so it stops stealing processing power from the Video Player!
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // Tell Unity to start downloading the level into RAM
        asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;

        // Unity stops async loading at 0.9f when allowSceneActivation is false
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // The next level is locked, loaded, and waiting behind the door!
        isSceneReady = true;
        
        // Uncloak the skip text!
        if (skipPromptText != null) skipPromptText.gameObject.SetActive(true);
    }

    void Update()
    {
        // If the level is fully loaded in the background AND the player presses E...
        if (isSceneReady && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ProceedToNextScene();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // If the video ends naturally before they press E, pause it on the last frame
        vp.Pause();
    }

    void ProceedToNextScene()
    {
        Debug.Log($"<color=cyan>[Agent Zeta] Fire in the hole! Breaching into: {nextSceneName}...</color>");
        
        // --- RESTORE CPU POWER ---
        // Give the game its normal processing power back for the actual gameplay!
        Application.backgroundLoadingPriority = ThreadPriority.High;

        // --- SECRETS UNLOCK HACK ---
        if (nextSceneName == "special_main_menu")
        {
            PlayerPrefs.SetInt("UnlockedRailgun", 1);
            PlayerPrefs.SetInt("UnlockedTimeForCoding", 1);
            PlayerPrefs.Save();
        }

        // Kick down the door and instantly transition to the pre-loaded scene!
        asyncLoad.allowSceneActivation = true;
    }
}