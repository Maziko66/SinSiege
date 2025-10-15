using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [SerializeField] private List<Transform> attackPoints;
    
    private Material _matFlashDamaged;
    [SerializeField] private Material _matOriginal;
    
    private Rigidbody2D rb;
    
    private Canvas _canvas;
    private GameManager _gameManager;
    private WaveManager _waveManager;
    private Player _player;
    
    
    [SerializeField] private float moveSpeed;
    [SerializeField] private float health = 15f;
    [SerializeField] private int damage;
    [SerializeField] private float exp = 1;


    [SerializeField] private List<GameObject> limbs;
    
    private List<SpriteRenderer> _childSpriteRenderers = new List<SpriteRenderer>();
    
    public bool followPlayer;
    
    private Vector2 _target;
    
    private void Awake()
    {
        foreach (GameObject limb in limbs)
        {
            _childSpriteRenderers.Add(limb.GetComponent<SpriteRenderer>());
        }
        
        _matFlashDamaged = Resources.Load<Material>("Materials/FlashDamaged");
        
        _canvas = FindFirstObjectByType<Canvas>();
        _gameManager = FindFirstObjectByType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        
        
    }
    
    private void Start()
    {
        _player = _gameManager.GetPlayer();
    }
    
    private void Update()
    {
        if (followPlayer)
        {
            if(_player == null) { return; }
            _target = _player.transform.position;
        }
        
    }
    
    private void LateUpdate()
    {
         // _spriteRenderer.sortingOrder = Mathf.RoundToInt(-_spriteRenderer.gameObject.transform.position.y * 100); // FIX HERE
         
         // if (_isDamaged && sliderHealth)
         // {
         //     Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);
         //    
         //     sliderHealth.transform.position = screenPosition;
         // }
    }
    
    private void FixedUpdate()
    {
        Movement();
    }
    
    private void Movement()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, _target, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }
    
    public void ReduceHealth(float damage)
    {
        // if (damage > 0 && !_isDamaged)
        // {
        //     _isDamaged = true;
        //     sliderHealth = Instantiate(prefabSliderHealth.gameObject, _canvas.transform).GetComponent<Slider>();
        //     sliderHealth.maxValue = _health;
        //     //Debug.Log("slider instantiated");
        // }
        
        StartCoroutine(FlashWhite());
        health -= damage;
        //Debug.Log("-damage");
        //sliderHealth.value = _health;
    }
    
    IEnumerator FlashWhite()
    {
        foreach (SpriteRenderer childSpriteRenderer in _childSpriteRenderers)
        {
            childSpriteRenderer.material = _matFlashDamaged;
            
        }
        
        yield return new WaitForSeconds(0.12f);
        
        foreach (SpriteRenderer childSpriteRenderer in _childSpriteRenderers)
        {
            childSpriteRenderer.material = _matOriginal;
        }
    }
}

