#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class LocalizationSync : EditorWindow
{
    private string sheetURL = "";
    private const string PREF_KEY = "LocalizationSheetURL";

    [MenuItem("Tools/Localization/Open Downloader")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationSync>("Localization Sync");
    }

    private void OnEnable()
    {
        sheetURL = EditorPrefs.GetString(PREF_KEY, "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Localization -> ScriptableObject", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("Google Sheet CSV Link:", EditorStyles.label);
        sheetURL = EditorGUILayout.TextField(sheetURL);
        GUILayout.Space(10);

        if (GUILayout.Button("Fetch & Bake Data", GUILayout.Height(40)))
        {
            if (string.IsNullOrEmpty(sheetURL)) return;
            EditorPrefs.SetString(PREF_KEY, sheetURL);
            DownloadAndBake();
        }
    }

    private void DownloadAndBake()
    {
        // 1. Download
        string csvData = "";
        try {
            using (System.Net.WebClient client = new System.Net.WebClient()) {
                client.Encoding = System.Text.Encoding.UTF8;
                csvData = client.DownloadString(sheetURL);
            }
        } catch (System.Exception e) {
            Debug.LogError("Download failed: " + e.Message);
            return;
        }

        // 2. Find or Create the Asset
        string assetPath = "Assets/Resources/LocalizationData.asset";
        LocalizationData dataAsset = AssetDatabase.LoadAssetAtPath<LocalizationData>(assetPath);
        
        if (dataAsset == null)
        {
            // Create folder if missing
            if (!System.IO.Directory.Exists(Application.dataPath + "/Resources"))
                System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
                
            dataAsset = ScriptableObject.CreateInstance<LocalizationData>();
            AssetDatabase.CreateAsset(dataAsset, assetPath);
        }

        // 3. Parse CSV and Fill SO
        // Clear old data
        dataAsset.languageCodes.Clear();
        dataAsset.entries.Clear();

        string[] lines = csvData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return;

        // Regex for CSV parsing (handles commas inside quotes)
        string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";
        
        // --- Parse Headers ---
        string[] headers = Regex.Split(lines[0], pattern);
        // Start at 1 to skip "key"
        for (int i = 1; i < headers.Length; i++) 
        {
            dataAsset.languageCodes.Add(headers[i].Trim().Replace("\"", ""));
        }

        // --- Parse Rows ---
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = Regex.Split(lines[i], pattern);
            if (cells.Length < headers.Length) continue;

            LocalizationData.LocalizationEntry newEntry = new LocalizationData.LocalizationEntry();
            newEntry.key = cells[0].Trim().Replace("\"", "");
            newEntry.values = new List<string>();

            // Loop through language columns
            for (int j = 1; j < headers.Length; j++)
            {
                string content = cells[j].Trim();
                if (content.StartsWith("\"") && content.EndsWith("\""))
                    content = content.Substring(1, content.Length - 2).Replace("\"\"", "\"");
                
                newEntry.values.Add(content);
            }

            dataAsset.entries.Add(newEntry);
        }

        // 4. Save Changes
        EditorUtility.SetDirty(dataAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>Successfully baked {dataAsset.entries.Count} keys into ScriptableObject!</color>");
    }
}
#endif