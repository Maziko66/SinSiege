using System;
using UnityEngine;
using UnityEngine.UI;

public class Cooldown : MonoBehaviour
{
    [SerializeField] private AudioSource sfxRefresh; 
    [SerializeField] private Slider slider;
    [SerializeField] private Text cooldownText;

    [SerializeField] private float refreshDelay;
    private float _cooldown;
    private bool _refreshed;

    private void Update()
    {
        if (_cooldown >= 0f)
        {
            _cooldown -= Time.deltaTime;
        }
        if (!_refreshed && _cooldown <= refreshDelay)
        {
            sfxRefresh.Play();
            _refreshed = true;
        }
    }

    public void SetRefreshDelay(float delay)
    {
        refreshDelay = delay;
    }
    
    public void SetCooldown(float cooldown)
    {
        _cooldown = cooldown;
    }

    public float GetCooldown()
    {
        return _cooldown;
    }

    public void SetRefreshed(bool state)
    {
        _refreshed = state;
    }
}
