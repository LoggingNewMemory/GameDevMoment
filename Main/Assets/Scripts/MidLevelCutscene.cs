using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class MidLevelCutscene : MonoBehaviour
{
    [Header("Cinematic Intel")]
    public GameObject videoCanvas; 
    public VideoPlayer videoPlayer;

    [Header("Deployment Payload")]
    public GameObject bossToSpawn; 

    private bool isPlaying = false;

    void Start()
    {
        // Keep the payload hidden until the spawner gives the green light!
        if (videoCanvas != null) videoCanvas.SetActive(false);
        if (bossToSpawn != null) bossToSpawn.SetActive(false); 

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    // AGENT ZETA TACTIC: The ArenaSpawner will call this directly now!
    public void StartCutscene()
    {
        isPlaying = true;
        Debug.Log("<color=cyan>[Agent Zeta] Wave cleared! Initiating Boss Cutscene...</color>");

        // Freeze reality!
        Time.timeScale = 0f;
        
        if (videoCanvas != null) videoCanvas.SetActive(true);
        if (videoPlayer != null) videoPlayer.Play();
    }

    void Update()
    {
        if (isPlaying && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            EndCutscene();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        EndCutscene();
    }

    void EndCutscene()
    {
        if (!isPlaying) return;
        isPlaying = false;

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoCanvas != null) videoCanvas.SetActive(false);
        
        // Unfreeze reality!
        Time.timeScale = 1f;
        
        // Spawn Shanna!
        if (bossToSpawn != null) bossToSpawn.SetActive(true);
    }
}