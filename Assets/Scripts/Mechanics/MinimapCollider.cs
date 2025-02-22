using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MinimapCollider : MonoBehaviour
{
    [SerializeField] private Minimap minimap;

    [SerializeField] private static readonly HashSet<string> trackableTags = new HashSet<string>()
    {
        "Enemy", "EnemyAir", "EnemyGround", "Base", "Tower"
    };
    
    private void Awake()
    {
        if (minimap == null)
        {
            minimap = FindFirstObjectByType<Minimap>();
        }

        if (minimap == null)
        {
            Debug.LogError("MinimapCollider: No Minimap found in the scene.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (minimap == null) return;
        //Debug.Log("on trigger minimap: " + other.gameObject.name);
        if (trackableTags.Contains(other.tag))
        {
            //Debug.Log("adding obj to trak: " + other.gameObject.name);
            minimap.AddObjectToTrack(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (minimap == null) return;

        //if (other.CompareTag("Enemy") || other.CompareTag("Base") || other.CompareTag("EnemyAir") || other.CompareTag("EnemyGround"))
        if (trackableTags.Contains(other.tag))
        {
            minimap.RemoveObjectFromList(other.gameObject);
            //Debug.Log("remove object from list triggered");
        }
    }
}