using UnityEngine;
using UnityEngine.Serialization;

public class MinimapCollider : MonoBehaviour
{
    [FormerlySerializedAs("_minimap")] [SerializeField] private Minimap minimap;

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
        if (minimap == null) return; // FIX: Prevent crash

        if (other.CompareTag("Enemy") || other.CompareTag("Base"))
        {
            minimap.AddObjectToTrack(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (minimap == null) return; // FIX: Prevent crash

        if (other.CompareTag("Enemy") || other.CompareTag("Base"))
        {
            minimap.RemoveObjectFromList(other.gameObject);
        }
    }
}