using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq; 

public class WaveEditorWindow : EditorWindow
{
    // Selection
    private LevelData _selectedLevelData;
    private SerializedObject _serializedLevelObject;
    private SerializedProperty _wavesListProperty;
    
    // Level List Management
    private List<LevelData> _cachedLevels = new List<LevelData>();
    private string[] _levelNames;
    private int _selectedLevelIndex = 0;
    
    // Path Constants
    private const string BaseWavePath = "Assets/Scriptable Objects/Waves";

    // Wave Selection
    private WaveSO _selectedWave;
    private SerializedObject _serializedWaveObject;
    private int _selectedWaveIndex = -1;

    // Layout & Resizing
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

    private void OnGUI()
    {
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
        
        // 2. Resize Handle (Draws between the two areas)
        ResizeHandle();

        // 3. Inspector
        DrawWaveInspector();
        
        EditorGUILayout.EndHorizontal();

        if (_serializedLevelObject != null)
        {
            _serializedLevelObject.ApplyModifiedProperties();
        }
    }

    // ----------------- RESIZING LOGIC -----------------
    private void ResizeHandle()
    {
        // Allocate space for the handle (5 pixels wide)
        Rect resizeRect = GUILayoutUtility.GetRect(5f, position.height, GUILayout.ExpandHeight(true));
        
        // 1. Draw a thin divider line (Optional visual polish)
        // We check for Repaint event to draw strictly visual elements
        if (Event.current.type == EventType.Repaint)
        {
            // Pick a color based on the skin (Pro vs Personal)
            Color splitterColor = EditorGUIUtility.isProSkin 
                ? new Color(0.12f, 0.12f, 0.12f) 
                : new Color(0.6f, 0.6f, 0.6f);
            
            // Draw a 1-pixel wide line in the middle of the 5-pixel space
            EditorGUI.DrawRect(new Rect(resizeRect.x + 2, resizeRect.y, 1, resizeRect.height), splitterColor);
        }
        
        // 2. Add the Resize Cursor functionality
        EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

        // 3. Handle Input Events
        Event e = Event.current;
        
        if (e.type == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
        {
            _isResizing = true;
        }
        
        if (_isResizing)
        {
            _sidebarWidth += e.delta.x;
            _sidebarWidth = Mathf.Clamp(_sidebarWidth, MinSidebarWidth, position.width - 100f);
            
            // Force repaint to show updates smoothly
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
        // Changed: Removed "box" style to eliminate the gap/margin
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
        
        // Optional: Draw a subtle vertical line at the right edge of the sidebar
        // This adds a border between sidebar and the resize handle area
        Rect r = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint)
        {
             EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), new Color(0.12f, 0.12f, 0.12f));
        }
    }

    private void DrawWaveInspector()
    {
        // Changed: Removed "box" style here as well
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedWave != null && _serializedWaveObject != null)
        {
            _serializedWaveObject.Update();

            EditorGUILayout.LabelField($"Editing: {_selectedWave.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            SerializedProperty prop = _serializedWaveObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name != "m_Script")
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
            }

            _serializedWaveObject.ApplyModifiedProperties();
        }
        else
        {
            // Centered label when no wave is selected
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
        _selectedWaveIndex = index;
        _selectedWave = wave;
        _serializedWaveObject = (_selectedWave != null) ? new SerializedObject(_selectedWave) : null;
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

        // 1. Handle Asset Deletion
        if (choice == 2 && wave != null)
        {
            string path = AssetDatabase.GetAssetPath(wave);
            AssetDatabase.DeleteAsset(path);
        }

        // 2. Remove from List Safely
        // Ensure the serialized object is up to date before we modify it
        _serializedLevelObject.Update();

        // Check if the index is valid
        if (index >= 0 && index < _wavesListProperty.arraySize)
        {
            SerializedProperty element = _wavesListProperty.GetArrayElementAtIndex(index);

            // Safer approach: Manually nullify the reference first
            if (element.objectReferenceValue != null)
            {
                element.objectReferenceValue = null;
            }

            // Now delete the (null) element. This will definitely remove the slot.
            _wavesListProperty.DeleteArrayElementAtIndex(index);
        }

        _serializedLevelObject.ApplyModifiedProperties();
        
        // 3. Reset Selection
        _selectedWave = null;
        _selectedWaveIndex = -1;
        _serializedWaveObject = null;

        // 4. Stop GUI to prevent layout errors
        GUIUtility.ExitGUI();
    }
}