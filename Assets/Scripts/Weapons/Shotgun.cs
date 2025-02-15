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
    [SerializeField] private int fireMode = 1;
    
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
    
    public void Fire(Vector3 targetVector3)
    {
        if (_cooldown.GetCooldown() < 0)
        {
            FireMethods.BulletFire(fireMode, bullet, transform, bulletSpeed, attackDamage, targetVector3, null, bulletCount, spreadAngle);
            
            sfxFire.Play();
            
            _cooldown.SetCooldown(attackInterval);
            _cooldown.SetRefreshed(false);
        }
        else
        {
            Debug.Log("Shotgun is on cooldown: " + _cooldown.GetCooldown());
        }
    }
}
