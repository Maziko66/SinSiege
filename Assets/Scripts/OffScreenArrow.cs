using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform)), RequireComponent(typeof(Image))]
public class OffScreenArrow : MonoBehaviour
{
    private Player _player;
    
    [Header("Settings")]
    public Transform target;
    public float edgePadding = 50f;
    public float minScale = 0.7f;
    public float maxScale = 1.3f;
    public float maxDistance = 30f;
    
    [Header("References")]
    public RectTransform arrowRect;
    public Image arrowImage;
    
    private Camera mainCamera;
    private RectTransform canvasRect;
    private Vector2 screenCenter;
    private Vector2 lastScreenSize;

    private void Awake()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        UpdateScreenCenter();
        
        if (arrowRect == null) arrowRect = GetComponent<RectTransform>();
        if (arrowImage == null) arrowImage = GetComponent<Image>();
    }

    private void Update()
    {
        if(_player.isPaused) {return;}
        // update for screen res, might need fixing
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            UpdateScreenCenter();
        }

        if (target == null)
        {
            arrowImage.enabled = false;
            return;
        }
        
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
        
        bool isOnScreen = screenPos.z > 0 && 
                         screenPos.x > 0 && screenPos.x < Screen.width &&
                         screenPos.y > 0 && screenPos.y < Screen.height;
        
        arrowImage.enabled = !isOnScreen;
        
        if (isOnScreen) return;
        
        if (screenPos.z < 0)
        {
            screenPos *= -1;
        }
        
        Vector2 screenDir = (new Vector2(screenPos.x, screenPos.y) - screenCenter);
        screenDir.Normalize();
        
        float angle = Mathf.Atan2(screenDir.y, screenDir.x);
        Vector2 edgePos = CalculateEdgePosition(angle);
        
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            edgePos,
            null,
            out canvasPos);
        
        arrowRect.anchoredPosition = canvasPos;
        arrowRect.localEulerAngles = new Vector3(0, 0, angle * Mathf.Rad2Deg);
        
        float distance = Vector3.Distance(mainCamera.transform.position, target.position);
        float scale = Mathf.Lerp(maxScale, minScale, Mathf.Clamp01(distance / maxDistance));
        arrowRect.localScale = Vector3.one * scale;
    }

    private void UpdateScreenCenter()
    {
        screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    private Vector2 CalculateEdgePosition(float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        
        float halfWidth = (Screen.width * 0.5f) - edgePadding;
        float halfHeight = (Screen.height * 0.5f) - edgePadding;
        
        float intersectX, intersectY;
        
        if (Mathf.Abs(cos) > Mathf.Abs(sin))
        {
            intersectX = Mathf.Sign(cos) * halfWidth;
            intersectY = intersectX * Mathf.Tan(angle);
            
            if (Mathf.Abs(intersectY) > halfHeight)
            {
                intersectY = Mathf.Sign(sin) * halfHeight;
                intersectX = intersectY / Mathf.Tan(angle);
            }
        }
        else
        {
            intersectY = Mathf.Sign(sin) * halfHeight;
            intersectX = intersectY / Mathf.Tan(angle);
            
            if (Mathf.Abs(intersectX) > halfWidth)
            {
                intersectX = Mathf.Sign(cos) * halfWidth;
                intersectY = intersectX * Mathf.Tan(angle);
            }
        }
        
        return screenCenter + new Vector2(intersectX, intersectY);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        arrowImage.enabled = target != null;
    }
}