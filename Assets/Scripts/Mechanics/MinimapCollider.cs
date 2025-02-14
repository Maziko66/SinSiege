using System;
using Unity.VisualScripting;
using UnityEngine;

public class MinimapCollider : MonoBehaviour
{
    private Minimap _minimap;

    private void Awake()
    {
        _minimap = FindFirstObjectByType<Minimap>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _minimap.AddObjectToTrack(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _minimap.RemoveObjectFromList(other.gameObject);
        }
    }
}
