using UnityEngine;

public class UIMouseParallax : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How much the UI element moves. Higher values = more movement.")]
    public float parallaxAmount = 20f;

    [Tooltip("If true, the element moves in the opposite direction of the mouse.")]
    public bool invertDirection = false;

    [Tooltip("Smoothing speed for the movement. Lower = snappier, Higher = floatier.")]
    public float smoothTime = 0.3f;

    private Vector2 _startPos;
    private Vector2 _targetPos;
    private Vector2 _currentVelocity;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        Vector2 centeredMousePos = Input.mousePosition - new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        
        float normalizedX = centeredMousePos.x / (Screen.width / 2f);
        float normalizedY = centeredMousePos.y / (Screen.height / 2f);
        
        float directionMultiplier = invertDirection ? -1f : 1f;

        _targetPos = new Vector2(
            _startPos.x + (normalizedX * parallaxAmount * directionMultiplier),
            _startPos.y + (normalizedY * parallaxAmount * directionMultiplier)
        );
        
        transform.position = Vector2.SmoothDamp(
            transform.position,
            _targetPos,
            ref _currentVelocity,
            smoothTime
        );
    }

    public void ResetStartPosition()
    {
        _startPos = transform.position;
    }
}