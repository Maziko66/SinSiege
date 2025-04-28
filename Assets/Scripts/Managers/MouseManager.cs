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
        angle = (angle + 360) % 360;
        _mouseDirection = AngleToDirection(angle);
    }
    
    private int AngleToDirection(float angle)
    {
        // if (angle >= 45 && angle < 135)
        //     return 1; // North
        // else if (angle >= 135 && angle < 225)
        //     return 2; // West
        // else if (angle >= 225 && angle < 315)
        //     return 3; // South
        // else
        //     return 4; // East
        return Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
    }

    // public int MouseDirectionX()
    // {
    //     if (_mouseDirection == 2)
    //     {
    //         return -1;
    //     }
    //     else if (_mouseDirection == 4)
    //     {
    //         return 1;
    //     }
    //     else
    //     {
    //         return 0;
    //     }
    // }
    //
    // public int MouseDirectionY()
    // {
    //     if (_mouseDirection == 1)
    //     {
    //         return 1;
    //     }
    //     else if (_mouseDirection == 3)
    //     {
    //         return -1;
    //     }
    //     else
    //     {
    //         return 0;
    //     }
    // }
    
    public int MouseDirectionX()
    {
        return _mouseDirection switch
        {
            0 => 1,    // E
            1 => 1,    // NE
            2 => 0,    // N
            3 => -1,   // NW
            4 => -1,   // W
            5 => -1,   // SW
            6 => 0,    // S
            7 => 1,    // SE
            _ => 0
        };
    }

    public int MouseDirectionY()
    {
        return _mouseDirection switch
        {
            0 => 0,    // E
            1 => 1,    // NE
            2 => 1,    // N
            3 => 1,    // NW
            4 => 0,    // W
            5 => -1,   // SW
            6 => -1,   // S
            7 => -1,   // SE
            _ => 0
        };
    }
    
    public Vector2Int MouseDirection8()
    {
        // Maps index to unit vector directions
        return _mouseDirection switch
        {
            0 => new Vector2Int(1, 0),   // E
            1 => new Vector2Int(1, 1),   // NE
            2 => new Vector2Int(0, 1),   // N
            3 => new Vector2Int(-1, 1),  // NW
            4 => new Vector2Int(-1, 0),  // W
            5 => new Vector2Int(-1, -1), // SW
            6 => new Vector2Int(0, -1),  // S
            7 => new Vector2Int(1, -1),  // SE
            _ => Vector2Int.zero
        };
    }
    
}