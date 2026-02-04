using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks the state of a single WaveSlot spawner within a group
/// </summary>
[System.Serializable]
public class WaveSpawnerState
{
    public WaveSO wave;
    public List<WaveSpawnData> remainingSpawns = new List<WaveSpawnData>();
    public float spawnCooldown;
    public int routeIndex;
    public bool isWaitingForSameRoute;
    
    public bool IsComplete => remainingSpawns.Count == 0;
}

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
    private List<List<Vector2>> _cachedPaths = new List<List<Vector2>>();

    [Header("Wave Groups")]
    [SerializeField] private int currentWaveGroupIndex = 0;
    [SerializeField] private List<WaveSpawnerState> activeSpawners = new List<WaveSpawnerState>();
    
    [SerializeField] private List<Enemy> enemyAliveList = new List<Enemy>();
    
    [SerializeField] private List<WaveSpawnData> currentHordeConfigList = new List<WaveSpawnData>();
    
    [Header("Current Wave")]
    [SerializeField] private float currentWaveTimer;
    [SerializeField] private bool waveActive;
    [SerializeField] private bool allWavesCompleted;

    [Header("Horde")]
    [SerializeField] private List<Enemy> hordeAliveList = new List<Enemy>();
    private bool _spawnHorde;
    private float _hordeInterval;
    private float _hordeCooldown;

    public void Init()
    {
        if (LevelInitializer.Instance != null)
            _upgradeManager = LevelInitializer.Instance.UpgradeManager;
        
        _cooldown = GetComponent<Cooldown>();

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
        if (_cachedPaths.Count == 0) Init();

        currentWaveTimer = 30;
        buttonStartWave.onClick.AddListener(() => SetWaveTimer(0));
    }

    private void Update()
    {
        if (allWavesCompleted) return;
        
        WaveTimer();
        
        if (waveActive)
        {
            UpdateSpawners();
            
            if (_upgradeManager != null) _upgradeManager.hasUpgraded = false;
        }
        
        SpawnHorde();
        
        // Wave complete check
        bool allSpawningComplete = activeSpawners.Count == 0;
        if (allSpawningComplete && enemyAliveList.Count <= 0 && hordeAliveList.Count <= 0 && waveActive)
        {
            enemyAliveList.Clear();
            waveActive = false;
            
            if (MusicManager.Instance != null) MusicManager.Instance.SetCombatToFalse();
            
            Debug.Log("Wave Complete");
            waveNumber++;
            currentWaveGroupIndex++;
            
            // Check if all waves are done
            if (levelData.WaveGroups == null || currentWaveGroupIndex >= levelData.WaveGroups.Count)
            {
                allWavesCompleted = true;
                Debug.Log("Congrats! No waves left.");
                return;
            }
            
            if (_upgradeManager != null && waveNumber % 5 == 0 && !_upgradeManager.hasUpgraded)
            {
                _upgradeManager.TimeToUpgrade();
            }
        }
    }

    private void StartWaveGroup()
    {
        activeSpawners.Clear();
        currentHordeConfigList.Clear();
        _spawnHorde = false;
        _hordeInterval = float.MaxValue;
        
        if (levelData.WaveGroups == null || currentWaveGroupIndex >= levelData.WaveGroups.Count)
        {
            Debug.Log("Congrats! No waves left.");
            allWavesCompleted = true;
            return;
        }
        
        WaveGroup group = levelData.WaveGroups[currentWaveGroupIndex];
        
        if (group.waveSlots == null || group.waveSlots.Count == 0)
        {
            Debug.Log("Wave group is empty.");
            return;
        }
        
        // Create spawner states for each WaveSlot in the group
        foreach (var slot in group.waveSlots)
        {
            if (slot.wave == null) continue;
            
            WaveSpawnerState spawner = new WaveSpawnerState
            {
                wave = slot.wave,
                remainingSpawns = new List<WaveSpawnData>(slot.wave.enemySpawns),
                spawnCooldown = 0f,
                routeIndex = slot.routeIndex,
                isWaitingForSameRoute = false
            };
            
            activeSpawners.Add(spawner);
            
            // Combine horde configs
            if (slot.wave.hasHorde && slot.wave.hordeSpawns != null)
            {
                currentHordeConfigList.AddRange(slot.wave.hordeSpawns);
                _spawnHorde = true;
                _hordeInterval = Mathf.Min(_hordeInterval, slot.wave.hordeInterval);
            }
        }
        
        SetWaveTimer(group.GetWaveCooldown());
    }

    private void UpdateSpawners()
    {
        HashSet<int> routesInUse = new HashSet<int>();
        
        // First pass: determine which routes are blocked
        for (int i = 0; i < activeSpawners.Count; i++)
        {
            var spawner = activeSpawners[i];
            if (spawner.IsComplete) continue;
            
            bool blockedByEarlier = false;
            for (int j = 0; j < i; j++)
            {
                if (!activeSpawners[j].IsComplete && activeSpawners[j].routeIndex == spawner.routeIndex)
                {
                    blockedByEarlier = true;
                    break;
                }
            }
            
            spawner.isWaitingForSameRoute = blockedByEarlier;
            
            if (!blockedByEarlier)
            {
                routesInUse.Add(spawner.routeIndex);
            }
        }
        
        // Second pass: update spawners that aren't blocked
        for (int i = activeSpawners.Count - 1; i >= 0; i--)
        {
            var spawner = activeSpawners[i];
            
            if (spawner.IsComplete)
            {
                activeSpawners.RemoveAt(i);
                continue;
            }
            
            if (spawner.isWaitingForSameRoute) continue;
            
            spawner.spawnCooldown -= Time.deltaTime;
            
            if (spawner.spawnCooldown <= 0f)
            {
                SpawnFromState(spawner);
            }
        }
    }

    private void SpawnFromState(WaveSpawnerState spawner)
    {
        if (spawner.remainingSpawns.Count == 0) return;
        
        WaveSpawnData data = spawner.remainingSpawns[0];

        if (data.enemyPrefab == null) 
        {
            spawner.remainingSpawns.RemoveAt(0);
            return;
        }

        List<Vector2> path = null;
        Vector3 startPos = Vector3.zero;
        int routeIndex = spawner.routeIndex;

        if (routeIndex >= 0 && routeIndex < _cachedPaths.Count)
        {
            path = _cachedPaths[routeIndex];
            if (path != null && path.Count > 0) startPos = path[0];
        }
        else
        {
            Debug.LogWarning($"Wave requested Route {routeIndex} but only {_cachedPaths.Count} routes exist.");
        }

        GameObject instObj = Instantiate(data.enemyPrefab.gameObject, enemyParent.transform);
        instObj.transform.position = startPos;
    
        Enemy enemy = instObj.GetComponent<Enemy>();
        enemy.followPlayer = false;
        
        enemy.SetPath(path);
        enemy.SetWaveManager(this);

        ApplyConfigToEnemy(enemy, data);

        spawner.remainingSpawns.RemoveAt(0);
        enemyAliveList.Add(enemy);
        
        if (spawner.remainingSpawns.Count > 0)
        {
            spawner.spawnCooldown = spawner.wave.GetSpawnInterval(spawner.remainingSpawns[0]);
        }
        else
        {
            spawner.spawnCooldown = spawner.wave.defaultSpawnInterval;
        }
    }

    private void WaveTimer()
    {
        if (waveActive) return;
        
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
        int edge = Random.Range(0, 4);

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
            StartWaveGroup();
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
        Init();
    }

    public void ResetWavesAndRoutes()
    {
        _cachedPaths.Clear();
        activeSpawners.Clear();
        currentWaveGroupIndex = 0;
        allWavesCompleted = false;
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