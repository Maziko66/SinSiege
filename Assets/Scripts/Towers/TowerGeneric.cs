using UnityEngine;

public class TowerGeneric : MonoBehaviour
{
    private GameManager _gameManager;
    
    
    [Header("Attributes")]
    [SerializeField] private float attackRangeDefault = 12f;
    [SerializeField] private float attackIntervalDefault = 1f;
    [SerializeField] private float attackDamageDefault = 1f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    
    public string towerName;
    public Sprite towerSprite;
    public TowerZone attachedZone;
    public int bulletsFired;
    public int bulletsHit;

    [SerializeField] private int towerCost = 100;

    [SerializeField] private float[] rankBonusDamage = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] rankBonusInterval = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] rankBonusRange = {0.0f, 0.1f, 0.2f, 0.3f};
    
    [SerializeField] private float[] levelBonusDamage = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] levelBonusInterval = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] levelBonusRange = {0.0f, 0.1f, 0.2f, 0.3f};
    
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

    # region GETTERS
    public int GetCost()
    {
        return towerCost;
    }

    public float GetAttackRange()
    {
        return attackRange;
    }

    public float GetAttackDamage()
    {
        return attackDamage;
    }

    public float GetAttackInterval()
    {
        return attackInterval;
    }
    
    #endregion
    
    private void UpdateDamageAttribute()
    {
        attackDamage *= (rankBonusDamage[attachedZone.rank] * levelBonusDamage[level]);
    }
}
