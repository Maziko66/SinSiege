using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq; 

public class WaveEditorWindow : EditorWindow
{
    // --- MODE SWITCHING ---
    private enum EditorMode { Waves, Segments, Routes }
    private EditorMode _currentMode = EditorMode.Waves;

    // --- Selection ---
    private LevelData _selectedLevelData;
    private SerializedObject _serializedLevelObject;
    private SerializedProperty _wavesListProperty;
    private SerializedProperty _mapRoutesProperty;
    private SerializedProperty _availableSegmentsProperty;

    // --- Level List Caching ---
    private List<LevelData> _cachedLevels = new List<LevelData>();
    private string[] _levelNames;
    private int _selectedLevelIndex = 0;
    
    // --- Constants ---
    private const string BaseWavePath = "Assets/Scriptable Objects/Waves";

    // --- Wave Editing ---
    private WaveSO _selectedWave;
    private SerializedObject _serializedWaveObject;
    private int _selectedWaveIndex = -1;
    
    // --- Path/Segment Editing ---
    private int _selectedRouteIndex = -1;
    private int _selectedSegmentIndex = -1;
    private bool _showSceneHandles = true;

    // --- Stats Caching ---
    private int _cachedLevelTotalGold;
    private float _cachedLevelTotalExp;

    // --- Layout ---
    private Vector2 _sidebarScrollPosition;
    private Vector2 _inspectorScrollPosition;
    private Vector2 _segmentAvailableScroll;
    private Vector2 _segmentSelectedScroll;
    private float _sidebarWidth = 300f;
    private bool _isResizing = false;
    private const float MinSidebarWidth = 200f;

    // --- Cached Styles ---
    private GUIStyle _selectedButtonStyle;
    private GUIStyle _centeredGreyLabel;

    [MenuItem("Tools/Wave Editor")]
    public static void ShowWindow()
    {
        GetWindow<WaveEditorWindow>("Wave Editor");
    }

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
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.gray }
            };
        }
    }

    private void OnGUI()
    {
        InitializeStyles();

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

        switch (_currentMode)
        {
            case EditorMode.Waves:
                DrawWaveEditor();
                break;
            case EditorMode.Segments:
                DrawSegmentEditor();
                break;
            case EditorMode.Routes:
                DrawPathEditor();
                break;
        }

        if (_serializedLevelObject != null)
        {
            _serializedLevelObject.ApplyModifiedProperties();
        }
    }

    // --- SCENE GUI (HANDLES) ---
    private void OnSceneGUI(SceneView sceneView)
    {
        if (_selectedLevelData == null || !_showSceneHandles) return;

        if (_currentMode == EditorMode.Segments)
        {
            DrawSegmentModeHandles();
        }
        else if (_currentMode == EditorMode.Routes)
        {
            DrawRouteModeHandles();
        }
        else if (_currentMode == EditorMode.Waves)
        {
            DrawWaveModeHandles();
        }
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
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(point, "Move Waypoint");
                            point.position = newPos;
                        }
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
            {
                Handles.DrawLine(point.position, seg.waypoints[w + 1].position, 2f);
            }
            
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

    private void DrawWaveModeHandles()
    {
        // Draw the route assigned to the selected wave
        if (_selectedWave == null || _serializedWaveObject == null) return;

        var routes = _selectedLevelData.MapRoutes;
        if (routes == null) return;

        // Get the route index from the selected wave
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

                // Draw lines between waypoints
                if (w < segment.waypoints.Count - 1 && segment.waypoints[w + 1] != null)
                {
                    Handles.DrawLine(point.position, segment.waypoints[w + 1].position, 2f);
                }

                // Draw dot at waypoint position
                Handles.DrawSolidDisc(point.position, Vector3.up, 0.15f);
                
                // Label
                Handles.Label(point.position + Vector3.up * 0.5f, $"{segment.segmentName}_{w}");
            }
        }

        // Draw spawn point if exists
        if (route.pathSegments.Count > 0 && route.pathSegments[0].spawnPoint != null)
        {
            Handles.color = Color.green;
            Transform spawn = route.pathSegments[0].spawnPoint;
            Handles.DrawSolidDisc(spawn.position, Vector3.up, 0.3f);
            Handles.Label(spawn.position + Vector3.up * 0.7f, "SPAWN");
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
            if (GUILayout.Button("Show Level in Project", GUILayout.Height(20)))
            {
                EditorGUIUtility.PingObject(_selectedLevelData);
                Selection.activeObject = _selectedLevelData;
            }
        }
        
        EditorGUILayout.Space(5);
        _currentMode = (EditorMode)GUILayout.Toolbar((int)_currentMode, new string[] { "Wave Editor", "Segment Editor", "Route Editor" }, GUILayout.Height(25));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    // ==========================================================================================
    //                                      PATH EDITOR LOGIC
    // ==========================================================================================

    private void DrawPathEditor()
    {
        EditorGUILayout.BeginHorizontal();
        
        // Left: Sidebar
        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawRoutesSidebar();
        EditorGUILayout.EndVertical();
        
        ResizeHandle();

        // Right: Inspector
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawRouteInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRoutesSidebar()
    {
        EditorGUILayout.LabelField("Map Routes", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Create New Route", GUILayout.Height(30)))
        {
            CreateNewRoute();
        }

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
                {
                    _selectedRouteIndex = i;
                    SceneView.RepaintAll(); 
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    deleteIndex = i;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            
            // Handle deletion outside the loop to avoid layout issues
            if (deleteIndex >= 0)
            {
                DeleteRoute(deleteIndex);
            }
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

            // VALIDATION: Check first segment
            if (segmentsProp.arraySize > 0)
            {
                SerializedProperty firstSeg = segmentsProp.GetArrayElementAtIndex(0);
                Transform sp = (Transform)firstSeg.FindPropertyRelative("spawnPoint").objectReferenceValue;
                if (sp == null)
                {
                    EditorGUILayout.HelpBox("First Segment must have a Spawn Point assigned!", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Route is empty. Add a starting segment with a Spawn Point.", MessageType.Info);
            }

            for (int i = 0; i < segmentsProp.arraySize; i++)
            {
                SerializedProperty segProp = segmentsProp.GetArrayElementAtIndex(i);
                DrawSegmentInRouteUI(segProp, segmentsProp, i);
                EditorGUILayout.Space(2);
            }

            // ONLY ALLOW ADDING FROM POOL
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
                        menu.AddItem(new GUIContent(menuPath), false, () => {
                            AddSegmentFromPoolToRoute(_selectedRouteIndex, indexCopy);
                        });
                    }
                    menu.ShowAsContext();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Segments available in Pool. Create them in Segment Editor first.", MessageType.Warning);
            }
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

        if (GUILayout.Button("Up", GUILayout.Width(25)) && index > 0)
        {
            listProp.MoveArrayElement(index, index - 1);
        }
        if (GUILayout.Button("Dn", GUILayout.Width(25)) && index < listProp.arraySize - 1)
        {
            listProp.MoveArrayElement(index, index + 1);
        }
        
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            listProp.DeleteArrayElementAtIndex(index);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    // --- Spawn Point Helper ---
    private void DrawSpawnPointSelector(SerializedProperty spawnProp)
    {
        Transform levelTransform = _selectedLevelData.transform;
        Transform spawnContainer = levelTransform.Find("Spawn Points");

        if (spawnContainer == null)
        {
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.PropertyField(spawnProp);
            EditorGUILayout.HelpBox("No 'Spawn Points' container found.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        List<string> options = new List<string> { "None" };
        List<Transform> values = new List<Transform> { null };

        foreach (Transform child in spawnContainer)
        {
            options.Add(child.name);
            values.Add(child);
        }

        Transform currentVal = (Transform)spawnProp.objectReferenceValue;
        int currentIndex = 0;
        if (currentVal != null)
        {
            int found = values.IndexOf(currentVal);
            if (found != -1) currentIndex = found;
        }

        int newIndex = EditorGUILayout.Popup("Spawn Point", currentIndex, options.ToArray());
        if (newIndex != currentIndex)
        {
            spawnProp.objectReferenceValue = values[newIndex];
        }
    }

    private void CreateNewRoute()
    {
        if (_selectedLevelData == null) return;

        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null)
            {
                Debug.LogError("LevelData component missing on Prefab Root!");
                return;
            }

            MapRoute newRoute = new MapRoute();
            newRoute.routeName = "New Route";
            newRoute.pathSegments = new List<PathSegment>();
            
            dataOnPrefab.MapRoutes.Add(newRoute);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            
            _selectedRouteIndex = dataOnPrefab.MapRoutes.Count - 1;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating route: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

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
            if (dataOnPrefab == null || dataOnPrefab.MapRoutes == null)
            {
                Debug.LogError("LevelData or MapRoutes missing on Prefab!");
                return;
            }

            if (index >= 0 && index < dataOnPrefab.MapRoutes.Count)
            {
                dataOnPrefab.MapRoutes.RemoveAt(index);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                
                if (_selectedRouteIndex >= index) _selectedRouteIndex = -1;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting route: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        ReloadSelectedLevel();
    }

    // ==========================================================================================
    //                                    SEGMENT EDITOR LOGIC
    // ==========================================================================================

    private void DrawSegmentEditor()
    {
        EditorGUILayout.BeginHorizontal();
        
        // --- LEFT SIDEBAR: Segment List ---
        EditorGUILayout.BeginVertical("box", GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        DrawSegmentsSidebar();
        EditorGUILayout.EndVertical();
        
        ResizeHandle();

        // --- RIGHT PANEL: Segment Inspector & Dual Lists ---
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        if (_selectedSegmentIndex != -1 && _availableSegmentsProperty != null && _selectedSegmentIndex < _availableSegmentsProperty.arraySize)
        {
            DrawDualListSegmentInspector();
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select a Segment to edit.", _centeredGreyLabel, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSegmentsSidebar()
    {
        EditorGUILayout.LabelField("Segment Pool", EditorStyles.boldLabel);
        
        if (GUILayout.Button("+ Create New Segment", GUILayout.Height(30)))
        {
            CreateNewSegmentInPool();
        }

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
                {
                    _selectedSegmentIndex = i;
                    SceneView.RepaintAll(); 
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    deleteIndex = i;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            
            // Handle deletion outside the loop
            if (deleteIndex >= 0)
            {
                DeleteSegmentFromPool(deleteIndex);
            }
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

        // 1. Header (fixed height, outside scroll)
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Segment Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nameProp);
        DrawSpawnPointSelector(spawnProp);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 2. Dual List UI - Calculate available height
        float availableHeight = position.height - 180f;
        availableHeight = Mathf.Max(availableHeight, 200f);

        EditorGUILayout.BeginHorizontal(GUILayout.Height(availableHeight));

        // --- LEFT COLUMN: Available Points in Scene ---
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
                {
                    SerializedProperty p = pointsProp.GetArrayElementAtIndex(i);
                    if (p.objectReferenceValue == child)
                    {
                        alreadyAdded = true; 
                        break;
                    }
                }

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
        else
        {
            EditorGUILayout.HelpBox("No 'Waypoints' GameObject found in Prefab.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        // --- RIGHT COLUMN: Selected Points ---
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
            
            if (GUILayout.Button("Up", GUILayout.Width(30)) && i > 0)
            {
                pointsProp.MoveArrayElement(i, i - 1);
            }
            if (GUILayout.Button("Dn", GUILayout.Width(30)) && i < pointsProp.arraySize - 1)
            {
                pointsProp.MoveArrayElement(i, i + 1);
            }
            
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                pointsProp.DeleteArrayElementAtIndex(i);
            }
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
            if (dataOnPrefab == null)
            {
                Debug.LogError("LevelData component missing on Prefab Root!");
                return;
            }

            // Use SerializedObject to access the backing field
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
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating segment: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

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
            if (dataOnPrefab == null)
            {
                Debug.LogError("LevelData missing on Prefab!");
                return;
            }

            // Use SerializedObject to access the backing field
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
        catch (System.Exception e)
        {
            Debug.LogError($"Error deleting segment: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

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
            if (dataOnPrefab == null || dataOnPrefab.AvailableSegments == null || dataOnPrefab.MapRoutes == null)
            {
                Debug.LogError("Invalid LevelData on prefab!");
                return;
            }

            if (poolIndex < 0 || poolIndex >= dataOnPrefab.AvailableSegments.Count)
            {
                Debug.LogError("Pool index out of range!");
                return;
            }

            if (routeIndex < 0 || routeIndex >= dataOnPrefab.MapRoutes.Count)
            {
                Debug.LogError("Route index out of range!");
                return;
            }

            // Read directly from the prefab's data
            PathSegment srcSeg = dataOnPrefab.AvailableSegments[poolIndex];
            MapRoute route = dataOnPrefab.MapRoutes[routeIndex];
            
            PathSegment newSeg = new PathSegment();
            newSeg.segmentName = srcSeg.segmentName;
            newSeg.spawnPoint = srcSeg.spawnPoint;
            newSeg.waypoints = new List<Transform>();
            
            if (srcSeg.waypoints != null)
            {
                foreach (var wp in srcSeg.waypoints)
                {
                    newSeg.waypoints.Add(wp);
                }
            }
            
            route.pathSegments.Add(newSeg);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error adding segment to route: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        ReloadSelectedLevel();
    }

    // ==========================================================================================
    //                                      WAVE EDITOR LOGIC
    // ==========================================================================================

    private void DrawWaveEditor()
    {
        EditorGUILayout.BeginHorizontal();
        DrawWaveSidebar();
        ResizeHandle();
        DrawWaveInspector();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("Waves List", EditorStyles.boldLabel);
        
        if (GUILayout.Button("+ Create New Wave", GUILayout.Height(30))) CreateNewWave();

        EditorGUILayout.Space();
        _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition);

        if (_wavesListProperty != null)
        {
            for (int i = 0; i < _wavesListProperty.arraySize; i++)
            {
                SerializedProperty waveProp = _wavesListProperty.GetArrayElementAtIndex(i);
                WaveSO waveRef = (WaveSO)waveProp.objectReferenceValue;

                EditorGUILayout.BeginHorizontal();

                GUIStyle btnStyle = (i == _selectedWaveIndex) ? _selectedButtonStyle : GUI.skin.button;
                string btnLabel = waveRef != null ? $"Wave {i + 1}: {waveRef.name}" : $"Wave {i + 1}: (Empty)";
                
                if (GUILayout.Button(btnLabel, btnStyle, GUILayout.Height(25))) SelectWave(i, waveRef);

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
    }

    private void DrawWaveInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedWave != null && _serializedWaveObject != null)
        {
            _serializedWaveObject.Update();

            // Wave Name Field with Rename functionality
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Editing:", GUILayout.Width(50));
            
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.DelayedTextField(_selectedWave.name, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName) && newName != _selectedWave.name)
            {
                RenameWaveAsset(_selectedWave, newName);
            }
            
            if (GUILayout.Button("Show in Project", GUILayout.Width(110)))
            {
                EditorGUIUtility.PingObject(_selectedWave);
                Selection.activeObject = _selectedWave;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal("helpBox");
            if (_wavesListProperty != null) EditorGUILayout.LabelField($"Waves: {_wavesListProperty.arraySize}", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("|", GUILayout.Width(10));
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
                
                if (iterator.name == "routeIndex")
                {
                    List<string> routeNames = new List<string>();
                    var routes = _selectedLevelData.MapRoutes;
                    if (routes != null)
                    {
                        for (int r = 0; r < routes.Count; r++)
                        {
                            routeNames.Add($"{r}: {routes[r].routeName}");
                        }
                    }
                    
                    if (routeNames.Count > 0)
                    {
                        int current = iterator.intValue;
                        int selected = EditorGUILayout.Popup("Route", current, routeNames.ToArray());
                        if (selected != current) iterator.intValue = selected;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No Routes defined in Level!", MessageType.Warning);
                        iterator.intValue = -1;
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
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
            EditorGUILayout.LabelField("Select a Wave from the list to edit.", _centeredGreyLabel, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // --- HELPERS (Common) ---

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
        _selectedLevelData = newLevel;
        _selectedWave = null;
        _selectedWaveIndex = -1;
        _serializedWaveObject = null;
        _selectedRouteIndex = -1;
        _selectedSegmentIndex = -1;

        if (_selectedLevelData != null)
        {
            _serializedLevelObject = new SerializedObject(_selectedLevelData);
            _wavesListProperty = _serializedLevelObject.FindProperty("waves");
            _mapRoutesProperty = _serializedLevelObject.FindProperty("mapRoutes");
            _availableSegmentsProperty = _serializedLevelObject.FindProperty("availableSegments");
            CalculateLevelTotals(); 
        }
        else
        {
            _serializedLevelObject = null;
            _wavesListProperty = null;
            _mapRoutesProperty = null;
            _availableSegmentsProperty = null;
        }
    }

    private void ReloadSelectedLevel()
    {
        if (_selectedLevelData == null) return;
        
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        
        // Store current selections
        int routeIdx = _selectedRouteIndex;
        int segmentIdx = _selectedSegmentIndex;
        
        // Force reimport to ensure fresh data
        AssetDatabase.ImportAsset(assetPath);
        
        // Reload
        LevelData reloaded = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        _selectedLevelData = null; // Force ChangeSelectedLevel to fully reinitialize
        ChangeSelectedLevel(reloaded);
        
        // Restore selections if still valid
        if (_mapRoutesProperty != null && routeIdx >= 0 && routeIdx < _mapRoutesProperty.arraySize)
        {
            _selectedRouteIndex = routeIdx;
        }
        if (_availableSegmentsProperty != null && segmentIdx >= 0 && segmentIdx < _availableSegmentsProperty.arraySize)
        {
            _selectedSegmentIndex = segmentIdx;
        }
        
        Repaint();
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

    private void ResizeHandle()
    {
        Rect resizeRect = GUILayoutUtility.GetRect(5f, 0f, GUILayout.Width(5f), GUILayout.ExpandHeight(true));
        
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

    // --- WAVE SPECIFIC HELPERS ---

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
        if (_wavesListProperty == null) return;

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
        if (_selectedLevelData == null || _wavesListProperty == null) return;

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
        int choice = EditorUtility.DisplayDialogComplex("Delete Wave", $"What do you want to do with {waveName}?", "Remove from List", "Cancel", "Delete Asset File");

        if (choice == 1) return;

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
            if (element.objectReferenceValue != null) element.objectReferenceValue = null;
            _wavesListProperty.DeleteArrayElementAtIndex(index);
        }
        _serializedLevelObject.ApplyModifiedProperties();

        CalculateLevelTotals();
        GUIUtility.ExitGUI();
    }

    private void RenameWaveAsset(WaveSO wave, string newName)
    {
        if (wave == null || string.IsNullOrWhiteSpace(newName)) return;

        string oldPath = AssetDatabase.GetAssetPath(wave);
        if (string.IsNullOrEmpty(oldPath)) return;

        // Sanitize the name for file system
        string sanitizedName = newName.Replace(" ", "_");
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            sanitizedName = sanitizedName.Replace(c.ToString(), "");
        }

        string error = AssetDatabase.RenameAsset(oldPath, sanitizedName);
        
        if (string.IsNullOrEmpty(error))
        {
            AssetDatabase.SaveAssets();
            // Refresh the serialized object to reflect the new name
            _serializedWaveObject = new SerializedObject(_selectedWave);
            Repaint();
        }
        else
        {
            Debug.LogError($"Failed to rename wave asset: {error}");
        }
    }
}