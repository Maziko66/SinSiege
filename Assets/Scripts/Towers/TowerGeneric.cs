using System;
using UnityEngine;

public class TowerGeneric : MonoBehaviour
{
    private GameManager _gameManager;
    
    public string towerName;
    public Sprite towerSprite;
    public TowerZone attachedZone;
    public int bulletsFired;
    public int bulletsHit;

    [SerializeField] private int towerCost = 100;

    [SerializeField] private float[] _RankBonusDamage = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] _RankBonusInterval = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] _RankBonusRange = {0.0f, 0.1f, 0.2f, 0.3f};
    
    [SerializeField] private int level;
    [SerializeField] private int[] upgradeCosts = { 100, 200, 300 };

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    public void IncreaseTowerZoneVet(float exp)
    {
        attachedZone.IncreaseVet(exp);
    }
    
    public void UpgradeTower()
    {
        if (_gameManager.coins < upgradeCosts[level])
        {
            Debug.Log("Not enough souls!");
            return;
        }
        level++;
        _gameManager.coins -= upgradeCosts[level - 1];
        _gameManager.UpdateCoinsText();
        Debug.Log(gameObject.name + " has been upgraded to level: " + level);
    }

    public int GetCost()
    {
        return towerCost;
    }
}
