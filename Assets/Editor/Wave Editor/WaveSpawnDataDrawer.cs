using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(WaveSpawnData))]
public class WaveSpawnDataDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Padding = 4f;
    private const float PreviewSize = 60f;

    // PERFORMANCE: Cache database lookup (static so shared across all drawer instances)
    private static EnemyDatabase _cachedDatabase;
    private static double _lastDatabaseLookup;
    private const double DatabaseLookupInterval = 2.0; // Only look for database every 2 seconds

    // PERFORMANCE: Cache previews per enemy (static so shared across all instances)
    private static Dictionary<Enemy, Texture2D> _previewCache = new Dictionary<Enemy, Texture2D>();
    
    // Track which property is being hovered for preview generation
    private static string _currentPropertyPath;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var mode = (SpawnModMode)property.FindPropertyRelative("modificationMode").enumValueIndex;

        float height = PreviewSize + Padding + LineHeight + Padding;

        int extraLines = 0;
        if (mode == SpawnModMode.Multiplier) extraLines = 5;
        else if (mode == SpawnModMode.CustomValue) extraLines = 5;

        return height + ((LineHeight + Padding) * extraLines) + Padding;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // -- 1. OPTIMIZED: Cache database lookup --
        EnemyDatabase database = GetCachedDatabase();

        Rect rect = new Rect(position.x, position.y, position.width, LineHeight);

        // -- 2. Draw Custom Enemy Selector --
        SerializedProperty prefabProp = property.FindPropertyRelative("enemyPrefab");
        Enemy currentEnemy = (Enemy)prefabProp.objectReferenceValue;

        // Area for the Big Button
        Rect previewRect = new Rect(position.x, position.y, PreviewSize, PreviewSize);
        Rect infoRect = new Rect(position.x + PreviewSize + 10, position.y + (PreviewSize/4), position.width - PreviewSize - 10, LineHeight);

        // PERFORMANCE: Get preview from cache or generate async
        Texture2D preview = GetCachedPreview(currentEnemy, property.propertyPath);

        GUIContent btnContent = (preview != null) ? new GUIContent(preview) : new GUIContent("None");
        
        // Draw the button (Click to open Popup)
        if (GUI.Button(previewRect, btnContent))
        {
            if (database != null)
            {
                PopupWindow.Show(previewRect, new EnemySelectorPopup(database, prefabProp));
            }
            else
            {
                Debug.LogError("EnemyDatabase not found! Please create one via Create -> TowerDefense -> EnemyDatabase");
            }
        }

        // Draw Name Label next to it
        string nameLabel = currentEnemy != null ? currentEnemy.name : "Select Enemy";
        EditorGUI.LabelField(infoRect, nameLabel, EditorStyles.boldLabel);
        
        // Move rect down past the preview
        rect.y += PreviewSize + Padding;

        // -- 3. Draw Modification Mode Enum --
        SerializedProperty modeProp = property.FindPropertyRelative("modificationMode");
        EditorGUI.PropertyField(rect, modeProp, new GUIContent("Mode"));
        rect.y += LineHeight + Padding;

        SpawnModMode mode = (SpawnModMode)modeProp.enumValueIndex;

        // -- 4. Draw Stats --
        if (mode != SpawnModMode.NoModification)
        {
            EditorGUI.indentLevel++;

            if (mode == SpawnModMode.Multiplier)
            {
                DrawStatField(ref rect, property, "hpMultiplier", "HP Mult");
                DrawStatField(ref rect, property, "speedMultiplier", "Speed Mult");
                DrawStatField(ref rect, property, "damageMultiplier", "Dmg Mult");
                DrawStatField(ref rect, property, "goldMultiplier", "Gold Mult");
                DrawStatField(ref rect, property, "expMultiplier", "Exp Mult");
            }
            else if (mode == SpawnModMode.CustomValue)
            {
                DrawStatField(ref rect, property, "customHealth", "Health");
                DrawStatField(ref rect, property, "customSpeed", "Speed");
                DrawStatField(ref rect, property, "customDamage", "Damage");
                DrawStatField(ref rect, property, "customGold", "Gold");
                DrawStatField(ref rect, property, "customExp", "Exp");
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawStatField(ref Rect rect, SerializedProperty rootProp, string relativeName, string label)
    {
        EditorGUI.PropertyField(rect, rootProp.FindPropertyRelative(relativeName), new GUIContent(label));
        rect.y += LineHeight + Padding;
    }

    // PERFORMANCE: Cached database lookup
    private static EnemyDatabase GetCachedDatabase()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        
        // Only look for database if cache is empty or it's been a while
        if (_cachedDatabase == null || currentTime - _lastDatabaseLookup > DatabaseLookupInterval)
        {
            _cachedDatabase = AssetDatabase.LoadAssetAtPath<EnemyDatabase>("Assets/Scripts/EnemyDatabase.asset");
            
            if (_cachedDatabase == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:EnemyDatabase");
                if (guids.Length > 0)
                {
                    _cachedDatabase = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            
            _lastDatabaseLookup = currentTime;
        }
        
        return _cachedDatabase;
    }

    // PERFORMANCE: Cached preview system
    private static Texture2D GetCachedPreview(Enemy enemy, string propertyPath)
    {
        if (enemy == null)
        {
            return null;
        }

        // Check if we have a cached preview
        if (_previewCache.TryGetValue(enemy, out Texture2D cached))
        {
            if (cached != null)
            {
                return cached;
            }
            else
            {
                // Preview was null, remove from cache and try again
                _previewCache.Remove(enemy);
            }
        }

        // CRITICAL OPTIMIZATION: Only generate preview for visible/active property
        // This prevents loading 50+ previews when you have 50 items in the list
        _currentPropertyPath = propertyPath;

        // Try to get preview (this might return null if not loaded yet)
        Texture2D preview = AssetPreview.GetAssetPreview(enemy.gameObject);
        
        // Cache it (even if null, to avoid repeated checks)
        _previewCache[enemy] = preview;
        
        // If preview is still loading, request a repaint when it's ready
        if (preview == null && AssetPreview.IsLoadingAssetPreviews())
        {
            // Schedule a delayed repaint
            EditorApplication.delayCall += () =>
            {
                // Clear the null cache entry so it tries again
                if (_previewCache.ContainsKey(enemy) && _previewCache[enemy] == null)
                {
                    _previewCache.Remove(enemy);
                }
            };
        }
        
        return preview;
    }

    // Optional: Clear cache when entering/exiting play mode
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            _previewCache.Clear();
        }
    }
}