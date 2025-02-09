using System;
using UnityEngine;

public class Fists : MonoBehaviour
{
    //private Rigidbody2D _rb;
    //private Animator _animator;
    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;

    [SerializeField] private AudioSource punchSound;

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

    public void Attack(Vector3 targetPosition)
    {
        if (Camera.main != null)
        {
            Vector3 attackDirection = (targetPosition - transform.parent.position).normalized;
            
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            _collider.enabled = true;
            _spriteRenderer.enabled = true;
            punchSound.Play();
            //Debug.Log("Fist attack, enabled fist collider");
            
            Invoke(nameof(DisableComponents), activeTime);
        }
        else
        {
            Debug.Log("camera not found");
        }
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
