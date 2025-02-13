using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private GameManager _gameManager;
    private Rigidbody2D _rb;
    private Camera _cam;

    public InputAction playerControls;
    
    [Header("Equipment")]
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Fists fists;
    
    [Header("Character Stats")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Triggers")]
    public GameObject lastTouchedTowerZone;
    #region ANIMATOR
    private Animator _animator;
    #endregion

    private Vector2 _moveDirection = Vector2.zero;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _gameManager = FindFirstObjectByType<GameManager>();
        _cam = FindFirstObjectByType<Camera>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        _moveDirection = playerControls.ReadValue<Vector2>();

        _animator.SetFloat("moveY", _moveDirection.y);
        _animator.SetFloat("moveX", _moveDirection.x);

    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = new Vector2(_moveDirection.x, _moveDirection.y).normalized * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            TowerZone towerZone = collision.GetComponent<TowerZone>();
            lastTouchedTowerZone = towerZone.gameObject;
            Vector3 instantiatePosition = _cam.WorldToScreenPoint(collision.transform.position);
            if(towerZone.isEmpty)
            {
                _gameManager.DrawUITowerBuilderCombat(instantiatePosition);
                Debug.Log("On Tower Zone Empty");
            }
            else
            {
                _gameManager.DrawUITowerManagerCombat(instantiatePosition);
                Debug.Log("On Tower Zone Full");
            }
        }
    }

    private void OnAttack()
    {
        System.Diagnostics.Debug.Assert(Camera.main != null, "Camera.main != null");
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        shotgun.Fire(mousePosition);
        //Debug.Log("fire");
    }

    private void OnSecondaryAttack()
    {
        System.Diagnostics.Debug.Assert(Camera.main != null, "Camera.main != null");
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f; // Ensure it's in 2D space
        fists.Attack(mousePosition);
        //Debug.Log("secondary");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            TowerZone towerZone = collision.GetComponent<TowerZone>();
            if(towerZone.isEmpty)
            {
                _gameManager.DestroyUITowerBuilderCombat();
                lastTouchedTowerZone = null;
                Debug.Log("Left Empty Tower Zone");
            }
            else
            {
                _gameManager.DestroyUITowerManagerCombat();
                lastTouchedTowerZone = null;
                Debug.Log("Left Full Tower Zone");
            }
        }
    }
}
