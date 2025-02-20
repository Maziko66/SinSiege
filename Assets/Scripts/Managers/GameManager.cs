using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private Canvas _canvas;
    private Player _player;
    private Camera _cam;
    

    #region GAMEOBJECTS

    [SerializeField] private CinemachineCamera cineCamPlayer;
    [SerializeField] private CinemachineCamera cineCamBuild;
    [SerializeField] private Base baseTower;

    [Header("Towers")]
    [SerializeField] private GameObject parentTowers;
    [SerializeField] private GameObject towerZoneParent;
    
    [Header("Tier I")]
    [SerializeField] private TowerGeneric towerPriest;
    [SerializeField] private TowerGeneric towerCross;
    [SerializeField] private TowerGeneric towerAngel;
    [SerializeField] private TowerGeneric towerChapel;

    [Header("Tier II")]
    [SerializeField] private TowerGeneric towerBishop;
    [SerializeField] private TowerGeneric towerArchangel;
    [SerializeField] private TowerGeneric towerProphet;
    [SerializeField] private TowerGeneric towerVirtue;
    [SerializeField] private TowerGeneric towerChurch;

    [Header("Tier III")]
    [SerializeField] private TowerGeneric towerArchbishop;
    [SerializeField] private TowerGeneric towerDemigod;
    [SerializeField] private TowerGeneric towerBasilica;
    [SerializeField] private TowerGeneric towerCherubim;
    [SerializeField] private TowerGeneric towerGardenOfEden;
    
    [Header("Tier IV")]
    [SerializeField] private TowerGeneric towerCardinal;
    [SerializeField] private TowerGeneric towerCathedral;
    [SerializeField] private TowerGeneric towerFallenAngel;
    [SerializeField] private TowerGeneric towerSeraphim;

    #endregion
    #region UI
    
    [Header("UI")]
    [FormerlySerializedAs("_UITowerBuilderCombat")]
    [SerializeField] private UITowerBuilderCombat uiTowerBuilderCombat;
    [SerializeField] private GameObject instantiatedUITowerBuilderCombat;
    [FormerlySerializedAs("_UITowerManagerCombat")] [SerializeField] private UITowerManagerCombat uiTowerManagerCombat;
    [SerializeField] private GameObject instantiatedUITowerManagerCombat;
    
    [SerializeField] private UIMergeMenuCombat uiMergeMenuCombat;
    [SerializeField] private GameObject instantiatedUIMergeMenuCombat;
    
    //[SerializeField] private Slider sliderBaseHealth;
    [SerializeField] private UISliderHp sliderBaseHealth;
    [SerializeField] private TextMeshProUGUI textSliderBaseHealth;
    #endregion

    #region VARIABLES

    [Header("Variables")]
    private int _baseStartingHealth;
    private int _baseHealth;
    
    private readonly Dictionary<string, (string, string)> _possibleTowerMergesByName = new();
    private readonly Dictionary<string, TowerGeneric> _towerKvp = new();
    
    [SerializeField] private List<TowerZone> _towerZones = new();
    
    private TowerGeneric[] _mergeArray = new TowerGeneric[2];
    private int _mergeArrayIndex = 0;
    private string[] _mergeArrayNames = new string[2];
    private TowerZone _mergeTowerZone;
    
    public bool onBuildMenu = false;
    
    #endregion
    
    private void Awake()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _player = FindFirstObjectByType<Player>();
        _cam = FindFirstObjectByType<Camera>();
    }

    void Start()
    {
        AddTowerZonesToTowerZones();
        
        //SliderBaseHealthMinMaxValueSet();
        _baseStartingHealth = baseTower.GetBaseStartingHealth();
        sliderBaseHealth.SliderMinMaxValueSet(_baseStartingHealth);
        UpdateBaseHealth();
        
        SetStateUIMergeMenuCombat(false);
        
        _towerKvp.Add(towerPriest.towerName, towerPriest);
        _towerKvp.Add(towerCross.towerName, towerCross);
        _towerKvp.Add(towerAngel.towerName, towerAngel);
        _towerKvp.Add(towerChapel.towerName, towerChapel);
        
        _towerKvp.Add(towerBishop.towerName, towerBishop);
        
        
        //Tier II Merges
        _possibleTowerMergesByName.Add(towerBishop.towerName, (towerPriest.towerName, towerCross.towerName));
        //possibleTowerMergesByName.Add(towerArchangel, (towerCross, towerAngel));
        //possibleTowerMergesByName.Add(towerProphet, (towerPriest, towerAngel));
        //possibleTowerMergesByName.Add(towerVirtue, (towerAngel, towerAngel));
        //possibleTowerMergesByName.Add(towerChurch, (towerChapel, towerPriest));
        //Tier III Merges
    }

    #region  UI METHODS
    public void DrawUITowerBuilderCombat()
    {
        if (instantiatedUITowerBuilderCombat == null)
        {
            DestroyUITowerBuilderCombat();
        }
        instantiatedUITowerBuilderCombat = Instantiate(uiTowerBuilderCombat.gameObject, _canvas.gameObject.transform);

        RectTransform rectTransform = instantiatedUITowerBuilderCombat.GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
        //Vector3 pos = _cam.WorldToScreenPoint(_player.transform.position);
        //instantiatedUITowerBuilderCombat.transform.position = pos;
        //Debug.Log("instantiatedUITowerBuilderCombat instantiated at: " + pos);
    }
    
    public void DestroyUITowerBuilderCombat()
    {
        Destroy(instantiatedUITowerBuilderCombat.gameObject);
    }
    
    public void DrawUITowerManagerCombat()
    {
        if(instantiatedUITowerManagerCombat == null)
        {
            DestroyUITowerManagerCombat();
        }
        instantiatedUITowerManagerCombat = Instantiate(uiTowerManagerCombat.gameObject, _canvas.gameObject.transform);
        
        UITowerManagerCombat instUI = instantiatedUITowerManagerCombat.GetComponent<UITowerManagerCombat>();
        TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();
        instUI.SetAttachedTower(zone.occupyingTower.GetComponent<TowerGeneric>());
        
        RectTransform rectTransform = instantiatedUITowerManagerCombat.GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
        
        //Vector3 pos = _cam.WorldToScreenPoint(_player.transform.position);
        //instantiatedUITowerManagerCombat.transform.position = pos;
        //Debug.Log("instantiatedUITowerManagerCombat instantiated at: " + pos);
    }
    
    public void DestroyUITowerManagerCombat()
    {
        Destroy(instantiatedUITowerManagerCombat.gameObject);
    }
    
    // public void DrawUIMergeMenuCombat()
    // {
    //     if (instantiatedUIMergeMenuCombat == null)
    //     {
    //         DestroyUITowerBuilderCombat();
    //     }
    //     instantiatedUIMergeMenuCombat = Instantiate(uiMergeMenuCombat.gameObject, _canvas.gameObject.transform);
    //
    //     RectTransform rectTransform = instantiatedUITowerBuilderCombat.GetComponent<RectTransform>();
    //     rectTransform.localPosition = Vector3.zero;
    //     //Vector3 pos = _cam.WorldToScreenPoint(_player.transform.position);
    //     //instantiatedUITowerBuilderCombat.transform.position = pos;
    //     //Debug.Log("instantiatedUITowerBuilderCombat instantiated at: " + pos);
    // }

    public void SetStateUIMergeMenuCombat(bool state)
    {
        uiMergeMenuCombat.gameObject.SetActive(state);
    }

    public void SwapBuildAndCombatMenu()
    {
        TransitionBetweenBuildAndPlayerCameras();
        onBuildMenu = !onBuildMenu;
    }
    
    public void TransitionBetweenBuildAndPlayerCameras()
    {
        cineCamPlayer.gameObject.SetActive(!cineCamPlayer.isActiveAndEnabled);
    }
    #endregion

    #region MERGE
    public void AddToMerge(TowerGeneric tower)
    {
        if (_mergeArrayIndex > 1)
        {
            Debug.Log("Merge array is full.");
            return;
        }

        if (_mergeArrayIndex == 1)
        {
            if (tower == _mergeArray[0])
            {
                Debug.Log("Cannot attach same tower again.");
                return;
            }
        }
        
        _mergeArray[_mergeArrayIndex] = tower;
        _mergeArrayNames[_mergeArrayIndex] = tower.towerName;
        
        uiMergeMenuCombat.towers[_mergeArrayIndex] = tower;
        uiMergeMenuCombat.SetSlotImage(_mergeArrayIndex, tower.towerSprite);
        
        _mergeArrayIndex++;

        if (_mergeArrayIndex > 0)
        {
            SetStateUIMergeMenuCombat(true);
        }
        
        _mergeTowerZone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();

        // if (_mergeArrayIndex >= 2)
        // {
        //     //make merge button available
        //     MergeTowers();
        // }
    }

    public void MergeTowers()
    {
        Debug.Log("Merging, initiating search, _mergeArrayIndex: " + _mergeArrayIndex);

        (string, string) towerNameTuple = (_mergeArrayNames[0], _mergeArrayNames[1]);
        string towerNameMerged = FindTowerByNameTuple(towerNameTuple);
        if (_towerKvp.TryGetValue(towerNameMerged, out TowerGeneric towerMerged))
        {
            CreateTower(towerMerged, true);
            //DrawUITowerManagerCombat();
            Debug.Log(towerNameMerged);
            ClearMerge();
        }
        else
        {
            Debug.Log("Tower Not Found");
        }
    }
    
    public void RemoveFromMerge()
    {
        _mergeArray[_mergeArrayIndex - 1] = null;
        _mergeArrayNames[_mergeArrayIndex - 1] = null;
        uiMergeMenuCombat.towers[_mergeArrayIndex - 1] = null;
        _mergeArrayIndex--;
    }
    
    public void ClearMerge()
    {
        Array.Clear(_mergeArray, 0, _mergeArray.Length);
        Array.Clear(_mergeArrayNames, 0, _mergeArrayNames.Length);
        Array.Clear(uiMergeMenuCombat.towers, 0, uiMergeMenuCombat.towers.Length);
        uiMergeMenuCombat.ResetSlotImage();
        _mergeArrayIndex = 0;
        SetStateUIMergeMenuCombat(false);
    }
    
    private string FindTowerByNameTuple((string, string) targetTuple)
    {
        foreach (var kvp in _possibleTowerMergesByName)
        {
            Debug.Log("Searching tuple: " + targetTuple + "inside kvp: " + kvp);
            if ((kvp.Value.Item1.Equals(targetTuple.Item1) && kvp.Value.Item2.Equals(targetTuple.Item2)) ||
                (kvp.Value.Item1.Equals(targetTuple.Item2) && kvp.Value.Item2.Equals(targetTuple.Item1)))
            {
                Debug.Log("search successful, " + kvp);
                return kvp.Key;
            }
        }
        Debug.Log("search failed");
        return null;
    }
    
    #endregion

    #region TOWER CONSTRUCTION

    public void CreateTower(TowerGeneric towerToCreate, bool calledFromMerge = false)
    {
        if (!calledFromMerge)
        {
            if(_player.lastTouchedTowerZone == null)
            {
                Debug.Log("lastTouchedTowerZone == null");
                return;
            }

            TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();

            if (!zone.isEmpty)
            {
                Debug.Log("zone is not empty");
                return;
            }

            GameObject newTower = Instantiate(towerToCreate.gameObject, parentTowers.transform);
            TowerGeneric towerGeneric = newTower.GetComponent<TowerGeneric>();
            newTower.transform.position = _player.lastTouchedTowerZone.transform.position;

            towerGeneric.attachedZone = zone;
            zone.occupyingTower = newTower.GetComponent<TowerGeneric>();
            zone.isEmpty = false;

            DestroyUITowerBuilderCombat();
            DrawUITowerManagerCombat();

        }
        else
        {
            if (_mergeTowerZone == null)
            {
                Debug.Log("_mergeTowerZone == null");
                return;
            }
            
            //TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();
            
            GameObject newTower = Instantiate(towerToCreate.gameObject, parentTowers.transform);
            TowerGeneric towerGeneric = newTower.GetComponent<TowerGeneric>();
            
            _mergeArray[0].attachedZone.occupyingTower = null;
            _mergeArray[0].attachedZone.isEmpty = true;
            Destroy(_mergeArray[0].gameObject);
            
            _mergeArray[1].attachedZone.occupyingTower = null;
            Destroy(_mergeArray[1].gameObject);
            
            newTower.transform.position = _mergeTowerZone.transform.position;
            _mergeTowerZone.occupyingTower = towerGeneric;
            towerGeneric.attachedZone = _mergeTowerZone;
        }
        
    }

    public void TowerDestroy(bool calledFromMerge = false)
    {
        if (calledFromMerge)
        {
            Destroy(_mergeTowerZone.occupyingTower.gameObject);
            DestroyUITowerManagerCombat();
            return;
        }
        
        if(_player.lastTouchedTowerZone == null)
        {
            Debug.Log("lastTouchedTowerZone == null");
            return;
        }

        TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();
        TowerGeneric towerToDestroy = zone.occupyingTower;
        
        
        if(zone.isEmpty)
        {
            Debug.Log("zone is empty");
            return;
        }

        if (_mergeArray[0] == towerToDestroy || _mergeArray[1] == towerToDestroy)
        {
            ClearMerge();
        }
        
        Destroy(towerToDestroy.gameObject);
        zone.isEmpty = true;

        DestroyUITowerManagerCombat();
        DrawUITowerBuilderCombat();
    }

    #endregion

    #region FUNCTIONAL

    private void AddTowerZonesToTowerZones()
    {
        int childAmount = towerZoneParent.transform.childCount;

        for (int i = 0; i < childAmount; i++)
        {
            _towerZones.Add(towerZoneParent.transform.GetChild(i).GetComponent<TowerZone>());
        }
    }

    #endregion
    
    public void UpdateBaseHealth()
    {
        _baseHealth = baseTower.GetBaseHealth();
        sliderBaseHealth.SliderValueSet(_baseHealth);
        sliderBaseHealth.SliderTextSet("Base Health: " + _baseHealth + "/" + _baseStartingHealth);
    }
}
