using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI Elements")]
    public GameObject pauseMenuUI;

    [Header("Settings")]
    public string mainMenuSceneName = "Special_Main_Menu";

    [Header("Player Reference")]
    [Tooltip("Drag Pria Sigma 1 here exactly ONCE.")]
    public GameObject playerRoot; 

    // Hidden lists that the code will fill up automatically!
    private DoomMovement playerMovement;
    private SimpleShoot[] allGuns;
    private SimpleMelee[] allSwords;

    void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        // THE MAGIC TRICK: Find the components automatically!
        if (playerRoot != null)
        {
            playerMovement = playerRoot.GetComponentInChildren<DoomMovement>(true);
            
            // "true" tells it to find weapons even if they are currently hidden/holstered!
            allGuns = playerRoot.GetComponentsInChildren<SimpleShoot>(true);
            allSwords = playerRoot.GetComponentsInChildren<SimpleMelee>(true);
        }
    }

    void Update()
    {
        // Directly reading the hardware
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;          
        AudioListener.pause = false;  
        GameIsPaused = false;

        // Re-lock and hide the cursor so the player can look around again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Turn everything back on!
        if (playerMovement != null) playerMovement.enabled = true;
        foreach (var gun in allGuns) { if (gun != null) gun.enabled = true; }
        foreach (var sword in allSwords) { if (sword != null) sword.enabled = true; }
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;           
        AudioListener.pause = true;   
        GameIsPaused = true;

        // Unlock and show the cursor so the player can actually click the UI buttons!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Turn everything off!
        if (playerMovement != null) playerMovement.enabled = false;
        foreach (var gun in allGuns) { if (gun != null) gun.enabled = false; }
        foreach (var sword in allSwords) { if (sword != null) sword.enabled = false; }
    }

    public void SaveProgress()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedLevel", currentScene);
        PlayerPrefs.Save();
        Debug.Log("Game Saved Successfully! Current Level: " + currentScene);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameIsPaused = false;
        
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}