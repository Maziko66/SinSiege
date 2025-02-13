using System;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    private Cooldown _cooldown;
    
    [SerializeField] private string weaponName = "Shotgun";
    
    [Header("Audio")]
    [SerializeField] private AudioSource sfxFire;
    [SerializeField] private AudioSource sfxReload;
    [SerializeField] private float sfxReloadDelay = 0.3f;
    [SerializeField] private Bullet bullet;
    
    [Header("Stats")]
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private float spreadAngle = 10f;
    [SerializeField] private float bulletMass = 1f;
    [SerializeField] private int bulletCount = 3;
    
    // private bool _reloaded;

    private void Awake()
    {
        _cooldown = GetComponent<Cooldown>();
    }

    private void Start()
    {
        _cooldown.SetSliderUIName(weaponName);
        //_cooldown.SetRefreshDelay(sfxReloadDelay);
    }

    private void Update()
    {
        // if (!_reloaded && _cooldown.GetCooldown() <= sfxReloadDelay)
        // {
        //     sfxReload.Play();
        //     _reloaded = true;
        // }
    }

    public void Fire(Vector3 targetVector3)
    {
        //if (_cooldown <= 0)
        if (_cooldown.GetCooldown() < 0)
        {
            // Bullet bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            // Vector2 direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
            // bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
            //
            // bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            // direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
            // direction = RotateVector2(direction, spreadAngle);
            // bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
            //
            // bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            // direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
            // direction = RotateVector2(direction, -spreadAngle);
            // bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
            
            float startAngle = (bulletCount * spreadAngle) / 2 - 5;
            for (int i = 0; i < bulletCount; i++)
            {
                Bullet bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
                Vector2 direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
                direction = RotateVector2(direction, startAngle);
                bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
                startAngle -= spreadAngle;
            }
            
            sfxFire.Play();
            
            _cooldown.SetCooldown(attackInterval);
            _cooldown.SetRefreshed(false);
            //_reloaded = false;
        }
        else
        {
            Debug.Log("Shotgun is on cooldown: " + _cooldown.GetCooldown());
        }
    }
    
    private Vector2 RotateVector2(Vector2 vector, float angle)
    {
        float radianAngle = Mathf.Deg2Rad * angle;
        float cosine = Mathf.Cos(radianAngle);
        float sine = Mathf.Sin(radianAngle);
        
        return new Vector2(
            cosine * vector.x - sine * vector.y,
            sine * vector.x + cosine * vector.y
        );
    }
}
