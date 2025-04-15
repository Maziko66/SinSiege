using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    Rigidbody2D rb;
    private Canvas _canvas;
    private GameManager _gameManager;

    [SerializeField] private Slider prefabSliderHealth;
    [SerializeField] private Vector3 sliderOffset;
    
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _health = 5f;
    [SerializeField] private int damage;
    [SerializeField] private float exp = 1;

    [SerializeField] private Vector3 _mainTargetLocation = Vector3.zero;
    
    
    public List<Vector2> waypoints = new List<Vector2>();
    private Vector2 _waypointStop;
    private int _waypointsIndex = 0;


    private Slider sliderHealth;
    private bool _isDamaged;

    public int coinValue;
    
    private void Awake()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _gameManager = FindFirstObjectByType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (waypoints.Count == 0)
        {
            _waypointStop = _mainTargetLocation;
        }
        else
        {
            _waypointStop = waypoints[_waypointsIndex];
        }
        
    }

    private void Update()
    {
        if(rb.position.x - _waypointStop.x > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform .localScale = new Vector3(1, 1, 1);
        }

        if (rb.position == _waypointStop)
        {
            _waypointsIndex++;
            if (_waypointsIndex >= waypoints.Count)
            {
                _waypointStop = Vector2.zero;
            }
            else
            {
                _waypointStop = waypoints[_waypointsIndex];
            }
        }

        if (_isDamaged && sliderHealth)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);

            // Set the slider's position
            sliderHealth.transform.position = screenPosition;
        }
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, _waypointStop, _moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    public void ReduceHealth(float damage)
    {
        if (damage > 0 && !_isDamaged)
        {
            _isDamaged = true;
            sliderHealth = Instantiate(prefabSliderHealth.gameObject, _canvas.transform).GetComponent<Slider>();
            sliderHealth.maxValue = _health;
            //Debug.Log("slider instantiated");
        }
        _health -= damage;
        //Debug.Log("-damage");
        sliderHealth.value = _health;
    }

    public void CheckHealth()
    {
        if (_health <= 0)
        {
            //StartCoroutine(DestroyWithDelay(gameObject));
            _gameManager.SpawnCoins(coinValue, transform.position);
            Destroy(sliderHealth.gameObject);
            Destroy(gameObject);
            return;
        }
    }
    
    IEnumerator DestroyWithDelay(GameObject obj)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        Destroy(sliderHealth.gameObject);
        Destroy(obj);
    }
    

    public int GetDamage()
    {
        return damage;
    }
    
    public float GetExp()
    {
        return exp;
    }
}
