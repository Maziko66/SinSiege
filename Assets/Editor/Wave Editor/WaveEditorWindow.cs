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
    
    // --- Stats Caching ---
    private int _cachedLevelTotalGold;
    private float _cachedLevelTotalExp;

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
        _serializedWaveObject = null;
        _serializedLevelObject = null;
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.Layout && _selectedLevelData != null)
        {
            CalculateLevelTotals();
        }

        DrawTopBar();

        if (_selectedLevelData == null)
        {
            EditorGUILayout.HelpBox("Please select a Level to begin.", MessageType.Info);
            return;
        }

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

        if (_serializedLevelObject != null)
        {
            _serializedLevelObject.ApplyModifiedProperties();
        }
    }
    
    // --- STATS LOGIC ---
    private void CalculateLevelTotals()
    {
        _cachedLevelTotalGold = 0;
        _cachedLevelTotalExp = 0;

        if (_selectedLevelData == null || _selectedLevelData.Waves == null) return;

        foreach (WaveSO wave in _selectedLevelData.Waves)
        {
            if (wave != null)
            {
                _cachedLevelTotalGold += wave.totalGoldValue;
                _cachedLevelTotalExp += wave.totalExpValue;
            }
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
            e.Use();
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
        
        // Row 1: Level Selection
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

        // Row 2: Level Stats Display
        if (_selectedLevelData != null && _wavesListProperty != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal("box");
            
            // --- NEW: Wave Count ---
            EditorGUILayout.LabelField($"Waves: {_wavesListProperty.arraySize}", EditorStyles.boldLabel, GUILayout.Width(80));
            
            // Divider
            GUILayout.Label("|", GUILayout.Width(10));

            // Gold
            GUI.color = new Color(1f, 0.9f, 0.4f); // Gold tint
            EditorGUILayout.LabelField($"Level Gold: {_cachedLevelTotalGold}", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            // Exp
            GUI.color = new Color(0.6f, 0.8f, 1f); // Blue tint
            EditorGUILayout.LabelField($"Level Exp: {_cachedLevelTotalExp}", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();

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
            _selectedWave = null;
            _selectedWaveIndex = -1;
            _serializedWaveObject = null;

            if (_selectedLevelData != null)
            {
                _serializedLevelObject = new SerializedObject(_selectedLevelData);
                _wavesListProperty = _serializedLevelObject.FindProperty("waves");
                CalculateLevelTotals(); 
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

    // ----------------- INSPECTOR -----------------
    private void DrawWaveInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedWave != null && _serializedWaveObject != null)
        {
            _serializedWaveObject.Update();

            // Header Area
            EditorGUILayout.LabelField($"Editing: {_selectedWave.name}", EditorStyles.boldLabel);
            
            // Wave Stats Display (Dynamic)
            EditorGUILayout.BeginHorizontal("helpBox");
            EditorGUILayout.LabelField($"Wave Gold: {_selectedWave.totalGoldValue}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Wave Exp: {_selectedWave.totalExpValue}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            SerializedProperty iterator = _serializedWaveObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false; 

                if (iterator.name == "m_Script") continue;
                if (iterator.name == "totalGoldValue" || iterator.name == "totalExpValue") continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            _serializedWaveObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                _selectedWave.CalculateTotalStats();
                EditorUtility.SetDirty(_selectedWave);
                CalculateLevelTotals();
            }
            
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
            _serializedWaveObject = new SerializedObject(_selectedWave);
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
        CalculateLevelTotals();
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

        _selectedWave = null;
        _selectedWaveIndex = -1;
        _serializedWaveObject = null;

        if (choice == 2 && wave != null)
        {
            string path = AssetDatabase.GetAssetPath(wave);
            AssetDatabase.DeleteAsset(path);
        }

        _serializedLevelObject.Update();
        if (index >= 0 && index < _wavesListProperty.arraySize)
        {
            SerializedProperty element = _wavesListProperty.GetArrayElementAtIndex(index);
            if (element.objectReferenceValue != null)
            {
                element.objectReferenceValue = null;
            }
            _wavesListProperty.DeleteArrayElementAtIndex(index);
        }
        _serializedLevelObject.ApplyModifiedProperties();

        GUIUtility.ExitGUI();
        CalculateLevelTotals();
    }
}