using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public enum TargetTag
    {
        Enemy,
        EnemyGround,
        EnemyAir
    }
    
    //[SerializeField] private GameObject _nozzle;
    private Cooldown _cooldown;
    
    [Header("Attributes")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    
    [Header("Bullet Properties")]
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private int bulletCount = 1;
    [SerializeField] private float spreadAngle;
    [SerializeField] private int bulletHealth = 1;
    [SerializeField] private FireMethods.FireMode fireMode = FireMethods.FireMode.Homing;
    [SerializeField] private FireMethods.TargetTag targetTag = FireMethods.TargetTag.Enemy;
    
    [Header("Other")]
    [SerializeField] private Bullet bullet;
    [SerializeField] private LayerMask enemyMask;

    [SerializeField] private Transform target;

    //private float _cooldown = 0;

    private string _currentTargetTag;
    private int _currentFireMode;

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        target = null;
        _currentTargetTag = FireMethods.GetTargetTagString(targetTag);
        _currentFireMode = FireMethods.GetFireMode(fireMode);
    }

    private void Update()
    {
        if(target == null)
        {
            FindTarget();
            return;
        }        

        if(!CheckTargetIsInRange())
        {
            target = null;
        }
        else if(_cooldown.GetCooldown() <= 0)
        {
            //RotateTowardsTarget();
            Fire();
        }
    }

    private void FindTarget()
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, attackRange, (Vector2)transform.position, 0f, enemyMask);
        
        if (_currentTargetTag != "Enemy")
        {
            List<RaycastHit2D> applicableHits = new List<RaycastHit2D>();
            
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag(_currentTargetTag))
                {
                    applicableHits.Add(hit);
                }
            }
            
            hits = applicableHits.ToArray();
        }
        
        
        if (hits.Length > 0 )
        {
            target = hits[0].transform;
            //Debug.Log("target set");
        }
    }

    private void RotateTowardsTarget()
    {
        float angle = Mathf.Atan2(target.position.y - transform.position.y, target.position.x - transform.position.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
        transform.rotation = targetRotation;
    }

    private bool CheckTargetIsInRange()
    {
        return Vector2.Distance(target.position, transform.position) <= attackRange;
    }

    private void Fire()
    {
        string tagString = FireMethods.GetTargetTagString(targetTag);
        //FireMethods.BulletFire(firingMode, bullet, transform, bulletSpeed, attackDamage, target.position, target);
        FireMethods.BulletFire(_currentFireMode, bullet, transform, bulletSpeed, attackDamage, target.position, bulletHealth, tagString, target);
        _cooldown.SetCooldown(attackInterval);
        _cooldown.SetRefreshed(false);
    }
}
