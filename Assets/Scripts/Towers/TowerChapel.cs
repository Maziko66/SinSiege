using System.Collections.Generic;
using UnityEngine;

public class TowerChapel : TowerGeneric
{
    [SerializeField] private GameObject aoe;
    private Cooldown _cooldown;
    
    [Header("Chapel Mechanical Values")]
    [SerializeField] private float expandSpeed = 6f;
    
    
    private bool _isExpanding;
    

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        target = null;
        currentTargetTag = FireMethods.GetTargetTagString(targetTag);
    }
    
    private void Update()
    {
        if(target == null && !_isExpanding)
        {
            
            FindTarget();
            return;
        }        

        if(!_isExpanding && !CheckTargetIsInRange())
        {
            target = null;
            aoe.transform.localScale = Vector3.one;
        }
        else
        {
            if (_cooldown.GetCooldown() <= 0)
            {
                Expand();
            }
        }
    }

    private void Expand()
    {
        if (aoe.transform.localScale.x < attackRange)
        {
            if (!_isExpanding)
            {
                _isExpanding = true;
            }
            aoe.transform.localScale += new Vector3(expandSpeed, expandSpeed, 0) * Time.deltaTime;
        }
        else if (aoe.transform.localScale.x >= attackRange)
        {
            aoe.transform.localScale = Vector3.one;
            _isExpanding = false;
            _cooldown.SetCooldown(attackInterval);
            _cooldown.SetRefreshed(false);
        }
    }

    private void FindTarget()
    {
        //RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, attackRange, (Vector2)transform.position, 0f, enemyMask);
        //int hitCount = Physics2D.CircleCastNonAlloc(transform.position, attackRadius, Vector2.zero, _hitsCache, 0f, enemyMask);
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, attackRange, (Vector2)Vector2.zero, 0f, enemyMask);
        
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
        else
        {
            target = null;
        }
    }

    private bool CheckTargetIsInRange()
    {
        return Vector2.Distance(target.position, transform.position) <= attackRange;
    }

    public float GetDamage()
    {
        return attackDamage;
    }
    
}
