using System.Linq.Expressions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private Canvas canvas;
    private Player player;

    #region GAMEOBJECTS
    [SerializeField] private Base baseTower;
    [Header("Towers")]
    [SerializeField] private GameObject _parentTowers;
    [SerializeField] private Tower _towerTest; 
    
    #endregion
    #region UI
    
    [Header("UI")]
    [FormerlySerializedAs("_UITowerBuilderCombat")]
    [SerializeField] private UITowerBuilderCombat uiTowerBuilderCombat;
    [SerializeField] private GameObject instantiatedUITowerBuilderCombat;
    [FormerlySerializedAs("_UITowerManagerCombat")] [SerializeField] private UITowerManagerCombat uiTowerManagerCombat;
    [SerializeField] private GameObject instantiatedUITowerManagerCombat;
    
    [SerializeField] private Slider sliderBaseHealth;
    [SerializeField] private TextMeshProUGUI textSliderBaseHealth;
    #endregion

    #region VARIABLES

    [Header("Variables")]
    private int _baseStartingHealth;
    private int _baseHealth;
    

    #endregion
    private void Awake()
    {
        canvas = FindFirstObjectByType<Canvas>();
        player = FindFirstObjectByType<Player>();
    }

    void Start()
    {
        SliderBaseHealthMinMaxValueSet();
    }

    // Update is called once per frame

    public void DrawUITowerBuilderCombat(Vector3 pos)
    {
        if (instantiatedUITowerBuilderCombat == null)
        {
            DestroyUITowerBuilderCombat();
        }
        instantiatedUITowerBuilderCombat = Instantiate(uiTowerBuilderCombat.gameObject, canvas.gameObject.transform);
        instantiatedUITowerBuilderCombat.transform.position = pos;
        
    }

    public void DestroyUITowerBuilderCombat()
    {
        Destroy(instantiatedUITowerBuilderCombat.gameObject);
    }

    public void DrawUITowerManagerCombat(Vector3 pos)
    {
        if(instantiatedUITowerManagerCombat == null)
        {
            DestroyUITowerManagerCombat();
        }
        instantiatedUITowerManagerCombat = Instantiate(uiTowerManagerCombat.gameObject, canvas.gameObject.transform);
        instantiatedUITowerManagerCombat.transform.position = pos;

    }

    public void DestroyUITowerManagerCombat()
    {
        Destroy(instantiatedUITowerManagerCombat.gameObject);
    }

    public void TowerCreateTest()
    {
        if(player.lastTouchedTowerZone == null)
        {
            Debug.Log("lastTouchedTowerZone == null");
            return;
        }

        TowerZone zone = player.lastTouchedTowerZone.GetComponent<TowerZone>();

        if (!zone.isEmpty)
        {
            Debug.Log("zone is not empty");
            return;
        }

        GameObject newTower = Instantiate(_towerTest.gameObject, _parentTowers.transform);
        newTower.transform.position = player.lastTouchedTowerZone.transform.position;

        zone.occupyingTower = newTower.GetComponent<Tower>();
        zone.isEmpty = false;

        DestroyUITowerBuilderCombat();
    }

    public void TowerDestroy()
    {
        if(player.lastTouchedTowerZone == null)
        {
            Debug.Log("lastTouchedTowerZone == null");
            return;
        }

        TowerZone zone = player.lastTouchedTowerZone.GetComponent<TowerZone>();

        if(zone.isEmpty)
        {
            Debug.Log("zone is empty");
            return;
        }
        Destroy(zone.occupyingTower.gameObject);
        zone.isEmpty = true;

        DestroyUITowerManagerCombat();
    }

    private void SliderBaseHealthMinMaxValueSet()
    {
        _baseStartingHealth = baseTower.GetBaseStartingHealth();
        sliderBaseHealth.maxValue = _baseStartingHealth;
        sliderBaseHealth.minValue = 0;
        UpdateBaseHealth();
    }
    public void UpdateBaseHealth()
    {
        _baseHealth = baseTower.GetBaseHealth();
        sliderBaseHealth.value = _baseHealth;
        SetTextSliderBaseHealth();
    }

    private void SetTextSliderBaseHealth()
    {
        textSliderBaseHealth.SetText("Base Health: " + _baseHealth + "/" + _baseStartingHealth);
    }
}
