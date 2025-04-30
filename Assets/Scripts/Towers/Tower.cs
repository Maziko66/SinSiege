using System.Collections.Generic;
using UnityEngine;

public class Tower : TowerGeneric
{
    [Header("Util")]
    [SerializeField] private int _segments = 32;
    
    //[SerializeField] private GameObject _nozzle;
    
    
    [Header("Other")]
    [SerializeField] private Transform fireTransform;

    //private float _cooldown = 0;
    
    private int _currentFireMode;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        // Calculate the angle between each segment
        float angleStep = 360f / _segments;
        
        DrawGizmoCircle(transform.position, GetAttackRange(), _segments);
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
    
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
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
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, GetAttackRange(), (Vector2)transform.position, 0f, enemyMask);
        
        if (currentTargetTag != "Enemy")
        {
            List<RaycastHit2D> applicableHits = new List<RaycastHit2D>();
            
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag(currentTargetTag) || hit.collider.CompareTag("Enemy"))
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
    

    private void Fire()
    {
        string tagString = FireMethods.GetTargetTagString(targetTag);
        //FireMethods.BulletFire(firingMode, bullet, transform, bulletSpeed, attackDamage, target.position, target);
        FireMethods.BulletFire(_currentFireMode, bullet, fireTransform, bulletSpeed, GetAttackDamage(), target.position, bulletHealth, tagString, this, target);
        bulletsFired++;
        _cooldown.SetCooldown(GetAttackInterval());
        _cooldown.SetRefreshed(false);
    }
    
    [ContextMenu("Update Target Tag")]
    private void UpdateTargetTag()
    {
        currentTargetTag = FireMethods.GetTargetTagString(targetTag);
    }
}
