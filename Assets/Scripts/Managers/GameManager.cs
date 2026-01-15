using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    private Canvas _canvas;
    private Player _player;
    private MinimapCollider _mmc;
    private Camera _cam;
    
    private BuildManager _buildManager;

    #region GAMEOBJECTS

    [SerializeField] private CinemachineCamera cineCamPlayer;
    [SerializeField] private CinemachineCamera cineCamBuild;
    [SerializeField] private Base baseTower;
    
    [SerializeField] private List<Coin> CoinPrefabs;
    [SerializeField] private List<int> CoinValues;
    [SerializeField] private GameObject coinParent;
    private int _coinTypeAmount;

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
    [SerializeField] private TextMeshProUGUI textCoins;
        
    #endregion

    #region VARIABLES

    [Header("Variables")]
    public int coins;
    
    private int _baseStartingHealth;
    private int _baseHealth;
    
    public bool onBuildMenu = false;
    public Vector2 mousePosition;
    
    #endregion

    public void Init()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _player = LevelInitializer.Instance.Player;
        _cam = LevelInitializer.Instance.MainCamera;
        _mmc = FindFirstObjectByType<MinimapCollider>();
        _buildManager = LevelInitializer.Instance.BuildManager;
    }
    
    void Start()
    {
        //SliderBaseHealthMinMaxValueSet();
        _baseStartingHealth = baseTower.GetBaseStartingHealth();
        sliderBaseHealth.SliderMinMaxValueSet(_baseStartingHealth);
        UpdateBaseHealth();
        
        _buildManager.SetStateUIMergeMenuCombat(false);
        UpdateCoinsText();

        _coinTypeAmount = CoinPrefabs.Count;
    }

    private void Update()
    {
        MousePosition();
    }

    #region  UI METHODS
    
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

    public void SwapBuildAndCombatMenu()
    {
        TransitionBetweenBuildAndPlayerCameras();
        _buildManager.DestroyUITowerBuilderCombat();
        _buildManager.DestroyUITowerManagerCombat();
        onBuildMenu = !onBuildMenu;
        _mmc.gameObject.SetActive(!onBuildMenu);
        _buildManager.SetStateBuildMenuCrosshair(onBuildMenu);
    }
    
    public void TransitionBetweenBuildAndPlayerCameras()
    {
        cineCamBuild.transform.position = cineCamPlayer.transform.position;
        cineCamPlayer.gameObject.SetActive(!cineCamPlayer.isActiveAndEnabled);
        _player.SetMoveDirection(Vector2.zero);
        _player.AnimatorToIdle();
    }
    
    #endregion

    

    

    #region GAMEPLAY RELATED

    public void UpdateBaseHealth()
    {
        _baseHealth = baseTower.GetBaseHealth();
        sliderBaseHealth.SliderValueSet(_baseHealth);
        sliderBaseHealth.SliderTextSet("Base Health: " + _baseHealth + "/" + _baseStartingHealth);
    }

    public void UpdateCoinsText()
    {
        textCoins.SetText("Souls: " + coins);
    }

    public void SpawnCoins(int value, Vector3 position)
    {
        if (CoinValues.Count != CoinPrefabs.Count)
        {
            Debug.LogError("CoinValues and CoinPrefabs must have the same length.");
            return;
        }
        int div;
        for (int i = 0; i < _coinTypeAmount; i++)
        {
            div = value / CoinValues[i];
            for (int j = 0; j < div; j++)
            {
                Coin newCoin = Instantiate(CoinPrefabs[i], coinParent.transform);
                float randX = Random.Range(-.5f, .5f);
                float randY = Random.Range(-.5f, .5f);
                newCoin.transform.position = position + new Vector3(randX, randY, 0);
            }
            value -= div * CoinValues[i];
        }
        //Debug.Log("Spawned coins.");
    }

    #endregion
    
    
    #region FUNCTIONAL

    private void MousePosition()
    {
        mousePosition = _cam.ScreenToWorldPoint(Input.mousePosition);
    }

    public Player GetPlayer()
    {
        return _player;
    }
    
    #endregion
    
}
