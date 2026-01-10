using UnityEngine;
using UnityEngine.UI;

public class InfiniteScroll : MonoBehaviour
{
    [Header("Settings")]
    public float snapSpeed = 10f; // Higher = faster snap
    public bool centerOnScreen = true; // If true, centers the selected item in the viewport

    [Header("References")]
    public ScrollRect scrollRect;
    public RectTransform viewportTransform; // Needed for centering logic
    public RectTransform contentPanelTransform;
    public HorizontalLayoutGroup HLG;
    public RectTransform[] ItemList;

    private float _oneSetWidth;
    private float _itemAndSpaceWidth;
    private float _targetX;
    private bool _isInitialized;

    void Start()
    {
        // 0. Disable ScrollRect mouse/touch control
        scrollRect.horizontal = false; 
        scrollRect.inertia = false;

        // 1. Calculate widths
        // Assumes all items are the same width
        float itemWidth = ItemList[0].rect.width;
        float spacing = HLG.spacing;
        
        _itemAndSpaceWidth = itemWidth + spacing;
        _oneSetWidth = _itemAndSpaceWidth * ItemList.Length;

        // 2. Clear & Instantiate 3 sets
        foreach (Transform child in contentPanelTransform)
        {
            Destroy(child.gameObject);
        }
        
        for (int i = 0; i < 3; i++)
        {
            foreach (var itemPrefab in ItemList)
            {
                RectTransform RT = Instantiate(itemPrefab, contentPanelTransform);
                RT.gameObject.SetActive(true);
                RT.localScale = Vector3.one;
            }
        }
        
        // 3. Initialize Positions
        // Start at Set 1 (The Middle Set)
        float startX = -_oneSetWidth;

        // Optional: Add offset to center the item in the viewport
        if (centerOnScreen && viewportTransform != null)
        {
            float viewportCenter = viewportTransform.rect.width / 2f;
            float itemCenter = itemWidth / 2f;
            startX += (viewportCenter - itemCenter);
        }

        _targetX = startX;
        
        InitializePosition(startX);
    }

    private void InitializePosition(float xPos)
    {
        Canvas.ForceUpdateCanvases();
        contentPanelTransform.anchoredPosition = new Vector2(xPos, contentPanelTransform.anchoredPosition.y);
        _isInitialized = true;
    }

    // --- BUTTON CONTROLS ---
    
    public void OnNextButtonClick()
    {
        _targetX -= _itemAndSpaceWidth;
    }

    public void OnPrevButtonClick()
    {
        _targetX += _itemAndSpaceWidth;
    }

    // --- MAIN LOOP ---

    void Update()
    {
        if (!_isInitialized) return;

        // 1. Smoothly move towards the target
        float currentX = contentPanelTransform.anchoredPosition.x;
        float newX = Mathf.Lerp(currentX, _targetX, Time.deltaTime * snapSpeed);
        
        // Snap to target if very close (prevents micro-jitter)
        if (Mathf.Abs(newX - _targetX) < 0.1f) newX = _targetX;

        contentPanelTransform.anchoredPosition = new Vector2(newX, contentPanelTransform.anchoredPosition.y);

        // 2. Seamless Teleport Logic
        // We check the TARGET position for the seamless loop to ensure consistent logic
        // If the target has scrolled past the bounds, we shift BOTH the physical object and the target value.

        // Moving Right -> Left (Next)
        // If we have moved past Set 1 completely into Set 2...
        // Threshold: The start of Set 2 (which is -2 * oneSetWidth)
        // We add the centering offset to the threshold check if needed
        
        float relativeX = contentPanelTransform.anchoredPosition.x;
        
        // Note: The thresholds are slightly loose to allow the lerp to overshoot smoothly
        if (relativeX > 0) 
        {
            // Too far Right (Seeing Left Buffer) -> Teleport Left
            contentPanelTransform.anchoredPosition -= new Vector2(_oneSetWidth, 0);
            _targetX -= _oneSetWidth;
        }
        else if (relativeX < -(_oneSetWidth * 2)) 
        {
            // Too far Left (Seeing Right Buffer) -> Teleport Right
            contentPanelTransform.anchoredPosition += new Vector2(_oneSetWidth, 0);
            _targetX += _oneSetWidth;
        }
    }
}