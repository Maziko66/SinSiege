using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class LevelInitializer : MonoBehaviour
{
    public static LevelInitializer Instance { get; private set; }

    [SerializeField] private List<LevelData> levelDatas  = new List<LevelData>();
    public List<LevelData> LevelDatas => levelDatas;

    public int levelIndex;
    
    [Header("References")]
    [field: SerializeField] public Player Player { get; private set; }
    [field: SerializeField] public Camera MainCamera { get; private set; }
    [field: SerializeField] public CinemachineCamera CinemachineCamera { get; private set; }
    [field: SerializeField] public CinemachineCamera CinemachineCameraBuildMode { get; private set; }

    [Header("Managers")]
    [field: SerializeField] public ArrowManager ArrowManager { get; private set; }
    [field: SerializeField] public BuildManager BuildManager { get; private set; }
    [field: SerializeField] public GameManager GameManager { get; private set; }
    [field: SerializeField] public MouseManager MouseManager { get; private set; }
    [field: SerializeField] public MusicManager MusicManager { get; private set; }
    [field: SerializeField] public SortingManager SortingManager { get; private set; }
    [field: SerializeField] public SoundManager SoundManager { get; private set; }
    [field: SerializeField] public TowerManager TowerManager { get; private set; }
    [field: SerializeField] public UpgradeManager UpgradeManager { get; private set; }
    [field: SerializeField] public WaveManager WaveManager { get; private set; }
    [field: SerializeField] public PersistentManager PersistentManager { get; private set; }
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        FindManagersAndObjects();
    }

    private void FindManagersAndObjects()
    {
        ArrowManager ??= FindFirstObjectByType<ArrowManager>();
        BuildManager ??= FindFirstObjectByType<BuildManager>();
        GameManager ??= FindFirstObjectByType<GameManager>();
        MouseManager ??= FindFirstObjectByType<MouseManager>();
        MusicManager ??= FindFirstObjectByType<MusicManager>();
        SortingManager ??= FindFirstObjectByType<SortingManager>();
        SoundManager ??= FindFirstObjectByType<SoundManager>();
        TowerManager ??= FindFirstObjectByType<TowerManager>();
        UpgradeManager ??= FindFirstObjectByType<UpgradeManager>();
        WaveManager ??= FindFirstObjectByType<WaveManager>();
        PersistentManager ??= FindFirstObjectByType<PersistentManager>();
        
        if (MainCamera == null) 
        {
            MainCamera = Camera.main;
        }
        
        CinemachineCamera ??= GameObject.FindGameObjectWithTag("CinemachineCam").GetComponent<CinemachineCamera>();
        CinemachineCameraBuildMode ??= GameObject.FindGameObjectWithTag("CinemachineBuildCam").GetComponent<CinemachineCamera>();
        
        Player ??= FindFirstObjectByType<Player>();
        Player.Init();
        
        ArrowManager.Init();
        BuildManager.Init();
        GameManager.Init();
        MouseManager.Init();
        WaveManager.Init();
    }
}
