using System;
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
    [SerializeField] private Tower _towerTest; 
    
    #endregion
    #region UI
    
    [Header("UI")]
    [FormerlySerializedAs("_UITowerBuilderCombat")]
    [SerializeField] private UITowerBuilderCombat uiTowerBuilderCombat;
    [SerializeField] private GameObject instantiatedUITowerBuilderCombat;
    [FormerlySerializedAs("_UITowerManagerCombat")] [SerializeField] private UITowerManagerCombat uiTowerManagerCombat;
    [SerializeField] private GameObject instantiatedUITowerManagerCombat;
    
    //[SerializeField] private Slider sliderBaseHealth;
    [SerializeField] private UISliderHp sliderBaseHealth;
    [SerializeField] private TextMeshProUGUI textSliderBaseHealth;
    #endregion

    #region VARIABLES

    [Header("Variables")]
    private int _baseStartingHealth;
    private int _baseHealth;
    
    [SerializeField] private GameObject[] _mergeArray = new GameObject[2];
    [SerializeField] private int _mergeArrayIndex = 0;
    
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
    }

    // Update is called once per frame

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
        instUI.SetAttachedTower(zone.occupyingTower);
        
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

    public void AddToMerge(GameObject tower)
    {
        if (_mergeArrayIndex > 1)
        {
            Debug.Log("Merge array is full.");
            return;
        }
        _mergeArray[_mergeArrayIndex] = tower;
        _mergeArrayIndex++;
    }
    
    public void RemoveFromMerge()
    {
        _mergeArray[_mergeArrayIndex - 1] = null;
        _mergeArrayIndex--;
    }
    
    public void ClearMerge()
    {
        Array.Clear(_mergeArray, 0, _mergeArray.Length);
        _mergeArrayIndex = 0;
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
