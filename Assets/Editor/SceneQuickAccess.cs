using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class SceneQuickAccess : EditorWindow
{
    // Shortcut: Ctrl + G
    [MenuItem("Scenes/Open Scene List _%g")]
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        SceneQuickAccess window = GetWindow<SceneQuickAccess>(true, "Scene Selector", true);
        
        // Sizing: Make it look like a popup list
        window.minSize = new Vector2(250, 300);
        window.Show();
    }

    private Vector2 scrollPos;

    private void OnGUI()
    {
        // 1. "Update Scenes" Button
        if (GUILayout.Button("Refetch Scenes", EditorStyles.toolbarButton))
        {
            AssetDatabase.Refresh();
        }

        GUILayout.Space(5);
        GUILayout.Label("Available Scenes:", EditorStyles.boldLabel);

        // 2. Scroll View for the list
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        if (sceneGuids.Length == 0)
        {
            GUILayout.Label("No scenes found in Assets/Scenes");
        }
        else
        {
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(path);

                // Highlight the current scene
                bool isCurrent = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == path;
                string display = isCurrent ? $"[OPEN] {sceneName}" : sceneName;
                
                // Draw the button
                // usage of "GUI.skin.button" makes it look like a standard button
                if (GUILayout.Button(display, EditorStyles.miniButtonLeft)) 
                {
                    OpenScene(path);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
            // Close the window after selection to act like a dropdown
            Close(); 
        }
    }
}