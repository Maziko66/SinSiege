using System;
using UnityEngine;
using FMODUnity;

public class Fists : MonoBehaviour
{
    private Cooldown _cooldown;
    
    //private Rigidbody2D _rb;
    //private Animator _animator;
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
   
    [SerializeField] private string weaponName = "Fists";
    
    [Header("Audio")]
    //[SerializeField] private AudioSource punchSound;
    public EventReference FireEvent;
    [SerializeField] private float sfxReloadDelay = 0.3f;
    
    [Header("Stats")]
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private FireMethods.TargetTag targetTag = FireMethods.TargetTag.Enemy;

    //private float _cooldown;
    private float _activeTimeCooldown;
    private string _currentTargetTag;
    
    [Header("Upgrades")]
    public UpgradeData upgradeAttackInterval;
    public UpgradeData upgradeAttackDamage;
    
    [Header("Calculated")]
    [SerializeField] private float calculatedAttackInterval;
    [SerializeField] private float calculatedAttackDamage;
    
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
        //_rb = GetComponent<Rigidbody2D>();
        //_animator = GetComponent<Animator>();
        _cooldown = GetComponent<Cooldown>();
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        //_cooldown.SetRefreshDelay(sfxReloadDelay);
        UpdateTargetTag();
        //Debug.Log(_currentTargetTag);
        _collider.enabled = false;
        _spriteRenderer.enabled = false;
        _cooldown.SetSliderUIName(weaponName);
    }

    public void Attack(Vector3 targetPosition)
    {
        if (Camera.main != null && _cooldown.GetCooldown() < 0)
        {
            Vector3 attackDirection = (targetPosition - transform.parent.position).normalized;
            
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            _collider.enabled = true;
            _spriteRenderer.enabled = true;
            //punchSound.Play();
            //Debug.Log("Fist attack, enabled fist collider");
            FMOD.Studio.EventInstance fire = FMODUnity.RuntimeManager.CreateInstance(FireEvent);
            //fire.setParameterByID(fullHealthParameterId, restoreAll ? 1.0f : 0.0f);
            //fire.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
            fire.start();
            fire.release();
            
            Invoke(nameof(DisableComponents), activeTime);
            _cooldown.SetCooldown(calculatedAttackInterval);
            _cooldown.SetRefreshed(false);
        }
        else
        {
            if (Camera.main == null)
            {
                Debug.Log("Camera.main is null");
            }
            else
            {
                Debug.Log("Fists on cooldown.");
            }
        }
    }
    
    private void DisableComponents()
    {
        _collider.enabled = false;
        _spriteRenderer.enabled = false;
        //Debug.Log("Disabled fist collider");
    }
    
    [ContextMenu("Update Target Tag")]
    private void UpdateTargetTag()
    {
        _currentTargetTag = FireMethods.GetTargetTagString(targetTag);
    }
    
    private void CalculateUpgrades()
    {
        calculatedAttackInterval = attackInterval + (upgradeAttackInterval?.Value ?? 0);
        calculatedAttackDamage   = damage         + (upgradeAttackDamage?.Value ?? 0);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_currentTargetTag == "Enemy" || other.gameObject.CompareTag(_currentTargetTag) || other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            enemy.ReduceHealth(calculatedAttackDamage);
            enemy.CheckHealth();
            //Debug.Log("fists hit enemy");
        }
    }
}