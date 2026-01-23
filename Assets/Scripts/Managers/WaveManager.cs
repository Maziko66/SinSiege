using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[System.Serializable]
public class Route
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

    private UpgradeManager _upgradeManager;
    private Cooldown _cooldown;

    [SerializeField] private Camera _camera;

    [SerializeField] private GameObject enemyParent;

    [Header("Info")]
    [SerializeField] private int waveNumber = 1;
    [SerializeField] private LevelData levelData;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textWaveTimer;
    [SerializeField] private Button buttonStartWave;
    
    private string _strWaveTimer = "Wave Timer: ";
    private string _strWaveInProgress = "Wave in progress";
    
    [Header("Routes")]
    [SerializeField] private List<Route> routes = new List<Route>();
    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
    [SerializeField] private Base headquarters;
    private Vector2 _headquartersPosition = Vector2.zero;
    
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

    [Header("Horde")]
    [SerializeField] private List<Enemy> hordeList = new List<Enemy>();
    [SerializeField] private List<Enemy> hordeAliveList = new List<Enemy>();
    private bool _spawnHorde;
    private float _hordeInterval;
    private float _hordeCooldown;

    public void Init()
    {
        _upgradeManager = LevelInitializer.Instance.UpgradeManager;
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        currentWaveTimer = 30;
        buttonStartWave.onClick.AddListener(() => SetWaveTimer(0));
        CreateVectorWaypoints();
        spawnCooldown *= spawnInterval;
        //GetEnemyList();
        
        _headquartersPosition = headquarters.transform.position;
    }

    private void Update()
    {
        WaveTimer();
        
        if (enemyList.Count > 0 && waveActive)
        {
            spawnCooldown -= Time.deltaTime;
            SpawnFromList();
            _upgradeManager.hasUpgraded = false;
        }
        
        SpawnHorde();
        // else if(enemyList.Count <= 0 && _waveActive)
        // {
        //     wavesListIndex++;
        //     GetEnemyList();
        // }

        
        
        //
        //if (enemyParent.transform.childCount == 0 && _waveActive)
        //if (enemyList.Count <= 0)
        
        //if (enemyAliveList.Count <= 0 && hordeAliveList.Count <= 0 && waveActive)
        if (enemyList.Count == 0 && enemyAliveList.Count <= 0 && hordeAliveList.Count <= 0 && waveActive)
        {
            enemyAliveList.Clear();
            waveActive = false;
            wavesListIndex++;
            //GetEnemyList();
            MusicManager.Instance?.SetCombatToFalse();
            spawnCooldown = 0;
            Debug.Log("enemy alive list <= 0");
            waveNumber++;
            if (waveNumber % 5 == 0 && !_upgradeManager.hasUpgraded)
            {
                _upgradeManager.TimeToUpgrade();
            }
        }

    }

    private void SpawnCooldown()
    {
        
    }

    private void GetEnemyList()
    {
        enemyList.Clear();
        hordeList.Clear();
        if (waves.Count < wavesListIndex + 1)
        {
            Debug.Log("no enemy waves found.");
            return;
        }
        currentWave = waves[wavesListIndex];
        enemyList = new List<Enemy>(currentWave.enemyList);

        hordeList = new List<Enemy>(currentWave.hordeList);
        
        //spawnPosition = waves[wavesListIndex].spawnPoint;
        //spawnPosition = spawnPoints[currentWave.routeIndex].transform.position;
        
        if (currentWave.routeIndex < spawnPoints.Count)
        {
            spawnPosition = spawnPoints[currentWave.routeIndex].transform.position;
        }
        else
        {
            Debug.LogError($"Wave {waveNumber} asks for Route {currentWave.routeIndex}, but only {spawnPoints.Count} spawn points exist!");
            spawnPosition = Vector3.zero; // Fallback
        }
        
        SetWaveTimer(currentWave.waveCooldown);
        
        _hordeInterval = currentWave.hordeInterval;
        _spawnHorde = currentWave.hasHorde;
    }

    private void SpawnFromList()
    {
        if(spawnCooldown <= 0.0f)
        {
            // GameObject instObj = Instantiate(enemyList[0].gameObject, spawnPosition, Quaternion.identity);
            GameObject instObj = Instantiate(enemyList[0].gameObject, enemyParent.transform);
            instObj.transform.position = spawnPosition;
            Enemy enemy = instObj.GetComponent<Enemy>();
            enemy.followPlayer = false;
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

    private void SpawnHorde()
    {
        if (enemyAliveList.Count > 0 && _hordeCooldown <= 0.0f && _spawnHorde)
        {
            int randIndex = Random.Range(0, hordeList.Count);
            
            GameObject instObj = Instantiate(hordeList[randIndex].gameObject, enemyParent.transform);
            Vector2 spawnPos = GetHordeSpawnPosition();
            instObj.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);

            Enemy enemy = instObj.GetComponent<Enemy>();
            //enemy.waypoints.Add(_headquartersPosition);
            enemy.followPlayer = true;
            enemy.isHorde = true;
            enemy.SetWaveManager(this);
            
            hordeAliveList.Add(enemy);
            _hordeCooldown = _hordeInterval;
        }
        
        _hordeCooldown -= Time.deltaTime;
        //Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
    }

    private Vector3 GetHordeSpawnPosition()
    {
        Vector3 viewportPos = GetRandomEdgeViewportPosition();
        Vector3 worldPos = _camera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, 0));
        return worldPos;
    }
    
    Vector2 GetRandomEdgeViewportPosition()
    {
        float x = 0f;
        float y = 0f;

        int edge = Random.Range(0, 4); // 0 = top, 1 = bottom, 2 = left, 3 = right

        switch (edge)
        {
            case 0: // top
                x = Random.Range(0f, 1f);
                y = 1f;
                break;
            case 1: // bottom
                x = Random.Range(0f, 1f);
                y = 0f;
                break;
            case 2: // left
                x = 0f;
                y = Random.Range(0f, 1f);
                break;
            case 3: // right
                x = 1f;
                y = Random.Range(0f, 1f);
                break;
        }

        return new Vector2(x, y);
    }

    private void SetWaveActive(bool state)
    {
        if (state)
        {
            waveActive = true;
            GetEnemyList();
            if (MusicManager.Instance != null) { MusicManager.Instance.SetCombatToTrue(); }else{Debug.Log("MusicManager instance is null.");}
            
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

    public void OnHordeDestroyed(Enemy enemy)
    {
        hordeAliveList.Remove(enemy);
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

    public void SetLevelData(LevelData levelData)
    {
        this.levelData = levelData;
    }

    public void GetWavesAndRoutesFromLevelData()
    {
        routes = new List<Route>(levelData.Routes);
        waves = new List<WaveSO>(levelData.Waves);
    }

    public void ResetWavesAndRoutes()
    {
        if (routes != null) routes.Clear(); 
        else routes = new List<Route>();

        if (waves != null) waves.Clear(); 
        else waves = new List<WaveSO>();
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Don't draw if we have no routes
        if (routes == null || routes.Count == 0) return;

        // A list of colors to cycle through so different routes look distinct
        Color[] routeColors = {
            Color.red,
            new Color(1f, 0.64f, 0f),   // Orange
            Color.yellow,
            new Color(0.6f, 1f, 0.2f),  // Lime
            Color.green,
            new Color(0f, 0.5f, 0.5f),  // Teal
            Color.cyan,
            Color.blue,
            new Color(0.5f, 0f, 1f),    // Violet
            Color.magenta,
            new Color(1f, 0.4f, 0.7f),  // Pink
            Color.white                 // White
        };

        for (int r = 0; r < routes.Count; r++)
        {
            Route currentRoute = routes[r];
            if (currentRoute.routepoints == null || currentRoute.routepoints.Count < 2) continue;

            // Pick a color based on the route index (loops back if you have many routes)
            Gizmos.color = routeColors[r % routeColors.Length];

            for (int i = 0; i < currentRoute.routepoints.Count - 1; i++)
            {
                GameObject startNode = currentRoute.routepoints[i];
                GameObject endNode = currentRoute.routepoints[i + 1];

                // Ensure nodes haven't been deleted from the scene
                if (startNode != null && endNode != null)
                {
                    // Draw the line connecting them
                    Gizmos.DrawLine(startNode.transform.position, endNode.transform.position);

                    // Draw a small sphere at every point so we can see the "joints"
                    Gizmos.DrawSphere(startNode.transform.position, 0.2f);
                    
                    // Mark the END of the path with a different shape (Cube)
                    if (i == currentRoute.routepoints.Count - 2)
                    {
                        Gizmos.DrawCube(endNode.transform.position, Vector3.one * 0.3f);
                    }
                }
            }
        }
    }
#endif
}
