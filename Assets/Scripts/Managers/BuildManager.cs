using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BuildManager : MonoBehaviour
{
    private Canvas _canvas;
    private Player _player;
    private GameManager _gameManager;
    
    [Header("UI")]
    [SerializeField] private UITowerBuilderCombat uiTowerBuilderCombat;
    [SerializeField] private UITowerManagerCombat uiTowerManagerCombat;
    [SerializeField] private UIMergeMenuCombat uiMergeMenuCombat;
    [SerializeField] private UISliderHp sliderTowerZoneXP;
    [SerializeField] private Vector3 sliderTowerZoneExpOffset;
    [SerializeField] private GameObject buildMenuCrosshair;
    [SerializeField] private GameObject buildMenuChecker;


    [Header("GameObjects")]
    [SerializeField] private GameObject parentTowers;
    [SerializeField] private GameObject towerZoneParent;
    [FormerlySerializedAs("_towerZones")] [SerializeField] private List<TowerZone> towerZones = new();
    
    [Header("Towers")]
    
    [Header("Tier I")]
    [SerializeField] private TowerGeneric towerPriest;
    [SerializeField] private TowerGeneric towerCross;
    [SerializeField] private TowerGeneric towerAngel;
    [SerializeField] private TowerGeneric towerChapel;
    public TowerGeneric TowerPriest => towerPriest;
    public TowerGeneric TowerCross => towerCross;
    public TowerGeneric TowerAngel => towerAngel;
    public TowerGeneric TowerChapel => towerChapel;

    [Header("Tier II")]
    [SerializeField] private TowerGeneric towerBishop;
    [SerializeField] private TowerGeneric towerArchangel;
    [SerializeField] private TowerGeneric towerProphet;
    [SerializeField] private TowerGeneric towerVirtue;
    [SerializeField] private TowerGeneric towerChurch;
    public TowerGeneric TowerBishop => towerBishop;
    public TowerGeneric TowerArchangel => towerArchangel;
    public TowerGeneric TowerProphet => towerProphet;
    public TowerGeneric TowerVirtue => towerVirtue;
    public TowerGeneric TowerChurch => towerChurch;

    [Header("Tier III")]
    [SerializeField] private TowerGeneric towerArchbishop;
    [SerializeField] private TowerGeneric towerDemigod;
    [SerializeField] private TowerGeneric towerBasilica;
    [SerializeField] private TowerGeneric towerCherubim;
    [SerializeField] private TowerGeneric towerGardenOfEden;
    public TowerGeneric TowerArchbishop => towerArchbishop;
    public TowerGeneric TowerDemigod => towerDemigod;
    public TowerGeneric TowerBasilica => towerBasilica;
    public TowerGeneric TowerCherubim => towerCherubim;
    public TowerGeneric TowerGardenOfEden => towerGardenOfEden;
    
    [Header("Tier IV")]
    [SerializeField] private TowerGeneric towerCardinal;
    [SerializeField] private TowerGeneric towerCathedral;
    [SerializeField] private TowerGeneric towerFallenAngel;
    [SerializeField] private TowerGeneric towerSeraphim;
    public TowerGeneric TowerCardinal => towerCardinal;
    public TowerGeneric TowerCathedral => towerCathedral;
    public TowerGeneric TowerFallenAngel => towerFallenAngel;
    public TowerGeneric TowerSeraphim => towerSeraphim;
    
    private RectTransform _uiTowerBuilderCombatRectTransform;
    private RectTransform _uiTowerManagerCombatRectTransform;
    
    private readonly Dictionary<string, (string, string)> _possibleTowerMergesByName = new();
    private readonly Dictionary<string, TowerGeneric> _towerKvp = new();
    
    private TowerGeneric[] _mergeArray = new TowerGeneric[2];
    private int _mergeArrayIndex = 0;
    private string[] _mergeArrayNames = new string[2];
    private TowerZone _mergeTowerZone;
    

    private void Awake()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _player = FindFirstObjectByType<Player>();
        
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        SetStateBuildMenuCrosshair(false);
        AddTowerZonesToTowerZones();
        
        _uiTowerBuilderCombatRectTransform = uiTowerBuilderCombat.GetComponent<RectTransform>();
        _uiTowerManagerCombatRectTransform = uiTowerManagerCombat.GetComponent<RectTransform>();
        
        uiTowerBuilderCombat.gameObject.SetActive(false);
        uiTowerManagerCombat.gameObject.SetActive(false);
        
        _towerKvp.Add(TowerPriest.towerName, TowerPriest);
        _towerKvp.Add(TowerCross.towerName, TowerCross);
        _towerKvp.Add(TowerAngel.towerName, TowerAngel);
        _towerKvp.Add(TowerChapel.towerName, TowerChapel);
        
        _towerKvp.Add(TowerBishop.towerName, TowerBishop);
        
        
        //Tier II Merges
        _possibleTowerMergesByName.Add(TowerBishop.towerName, (TowerPriest.towerName, TowerCross.towerName));
        //possibleTowerMergesByName.Add(towerArchangel, (towerCross, towerAngel));
        //possibleTowerMergesByName.Add(towerProphet, (towerPriest, towerAngel));
        //possibleTowerMergesByName.Add(towerVirtue, (towerAngel, towerAngel));
        //possibleTowerMergesByName.Add(towerChurch, (towerChapel, towerPriest));
        //Tier III Merges
        
        TowerZoneExpSliderActive(false);
    }
    

    private void Update()
    {
        // if (_gameManager.onBuildMenu)
        // {
        //     Collider2D mouseOverlapCollider = Physics2D.OverlapPoint(_gameManager.mousePosition);
        //     //Debug.Log("Collider: " + (mouseOverlapCollider != null ? mouseOverlapCollider.name : "None"));
        //
        //     if (mouseOverlapCollider != null)
        //     {
        //         if (mouseOverlapCollider.CompareTag("Tower Zone") || mouseOverlapCollider.CompareTag("Tower Zone Extended"))
        //         {
        //             TowerZone towerZone = mouseOverlapCollider.GetComponent<TowerZone>();
        //             _lastTouchedTowerZone = towerZone.gameObject;
        //             //Vector3 instantiatePosition = _cam.WorldToScreenPoint(mouseOverlapCollider.transform.position);
        //             if(towerZone.isEmpty)
        //             {
        //                 DrawUITowerBuilderCombat(true);
        //                 Debug.Log("On Tower Zone Empty");
        //             }
        //             else
        //             {
        //                 DrawUITowerManagerCombat();
        //                 Debug.Log("On Tower Zone Full");
        //             }
        //         }
        //         //Debug.Log("Mouse is over: " + mouseOverlapCollider.gameObject.name);
        //     }
        //     else if (_lastTouchedTowerZone != null)
        //     {
        //         Destroy(_lastTouchedTowerZone.gameObject);
        //     }
        // }
        SetTowerZoneExpSliderPosition();
    }

    #region UI DRAW

    public void TowerZoneExpSliderActive(bool state)
    {
        sliderTowerZoneXP.gameObject.SetActive(state);
        if(_player.lastTouchedTowerZone == null) {return;}
        TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();
        sliderTowerZoneXP.SliderMinMaxValueSet(zone.GetMaxVet());
        sliderTowerZoneXP.SliderValueSet(zone.GetVet());
    }
    
    public UISliderHp GetZoneXpSlider()
    {
        return sliderTowerZoneXP;
    }

    private void SetTowerZoneExpSliderPosition()
    {
        if(_player.lastTouchedTowerZone == null) {return;}
        GameObject zone = _player.lastTouchedTowerZone;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(zone.transform.position + sliderTowerZoneExpOffset);

        TowerZone towerZone = zone.GetComponent<TowerZone>();
        towerZone.SetSliderText();

        sliderTowerZoneXP.transform.position = screenPosition;
    }
    
    public void DrawUITowerBuilderCombat(bool calledFromBuildMenu = false)
    {
        uiTowerBuilderCombat.gameObject.SetActive(true);
        //RectTransform rectTransform = uiTowerBuilderCombat.GetComponent<RectTransform>();
        if (calledFromBuildMenu)
        {
            //rectTransform.localPosition = Camera.main.WorldToScreenPoint(_lastTouchedTowerZone.transform.position);
            Vector3 worldPosition = _player.lastTouchedTowerZone.transform.position;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                screenPosition,
                null,
                out Vector2 localPosition
                );
            
            _uiTowerBuilderCombatRectTransform.localPosition = localPosition;
        }
        else
        {
            _uiTowerBuilderCombatRectTransform.localPosition = Vector3.zero;
        }
    }
    
    public void DestroyUITowerBuilderCombat()
    {
        if (uiTowerBuilderCombat == null)
        {
            Debug.Log("uiTowerBuilderCombat == null");
            return;
        }
        uiTowerBuilderCombat.gameObject.SetActive(false);
    }
    
    public void DrawUITowerManagerCombat(bool calledFromBuildMenu = false)
    {
        uiTowerManagerCombat.gameObject.SetActive(true);
        TowerZone zone = _player.lastTouchedTowerZone.GetComponent<TowerZone>();
        uiTowerManagerCombat.SetAttachedTower(zone.occupyingTower.GetComponent<TowerGeneric>());
        
        if (calledFromBuildMenu)
        {
            Vector3 worldPosition = zone.transform.position;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                screenPosition,
                null,
                out Vector2 localPosition
                );
            
            _uiTowerManagerCombatRectTransform.localPosition = localPosition;
        }
        else
        {
            _uiTowerManagerCombatRectTransform.localPosition = Vector3.zero;
        }
        uiTowerManagerCombat.SetUpgradeButtonListener();
    }
    
    public void DrawUITowerBuilderBuild()
    {
        
    }
    
    public void DestroyUITowerManagerCombat()
    {
        if (uiTowerBuilderCombat == null)
        {
            Debug.Log("uiTowerBuilderCombat is null.");
            return;
        }
        uiTowerManagerCombat.RemoveUpgradeButtonListener();
        uiTowerManagerCombat.gameObject.SetActive(false);
    }

    #endregion
    
    #region TOWER CONSTRUCTION

    public void CreateTower(TowerGeneric towerToCreate, bool calledFromMerge = false)
    {
        if (_gameManager.coins < towerToCreate.GetCost())
        {
            Debug.Log("Not enough souls to buy tower, need " + (towerToCreate.GetCost() - _gameManager.coins) + " more souls.");
            return;
        }
        
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
            DrawUITowerManagerCombat(_gameManager.onBuildMenu);

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
        
        _gameManager.coins -= towerToCreate.GetCost();
        _gameManager.UpdateCoinsText();
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
        DrawUITowerBuilderCombat(_gameManager.onBuildMenu);
    }

    #endregion

    #region MERGE COMBAT

    public void SetStateUIMergeMenuCombat(bool state)
    {
        uiMergeMenuCombat.gameObject.SetActive(state);
    }

    public void AddToUIMergeMenuCombat(int index, TowerGeneric tower)
    {
        uiMergeMenuCombat.towers[index] = tower;
        uiMergeMenuCombat.SetSlotImage(index, tower.towerSprite);
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
        
        AddToUIMergeMenuCombat(_mergeArrayIndex, tower);
        
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
    
    private void AddTowerZonesToTowerZones()
    {
        int childAmount = towerZoneParent.transform.childCount;

        for (int i = 0; i < childAmount; i++)
        {
            towerZones.Add(towerZoneParent.transform.GetChild(i).GetComponent<TowerZone>());
        }
    }

    public void SetStateBuildMenuCrosshair(bool onBuildMenu)
    {
        buildMenuCrosshair.SetActive(onBuildMenu);
        buildMenuChecker.SetActive(onBuildMenu);
    }
}
