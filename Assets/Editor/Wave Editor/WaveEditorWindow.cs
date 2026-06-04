using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Level authoring tool for waves, path segments and routes.
///
/// PERSISTENCE MODEL
/// -----------------
/// The previous version edited the level *prefab asset* through two conflicting
/// paths (SerializedObject for waves, PrefabUtility.LoadPrefabContents for
/// routes/segments) and cross-linked Transforms between two prefab-load contexts.
/// That caused edits to revert, exceptions, and broken waypoint references.
///
/// This version edits the level as a LIVE object inside Prefab Mode. You pick a
/// level, the tool opens it in the prefab stage, and every edit goes through a
/// single SerializedObject on the live LevelData (or Undo-recorded Transform
/// moves). Saving uses Unity's normal Prefab Mode save (Auto Save is on by
/// default, or Ctrl+S). Scene handles operate on the same live Transforms, so
/// references can never desync.
/// </summary>
public class WaveEditorWindow : EditorWindow
{
    private enum EditorMode { Waves, Segments, Routes }
    private EditorMode _mode = EditorMode.Waves;

    private const string LevelFolder = "Assets/Prefabs/Levels";
    private const string BaseWavePath = "Assets/Scriptable Objects/Waves";
    private const float MinSidebarWidth = 250f;

    // ---- Level prefab discovery ----
    private string[] _levelPaths = new string[0];
    private string[] _levelNames = new string[0];
    private int _levelDropdownIndex = -1;

    // ---- Live binding to the open prefab stage ----
    private LevelData _level;                 // live component inside the prefab stage
    private string _boundAssetPath;           // assetPath of the stage we are bound to
    private SerializedObject _levelSO;
    private SerializedProperty _waveGroupsProp;
    private SerializedProperty _mapRoutesProp;
    private SerializedProperty _availableSegmentsProp;

    // ---- Selection state ----
    private WaveSO _selectedWave;
    private SerializedObject _selectedWaveSO;
    private int _selWaveGroup = -1;
    private int _selWaveSlot = -1;
    private bool _editingFromPool;
    private int _selRoute = -1;
    private int _selSegment = -1;
    private bool _showHandles = true;

    // ---- Wave pool ----
    private Dictionary<string, List<WaveSO>> _wavePool = new Dictionary<string, List<WaveSO>>();
    private Dictionary<string, bool> _folderFoldouts = new Dictionary<string, bool>();

    // ---- Cached totals ----
    private int _totalGold;
    private float _totalExp;

    // ---- Layout ----
    private float _sidebarWidth = 320f;
    private bool _resizing;
    private Vector2 _poolScroll, _sidebarScroll, _inspectorScroll, _segAvailScroll, _segSelScroll;

    // ---- Styles ----
    private bool _stylesReady;
    private GUIStyle _selectedButton;
    private GUIStyle _centeredGrey;
    private GUIStyle _groupBox;
    private GUIStyle _slot;
    private GUIStyle _slotSelected;
    private GUIStyle _folderHeader;
    private GUIStyle _poolItemSelected;

    [MenuItem("Tools/Wave Editor")]
    public static void ShowWindow() => GetWindow<WaveEditorWindow>("Wave Editor");

    // ==================== LIFECYCLE ====================

    private void OnEnable()
    {
        RefreshLevelList();
        SceneView.duringSceneGui += OnSceneGUI;
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        TryBindCurrentStage();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        Unbind();
    }

    private void OnPrefabStageOpened(PrefabStage stage) => TryBind(stage);

    private void OnPrefabStageClosing(PrefabStage stage)
    {
        if (stage != null && stage.assetPath == _boundAssetPath)
            Unbind();
    }

    // ==================== STAGE BINDING ====================

    private void TryBindCurrentStage() => TryBind(PrefabStageUtility.GetCurrentPrefabStage());

    private void TryBind(PrefabStage stage)
    {
        if (stage == null) return;
        LevelData level = stage.prefabContentsRoot != null
            ? stage.prefabContentsRoot.GetComponent<LevelData>()
            : null;
        if (level == null) return;          // open prefab isn't a level; leave current binding
        Bind(level, stage.assetPath);
    }

    private void Bind(LevelData level, string assetPath)
    {
        _level = level;
        _boundAssetPath = assetPath;
        _levelSO = new SerializedObject(_level);
        _waveGroupsProp = _levelSO.FindProperty("waveGroups");
        _mapRoutesProp = _levelSO.FindProperty("mapRoutes");
        _availableSegmentsProp = _levelSO.FindProperty("availableSegments");

        ResetSelection();
        RefreshWavePool();
        RecalcTotals();
        SyncDropdownToBound();
        Repaint();
    }

    private void Unbind()
    {
        _level = null;
        _boundAssetPath = null;
        _levelSO = null;
        _waveGroupsProp = _mapRoutesProp = _availableSegmentsProp = null;
        ResetSelection();
        _wavePool.Clear();
        Repaint();
    }

    private void ResetSelection()
    {
        _selectedWave = null;
        _selectedWaveSO = null;
        _selWaveGroup = _selWaveSlot = -1;
        _selRoute = _selSegment = -1;
        _editingFromPool = false;
    }

    /// <summary>
    /// Keeps the live binding in sync with whatever prefab stage is open. Cheap
    /// enough to run every repaint, and it self-heals if the SerializedObject's
    /// target was invalidated (domain reload, stage reopen, etc.).
    /// </summary>
    private void RevalidateBinding()
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool stageIsLevel = stage != null && stage.prefabContentsRoot != null &&
                            stage.prefabContentsRoot.GetComponent<LevelData>() != null;

        if (stageIsLevel)
        {
            bool needsRebind = _level == null
                               || _levelSO == null
                               || _levelSO.targetObject == null
                               || _boundAssetPath != stage.assetPath;
            if (needsRebind)
                TryBind(stage);
        }
        else if (_level != null)
        {
            Unbind();
        }
    }

    // ==================== MAIN GUI ====================

    private void OnGUI()
    {
        EnsureStyles();
        RevalidateBinding();

        DrawTopBar();

        if (_level == null)
        {
            DrawNoLevelHelp();
            return;
        }

        _levelSO.Update();

        switch (_mode)
        {
            case EditorMode.Waves: DrawWaveMode(); break;
            case EditorMode.Segments: DrawSegmentMode(); break;
            case EditorMode.Routes: DrawRouteMode(); break;
        }

        // Apply any field-level edits made by PropertyField calls this frame.
        // Structural edits (insert/delete/move) Apply immediately + ExitGUI, so
        // this only covers simple value changes.
        if (_levelSO != null && _levelSO.targetObject != null)
            _levelSO.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_level == null || !_showHandles) return;
        if (_mode == EditorMode.Routes) DrawRouteHandles();
        else if (_mode == EditorMode.Segments) DrawSegmentHandles();
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level:", GUILayout.Width(40));

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup(_levelDropdownIndex, _levelNames, GUILayout.Width(220));
        if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < _levelPaths.Length)
            OpenLevel(_levelPaths[newIndex]);

        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            RefreshLevelList();
            RefreshWavePool();
        }

        GUILayout.FlexibleSpace();

        if (_level != null)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            bool dirty = stage != null && stage.scene.isDirty;
            Color prev = GUI.color;
            GUI.color = dirty ? new Color(1f, 0.7f, 0.3f) : new Color(0.5f, 1f, 0.5f);
            GUILayout.Label(dirty ? "● Unsaved (Ctrl+S)" : "✓ Saved", EditorStyles.boldLabel);
            GUI.color = prev;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        _mode = (EditorMode)GUILayout.Toolbar((int)_mode,
            new[] { "Waves", "Segments", "Routes" }, GUILayout.Height(24));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawNoLevelHelp()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.HelpBox(
            "No level open for editing.\n\n" +
            "Pick a level from the dropdown above to open it in Prefab Mode. " +
            "All edits are made on the live prefab and saved with the normal " +
            "Prefab Mode save (Auto Save, or Ctrl+S).",
            MessageType.Info);

        if (_levelPaths.Length == 0)
            EditorGUILayout.HelpBox($"No level prefabs found under '{LevelFolder}'. " +
                                    "A level prefab is any prefab with a LevelData component on its root.",
                                    MessageType.Warning);
    }

    // ==================== LEVEL DISCOVERY / OPEN ====================

    private void RefreshLevelList()
    {
        var paths = new List<string>();
        var names = new List<string>();

        if (Directory.Exists(LevelFolder))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LevelFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.GetComponent<LevelData>() != null)
                {
                    paths.Add(path);
                    names.Add(Path.GetFileNameWithoutExtension(path));
                }
            }
        }

        _levelPaths = paths.ToArray();
        _levelNames = names.ToArray();
        SyncDropdownToBound();
    }

    private void OpenLevel(string assetPath)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == assetPath)
        {
            TryBindCurrentStage();
            return;
        }
        // Opening fires prefabStageOpened -> TryBind. RevalidateBinding() also
        // catches it on the next repaint, so timing differences are harmless.
        PrefabStageUtility.OpenPrefab(assetPath);
        GUIUtility.ExitGUI();
    }

    private void SyncDropdownToBound()
    {
        _levelDropdownIndex = string.IsNullOrEmpty(_boundAssetPath)
            ? -1
            : System.Array.IndexOf(_levelPaths, _boundAssetPath);
    }

    // ==================== SHARED HELPERS ====================

    private void EnsureStyles()
    {
        if (_stylesReady) return;

        _selectedButton = new GUIStyle(GUI.skin.button) { normal = GUI.skin.button.active };
        _centeredGrey = new GUIStyle(EditorStyles.label)
        { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = Color.gray } };
        _groupBox = new GUIStyle("helpBox")
        { padding = new RectOffset(8, 8, 6, 6), margin = new RectOffset(0, 0, 4, 4) };
        _slot = new GUIStyle(GUI.skin.button)
        { alignment = TextAnchor.MiddleCenter, fontSize = 11, fixedHeight = 22 };
        _slotSelected = new GUIStyle(_slot) { normal = GUI.skin.button.active };
        _folderHeader = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 11 };
        _poolItemSelected = new GUIStyle(EditorStyles.helpBox)
        { padding = new RectOffset(2, 2, 2, 2), margin = new RectOffset(0, 0, 1, 1) };

        _stylesReady = true;
    }

    private void RecalcTotals()
    {
        _totalGold = 0;
        _totalExp = 0f;
        if (_level == null || _level.WaveGroups == null) return;
        foreach (WaveGroup group in _level.WaveGroups)
        {
            if (group?.waveSlots == null) continue;
            foreach (WaveSlot slot in group.waveSlots)
                if (slot?.wave != null)
                {
                    _totalGold += slot.wave.totalGoldValue;
                    _totalExp += slot.wave.totalExpValue;
                }
        }
    }

    private bool IsWaveAssigned(WaveSO wave)
    {
        if (_level == null || _level.WaveGroups == null) return false;
        foreach (WaveGroup group in _level.WaveGroups)
            if (group?.waveSlots != null)
                foreach (WaveSlot slot in group.waveSlots)
                    if (slot != null && slot.wave == wave) return true;
        return false;
    }

    private Color RouteColor(int routeIndex)
    {
        Color[] colors =
        {
            new Color(1f, 0.6f, 0.6f), new Color(0.6f, 0.8f, 1f), new Color(0.6f, 1f, 0.6f),
            new Color(1f, 1f, 0.6f),   new Color(1f, 0.6f, 1f),   new Color(0.6f, 1f, 1f),
            new Color(1f, 0.8f, 0.6f), new Color(0.8f, 0.6f, 1f),
        };
        return routeIndex < 0 ? Color.gray : colors[routeIndex % colors.Length];
    }

    private void MarkLevelDirty()
    {
        if (_level != null)
            EditorSceneManager.MarkSceneDirty(_level.gameObject.scene);
    }

    /// <summary>Commit a structural array change and restart the IMGUI frame cleanly.</summary>
    private void CommitStructuralChange()
    {
        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        RecalcTotals();
        GUIUtility.ExitGUI();
    }

    private void DrawResizeHandle()
    {
        Rect rect = GUILayoutUtility.GetRect(5f, 0f, GUILayout.Width(5f), GUILayout.ExpandHeight(true));
        if (Event.current.type == EventType.Repaint)
        {
            Color c = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y, 1, rect.height), c);
        }
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) { _resizing = true; e.Use(); }
        if (_resizing)
        {
            _sidebarWidth = Mathf.Clamp(_sidebarWidth + e.delta.x, MinSidebarWidth, position.width - 320f);
            Repaint();
        }
        if (e.type == EventType.MouseUp) _resizing = false;
    }

    // ============================================================
    //  WAVE MODE
    // ============================================================

    private void DrawWaveMode()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(290), GUILayout.ExpandHeight(true));
        DrawWavePool();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawWaveGroups();
        EditorGUILayout.EndVertical();

        DrawResizeHandle();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawWaveInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWavePool()
    {
        EditorGUILayout.LabelField("Available Waves", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Create New Wave", GUILayout.Height(24))) CreateWaveAsset(-1, -1);
        if (GUILayout.Button("Refresh Pool", GUILayout.Height(18))) RefreshWavePool();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft))
            foreach (var k in _folderFoldouts.Keys.ToList()) _folderFoldouts[k] = true;
        if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight))
            foreach (var k in _folderFoldouts.Keys.ToList()) _folderFoldouts[k] = false;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        _poolScroll = EditorGUILayout.BeginScrollView(_poolScroll);

        if (_wavePool.Count == 0)
        {
            EditorGUILayout.HelpBox("No waves found.", MessageType.Info);
        }
        else
        {
            string levelName = _level != null ? _level.name : "";
            var folders = _wavePool.Keys
                .OrderBy(f => (f == levelName ? "0" : "1") + f)
                .ToList();

            foreach (string folder in folders)
            {
                List<WaveSO> waves = _wavePool[folder];
                if (waves.Count == 0) continue;
                if (!_folderFoldouts.ContainsKey(folder)) _folderFoldouts[folder] = folder == levelName;

                int assigned = waves.Count(IsWaveAssigned);
                bool isLevelFolder = folder == levelName;

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = isLevelFolder ? new Color(0.8f, 1f, 0.8f) : Color.white;
                _folderFoldouts[folder] = EditorGUILayout.Foldout(_folderFoldouts[folder], "", true, _folderHeader);
                EditorGUILayout.LabelField($"📁 {folder} ({assigned}/{waves.Count})", EditorStyles.boldLabel);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (_folderFoldouts[folder])
                {
                    EditorGUI.indentLevel++;
                    foreach (WaveSO wave in waves.OrderBy(w => w.name))
                        if (wave != null) DrawWavePoolItem(wave);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.HelpBox("Click a wave to edit it. Select an empty slot, then '>' to assign.", MessageType.None);
    }

    private void DrawWavePoolItem(WaveSO wave)
    {
        bool isAssigned = IsWaveAssigned(wave);
        bool isSelected = _selectedWave == wave && _editingFromPool;

        if (isSelected) GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        else if (isAssigned) GUI.backgroundColor = new Color(0.72f, 0.72f, 0.72f);

        EditorGUILayout.BeginHorizontal(isSelected ? _poolItemSelected : GUIStyle.none);
        GUILayout.Space(15);

        string label = wave.name.Length > 25 ? wave.name.Substring(0, 22) + "..." : wave.name;
        if (GUILayout.Button(isAssigned ? $"✓ {label}" : label, EditorStyles.label, GUILayout.ExpandWidth(true)))
            SelectWaveFromPool(wave);
        GUI.backgroundColor = Color.white;

        bool canAssign = _selWaveGroup >= 0 && _selWaveSlot >= 0 && !_editingFromPool;
        using (new EditorGUI.DisabledScope(!canAssign))
        {
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button(">", GUILayout.Width(22)))
                AssignWaveToSlot(_selWaveGroup, _selWaveSlot, wave);
            GUI.backgroundColor = Color.white;
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("X", GUILayout.Width(20))) DeleteWaveAsset(wave);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveGroups()
    {
        EditorGUILayout.BeginHorizontal("helpBox");
        EditorGUILayout.LabelField($"Total Gold: {_totalGold}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Exp: {_totalExp:F1}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Wave Groups", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add Wave Group", GUILayout.Height(24))) AddWaveGroup();

        EditorGUILayout.Space(4);
        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

        if (_waveGroupsProp != null)
        {
            for (int g = 0; g < _waveGroupsProp.arraySize; g++)
                DrawWaveGroup(g);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawWaveGroup(int g)
    {
        SerializedProperty groupProp = _waveGroupsProp.GetArrayElementAtIndex(g);
        SerializedProperty slotsProp = groupProp.FindPropertyRelative("waveSlots");

        EditorGUILayout.BeginVertical(_groupBox);

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Wave {g + 1}", EditorStyles.boldLabel, GUILayout.Width(60));

        int gold = 0; float exp = 0f;
        for (int w = 0; w < slotsProp.arraySize; w++)
        {
            var wave = (WaveSO)slotsProp.GetArrayElementAtIndex(w).FindPropertyRelative("wave").objectReferenceValue;
            if (wave != null) { gold += wave.totalGoldValue; exp += wave.totalExpValue; }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"G:{gold} E:{exp:F0}", GUILayout.Width(90));

        using (new EditorGUI.DisabledScope(g == 0))
            if (GUILayout.Button("↑", GUILayout.Width(22))) { MoveWaveGroup(g, g - 1); return; }
        using (new EditorGUI.DisabledScope(g >= _waveGroupsProp.arraySize - 1))
            if (GUILayout.Button("↓", GUILayout.Width(22))) { MoveWaveGroup(g, g + 1); return; }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("X", GUILayout.Width(22))) { DeleteWaveGroup(g); return; }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Slots row (horizontal scroll so many slots never overflow off-screen)
        var slotScroll = EditorGUILayout.BeginScrollView(
            GetGroupScroll(g), false, false,
            GUI.skin.horizontalScrollbar, GUIStyle.none, GUIStyle.none,
            GUILayout.Height(72));
        SetGroupScroll(g, slotScroll);

        EditorGUILayout.BeginHorizontal();
        for (int w = 0; w < slotsProp.arraySize; w++)
            if (DrawWaveSlot(g, w, slotsProp)) { return; } // structural change happened

        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("+", GUILayout.Width(26), GUILayout.Height(58))) { AddWaveSlot(g); return; }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <returns>true if a structural change was committed (caller must stop drawing).</returns>
    private bool DrawWaveSlot(int g, int w, SerializedProperty slotsProp)
    {
        SerializedProperty slotProp = slotsProp.GetArrayElementAtIndex(w);
        SerializedProperty waveProp = slotProp.FindPropertyRelative("wave");
        SerializedProperty routeProp = slotProp.FindPropertyRelative("routeIndex");

        var wave = (WaveSO)waveProp.objectReferenceValue;
        int routeIdx = routeProp.intValue;
        bool selected = g == _selWaveGroup && w == _selWaveSlot && !_editingFromPool;

        EditorGUILayout.BeginVertical(GUILayout.Width(110));

        // Route dropdown
        GUI.backgroundColor = RouteColor(routeIdx);
        var routeOptions = BuildRouteOptions();
        if (routeOptions.Length > 0)
        {
            int clampedRoute = Mathf.Clamp(routeIdx, 0, routeOptions.Length - 1);
            int newRoute = EditorGUILayout.Popup(clampedRoute, routeOptions, GUILayout.Width(105));
            // Compare against the clamped value so merely *displaying* an
            // out-of-range index doesn't silently rewrite it; only a real
            // user selection commits a change.
            if (newRoute != clampedRoute)
            {
                routeProp.intValue = newRoute;
                _levelSO.ApplyModifiedProperties();
                MarkLevelDirty();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No Routes", GUILayout.Width(105));
        }
        GUI.backgroundColor = Color.white;

        // Wave button
        string label = wave != null ? wave.name : "(Empty)";
        if (label.Length > 12) label = label.Substring(0, 9) + "...";
        GUI.backgroundColor = wave != null ? RouteColor(routeIdx) : new Color(0.9f, 0.9f, 0.9f);
        if (GUILayout.Button(label, selected ? _slotSelected : _slot, GUILayout.Width(105)))
            SelectWaveSlot(g, w, wave);
        GUI.backgroundColor = Color.white;

        // Remove slot
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        bool remove = GUILayout.Button("× remove", GUILayout.Width(105), GUILayout.Height(16));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();

        if (remove) { DeleteWaveSlot(g, w); return true; }
        return false;
    }

    private string[] BuildRouteOptions()
    {
        var routes = _level != null ? _level.MapRoutes : null;
        if (routes == null) return new string[0];
        var options = new string[routes.Count];
        for (int r = 0; r < routes.Count; r++)
        {
            string name = string.IsNullOrEmpty(routes[r].routeName) ? $"Route {r}" : routes[r].routeName;
            options[r] = $"{r}: {name}";
        }
        return options;
    }

    private void DrawWaveInspector()
    {
        _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

        if (_selectedWave != null && _selectedWaveSO != null && _selectedWaveSO.targetObject != null)
        {
            DrawSelectedWaveInspector();
        }
        else if (_selWaveGroup >= 0 && _selWaveSlot >= 0 && !_editingFromPool)
        {
            EditorGUILayout.LabelField("Empty Wave Slot", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select a wave from the pool and click '>' to assign, or:", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var assigned = (WaveSO)EditorGUILayout.ObjectField("Drag Wave Here", null, typeof(WaveSO), false);
            if (EditorGUI.EndChangeCheck() && assigned != null)
                AssignWaveToSlot(_selWaveGroup, _selWaveSlot, assigned);

            EditorGUILayout.Space();
            if (GUILayout.Button("Create New Wave & Assign", GUILayout.Height(30)))
                CreateWaveAsset(_selWaveGroup, _selWaveSlot);
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a wave to edit.", _centeredGrey, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedWaveInspector()
    {
        _selectedWaveSO.Update();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_editingFromPool ? "Editing (pool):" : "Editing:",
            GUILayout.Width(_editingFromPool ? 90 : 55));
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.DelayedTextField(_selectedWave.name, EditorStyles.textField);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName) && newName != _selectedWave.name)
            RenameWaveAsset(_selectedWave, newName);
        if (GUILayout.Button("Ping", GUILayout.Width(50)))
        {
            EditorGUIUtility.PingObject(_selectedWave);
            Selection.activeObject = _selectedWave;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal("helpBox");
        EditorGUILayout.LabelField($"Gold: {_selectedWave.totalGoldValue}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Exp: {_selectedWave.totalExpValue:F1}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        SerializedProperty it = _selectedWaveSO.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.name == "m_Script" || it.name == "totalGoldValue" || it.name == "totalExpValue") continue;
            EditorGUILayout.PropertyField(it, true);
        }
        _selectedWaveSO.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            _selectedWave.CalculateTotalStats();
            EditorUtility.SetDirty(_selectedWave);
            RecalcTotals();
        }
    }

    // ---- Wave selection ----

    private void SelectWaveFromPool(WaveSO wave)
    {
        _selectedWave = wave;
        _selectedWaveSO = wave != null ? new SerializedObject(wave) : null;
        _selWaveGroup = _selWaveSlot = -1;
        _editingFromPool = true;
        GUI.FocusControl(null);
    }

    private void SelectWaveSlot(int g, int w, WaveSO wave)
    {
        _selWaveGroup = g;
        _selWaveSlot = w;
        _selectedWave = wave;
        _selectedWaveSO = wave != null ? new SerializedObject(wave) : null;
        _editingFromPool = false;
        GUI.FocusControl(null);
    }

    // ---- Wave group / slot structural ops (all on the live LevelData) ----

    private void AddWaveGroup()
    {
        int i = _waveGroupsProp.arraySize;
        _waveGroupsProp.InsertArrayElementAtIndex(i);
        _waveGroupsProp.GetArrayElementAtIndex(i).FindPropertyRelative("waveSlots").ClearArray();
        CommitStructuralChange();
    }

    private void MoveWaveGroup(int from, int to)
    {
        _waveGroupsProp.MoveArrayElement(from, to);
        if (_selWaveGroup == from) _selWaveGroup = to;
        else if (_selWaveGroup == to) _selWaveGroup = from;
        CommitStructuralChange();
    }

    private void DeleteWaveGroup(int index)
    {
        if (!EditorUtility.DisplayDialog("Delete Wave Group", $"Delete Wave Group {index + 1}?", "Delete", "Cancel"))
            return;
        _waveGroupsProp.DeleteArrayElementAtIndex(index);
        if (_selWaveGroup == index) ResetWaveSlotSelection();
        else if (_selWaveGroup > index) _selWaveGroup--;
        CommitStructuralChange();
    }

    private void AddWaveSlot(int g)
    {
        SerializedProperty slots = _waveGroupsProp.GetArrayElementAtIndex(g).FindPropertyRelative("waveSlots");
        int i = slots.arraySize;
        slots.InsertArrayElementAtIndex(i);
        SerializedProperty slot = slots.GetArrayElementAtIndex(i);
        slot.FindPropertyRelative("wave").objectReferenceValue = null;
        slot.FindPropertyRelative("routeIndex").intValue = 0;
        _levelSO.ApplyModifiedProperties();
        SelectWaveSlot(g, i, null);
        MarkLevelDirty();
        GUIUtility.ExitGUI();
    }

    private void DeleteWaveSlot(int g, int w)
    {
        SerializedProperty slots = _waveGroupsProp.GetArrayElementAtIndex(g).FindPropertyRelative("waveSlots");
        slots.DeleteArrayElementAtIndex(w);
        if (_selWaveGroup == g && _selWaveSlot == w) ResetWaveSlotSelection();
        else if (_selWaveGroup == g && _selWaveSlot > w) _selWaveSlot--;
        CommitStructuralChange();
    }

    private void AssignWaveToSlot(int g, int w, WaveSO wave)
    {
        SerializedProperty slots = _waveGroupsProp.GetArrayElementAtIndex(g).FindPropertyRelative("waveSlots");
        slots.GetArrayElementAtIndex(w).FindPropertyRelative("wave").objectReferenceValue = wave;
        _levelSO.ApplyModifiedProperties();
        SelectWaveSlot(g, w, wave);
        MarkLevelDirty();
        RecalcTotals();
    }

    private void ResetWaveSlotSelection()
    {
        _selWaveSlot = -1;
        _selectedWave = null;
        _selectedWaveSO = null;
    }

    // ---- Wave asset management ----

    private void RefreshWavePool()
    {
        _wavePool.Clear();
        if (!Directory.Exists(BaseWavePath)) return;

        foreach (string guid in AssetDatabase.FindAssets("t:WaveSO", new[] { BaseWavePath }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WaveSO wave = AssetDatabase.LoadAssetAtPath<WaveSO>(path);
            if (wave == null) continue;

            string folderPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folder = folderPath == BaseWavePath ? "Shared" : Path.GetFileName(folderPath);

            if (!_wavePool.ContainsKey(folder)) _wavePool[folder] = new List<WaveSO>();
            _wavePool[folder].Add(wave);
        }

        string levelName = _level != null ? _level.name : "";
        foreach (string folder in _wavePool.Keys)
            if (!_folderFoldouts.ContainsKey(folder))
                _folderFoldouts[folder] = folder == levelName;
    }

    private void CreateWaveAsset(int assignGroup, int assignSlot)
    {
        if (_level == null) return;

        string folder = $"{BaseWavePath}/{_level.name}";
        if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); AssetDatabase.Refresh(); }

        string baseName = $"Wave_{_level.name.Replace(" ", "_")}_";
        int n = 0;
        string path = $"{folder}/{baseName}{n:D3}.asset";
        while (AssetDatabase.LoadAssetAtPath<WaveSO>(path) != null)
            path = $"{folder}/{baseName}{++n:D3}.asset";

        WaveSO wave = CreateInstance<WaveSO>();
        wave.name = $"{baseName}{n:D3}";
        AssetDatabase.CreateAsset(wave, path);
        AssetDatabase.SaveAssets();
        RefreshWavePool();

        if (assignGroup >= 0 && assignSlot >= 0) AssignWaveToSlot(assignGroup, assignSlot, wave);
        else SelectWaveFromPool(wave);

        EditorGUIUtility.PingObject(wave);
    }

    private void DeleteWaveAsset(WaveSO wave)
    {
        if (wave == null) return;
        string msg = $"Delete '{wave.name}'?" + (IsWaveAssigned(wave) ? "\n\nWARNING: this wave is assigned to a slot!" : "");
        if (!EditorUtility.DisplayDialog("Delete Wave", msg, "Delete", "Cancel")) return;

        if (_selectedWave == wave) { _selectedWave = null; _selectedWaveSO = null; _editingFromPool = false; }
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(wave));
        RefreshWavePool();
        RecalcTotals();
    }

    private void RenameWaveAsset(WaveSO wave, string newName)
    {
        string oldPath = AssetDatabase.GetAssetPath(wave);
        if (string.IsNullOrEmpty(oldPath)) return;

        string clean = newName.Replace(" ", "_");
        foreach (char c in Path.GetInvalidFileNameChars()) clean = clean.Replace(c.ToString(), "");

        string error = AssetDatabase.RenameAsset(oldPath, clean);
        if (string.IsNullOrEmpty(error))
        {
            AssetDatabase.SaveAssets();
            _selectedWaveSO = new SerializedObject(_selectedWave);
            RefreshWavePool();
        }
        else Debug.LogError($"Rename failed: {error}");
    }

    // ---- Per-group horizontal scroll storage ----
    private readonly Dictionary<int, Vector2> _groupScrolls = new Dictionary<int, Vector2>();
    private Vector2 GetGroupScroll(int g) => _groupScrolls.TryGetValue(g, out var v) ? v : Vector2.zero;
    private void SetGroupScroll(int g, Vector2 v) => _groupScrolls[g] = v;

    // ============================================================
    //  SEGMENT MODE
    // ============================================================

    private void DrawSegmentMode()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawSegmentSidebar();
        EditorGUILayout.EndVertical();

        DrawResizeHandle();

        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        if (_selSegment >= 0 && _availableSegmentsProp != null && _selSegment < _availableSegmentsProp.arraySize)
            DrawSegmentInspector();
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a segment to edit.", _centeredGrey);
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSegmentSidebar()
    {
        EditorGUILayout.LabelField("Segment Pool", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Create New Segment", GUILayout.Height(28))) CreateSegment();

        EditorGUILayout.Space();
        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

        if (_availableSegmentsProp != null)
        {
            for (int i = 0; i < _availableSegmentsProp.arraySize; i++)
            {
                SerializedProperty segProp = _availableSegmentsProp.GetArrayElementAtIndex(i);
                string name = segProp.FindPropertyRelative("segmentName").stringValue;
                if (string.IsNullOrEmpty(name)) name = $"Segment {i}";

                EditorGUILayout.BeginHorizontal();
                GUIStyle style = i == _selSegment ? _selectedButton : GUI.skin.button;
                if (GUILayout.Button(name, style, GUILayout.Height(25)))
                {
                    _selSegment = i;
                    SceneView.RepaintAll();
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25))) { DeleteSegment(i); return; }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        _showHandles = EditorGUILayout.Toggle("Show Scene Handles", _showHandles);
    }

    private void DrawSegmentInspector()
    {
        SerializedProperty segProp = _availableSegmentsProp.GetArrayElementAtIndex(_selSegment);
        SerializedProperty nameProp = segProp.FindPropertyRelative("segmentName");
        SerializedProperty spawnProp = segProp.FindPropertyRelative("spawnPoint");
        SerializedProperty pointsProp = segProp.FindPropertyRelative("waypoints");

        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Segment Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nameProp);
        DrawSpawnPointSelector(spawnProp);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        float height = Mathf.Max(position.height - 200f, 200f);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(height));

        // Available waypoint transforms (children of the "Waypoints" object)
        EditorGUILayout.BeginVertical("box", GUILayout.Width(300));
        EditorGUILayout.LabelField("Available in Prefab", EditorStyles.boldLabel);
        Transform waypointsRoot = _level.transform.Find("Waypoints");
        if (waypointsRoot == null)
        {
            EditorGUILayout.HelpBox("No 'Waypoints' child found on the level root.", MessageType.Warning);
        }
        else
        {
            _segAvailScroll = EditorGUILayout.BeginScrollView(_segAvailScroll);
            foreach (Transform child in waypointsRoot)
            {
                bool added = false;
                for (int i = 0; i < pointsProp.arraySize; i++)
                    if (pointsProp.GetArrayElementAtIndex(i).objectReferenceValue == child) { added = true; break; }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(child.name, GUILayout.MinWidth(50));
                using (new EditorGUI.DisabledScope(added))
                    if (GUILayout.Button("Add >", GUILayout.Width(50)))
                    {
                        int idx = pointsProp.arraySize;
                        pointsProp.InsertArrayElementAtIndex(idx);
                        pointsProp.GetArrayElementAtIndex(idx).objectReferenceValue = child;
                        _levelSO.ApplyModifiedProperties();
                        MarkLevelDirty();
                        SceneView.RepaintAll();
                    }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        // Selected waypoints (ordered)
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Selected Waypoints", EditorStyles.boldLabel);
        _segSelScroll = EditorGUILayout.BeginScrollView(_segSelScroll);
        for (int i = 0; i < pointsProp.arraySize; i++)
        {
            var t = (Transform)pointsProp.GetArrayElementAtIndex(i).objectReferenceValue;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i}. {(t != null ? t.name : "(Null)")}", GUILayout.ExpandWidth(true));
            using (new EditorGUI.DisabledScope(i == 0))
                if (GUILayout.Button("Up", GUILayout.Width(30))) { pointsProp.MoveArrayElement(i, i - 1); ApplyAndRepaintScene(); return; }
            using (new EditorGUI.DisabledScope(i >= pointsProp.arraySize - 1))
                if (GUILayout.Button("Dn", GUILayout.Width(30))) { pointsProp.MoveArrayElement(i, i + 1); ApplyAndRepaintScene(); return; }
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("X", GUILayout.Width(25))) { RemoveObjectRefArrayElement(pointsProp, i); ApplyAndRepaintScene(); return; }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void ApplyAndRepaintScene()
    {
        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        SceneView.RepaintAll();
        GUIUtility.ExitGUI();
    }

    private void DrawSpawnPointSelector(SerializedProperty spawnProp)
    {
        Transform container = _level.transform.Find("Spawn Points");
        if (container == null) { EditorGUILayout.PropertyField(spawnProp); return; }

        var options = new List<string> { "None" };
        var values = new List<Transform> { null };
        foreach (Transform child in container) { options.Add(child.name); values.Add(child); }

        var current = (Transform)spawnProp.objectReferenceValue;
        int index = current != null ? Mathf.Max(0, values.IndexOf(current)) : 0;
        int newIndex = EditorGUILayout.Popup("Spawn Point", index, options.ToArray());
        if (newIndex != index)
        {
            spawnProp.objectReferenceValue = values[newIndex];
            _levelSO.ApplyModifiedProperties();
            MarkLevelDirty();
        }
    }

    private void CreateSegment()
    {
        int i = _availableSegmentsProp.arraySize;
        _availableSegmentsProp.InsertArrayElementAtIndex(i);
        SerializedProperty seg = _availableSegmentsProp.GetArrayElementAtIndex(i);
        seg.FindPropertyRelative("segmentName").stringValue = $"New_Segment_{i}";
        seg.FindPropertyRelative("spawnPoint").objectReferenceValue = null;
        seg.FindPropertyRelative("waypoints").ClearArray();
        _selSegment = i;
        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        GUIUtility.ExitGUI();
    }

    private void DeleteSegment(int index)
    {
        _availableSegmentsProp.DeleteArrayElementAtIndex(index);
        if (_selSegment >= index) _selSegment = -1;
        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        SceneView.RepaintAll();
        GUIUtility.ExitGUI();
    }

    // ============================================================
    //  ROUTE MODE
    // ============================================================

    private void DrawRouteMode()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawRouteSidebar();
        EditorGUILayout.EndVertical();

        DrawResizeHandle();

        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawRouteInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRouteSidebar()
    {
        EditorGUILayout.LabelField("Map Routes", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Create New Route", GUILayout.Height(28))) CreateRoute();

        EditorGUILayout.Space();
        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

        if (_mapRoutesProp != null)
        {
            for (int i = 0; i < _mapRoutesProp.arraySize; i++)
            {
                SerializedProperty routeProp = _mapRoutesProp.GetArrayElementAtIndex(i);
                string name = routeProp.FindPropertyRelative("routeName").stringValue;
                if (string.IsNullOrEmpty(name)) name = $"Route {i}";

                EditorGUILayout.BeginHorizontal();
                GUIStyle style = i == _selRoute ? _selectedButton : GUI.skin.button;
                if (GUILayout.Button($"{i}: {name}", style, GUILayout.Height(25)))
                {
                    _selRoute = i;
                    SceneView.RepaintAll();
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25))) { DeleteRoute(i); return; }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        _showHandles = EditorGUILayout.Toggle("Show Scene Handles", _showHandles);
    }

    private void DrawRouteInspector()
    {
        _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

        if (_selRoute < 0 || _mapRoutesProp == null || _selRoute >= _mapRoutesProp.arraySize)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a route to edit.", _centeredGrey, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
            return;
        }

        SerializedProperty routeProp = _mapRoutesProp.GetArrayElementAtIndex(_selRoute);
        EditorGUILayout.LabelField("Route Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(routeProp.FindPropertyRelative("routeName"));
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Segment Sequence", EditorStyles.boldLabel);
        SerializedProperty segments = routeProp.FindPropertyRelative("pathSegments");

        if (segments.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Route is empty. Add a segment from the pool below.", MessageType.Info);
        }
        else
        {
            Transform firstSpawn = (Transform)segments.GetArrayElementAtIndex(0)
                .FindPropertyRelative("spawnPoint").objectReferenceValue;
            if (firstSpawn == null)
                EditorGUILayout.HelpBox("The first segment must have a Spawn Point.", MessageType.Error);
        }

        for (int i = 0; i < segments.arraySize; i++)
            if (DrawRouteSegmentRow(segments, i)) return;

        EditorGUILayout.Space(8);
        if (_availableSegmentsProp != null && _availableSegmentsProp.arraySize > 0)
        {
            if (GUILayout.Button("+ Add Segment from Pool", GUILayout.Height(28)))
                ShowAddSegmentMenu(segments);
        }
        else
        {
            EditorGUILayout.HelpBox("No segments in the pool. Create some in the Segments tab first.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private bool DrawRouteSegmentRow(SerializedProperty segments, int i)
    {
        SerializedProperty segProp = segments.GetArrayElementAtIndex(i);
        string name = segProp.FindPropertyRelative("segmentName").stringValue;
        bool hasSpawn = segProp.FindPropertyRelative("spawnPoint").objectReferenceValue != null;

        EditorGUILayout.BeginHorizontal("helpBox");
        EditorGUILayout.LabelField($"{i + 1}. {name}" + (hasSpawn ? " [Spawn]" : ""),
            EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

        using (new EditorGUI.DisabledScope(i == 0))
            if (GUILayout.Button("Up", GUILayout.Width(28))) { segments.MoveArrayElement(i, i - 1); ApplyAndRepaintScene(); return true; }
        using (new EditorGUI.DisabledScope(i >= segments.arraySize - 1))
            if (GUILayout.Button("Dn", GUILayout.Width(28))) { segments.MoveArrayElement(i, i + 1); ApplyAndRepaintScene(); return true; }
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("X", GUILayout.Width(25))) { segments.DeleteArrayElementAtIndex(i); ApplyAndRepaintScene(); return true; }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
        return false;
    }

    private void ShowAddSegmentMenu(SerializedProperty segments)
    {
        var menu = new GenericMenu();
        for (int s = 0; s < _availableSegmentsProp.arraySize; s++)
        {
            SerializedProperty poolSeg = _availableSegmentsProp.GetArrayElementAtIndex(s);
            string name = poolSeg.FindPropertyRelative("segmentName").stringValue;
            bool hasSpawn = poolSeg.FindPropertyRelative("spawnPoint").objectReferenceValue != null;
            int poolIndex = s;
            menu.AddItem(new GUIContent(name + (hasSpawn ? " (Has Spawn)" : "")), false,
                () => AddSegmentFromPool(segments, poolIndex));
        }
        menu.ShowAsContext();
    }

    /// <summary>
    /// Copies a pooled segment into the route. Because we edit the live prefab,
    /// the copied spawnPoint / waypoint Transforms reference the same live child
    /// objects as the pool entry — no cross-context reference breakage.
    /// </summary>
    private void AddSegmentFromPool(SerializedProperty segments, int poolIndex)
    {
        if (_level.AvailableSegments == null || poolIndex < 0 || poolIndex >= _level.AvailableSegments.Count)
            return;

        PathSegment src = _level.AvailableSegments[poolIndex];

        int idx = segments.arraySize;
        segments.InsertArrayElementAtIndex(idx);
        SerializedProperty dst = segments.GetArrayElementAtIndex(idx);

        dst.FindPropertyRelative("segmentName").stringValue = src.segmentName;
        dst.FindPropertyRelative("spawnPoint").objectReferenceValue = src.spawnPoint;

        SerializedProperty wp = dst.FindPropertyRelative("waypoints");
        wp.ClearArray();
        if (src.waypoints != null)
            for (int i = 0; i < src.waypoints.Count; i++)
            {
                wp.InsertArrayElementAtIndex(i);
                wp.GetArrayElementAtIndex(i).objectReferenceValue = src.waypoints[i];
            }

        ApplyAndRepaintScene();
    }

    private void CreateRoute()
    {
        int i = _mapRoutesProp.arraySize;
        _mapRoutesProp.InsertArrayElementAtIndex(i);
        SerializedProperty route = _mapRoutesProp.GetArrayElementAtIndex(i);
        route.FindPropertyRelative("routeName").stringValue = "New Route";
        route.FindPropertyRelative("spawnPoint").objectReferenceValue = null;
        route.FindPropertyRelative("pathSegments").ClearArray();
        _selRoute = i;
        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        GUIUtility.ExitGUI();
    }

    private void DeleteRoute(int index)
    {
        _mapRoutesProp.DeleteArrayElementAtIndex(index);
        if (_selRoute >= index) _selRoute = -1;
        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        SceneView.RepaintAll();
        GUIUtility.ExitGUI();
    }

    // ==================== SCENE HANDLES ====================

    private void DrawRouteHandles()
    {
        var routes = _level.MapRoutes;
        if (routes == null) return;

        for (int r = 0; r < routes.Count; r++)
        {
            bool isSelected = r == _selRoute;
            Color color = isSelected ? Color.green : new Color(1f, 1f, 1f, 0.3f);
            MapRoute route = routes[r];
            if (route.pathSegments == null) continue;

            foreach (PathSegment seg in route.pathSegments)
            {
                if (seg.waypoints == null) continue;
                for (int w = 0; w < seg.waypoints.Count; w++)
                {
                    Transform point = seg.waypoints[w];
                    if (point == null) continue;

                    if (w < seg.waypoints.Count - 1 && seg.waypoints[w + 1] != null)
                    {
                        Handles.color = color;
                        Handles.DrawLine(point.position, seg.waypoints[w + 1].position, 2f);
                    }

                    if (isSelected)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos = Handles.PositionHandle(point.position, Quaternion.identity);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(point, "Move Waypoint");
                            point.position = newPos;
                        }
                        Handles.Label(point.position + Vector3.up * 0.5f, $"{seg.segmentName}_{w}");
                    }
                }
            }
        }
    }

    private void DrawSegmentHandles()
    {
        PathSegment seg = (_selSegment >= 0 && _level.AvailableSegments != null &&
                           _selSegment < _level.AvailableSegments.Count)
            ? _level.AvailableSegments[_selSegment]
            : null;

        // 1. Show EVERY waypoint in the level (children of the "Waypoints" object).
        //    When a segment is selected, each dot is a clickable button: click an
        //    available (white) one to add it to the segment, click a member (green)
        //    one to remove it.
        Transform waypointsRoot = _level.transform.Find("Waypoints");
        if (waypointsRoot != null)
        {
            foreach (Transform child in waypointsRoot)
            {
                if (child == null) continue;

                int order = (seg?.waypoints != null) ? seg.waypoints.IndexOf(child) : -1;
                bool inSeg = order >= 0;
                float size = HandleUtility.GetHandleSize(child.position);

                Handles.color = inSeg ? Color.green : new Color(1f, 1f, 1f, 0.6f);

                if (seg != null)
                {
                    // Interactive toggle. The dot sits at the centre; the move
                    // PositionHandle (drawn below) owns the offset axes, so the
                    // handle system routes a centre click here and an axis drag there.
                    if (Handles.Button(child.position, Quaternion.identity,
                                       size * 0.08f, size * 0.13f, Handles.DotHandleCap))
                    {
                        ToggleWaypointInSegment(child);
                        return; // segment list changed; abort this pass
                    }
                }
                else
                {
                    Handles.DotHandleCap(0, child.position, Quaternion.identity,
                                         size * 0.06f, EventType.Repaint);
                }

                Handles.Label(child.position + Vector3.up * 0.25f,
                              inSeg ? $"{child.name} [{order}]" : child.name);
            }
        }

        // 1b. Show spawn points (children of "Spawn Points") in orange. The one
        //     assigned to the selected segment is highlighted, with a dotted line
        //     to the segment's first waypoint.
        Transform spawnRoot = _level.transform.Find("Spawn Points");
        Transform segSpawn = seg != null ? seg.spawnPoint : null;
        if (spawnRoot != null)
        {
            foreach (Transform sp in spawnRoot)
            {
                if (sp == null) continue;
                bool assigned = sp == segSpawn;
                float size = HandleUtility.GetHandleSize(sp.position);
                Handles.color = assigned ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.8f, 0.35f, 0.5f);
                Handles.DotHandleCap(0, sp.position, Quaternion.identity,
                                     size * (assigned ? 0.09f : 0.06f), EventType.Repaint);
                Handles.Label(sp.position + Vector3.up * 0.3f,
                              assigned ? $"★ SPAWN: {sp.name}" : $"(spawn) {sp.name}");
            }
        }
        if (segSpawn != null && seg?.waypoints != null && seg.waypoints.Count > 0 && seg.waypoints[0] != null)
        {
            Handles.color = new Color(1f, 0.55f, 0.1f);
            Handles.DrawDottedLine(segSpawn.position, seg.waypoints[0].position, 4f);
        }

        // 2. Connect the selected segment's waypoints in order and make them movable.
        if (seg?.waypoints == null) return;

        Handles.color = Color.cyan;
        for (int w = 0; w < seg.waypoints.Count; w++)
        {
            Transform point = seg.waypoints[w];
            if (point == null) continue;

            if (w < seg.waypoints.Count - 1 && seg.waypoints[w + 1] != null)
                Handles.DrawLine(point.position, seg.waypoints[w + 1].position, 2f);

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(point.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point, "Move Waypoint");
                point.position = newPos;
            }
        }
    }

    /// <summary>
    /// Adds the waypoint to the selected segment (appended in click order), or
    /// removes it if it is already a member. Called from the scene-view dots.
    /// </summary>
    private void ToggleWaypointInSegment(Transform waypoint)
    {
        if (_selSegment < 0 || _availableSegmentsProp == null ||
            _selSegment >= _availableSegmentsProp.arraySize) return;

        _levelSO.Update();
        SerializedProperty wp = _availableSegmentsProp
            .GetArrayElementAtIndex(_selSegment)
            .FindPropertyRelative("waypoints");

        int existing = -1;
        for (int i = 0; i < wp.arraySize; i++)
            if (wp.GetArrayElementAtIndex(i).objectReferenceValue == waypoint) { existing = i; break; }

        if (existing >= 0)
        {
            RemoveObjectRefArrayElement(wp, existing);
        }
        else
        {
            int idx = wp.arraySize;
            wp.InsertArrayElementAtIndex(idx);
            wp.GetArrayElementAtIndex(idx).objectReferenceValue = waypoint;
        }

        _levelSO.ApplyModifiedProperties();
        MarkLevelDirty();
        Repaint();            // refresh the inspector's "Selected Waypoints" list
        SceneView.RepaintAll();
    }

    /// <summary>
    /// Removes one element from a SerializedProperty array of object references.
    /// DeleteArrayElementAtIndex only nulls a non-null object-reference element on
    /// the first call, so we null it explicitly first to guarantee a real removal.
    /// </summary>
    private static void RemoveObjectRefArrayElement(SerializedProperty arrayProp, int index)
    {
        if (index < 0 || index >= arrayProp.arraySize) return;
        SerializedProperty elem = arrayProp.GetArrayElementAtIndex(index);
        if (elem.propertyType == SerializedPropertyType.ObjectReference && elem.objectReferenceValue != null)
            elem.objectReferenceValue = null;
        arrayProp.DeleteArrayElementAtIndex(index);
    }
}
