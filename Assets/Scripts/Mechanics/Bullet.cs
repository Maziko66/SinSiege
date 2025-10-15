using System;
using UnityEngine;
using FMODUnity;

public class Bullet : MonoBehaviour
{
    private Rigidbody2D _rb;

    private TowerGeneric _towerGeneric;
    
    private float _speed = 5f;
    private float _damage = 1f;
    private int _health = 1;
    private float _mass = 1f;

    private Vector3 _startPosition;
    private Vector3 _targetVector;

    private Transform _target;

    private int _bulletMode; //0: Turret Homing, 1: Shoutgun, Direct, 2: Enemy, Direct

    private float _lifeSpan = 4;
    
    private string _targetTag;

    private bool _isSpinning;

    private float _speedSpin = -450f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        //Debug.Log("Bullet Start");
        _rb.mass = _mass;
        if (_bulletMode == 1 || _bulletMode == 2)
        {
            _rb.AddForce(_targetVector * _speed /** _mass*/, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (_bulletMode == 0)
        {
            if(!_target)
            {
                Destroy(gameObject); return;
            }
            Vector2 newPosition = Vector2.MoveTowards(_rb.position, _target.position, _speed * Time.fixedDeltaTime);
            _rb.MovePosition(newPosition);
            
            if (_isSpinning)
            {
                transform.Rotate(0f, 0f, _speedSpin * Time.fixedDeltaTime);
            }
        }
        
        _lifeSpan -= Time.fixedDeltaTime;

        if (_lifeSpan <= 0)
        {
            Destroy(gameObject);
        }
        
        // else if (_firedFrom == 1)
        // {
        //     //Vector2 newPosition = Vector2.MoveTowards(rb.position, _targetVector, _speed * Time.fixedDeltaTime);
        //     //rb.MovePosition(newPosition);
        //     
        // }
        
    }
    
    /// <summary>
    /// Sets instantiated bullet attributes.
    /// </summary>
    /// <param name="speed">Bullet speed.</param>
    /// <param name="damage">Bullet damage.</param>
    /// <param name="health">Bullet health.</param>
    /// <param name="startPosition">Bullet start positıon.</param>
    /// <param name="targetVector">Target vector.</param>
    /// <param name="mode">Bullet mode. (0 = homing, 1 = direct)</param>
    /// <param name="tagToHit">Tag of gameobject to destroy.</param>
    /// <param name="mass">Bullet mass.</param>
    /// <param name="isSpinning">Checks bullet spin.</param>
    public void SetBulletStats(float speed, float damage, int health, Vector3 startPosition, Vector3 targetVector, int mode, string tagToHit, float mass = 1f, bool isSpinning = false)
    {
        SetBulletSpeed(speed);
        SetBulletDamage(damage);
        SetBulletHealth(health);
        SetStartPosition(startPosition);
        SetTargetVector(targetVector);
        SetFiredFrom(mode);
        SetTargetTag(tagToHit);
        SetBulletMass(mass);
        SetBulletSpinning(isSpinning);
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
        _bulletMode = firedFrom;
    }

    private void CheckHealth()
    {
        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void SetBulletMass(float mass = 1f)
    {
        _mass = mass;
    }

    public void SetBulletSpinning(bool isSpinning)
    {
        _isSpinning = isSpinning;
    }

    public void SetTargetTag(string tag)
    {
        _targetTag = tag;
    }

    public void SetTowerGeneric(TowerGeneric towerGeneric)
    {
        _towerGeneric = towerGeneric;
    }

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if (collision.gameObject.CompareTag(_targetTag) || (_targetTag == "Enemy" && (collision.gameObject.CompareTag("EnemyGround") || collision.gameObject.CompareTag("EnemyAir"))))
    //     {
    //         Enemy enemy = collision.gameObject.GetComponent<Enemy>();
    //         enemy.ReduceHealth(_damage);
    //         enemy.CheckHealth();
    //         _health--;
    //         CheckHealth();
    //         //Debug.Log("hit enemy");
    //     }
    // }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_bulletMode == 2)
        {

        }
        else if (_targetTag == "Enemy" || other.gameObject.CompareTag(_targetTag) || other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            if (enemy)
            {
                if (SoundManager.Instance != null && gameObject.scene.isLoaded)
                {
                    //SoundManager.Instance.PlaySound(SoundManager.Instance.sfxCoinPickup, transform.position);
                    SoundManager.Instance.PlaySound(SoundManager.Instance.sfxEnemyDamage);
                }
                enemy.ReduceHealth(_damage);
                enemy.CheckHealth();
                _health--;
                if (_towerGeneric)
                {
                    _towerGeneric.bulletsHit++;
                    _towerGeneric.IncreaseTowerZoneVet(enemy.GetExp());
                }
                CheckHealth();
                //Debug.Log("hit enemy");
            }
            else
            {
                Boss boss = other.gameObject.GetComponent<Boss>();
                {
                    if (boss)
                    {
                        if (SoundManager.Instance != null && gameObject.scene.isLoaded)
                        {
                            //SoundManager.Instance.PlaySound(SoundManager.Instance.sfxCoinPickup, transform.position);
                            SoundManager.Instance.PlaySound(SoundManager.Instance.sfxEnemyDamage);
                        }
                        boss.ReduceHealth(_damage);
                        //boss.CheckHealth();
                        if (_towerGeneric)
                        {
                            _towerGeneric.bulletsHit++;
                            _towerGeneric.IncreaseTowerZoneVet(enemy.GetExp());
                        }

                        _health--;
                        CheckHealth();
                    }
                }
            }
            
        }
        else if (other.gameObject.CompareTag("Bound"))
        {
            _health = 0;
            CheckHealth();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Player player = other.gameObject.GetComponent<Player>();
            Debug.Log("Bullet hit player.");
            
            _health--;
            CheckHealth();
        }
    }
}
