using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathSegment
{
    public string segmentName;
    public Transform spawnPoint;
    public List<Transform> waypoints;
}

[System.Serializable]
public class MapRoute
{
    public string routeName; 
    public Transform spawnPoint;
    public List<PathSegment> pathSegments;
    
    public List<Vector2> GetCalculatedPath()
    {
        List<Vector2> fullPath = new List<Vector2>();

        if(spawnPoint != null) fullPath.Add(spawnPoint.position);

        foreach (var segment in pathSegments)
        {
            if (segment.waypoints == null) continue;
            
            foreach (var point in segment.waypoints)
            {
                if(point != null) fullPath.Add(point.position);
            }
        }

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

public class LevelData : MonoBehaviour
{
    public int levelIndex;
    
    [Header("Modular Routes")]
    [SerializeField] private List<MapRoute> mapRoutes = new List<MapRoute>();
    public List<MapRoute> MapRoutes => mapRoutes;

    [Header("Wave Groups")]
    [Tooltip("Each WaveGroup contains WaveSlots (wave + route) that spawn simultaneously")]
    [SerializeField] private List<WaveGroup> waveGroups = new List<WaveGroup>();
    public List<WaveGroup> WaveGroups => waveGroups;
    
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    public List<Transform> SpawnPoints => spawnPoints;

    [Header("Segment Pool")]
    [SerializeField] private List<PathSegment> availableSegments = new List<PathSegment>();
    public List<PathSegment> AvailableSegments => availableSegments;
}