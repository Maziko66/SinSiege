using UnityEngine;
using UnityEngine.UI;

public class UITowerBuilderCombat : MonoBehaviour
{
    GameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    [Header("Buttons")]
    public Button buttonTowerTest;

    void Start()
    {
        buttonTowerTest.onClick.AddListener(gameManager.TowerCreateTest);
    }

}
