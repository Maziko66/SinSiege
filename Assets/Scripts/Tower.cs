using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Tower : MonoBehaviour
{
    //[SerializeField] private GameObject _nozzle;

    [FormerlySerializedAs("_attackRange")]
    [Header("Attributes")]
    [SerializeField] private float attackRange = 6f;
    [FormerlySerializedAs("_attackInterval")] [SerializeField] private float attackInterval = 1f;
    [FormerlySerializedAs("_attackDamage")] [SerializeField] private float attackDamage = 1f;
    [FormerlySerializedAs("_bulletSpeed")] [SerializeField] private float bulletSpeed = 6f;

    [FormerlySerializedAs("_bullet")]
    [Header("Other")]
    [SerializeField] private Bullet bullet;
    [SerializeField] private LayerMask enemyMask;

    [SerializeField] private Transform target;

    private float _cooldown = 0;

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
        _cooldown -= Time.deltaTime;

        if (_cooldown <= 0)
        {
            Bullet bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, target.position, 0);
            bullet.SetTarget(target);
            _cooldown = attackInterval;
        }
    }
}
