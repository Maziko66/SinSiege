using System.Collections.Generic;
using UnityEngine;

// --- 1. Define the Modular Classes ---

[System.Serializable]
public class PathSegment
{
    public string segmentName;
    public Transform spawnPoint; // New: Optional spawn point
    public List<Transform> waypoints; // Drag empty GameObjects here to define curves
}

[System.Serializable]
public class MapRoute
{
    public string routeName; 
    public Transform spawnPoint;   // Where the enemy appears
    public List<PathSegment> pathSegments; // Connect multiple segments together
    // public Transform baseTarget; // REMOVED: Auto-found at runtime
    
    // Helper to calculate the full path
    public List<Vector2> GetCalculatedPath()
    {
        List<Vector2> fullPath = new List<Vector2>();

        // 1. Add Spawn Point
        if(spawnPoint != null) fullPath.Add(spawnPoint.position);

        // 2. Add Segments
        foreach (var segment in pathSegments)
        {
            if (segment.waypoints == null) continue;
            
            foreach (var point in segment.waypoints)
            {
                if(point != null) fullPath.Add(point.position);
            }
        }

        // 3. Add Base (Auto-find)
        // Note: In editor time this might fail if the scene isn't loaded or Base is part of the prefab hierarchy but not instantiated?
        // But user said "find the base when awake under children of the level prefab"
        // If this runs at runtime, FindFirstObjectByType is fine.
        Base foundBase = Object.FindFirstObjectByType<Base>();
        
        if (foundBase != null)
        {
            fullPath.Add(foundBase.transform.position);
        }
        else
        {
            // Fallback: If LevelData is on the root, maybe checking children?
            // But LevelData usually resides on the specific level object.
            // We'll leave the warning for safety.
            Debug.LogWarning($"[MapRoute] Route '{routeName}' could not auto-find 'Base' in the scene.");
        }

        return fullPath;
    }
}

// --- 2. The LevelData Class ---

public class LevelData : MonoBehaviour
{
    public int levelIndex;
    
    [Header("Modular Routes")]
    // This replaces your old 'routes' list
    [SerializeField] private List<MapRoute> mapRoutes = new List<MapRoute>();
    public List<MapRoute> MapRoutes => mapRoutes;

    [SerializeField] private List<WaveSO> waves = new List<WaveSO>();
    public List<WaveSO> Waves => waves;
    
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    public List<Transform> SpawnPoints => spawnPoints;

    [Header("Segment Pool")]
    [SerializeField] private List<PathSegment> availableSegments = new List<PathSegment>();
    public List<PathSegment> AvailableSegments => availableSegments;
}