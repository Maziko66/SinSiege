using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
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
    
    [Header("Pathing")]
    [SerializeField] private Base headquarters;
    // We cache the calculated paths (List of Vector2s) here for performance
    private List<List<Vector2>> _cachedPaths = new List<List<Vector2>>();

    public List<WaveSO> waves = new List<WaveSO>();
    
    [SerializeField] private List<Enemy> enemyList = new List<Enemy>(); // Debug/View only
    [SerializeField] private List<Enemy> enemyAliveList = new List<Enemy>();
    
    [SerializeField] private List<WaveSpawnData> currentWaveConfigList = new List<WaveSpawnData>();
    [SerializeField] private List<WaveSpawnData> currentHordeConfigList = new List<WaveSpawnData>();
    
    [Header("Spawner Variables")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private float spawnCooldown = 0.0f;
    [SerializeField] private int wavesListIndex = 0;
    
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
        // Safety check if singleton exists, otherwise try GetComponent or find
        if (LevelInitializer.Instance != null)
            _upgradeManager = LevelInitializer.Instance.UpgradeManager;
        
        _cooldown = GetComponent<Cooldown>();

        // Pre-Calculate all paths from LevelData when the level starts
        if (levelData != null)
        {
            _cachedPaths.Clear();
            foreach (var mapRoute in levelData.MapRoutes)
            {
                _cachedPaths.Add(mapRoute.GetCalculatedPath());
            }
        }
    }

    private void Start()
    {
        // Ensure Init is called if not called externally
        if (_cachedPaths.Count == 0) Init();

        currentWaveTimer = 30;
        buttonStartWave.onClick.AddListener(() => SetWaveTimer(0));
        
        spawnCooldown *= spawnInterval;
    }

    private void Update()
    {
        WaveTimer();
        
        if (currentWaveConfigList.Count > 0 && waveActive)
        {
            spawnCooldown -= Time.deltaTime;
            SpawnFromList();
            if (_upgradeManager != null) _upgradeManager.hasUpgraded = false;
        }
        
        SpawnHorde();
        
        // Game Loop / Next Wave Logic
        if (currentWaveConfigList.Count == 0 && enemyAliveList.Count <= 0 && hordeAliveList.Count <= 0 && waveActive)
        {
            enemyAliveList.Clear();
            waveActive = false;
            wavesListIndex++;
            
            if (MusicManager.Instance != null) MusicManager.Instance.SetCombatToFalse();
            
            spawnCooldown = 0;
            Debug.Log("Wave Complete");
            waveNumber++;
            
            if (_upgradeManager != null && waveNumber % 5 == 0 && !_upgradeManager.hasUpgraded)
            {
                _upgradeManager.TimeToUpgrade();
            }
        }
    }

    private void GetEnemyList()
    {
        currentWaveConfigList.Clear();
        currentHordeConfigList.Clear();
    
        if (waves.Count < wavesListIndex + 1)
        {
            Debug.Log("No more waves defined.");
            return;
        }

        currentWave = waves[wavesListIndex];
    
        // Copy the configs
        currentWaveConfigList = new List<WaveSpawnData>(currentWave.enemySpawns);
        currentHordeConfigList = new List<WaveSpawnData>(currentWave.hordeSpawns);

        SetWaveTimer(currentWave.waveCooldown);
        _hordeInterval = currentWave.hordeInterval;
        _spawnHorde = currentWave.hasHorde;
    }

    private void SpawnFromList()
    {
        if (spawnCooldown <= 0.0f && currentWaveConfigList.Count > 0)
        {
            // 1. Get the Data Config
            WaveSpawnData data = currentWaveConfigList[0];

            if (data.enemyPrefab == null) 
            {
                currentWaveConfigList.RemoveAt(0);
                return;
            }

            // 2. Determine Path & Start Position
            List<Vector2> path = null;
            Vector3 startPos = Vector3.zero;
            int routeIndex = currentWave.routeIndex;

            if (routeIndex >= 0 && routeIndex < _cachedPaths.Count)
            {
                path = _cachedPaths[routeIndex];
                if(path != null && path.Count > 0) startPos = path[0];
            }
            else
            {
                Debug.LogWarning($"Wave requested Route {routeIndex} but only {_cachedPaths.Count} routes exist.");
            }

            // 3. Instantiate
            GameObject instObj = Instantiate(data.enemyPrefab.gameObject, enemyParent.transform);
            instObj.transform.position = startPos;
        
            Enemy enemy = instObj.GetComponent<Enemy>();
            enemy.followPlayer = false;
            
            // 4. Set the Path (New Method)
            enemy.SetPath(path);
            enemy.SetWaveManager(this);

            // 5. Apply Stats
            ApplyConfigToEnemy(enemy, data);

            // 6. Cleanup
            currentWaveConfigList.RemoveAt(0);
            enemyAliveList.Add(enemy); 
            spawnCooldown = spawnInterval;
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
            SetWaveActive(true);
        }
    }
    
    private void SetWaveTimer(float seconds, bool calledFromButton = false)
    {
        if (calledFromButton && waveActive)
        {
            Debug.Log("Wave is active, wait until it's over.");
            return;
        }
        currentWaveTimer = seconds;
    }

    private void SpawnHorde()
    {
        if (enemyAliveList.Count > 0 && _hordeCooldown <= 0.0f && _spawnHorde && currentHordeConfigList.Count > 0)
        {
            int randIndex = Random.Range(0, currentHordeConfigList.Count);
            WaveSpawnData data = currentHordeConfigList[randIndex];

            GameObject instObj = Instantiate(data.enemyPrefab.gameObject, enemyParent.transform);
            Vector2 spawnPos = GetHordeSpawnPosition();
            instObj.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);

            Enemy enemy = instObj.GetComponent<Enemy>();
            enemy.followPlayer = true;
            enemy.isHorde = true;
            enemy.SetWaveManager(this);
        
            ApplyConfigToEnemy(enemy, data);

            hordeAliveList.Add(enemy);
            _hordeCooldown = _hordeInterval;
        }
        _hordeCooldown -= Time.deltaTime;
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
        int edge = Random.Range(0, 4); // 0=top, 1=bottom, 2=left, 3=right

        switch (edge)
        {
            case 0: x = Random.Range(0f, 1f); y = 1f; break;
            case 1: x = Random.Range(0f, 1f); y = 0f; break;
            case 2: x = 0f; y = Random.Range(0f, 1f); break;
            case 3: x = 1f; y = Random.Range(0f, 1f); break;
        }
        return new Vector2(x, y);
    }

    private void SetWaveActive(bool state)
    {
        if (state)
        {
            waveActive = true;
            GetEnemyList();
            if (MusicManager.Instance != null) MusicManager.Instance.SetCombatToTrue();
        }
        else
        {
            waveActive = false;
            if (MusicManager.Instance != null) MusicManager.Instance.SetCombatToFalse();
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

    public void SetLevelData(LevelData levelData)
    {
        this.levelData = levelData;
    }

    public void GetWavesAndRoutesFromLevelData()
    {
        // We only load waves here now, routes are processed in Init()
        waves = new List<WaveSO>(levelData.Waves);
        Init(); // Re-cache paths
    }

    public void ResetWavesAndRoutes()
    {
        _cachedPaths.Clear();
        if (waves != null) waves.Clear(); 
        else waves = new List<WaveSO>();
    }

    private void ApplyConfigToEnemy(Enemy enemy, WaveSpawnData data)
    {
        float finalHealth = enemy.BaseHealth; 
        float finalSpeed = enemy.BaseSpeed;
        int finalDamage = enemy.BaseDamage;
        int finalCoin = enemy.BaseCoin;
        float finalExp = enemy.BaseExp;

        if (data.modificationMode == SpawnModMode.Multiplier)
        {
            finalHealth *= data.hpMultiplier;
            finalSpeed *= data.speedMultiplier;
            finalDamage = Mathf.RoundToInt(finalDamage * data.damageMultiplier);
            finalCoin = Mathf.RoundToInt(finalCoin * data.goldMultiplier);
            finalExp *= data.expMultiplier;
        }
        else if (data.modificationMode == SpawnModMode.CustomValue)
        {
            finalHealth = data.customHealth;
            finalSpeed = data.customSpeed;
            finalDamage = data.customDamage;
            finalCoin = data.customGold;
            finalExp = data.customExp;
        }

        enemy.InitializeStats(finalHealth, finalSpeed, finalDamage, finalCoin, finalExp);
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (levelData == null || levelData.MapRoutes == null) return;

        Color[] routeColors = {
            Color.red, new Color(1f, 0.64f, 0f), Color.yellow, new Color(0.6f, 1f, 0.2f),
            Color.green, new Color(0f, 0.5f, 0.5f), Color.cyan, Color.blue, Color.magenta, Color.white
        };

        for (int r = 0; r < levelData.MapRoutes.Count; r++)
        {
            var route = levelData.MapRoutes[r];
            List<Vector2> points = route.GetCalculatedPath();

            if (points.Count < 2) continue;

            Gizmos.color = routeColors[r % routeColors.Length];

            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i+1]);
                Gizmos.DrawSphere(points[i], 0.2f);
            }
            if(points.Count > 0)
                Gizmos.DrawCube(points[points.Count - 1], Vector3.one * 0.4f);
        }
    }
#endif
}