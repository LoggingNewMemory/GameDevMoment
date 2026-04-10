using UnityEngine;
using UnityEditor;

public class VRMToURPConverter : EditorWindow
{
    // This creates a new button at the very top of your Unity window!
    [MenuItem("Tools/1-Click VRM to URP Material Converter")]
    public static void UpgradeMaterials()
    {
        // 1. Get whatever you currently have clicked on in the Hierarchy
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Please select Jambret or The Black in the Hierarchy first!");
            return;
        }

        // Allow you to Ctrl+Z if you don't like the result
        Undo.RecordObjects(selectedObjects, "Auto-Convert VRM Materials");

        int convertedCount = 0;
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLitShader == null)
        {
            Debug.LogError("Could not find the URP Lit shader. Are you sure URP is installed?");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            // Find every mesh on the character (Body, Face, Hair, etc.)
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            
            foreach (Renderer rend in renderers)
            {
                Material[] mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    
                    // Skip if it's already converted or missing
                    if (mat == null || mat.shader == urpLitShader) continue;

                    // 2. RESCUE THE TEXTURES
                    // Old shaders use "_MainTex", URP uses "_BaseMap"
                    Texture mainTex = null;
                    if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");

                    Color mainColor = Color.white;
                    if (mat.HasProperty("_Color")) mainColor = mat.GetColor("_Color");

                    // 3. APPLY THE URP SHADER
                    mat.shader = urpLitShader;

                    // 4. PLUG THE TEXTURE BACK IN
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    mat.SetColor("_BaseColor", mainColor);

                    // 5. AUTO-FIX TRANSPARENCY
                    // If the material name has "Hair" in it, automatically turn on Alpha Clipping so it isn't a solid block!
                    if (mat.name.ToLower().Contains("hair"))
                    {
                        mat.SetFloat("_AlphaClip", 1.0f);
                    }

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Success! Converted {convertedCount} materials to URP Lit. They will now cast shadows!");
    }
}