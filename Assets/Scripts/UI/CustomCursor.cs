using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image image;
    
    public List<Sprite> sprites = new List<Sprite>();
    
    
    Vector2 localMousePosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    private void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponentInParent<Canvas>().GetComponent<RectTransform>(),
           Input.mousePosition,
           GetComponentInParent<Canvas>().worldCamera,
           out localMousePosition))
        {
            rectTransform.anchoredPosition = localMousePosition;
        }
    }

    public void ResizeOnHovering()
    {
        SetCursorSprite(1);
        //Debug.Log("Mouse hovering over resize object.");
    }

    public void ResizeLeftHovering()
    {
        SetCursorSprite(0);
        //Debug.Log("Mouse left hovering over resize object.");
    }

    public void SetCursorSprite(int index)
    {
        image.sprite = sprites[index];
    }

    public Vector2 GetPosition()
    {
        return localMousePosition;
    }
}
