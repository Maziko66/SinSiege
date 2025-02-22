using UnityEngine;
using UnityEngine.UI;

public class UITowerBuilderCombat : MonoBehaviour
{
    private BuildManager _buildManager;

    [Header("Tower Objects")]
    [SerializeField] private TowerGeneric towerPriest;
    [SerializeField] private TowerGeneric towerCross;
    [SerializeField] private TowerGeneric towerAngel;
    [SerializeField] private TowerGeneric towerChapel;
    
    [Header("Buttons")]
    [SerializeField] private Button buttonTowerPriest;
    [SerializeField] private Button buttonTowerCross;
    [SerializeField] private Button buttonTowerAngel;
    [SerializeField] private Button buttonTowerChapel;
    
    private void Awake()
    {
        _buildManager = FindFirstObjectByType<BuildManager>();
    }
    
    void Start()
    {
        buttonTowerPriest.onClick.AddListener(() => _buildManager.CreateTower(_buildManager.TowerPriest));
        buttonTowerCross.onClick.AddListener(() => _buildManager.CreateTower(_buildManager.TowerCross));
        buttonTowerAngel.onClick.AddListener(() => _buildManager.CreateTower(_buildManager.TowerAngel));
        buttonTowerChapel.onClick.AddListener(() => _buildManager.CreateTower(_buildManager.TowerChapel));
    }

}
