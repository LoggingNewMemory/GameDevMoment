using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI Elements")]
    public GameObject pauseMenuUI;
    
    [Header("Menu Buttons")]
    public GameObject resumeButton;   // Drag normal Resume button here
    public GameObject restartButton;  // Drag new Restart button here
    public GameObject saveButton;     // Drag Save button here

    [Header("Settings")]
    public string mainMenuSceneName = "Special_Main_Menu";

    [Header("Player Reference")]
    public GameObject playerRoot; 

    private DoomMovement playerMovement;
    private SimpleShoot[] allGuns;
    private SimpleMelee[] allSwords;
    
    // NEW: Track if the player is dead so they can't unpause with Escape!
    private bool isPlayerDead = false;

    void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        if (playerRoot != null)
        {
            playerMovement = playerRoot.GetComponentInChildren<DoomMovement>(true);
            allGuns = playerRoot.GetComponentsInChildren<SimpleShoot>(true);
            allSwords = playerRoot.GetComponentsInChildren<SimpleMelee>(true);
        }
    }

    void Update()
    {
        // If the player is dead, completely ignore the Escape key!
        if (isPlayerDead) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;          
        AudioListener.pause = false;  
        GameIsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Ensure normal pause menu layout
        if (resumeButton != null) resumeButton.SetActive(true);
        if (restartButton != null) restartButton.SetActive(false);
        if (saveButton != null) saveButton.SetActive(true);

        if (playerMovement != null) playerMovement.enabled = false;
        foreach (var gun in allGuns) { if (gun != null) gun.enabled = false; }
        foreach (var sword in allSwords) { if (sword != null) sword.enabled = false; }
    }

    // ==========================================
    // NEW: GAME OVER LOGIC
    // ==========================================
    public void TriggerGameOver()
    {
        isPlayerDead = true;
        GameIsPaused = true;
        
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;           
        AudioListener.pause = true;   

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Swap the buttons! Hide Resume/Save, Show Restart
        if (resumeButton != null) resumeButton.SetActive(false);
        if (restartButton != null) restartButton.SetActive(true);
        if (saveButton != null) saveButton.SetActive(false); // No saving while dead!

        // Freeze the player
        if (playerMovement != null) playerMovement.enabled = false;
        foreach (var gun in allGuns) { if (gun != null) gun.enabled = false; }
        foreach (var sword in allSwords) { if (sword != null) sword.enabled = false; }
    }

    public void RestartLevel()
    {
        // Unfreeze time and reload the exact scene we are currently in
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameIsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ==========================================
    // EXISTING BUTTONS
    // ==========================================
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
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}