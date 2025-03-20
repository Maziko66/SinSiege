using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textWaveTimer;
    [SerializeField] private Button buttonStartWave;

    private string _strWaveTimer = "Wave Timer: ";
    private string _strWaveInProgress = "Wave in progress";
    
    [Header("Routes")]
    [SerializeField] private List<Route> routes = new List<Route>();
    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
    
    public List<WaveSO> waves = new List<WaveSO>();
    
    
    [SerializeField] private List<Enemy> enemyList = new List<Enemy>();
    [SerializeField] private Vector3 spawnPosition;
    
    [Header("Spawner Variables")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private float spawnCooldown = 1.0f;
    [SerializeField] private int wavesListIndex = 0;
    [SerializeField] private List<GameObject> spawnPoints = new List<GameObject>();
    
    private WaveSO _currentWave;
    private float _currentWaveTimer;
    private bool _waveActive;

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        buttonStartWave.onClick.AddListener(() => SetWaveTimer(0));
        CreateVectorWaypoints();
        spawnCooldown *= spawnInterval;
        GetEnemyList();
    }

    private void Update()
    {
        WaveTimer();
        
        if (enemyList.Count > 0 && _waveActive)
        {
            spawnCooldown -= Time.deltaTime;
            SpawnFromList();
        }

        if (enemyList.Count <= 0)
        {
            _waveActive = false;
            wavesListIndex++;
            GetEnemyList();
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
        SetWaveTimer(_currentWave.waveCooldown);

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

    private void WaveTimer()
    {
        if (_waveActive) {return;}
        _currentWaveTimer -= Time.deltaTime;
        textWaveTimer.SetText(_strWaveTimer + (int)_currentWaveTimer);
        if (_currentWaveTimer <= 0)
        {
            textWaveTimer.SetText(_strWaveInProgress);
            _waveActive = true;
        }
    }
    
    private void SetWaveTimer(float seconds)
    {
        _currentWaveTimer = seconds;
    }
}
