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

    [SerializeField] private GameObject enemyParent;

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
    [SerializeField] private List<Enemy> enemyAliveList = new List<Enemy>();
    [SerializeField] private Vector3 spawnPosition;
    
    [Header("Spawner Variables")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private float spawnCooldown = 0.0f;
    [SerializeField] private int wavesListIndex = 0;
    [SerializeField] private List<GameObject> spawnPoints = new List<GameObject>();
    
    [Header("Current Wave")]
    [SerializeField] private WaveSO currentWave;
    [SerializeField] private float currentWaveTimer;
    [SerializeField] private bool waveActive;

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        currentWaveTimer = 30;
        buttonStartWave.onClick.AddListener(() => SetWaveTimer(0));
        CreateVectorWaypoints();
        spawnCooldown *= spawnInterval;
        //GetEnemyList();
    }

    private void Update()
    {
        WaveTimer();
        
        if (enemyList.Count > 0 && waveActive)
        {
            spawnCooldown -= Time.deltaTime;
            SpawnFromList();
        }
        // else if(enemyList.Count <= 0 && _waveActive)
        // {
        //     wavesListIndex++;
        //     GetEnemyList();
        // }

        
        
        //
        //if (enemyParent.transform.childCount == 0 && _waveActive)
        //if (enemyList.Count <= 0)
        if (enemyAliveList.Count <= 0 && waveActive)
        {
            enemyAliveList.Clear();
            waveActive = false;
            wavesListIndex++;
            //GetEnemyList();
            MusicManager.Instance.SetCombatToFalse();
            spawnCooldown = 0;
            Debug.Log("enemy alive list <= 0");
        }

    }

    private void SpawnCooldown()
    {
        
    }

    private void GetEnemyList()
    {
        enemyList.Clear();
        if (waves.Count < wavesListIndex + 1)
        {
            Debug.Log("no enemy waves found.");
            return;
        }
        currentWave = waves[wavesListIndex];
        enemyList = new List<Enemy>(currentWave.enemyList);
        //spawnPosition = waves[wavesListIndex].spawnPoint;
        spawnPosition = spawnPoints[currentWave.routeIndex].transform.position;
        SetWaveTimer(currentWave.waveCooldown);

    }

    private void SpawnFromList()
    {
        if(spawnCooldown <= 0.0f)
        {
            // GameObject instObj = Instantiate(enemyList[0].gameObject, spawnPosition, Quaternion.identity);
            GameObject instObj = Instantiate(enemyList[0].gameObject, enemyParent.transform);
            instObj.transform.position = spawnPosition;
            Enemy enemy = instObj.GetComponent<Enemy>();
            enemy.waypoints = waypoints[currentWave.routeIndex].waypoints;
            enemy.SetWaveManager(this);
            enemyList.RemoveAt(0);
            enemyAliveList.Add(enemy);
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
        if (waveActive) {return;}
        
        currentWaveTimer -= Time.deltaTime;
        textWaveTimer.SetText(_strWaveTimer + (int)currentWaveTimer);
        
        if (currentWaveTimer <= 0)
        {
            textWaveTimer.SetText(_strWaveInProgress);
            //waveActive = true;
            //MusicManager.Instance.SetCombatToTrue();
            SetWaveActive(true);
        }
    }
    
    private void SetWaveTimer(float seconds)
    {
        currentWaveTimer = seconds;
        // if (seconds <= 0)
        // {
        //     //waveActive = true;
        //     //MusicManager.Instance.SetCombatToTrue();
        //     SetWaveActive(true);
        // }
    }

    private void SetWaveActive(bool state)
    {
        if (state)
        {
            waveActive = true;
            GetEnemyList();
            MusicManager.Instance.SetCombatToTrue();
        }
        else
        {
            waveActive = false;
            MusicManager.Instance.SetCombatToFalse();
        }
    }

    public void OnEnemyDestroyed(Enemy enemy)
    {
        enemyAliveList.Remove(enemy);
    }
    
    [ContextMenu("CalculateTotalGoldOfAllWaves")]
    public void CalculateTotalGoldOfAllWaves()
    {
        foreach (WaveSO wave in waves)
        {
            wave.totalGoldValue = 0;
            foreach (Enemy enemy in wave.enemyList)
            {
                wave.totalGoldValue += enemy.coinValue;
            }
        }
    }
}
