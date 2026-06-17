using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AgentZetaGunUpdater
{
    // This creates a brand new button at the very top of your Unity window!
    [MenuItem("Tools/Agent Zeta/Upgrade All Guns to Lasers")]
    public static void UpgradeAllGuns()
    {
        // 1. Find EVERY Single SimpleShoot script in the active level (even if it's hidden)
        SimpleShoot[] allGuns = Object.FindObjectsByType<SimpleShoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        int upgradedCount = 0;
        int skippedCount = 0;

        // 2. Loop through every gun and filter out the Railgun!
        foreach (SimpleShoot gun in allGuns)
        {
            // --- AGENT ZETA FILTER PROTOCOL ---
            // If the gun is flagged as a Railgun, or has "Railgun" in its name, SKIP IT!
            if (gun.isRailgun || gun.gameObject.name.Contains("Railgun"))
            {
                skippedCount++;
                continue; 
            }
            // ----------------------------------

            gun.useProjectile = true; // TICKS THE BOX AUTOMATICALLY!
            
            // Tell Unity we modified this object so it knows to save it
            EditorUtility.SetDirty(gun); 
            upgradedCount++;
        }
        
        // 3. Mark the whole scene as "Dirty" (unsaved changes) so CTRL+S actually saves our hack!
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        Debug.Log($"<color=green>[Agent Zeta] MEGA HACK COMPLETE: {upgradedCount} guns upgraded to Lasers! Skipped {skippedCount} Railguns to preserve their power!</color>");
    }
}