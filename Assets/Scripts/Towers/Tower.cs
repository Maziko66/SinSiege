using System;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Util")]
    public int segments = 32;
    
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
    [SerializeField] private Transform fireTransform;
    [SerializeField] private Transform target;

    //private float _cooldown = 0;

    private string _currentTargetTag;
    private int _currentFireMode;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        // Calculate the angle between each segment
        float angleStep = 360f / segments;
        
        DrawGizmoCircle(transform.position, attackRange, segments);
    }

    private void DrawGizmoCircle(Vector3 center, float radius, int segments)
    {
        // Calculate the angle between each segment
        float angleStep = 360f / segments;

        // Draw the circle using line segments
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;

            Vector3 point1 = center + Quaternion.Euler(0, 0, angle1) * Vector3.right * radius;
            Vector3 point2 = center + Quaternion.Euler(0, 0, angle2) * Vector3.right * radius;

            Gizmos.DrawLine(point1, point2);
        }
    }
    
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
                if (hit.collider.CompareTag(_currentTargetTag) || hit.collider.CompareTag("Enemy"))
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
        FireMethods.BulletFire(_currentFireMode, bullet, fireTransform, bulletSpeed, attackDamage, target.position, bulletHealth, tagString, target);
        _cooldown.SetCooldown(attackInterval);
        _cooldown.SetRefreshed(false);
    }
    
    [ContextMenu("Update Target Tag")]
    private void UpdateTargetTag()
    {
        _currentTargetTag = FireMethods.GetTargetTagString(targetTag);
    }
}
