using System;
using UnityEngine;

public class MouseManager : MonoBehaviour
{
    private Player _player;

    [SerializeField] private Vector2 _mousePos;
    public Vector2 MousePos => _mousePos;

    [SerializeField] private float screenHeight;
    [SerializeField] private float screenWidth;

    [SerializeField] private Vector2 _mousePosNormalized;
    public Vector2 MousePosNormalized => _mousePosNormalized;

    [SerializeField] private int _mouseDirection;
    public int MouseDirection => _mouseDirection;

    private void Awake()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        screenHeight = Screen.height;
        screenWidth = Screen.width;
    }

    private void Update()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        if (_player.isPaused)
        {
            return;
        }

        Vector2 halfScreen = new Vector2(screenWidth / 2, screenHeight / 2);
        _mousePos = (Vector2)Input.mousePosition - halfScreen;
        _mousePosNormalized = new Vector2(Mathf.Sign(_mousePos.x), Mathf.Sign(_mousePos.y));
        
        float angle = Mathf.Atan2(_mousePos.y, _mousePos.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360; // Normalize angle to 0-360
        _mouseDirection = AngleToDirection(angle);
    }
    
    private int AngleToDirection(float angle)
    {
        if (angle >= 45 && angle < 135)
            return 1; // North
        else if (angle >= 135 && angle < 225)
            return 2; // West
        else if (angle >= 225 && angle < 315)
            return 3; // South
        else
            return 4; // East
    }

    public int MouseDirectionX()
    {
        if (_mouseDirection == 2)
        {
            return -1;
        }
        else if (_mouseDirection == 4)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
    
    public int MouseDirectionY()
    {
        if (_mouseDirection == 1)
        {
            return 1;
        }
        else if (_mouseDirection == 3)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }
    
}