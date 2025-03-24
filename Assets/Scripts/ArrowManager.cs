using System;
using System.Collections.Generic;
using UnityEngine;

public class ArrowManager : MonoBehaviour
{
    private Player _player;
    
    [Header("Settings")]
    public GameObject arrowPrefab;
    public string targetTag = "Target";
    public float checkRadius = 25f;
    public LayerMask targetLayer;
    
    [Header("References")]
    public Transform player;
    public Transform arrowsParent;
    
    private List<Transform> currentTargets = new List<Transform>();
    private List<OffScreenArrow> arrows = new List<OffScreenArrow>();
    private Canvas parentCanvas;

    private void Awake()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }
    
    private void Update()
    {
        if (_player.isPaused) {return;}
        FindTargets();
        UpdateArrows();
    }
    
    private void FindTargets()
    {
        currentTargets.Clear();
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, checkRadius, targetLayer);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(targetTag))
            {
                currentTargets.Add(hit.transform);
            }
        }
    }
    
    private void UpdateArrows()
    {
        // Create new arrows if needed
        while (arrows.Count < currentTargets.Count)
        {
            GameObject newArrow = Instantiate(arrowPrefab, arrowsParent);
            
            arrows.Add(newArrow.GetComponent<OffScreenArrow>());
        }
        
        // Disable extra arrows
        for (int i = currentTargets.Count; i < arrows.Count; i++)
        {
            arrows[i].gameObject.SetActive(false);
        }
        
        // Assign targets to active arrows
        for (int i = 0; i < currentTargets.Count; i++)
        {
            if (!arrows[i].gameObject.activeSelf)
            {
                arrows[i].gameObject.SetActive(true);
            }
            arrows[i].SetTarget(currentTargets[i]);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, checkRadius);
        }
    }
    
    private void CreateNewArrow()
    {
        GameObject newArrow = Instantiate(arrowPrefab, transform);
        newArrow.transform.SetParent(transform); // Ensure it's parented to the manager
        arrows.Add(newArrow.GetComponent<OffScreenArrow>());
    }
}