#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System;
using System.Globalization;
using System.Collections.Generic; // Required for List/HashSet

public class SheetFetcher : EditorWindow
{
    private const string UPGRADES_PATH = "Assets/Resources/Upgrades";
    private const string PREF_URL_KEY = "SheetFetcher_UpgradeURL";

    [MenuItem("Tools/Sheet Fetcher/Open Menu", priority = 0)]
    public static void ShowWindow()
    {
        GetWindow<SheetFetcher>("Sheet Fetcher");
    }

    [MenuItem("Tools/Sheet Fetcher/Sync Upgrades", priority = 1)]
    public static void SyncDirectly()
    {
        string savedURL = EditorPrefs.GetString(PREF_URL_KEY, "");
        if (string.IsNullOrEmpty(savedURL))
        {
            Debug.LogError("No URL found. Please open the menu and paste your link.");
            return;
        }
        RunFetchLogic(savedURL, false);
    }

    private string currentUrl = "";

    private void OnEnable()
    {
        currentUrl = EditorPrefs.GetString(PREF_URL_KEY, "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Data Sync", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Upgrades Database", EditorStyles.boldLabel);
        GUILayout.Label("Order: ID | Name | Desc | Level | Type | Value | IsMult | [Ignored] | Sec | Ter", EditorStyles.miniLabel);

        EditorGUI.BeginChangeCheck();
        currentUrl = EditorGUILayout.TextField("Google Sheet Link:", currentUrl);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PREF_URL_KEY, currentUrl);
        }

        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sync Upgrades", GUILayout.Height(30)))
        {
            RunFetchLogic(currentUrl, false);
        }
        if (GUILayout.Button("Debug (Print Rows)", GUILayout.Height(30)))
        {
            RunFetchLogic(currentUrl, true);
        }
        GUILayout.EndHorizontal();
    }

    private static void RunFetchLogic(string rawUrl, bool debugMode)
    {
        // 1. URL AUTO-FIXER
        string csvUrl = rawUrl;
        if (rawUrl.Contains("/edit"))
        {
            int index = rawUrl.IndexOf("/edit");
            csvUrl = rawUrl.Substring(0, index) + "/export?format=csv";
        }

        string csvData = DownloadCSV(csvUrl);
        if (string.IsNullOrEmpty(csvData)) return;

        // 2. DETECT SEPARATOR
        char separator = ',';
        string[] lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0 && lines[0].Contains(";") && !lines[0].Contains(","))
        {
            separator = ';';
        }

        if (debugMode) Debug.Log($"<color=yellow>Detected Separator:</color> '{separator}'");

        if (!Directory.Exists(UPGRADES_PATH))
        {
            Directory.CreateDirectory(UPGRADES_PATH);
            AssetDatabase.Refresh();
        }

        string pattern = separator + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"; 

        int updatedCount = 0;
        int newCount = 0;
        int deletedCount = 0;

        // TRACKING: Keep track of every valid filename we encounter in the Sheet
        HashSet<string> processedFiles = new HashSet<string>();

        int startRow = debugMode ? 0 : 1;

        for (int i = startRow; i < lines.Length; i++)
        {
            try
            {
                string[] cells = Regex.Split(lines[i], pattern);
                for (int k = 0; k < cells.Length; k++) 
                    cells[k] = cells[k].Trim().Replace("\"", "");

                if (debugMode)
                {
                    string idPreview = cells.Length > 0 ? cells[0] : "null";
                    string namePreview = cells.Length > 1 ? cells[1] : "null";
                    Debug.Log($"Row {i}: ID={idPreview} | Name={namePreview}");
                    if (i > 5) break; 
                    continue;
                }

                if (cells.Length < 7) continue;

                // --- MAPPING ---
                int id = ParseIntSafe(cells[0]);         
                string name = cells[1]; 
                int level = ParseIntSafe(cells[3]);      

                string typeStr = cells[4];
                UpgradeType typeEnum = UpgradeType.Custom;
                try { typeEnum = (UpgradeType)Enum.Parse(typeof(UpgradeType), typeStr, true); }
                catch { Debug.LogWarning($"Row {i}: Unknown Type '{typeStr}'"); }

                float val = ParseFloatSafe(cells[5]);
                bool isMulti = cells[6].ToUpper() == "TRUE" || cells[6] == "1";
                
                // --- FILENAME GENERATION ---
                string cleanName = name.Replace("upgradeName", "")
                                       .Replace("Name", "")
                                       .Replace(" ", "")
                                       .Trim();
                
                string fileName = $"{cleanName}_{id}";
                fileName = Regex.Replace(fileName, "[^a-zA-Z0-9_]", ""); // Sanitize

                if (string.IsNullOrEmpty(fileName)) continue;

                // MARK AS PROCESSED
                processedFiles.Add(fileName);

                float secVal = (cells.Length > 8) ? ParseFloatSafe(cells[8]) : 0f;
                float terVal = (cells.Length > 9) ? ParseFloatSafe(cells[9]) : 0f;

                // --- ASSET HANDLING ---
                string assetPath = $"{UPGRADES_PATH}/{fileName}.asset";
                UpgradeData asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);

                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<UpgradeData>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                    newCount++;
                }
                else
                {
                    updatedCount++;
                }

                SerializedObject so = new SerializedObject(asset);
                so.FindProperty("upgradeID").intValue = id;
                so.FindProperty("upgradeName").stringValue = name;
                so.FindProperty("upgradeLevel").intValue = level;
                so.FindProperty("upgradeType").enumValueIndex = (int)typeEnum;
                so.FindProperty("value").floatValue = val;
                so.FindProperty("isMultiplier").boolValue = isMulti;
                so.FindProperty("identifier").stringValue = fileName; 
                
                var secProp = so.FindProperty("secondaryValue");
                if (secProp != null) secProp.floatValue = secVal;

                var terProp = so.FindProperty("ternaryValue");
                if (terProp != null) terProp.floatValue = terVal;

                so.ApplyModifiedProperties();
                
                if (asset.name != fileName) 
                {
                    AssetDatabase.RenameAsset(assetPath, fileName);
                    AssetDatabase.SaveAssets(); 
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing row {i}: {ex.Message}");
            }
        }

        // --- CLEANUP LOGIC (DELETE OLD FILES) ---
        if (!debugMode)
        {
            string[] allAssetPaths = Directory.GetFiles(UPGRADES_PATH, "*.asset");
            foreach (string filePath in allAssetPaths)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                
                // If the file in the folder was NOT in our CSV list, delete it.
                if (!processedFiles.Contains(fileNameWithoutExt))
                {
                    // Convert system path to Unity path for deletion
                    string unityPath = filePath.Replace("\\", "/");
                    
                    // Double check we aren't deleting something weird (optional safety)
                    if(AssetDatabase.LoadAssetAtPath<UpgradeData>(unityPath) != null)
                    {
                        AssetDatabase.DeleteAsset(unityPath);
                        deletedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Sync Complete!</color> Created: {newCount}, Updated: {updatedCount}, <color=orange>Deleted: {deletedCount}</color>");
        }
    }

    private static string DownloadCSV(string url)
    {
        try
        {
            using (WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                return client.DownloadString(url);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Download Error: {e.Message}");
            return null;
        }
    }

    private static float ParseFloatSafe(string data)
    {
        if (string.IsNullOrEmpty(data)) return 0f;
        if (float.TryParse(data, NumberStyles.Any, CultureInfo.InvariantCulture, out float result)) return result;
        if (float.TryParse(data, NumberStyles.Any, new CultureInfo("tr-TR"), out float resultTR)) return resultTR;
        return 0f;
    }

    private static int ParseIntSafe(string data)
    {
        if (string.IsNullOrEmpty(data)) return 0;
        if (int.TryParse(data, out int result)) return result;
        return 0;
    }
}
#endif