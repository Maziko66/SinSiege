using UnityEngine;
using UnityEngine.UI;

public class UITowerManagerCombat : MonoBehaviour
{
    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    [Header("Buttons")]
    public Button buttonTowerDestroy;

    void Start()
    {
        buttonTowerDestroy.onClick.AddListener(_gameManager.TowerDestroy);
    }
}
