using System;
using UnityEngine;

public class Fists : MonoBehaviour
{
    //private Rigidbody2D _rb;
    //private Animator _animator;
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;

    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float damage = 5f;

    private float _cooldown;
    private float _activeTimeCooldown;
    
    
    private void Awake()
    {
        //_rb = GetComponent<Rigidbody2D>();
        //_animator = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _collider.enabled = false;
        _spriteRenderer.enabled = false;
    }

    public void Attack()
    {
        _collider.enabled = true;
        _spriteRenderer.enabled = true;
        Debug.Log("Fist attack, enabled fist collider");
        Invoke(nameof(DisableComponents), activeTime);
    }

    private void DisableComponents()
    {
        _collider.enabled = false;
        _spriteRenderer.enabled = false;
        Debug.Log("Disabled fist collider");
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.ReduceHealth(damage);
            enemy.CheckHealth();
            Debug.Log("fists hit enemy");
        }
    }
}
