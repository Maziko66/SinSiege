using UnityEngine;
using UnityEngine.UI;

public class UITowerBuilderCombat : MonoBehaviour
{
    private GameManager _gameManager;

    [Header("Tower Objects")]
    [SerializeField] private GameObject towerPriest;
    [SerializeField] private GameObject towerCross;
    [SerializeField] private GameObject towerAngel;
    [SerializeField] private GameObject towerChapel;
    
    [Header("Buttons")]
    [SerializeField] private Button buttonTowerPriest;
    [SerializeField] private Button buttonTowerCross;
    [SerializeField] private Button buttonTowerAngel;
    [SerializeField] private Button buttonTowerChapel;
    
    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }
    
    void Start()
    {
        buttonTowerPriest.onClick.AddListener(() =>_gameManager.CreateTower(towerPriest));
        buttonTowerCross.onClick.AddListener(() =>_gameManager.CreateTower(towerCross));
        buttonTowerAngel.onClick.AddListener(() =>_gameManager.CreateTower(towerAngel));
        buttonTowerChapel.onClick.AddListener(() =>_gameManager.CreateTower(towerChapel));
    }

}
