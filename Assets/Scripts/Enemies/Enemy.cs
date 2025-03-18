using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    Rigidbody2D rb;

    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _health = 5f;
    [SerializeField] private int damage;

    [SerializeField] private Vector3 _mainTargetLocation = Vector3.zero;
    
    
    public List<Vector2> waypoints = new List<Vector2>();
    private Vector2 _waypointStop;
    private int _waypointsIndex = 0;
    
    
    private void Awake()
    {
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
        _health -= damage;
    }

    public void CheckHealth()
    {
        if (_health <= 0)
        {
            //StartCoroutine(DestroyWithDelay(gameObject));
            Destroy(gameObject); return;
        }
    }
    
    IEnumerator DestroyWithDelay(GameObject obj)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        Destroy(obj);
    }
    

    public int GetDamage()
    {
        return damage;
    }
}
