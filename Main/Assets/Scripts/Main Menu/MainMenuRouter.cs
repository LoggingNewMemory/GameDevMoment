using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuRouter : MonoBehaviour
{
    [Header("Scene Destinations")]
    [Tooltip("Exact name of your normal main menu scene")]
    public string normalMenuName = "Main_Menu"; 
    
    [Tooltip("Exact name of your post-game special main menu scene")]
    public string specialMenuName = "Special_Main_Menu";

    void Start()
    {
        // --- AGENT ZETA VIP CHECK ---
        // We check if the player has the Final Boss reward unlocked!
        if (PlayerPrefs.GetInt("Unlocked_TimeForCoding", 0) == 1)
        {
            Debug.Log("<color=magenta>[Agent Zeta] VIP DETECTED! Routing to Special Menu!</color>");
            SceneManager.LoadScene(specialMenuName);
        }
        else
        {
            Debug.Log("<color=cyan>[Agent Zeta] Standard player detected. Routing to Normal Menu.</color>");
            SceneManager.LoadScene(normalMenuName);
        }
    }
}