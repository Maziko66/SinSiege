using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
internal class Route
{
    public List<GameObject> routepoints;
}

[System.Serializable]
internal class Waypoint
{
    public List<Vector2> waypoints;
}

public class WaveManager : MonoBehaviour
{
    //LISTS TO CREATE:
    //listSpawnPoints, listWaveGroups

    private Cooldown _cooldown;
    
    [Header("Routes")]
    [SerializeField] private List<Route> routes = new List<Route>();
    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
    
    
    
    
    public List<WaveSO> waves = new List<WaveSO>();
    private WaveSO _currentWave;
    [SerializeField] private List<Enemy> enemyList = new List<Enemy>();
    [SerializeField] private Vector3 spawnPosition;

    [Header("Spawner Variables")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private float spawnCooldown = 1.0f;
    [SerializeField] private int wavesListIndex = 0;
    [SerializeField] private List<GameObject> spawnPoints = new List<GameObject>();


    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        CreateVectorWaypoints();
        spawnCooldown *= spawnInterval;
        GetEnemyList();
    }

    private void Update()
    {
        if (enemyList.Count > 0)
        {
            spawnCooldown -= Time.deltaTime;
            SpawnFromList();
        }
        

    }


    private void SpawnCooldown()
    {
        
    }

    private void GetEnemyList()
    {
        enemyList.Clear();
        _currentWave = waves[wavesListIndex];
        enemyList = new List<Enemy>(_currentWave.enemyList);
        //spawnPosition = waves[wavesListIndex].spawnPoint;
        spawnPosition = spawnPoints[_currentWave.routeIndex].transform.position;

    }

    private void SpawnFromList()
    {
        if(spawnCooldown <= 0.0f)
        {
            GameObject instObj = Instantiate(enemyList[0].gameObject, spawnPosition, Quaternion.identity);
            Enemy enemy = instObj.GetComponent<Enemy>();
            enemy.waypoints = waypoints[_currentWave.routeIndex].waypoints;
            enemyList.RemoveAt(0);
            spawnCooldown = spawnInterval;
        }
        
    }

    private void CreateVectorWaypoints()
    {
        for (int i = 0; i < routes.Count; i++)
        {
            Waypoint newWaypoint = new Waypoint();
            newWaypoint.waypoints = new List<Vector2>();
            waypoints.Add(newWaypoint);
            for (int j = 0; j < routes[i].routepoints.Count; j++)
            {
                Vector2 position = routes[i].routepoints[j].transform.position;
                waypoints[i].waypoints.Add(position);
            }
        }
    }
}
