using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour
{
    public int levelIndex;
    
    [SerializeField] private List<Route> routes = new List<Route>();
    public List<Route> Routes => routes;
    
    [SerializeField] private List<WaveSO> waves = new List<WaveSO>();
    public List<WaveSO> Waves => waves;
    
    [SerializeField] private List<GameObject> spawnPoints = new List<GameObject>();
    public List<GameObject> SpawnPoints => spawnPoints;
}
