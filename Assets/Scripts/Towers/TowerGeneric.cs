using System;
using UnityEngine;

public class TowerGeneric : MonoBehaviour
{
    private GameManager _gameManager;
    private Animator _animator;
    
    protected Cooldown _cooldown;

    [SerializeField] private string animationInit;
    
    [Header("Tower Data")]
    public string towerName;
    public Sprite towerSprite;
    public TowerZone attachedZone;
    
    [Header("Tower Values")]
    public int bulletsFired;
    public int bulletsHit;
    [SerializeField] private int level;
    [SerializeField] private int towerCost = 100;
    [SerializeField] private int[] upgradeCosts = { 100, 200, 300 };
    
    [Header("Default Mechanical Values")]
    [SerializeField] protected float attackRangeDefault = 12f;
    [SerializeField] protected float attackIntervalDefault = 1f;
    [SerializeField] protected float attackDamageDefault = 1f;
    [SerializeField] protected FireMethods.TargetTag targetTagDefault = FireMethods.TargetTag.Enemy;
    
    [Header("Mechanical Values")]
    [SerializeField] protected float attackRange = 12f;
    [SerializeField] protected float attackInterval = 1f;
    [SerializeField] protected float attackDamage = 1f;
    [SerializeField] protected FireMethods.TargetTag targetTag = FireMethods.TargetTag.Enemy;
    
    [Header("Bullet Properties")]
    [SerializeField] protected Bullet bullet;
    [SerializeField] protected float bulletSpeed = 6f;
    [SerializeField] protected int bulletCount = 1;
    [SerializeField] protected float spreadAngle;
    [SerializeField] protected int bulletHealth = 1;
    [SerializeField] protected FireMethods.FireMode fireMode = FireMethods.FireMode.Homing;
    //[SerializeField] private FireMethods.TargetTag targetTag = FireMethods.TargetTag.Enemy;
    
    [Header("Tower Rank Multipliers")]
    [SerializeField] protected float[] rankBonusDamage = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] protected float[] rankBonusInterval = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] protected float[] rankBonusRange = {0.0f, 0.1f, 0.2f, 0.3f};
    
    [Header("Tower Level Modifiers")]
    [SerializeField] protected float[] levelBonusDamage = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] protected float[] levelBonusInterval = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] protected float[] levelBonusRange = {0.0f, 0.1f, 0.2f, 0.3f};
    
    [Header("Other")]
    [SerializeField] protected LayerMask enemyMask;
    [SerializeField] protected Transform target;

    protected string currentTargetTag;

    protected virtual void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _cooldown = GetComponent<Cooldown>();
        _animator = GetComponent<Animator>();
    }
    
    protected virtual void Start()
    {
        target = null;
        currentTargetTag = FireMethods.GetTargetTagString(targetTag);
        
        if (_animator != null)
        {
            _animator.Play(animationInit);
        }
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
    
    protected bool CheckTargetIsInRange()
    {
        return Vector2.Distance(target.position, transform.position) <= attackRange;
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
