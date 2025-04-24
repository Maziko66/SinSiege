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
        if (_player.isPaused) return;

        FindTargets();
        UpdateArrows();
    }

    private void FindTargets()
    {
        currentTargets.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, checkRadius, targetLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("EnemyAir") || hit.CompareTag("EnemyGround"))
            {
                currentTargets.Add(hit.transform);
            }
        }
    }

    private void UpdateArrows()
    {
        // Ensure we have enough arrows
        while (arrows.Count < currentTargets.Count)
        {
            GameObject newArrow = Instantiate(arrowPrefab, arrowsParent);
            OffScreenArrow arrowComponent = newArrow.GetComponent<OffScreenArrow>();

            if (arrowComponent == null)
            {
                Debug.LogError("Arrow prefab does not have OffScreenArrow component!");
                Destroy(newArrow);
                return;
            }

            arrows.Add(arrowComponent);
        }

        // Disable unused arrows
        for (int i = currentTargets.Count; i < arrows.Count; i++)
        {
            if (arrows[i] != null && arrows[i].gameObject.activeSelf)
            {
                arrows[i].gameObject.SetActive(false);
            }
        }

        // Update and activate used arrows
        for (int i = 0; i < currentTargets.Count; i++)
        {
            OffScreenArrow arrow = arrows[i];

            if (arrow == null)
            {
                Debug.LogWarning($"Arrow at index {i} is null!");
                continue;
            }

            if (!arrow.gameObject.activeSelf)
            {
                arrow.gameObject.SetActive(true);
            }

            arrow.SetTarget(currentTargets[i]);
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
}
