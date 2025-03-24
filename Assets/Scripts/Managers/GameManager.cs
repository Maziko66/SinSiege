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
    private MinimapCollider _mmc;
    private Camera _cam;
    
    private BuildManager _buildManager;

    #region GAMEOBJECTS

    [SerializeField] private CinemachineCamera cineCamPlayer;
    [SerializeField] private CinemachineCamera cineCamBuild;
    [SerializeField] private Base baseTower;

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
    
    public bool onBuildMenu = false;
    public Vector2 mousePosition;
    
    #endregion
    
    private void Awake()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _player = FindFirstObjectByType<Player>();
        _cam = FindFirstObjectByType<Camera>();
        _mmc = FindFirstObjectByType<MinimapCollider>();
        _buildManager = FindFirstObjectByType<BuildManager>();
    }

    void Start()
    {
        //SliderBaseHealthMinMaxValueSet();
        _baseStartingHealth = baseTower.GetBaseStartingHealth();
        sliderBaseHealth.SliderMinMaxValueSet(_baseStartingHealth);
        UpdateBaseHealth();
        
        _buildManager.SetStateUIMergeMenuCombat(false);
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

    #endregion
    
    #region FUNCTIONAL

    private void MousePosition()
    {
        mousePosition = _cam.ScreenToWorldPoint(Input.mousePosition);
    }
    
    #endregion
    
}
