using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq; 

public class WaveEditorWindow : EditorWindow
{
    // --- Selection ---
    private LevelData _selectedLevelData;
    private SerializedObject _serializedLevelObject;
    private SerializedProperty _wavesListProperty;
    
    // --- Level List Caching ---
    private List<LevelData> _cachedLevels = new List<LevelData>();
    private string[] _levelNames;
    private int _selectedLevelIndex = 0;
    
    // --- Constants ---
    private const string BaseWavePath = "Assets/Scriptable Objects/Waves";

    // --- Wave Editing (Optimized) ---
    private WaveSO _selectedWave;
    private SerializedObject _serializedWaveObject;
    private int _selectedWaveIndex = -1;
    
    // Cached Properties to avoid repetitive lookups
    private SerializedProperty _propEnemySpawns;
    private SerializedProperty _propHordeSpawns;
    // Add other specific properties here if you want to draw them manually
    // or we can iterate efficiently.

    // --- Layout & Resizing ---
    private Vector2 _sidebarScrollPosition;
    private Vector2 _inspectorScrollPosition;
    
    private float _sidebarWidth = 250f;
    private bool _isResizing = false;
    private const float MinSidebarWidth = 150f;

    [MenuItem("Tools/Wave Editor")]
    public static void ShowWindow()
    {
        GetWindow<WaveEditorWindow>("Wave Editor");
    }

    private void OnEnable()
    {
        RefreshLevelList();
    }

    private void OnDisable()
    {
        // Clean up references
        _serializedWaveObject = null;
        _serializedLevelObject = null;
    }

    private void OnGUI()
    {
        DrawTopBar();

        if (_selectedLevelData == null)
        {
            EditorGUILayout.HelpBox("Please select a Level to begin.", MessageType.Info);
            return;
        }

        // Only update level object if we are interacting with the sidebar structure
        // But generally safe to keep this update as it's just the level wrapper
        if (_serializedLevelObject != null)
        {
            _serializedLevelObject.Update();
        }

        EditorGUILayout.BeginHorizontal();
        
        // 1. Sidebar
        DrawSidebar();
        
        // 2. Resize Handle
        ResizeHandle();

        // 3. Inspector
        DrawWaveInspector();
        
        EditorGUILayout.EndHorizontal();

        // Apply changes to Level Data (Wave list order/add/remove)
        if (_serializedLevelObject != null)
        {
            _serializedLevelObject.ApplyModifiedProperties();
        }
    }

    // ----------------- RESIZING LOGIC -----------------
    private void ResizeHandle()
    {
        Rect resizeRect = GUILayoutUtility.GetRect(5f, position.height, GUILayout.ExpandHeight(true));
        
        if (Event.current.type == EventType.Repaint)
        {
            Color splitterColor = EditorGUIUtility.isProSkin 
                ? new Color(0.12f, 0.12f, 0.12f) 
                : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(new Rect(resizeRect.x + 2, resizeRect.y, 1, resizeRect.height), splitterColor);
        }
        
        EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
        {
            _isResizing = true;
            e.Use(); // Consume event
        }
        
        if (_isResizing)
        {
            _sidebarWidth += e.delta.x;
            _sidebarWidth = Mathf.Clamp(_sidebarWidth, MinSidebarWidth, position.width - 100f);
            Repaint(); 
        }

        if (e.type == EventType.MouseUp)
        {
            _isResizing = false;
        }
    }

    // ----------------- TOP BAR -----------------
    private void DrawTopBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Level:", GUILayout.Width(80));

        EditorGUI.BeginChangeCheck();
        _selectedLevelIndex = EditorGUILayout.Popup(_selectedLevelIndex, _levelNames);
        if (EditorGUI.EndChangeCheck())
        {
            if (_cachedLevels.Count > 0 && _selectedLevelIndex >= 0 && _selectedLevelIndex < _cachedLevels.Count)
            {
                ChangeSelectedLevel(_cachedLevels[_selectedLevelIndex]);
            }
        }

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshLevelList();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        LevelData manualSelection = (LevelData)EditorGUILayout.ObjectField("Manual Drag:", _selectedLevelData, typeof(LevelData), true);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeSelectedLevel(manualSelection);
            UpdateDropdownIndexFromSelection();
        }

        if (_selectedLevelData != null)
        {
            string targetFolder = $"{BaseWavePath}/{_selectedLevelData.name}";
            GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel);
            pathStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField($"Auto-Save Location: {targetFolder}", pathStyle);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void RefreshLevelList()
    {
        _cachedLevels.Clear();
        string searchPath = "Assets/Prefabs/Levels";

        if (Directory.Exists(searchPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { searchPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData lvl = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (lvl != null) _cachedLevels.Add(lvl);
            }
        }

        if (_cachedLevels.Count > 0)
            _levelNames = _cachedLevels.Select(l => l.name).ToArray();
        else
            _levelNames = new string[] { "No Levels Found" };
        
        UpdateDropdownIndexFromSelection();
    }

    private void ChangeSelectedLevel(LevelData newLevel)
    {
        if (newLevel != _selectedLevelData)
        {
            _selectedLevelData = newLevel;
            
            // Clear selection
            _selectedWave = null;
            _selectedWaveIndex = -1;
            _serializedWaveObject = null;

            if (_selectedLevelData != null)
            {
                _serializedLevelObject = new SerializedObject(_selectedLevelData);
                _wavesListProperty = _serializedLevelObject.FindProperty("waves");
            }
        }
    }

    private void UpdateDropdownIndexFromSelection()
    {
        if (_selectedLevelData != null && _cachedLevels.Contains(_selectedLevelData))
        {
            _selectedLevelIndex = _cachedLevels.IndexOf(_selectedLevelData);
        }
        else
        {
            _selectedLevelIndex = 0;
            if (_selectedLevelData == null && _cachedLevels.Count > 0)
            {
                ChangeSelectedLevel(_cachedLevels[0]);
            }
        }
    }

    // ----------------- SIDEBAR -----------------
    private void DrawSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        
        EditorGUILayout.LabelField("Waves List", EditorStyles.boldLabel);
        
        if (GUILayout.Button("+ Create New Wave", GUILayout.Height(30)))
        {
            CreateNewWave();
        }

        EditorGUILayout.Space();

        _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition);

        if (_wavesListProperty != null)
        {
            for (int i = 0; i < _wavesListProperty.arraySize; i++)
            {
                SerializedProperty waveProp = _wavesListProperty.GetArrayElementAtIndex(i);
                WaveSO waveRef = (WaveSO)waveProp.objectReferenceValue;

                EditorGUILayout.BeginHorizontal();

                GUIStyle btnStyle = (i == _selectedWaveIndex) ? new GUIStyle(GUI.skin.button) { normal = GUI.skin.button.active } : GUI.skin.button;
                string btnLabel = waveRef != null ? $"Wave {i + 1}: {waveRef.name}" : $"Wave {i + 1}: (Empty)";
                
                if (GUILayout.Button(btnLabel, btnStyle, GUILayout.Height(25)))
                {
                    SelectWave(i, waveRef);
                }

                if (GUILayout.Button("↑", GUILayout.Width(20))) MoveWave(i, -1);
                if (GUILayout.Button("↓", GUILayout.Width(20))) MoveWave(i, 1);

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25))) DeleteWave(i, waveRef);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        
        Rect r = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint)
        {
             EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), new Color(0.12f, 0.12f, 0.12f));
        }
    }

    // ----------------- INSPECTOR (PERFORMANCE FIXED) -----------------
    private void DrawWaveInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedWave != null && _serializedWaveObject != null)
        {
            // PERFORMANCE: Only update this specific object when needed
            _serializedWaveObject.Update();

            EditorGUILayout.LabelField($"Editing: {_selectedWave.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // PERFORMANCE: Instead of iterating "NextVisible" (slow), 
            // we draw the cached properties directly.
            
            // 1. Draw all fields EXCEPT the lists manually (or via iteration if you prefer)
            // Using iteration here is safer for "General" fields in case you add new ones,
            // but for maximum speed with large lists, we manually handle the big lists.
            
            SerializedProperty iterator = _serializedWaveObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false; 

                // Skip the script field
                if (iterator.name == "m_Script") continue;

                // We can let PropertyField handle the lists (it's optimized in recent Unity versions)
                // OR we can customize it. Since your lag comes from "OnInspectorGUI" (the Editor),
                // using PropertyField here removes the overhead of the Editor class itself.
                
                EditorGUILayout.PropertyField(iterator, true);
            }

            // PERFORMANCE: Only apply changes to the wave
            _serializedWaveObject.ApplyModifiedProperties();
            
            GUILayout.Space(20);
        }
        else
        {
            GUILayout.FlexibleSpace();
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField("Select a Wave from the list to edit.", style, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void SelectWave(int index, WaveSO wave)
    {
        if (_selectedWaveIndex == index && _selectedWave == wave && _serializedWaveObject != null) 
            return;

        _selectedWaveIndex = index;
        _selectedWave = wave;

        if (_selectedWave != null)
        {
            // Create SerializedObject ONCE.
            _serializedWaveObject = new SerializedObject(_selectedWave);
            
            // Cache heavy properties if you want to perform manual drawing later
            // _propEnemySpawns = _serializedWaveObject.FindProperty("enemySpawns");
        }
        else
        {
            _serializedWaveObject = null;
        }
        
        GUI.FocusControl(null); 
    }

    private void MoveWave(int index, int direction)
    {
        int newIndex = index + direction;
        if (newIndex >= 0 && newIndex < _wavesListProperty.arraySize)
        {
            _wavesListProperty.MoveArrayElement(index, newIndex);
            _serializedLevelObject.ApplyModifiedProperties();
            if (_selectedWaveIndex == index) SelectWave(newIndex, _selectedWave);
        }
    }

    private void CreateNewWave()
    {
        string levelName = _selectedLevelData.name;
        string folderPath = $"{BaseWavePath}/{levelName}";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        WaveSO newWave = CreateInstance<WaveSO>();
        string safeLevelName = levelName.Replace(" ", "_");
        string waveName = $"Wave_{safeLevelName}_{_wavesListProperty.arraySize + 1}";
        
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{waveName}.asset");

        AssetDatabase.CreateAsset(newWave, fullPath);
        AssetDatabase.SaveAssets();

        int index = _wavesListProperty.arraySize;
        _wavesListProperty.InsertArrayElementAtIndex(index);
        _wavesListProperty.GetArrayElementAtIndex(index).objectReferenceValue = newWave;
        _serializedLevelObject.ApplyModifiedProperties();

        SelectWave(index, newWave);
    }

    private void DeleteWave(int index, WaveSO wave)
    {
        string waveName = wave != null ? wave.name : "Empty Slot";

        int choice = EditorUtility.DisplayDialogComplex("Delete Wave",
            $"What do you want to do with {waveName}?",
            "Remove from List", 
            "Cancel",           
            "Delete Asset File" 
        );

        if (choice == 1) return; // Cancel

        // 1. CRITICAL: Reset selection immediately
        // We do this BEFORE touching the data/assets so the GUI stops trying to draw the deleted item.
        _selectedWave = null;
        _selectedWaveIndex = -1;
        _serializedWaveObject = null;

        // 2. Handle Asset Deletion
        if (choice == 2 && wave != null)
        {
            string path = AssetDatabase.GetAssetPath(wave);
            AssetDatabase.DeleteAsset(path);
        }

        // 3. Remove from List Safely
        _serializedLevelObject.Update();
        if (index >= 0 && index < _wavesListProperty.arraySize)
        {
            SerializedProperty element = _wavesListProperty.GetArrayElementAtIndex(index);
        
            // Nullify reference first to ensure clean deletion
            if (element.objectReferenceValue != null)
            {
                element.objectReferenceValue = null;
            }
        
            // Delete the slot
            _wavesListProperty.DeleteArrayElementAtIndex(index);
        }
        _serializedLevelObject.ApplyModifiedProperties();

        // 4. Stop GUI loop
        GUIUtility.ExitGUI();
    }
}