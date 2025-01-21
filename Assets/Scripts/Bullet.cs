using UnityEngine;

public class Bullet : MonoBehaviour
{
    Rigidbody2D rb;

    private float _speed = 5f;
    private float _damage = 1f;
    private int _health = 1;

    private Vector3 _startPosition;
    private Vector3 _targetVector;

    private Transform _target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        
    }
    private void Update()
    {
        if(_target == null)
        {
            Destroy(gameObject); return;
        }    
    }

    private void FixedUpdate()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, _target.position, _speed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    public void SetBulletStats(float speed, float damage, int health, Vector3 startPosition, Vector3 targetVector)
    {
        SetBulletSpeed(speed);
        SetBulletDamage(damage);
        SetBulletHealth(health);
        SetStartPosition(startPosition);
        SetTargetVector(targetVector);
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

    public void CheckHealth()
    {
        if (_health <= 0)
        {
            Destroy(gameObject); return;
        }
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
            Debug.Log("hit enemy");
        }
    }
}
