using System;
using System.Collections.Generic;
using UnityEngine;

public class Minimap : MonoBehaviour
{
    [System.Serializable]
    public class TagPrefabPair
    {
        public string tag;
        public GameObject prefab;
    }
    
    [Header("Minimap Settings")]
    [SerializeField] private MinimapCollider minimapCollider;
    [SerializeField] private float scaleFactor = 4f;
    [SerializeField] private Vector2 offset = new Vector2(128, 128);
    
    [Header("Pixel Prefabs")]
    [SerializeField] private List<TagPrefabPair> tagPrefabPairs;
    
    private List<GameObject> _objectsToTrack = new List<GameObject>();

    private Dictionary<GameObject, GameObject> _objectToPixelMap = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, Vector3> _lastKnownPositions = new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        if (minimapCollider == null)
        {
            minimapCollider = FindFirstObjectByType<MinimapCollider>();
        }

        if (minimapCollider == null)
        {
            Debug.LogError("Minimap: No MinimapCollider found in the scene.");
        }
    }
    
    private void Update()
    {
        if (minimapCollider == null) return;

        Vector2 mmcPosition = new Vector2(minimapCollider.transform.position.x, minimapCollider.transform.position.y);

        foreach (var obj in _objectsToTrack)
        {
            if (!_objectToPixelMap.TryGetValue(obj, out GameObject pixel) || pixel == null) continue;

            // Check if the object has moved
            Vector3 objPosition = obj.transform.position;
            if (_lastKnownPositions.TryGetValue(obj, out var lastPos) && lastPos == objPosition)
            {
                if (obj.CompareTag("Base")) 
                {
                    UpdatePixelPosition(obj, mmcPosition, pixel);
                }
                continue;
            }

            UpdatePixelPosition(obj, mmcPosition, pixel);

            _lastKnownPositions[obj] = objPosition;
        }
    }

    private void UpdatePixelPosition(GameObject obj, Vector2 mmcPosition, GameObject pixel)
    {
        Vector2 drawPos = (new Vector2(obj.transform.position.x, obj.transform.position.y) - mmcPosition) * scaleFactor - offset;
        pixel.transform.localPosition = drawPos;
    }

    public void AddObjectToTrack(GameObject obj)
    {
        if (_objectToPixelMap.ContainsKey(obj)) return;
        
        GameObject prefab = GetPrefabForTag(obj.tag);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab found for tag: {obj.tag}");
            return;
        }

        _objectsToTrack.Add(obj);
        
        GameObject newPixel = Instantiate(prefab, transform);
        
        Vector2 mmcPosition = new Vector2(minimapCollider.transform.position.x, minimapCollider.transform.position.y);
        Vector2 drawPos = (new Vector2(obj.transform.position.x, obj.transform.position.y) - mmcPosition) * scaleFactor - offset;
        newPixel.transform.localPosition = drawPos;
        
        _objectToPixelMap[obj] = newPixel;
        _lastKnownPositions[obj] = obj.transform.position;

        //Debug.Log($"Added {obj.name} (tag: {obj.tag}) to objectsToTrack");
    }

    public void RemoveObjectFromList(GameObject obj)
    {
        if (obj == null) return; // FIX: Prevent null reference

        if (_objectToPixelMap.TryGetValue(obj, out var pixel))
        {
            Destroy(pixel);
            _objectToPixelMap.Remove(obj);
            _lastKnownPositions.Remove(obj);
        }

        _objectsToTrack.Remove(obj);
    }

    private GameObject GetPrefabForTag(string objTag)
    {
        foreach (var pair in tagPrefabPairs)
        {
            if (pair.tag == objTag)
            {
                return pair.prefab;
            }
        }
        return null;
    }
}
