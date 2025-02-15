using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Tower : MonoBehaviour
{
    //[SerializeField] private GameObject _nozzle;
    private Cooldown _cooldown;
    
    [Header("Attributes")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    
    [Header("Bullet Properties")]
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private int bulletCount = 1;
    [SerializeField] private float spreadAngle = 0f;
    [SerializeField] private int firingMode = 0;
    
    [Header("Other")]
    [SerializeField] private Bullet bullet;
    [SerializeField] private LayerMask enemyMask;

    [SerializeField] private Transform target;

    //private float _cooldown = 0;

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        target = null;
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
        FireMethods.BulletFire(firingMode, bullet, transform, bulletSpeed, attackDamage, target.position, target);
        _cooldown.SetCooldown(attackInterval);
        _cooldown.SetRefreshed(false);
    }
}
