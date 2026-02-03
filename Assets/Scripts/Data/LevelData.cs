using System.Collections.Generic;
using UnityEngine;

// --- 1. Define the Modular Classes ---

[System.Serializable]
public class PathSegment
{
    public string segmentName;
    public Transform spawnPoint; // Optional spawn point
    public List<Transform> waypoints; // Drag empty GameObjects here to define curves
}

[System.Serializable]
public class MapRoute
{
    public string routeName; 
    public Transform spawnPoint;   // Where the enemy appears
    public List<PathSegment> pathSegments; // Connect multiple segments together
    
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
        Base foundBase = Object.FindFirstObjectByType<Base>();
        
        if (foundBase != null)
        {
            fullPath.Add(foundBase.transform.position);
        }
        else
        {
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
    [SerializeField] private List<MapRoute> mapRoutes = new List<MapRoute>();
    public List<MapRoute> MapRoutes => mapRoutes;

    [Header("Wave Groups")]
    [Tooltip("Each WaveGroup contains multiple WaveSOs that spawn simultaneously")]
    [SerializeField] private List<WaveGroup> waveGroups = new List<WaveGroup>();
    public List<WaveGroup> WaveGroups => waveGroups;
    
    // Legacy support - returns all waves flattened (for compatibility)
    public List<WaveSO> Waves
    {
        get
        {
            List<WaveSO> allWaves = new List<WaveSO>();
            foreach (var group in waveGroups)
            {
                if (group.waveSet != null)
                {
                    allWaves.AddRange(group.waveSet);
                }
            }
            return allWaves;
        }
    }
    
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    public List<Transform> SpawnPoints => spawnPoints;

    [Header("Segment Pool")]
    [SerializeField] private List<PathSegment> availableSegments = new List<PathSegment>();
    public List<PathSegment> AvailableSegments => availableSegments;
}