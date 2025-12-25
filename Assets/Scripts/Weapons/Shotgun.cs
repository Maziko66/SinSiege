using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    private Cooldown _cooldown;
    
    [SerializeField] private string weaponName = "Shotgun";
    
    [Header("Audio")]
    //[SerializeField] private AudioSource sfxFire;
    //[SerializeField] private AudioSource sfxReload;
    public EventReference FireEvent;
    [SerializeField] private float sfxReloadDelay = 0.3f;
    [SerializeField] private Bullet bullet;
    
    [Header("Stats")]
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private float spreadAngle = 10f;
    [SerializeField] private float bulletMass = 1f;
    [SerializeField] private int bulletCount = 3;
    [SerializeField] private int bulletHealth = 1;
    [SerializeField] private FireMethods.TargetTag targetTag = FireMethods.TargetTag.Enemy;
    [SerializeField] private FireMethods.FireMode fireMode = FireMethods.FireMode.Homing;
    
    [Header("Other")]
    [SerializeField] private bool continuousFire;
    
    [Header("Upgrades")]
    public UpgradeData upgradeAttackInterval;
    public UpgradeData upgradeAttackDamage;
    public UpgradeData upgradeBulletSpeed;
    public UpgradeData upgradeBulletCount;
    public UpgradeData upgradeBulletHealth;
    
    
    [Header("Calculated")]
    [SerializeField] private float calculatedAttackInterval;
    [SerializeField] private float calculatedAttackDamage;
    [SerializeField] private float calculatedBulletSpeed;
    [SerializeField] private float calculatedBulletCount;
    [SerializeField] private float calculatedBulletHealth;
    
    // private bool _reloaded;
    
    private string _currentTargetTag;
    private int _currentFireMode;
    
    private void OnEnable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnRecalculateUpgrades += CalculateUpgrades;
        }
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnRecalculateUpgrades -= CalculateUpgrades;
        }
    }
    
    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Update()
    {
        if (continuousFire)
        {
            Fire(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    private void Start()
    {
        _cooldown.SetSliderUIName(weaponName);
        //_cooldown.SetRefreshDelay(sfxReloadDelay);
        UpdateTargetTag();
        _currentFireMode = FireMethods.GetFireMode(fireMode);
    }

    [ContextMenu("Update Target Tag")]
    private void UpdateTargetTag()
    {
        _currentTargetTag = FireMethods.GetTargetTagString(targetTag);
    }
    
    public void Fire(Vector3 targetVector3)
    {
        if (_cooldown.GetCooldown() < 0)
        {
            //FireMethods.BulletFire(fireMode, bullet, transform, bulletSpeed, attackDamage, targetVector3, null, bulletCount, spreadAngle);
            FireMethods.BulletFire(_currentFireMode, bullet, transform,bulletSpeed,attackDamage, targetVector3, bulletHealth, _currentTargetTag, null, null, bulletCount, spreadAngle);
            
            //RuntimeManager.PlayOneShot("event:/SFX/Player/Shotgunman/ShotgunFire");
            
            FMOD.Studio.EventInstance fire = FMODUnity.RuntimeManager.CreateInstance(FireEvent);
            //fire.setParameterByID(fullHealthParameterId, restoreAll ? 1.0f : 0.0f);
            //fire.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
            fire.start();
            fire.release();
            
            _cooldown.SetCooldown(attackInterval);
            _cooldown.SetRefreshed(false);
        }
        else
        {
            //Debug.Log("Shotgun is on cooldown: " + _cooldown.GetCooldown());
        }
    }

    private void CalculateUpgrades()
    {
        calculatedAttackInterval = attackInterval + (upgradeAttackInterval?.Value ?? 0);
        calculatedAttackDamage   = attackDamage   + (upgradeAttackDamage?.Value ?? 0);
        calculatedBulletSpeed    = bulletSpeed    + (upgradeBulletSpeed?.Value ?? 0);
        calculatedBulletCount    = bulletCount    + (upgradeBulletCount?.Value ?? 0);
        calculatedBulletHealth   = bulletHealth   + (upgradeBulletHealth?.Value ?? 0);
    }
}
