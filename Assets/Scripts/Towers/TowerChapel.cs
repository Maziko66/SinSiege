using UnityEngine;

public class TowerChapel : MonoBehaviour
{
    [SerializeField] private GameObject aoe;
    private Cooldown _cooldown;
    
    [Header("Attributes")]
    [SerializeField] private float attackRadius = 6f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float expandSpeed = 6f;
    
    [Header("Other")]
    [SerializeField] private LayerMask enemyMask;

    [SerializeField] private Transform target;
    
    private RaycastHit2D[] _hitsCache;

    private bool _isExpanding;

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        _hitsCache = new RaycastHit2D[1];
        target = null;
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
        if (aoe.transform.localScale.x < attackRadius)
        {
            if (!_isExpanding)
            {
                _isExpanding = true;
            }
            aoe.transform.localScale += new Vector3(expandSpeed, expandSpeed, 0) * Time.deltaTime;
        }
        else if (aoe.transform.localScale.x >= attackRadius)
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
        int hitCount = Physics2D.CircleCastNonAlloc(transform.position, attackRadius, Vector2.zero, _hitsCache, 0f, enemyMask);
        if (hitCount > 0)
        {
            target = _hitsCache[0].transform;
            //Debug.Log("target set");
        }
        else
        {
            target = null;
        }
    }

    private bool CheckTargetIsInRange()
    {
        return Vector2.Distance(target.position, transform.position) <= attackRadius;
    }

    public float GetDamage()
    {
        return attackDamage;
    }
    
}
