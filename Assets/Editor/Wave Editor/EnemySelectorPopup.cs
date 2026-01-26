using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class EnemySelectorPopup : PopupWindowContent
{
    private readonly EnemyDatabase _database;
    private readonly SerializedProperty _property;
    private Vector2 _scrollPos;
    
    // Layout Constants
    private const float IconSize = 70f; 
    private const float Padding = 5f;
    private const int Columns = 3;

    // Categorization
    private Dictionary<string, List<Enemy>> _categorizedEnemies = new Dictionary<string, List<Enemy>>();
    private readonly string[] _orderedCategories = new string[] 
    { 
        "Pride", "Envy", "Wrath", "Sloth", "Greed", "Gluttony", "Lust", "Depth", "Generic" 
    };

    // Performance Caching
    private Dictionary<Enemy, Texture2D> _previewCache = new Dictionary<Enemy, Texture2D>();
    private bool _previewsLoaded = false;

    public EnemySelectorPopup(EnemyDatabase database, SerializedProperty property)
    {
        _database = database;
        _property = property;
    }

    public override Vector2 GetWindowSize()
    {
        // Width for 3 columns + scrollbar padding
        float width = (IconSize + Padding) * Columns + 30f; 
        return new Vector2(width, 500); // Fixed height, scrollable content
    }

    public override void OnOpen()
    {
        base.OnOpen();
        CategorizeEnemies();
        LoadPreviews();
    }

    private void CategorizeEnemies()
    {
        _categorizedEnemies.Clear();

        if (_database == null || _database.allEnemies == null) return;

        foreach (Enemy enemy in _database.allEnemies)
        {
            if (enemy == null) continue;

            string path = AssetDatabase.GetAssetPath(enemy);
            string category = GetCategoryFromPath(path);

            if (!_categorizedEnemies.ContainsKey(category))
            {
                _categorizedEnemies[category] = new List<Enemy>();
            }
            _categorizedEnemies[category].Add(enemy);
        }
    }

    private string GetCategoryFromPath(string path)
    {
        // path example: "Assets/Prefabs/Enemies/Pride/Lion.prefab"
        // We look for the folder immediately following "Enemies"
        
        string keyword = "/Enemies/";
        int index = path.IndexOf(keyword);

        if (index != -1)
        {
            string subPath = path.Substring(index + keyword.Length);
            // subPath is now "Pride/Lion.prefab" or "Generic/Goblin.prefab"
            
            int slashIndex = subPath.IndexOf('/');
            if (slashIndex != -1)
            {
                return subPath.Substring(0, slashIndex);
            }
        }

        return "Uncategorized";
    }

    private void LoadPreviews()
    {
        if (_database == null || _database.allEnemies.Count == 0) return;

        foreach (Enemy enemy in _database.allEnemies)
        {
            if (enemy != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(enemy.gameObject);
                _previewCache[enemy] = preview;
            }
        }

        if (AssetPreview.IsLoadingAssetPreviews())
        {
            EditorApplication.delayCall += CheckPreviewsLoaded;
        }
        else
        {
            _previewsLoaded = true;
        }
    }

    private void CheckPreviewsLoaded()
    {
        if (!AssetPreview.IsLoadingAssetPreviews())
        {
            _previewsLoaded = true;
            // Update cache one last time
            foreach (Enemy enemy in _database.allEnemies)
            {
                if (enemy != null)
                    _previewCache[enemy] = AssetPreview.GetAssetPreview(enemy.gameObject);
            }
            editorWindow?.Repaint();
        }
        else
        {
            EditorApplication.delayCall += CheckPreviewsLoaded;
        }
    }

    public override void OnGUI(Rect rect)
    {
        GUILayout.Label("Select Enemy", EditorStyles.boldLabel);
        
        if (_database == null || _database.allEnemies.Count == 0)
        {
            GUILayout.Label("No Enemies found in Database.");
            return;
        }

        if (!_previewsLoaded && AssetPreview.IsLoadingAssetPreviews())
        {
            EditorGUILayout.HelpBox("Loading previews...", MessageType.Info);
        }

        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        // 1. Draw Ordered Categories first (Pride, Envy, etc.)
        foreach (string category in _orderedCategories)
        {
            if (_categorizedEnemies.ContainsKey(category))
            {
                DrawCategorySection(category, _categorizedEnemies[category]);
            }
        }

        // 2. Draw any other categories found (e.g. "Uncategorized" or custom folders)
        foreach (var kvp in _categorizedEnemies)
        {
            if (!_orderedCategories.Contains(kvp.Key))
            {
                DrawCategorySection(kvp.Key, kvp.Value);
            }
        }

        GUILayout.EndScrollView();
    }

    private void DrawCategorySection(string header, List<Enemy> enemies)
    {
        if (enemies.Count == 0) return;

        // Draw Header
        GUILayout.Space(10);
        GUILayout.Label(header, EditorStyles.boldLabel);
        GUILayout.Space(2);

        // Draw Grid
        int count = enemies.Count;
        int index = 0;

        while (index < count)
        {
            GUILayout.BeginHorizontal();
            
            for (int i = 0; i < Columns; i++)
            {
                if (index < count)
                {
                    Enemy enemy = enemies[index];
                    DrawEnemyCell(enemy);
                    index++;
                }
                else
                {
                    // Empty spacer to maintain layout alignment
                    GUILayout.Label("", GUILayout.Width(IconSize));
                }

                if (i < Columns - 1) GUILayout.Space(Padding);
            }
            
            GUILayout.EndHorizontal();
            GUILayout.Space(10); // Row spacing
        }
    }

    private void DrawEnemyCell(Enemy enemy)
    {
        GUILayout.BeginVertical(GUILayout.Width(IconSize)); 

        // Get cached preview
        Texture2D preview = null;
        if (_previewCache.TryGetValue(enemy, out Texture2D cached))
        {
            preview = cached;
        }

        GUIContent btnContent = (preview != null) ? new GUIContent(preview, enemy.name) : new GUIContent("...", enemy.name);

        if (GUILayout.Button(btnContent, GUILayout.Width(IconSize), GUILayout.Height(IconSize)))
        {
            SelectEnemy(enemy);
        }

        // Name Label
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.alignment = TextAnchor.UpperCenter;
        labelStyle.wordWrap = true;
        labelStyle.clipping = TextClipping.Clip;

        GUILayout.Label(enemy.name, labelStyle, GUILayout.Width(IconSize));

        GUILayout.EndVertical();
    }

    private void SelectEnemy(Enemy enemy)
    {
        _property.serializedObject.Update();
        _property.objectReferenceValue = enemy;
        _property.serializedObject.ApplyModifiedProperties();
        editorWindow.Close();
    }
}