using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq; 

public class WaveEditorWindow : EditorWindow
{
    // --- MODE SWITCHING ---
    private enum EditorMode { Waves, Segments, Routes } // Updated Enum
    private EditorMode _currentMode = EditorMode.Waves;

    // --- Selection ---
    private LevelData _selectedLevelData;
    private SerializedObject _serializedLevelObject;
    private SerializedProperty _wavesListProperty;
    private SerializedProperty _mapRoutesProperty;
    private SerializedProperty _availableSegmentsProperty; // New Property

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
    private int _selectedSegmentIndex = -1; // New Index for Segment Editor
    private bool _showSceneHandles = true;

    // --- Stats Caching ---
    private int _cachedLevelTotalGold;
    private float _cachedLevelTotalExp;

    // --- Layout ---
    private Vector2 _sidebarScrollPosition;
    private Vector2 _inspectorScrollPosition;
    private Vector2 _segmentAvailableScroll; // New
    private Vector2 _segmentSelectedScroll; // New
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
        SceneView.duringSceneGui += OnSceneGUI; 
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI; 
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

        switch (_currentMode)
        {
            case EditorMode.Waves:
                DrawWaveEditor();
                break;
            case EditorMode.Segments:
                DrawSegmentEditor();
                break;
            case EditorMode.Routes: // Renamed from Paths for clarity, effectively existing logic
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
        // Add support for Segment Editor handles too?
        if (_selectedLevelData == null || !_showSceneHandles) return;

        if (_currentMode == EditorMode.Segments)
        {
            DrawSegmentModeHandles();
        }
        else if (_currentMode == EditorMode.Routes)
        {
            DrawRouteModeHandles();
        }
    }

    private void DrawRouteModeHandles()
    {
        var routes = _selectedLevelData.MapRoutes;
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
        // Draw handles for the currently selected Segment from the Pool
        var pool = _selectedLevelData.AvailableSegments;
        if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= pool.Count) return;

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

        if (GUILayout.Button("Auto-Create Route from Waypoints", GUILayout.Height(30)))
        {
            PopulateRouteFromHierarchy();
        }

        EditorGUILayout.Space();

        _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition);

        if (_mapRoutesProperty != null)
        {
            for (int i = 0; i < _mapRoutesProperty.arraySize; i++)
            {
                SerializedProperty routeProp = _mapRoutesProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = routeProp.FindPropertyRelative("routeName");
                
                string name = nameProp.stringValue;
                if (string.IsNullOrEmpty(name)) name = $"Route {i}";

                EditorGUILayout.BeginHorizontal();

                GUIStyle btnStyle = (i == _selectedRouteIndex) ? new GUIStyle(GUI.skin.button) { normal = GUI.skin.button.active } : GUI.skin.button;
                
                if (GUILayout.Button(name, btnStyle, GUILayout.Height(25)))
                {
                    _selectedRouteIndex = i;
                    SceneView.RepaintAll(); 
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _mapRoutesProperty.DeleteArrayElementAtIndex(i);
                    _mapRoutesProperty.serializedObject.ApplyModifiedProperties();
                    if (_selectedRouteIndex >= i) _selectedRouteIndex = -1;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        
        GUILayout.FlexibleSpace();
        _showSceneHandles = EditorGUILayout.Toggle("Show Scene Handles", _showSceneHandles);
    }

    private void DrawRouteInspector()
    {
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedRouteIndex != -1 && _selectedRouteIndex < _mapRoutesProperty.arraySize)
        {
            SerializedProperty routeProp = _mapRoutesProperty.GetArrayElementAtIndex(_selectedRouteIndex);
            
            EditorGUILayout.LabelField("Route Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(routeProp.FindPropertyRelative("routeName"));
            // Deprecated direct spawnPoint on Route? 
            // User: "Routes will start with a segment that has Spawn point"
            // So we might hide the route's own spawnPoint field or keep it as legacy override?
            // Let's keep it but maybe warn if redundant.
            // Actually, LevelData.MapRoute has spawnPoint. Let's leave it for now but focus on Segments.

            
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
                DrawSegmentInRouteUI(segProp, segmentsProp, i); // Custom mini-view
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
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField("Select a Route to edit.", style, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSegmentInRouteUI(SerializedProperty segmentProp, SerializedProperty listProp, int index)
    {
        EditorGUILayout.BeginHorizontal("helpBox");
        
        SerializedProperty nameProp = segmentProp.FindPropertyRelative("segmentName");
        // We might want to show if it has a spawn point
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

    // --- Segment UI ---
    private void DrawSegmentUI(SerializedProperty segmentProp, SerializedProperty listProp, int index)
    {
        EditorGUILayout.BeginVertical("helpBox");
        
        SerializedProperty nameProp = segmentProp.FindPropertyRelative("segmentName");
        SerializedProperty pointsProp = segmentProp.FindPropertyRelative("waypoints");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Segment {index + 1}", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Remove Segment", GUILayout.Width(110)))
        {
            listProp.DeleteArrayElementAtIndex(index);
            listProp.serializedObject.ApplyModifiedProperties(); // Save delete immediately
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUIUtility.ExitGUI(); 
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(nameProp);

        EditorGUI.indentLevel++;
        pointsProp.isExpanded = EditorGUILayout.Foldout(pointsProp.isExpanded, "Waypoints", true);
        
        if (pointsProp.isExpanded)
        {
            // 1. Gather Valid Options
            string segmentName = nameProp.stringValue;
            Transform segmentContainer = null;
            Transform waypointsContainer = _selectedLevelData.transform.Find("Waypoints");
            
            if (waypointsContainer != null)
                segmentContainer = waypointsContainer.Find(segmentName);

            // Warning Box logic
            if (segmentContainer == null)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
                EditorGUILayout.HelpBox($"GameObject 'Waypoints/{segmentName}' not found in Prefab.\nCannot find points to list.", MessageType.Warning);
                GUI.backgroundColor = Color.white;
            }

            List<string> options = new List<string> { "None" };
            List<Transform> values = new List<Transform> { null };

            if (segmentContainer != null)
            {
                foreach (Transform child in segmentContainer)
                {
                    options.Add(child.name);
                    values.Add(child);
                }
            }

            // 2. Draw Points
            for (int p = 0; p < pointsProp.arraySize; p++)
            {
                SerializedProperty pointRef = pointsProp.GetArrayElementAtIndex(p);
                Transform currentPoint = (Transform)pointRef.objectReferenceValue;

                EditorGUILayout.BeginHorizontal();
                
                int currentIndex = 0;
                if (currentPoint != null)
                {
                    int found = values.IndexOf(currentPoint);
                    if (found != -1) currentIndex = found;
                    else
                    {
                        options.Add(currentPoint.name + " (External)");
                        values.Add(currentPoint);
                        currentIndex = values.Count - 1;
                    }
                }

                int newIndex = EditorGUILayout.Popup($"Point {p}", currentIndex, options.ToArray());
                if (newIndex != currentIndex)
                {
                    pointRef.objectReferenceValue = values[newIndex];
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    pointsProp.DeleteArrayElementAtIndex(p);
                    pointsProp.serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // 3. Add Slot
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Empty Slot", GUILayout.Height(20)))
            {
                pointsProp.InsertArrayElementAtIndex(pointsProp.arraySize);
                pointsProp.GetArrayElementAtIndex(pointsProp.arraySize - 1).objectReferenceValue = null;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.EndVertical();
    }

    private void CreateNewRoute()
    {
        int index = _mapRoutesProperty.arraySize;
        _mapRoutesProperty.InsertArrayElementAtIndex(index);
        SerializedProperty newRoute = _mapRoutesProperty.GetArrayElementAtIndex(index);
        
        newRoute.FindPropertyRelative("routeName").stringValue = "New Route";
        newRoute.FindPropertyRelative("pathSegments").ClearArray();
        newRoute.FindPropertyRelative("spawnPoint").objectReferenceValue = null;
        newRoute.FindPropertyRelative("baseTarget").objectReferenceValue = null;
        
        _selectedRouteIndex = index;
    }

    // --- UPDATED METHOD: Uses PrefabUtility to modify hierarchy on disk ---
    private void CreateSegmentForRoute(int routeIndex)
    {
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        
        // 1. Load the Prefab content so we can edit the hierarchy
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            // 2. Modify Hierarchy on the 'prefabRoot' instance
            Transform waypointsContainer = prefabRoot.transform.Find("Waypoints");
            if (waypointsContainer == null)
            {
                GameObject containerGO = new GameObject("Waypoints");
                containerGO.transform.SetParent(prefabRoot.transform);
                containerGO.transform.localPosition = Vector3.zero;
                waypointsContainer = containerGO.transform;
            }

            // Generate unique name
            int count = waypointsContainer.childCount;
            string segName = $"Segment_{count}";
            
            GameObject segGO = new GameObject(segName);
            segGO.transform.SetParent(waypointsContainer);
            segGO.transform.localPosition = Vector3.zero;

            // 3. Update the LevelData component ON THE PREFAB ROOT
            // We cannot use _serializedLevelObject here because it points to the Asset, which is being overwritten
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab != null && routeIndex < dataOnPrefab.MapRoutes.Count)
            {
                MapRoute route = dataOnPrefab.MapRoutes[routeIndex];
                
                PathSegment newSeg = new PathSegment();
                newSeg.segmentName = segName;
                newSeg.waypoints = new List<Transform>();
                
                route.pathSegments.Add(newSeg);
            }

            // 4. Save changes back to the Asset
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating segment: {e.Message}");
        }
        finally
        {
            // 5. Unload the temporary prefab scene
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // 6. Force Refresh the tool to reload the updated asset
        ChangeSelectedLevel(AssetDatabase.LoadAssetAtPath<LevelData>(assetPath));
        Repaint();
    }

    private void PopulateRouteFromHierarchy()
    {
        if (_selectedLevelData == null) return;

        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            Transform waypointsContainer = prefabRoot.transform.Find("Waypoints");
            if (waypointsContainer == null)
            {
                Debug.LogError("No 'Waypoints' object found in Level Prefab root!");
                return;
            }

            // Create new Route data
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            if (dataOnPrefab == null)
            {
                Debug.LogError("LevelData component missing on Prefab Root!");
                return;
            }

            MapRoute newRoute = new MapRoute();
            newRoute.routeName = "Auto_Route_" + (dataOnPrefab.MapRoutes.Count + 1);
            newRoute.pathSegments = new List<PathSegment>();

            PathSegment newSeg = new PathSegment();
            newSeg.segmentName = "Main_Path";
            newSeg.waypoints = new List<Transform>();

            // Add all children of Waypoints as points
            foreach (Transform child in waypointsContainer)
            {
                newSeg.waypoints.Add(child);
            }

            newRoute.pathSegments.Add(newSeg);
            dataOnPrefab.MapRoutes.Add(newRoute);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            Debug.Log($"Auto-created route '{newRoute.routeName}' with {newSeg.waypoints.Count} points.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error populating route: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        ChangeSelectedLevel(AssetDatabase.LoadAssetAtPath<LevelData>(assetPath));
        Repaint();
    }

    // ==========================================================================================
    //                                    SEGMENT EDITOR LOGIC
    // ==========================================================================================

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
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField("Select a Segment to edit.", style, GUILayout.ExpandWidth(true));
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
            for (int i = 0; i < _availableSegmentsProperty.arraySize; i++)
            {
                SerializedProperty segProp = _availableSegmentsProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = segProp.FindPropertyRelative("segmentName");
                
                string name = nameProp.stringValue;
                if (string.IsNullOrEmpty(name)) name = $"Segment {i}";

                EditorGUILayout.BeginHorizontal();

                GUIStyle btnStyle = (i == _selectedSegmentIndex) ? new GUIStyle(GUI.skin.button) { normal = GUI.skin.button.active } : GUI.skin.button;
                
                if (GUILayout.Button(name, btnStyle, GUILayout.Height(25)))
                {
                    _selectedSegmentIndex = i;
                    SceneView.RepaintAll(); 
                    GUI.FocusControl(null);
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _availableSegmentsProperty.DeleteArrayElementAtIndex(i);
                    _availableSegmentsProperty.serializedObject.ApplyModifiedProperties();
                    if (_selectedSegmentIndex >= i) _selectedSegmentIndex = -1;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
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

        // 1. Header
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Segment Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(nameProp);
        DrawSpawnPointSelector(spawnProp); // Reuse existing helper
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // 2. Dual List UI
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true)); // Main split

        // --- LEFT COLUMN: Available Points in Scene (under Waypoints) ---
        // Use a min width but allow expansion, or fixed width?
        // User "too wide": Let's try flexible but with a max or fixed reasonable width.
        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("Available in Scene", EditorStyles.boldLabel);
        
        Transform waypointsRoot = _selectedLevelData.transform.Find("Waypoints");
        if (waypointsRoot != null)
        {
            _segmentAvailableScroll = EditorGUILayout.BeginScrollView(_segmentAvailableScroll, GUILayout.ExpandHeight(true));
            
            // Scan recursive? Or just children? User said "Waypoints -> points"
            foreach (Transform child in waypointsRoot)
            {
                // Check if already in list?
                bool alreadyAdded = false;
                for(int i=0; i<pointsProp.arraySize; i++)
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
                    pointsProp.serializedObject.ApplyModifiedProperties();
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
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("Selected Waypoints", EditorStyles.boldLabel);
        
        _segmentSelectedScroll = EditorGUILayout.BeginScrollView(_segmentSelectedScroll, GUILayout.ExpandHeight(true));
        
        // Simple list with Remove buttons
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
        
        // Bottom Margin
        GUILayout.Space(20); 
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        
        if (GUI.changed)
        {
            pointsProp.serializedObject.ApplyModifiedProperties();
        }
    }

    private void CreateNewSegmentInPool()
    {
        // Similar to creating a segment on disk, but here we just add to the list
        // and MAYBE create a container object in the scene if we want to be fancy, 
        // but broadly we just need the data entry first.
        
        int index = _availableSegmentsProperty.arraySize;
        _availableSegmentsProperty.InsertArrayElementAtIndex(index);
        SerializedProperty newSeg = _availableSegmentsProperty.GetArrayElementAtIndex(index);
        
        newSeg.FindPropertyRelative("segmentName").stringValue = "New_Segment_" + index;
        newSeg.FindPropertyRelative("waypoints").ClearArray();
        
        _selectedSegmentIndex = index;
    }

    // Refactored helper to draw points list, shared or specific?
    private void DrawWaypointsList(SerializedProperty pointsProp, string segmentNameContext)
    {
        EditorGUILayout.BeginVertical("helpBox");
        pointsProp.isExpanded = EditorGUILayout.Foldout(pointsProp.isExpanded, "Waypoints", true);
        
        if (pointsProp.isExpanded)
        {
             // 1. Gather Valid Options (Same logic as before, roughly)
            List<string> options = new List<string> { "None" };
            List<Transform> values = new List<Transform> { null };
            
            // Try to find a logical container in the scene?
            // "Waypoints/{segmentName}"
             Transform waypointsContainer = _selectedLevelData.transform.Find("Waypoints");
             Transform segmentContainer = null;
             if (waypointsContainer != null) segmentContainer = waypointsContainer.Find(segmentNameContext);

             if (segmentContainer != null)
             {
                 foreach (Transform child in segmentContainer)
                 {
                     options.Add(child.name);
                     values.Add(child);
                 }
             }

            // 2. Draw Points
            for (int p = 0; p < pointsProp.arraySize; p++)
            {
                SerializedProperty pointRef = pointsProp.GetArrayElementAtIndex(p);
                Transform currentPoint = (Transform)pointRef.objectReferenceValue;

                EditorGUILayout.BeginHorizontal();
                
                int currentIndex = 0;
                if (currentPoint != null)
                {
                    int found = values.IndexOf(currentPoint);
                    if (found != -1) currentIndex = found;
                    else
                    {
                        options.Add(currentPoint.name + " (External)");
                        values.Add(currentPoint);
                        currentIndex = values.Count - 1;
                    }
                }

                int newIndex = EditorGUILayout.Popup($"Point {p}", currentIndex, options.ToArray());
                // Also allow object drag
                Transform dragged = (Transform)EditorGUILayout.ObjectField(values[newIndex], typeof(Transform), true, GUILayout.Width(100));
                
                if (dragged != values[newIndex])
                {
                     pointRef.objectReferenceValue = dragged;
                }
                else if (newIndex != currentIndex)
                {
                    pointRef.objectReferenceValue = values[newIndex];
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    pointsProp.DeleteArrayElementAtIndex(p);
                    pointsProp.serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // 3. Add Slot
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Empty Slot", GUILayout.Height(20)))
            {
                pointsProp.InsertArrayElementAtIndex(pointsProp.arraySize);
                pointsProp.GetArrayElementAtIndex(pointsProp.arraySize - 1).objectReferenceValue = null;
            }
            if (GUILayout.Button("Auto-Fill from Selection", GUILayout.Height(20)))
            {
                foreach(var obj in Selection.transforms)
                {
                     pointsProp.InsertArrayElementAtIndex(pointsProp.arraySize);
                     pointsProp.GetArrayElementAtIndex(pointsProp.arraySize - 1).objectReferenceValue = obj;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void AddSegmentFromPoolToRoute(int routeIndex, int poolIndex)
    {
        // We need to copy the data from the pool to the route structure on the prefab
        // Similar to CreateSegmentForRoute but populating data
        
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevelData);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            SerializedObject tempSO = new SerializedObject(prefabRoot.GetComponent<LevelData>());
            SerializedProperty poolProp = tempSO.FindProperty("availableSegments");
            SerializedProperty srcSeg = poolProp.GetArrayElementAtIndex(poolIndex);

            // Get Destination
            LevelData dataOnPrefab = prefabRoot.GetComponent<LevelData>();
            MapRoute route = dataOnPrefab.MapRoutes[routeIndex];
            
            PathSegment newSeg = new PathSegment();
            newSeg.segmentName = srcSeg.FindPropertyRelative("segmentName").stringValue;
            // Copy Spawn Point
            SerializedProperty srcSpawn = srcSeg.FindPropertyRelative("spawnPoint");
            newSeg.spawnPoint = (Transform)srcSpawn.objectReferenceValue;

            newSeg.waypoints = new List<Transform>();
            
            SerializedProperty srcPoints = srcSeg.FindPropertyRelative("waypoints");
            for(int k=0; k<srcPoints.arraySize; k++)
            {
                newSeg.waypoints.Add((Transform)srcPoints.GetArrayElementAtIndex(k).objectReferenceValue);
            }
            
            route.pathSegments.Add(newSeg);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        }
        catch (System.Exception e) { Debug.LogError(e); }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }

        ChangeSelectedLevel(AssetDatabase.LoadAssetAtPath<LevelData>(assetPath));
        Repaint();
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

                GUIStyle btnStyle = (i == _selectedWaveIndex) ? new GUIStyle(GUI.skin.button) { normal = GUI.skin.button.active } : GUI.skin.button;
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
        
        Rect r = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint)
        {
             EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), new Color(0.12f, 0.12f, 0.12f));
        }
    }

    private void DrawWaveInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

        if (_selectedWave != null && _serializedWaveObject != null)
        {
            _serializedWaveObject.Update();

            EditorGUILayout.LabelField($"Editing: {_selectedWave.name}", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal("helpBox");
            if(_wavesListProperty != null) EditorGUILayout.LabelField($"Waves: {_wavesListProperty.arraySize}", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("|", GUILayout.Width(10));
            EditorGUILayout.LabelField($"Wave Gold: {_selectedWave.totalGoldValue}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Wave Exp: {_selectedWave.totalExpValue}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            // Custom handling for Route Index to show dropdown
            SerializedProperty iterator = _serializedWaveObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false; 
                if (iterator.name == "m_Script") continue;
                if (iterator.name == "totalGoldValue" || iterator.name == "totalExpValue") continue;
                
                if (iterator.name == "routeIndex")
                {
                    // Draw Dropdown
                    List<string> routeNames = new List<string>();
                    var routes = _selectedLevelData.MapRoutes;
                    for(int r=0; r<routes.Count; r++)
                    {
                        routeNames.Add($"{r}: {routes[r].routeName}");
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
            var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = Color.gray } };
            EditorGUILayout.LabelField("Select a Wave from the list to edit.", style, GUILayout.ExpandWidth(true));
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
        if (newLevel != _selectedLevelData)
        {
            _selectedLevelData = newLevel;
            _selectedWave = null;
            _selectedWaveIndex = -1;
            _serializedWaveObject = null;
            _selectedRouteIndex = -1;

            if (_selectedLevelData != null)
            {
                _serializedLevelObject = new SerializedObject(_selectedLevelData);
                _wavesListProperty = _serializedLevelObject.FindProperty("waves");
                _mapRoutesProperty = _serializedLevelObject.FindProperty("mapRoutes");
                _availableSegmentsProperty = _serializedLevelObject.FindProperty("availableSegments");
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

        GUIUtility.ExitGUI();
        CalculateLevelTotals();
    }
}