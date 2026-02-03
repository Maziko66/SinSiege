using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq; 

public class WaveEditorWindow : EditorWindow
{
    private enum EditorMode { Waves, Segments, Routes }
    private EditorMode _currentMode = EditorMode.Waves;

    private LevelData _selectedLevelData;
    private SerializedObject _serializedLevelObject;
    private SerializedProperty _waveGroupsProperty;
    private SerializedProperty _mapRoutesProperty;
    private SerializedProperty _availableSegmentsProperty;

    private List<LevelData> _cachedLevels = new List<LevelData>();
    private string[] _levelNames;
    private int _selectedLevelIndex = 0;
    
    private const string BaseWavePath = "Assets/Scriptable Objects/Waves";

    private WaveSO _selectedWave;
    private SerializedObject _serializedWaveObject;
    private int _selectedWaveGroupIndex = -1;
    private int _selectedWaveSlotIndex = -1;
    
    // Wave Pool with folder grouping
    private Dictionary<string, List<WaveSO>> _wavePoolByFolder = new Dictionary<string, List<WaveSO>>();
    private Dictionary<string, bool> _folderFoldouts = new Dictionary<string, bool>();
    private Vector2 _wavePoolScrollPosition;
    
    private int _selectedRouteIndex = -1;
    private int _selectedSegmentIndex = -1;
    private bool _showSceneHandles = true;

    private int _cachedLevelTotalGold;
    private float _cachedLevelTotalExp;

    private Vector2 _sidebarScrollPosition;
    private Vector2 _inspectorScrollPosition;
    private Vector2 _segmentAvailableScroll;
    private Vector2 _segmentSelectedScroll;
    private float _sidebarWidth = 350f;
    private bool _isResizing = false;
    private const float MinSidebarWidth = 250f;

    private GUIStyle _selectedButtonStyle;
    private GUIStyle _centeredGreyLabel;
    private GUIStyle _waveGroupBoxStyle;
    private GUIStyle _waveSlotStyle;
    private GUIStyle _selectedWaveSlotStyle;
    private GUIStyle _folderHeaderStyle;

    [MenuItem("Tools/Wave Editor")]
    public static void ShowWindow() { GetWindow<WaveEditorWindow>("Wave Editor"); }

    private void OnEnable()
    {
        RefreshLevelList();
        SceneView.duringSceneGui += OnSceneGUI; 
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI; 
        _serializedWaveObject = null;
        _serializedLevelObject = null;
    }

    private void InitializeStyles()
    {
        if (_selectedButtonStyle == null)
        {
            _selectedButtonStyle = new GUIStyle(GUI.skin.button);
            _selectedButtonStyle.normal = GUI.skin.button.active;
        }
        if (_centeredGreyLabel == null)
        {
            _centeredGreyLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 14,
                normal = { textColor = Color.gray }
            };
        }
        if (_waveGroupBoxStyle == null)
        {
            _waveGroupBoxStyle = new GUIStyle("helpBox")
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }
        if (_waveSlotStyle == null)
        {
            _waveSlotStyle = new GUIStyle(GUI.skin.button)
            { alignment = TextAnchor.MiddleLeft, fontSize = 11, fixedHeight = 22 };
        }
        if (_selectedWaveSlotStyle == null)
        {
            _selectedWaveSlotStyle = new GUIStyle(GUI.skin.button)
            { alignment = TextAnchor.MiddleLeft, fontSize = 11, fixedHeight = 22 };
            _selectedWaveSlotStyle.normal = GUI.skin.button.active;
        }
        if (_folderHeaderStyle == null)
        {
            _folderHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
        }
    }

    private void OnGUI()
    {
        InitializeStyles();
        if (Event.current.type == EventType.Layout && _selectedLevelData != null) CalculateLevelTotals();
        DrawTopBar();
        if (_selectedLevelData == null) { EditorGUILayout.HelpBox("Please select a Level to begin.", MessageType.Info); return; }
        if (_serializedLevelObject != null) _serializedLevelObject.Update();

        switch (_currentMode)
        {
            case EditorMode.Waves: DrawWaveEditor(); break;
            case EditorMode.Segments: DrawSegmentEditor(); break;
            case EditorMode.Routes: DrawPathEditor(); break;
        }
        if (_serializedLevelObject != null) _serializedLevelObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_selectedLevelData == null || !_showSceneHandles) return;
        if (_currentMode == EditorMode.Segments) DrawSegmentModeHandles();
        else if (_currentMode == EditorMode.Routes) DrawRouteModeHandles();
        else if (_currentMode == EditorMode.Waves) DrawWaveModeHandles();
    }

    private void DrawRouteModeHandles()
    {
        var routes = _selectedLevelData.MapRoutes;
        if (routes == null) return;
        for (int r = 0; r < routes.Count; r++)
        {
            bool isSelectedRoute = (r == _selectedRouteIndex);
            Color routeColor = isSelectedRoute ? Color.green : new Color(1f, 1f, 1f, 0.3f);
            MapRoute route = routes[r];
            if (route.pathSegments == null) continue;
            foreach (var segment in route.pathSegments)
            {
                if (segment.waypoints == null) continue;
                for (int w = 0; w < segment.waypoints.Count; w++)
                {
                    Transform point = segment.waypoints[w];
                    if (point == null) continue;
                    if (w < segment.waypoints.Count - 1 && segment.waypoints[w+1] != null)
                    {
                        Handles.color = routeColor;
                        Handles.DrawLine(point.position, segment.waypoints[w+1].position, 2f);
                    }
                    if (isSelectedRoute)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos = Handles.PositionHandle(point.position, Quaternion.identity);
                        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(point, "Move Waypoint"); point.position = newPos; }
                        Handles.Label(point.position + Vector3.up * 0.5f, $"{segment.segmentName}_{w}");
                    }
                }
            }
        }
    }

    private void DrawSegmentModeHandles()
    {
        var pool = _selectedLevelData.AvailableSegments;
        if (pool == null || _selectedSegmentIndex < 0 || _selectedSegmentIndex >= pool.Count) return;
        PathSegment seg = pool[_selectedSegmentIndex];
        if (seg.waypoints == null) return;
        Handles.color = Color.cyan;
        for (int w = 0; w < seg.waypoints.Count; w++)
        {
            Transform point = seg.waypoints[w];
            if (point == null) continue;
            if (w < seg.waypoints.Count - 1 && seg.waypoints[w + 1] != null)
                Handles.DrawLine(point.position, seg.waypoints[w + 1].position, 2f);
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(point.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(point, "Move Waypoint"); point.position = newPos; }
            Handles.Label(point.position + Vector3.up * 0.5f, $"{seg.segmentName}_{w}");
        }
    }

    private void DrawWaveModeHandles()
    {
        if (_selectedWave == null || _serializedWaveObject == null) return;
        var routes = _selectedLevelData.MapRoutes;
        if (routes == null) return;
        SerializedProperty routeIndexProp = _serializedWaveObject.FindProperty("routeIndex");
        if (routeIndexProp == null) return;
        int routeIndex = routeIndexProp.intValue;
        if (routeIndex < 0 || routeIndex >= routes.Count) return;
        MapRoute route = routes[routeIndex];
        if (route.pathSegments == null) return;
        Handles.color = Color.yellow;
        foreach (var segment in route.pathSegments)
        {
            if (segment.waypoints == null) continue;
            for (int w = 0; w < segment.waypoints.Count; w++)
            {
                Transform point = segment.waypoints[w];
                if (point == null) continue;
                if (w < segment.waypoints.Count - 1 && segment.waypoints[w + 1] != null)
                    Handles.DrawLine(point.position, segment.waypoints[w + 1].position, 2f);
                Handles.DrawSolidDisc(point.position, Vector3.up, 0.15f);
                Handles.Label(point.position + Vector3.up * 0.5f, $"{segment.segmentName}_{w}");
            }
        }
        if (route.pathSegments.Count > 0 && route.pathSegments[0].spawnPoint != null)
        {
            Handles.color = Color.green;
            Transform spawn = route.pathSegments[0].spawnPoint;
            Handles.DrawSolidDisc(spawn.position, Vector3.up, 0.3f);
            Handles.Label(spawn.position + Vector3.up * 0.7f, "SPAWN");
        }
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Level:", GUILayout.Width(80));
        EditorGUI.BeginChangeCheck();
        _selectedLevelIndex = EditorGUILayout.Popup(_selectedLevelIndex, _levelNames);
        if (EditorGUI.EndChangeCheck())
            if (_cachedLevels.Count > 0 && _selectedLevelIndex >= 0 && _selectedLevelIndex < _cachedLevels.Count)
                ChangeSelectedLevel(_cachedLevels[_selectedLevelIndex]);
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) { RefreshLevelList(); RefreshWavePool(); }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        LevelData manualSelection = (LevelData)EditorGUILayout.ObjectField("Manual Drag:", _selectedLevelData, typeof(LevelData), true);
        if (EditorGUI.EndChangeCheck()) { ChangeSelectedLevel(manualSelection); UpdateDropdownIndexFromSelection(); }

        if (_selectedLevelData != null)
            if (GUILayout.Button("Show Level in Project", GUILayout.Height(20)))
            { EditorGUIUtility.PingObject(_selectedLevelData); Selection.activeObject = _selectedLevelData; }
        
        EditorGUILayout.Space(5);
        _currentMode = (EditorMode)GUILayout.Toolbar((int)_currentMode, new string[] { "Wave Editor", "Segment Editor", "Route Editor" }, GUILayout.Height(25));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawPathEditor()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawRoutesSidebar();
        EditorGUILayout.EndVertical();
        ResizeHandle();
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawRouteInspector();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRoutesSidebar()
    {
        EditorGUILayout.LabelField("Map Routes", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Create New Route", GUILayout.Height(30))) CreateNewRoute();
        EditorGUILayout.Space();
        _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition);
        if (_mapRoutesProperty != null)
        {
            int deleteIndex = -1;
            for (int i = 0; i < _mapRoutesProperty.arraySize; i++)
            {
                SerializedProperty routeProp = _mapRoutesProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = routeProp.FindPropertyRelative("routeName");
                string name = nameProp.stringValue;
                if (string.IsNullOrEmpty(name)) name = $"Route {i}";
                EditorGUILayout.BeginHorizontal();
                GUIStyle btnStyle = (i == _selectedRouteIndex) ? _selectedButtonStyle : GUI.skin.button;
                if (GUILayout.Button(name, btnStyle, GUILayout.Height(25)))
                { _selectedRouteIndex = i; SceneView.RepaintAll(); GUI.FocusControl(null); }
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25))) deleteIndex = i;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            if (deleteIndex >= 0) DeleteRoute(deleteIndex);
        }
        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        _showSceneHandles = EditorGUILayout.Toggle("Show Scene Handles", _showSceneHandles);
    }

    private void DrawRouteInspector()
    {
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);
        if (_selectedRouteIndex != -1 && _mapRoutesProperty != null && _selectedRouteIndex < _mapRoutesProperty.arraySize)
        {
            SerializedProperty routeProp = _mapRoutesProperty.GetArrayElementAtIndex(_selectedRouteIndex);
            EditorGUILayout.LabelField("Route Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(routeProp.FindPropertyRelative("routeName"));
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Segment Sequence", EditorStyles.boldLabel);
            SerializedProperty segmentsProp = routeProp.FindPropertyRelative("pathSegments");
            if (segmentsProp.arraySize > 0)
            {
                SerializedProperty firstSeg = segmentsProp.GetArrayElementAtIndex(0);
                Transform sp = (Transform)firstSeg.FindPropertyRelative("spawnPoint").objectReferenceValue;
                if (sp == null) EditorGUILayout.HelpBox("First Segment must have a Spawn Point assigned!", MessageType.Error);
            }
            else EditorGUILayout.HelpBox("Route is empty. Add a starting segment with a Spawn Point.", MessageType.Info);
            for (int i = 0; i < segmentsProp.arraySize; i++)
            {
                SerializedProperty segProp = segmentsProp.GetArrayElementAtIndex(i);
                DrawSegmentInRouteUI(segProp, segmentsProp, i);
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.Space(5);
            if (_availableSegmentsProperty != null && _availableSegmentsProperty.arraySize > 0)
            {
                if (GUILayout.Button("+ Add Segment from Pool", GUILayout.Height(30)))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int s = 0; s < _availableSegmentsProperty.arraySize; s++)
                    {
                        SerializedProperty savedSeg = _availableSegmentsProperty.GetArrayElementAtIndex(s);
                        string sName = savedSeg.FindPropertyRelative("segmentName").stringValue;
                        Transform sSpawn = (Transform)savedSeg.FindPropertyRelative("spawnPoint").objectReferenceValue;
                        string menuPath = sName;
                        if (sSpawn != null) menuPath += " (Has Spawn)";
                        int indexCopy = s; 
                        menu.AddItem(new GUIContent(menuPath), false, () => { AddSegmentFromPoolToRoute(_selectedRouteIndex, indexCopy); });
                    }
                    menu.ShowAsContext();
                }
            }
            else EditorGUILayout.HelpBox("No Segments available in Pool.", MessageType.Warning);
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a Route to edit.", _centeredGreyLabel, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawSegmentInRouteUI(SerializedProperty segmentProp, SerializedProperty listProp, int index)
    {
        EditorGUILayout.BeginHorizontal("helpBox");
        SerializedProperty nameProp = segmentProp.FindPropertyRelative("segmentName");
        SerializedProperty spawnProp = segmentProp.FindPropertyRelative("spawnPoint");
        bool hasSpawn = spawnProp.objectReferenceValue != null;
        string label = $"{index + 1}. {nameProp.stringValue}";
        if (hasSpawn) label += " [Spawn]";
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Up", GUILayout.Width(25)) && index > 0) listProp.MoveArrayElement(index, index - 1);
        if (GUILayout.Button("Dn", GUILayout.Width(25)) && index < listProp.arraySize - 1) listProp.MoveArrayElement(index, index + 1);
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("X", GUILayout.Width(25))) listProp.DeleteArrayElementAtIndex(index);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpawnPointSelector(SerializedProperty spawnProp)
    {
        Transform levelTransform = _selectedLevelData.transform;
        Transform spawnContainer = levelTransform.Find("Spawn Points");
        if (spawnContainer == null) { EditorGUILayout.PropertyField(spawnProp); return; }
        List<string> options = new List<string> { "None" };
        List<Transform> values = new List<Transform> { null };
        foreach (Transform child in spawnContainer) { options.Add(child.name); values.Add(child); }
        Transform currentVal = (Transform)spawnProp.objectReferenceValue;
        int currentIndex = 0;
        if (currentVal != null) { int found = values.IndexOf(currentVal); if (found != -1) currentIndex = found; }
        int newIndex = EditorGUILayout.Popup("Spawn Point", currentIndex, options.ToArray());
        if (newIndex != currentIndex) spawnProp.objectReferenceValue = values[newIndex];
    }

    private void CreateNewRoute()
    {
        if (_selectedLevelData == null) return;
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null) { Debug.LogError("LevelData missing!"); return; }
            MapRoute newRoute = new MapRoute { routeName = "New Route", pathSegments = new List<PathSegment>() };
            dataOnPrefab.MapRoutes.Add(newRoute);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            _selectedRouteIndex = dataOnPrefab.MapRoutes.Count - 1;
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        ReloadSelectedLevel();
    }

    private void DeleteRoute(int index)
    {
        if (_selectedLevelData == null) return;
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null || dataOnPrefab.MapRoutes == null) return;
            if (index >= 0 && index < dataOnPrefab.MapRoutes.Count)
            {
                dataOnPrefab.MapRoutes.RemoveAt(index);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                if (_selectedRouteIndex >= index) _selectedRouteIndex = -1;
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        ReloadSelectedLevel();
    }

    private void DrawSegmentEditor()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawSegmentsSidebar();
        EditorGUILayout.EndVertical();
        ResizeHandle();
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        if (_selectedSegmentIndex != -1 && _availableSegmentsProperty != null && _selectedSegmentIndex < _availableSegmentsProperty.arraySize)
            DrawDualListSegmentInspector();
        else { GUILayout.FlexibleSpace(); EditorGUILayout.LabelField("Select a Segment to edit.", _centeredGreyLabel); GUILayout.FlexibleSpace(); }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSegmentsSidebar()
    {
        EditorGUILayout.LabelField("Segment Pool", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Create New Segment", GUILayout.Height(30))) CreateNewSegmentInPool();
        EditorGUILayout.Space();
        _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition);
        if (_availableSegmentsProperty != null)
        {
            int deleteIndex = -1;
            for (int i = 0; i < _availableSegmentsProperty.arraySize; i++)
            {
                SerializedProperty segProp = _availableSegmentsProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = segProp.FindPropertyRelative("segmentName");
                string name = nameProp.stringValue;
                if (string.IsNullOrEmpty(name)) name = $"Segment {i}";
                EditorGUILayout.BeginHorizontal();
                GUIStyle btnStyle = (i == _selectedSegmentIndex) ? _selectedButtonStyle : GUI.skin.button;
                if (GUILayout.Button(name, btnStyle, GUILayout.Height(25)))
                { _selectedSegmentIndex = i; SceneView.RepaintAll(); GUI.FocusControl(null); }
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25))) deleteIndex = i;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            if (deleteIndex >= 0) DeleteSegmentFromPool(deleteIndex);
        }
        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();
        _showSceneHandles = EditorGUILayout.Toggle("Show Scene Handles", _showSceneHandles);
    }

    private void DrawDualListSegmentInspector()
    {
        SerializedProperty segProp = _availableSegmentsProperty.GetArrayElementAtIndex(_selectedSegmentIndex);
        SerializedProperty nameProp = segProp.FindPropertyRelative("segmentName");
        SerializedProperty spawnProp = segProp.FindPropertyRelative("spawnPoint");
        SerializedProperty pointsProp = segProp.FindPropertyRelative("waypoints");

        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Segment Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nameProp);
        DrawSpawnPointSelector(spawnProp);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        float availableHeight = Mathf.Max(position.height - 180f, 200f);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(availableHeight));
        EditorGUILayout.BeginVertical("box", GUILayout.Width(300));
        EditorGUILayout.LabelField("Available in Scene", EditorStyles.boldLabel);
        Transform waypointsRoot = _selectedLevelData.transform.Find("Waypoints");
        if (waypointsRoot != null)
        {
            _segmentAvailableScroll = EditorGUILayout.BeginScrollView(_segmentAvailableScroll);
            foreach (Transform child in waypointsRoot)
            {
                bool alreadyAdded = false;
                for (int i = 0; i < pointsProp.arraySize; i++)
                    if (pointsProp.GetArrayElementAtIndex(i).objectReferenceValue == child) { alreadyAdded = true; break; }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(child.name, GUILayout.MinWidth(50));
                GUI.enabled = !alreadyAdded;
                if (GUILayout.Button("Add >", GUILayout.Width(50)))
                {
                    pointsProp.InsertArrayElementAtIndex(pointsProp.arraySize);
                    pointsProp.GetArrayElementAtIndex(pointsProp.arraySize - 1).objectReferenceValue = child;
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Selected Waypoints", EditorStyles.boldLabel);
        _segmentSelectedScroll = EditorGUILayout.BeginScrollView(_segmentSelectedScroll);
        for (int i = 0; i < pointsProp.arraySize; i++)
        {
            SerializedProperty pt = pointsProp.GetArrayElementAtIndex(i);
            Transform t = (Transform)pt.objectReferenceValue;
            string label = t != null ? t.name : "(Null)";
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i}. {label}", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Up", GUILayout.Width(30)) && i > 0) pointsProp.MoveArrayElement(i, i - 1);
            if (GUILayout.Button("Dn", GUILayout.Width(30)) && i < pointsProp.arraySize - 1) pointsProp.MoveArrayElement(i, i + 1);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("X", GUILayout.Width(25))) pointsProp.DeleteArrayElementAtIndex(i);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void CreateNewSegmentInPool()
    {
        if (_selectedLevelData == null) return;
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null) return;
            SerializedObject tempSO = new SerializedObject(dataOnPrefab);
            SerializedProperty segmentsProp = tempSO.FindProperty("availableSegments");
            int index = segmentsProp.arraySize;
            segmentsProp.InsertArrayElementAtIndex(index);
            SerializedProperty newSeg = segmentsProp.GetArrayElementAtIndex(index);
            newSeg.FindPropertyRelative("segmentName").stringValue = "New_Segment_" + index;
            newSeg.FindPropertyRelative("waypoints").ClearArray();
            newSeg.FindPropertyRelative("spawnPoint").objectReferenceValue = null;
            tempSO.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            _selectedSegmentIndex = index;
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        ReloadSelectedLevel();
    }

    private void DeleteSegmentFromPool(int index)
    {
        if (_selectedLevelData == null) return;
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null) return;
            SerializedObject tempSO = new SerializedObject(dataOnPrefab);
            SerializedProperty segmentsProp = tempSO.FindProperty("availableSegments");
            if (index >= 0 && index < segmentsProp.arraySize)
            {
                segmentsProp.DeleteArrayElementAtIndex(index);
                tempSO.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                if (_selectedSegmentIndex >= index) _selectedSegmentIndex = -1;
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        ReloadSelectedLevel();
    }

    private void AddSegmentFromPoolToRoute(int routeIndex, int poolIndex)
    {
        if (_selectedLevelData == null) return;
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null || dataOnPrefab.AvailableSegments == null || dataOnPrefab.MapRoutes == null) return;
            if (poolIndex < 0 || poolIndex >= dataOnPrefab.AvailableSegments.Count) return;
            if (routeIndex < 0 || routeIndex >= dataOnPrefab.MapRoutes.Count) return;
            PathSegment srcSeg = dataOnPrefab.AvailableSegments[poolIndex];
            MapRoute route = dataOnPrefab.MapRoutes[routeIndex];
            PathSegment newSeg = new PathSegment
            {
                segmentName = srcSeg.segmentName,
                spawnPoint = srcSeg.spawnPoint,
                waypoints = new List<Transform>(srcSeg.waypoints ?? new List<Transform>())
            };
            route.pathSegments.Add(newSeg);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        ReloadSelectedLevel();
    }

    // ==========================================================================================
    //                                      WAVE EDITOR LOGIC
    // ==========================================================================================

    private void DrawWaveEditor()
    {
        EditorGUILayout.BeginHorizontal();
        
        // Left panel - Wave Pool with folder grouping
        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.ExpandHeight(true));
        DrawWavePoolPanel();
        EditorGUILayout.EndVertical();
        
        // Middle panel - Wave Groups
        EditorGUILayout.BeginVertical(GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawWaveSidebar();
        EditorGUILayout.EndVertical();
        
        ResizeHandle();
        
        // Right panel - Wave Inspector
        DrawWaveInspector();
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWavePoolPanel()
    {
        EditorGUILayout.LabelField("Available Waves", EditorStyles.boldLabel);
        
        if (GUILayout.Button("+ Create New Wave", GUILayout.Height(25))) CreateNewWaveAsset();
        if (GUILayout.Button("Refresh Pool", GUILayout.Height(20))) RefreshWavePool();
        
        // Expand/Collapse All buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft))
        {
            var keys = _folderFoldouts.Keys.ToList();
            foreach (var key in keys) _folderFoldouts[key] = true;
        }
        if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight))
        {
            var keys = _folderFoldouts.Keys.ToList();
            foreach (var key in keys) _folderFoldouts[key] = false;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        _wavePoolScrollPosition = EditorGUILayout.BeginScrollView(_wavePoolScrollPosition);
        
        if (_wavePoolByFolder.Count == 0)
            EditorGUILayout.HelpBox("No waves found.\nCreate new or check path.", MessageType.Info);
        else
        {
            // Sort folders - put level-specific folder first, then others alphabetically
            var sortedFolders = _wavePoolByFolder.Keys.OrderBy(f => 
            {
                if (_selectedLevelData != null && f == _selectedLevelData.name) return "0" + f;
                return "1" + f;
            }).ToList();
            
            foreach (var folderName in sortedFolders)
            {
                var wavesInFolder = _wavePoolByFolder[folderName];
                if (wavesInFolder.Count == 0) continue;
                
                // Ensure foldout state exists
                if (!_folderFoldouts.ContainsKey(folderName))
                    _folderFoldouts[folderName] = (_selectedLevelData != null && folderName == _selectedLevelData.name);
                
                // Count assigned waves in this folder
                int assignedCount = wavesInFolder.Count(w => IsWaveAssignedToAnyGroup(w));
                
                // Draw folder header
                EditorGUILayout.BeginHorizontal();
                
                // Folder icon and foldout
                bool isLevelFolder = (_selectedLevelData != null && folderName == _selectedLevelData.name);
                GUI.backgroundColor = isLevelFolder ? new Color(0.8f, 1f, 0.8f) : Color.white;
                
                _folderFoldouts[folderName] = EditorGUILayout.Foldout(_folderFoldouts[folderName], "", true, _folderHeaderStyle);
                
                // Folder name with count
                string folderLabel = $"📁 {folderName} ({assignedCount}/{wavesInFolder.Count})";
                EditorGUILayout.LabelField(folderLabel, EditorStyles.boldLabel);
                
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                
                // Draw waves if expanded
                if (_folderFoldouts[folderName])
                {
                    EditorGUI.indentLevel++;
                    foreach (var wave in wavesInFolder.OrderBy(w => w.name))
                    {
                        if (wave == null) continue;
                        DrawWavePoolItem(wave);
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(2);
            }
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Select empty slot, click '>' to assign.", MessageType.None);
    }

    private void DrawWavePoolItem(WaveSO wave)
    {
        bool isAssigned = IsWaveAssignedToAnyGroup(wave);
        
        EditorGUILayout.BeginHorizontal();
        
        // Indent space
        GUILayout.Space(15);
        
        string label = wave.name;
        if (label.Length > 30) label = label.Substring(0, 27) + "...";
        
        if (isAssigned)
        {
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label($"✓ {label}", GUILayout.ExpandWidth(true));
        }
        else
        {
            GUILayout.Label(label, GUILayout.ExpandWidth(true));
        }
        
        // Route badge
        GUI.backgroundColor = GetRouteColor(wave.routeIndex);
        GUILayout.Label($"R{wave.routeIndex}", EditorStyles.miniButton, GUILayout.Width(25));
        GUI.backgroundColor = Color.white;
        
        // Add button - only enabled if an empty slot is selected
        GUI.enabled = (_selectedWaveGroupIndex >= 0 && _selectedWaveSlotIndex >= 0 && _selectedWave == null);
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button(">", GUILayout.Width(22)))
            AssignWaveToSlot(_selectedWaveGroupIndex, _selectedWaveSlotIndex, wave);
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveSidebar()
    {
        EditorGUILayout.BeginHorizontal("helpBox");
        EditorGUILayout.LabelField($"Total Gold: {_cachedLevelTotalGold}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Exp: {_cachedLevelTotalExp:F1}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Wave Groups", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add Wave Group", GUILayout.Height(25))) AddNewWaveGroup();

        EditorGUILayout.Space();
        _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition);

        if (_waveGroupsProperty != null)
        {
            int deleteGroupIndex = -1;
            
            for (int g = 0; g < _waveGroupsProperty.arraySize; g++)
            {
                SerializedProperty groupProp = _waveGroupsProperty.GetArrayElementAtIndex(g);
                SerializedProperty waveSetProp = groupProp.FindPropertyRelative("waveSet");
                
                EditorGUILayout.BeginVertical(_waveGroupBoxStyle);
                EditorGUILayout.BeginHorizontal();
                
                bool isSelectedGroup = (g == _selectedWaveGroupIndex);
                GUI.backgroundColor = isSelectedGroup ? new Color(0.7f, 0.9f, 0.7f) : Color.white;
                EditorGUILayout.LabelField($"Wave {g + 1}", EditorStyles.boldLabel, GUILayout.Width(60));
                
                int groupGold = 0; float groupExp = 0f;
                for (int w = 0; w < waveSetProp.arraySize; w++)
                {
                    WaveSO wave = (WaveSO)waveSetProp.GetArrayElementAtIndex(w).objectReferenceValue;
                    if (wave != null) { groupGold += wave.totalGoldValue; groupExp += wave.totalExpValue; }
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"G:{groupGold} E:{groupExp:F0}", GUILayout.Width(80));
                
                if (GUILayout.Button("↑", GUILayout.Width(20)) && g > 0)
                {
                    _waveGroupsProperty.MoveArrayElement(g, g - 1);
                    if (_selectedWaveGroupIndex == g) _selectedWaveGroupIndex = g - 1;
                    else if (_selectedWaveGroupIndex == g - 1) _selectedWaveGroupIndex = g;
                }
                if (GUILayout.Button("↓", GUILayout.Width(20)) && g < _waveGroupsProperty.arraySize - 1)
                {
                    _waveGroupsProperty.MoveArrayElement(g, g + 1);
                    if (_selectedWaveGroupIndex == g) _selectedWaveGroupIndex = g + 1;
                    else if (_selectedWaveGroupIndex == g + 1) _selectedWaveGroupIndex = g;
                }
                
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(22))) deleteGroupIndex = g;
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                int deleteSlotIndex = -1, deleteSlotGroupIndex = -1;
                
                for (int w = 0; w < waveSetProp.arraySize; w++)
                {
                    SerializedProperty waveProp = waveSetProp.GetArrayElementAtIndex(w);
                    WaveSO waveRef = (WaveSO)waveProp.objectReferenceValue;
                    bool isSelected = (g == _selectedWaveGroupIndex && w == _selectedWaveSlotIndex);
                    GUIStyle slotStyle = isSelected ? _selectedWaveSlotStyle : _waveSlotStyle;
                    
                    string slotLabel = waveRef != null ? $"R{waveRef.routeIndex}: {waveRef.name}" : "(Empty)";
                    if (slotLabel.Length > 15) slotLabel = slotLabel.Substring(0, 12) + "...";
                    
                    EditorGUILayout.BeginVertical(GUILayout.Width(100));
                    GUI.backgroundColor = waveRef != null ? GetRouteColor(waveRef.routeIndex) : new Color(0.9f, 0.9f, 0.9f);
                    if (GUILayout.Button(slotLabel, slotStyle, GUILayout.Width(95))) SelectWaveSlot(g, w, waveRef);
                    GUI.backgroundColor = Color.white;
                    
                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("×", GUILayout.Width(95), GUILayout.Height(16)))
                    { deleteSlotIndex = w; deleteSlotGroupIndex = g; }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndVertical();
                }
                
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(38))) AddWaveSlotToGroup(g);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                
                if (deleteSlotIndex >= 0 && deleteSlotGroupIndex == g) DeleteWaveSlot(g, deleteSlotIndex);
                EditorGUILayout.EndVertical();
            }
            
            if (deleteGroupIndex >= 0) DeleteWaveGroup(deleteGroupIndex);
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawWaveInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedWave != null && _serializedWaveObject != null)
        {
            _serializedWaveObject.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Editing:", GUILayout.Width(50));
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.DelayedTextField(_selectedWave.name, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName) && newName != _selectedWave.name)
                RenameWaveAsset(_selectedWave, newName);
            if (GUILayout.Button("Show in Project", GUILayout.Width(110)))
            { EditorGUIUtility.PingObject(_selectedWave); Selection.activeObject = _selectedWave; }
            EditorGUILayout.EndHorizontal();
            
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
                if (iterator.name == "m_Script" || iterator.name == "totalGoldValue" || iterator.name == "totalExpValue") continue;
                if (iterator.name == "routeIndex")
                {
                    List<string> routeNames = new List<string>();
                    var routes = _selectedLevelData.MapRoutes;
                    if (routes != null) for (int r = 0; r < routes.Count; r++) routeNames.Add($"{r}: {routes[r].routeName}");
                    if (routeNames.Count > 0)
                    {
                        int current = iterator.intValue;
                        int selected = EditorGUILayout.Popup("Route", current, routeNames.ToArray());
                        if (selected != current) iterator.intValue = selected;
                    }
                    else { EditorGUILayout.HelpBox("No Routes defined!", MessageType.Warning); iterator.intValue = -1; }
                }
                else EditorGUILayout.PropertyField(iterator, true);
            }
            _serializedWaveObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            { _selectedWave.CalculateTotalStats(); EditorUtility.SetDirty(_selectedWave); CalculateLevelTotals(); }
        }
        else if (_selectedWaveGroupIndex >= 0 && _selectedWaveSlotIndex >= 0)
        {
            EditorGUILayout.LabelField("Empty Wave Slot", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select from 'Available Waves' and click '>' or drag below.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            WaveSO assignedWave = (WaveSO)EditorGUILayout.ObjectField("Drag Wave Here", null, typeof(WaveSO), false);
            if (EditorGUI.EndChangeCheck() && assignedWave != null)
                AssignWaveToSlot(_selectedWaveGroupIndex, _selectedWaveSlotIndex, assignedWave);
            EditorGUILayout.Space();
            if (GUILayout.Button("Create New Wave", GUILayout.Height(30)))
                CreateNewWaveForSlot(_selectedWaveGroupIndex, _selectedWaveSlotIndex);
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a Wave to edit.", _centeredGreyLabel, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ==========================================================================================
    //                                      HELPERS
    // ==========================================================================================

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
        _levelNames = _cachedLevels.Count > 0 ? _cachedLevels.Select(l => l.name).ToArray() : new string[] { "No Levels Found" };
        UpdateDropdownIndexFromSelection();
    }

    private void RefreshWavePool()
    {
        _wavePoolByFolder.Clear();
        if (_selectedLevelData == null) return;
        
        // Search all subfolders under BaseWavePath
        if (!Directory.Exists(BaseWavePath)) return;
        
        // Get all WaveSO assets
        string[] guids = AssetDatabase.FindAssets("t:WaveSO", new string[] { BaseWavePath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WaveSO wave = AssetDatabase.LoadAssetAtPath<WaveSO>(path);
            if (wave == null) continue;
            
            // Extract folder name from path
            string folderPath = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(folderPath);
            
            // If it's directly in BaseWavePath, use "Shared" as folder name
            if (folderPath == BaseWavePath.Replace("/", "\\") || folderPath == BaseWavePath)
                folderName = "Shared";
            
            if (!_wavePoolByFolder.ContainsKey(folderName))
                _wavePoolByFolder[folderName] = new List<WaveSO>();
            
            _wavePoolByFolder[folderName].Add(wave);
        }
        
        // Initialize foldout states for new folders
        foreach (var folder in _wavePoolByFolder.Keys)
        {
            if (!_folderFoldouts.ContainsKey(folder))
            {
                // Auto-expand the current level's folder
                _folderFoldouts[folder] = (_selectedLevelData != null && folder == _selectedLevelData.name);
            }
        }
    }

    private bool IsWaveAssignedToAnyGroup(WaveSO wave)
    {
        if (_selectedLevelData == null || _selectedLevelData.WaveGroups == null) return false;
        foreach (var group in _selectedLevelData.WaveGroups)
            if (group.waveSet != null)
                foreach (var w in group.waveSet)
                    if (w == wave) return true;
        return false;
    }

    private Color GetRouteColor(int routeIndex)
    {
        Color[] routeColors = {
            new Color(1f, 0.6f, 0.6f), new Color(0.6f, 0.8f, 1f), new Color(0.6f, 1f, 0.6f),
            new Color(1f, 1f, 0.6f), new Color(1f, 0.6f, 1f), new Color(0.6f, 1f, 1f),
            new Color(1f, 0.8f, 0.6f), new Color(0.8f, 0.6f, 1f),
        };
        return routeIndex < 0 ? Color.gray : routeColors[routeIndex % routeColors.Length];
    }

    private void ChangeSelectedLevel(LevelData newLevel)
    {
        _selectedLevelData = newLevel;
        _selectedWave = null;
        _selectedWaveGroupIndex = -1;
        _selectedWaveSlotIndex = -1;
        _serializedWaveObject = null;
        _selectedRouteIndex = -1;
        _selectedSegmentIndex = -1;

        if (_selectedLevelData != null)
        {
            _serializedLevelObject = new SerializedObject(_selectedLevelData);
            _waveGroupsProperty = _serializedLevelObject.FindProperty("waveGroups");
            _mapRoutesProperty = _serializedLevelObject.FindProperty("mapRoutes");
            _availableSegmentsProperty = _serializedLevelObject.FindProperty("availableSegments");
            CalculateLevelTotals();
            RefreshWavePool();
            
            // Auto-expand the level's folder
            if (_folderFoldouts.ContainsKey(_selectedLevelData.name))
                _folderFoldouts[_selectedLevelData.name] = true;
        }
        else
        {
            _serializedLevelObject = null;
            _waveGroupsProperty = null;
            _mapRoutesProperty = null;
            _availableSegmentsProperty = null;
            _wavePoolByFolder.Clear();
        }
    }

    private void ReloadSelectedLevel()
    {
        if (_selectedLevelData == null) return;
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        int routeIdx = _selectedRouteIndex, segmentIdx = _selectedSegmentIndex;
        int groupIdx = _selectedWaveGroupIndex, slotIdx = _selectedWaveSlotIndex;
        
        AssetDatabase.ImportAsset(assetPath);
        LevelData reloaded = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        _selectedLevelData = null;
        ChangeSelectedLevel(reloaded);
        
        if (_mapRoutesProperty != null && routeIdx >= 0 && routeIdx < _mapRoutesProperty.arraySize) _selectedRouteIndex = routeIdx;
        if (_availableSegmentsProperty != null && segmentIdx >= 0 && segmentIdx < _availableSegmentsProperty.arraySize) _selectedSegmentIndex = segmentIdx;
        if (_waveGroupsProperty != null && groupIdx >= 0 && groupIdx < _waveGroupsProperty.arraySize)
        {
            _selectedWaveGroupIndex = groupIdx;
            SerializedProperty groupProp = _waveGroupsProperty.GetArrayElementAtIndex(groupIdx);
            SerializedProperty waveSetProp = groupProp.FindPropertyRelative("waveSet");
            if (slotIdx >= 0 && slotIdx < waveSetProp.arraySize)
            {
                _selectedWaveSlotIndex = slotIdx;
                WaveSO wave = (WaveSO)waveSetProp.GetArrayElementAtIndex(slotIdx).objectReferenceValue;
                if (wave != null) { _selectedWave = wave; _serializedWaveObject = new SerializedObject(wave); }
            }
        }
        Repaint();
    }

    private void UpdateDropdownIndexFromSelection()
    {
        if (_selectedLevelData != null && _cachedLevels.Contains(_selectedLevelData))
            _selectedLevelIndex = _cachedLevels.IndexOf(_selectedLevelData);
        else
        {
            _selectedLevelIndex = 0;
            if (_selectedLevelData == null && _cachedLevels.Count > 0) ChangeSelectedLevel(_cachedLevels[0]);
        }
    }

    private void CalculateLevelTotals()
    {
        _cachedLevelTotalGold = 0;
        _cachedLevelTotalExp = 0;
        if (_selectedLevelData == null || _selectedLevelData.WaveGroups == null) return;
        foreach (WaveGroup group in _selectedLevelData.WaveGroups)
            if (group.waveSet != null)
                foreach (WaveSO wave in group.waveSet)
                    if (wave != null) { _cachedLevelTotalGold += wave.totalGoldValue; _cachedLevelTotalExp += wave.totalExpValue; }
    }

    private void ResizeHandle()
    {
        Rect resizeRect = GUILayoutUtility.GetRect(5f, 0f, GUILayout.Width(5f), GUILayout.ExpandHeight(true));
        if (Event.current.type == EventType.Repaint)
        {
            Color splitterColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(new Rect(resizeRect.x + 2, resizeRect.y, 1, resizeRect.height), splitterColor);
        }
        EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);
        Event e = Event.current;
        if (e.type == EventType.MouseDown && resizeRect.Contains(e.mousePosition)) { _isResizing = true; e.Use(); }
        if (_isResizing) { _sidebarWidth += e.delta.x; _sidebarWidth = Mathf.Clamp(_sidebarWidth, MinSidebarWidth, position.width - 100f); Repaint(); }
        if (e.type == EventType.MouseUp) _isResizing = false;
    }

    private void SelectWaveSlot(int groupIndex, int slotIndex, WaveSO wave)
    {
        _selectedWaveGroupIndex = groupIndex;
        _selectedWaveSlotIndex = slotIndex;
        _selectedWave = wave;
        _serializedWaveObject = wave != null ? new SerializedObject(wave) : null;
        GUI.FocusControl(null);
        SceneView.RepaintAll();
    }

    private void AddNewWaveGroup()
    {
        if (_waveGroupsProperty == null) return;
        int index = _waveGroupsProperty.arraySize;
        _waveGroupsProperty.InsertArrayElementAtIndex(index);
        SerializedProperty newGroup = _waveGroupsProperty.GetArrayElementAtIndex(index);
        newGroup.FindPropertyRelative("waveSet").ClearArray();
        _serializedLevelObject.ApplyModifiedProperties();
    }

    private void DeleteWaveGroup(int index)
    {
        if (_waveGroupsProperty == null) return;
        if (!EditorUtility.DisplayDialog("Delete Wave Group", $"Delete Wave Group {index + 1}?", "Delete", "Cancel")) return;
        _waveGroupsProperty.DeleteArrayElementAtIndex(index);
        _serializedLevelObject.ApplyModifiedProperties();
        if (_selectedWaveGroupIndex == index) { _selectedWaveGroupIndex = -1; _selectedWaveSlotIndex = -1; _selectedWave = null; _serializedWaveObject = null; }
        else if (_selectedWaveGroupIndex > index) _selectedWaveGroupIndex--;
        CalculateLevelTotals();
    }

    private void AddWaveSlotToGroup(int groupIndex)
    {
        if (_waveGroupsProperty == null) return;
        SerializedProperty groupProp = _waveGroupsProperty.GetArrayElementAtIndex(groupIndex);
        SerializedProperty waveSetProp = groupProp.FindPropertyRelative("waveSet");
        int index = waveSetProp.arraySize;
        waveSetProp.InsertArrayElementAtIndex(index);
        waveSetProp.GetArrayElementAtIndex(index).objectReferenceValue = null;
        _serializedLevelObject.ApplyModifiedProperties();
        SelectWaveSlot(groupIndex, index, null);
    }

    private void DeleteWaveSlot(int groupIndex, int slotIndex)
    {
        if (_waveGroupsProperty == null) return;
        SerializedProperty groupProp = _waveGroupsProperty.GetArrayElementAtIndex(groupIndex);
        SerializedProperty waveSetProp = groupProp.FindPropertyRelative("waveSet");
        WaveSO wave = (WaveSO)waveSetProp.GetArrayElementAtIndex(slotIndex).objectReferenceValue;
        
        if (wave != null)
        {
            int choice = EditorUtility.DisplayDialogComplex("Delete Wave Slot", $"What to do with {wave.name}?", "Remove from Slot", "Cancel", "Delete Asset");
            if (choice == 1) return;
            if (choice == 2)
            {
                bool areYouSure = EditorUtility.DisplayDialog("Are you sure?", $"Deleting: {wave.name}?", "Yes", "No");
                if (areYouSure)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(wave)); RefreshWavePool();
                }
                
            }
        }
        
        waveSetProp.GetArrayElementAtIndex(slotIndex).objectReferenceValue = null;
        waveSetProp.DeleteArrayElementAtIndex(slotIndex);
        _serializedLevelObject.ApplyModifiedProperties();
        
        if (_selectedWaveGroupIndex == groupIndex && _selectedWaveSlotIndex == slotIndex)
        { _selectedWaveSlotIndex = -1; _selectedWave = null; _serializedWaveObject = null; }
        else if (_selectedWaveGroupIndex == groupIndex && _selectedWaveSlotIndex > slotIndex) _selectedWaveSlotIndex--;
        CalculateLevelTotals();
    }

    private void AssignWaveToSlot(int groupIndex, int slotIndex, WaveSO wave)
    {
        if (_waveGroupsProperty == null) return;
        SerializedProperty groupProp = _waveGroupsProperty.GetArrayElementAtIndex(groupIndex);
        SerializedProperty waveSetProp = groupProp.FindPropertyRelative("waveSet");
        waveSetProp.GetArrayElementAtIndex(slotIndex).objectReferenceValue = wave;
        _serializedLevelObject.ApplyModifiedProperties();
        SelectWaveSlot(groupIndex, slotIndex, wave);
        CalculateLevelTotals();
    }

    private void CreateNewWaveAsset()
    {
        if (_selectedLevelData == null) return;
        string levelName = _selectedLevelData.name;
        string folderPath = $"{BaseWavePath}/{levelName}";
        if (!Directory.Exists(folderPath)) { Directory.CreateDirectory(folderPath); AssetDatabase.Refresh(); }
        
        // WaveSO newWave = CreateInstance<WaveSO>();
        // string waveName = $"Wave_{levelName.Replace(" ", "_")}_{System.DateTime.Now.Ticks % 10000}";
        // string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{waveName}.asset");
        // AssetDatabase.CreateAsset(newWave, fullPath);
        // AssetDatabase.SaveAssets();
        // RefreshWavePool();
        // EditorGUIUtility.PingObject(newWave);
        
        // --- NEW NAMING LOGIC START ---
    
        // 1. Define the naming pattern (e.g. "Wave_Level_One_")
        string cleanLevelName = levelName.Replace(" ", "_");
        string baseFileName = $"Wave_{cleanLevelName}_";
    
        // 2. Start checking from 1 and increment until we find a free spot
        int index = 1;
        string fullPath = $"{folderPath}/{baseFileName}{index}.asset";

        // Check if the asset already exists at this path
        while (AssetDatabase.LoadAssetAtPath<WaveSO>(fullPath) != null)
        {
            index++;
            fullPath = $"{folderPath}/{baseFileName}{index}.asset";
        }
        // --- NEW NAMING LOGIC END ---

        // Create and Save
        WaveSO newWave = CreateInstance<WaveSO>();
    
        // Assign the name to the internal object name as well (optional but good practice)
        newWave.name = $"{baseFileName}{index}"; 

        AssetDatabase.CreateAsset(newWave, fullPath);
        AssetDatabase.SaveAssets();
    
        RefreshWavePool();
        EditorGUIUtility.PingObject(newWave);
    }

    private void CreateNewWaveForSlot(int groupIndex, int slotIndex)
    {
        if (_selectedLevelData == null || _waveGroupsProperty == null) return;
        string levelName = _selectedLevelData.name;
        string folderPath = $"{BaseWavePath}/{levelName}";
        if (!Directory.Exists(folderPath)) { Directory.CreateDirectory(folderPath); AssetDatabase.Refresh(); }
        
        WaveSO newWave = CreateInstance<WaveSO>();
        string waveName = $"Wave_{levelName.Replace(" ", "_")}_G{groupIndex + 1}_S{slotIndex + 1}";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{waveName}.asset");
        AssetDatabase.CreateAsset(newWave, fullPath);
        AssetDatabase.SaveAssets();
        RefreshWavePool();
        AssignWaveToSlot(groupIndex, slotIndex, newWave);
    }

    private void RenameWaveAsset(WaveSO wave, string newName)
    {
        if (wave == null || string.IsNullOrWhiteSpace(newName)) return;
        string oldPath = AssetDatabase.GetAssetPath(wave);
        if (string.IsNullOrEmpty(oldPath)) return;
        string sanitizedName = newName.Replace(" ", "_");
        foreach (char c in Path.GetInvalidFileNameChars()) sanitizedName = sanitizedName.Replace(c.ToString(), "");
        string error = AssetDatabase.RenameAsset(oldPath, sanitizedName);
        if (string.IsNullOrEmpty(error))
        { AssetDatabase.SaveAssets(); _serializedWaveObject = new SerializedObject(_selectedWave); RefreshWavePool(); Repaint(); }
        else Debug.LogError($"Failed to rename: {error}");
    }
}