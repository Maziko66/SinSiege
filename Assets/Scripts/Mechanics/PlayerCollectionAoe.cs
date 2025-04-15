using System;
using UnityEngine;

public class PlayerCollectionAoe : MonoBehaviour
{
    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Coin"))
        {
            Coin coin = other.gameObject.GetComponent<Coin>();
            _gameManager.coins += coin.value;
            _gameManager.UpdateCoinsText();
            Destroy(coin.gameObject);
        }
    }
}
