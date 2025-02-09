using System;
using UnityEngine;

public class Base : MonoBehaviour
{
    private GameManager gameManager;
    
    [SerializeField] private int baseStartingHealth;
    [SerializeField] private int baseHealth;

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
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            baseHealth -= enemy.GetDamage();
            gameManager.UpdateBaseHealth();
            Destroy(collision.gameObject);
        }
    }
}
