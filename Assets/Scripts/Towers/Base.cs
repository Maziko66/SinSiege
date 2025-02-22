using System;
using UnityEngine;

public class Base : MonoBehaviour
{
    private GameManager gameManager;
    
    [SerializeField] private int baseStartingHealth;
    [SerializeField] private int baseHealth;
    [SerializeField] private int baseDamage = 999;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        baseHealth = baseStartingHealth;
    }
    

    public int GetBaseHealth()
    {
        return baseHealth;
    }

    public int GetBaseStartingHealth()
    {
        return baseStartingHealth;
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
