using System;
using UnityEngine;
using UnityEngine.UI;

public class UITowerManagerCombat : MonoBehaviour
{
    private BuildManager _buildManager;

    private TowerGeneric _attachedTower;
   
    

    [Header("Buttons")]
    [SerializeField] private Button buttonDestroy;
    [SerializeField] private Button buttonUpgrade;
    [SerializeField] private Button buttonMerge;
    [SerializeField] private Button buttonDetails;
    
    private void Awake()
    {
        _buildManager = FindFirstObjectByType<BuildManager>();
    }

    private void Start()
    {
        buttonDestroy.onClick.AddListener(() => _buildManager.TowerDestroy());
        buttonMerge.onClick.AddListener(() => _buildManager.AddToMerge(_attachedTower));
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        // buttonUpgrade.onClick.RemoveListener(_attachedTower.UpgradeTower);
        // _attachedTower = null;
        // Debug.Log("Disable Attached tower set to: " + _attachedTower.name);
    }

    public void SetAttachedTower(TowerGeneric tower)
    {
        _attachedTower = tower;
        Debug.Log("Attached tower set to: " + _attachedTower.name);
    }

    public void SetUpgradeButtonListener()
    {
        if (_attachedTower == null)
        {
            
        }
        buttonUpgrade.onClick.RemoveListener(_attachedTower.UpgradeTower);
        buttonUpgrade.onClick.AddListener(_attachedTower.UpgradeTower);
    }

    public void RemoveUpgradeButtonListener()
    {
        if (_attachedTower == null)
        {
            Debug.Log("Attached tower is null.");
            return;
        }
        buttonUpgrade.onClick.RemoveListener(_attachedTower.UpgradeTower);
    }
}
