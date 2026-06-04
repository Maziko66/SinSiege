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

    private enum DetailTab { Editor, Library }
    private DetailTab _detailTab = DetailTab.Editor;

    private const string LevelFolder = "Assets/Prefabs/Levels";
    private const string BaseWavePath = "Assets/Scriptable Objects/Waves";
    private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
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
    private string _librarySearch = "";

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

        // Left: the wave timeline (groups and their spawner slots).
        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawWaveGroups();
        EditorGUILayout.EndVertical();

        DrawResizeHandle();

        // Right: context panel — Wave Editor or the wave Library.
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawWaveInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWavePool()
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("＋ Create New Wave", GUILayout.Height(24))) CreateWaveAsset(-1, -1);
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Refresh", GUILayout.Height(24), GUILayout.Width(64))) RefreshWavePool();
        EditorGUILayout.EndHorizontal();

        // Search
        EditorGUILayout.BeginHorizontal();
        _librarySearch = EditorGUILayout.TextField(_librarySearch, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
        {
            _librarySearch = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        bool slotSelected = _selWaveGroup >= 0 && _selWaveSlot >= 0 && !_editingFromPool;
        EditorGUILayout.HelpBox(slotSelected
            ? $"Click 'Assign' to put a wave in the selected slot (Wave {_selWaveGroup + 1}, spawner {_selWaveSlot + 1}). Click a name to edit."
            : "Click a wave's name to edit it. Select a spawner in the timeline to enable 'Assign' (or just drag a wave onto a slot).",
            MessageType.None);

        bool searching = !string.IsNullOrWhiteSpace(_librarySearch);
        string needle = _librarySearch.ToLowerInvariant();

        EditorGUILayout.Space(2);
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
                List<WaveSO> waves = _wavePool[folder]
                    .Where(w => w != null && (!searching || w.name.ToLowerInvariant().Contains(needle)))
                    .OrderBy(w => w.name)
                    .ToList();
                if (waves.Count == 0) continue;
                if (!_folderFoldouts.ContainsKey(folder)) _folderFoldouts[folder] = folder == levelName;

                bool expanded = searching || _folderFoldouts[folder];
                int assigned = waves.Count(IsWaveAssigned);
                bool isLevelFolder = folder == levelName;

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = isLevelFolder ? new Color(0.8f, 1f, 0.8f) : Color.white;
                if (!searching)
                    _folderFoldouts[folder] = EditorGUILayout.Foldout(_folderFoldouts[folder], "", true, _folderHeader);
                EditorGUILayout.LabelField($"{folder}  ({assigned}/{waves.Count})", EditorStyles.boldLabel);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUI.indentLevel++;
                    foreach (WaveSO wave in waves) DrawWavePoolItem(wave);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawWavePoolItem(WaveSO wave)
    {
        bool isAssigned = IsWaveAssigned(wave);
        bool isSelected = _selectedWave == wave && _editingFromPool;
        bool canAssign = _selWaveGroup >= 0 && _selWaveSlot >= 0 && !_editingFromPool;

        if (isSelected) GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);

        EditorGUILayout.BeginHorizontal(isSelected ? _poolItemSelected : GUIStyle.none);
        GUILayout.Space(12);

        string tick = isAssigned ? "✓ " : "";
        string label = wave.name.Length > 26 ? wave.name.Substring(0, 23) + "..." : wave.name;
        if (GUILayout.Button(tick + label, EditorStyles.label, GUILayout.ExpandWidth(true)))
            SelectWaveFromPool(wave);
        GUI.backgroundColor = Color.white;

        using (new EditorGUI.DisabledScope(!canAssign))
        {
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("Assign", GUILayout.Width(54)))
                AssignWaveToSlot(_selWaveGroup, _selWaveSlot, wave);
            GUI.backgroundColor = Color.white;
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("✕", GUILayout.Width(22))) DeleteWaveAsset(wave);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveGroups()
    {
        EditorGUILayout.BeginHorizontal("helpBox");
        EditorGUILayout.LabelField($"Total Gold {_totalGold}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Exp {_totalExp:F0}", EditorStyles.boldLabel);
        int waveCount = _waveGroupsProp?.arraySize ?? 0;
        EditorGUILayout.LabelField($"{waveCount} wave{(waveCount == 1 ? "" : "s")}",
            EditorStyles.miniLabel, GUILayout.Width(64));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

        if (_waveGroupsProp != null)
        {
            for (int g = 0; g < _waveGroupsProp.arraySize; g++)
                DrawWaveGroup(g);
        }

        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("＋ Add Wave", GUILayout.Height(26))) AddWaveGroup();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void DrawWaveGroup(int g)
    {
        SerializedProperty groupProp = _waveGroupsProp.GetArrayElementAtIndex(g);
        SerializedProperty slotsProp = groupProp.FindPropertyRelative("waveSlots");

        EditorGUILayout.BeginVertical(_groupBox);

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Wave {g + 1}", EditorStyles.boldLabel, GUILayout.Width(58));

        int gold = 0; float exp = 0f;
        for (int w = 0; w < slotsProp.arraySize; w++)
        {
            var wv = (WaveSO)slotsProp.GetArrayElementAtIndex(w).FindPropertyRelative("wave").objectReferenceValue;
            if (wv != null) { gold += wv.totalGoldValue; exp += wv.totalExpValue; }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"G {gold}  E {exp:F0}", EditorStyles.miniLabel, GUILayout.Width(86));

        using (new EditorGUI.DisabledScope(g == 0))
            if (GUILayout.Button("↑", GUILayout.Width(22))) { MoveWaveGroup(g, g - 1); return; }
        using (new EditorGUI.DisabledScope(g >= _waveGroupsProp.arraySize - 1))
            if (GUILayout.Button("↓", GUILayout.Width(22))) { MoveWaveGroup(g, g + 1); return; }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("✕", GUILayout.Width(22))) { DeleteWaveGroup(g); return; }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (slotsProp.arraySize == 0)
            EditorGUILayout.LabelField("No spawners yet — add one below.", EditorStyles.miniLabel);

        // Spawner slots, stacked full-width (no more cramped horizontal scroll).
        for (int w = 0; w < slotsProp.arraySize; w++)
            if (DrawWaveSlot(g, w, slotsProp)) return; // structural change happened

        GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
        if (GUILayout.Button("＋ Add spawner", EditorStyles.miniButton)) { AddWaveSlot(g); return; }
        GUI.backgroundColor = Color.white;

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

        EditorGUILayout.BeginHorizontal(selected ? _poolItemSelected : GUIStyle.none);

        // Route dropdown (colour-coded)
        GUI.backgroundColor = RouteColor(routeIdx);
        var routeOptions = BuildRouteOptions();
        if (routeOptions.Length > 0)
        {
            int clampedRoute = Mathf.Clamp(routeIdx, 0, routeOptions.Length - 1);
            int newRoute = EditorGUILayout.Popup(clampedRoute, routeOptions, GUILayout.Width(116));
            if (newRoute != clampedRoute)
            {
                routeProp.intValue = newRoute;
                _levelSO.ApplyModifiedProperties();
                MarkLevelDirty();
            }
        }
        else EditorGUILayout.LabelField("No Routes", GUILayout.Width(116));
        GUI.backgroundColor = Color.white;

        // Wave name — click to edit (filled) or jump to Library (empty).
        string label = wave != null ? wave.name : "drag a wave here · or pick in Library";
        if (wave != null && label.Length > 24) label = label.Substring(0, 21) + "...";
        GUI.backgroundColor = wave != null ? RouteColor(routeIdx) : new Color(0.92f, 0.92f, 0.92f);
        if (GUILayout.Button(label, selected ? _slotSelected : _slot, GUILayout.ExpandWidth(true)))
        {
            SelectWaveSlot(g, w, wave);
            _detailTab = wave != null ? DetailTab.Editor : DetailTab.Library;
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        bool remove = GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(22));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        Rect rowRect = GUILayoutUtility.GetLastRect();
        if (HandleSlotDragAndDrop(rowRect, g, w)) return true;

        if (remove) { DeleteWaveSlot(g, w); return true; }
        return false;
    }

    /// <summary>Accepts a WaveSO dragged from the Project window (or Library) onto a slot row.</summary>
    private bool HandleSlotDragAndDrop(Rect rect, int g, int w)
    {
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition)) return false;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return false;

        WaveSO dropped = DragAndDrop.objectReferences.OfType<WaveSO>().FirstOrDefault();
        DragAndDrop.visualMode = dropped != null ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

        if (e.type == EventType.DragPerform && dropped != null)
        {
            DragAndDrop.AcceptDrag();
            AssignWaveToSlot(g, w, dropped);
            e.Use();
            GUIUtility.ExitGUI();
            return true;
        }
        e.Use();
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
        // Tab toolbar: edit the current wave, or browse the library.
        _detailTab = (DetailTab)GUILayout.Toolbar((int)_detailTab,
            new[] { "Wave Editor", "Library" }, GUILayout.Height(22));
        EditorGUILayout.Space(2);

        if (_detailTab == DetailTab.Library)
        {
            DrawWavePool();
            return;
        }

        _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

        if (_selectedWave != null && _selectedWaveSO != null && _selectedWaveSO.targetObject != null)
        {
            DrawSelectedWaveInspector();
        }
        else if (_selWaveGroup >= 0 && _selWaveSlot >= 0 && !_editingFromPool)
        {
            EditorGUILayout.LabelField($"Wave {_selWaveGroup + 1} · spawner {_selWaveSlot + 1}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This spawner is empty.\n\n" +
                "• Drag a WaveSO from the Project window onto the slot, or\n" +
                "• open the Library to pick an existing wave, or\n" +
                "• create a fresh wave below.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Open Library", GUILayout.Height(24))) _detailTab = DetailTab.Library;
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("Create New Wave & Assign", GUILayout.Height(28)))
                CreateWaveAsset(_selWaveGroup, _selWaveSlot);
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a spawner or a library wave to edit.",
                _centeredGrey, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedWaveInspector()
    {
        _selectedWaveSO.Update();

        // ---- Header: context + name + ping ----
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            _editingFromPool ? "Library wave" : $"Wave {_selWaveGroup + 1} · spawner {_selWaveSlot + 1}",
            EditorStyles.miniBoldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(46)))
        {
            EditorGUIUtility.PingObject(_selectedWave);
            Selection.activeObject = _selectedWave;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.DelayedTextField(_selectedWave.name);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName) && newName != _selectedWave.name)
        {
            RenameWaveAsset(_selectedWave, newName);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.BeginHorizontal("helpBox");
        EditorGUILayout.LabelField($"Gold {_selectedWave.totalGoldValue}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Exp {_selectedWave.totalExpValue:F1}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // ---- Editable body ----
        EditorGUI.BeginChangeCheck();

        SectionHeader("Timing");
        EditorGUILayout.PropertyField(_selectedWaveSO.FindProperty("waveCooldown"),
            new GUIContent("Pre-Wave Cooldown (s)", "Countdown before this wave's group starts."));
        EditorGUILayout.PropertyField(_selectedWaveSO.FindProperty("defaultSpawnInterval"),
            new GUIContent("Default Spawn Interval (s)", "Gap between spawns when an entry doesn't override it."));

        SectionHeader("Enemy Spawns");
        DrawSpawnList(_selectedWaveSO.FindProperty("enemySpawns"), "enemy");

        SectionHeader("Horde");
        SerializedProperty hasHorde = _selectedWaveSO.FindProperty("hasHorde");
        EditorGUILayout.PropertyField(hasHorde, new GUIContent("Has Horde"));
        if (hasHorde.boolValue)
        {
            EditorGUILayout.PropertyField(_selectedWaveSO.FindProperty("hordeInterval"),
                new GUIContent("Horde Interval (s)"));
            DrawSpawnList(_selectedWaveSO.FindProperty("hordeSpawns"), "horde enemy");
        }

        if (EditorGUI.EndChangeCheck())
        {
            _selectedWaveSO.ApplyModifiedProperties();
            _selectedWave.CalculateTotalStats();
            EditorUtility.SetDirty(_selectedWave);
            RecalcTotals();
        }
        else
        {
            _selectedWaveSO.ApplyModifiedProperties();
        }
    }

    private void SectionHeader(string title)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect r = GUILayoutUtility.GetLastRect();
        EditorGUI.DrawRect(new Rect(r.x, r.yMax + 1, r.width, 1),
            EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.12f) : new Color(0, 0, 0, 0.15f));
        EditorGUILayout.Space(2);
    }

    private void DrawSpawnList(SerializedProperty list, string noun)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{list.arraySize} {noun}{(list.arraySize == 1 ? "" : "s")}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("＋ Add", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            int i = list.arraySize;
            list.InsertArrayElementAtIndex(i);
            SerializedProperty ne = list.GetArrayElementAtIndex(i);
            ne.FindPropertyRelative("enemyPrefab").objectReferenceValue = null;
            ne.FindPropertyRelative("count").intValue = 1;
            ne.FindPropertyRelative("spawnIntervalOverride").floatValue = -1f;
            ne.FindPropertyRelative("modificationMode").enumValueIndex = 0;
            ne.FindPropertyRelative("hpMultiplier").floatValue = 1f;
            ne.FindPropertyRelative("speedMultiplier").floatValue = 1f;
            ne.FindPropertyRelative("damageMultiplier").floatValue = 1f;
            ne.FindPropertyRelative("goldMultiplier").floatValue = 1f;
            ne.FindPropertyRelative("expMultiplier").floatValue = 1f;
            CommitWaveChange();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (list.arraySize == 0)
        {
            EditorGUILayout.HelpBox($"No {noun} entries yet.", MessageType.None);
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
            if (DrawSpawnEntry(list, i)) return; // structural change → frame restarted
    }

    /// <returns>true if the list changed structurally (caller must stop drawing).</returns>
    private bool DrawSpawnEntry(SerializedProperty list, int i)
    {
        SerializedProperty elem = list.GetArrayElementAtIndex(i);
        SerializedProperty enemy = elem.FindPropertyRelative("enemyPrefab");
        SerializedProperty countP = elem.FindPropertyRelative("count");
        SerializedProperty interval = elem.FindPropertyRelative("spawnIntervalOverride");
        SerializedProperty mode = elem.FindPropertyRelative("modificationMode");

        Enemy enemyObj = enemy.objectReferenceValue as Enemy;

        EditorGUILayout.BeginHorizontal(_groupBox);

        // Left: clickable enemy sprite (opens the grid picker)
        Rect spriteRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
        DrawEnemySpritePreview(spriteRect, enemyObj);
        EditorGUIUtility.AddCursorRect(spriteRect, MouseCursor.Link);
        if (GUI.Button(spriteRect, new GUIContent("", "Click to choose enemy"), GUIStyle.none))
            OpenEnemyPicker(spriteRect, enemy.propertyPath);

        EditorGUILayout.BeginVertical();

        // Row 1: index, enemy picker, reorder, delete
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(26));
        GUIContent pickContent = new GUIContent(enemyObj != null ? enemyObj.name : "Choose enemy…");
        Rect pickRect = GUILayoutUtility.GetRect(pickContent, EditorStyles.popup, GUILayout.ExpandWidth(true));
        if (GUI.Button(pickRect, pickContent, EditorStyles.popup))
            OpenEnemyPicker(pickRect, enemy.propertyPath);
        using (new EditorGUI.DisabledScope(i == 0))
            if (GUILayout.Button("↑", GUILayout.Width(22))) { list.MoveArrayElement(i, i - 1); CommitWaveChange(); return true; }
        using (new EditorGUI.DisabledScope(i >= list.arraySize - 1))
            if (GUILayout.Button("↓", GUILayout.Width(22))) { list.MoveArrayElement(i, i + 1); CommitWaveChange(); return true; }
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("✕", GUILayout.Width(22))) { list.DeleteArrayElementAtIndex(i); CommitWaveChange(); return true; }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // Row 2: count
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Count", GUILayout.Width(58));
        countP.intValue = Mathf.Max(1, EditorGUILayout.IntField(Mathf.Max(1, countP.intValue), GUILayout.Width(70)));
        GUILayout.Label("× this enemy", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Row 3: spawn interval (default vs override)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Interval", GUILayout.Width(58));
        bool custom = interval.floatValue >= 0f;
        bool newCustom = EditorGUILayout.ToggleLeft("Override", custom, GUILayout.Width(76));
        if (newCustom != custom)
            interval.floatValue = newCustom
                ? Mathf.Max(0f, _selectedWaveSO.FindProperty("defaultSpawnInterval").floatValue)
                : -1f;
        if (newCustom)
            interval.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(interval.floatValue, GUILayout.Width(70)));
        else
            EditorGUILayout.LabelField("(wave default)", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Row 4: stat modification — only show the fields that apply
        EditorGUILayout.PropertyField(mode, new GUIContent("Stat Mode"));
        var modeVal = (SpawnModMode)mode.enumValueIndex;
        if (modeVal == SpawnModMode.Multiplier)
        {
            EditorGUI.indentLevel++;
            DrawStatField(elem, "hpMultiplier", "HP ×");
            DrawStatField(elem, "speedMultiplier", "Speed ×");
            DrawStatField(elem, "damageMultiplier", "Damage ×");
            DrawStatField(elem, "goldMultiplier", "Gold ×");
            DrawStatField(elem, "expMultiplier", "Exp ×");
            EditorGUI.indentLevel--;
        }
        else if (modeVal == SpawnModMode.CustomValue)
        {
            EditorGUI.indentLevel++;
            DrawStatField(elem, "customHealth", "Health");
            DrawStatField(elem, "customSpeed", "Speed");
            DrawStatField(elem, "customDamage", "Damage");
            DrawStatField(elem, "customGold", "Gold");
            DrawStatField(elem, "customExp", "Exp");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();    // inner content column
        EditorGUILayout.EndHorizontal();  // outer _groupBox (sprite + content)
        return false;
    }

    private void DrawStatField(SerializedProperty elem, string prop, string label)
    {
        EditorGUILayout.PropertyField(elem.FindPropertyRelative(prop), new GUIContent(label));
    }

    /// <summary>Draws the enemy prefab's SpriteRenderer sprite inside the given rect.</summary>
    private void DrawEnemySpritePreview(Rect rect, Enemy enemy)
    {
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
            ? new Color(0f, 0f, 0f, 0.20f) : new Color(0f, 0f, 0f, 0.08f));

        Sprite sprite = GetEnemySprite(enemy);
        if (sprite != null && sprite.texture != null) DrawSprite(rect, sprite);
        else GUI.Label(rect, "∅", _centeredGrey);
    }

    /// <summary>Returns the prefab's sprite (own SpriteRenderer, else first in children).</summary>
    private static Sprite GetEnemySprite(Enemy enemy)
    {
        if (enemy == null) return null;
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null) sr = enemy.GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }

    /// <summary>Draws a (possibly atlassed) sprite into a rect, preserving aspect ratio.</summary>
    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;
        Texture tex = sprite.texture;
        Rect sr = sprite.rect;
        Rect uv = new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height);
        float aspect = sr.width / Mathf.Max(1f, sr.height);
        Rect fit = FitRect(rect, aspect);
        GUI.DrawTextureWithTexCoords(fit, tex, uv);
    }

    /// <summary>Centres a rect of the given aspect ratio inside an outer rect.</summary>
    private static Rect FitRect(Rect outer, float aspect)
    {
        float w = outer.width, h = outer.height;
        if (w / h > aspect) w = h * aspect;
        else h = w / aspect;
        return new Rect(outer.x + (outer.width - w) * 0.5f,
                        outer.y + (outer.height - h) * 0.5f, w, h);
    }

    /// <summary>Opens the sprite-grid enemy picker and assigns the result to the property at 'path'.</summary>
    private void OpenEnemyPicker(Rect anchor, string path)
    {
        PopupWindow.Show(anchor, new EnemyPickerPopup(picked =>
        {
            if (_selectedWaveSO == null || _selectedWaveSO.targetObject == null) return;
            SerializedProperty prop = _selectedWaveSO.FindProperty(path);
            if (prop == null) return;

            prop.objectReferenceValue = picked;
            _selectedWaveSO.ApplyModifiedProperties();
            if (_selectedWave != null)
            {
                _selectedWave.CalculateTotalStats();
                EditorUtility.SetDirty(_selectedWave);
            }
            RecalcTotals();
            Repaint();
        }));
    }

    /// <summary>
    /// Grid picker popup: shows every prefab with an Enemy component found under
    /// EnemyPrefabFolder (recursively) as a sprite thumbnail. Click one to assign.
    /// </summary>
    private class EnemyPickerPopup : PopupWindowContent
    {
        private struct Item { public Enemy enemy; public Sprite sprite; public string name; }

        private static List<Item> _items;
        private readonly System.Action<Enemy> _onPick;
        private Vector2 _scroll;
        private string _search = "";
        private GUIStyle _cellLabel;

        public EnemyPickerPopup(System.Action<Enemy> onPick)
        {
            _onPick = onPick;
            Rebuild();
        }

        public override Vector2 GetWindowSize() => new Vector2(380, 420);

        private static void Rebuild()
        {
            _items = new List<Item>();
            if (!Directory.Exists(EnemyPrefabFolder)) return;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null) continue;
                Enemy e = go.GetComponent<Enemy>();
                if (e == null) continue;
                _items.Add(new Item { enemy = e, sprite = GetEnemySprite(e), name = go.name });
            }
            _items.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }

        public override void OnGUI(Rect rect)
        {
            if (_cellLabel == null)
                _cellLabel = new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = 9 };

            if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Choose Enemy", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60))) Rebuild();
            EditorGUILayout.EndHorizontal();

            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);

            if (_items == null || _items.Count == 0)
            {
                EditorGUILayout.HelpBox($"No prefabs with an Enemy component under '{EnemyPrefabFolder}'.", MessageType.Info);
                return;
            }

            bool searching = !string.IsNullOrWhiteSpace(_search);
            string needle = searching ? _search.ToLowerInvariant() : "";

            const float cell = 84f, pad = 4f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / cell));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int shown = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (Item it in _items)
            {
                if (searching && !it.name.ToLowerInvariant().Contains(needle)) continue;
                if (shown > 0 && shown % cols == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                DrawCell(it, cell, pad);
                shown++;
            }
            EditorGUILayout.EndHorizontal();

            if (shown == 0) EditorGUILayout.LabelField("No matches.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        private void DrawCell(Item it, float cell, float pad)
        {
            Rect r = GUILayoutUtility.GetRect(cell, cell, GUILayout.Width(cell), GUILayout.Height(cell));
            bool hover = r.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(r, hover ? new Color(0.35f, 0.55f, 0.9f, 0.35f) : new Color(0f, 0f, 0f, 0.12f));

            Rect img = new Rect(r.x + pad, r.y + pad, r.width - 2 * pad, r.height - 22 - pad);
            DrawSprite(img, it.sprite);

            GUI.Label(new Rect(r.x + 1, r.yMax - 20, r.width - 2, 19), it.name, _cellLabel);

            if (GUI.Button(r, new GUIContent("", it.name), GUIStyle.none))
            {
                _onPick?.Invoke(it.enemy);
                editorWindow.Close();
            }
        }
    }

    /// <summary>Commits a structural change to the selected wave asset and restarts the frame.</summary>
    private void CommitWaveChange()
    {
        _selectedWaveSO.ApplyModifiedProperties();
        if (_selectedWave != null)
        {
            _selectedWave.CalculateTotalStats();
            EditorUtility.SetDirty(_selectedWave);
        }
        RecalcTotals();
        GUIUtility.ExitGUI();
    }

    // ---- Wave selection ----

    private void SelectWaveFromPool(WaveSO wave)
    {
        _selectedWave = wave;
        _selectedWaveSO = wave != null ? new SerializedObject(wave) : null;
        _selWaveGroup = _selWaveSlot = -1;
        _editingFromPool = true;
        _detailTab = DetailTab.Editor;   // picking a library wave jumps to the editor
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
        _detailTab = DetailTab.Library;   // new spawner is empty — show the picker
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
        _detailTab = DetailTab.Editor;    // show the freshly-assigned wave
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
