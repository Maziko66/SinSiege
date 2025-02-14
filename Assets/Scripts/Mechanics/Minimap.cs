using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Minimap : MonoBehaviour
{
    [SerializeField] private GameObject pixelPrefab;
    [SerializeField] private MinimapCollider minimapCollider;
    [SerializeField] private float scaleFactor = 4f;
    [SerializeField] private Vector2 offset = new Vector2(128, 128);
    
    [SerializeField] private List<GameObject> objectsToTrack;
    
    private Dictionary<GameObject, GameObject> objectToPixelMap = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, Vector3> lastKnownPositions = new Dictionary<GameObject, Vector3>();

    private List<GameObject> _serializedPixels = new List<GameObject>();
    private void Awake()
    {
        if (minimapCollider == null)
        {
            minimapCollider = FindFirstObjectByType<MinimapCollider>();
        }

        if (minimapCollider == null)
        {
            Debug.LogWarning("Minimap: No MinimapCollider found in the scene.");
        }
    }

    // private void Update()
    // {
    //     foreach (GameObject obj in _serializedPixels)
    //     {
    //         Destroy(obj.gameObject);
    //     }
    //     
    //     Vector2 mmcPosition = new Vector2(minimapCollider.transform.position.x, minimapCollider.transform.position.y);
    //     foreach (GameObject obj in objectsToTrack)
    //     {
    //         Vector2 drawPos =new Vector2(obj.transform.position.x, obj.transform.position.y) - mmcPosition;
    //         drawPos = (drawPos * scaleFactor) - offset;
    //         GameObject newPixel = Instantiate(pixel, transform);
    //         _serializedPixels.Add(newPixel);
    //         newPixel.transform.localPosition = drawPos;
    //     }
    // }
    
    private void Update()
    {
        Vector2 mmcPosition = new Vector2(minimapCollider.transform.position.x, minimapCollider.transform.position.y);

        foreach (var obj in objectsToTrack)
        {
            if (!objectToPixelMap.ContainsKey(obj)) continue;

            // Check if the object has moved
            if (lastKnownPositions.TryGetValue(obj, out var lastPos) && lastPos == obj.transform.position)
            {
                continue; // Skip if the object hasn't moved
            }

            // Update the pixel position
            Vector2 drawPos = (new Vector2(obj.transform.position.x, obj.transform.position.y) - mmcPosition) * scaleFactor - offset;
            objectToPixelMap[obj].transform.localPosition = drawPos;

            // Update the last known position
            lastKnownPositions[obj] = obj.transform.position;
        }
    }
    
    // public void AddObjectToTrack(GameObject obj)
    // {
    //     objectsToTrack.Add(obj);
    //     Debug.Log("Added " + obj.name + " to objectsToTrack");
    // }
    //
    // public void RemoveObjectFromList(GameObject obj)
    // {
    //     objectsToTrack.Remove(obj);
    // }
    
    public void AddObjectToTrack(GameObject obj)
    {
        if (objectToPixelMap.ContainsKey(obj)) return;

        objectsToTrack.Add(obj);
        
        GameObject newPixel = Instantiate(pixelPrefab, transform);
        
        Vector2 mmcPosition = new Vector2(minimapCollider.transform.position.x, minimapCollider.transform.position.y);
        Vector2 drawPos = (new Vector2(obj.transform.position.x, obj.transform.position.y) - mmcPosition) * scaleFactor - offset;
        newPixel.transform.localPosition = drawPos;
        
        objectToPixelMap[obj] = newPixel;
        lastKnownPositions[obj] = obj.transform.position;

        Debug.Log("Added " + obj.name + " to objectsToTrack");
    }

    public void RemoveObjectFromList(GameObject obj)
    {
        if (objectToPixelMap.TryGetValue(obj, out var pixel))
        {
            Destroy(pixel);
            objectToPixelMap.Remove(obj);
            lastKnownPositions.Remove(obj);
        }

        objectsToTrack.Remove(obj);
    }
}
