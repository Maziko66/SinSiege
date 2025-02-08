using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody2D _rb;

    private float _speed = 5f;
    private float _damage = 1f;
    private int _health = 1;
    private float _mass = 1f;

    private Vector3 _startPosition;
    private Vector3 _targetVector;

    private Transform _target;

    private int _firedFrom; //0: Turret, 1: Shoutgun
    

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _rb.mass = _mass;
        if (_firedFrom == 1)
        {
            //Vector2 direction = ((Vector2)_targetVector - (Vector2)transform.position).normalized;
            _rb.AddForce(_targetVector * _speed /** _mass*/, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (_firedFrom == 0)
        {
            if(!_target)
            {
                Destroy(gameObject); return;
            }
            Vector2 newPosition = Vector2.MoveTowards(_rb.position, _target.position, _speed * Time.fixedDeltaTime);
            _rb.MovePosition(newPosition);
            
        }
        else if (_firedFrom == 1)
        {
            //Vector2 newPosition = Vector2.MoveTowards(rb.position, _targetVector, _speed * Time.fixedDeltaTime);
            //rb.MovePosition(newPosition);
            
        }
        
    }

    public void SetBulletStats(float speed, float damage, int health, Vector3 startPosition, Vector3 targetVector, int firedFrom, float mass = 1f)
    {
        SetBulletSpeed(speed);
        SetBulletDamage(damage);
        SetBulletHealth(health);
        SetStartPosition(startPosition);
        SetTargetVector(targetVector);
        SetFiredFrom(firedFrom);
        SetBulletMass(mass);
    }

    public void SetBulletSpeed(float speed)
    {
        _speed = speed;
    }

    public void SetBulletHealth(int health)
    {
        _health = health;
    }

    public void SetBulletDamage(float damage)
    {
        _damage = damage;
    }

    public void SetStartPosition(Vector3 startPosition)
    {
        _startPosition = startPosition;
    }

    public void SetTargetVector(Vector3 targetVector)
    {
        _targetVector = targetVector;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void SetFiredFrom(int firedFrom)
    {
        _firedFrom = firedFrom;
    }

    private void CheckHealth()
    {
        if (_health <= 0)
        {
            Destroy(gameObject); return;
        }
    }

    public void SetBulletMass(float mass = 1f)
    {
        _mass = mass;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.ReduceHealth(_damage);
            enemy.CheckHealth();
            _health--;
            CheckHealth();
            //Debug.Log("hit enemy");
        }
    }
}
