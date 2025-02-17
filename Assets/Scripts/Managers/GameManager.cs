using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private Canvas _canvas;
    private Player _player;
    private Camera _cam;

    #region GAMEOBJECTS
    [SerializeField] private Base baseTower;

    [Header("Towers")]
    [SerializeField] private GameObject parentTowers;
    
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
    
    private Dictionary<TowerGeneric, (TowerGeneric, TowerGeneric)> possibleTowerMerges = new();
    private Dictionary<string, (string, string)> possibleTowerMergesByName = new();
    
    [SerializeField] private TowerGeneric[] _mergeArray = new TowerGeneric[2];
    [SerializeField] private int _mergeArrayIndex = 0;

    [SerializeField] private string[] _mergeArrayNames = new string[2];
    
    #endregion
    
    private void Awake()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _player = FindFirstObjectByType<Player>();
        _cam = FindFirstObjectByType<Camera>();
    }

    void Start()
    {
        //SliderBaseHealthMinMaxValueSet();
        _baseStartingHealth = baseTower.GetBaseStartingHealth();
        sliderBaseHealth.SliderMinMaxValueSet(_baseStartingHealth);
        UpdateBaseHealth();
        
        //Tier II Merges
        possibleTowerMerges.Add(towerBishop, (towerPriest, towerCross));
        possibleTowerMergesByName.Add("Bishop", ("Priest", "Cross"));
        //possibleTowerMerges.Add(towerArchangel, (towerCross, towerAngel));
        //possibleTowerMerges.Add(towerProphet, (towerPriest, towerAngel));
        //possibleTowerMerges.Add(towerVirtue, (towerAngel, towerAngel));
        //possibleTowerMerges.Add(towerChurch, (towerChapel, towerPriest));
        //Tier III Merges
        
        Debug.Log(possibleTowerMerges);
        Debug.Log(possibleTowerMerges[towerBishop]);
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
    #endregion

    #region MERGE
    public void AddToMerge(TowerGeneric tower)
    {
        if (_mergeArrayIndex > 1)
        {
            Debug.Log("Merge array is full.");
            return;
        }
        _mergeArray[_mergeArrayIndex] = tower;
        _mergeArrayNames[_mergeArrayIndex] = tower.towerName;
        uiMergeMenuCombat.towers[_mergeArrayIndex] = tower;
        _mergeArrayIndex++;

        if (_mergeArrayIndex >= 2)
        {
            //make merge button available
            Debug.Log("Merge array is full, initiating search, _mergeArrayIndex: " + _mergeArrayIndex);
            
            //(TowerGeneric, TowerGeneric) towerTuple = (_mergeArray[0], _mergeArray[1]);
            //TowerGeneric towerMerged = FindTowerByTuple(towerTuple);
            
            (string, string) towerNameTuple = (_mergeArrayNames[0], _mergeArrayNames[1]);
            string towerNameMerged = FindTowerByNameTuple(towerNameTuple);
            
            Debug.Log(towerNameMerged);
            ClearMerge();
        }
    }

    public void MergeTowers()
    {
        
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
        _mergeArrayIndex = 0;
    }
    
    private TowerGeneric FindTowerByTuple((TowerGeneric, TowerGeneric) targetTuple)
    {
        foreach (var kvp in possibleTowerMerges)
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
    
    private string FindTowerByNameTuple((string, string) targetTuple)
    {
        foreach (var kvp in possibleTowerMergesByName)
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
    
    public void CreateTower(GameObject towerToCreate)
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

        GameObject newTower = Instantiate(towerToCreate, parentTowers.transform);
        newTower.transform.position = _player.lastTouchedTowerZone.transform.position;

        zone.occupyingTower = newTower.gameObject;
        zone.isEmpty = false;

        DestroyUITowerBuilderCombat();
        DrawUITowerManagerCombat();
    }

    public void TowerDestroy()
    {
        if(_player.lastTouchedTowerZone == null)
        {
            Debug.Log("lastTouchedTowerZone == null");
            return;
        }

        TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();

        if(zone.isEmpty)
        {
            Debug.Log("zone is empty");
            return;
        }
        Destroy(zone.occupyingTower.gameObject);
        zone.isEmpty = true;

        DestroyUITowerManagerCombat();
        DrawUITowerBuilderCombat();
    }
    
    public void UpdateBaseHealth()
    {
        _baseHealth = baseTower.GetBaseHealth();
        sliderBaseHealth.SliderValueSet(_baseHealth);
        sliderBaseHealth.SliderTextSet("Base Health: " + _baseHealth + "/" + _baseStartingHealth);
    }
}
