using System;
using UnityEngine;

public class Base : MonoBehaviour
{
    private GameManager gameManager;

    [Header("Base Angel")]
    [SerializeField] private GameObject baseAngel;
    [SerializeField] private float baseAngelOscillateSpeed = 1f;
    [SerializeField] private float baseAngelOscillateAmplitude = 1f;
    private Vector3 _baseAngelStartingPosition;

    [Header("Rotating Balls")]
    [SerializeField] private Transform[] balls;
    [SerializeField] private Transform ballsCenter;
    [SerializeField] private float ballsRadiusX = 3f;
    [SerializeField] private float ballsRadiusY = 3f;
    [SerializeField] private float ballsRadiusZ = 1;
    [SerializeField] private float ballsSpeed = 1f;
    [SerializeField] private float ballsOscillateCoefficientX = 1f;
    [SerializeField] private float ballsOscillateCoefficientY = 1f;
    [SerializeField] private float ballsOscillateSpeedX = 1f;
    [SerializeField] private float ballsOscillateSpeedY = 1f;
    
    [Header("Values")]    
    [SerializeField] private int baseStartingHealth;
    [SerializeField] private int baseHealth;
    [SerializeField] private int baseDamage = 999;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        baseHealth = baseStartingHealth;
    }

    private void Start()
    {
        _baseAngelStartingPosition = baseAngel.transform.position;
    }

    private void Update()
    {
        baseAngel.transform.position = _baseAngelStartingPosition + Vector3.up * (Mathf.Sin(Time.time * baseAngelOscillateSpeed) * baseAngelOscillateAmplitude); // base angel hovering anim
        RotateBalls();
    }

    public int GetBaseHealth()
    {
        return baseHealth;
    }

    public int GetBaseStartingHealth()
    {
        return baseStartingHealth;
    }

    private void RotateBalls()
    {
        float time = Time.time * ballsSpeed;

        for (int i = 0; i < balls.Length; i++)
        {
            float angle = time + (i * Mathf.PI * 2 / balls.Length); // even spacing
            Vector3 pos = GetCircularPosition(ballsCenter.position, ballsRadiusX, ballsRadiusY, ballsRadiusZ, angle);
            balls[i].position = pos;
        }
    }
    
    Vector3 GetCircularPosition(Vector3 center, float radiusX, float radiusY, float radiusZ, float angle)
    {
        float x = Mathf.Cos(angle) * radiusX;
        float y = Mathf.Sin(angle) * (radiusY + Mathf.Sin(Time.time * ballsOscillateSpeedY) * ballsOscillateCoefficientY);
        //float z = Mathf.Sin(angle) * radiusZ;
        return center + new Vector3(x, y, 0);
    }
    
    
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("EnemyGround") || other.CompareTag("EnemyAir"))
        {
            //Debug.Log("enemy on base");
            Enemy enemy = other.GetComponent<Enemy>();
            baseHealth -= enemy.GetDamage();
            gameManager.UpdateBaseHealth();
            enemy.ReduceHealth(baseDamage);
            enemy.CheckHealth();
            //Destroy(other.gameObject);
        }
    }
}
