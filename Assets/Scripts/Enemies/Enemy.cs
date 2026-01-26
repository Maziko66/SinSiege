using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private Material _matFlashDamaged;
    private Material _matOriginal;
    
    Rigidbody2D rb;
    private Canvas _canvas;
    private GameManager _gameManager;
    private WaveManager _waveManager;
    private SpriteRenderer _spriteRenderer;
    private Player _player;

    [SerializeField] private Slider prefabSliderHealth;
    [SerializeField] private Vector3 sliderOffset;
    
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _health = 5f;
    [SerializeField] private int damage;
    [SerializeField] private float exp = 1;
    [SerializeField] public int coinValue;

    // --- NEW PATH VARIABLES ---
    private List<Vector2> _pathPoints;
    private Vector2 _currentTarget;
    private int _pathIndex = 0;

    private Slider sliderHealth;
    private bool _isDamaged;

    public bool followPlayer;
    public bool isHorde;
    
    // Properties for WaveManager to read/modify
    public float BaseHealth => _health;
    public float BaseSpeed => _moveSpeed;
    public int BaseDamage => damage;
    public int BaseCoin => coinValue;
    public float BaseExp => exp;
    
    private void Awake()
    {
        _matFlashDamaged = Resources.Load<Material>("Materials/FlashDamaged");
        
        _canvas = FindFirstObjectByType<Canvas>();
        _gameManager = FindFirstObjectByType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        _matOriginal = _spriteRenderer.material;
    }

    private void Start()
    {
        _player = _gameManager.GetPlayer();
        
        // If path is not set yet (or spawned manually), default to current position
        if (_pathPoints == null || _pathPoints.Count == 0)
        {
            _currentTarget = transform.position;
        }
    }

    // --- NEW SET PATH METHOD ---
    public void SetPath(List<Vector2> newPath)
    {
        if (newPath != null && newPath.Count > 0)
        {
            _pathPoints = new List<Vector2>(newPath);
            // Index 0 is spawn point (where we are), so target index 1 immediately
            _pathIndex = 1; 
            
            if (_pathPoints.Count > 1)
                _currentTarget = _pathPoints[_pathIndex];
            else
                _currentTarget = _pathPoints[0];
        }
    }

    private void Update()
    {
        // Flip Sprite based on target x direction
        if(rb.position.x - _currentTarget.x > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }

        if (followPlayer)
        {
            if(_player != null) 
            {
                _currentTarget = _player.transform.position;
            }
        }
        else if (_pathPoints != null && _pathPoints.Count > 0)
        {
            // Check distance to current waypoint
            if (Vector2.Distance(rb.position, _currentTarget) < 0.1f)
            {
                _pathIndex++;
                if (_pathIndex >= _pathPoints.Count)
                {
                    // Reached the end (Base)
                    // TODO: Deal damage to base?
                    CheckHealth(); // Destroy for now
                }
                else
                {
                    _currentTarget = _pathPoints[_pathIndex];
                }
            }
        }
    }

    private void LateUpdate()
    {
        _spriteRenderer.sortingOrder = Mathf.RoundToInt(-_spriteRenderer.gameObject.transform.position.y * 100);
        
        if (_isDamaged && sliderHealth)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);
            sliderHealth.transform.position = screenPosition;
        }
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, _currentTarget, _moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    public void ReduceHealth(float damage)
    {
        if (damage > 0 && !_isDamaged)
        {
            _isDamaged = true;
            sliderHealth = Instantiate(prefabSliderHealth.gameObject, _canvas.transform).GetComponent<Slider>();
            sliderHealth.maxValue = _health;
        }
        
        StartCoroutine(FlashWhite());
        _health -= damage;
        if(sliderHealth != null) sliderHealth.value = _health;
        
        CheckHealth(); // Added check here to ensure immediate death on hit
    }

    public void CheckHealth()
    {
        if (_health <= 0)
        {
            _gameManager.SpawnCoins(coinValue, transform.position);
            if(sliderHealth != null) Destroy(sliderHealth.gameObject);
            Destroy(gameObject);
            SoundManager.Instance.PlaySound(SoundManager.Instance.sfxEnemyDeath);
        }
    }
    
    private void OnDestroy()
    {
        if (_waveManager != null)
        {
            if (isHorde)
                _waveManager.OnHordeDestroyed(this);
            else
                _waveManager.OnEnemyDestroyed(this);
        }
    }

    public int GetDamage() { return damage; }
    public float GetExp() { return exp; }

    public void SetWaveManager(WaveManager waveManager)
    {
        _waveManager = waveManager;
    }
    
    IEnumerator FlashWhite()
    {
        _spriteRenderer.material = _matFlashDamaged;
        yield return new WaitForSeconds(0.05f);
        _spriteRenderer.material = _matOriginal;
    }
    
    public void InitializeStats(float overrideHealth, float overrideSpeed, int overrideDamage, int overrideCoin, float overrideExp)
    {
        _health = overrideHealth;
        _moveSpeed = overrideSpeed;
        damage = overrideDamage;
        coinValue = overrideCoin;
        exp = overrideExp;

        if (sliderHealth != null)
        {
            sliderHealth.maxValue = _health;
            sliderHealth.value = _health;
        }
    }
}