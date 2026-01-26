using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EnemySelectorPopup : PopupWindowContent
{
    private readonly EnemyDatabase _database;
    private readonly SerializedProperty _property;
    private Vector2 _scrollPos;
    
    // Constants for layout
    private const float IconSize = 70f; 
    private const float Padding = 5f;
    private const int Columns = 3;

    // PERFORMANCE: Cache all previews on open
    private Dictionary<Enemy, Texture2D> _previewCache = new Dictionary<Enemy, Texture2D>();
    private bool _previewsLoaded = false;

    public EnemySelectorPopup(EnemyDatabase database, SerializedProperty property)
    {
        _database = database;
        _property = property;
    }

    public override Vector2 GetWindowSize()
    {
        float width = (IconSize + Padding) * Columns + 25f;
        return new Vector2(width, 450); 
    }

    public override void OnOpen()
    {
        base.OnOpen();
        // PERFORMANCE: Preload all previews asynchronously
        LoadPreviews();
    }

    private void LoadPreviews()
    {
        if (_database == null || _database.allEnemies.Count == 0)
            return;

        // Start loading previews in the background
        foreach (Enemy enemy in _database.allEnemies)
        {
            if (enemy != null)
            {
                // This triggers async loading
                Texture2D preview = AssetPreview.GetAssetPreview(enemy.gameObject);
                _previewCache[enemy] = preview;
            }
        }

        // Schedule updates while previews are loading
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
            
            // Refresh cache with loaded previews
            foreach (Enemy enemy in _database.allEnemies)
            {
                if (enemy != null)
                {
                    _previewCache[enemy] = AssetPreview.GetAssetPreview(enemy.gameObject);
                }
            }
            
            editorWindow?.Repaint();
        }
        else
        {
            // Keep checking
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

        // Show loading indicator if previews aren't ready
        if (!_previewsLoaded && AssetPreview.IsLoadingAssetPreviews())
        {
            EditorGUILayout.HelpBox("Loading previews...", MessageType.Info);
        }

        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        int index = 0;
        while (index < _database.allEnemies.Count)
        {
            GUILayout.BeginHorizontal();
            
            for (int i = 0; i < Columns; i++)
            {
                if (index >= _database.allEnemies.Count) 
                {
                    GUILayout.Label("", GUILayout.Width(IconSize));
                }
                else
                {
                    Enemy enemy = _database.allEnemies[index];
                    if (enemy != null)
                    {
                        DrawEnemyCell(enemy);
                    }
                    index++;
                }
                
                if (i < Columns - 1) GUILayout.Space(Padding);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();
    }

    private void DrawEnemyCell(Enemy enemy)
    {
        GUILayout.BeginVertical(GUILayout.Width(IconSize)); 

        // PERFORMANCE: Use cached preview
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

        // Name label
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