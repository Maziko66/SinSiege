using UnityEngine;
using UnityEngine.UI;

public class UITowerManagerCombat : MonoBehaviour
{
    private GameManager _gameManager;

    private TowerGeneric _attachedTower;

    [Header("Buttons")]
    [SerializeField] private Button buttonDestroy;
    [SerializeField] private Button buttonUpgrade;
    [SerializeField] private Button buttonMerge;
    [SerializeField] private Button buttonDetails;
    
    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        buttonDestroy.onClick.AddListener(_gameManager.TowerDestroy);
        buttonMerge.onClick.AddListener(() => _gameManager.AddToMerge(_attachedTower));
    }

    public void SetAttachedTower(TowerGeneric tower)
    {
        _attachedTower = tower;
    }
}
