using UnityEngine;
using UnityEngine.UI;

public class UITowerManagerCombat : MonoBehaviour
{
    GameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    [Header("Buttons")]
    public Button buttonTowerDestroy;

    void Start()
    {
        buttonTowerDestroy.onClick.AddListener(gameManager.TowerDestroy);
    }
}
