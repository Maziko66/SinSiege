using System;
using UnityEngine;
using UnityEngine.UI;

public class UIMergeMenuCombat : MonoBehaviour
{
    private GameManager _gameManager;
    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    [SerializeField] private Button buttonClear;
    [SerializeField] private Button buttonMerge;

    [SerializeField] private Image slot1;
    [SerializeField] private Image slot2;
    
    public TowerGeneric[] towers = new TowerGeneric[2];
    
    void Start()
    {
        buttonClear.onClick.AddListener(() => _gameManager.ClearMerge());
        buttonMerge.onClick.AddListener(() => _gameManager.MergeTowers());
    }
    
}
