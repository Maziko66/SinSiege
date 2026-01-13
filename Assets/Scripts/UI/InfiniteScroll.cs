using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Added for UnityEvent

public class InfiniteScroll : MonoBehaviour
{
    [Header("Settings")]
    public float snapSpeed = 10f;
    public bool centerOnScreen = true;

    [Header("Events")]
    public UnityEvent<GameObject> onSelectionChanged; 

    [Header("References")]
    public ScrollRect scrollRect;
    public RectTransform viewportTransform;
    public RectTransform contentPanelTransform;
    public HorizontalLayoutGroup HLG;
    public RectTransform[] ItemList;

    private float _oneSetWidth;
    private float _itemAndSpaceWidth;
    private float _targetX;
    private bool _isInitialized;
    
    private int _currentIndex = 0; 

    void Start()
    {
        scrollRect.horizontal = false; 
        scrollRect.inertia = false;

        float itemWidth = ItemList[0].rect.width;
        float spacing = HLG.spacing;
        
        _itemAndSpaceWidth = itemWidth + spacing;
        _oneSetWidth = _itemAndSpaceWidth * ItemList.Length;

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
        
        float startX = -_oneSetWidth;
        if (centerOnScreen && viewportTransform != null)
        {
            float viewportCenter = viewportTransform.rect.width / 2f;
            float itemCenter = itemWidth / 2f;
            startX += (viewportCenter - itemCenter);
        }

        _targetX = startX;
        InitializePosition(startX);
        
        NotifySelection();
    }

    private void InitializePosition(float xPos)
    {
        Canvas.ForceUpdateCanvases();
        contentPanelTransform.anchoredPosition = new Vector2(xPos, contentPanelTransform.anchoredPosition.y);
        _isInitialized = true;
    }
    
    public void OnNextButtonClick()
    {
        _targetX -= _itemAndSpaceWidth;
        UpdateIndex(1);
    }

    public void OnPrevButtonClick()
    {
        _targetX += _itemAndSpaceWidth;
        UpdateIndex(-1);
    }

    private void UpdateIndex(int direction)
    {
        _currentIndex += direction;
        
        if (_currentIndex >= ItemList.Length)
        {
            _currentIndex = 0;
        }
        else if (_currentIndex < 0)
        {
            _currentIndex = ItemList.Length - 1;
        }

        NotifySelection();
    }

    private void NotifySelection()
    {
        GameObject selectedPrefab = ItemList[_currentIndex].gameObject;
        
        onSelectionChanged?.Invoke(selectedPrefab);
    }
    
    public GameObject GetSelectedCharacterPrefab()
    {
        if (ItemList == null || ItemList.Length == 0) return null;
        return ItemList[_currentIndex].gameObject;
    }
    
    public int GetSelectedIndex()
    {
        return _currentIndex;
    }

    public void DebugGetSelectedIndex()
    {
        int selectedIndex = GetSelectedIndex();
        Debug.Log(selectedIndex);
    }

    void Update()
    {
        if (!_isInitialized) return;

        float currentX = contentPanelTransform.anchoredPosition.x;
        float newX = Mathf.Lerp(currentX, _targetX, Time.deltaTime * snapSpeed);
        
        if (Mathf.Abs(newX - _targetX) < 0.1f) newX = _targetX;

        contentPanelTransform.anchoredPosition = new Vector2(newX, contentPanelTransform.anchoredPosition.y);

        float relativeX = contentPanelTransform.anchoredPosition.x;
        
        if (relativeX > 0) 
        {
            contentPanelTransform.anchoredPosition -= new Vector2(_oneSetWidth, 0);
            _targetX -= _oneSetWidth;
        }
        else if (relativeX < -(_oneSetWidth * 2)) 
        {
            contentPanelTransform.anchoredPosition += new Vector2(_oneSetWidth, 0);
            _targetX += _oneSetWidth;
        }
    }
}