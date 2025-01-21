using System;
using UnityEditor;
using UnityEngine;

public class Tower : MonoBehaviour
{
    //[SerializeField] private GameObject _nozzle;

    [Header("Attributes")]
    [SerializeField] private float _attackRange = 6f;
    [SerializeField] private float _attackInterval = 1f;
    [SerializeField] private float _attackDamage = 1f;
    [SerializeField] private float _bulletSpeed = 6f;

    [Header("Other")]
    [SerializeField] private Bullet _bullet;
    [SerializeField] private LayerMask enemyMask;

    [SerializeField] private Transform target;

    private float cooldown = 0;

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
        else
        {
            RotateTowardsTarget();
            Fire();
        }
    }

    private void FindTarget()
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, _attackRange, (Vector2)transform.position, 0f, enemyMask);

        if (hits.Length > 0 )
        {
            target = hits[0].transform;
            Debug.Log("target set");
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
        return Vector2.Distance(target.position, transform.position) <= _attackRange;
    }

    private void Fire()
    {
        cooldown -= Time.deltaTime;

        if (cooldown <= 0)
        {
            Bullet bullet = Instantiate(_bullet, transform.position, Quaternion.identity);
            bullet.SetBulletStats(_bulletSpeed, _attackDamage, 1, transform.position, target.position);
            bullet.SetTarget(target);
            cooldown = _attackInterval;
        }
    }
}
