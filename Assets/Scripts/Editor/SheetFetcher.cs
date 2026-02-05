#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System;
using System.Globalization;
using System.Collections.Generic;

public class SheetFetcher : EditorWindow
{
    // --- PATHS ---
    private const string UPGRADES_PATH = "Assets/Resources/Upgrades";
    private const string CHARACTERS_PATH = "Assets/Resources/Characters";

    // --- PREFS KEYS ---
    private const string PREF_UPGRADE_URL_KEY = "SheetFetcher_UpgradeURL";
    private const string PREF_CHAR_URL_KEY = "SheetFetcher_CharURL";
    private const string PREF_TOWER_URL_KEY = "SheetFetcher_TowerURL";

    // --- VARIABLES ---
    private string upgradeUrl = "";
    private string characterUrl = "";
    private string towerUrl = "";

    [MenuItem("Tools/Sheet Fetcher/Open Menu", priority = 0)]
    public static void ShowWindow()
    {
        GetWindow<SheetFetcher>("Sheet Fetcher");
    }

    [MenuItem("Tools/Sheet Fetcher/Sync Upgrades", priority = 1)]
    public static void SyncUpgradesDirectly()
    {
        string savedURL = EditorPrefs.GetString(PREF_UPGRADE_URL_KEY, "");
        if (ValidateUrl(savedURL)) SyncUpgradesLogic(savedURL, false);
    }

    [MenuItem("Tools/Sheet Fetcher/Sync Characters", priority = 2)]
    public static void SyncCharactersDirectly()
    {
        string savedURL = EditorPrefs.GetString(PREF_CHAR_URL_KEY, "");
        if (ValidateUrl(savedURL)) SyncCharactersLogic(savedURL, false);
    }

    [MenuItem("Tools/Sheet Fetcher/Sync Towers", priority = 3)]
    public static void SyncTowersDirectly()
    {
        string savedURL = EditorPrefs.GetString(PREF_TOWER_URL_KEY, "");
        if (ValidateUrl(savedURL)) SyncTowersLogic(savedURL, false);
    }

    private void OnEnable()
    {
        upgradeUrl = EditorPrefs.GetString(PREF_UPGRADE_URL_KEY, "");
        characterUrl = EditorPrefs.GetString(PREF_CHAR_URL_KEY, "");
        towerUrl = EditorPrefs.GetString(PREF_TOWER_URL_KEY, "");
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Data Sync", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- UPGRADES SECTION ---
        DrawSection("Upgrades Database", ref upgradeUrl, PREF_UPGRADE_URL_KEY,
            () => SyncUpgradesLogic(upgradeUrl, false),
            () => SyncUpgradesLogic(upgradeUrl, true));

        GUILayout.Space(15);
        DrawUILine(Color.gray);
        GUILayout.Space(15);

        // --- CHARACTERS SECTION ---
        DrawSection("Characters Database", ref characterUrl, PREF_CHAR_URL_KEY,
            () => SyncCharactersLogic(characterUrl, false),
            () => SyncCharactersLogic(characterUrl, true));

        GUILayout.Space(15);
        DrawUILine(Color.gray);
        GUILayout.Space(15);

        // --- TOWERS SECTION ---
        DrawSection("Towers Database", ref towerUrl, PREF_TOWER_URL_KEY,
            () => SyncTowersLogic(towerUrl, false),
            () => SyncTowersLogic(towerUrl, true));
    }

    // Helper to draw UI sections to keep OnGUI clean
    private void DrawSection(string title, ref string urlVar, string prefKey, Action onSync, Action onDebug)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        urlVar = EditorGUILayout.TextField("CSV Link:", urlVar);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(prefKey, urlVar);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"Sync {title.Split(' ')[0]}", GUILayout.Height(30))) onSync?.Invoke();
        if (GUILayout.Button("Debug (Print Rows)", GUILayout.Height(30))) onDebug?.Invoke();
        GUILayout.EndHorizontal();
    }

    // =================================================================================================
    // LOGIC: CHARACTERS
    // =================================================================================================
    private static void SyncCharactersLogic(string rawUrl, bool debugMode)
    {
        FetchAndProcess(rawUrl, debugMode, CHARACTERS_PATH, (cells, processedFiles, newCount, updatedCount) =>
        {
            if (cells.Length < 7) return false;

            string idStr = cells[0];
            MasterDictionary.Characters charEnum;
            try
            {
                charEnum = (MasterDictionary.Characters)Enum.Parse(typeof(MasterDictionary.Characters), idStr, true);
            }
            catch
            {
                Debug.LogWarning($"Unknown Character Enum: {idStr}");
                return false;
            }

            string fileName = $"Character_{idStr}";
            processedFiles.Add(fileName);

            string assetPath = $"{CHARACTERS_PATH}/{fileName}.asset";
            CharacterData asset = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
            bool isNew = false;

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(asset, assetPath);
                isNew = true;
            }

            SerializedObject so = new SerializedObject(asset);
            SetProp(so, "id", (int)charEnum);
            SetProp(so, "characterName", cells[1]);
            SetProp(so, "fullName", cells[2]);
            SetProp(so, "desc", cells[3]);
            SetProp(so, "damage", ParseIntSafe(cells[4]));
            SetProp(so, "attackSpeed", ParseIntSafe(cells[5]));
            SetProp(so, "movementSpeed", ParseIntSafe(cells[6]));

            so.ApplyModifiedProperties();

            if (asset.name != fileName)
            {
                AssetDatabase.RenameAsset(assetPath, fileName);
                AssetDatabase.SaveAssets();
            }

            if (isNew) newCount.Value++; else updatedCount.Value++;
            return true;
        });
    }

    // =================================================================================================
    // LOGIC: UPGRADES
    // =================================================================================================
    private static void SyncUpgradesLogic(string rawUrl, bool debugMode)
    {
        FetchAndProcess(rawUrl, debugMode, UPGRADES_PATH, (cells, processedFiles, newCount, updatedCount) =>
        {
            if (cells.Length < 7) return false;

            int id = ParseIntSafe(cells[0]);
            string name = cells[1];
            string cleanName = name.Replace("upgradeName", "").Replace("Name", "").Replace(" ", "").Trim();
            string fileName = $"{cleanName}_{id}";
            fileName = Regex.Replace(fileName, "[^a-zA-Z0-9_]", "");
            if (string.IsNullOrEmpty(fileName)) return false;

            processedFiles.Add(fileName);

            string assetPath = $"{UPGRADES_PATH}/{fileName}.asset";
            UpgradeData asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);
            bool isNew = false;

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
                isNew = true;
            }

            int level = ParseIntSafe(cells[3]);
            string typeStr = cells[4];
            UpgradeType typeEnum = UpgradeType.Custom;
            try { typeEnum = (UpgradeType)Enum.Parse(typeof(UpgradeType), typeStr, true); } catch { }
            float val = ParseFloatSafe(cells[5]);
            bool isMulti = cells[6].ToUpper() == "TRUE" || cells[6] == "1";
            float secVal = (cells.Length > 8) ? ParseFloatSafe(cells[8]) : 0f;
            float terVal = (cells.Length > 9) ? ParseFloatSafe(cells[9]) : 0f;

            SerializedObject so = new SerializedObject(asset);
            SetProp(so, "upgradeID", id);
            SetProp(so, "upgradeName", name);
            SetProp(so, "upgradeLevel", level);
            SetProp(so, "upgradeType", (int)typeEnum);
            SetProp(so, "value", val);
            SetProp(so, "isMultiplier", isMulti);
            SetProp(so, "identifier", fileName);
            SetProp(so, "secondaryValue", secVal);
            SetProp(so, "ternaryValue", terVal);

            so.ApplyModifiedProperties();

            if (asset.name != fileName)
            {
                AssetDatabase.RenameAsset(assetPath, fileName);
                AssetDatabase.SaveAssets();
            }

            if (isNew) newCount.Value++; else updatedCount.Value++;
            return true;
        });
    }

    // =================================================================================================
    // LOGIC: TOWERS (FIXED COLUMN MAPPING)
    // =================================================================================================
    private static void SyncTowersLogic(string rawUrl, bool debugMode)
    {
        ReferencesSO refs = Refs.R;
        if (refs == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ReferencesSO");
            if (guids.Length > 0)
                refs = AssetDatabase.LoadAssetAtPath<ReferencesSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (refs == null || refs.TowerReferences == null)
        {
            Debug.LogError("Could not find 'ReferencesSO'.");
            return;
        }

        int listSize = refs.TowerReferences.Length;

        // MAPPING BASED ON SCREENSHOT:
        // Col A [0] = Name
        // Col B [1] = ID
        // Col C [2] = Animation
        // Col D [3] = Tier
        // Col E [4] = mergeContent (SKIP)
        // Col F [5] = targetTag
        // Col G [6] = Range
        // Col H [7] = Interval
        // Col I [8] = Damage
        // Col J [9] = Bullet Speed
        // Col K [10] = Bullet Count
        // Col L [11] = Spread
        // Col M [12] = Bullet Health
        // Col N [13] = Is Spinning
        // Col O [14] = Is AOE

        FetchAndProcess(rawUrl, debugMode, "Assets", (cells, processedFiles, newCount, updatedCount) =>
        {
            if (cells.Length < 10) return false;

            // 1. Get ID from Col B (index 1)
            int sheetID = ParseIntSafe(cells[1]);

            if (sheetID < 0 || sheetID >= listSize)
            {
                Debug.LogWarning($"Skipping Row: ID {sheetID} is out of bounds (List Size: {listSize}).");
                return false;
            }

            TowerReference reference = refs.TowerReferences[sheetID];
            if (reference == null || reference.prefab == null) return false;

            GameObject towerPrefab = reference.prefab;
            TowerGeneric towerScript = towerPrefab.GetComponent<TowerGeneric>();

            if (towerScript == null) return false;

            SerializedObject so = new SerializedObject(towerScript);

            // 2. Map Properties
            SetProp(so, "id", sheetID);
            SetProp(so, "towerName", cells[0]);        // Name is Col 0
            SetProp(so, "animationInit", cells[2]);    // Anim is Col 2
            SetProp(so, "tier", ParseIntSafe(cells[3])); // Tier is Col 3
            
            // Skip cells[4] (MergeContent)

            // Tag is Col 5
            string tagStr = cells[5];
            if (string.IsNullOrEmpty(tagStr)) tagStr = "Enemy";
            FireMethods.TargetTag tagEnum = FireMethods.TargetTag.Enemy;
            try { tagEnum = (FireMethods.TargetTag)Enum.Parse(typeof(FireMethods.TargetTag), tagStr, true); } catch { }
            SetProp(so, "targetTagDefault", (int)tagEnum);

            SetProp(so, "attackRangeDefault", ParseFloatSafe(cells[6]));
            SetProp(so, "attackIntervalDefault", ParseFloatSafe(cells[7]));
            SetProp(so, "attackDamageDefault", ParseFloatSafe(cells[8]));

            SetProp(so, "bulletSpeed", ParseFloatSafe(cells[9]));
            SetProp(so, "bulletCount", ParseIntSafe(cells[10]));
            SetProp(so, "spreadAngle", ParseFloatSafe(cells[11]));
            SetProp(so, "bulletHealth", ParseIntSafe(cells[12]));

            bool spin = (cells.Length > 13) && (cells[13].ToUpper() == "TRUE" || cells[13] == "1");
            SetProp(so, "bulletIsSpinning", spin);

            bool isAOE = (cells.Length > 14) && (cells[14].ToUpper() == "TRUE" || cells[14] == "1");
            SetProp(so, "isAOEBullet", isAOE);

            if (so.ApplyModifiedProperties())
            {
                updatedCount.Value++;
                EditorUtility.SetDirty(towerPrefab);
                Debug.Log($"<color=green>Updated:</color> [{sheetID}] {towerPrefab.name}");
            }
            
            return true;
        }, skipDeletePhase: true); 
    }

    // =================================================================================================
    // CORE PROCESSING ENGINE
    // =================================================================================================
    private class RefInt { public int Value; }

    private static void FetchAndProcess(string rawUrl, bool debugMode, string savePath,
        Func<string[], HashSet<string>, RefInt, RefInt, bool> rowProcessor, bool skipDeletePhase = false)
    {
        string csvUrl = rawUrl;
        if (rawUrl.Contains("/edit"))
        {
            int index = rawUrl.IndexOf("/edit");
            csvUrl = rawUrl.Substring(0, index) + "/export?format=csv";
        }

        string csvData = DownloadCSV(csvUrl);
        if (string.IsNullOrEmpty(csvData)) return;

        char separator = ',';
        string[] lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0 && lines[0].Contains(";") && !lines[0].Contains(",")) separator = ';';

        if (debugMode) Debug.Log($"<color=yellow>Detected Separator:</color> '{separator}'");

        if (!skipDeletePhase && !Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        string pattern = separator + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";
        HashSet<string> processedFiles = new HashSet<string>();
        RefInt newCount = new RefInt();
        RefInt updatedCount = new RefInt();
        int deletedCount = 0;

        int startRow = debugMode ? 0 : 1; 

        for (int i = startRow; i < lines.Length; i++)
        {
            try
            {
                string[] cells = Regex.Split(lines[i], pattern);
                for (int k = 0; k < cells.Length; k++) cells[k] = cells[k].Trim().Replace("\"", "");

                if (debugMode)
                {
                    Debug.Log($"Row {i}: {string.Join(" | ", cells)}");
                    if (i > 5) break;
                    continue;
                }

                rowProcessor(cells, processedFiles, newCount, updatedCount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing row {i}: {ex.Message}");
            }
        }

        if (!debugMode && !skipDeletePhase)
        {
            string[] allAssetPaths = Directory.GetFiles(savePath, "*.asset");
            foreach (string filePath in allAssetPaths)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                if (!processedFiles.Contains(fileNameWithoutExt))
                {
                    string unityPath = filePath.Replace("\\", "/");
                    if (AssetDatabase.LoadMainAssetAtPath(unityPath) != null)
                    {
                        AssetDatabase.DeleteAsset(unityPath);
                        deletedCount++;
                    }
                }
            }
        }

        if (!debugMode)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Sync Complete!</color> Updated: {updatedCount.Value}");
        }
    }

    // =================================================================================================
    // UTILITIES
    // =================================================================================================

    private static void SetProp(SerializedObject so, string propName, object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null) return; 

        if (value is int i)
        {
            if (prop.propertyType == SerializedPropertyType.Enum) prop.enumValueIndex = i;
            else prop.intValue = i;
        }
        else if (value is float f) prop.floatValue = f;
        else if (value is string s) prop.stringValue = s;
        else if (value is bool b) prop.boolValue = b;
    }

    private static bool ValidateUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("URL is empty.");
            return false;
        }
        return true;
    }

    private static void DrawUILine(Color color, int thickness = 1, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
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