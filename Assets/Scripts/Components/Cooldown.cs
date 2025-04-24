using FMOD.Studio;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class Cooldown : MonoBehaviour
{
    private Player _player;
    
    [SerializeField] private UISliderHp slider;
    //[SerializeField] private AudioSource sfxRefresh; 
    public EventReference sfxRefreshEvent;
    [SerializeField] private Text cooldownText;

    [SerializeField] private float refreshDelay;
    private float _cooldown;
    private bool _refreshed;
    private bool _shouldPlaySound;

    private void Awake()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(sfxRefreshEvent.Path))
        {
            _shouldPlaySound = true;
        }
    }

    private void Update()
    {
        if(_player.isPaused) {return;}
        if (_cooldown >= 0f)
        {
            _cooldown -= Time.deltaTime;

            slider?.SliderValueSet(slider.GetMaxValue() - _cooldown);

        }
        if (!_refreshed && _cooldown <= refreshDelay)
        {
            // if (sfxRefresh)
            // {
            //     sfxRefresh.Play();
            // }
            if (_shouldPlaySound)
            {
                EventInstance refreshEvent = RuntimeManager.CreateInstance(sfxRefreshEvent);
                refreshEvent.start();
                refreshEvent.release();
            }
            
            
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
        slider?.SliderMinMaxValueSet(cooldown);

    }

    public float GetCooldown()
    {
        return _cooldown;
    }

    public void SetRefreshed(bool state)
    {
        _refreshed = state;
    }

    public void SetSliderUIName(string str)
    {
        slider.SliderTextSet(str);
    }
}
