using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private GameManager gameManager;
    private Rigidbody2D rb;
    private Camera cam;

    public InputAction playerControls;

    [Header("Character Stats")]
    [SerializeField] private float _moveSpeed = 3f;

    [Header("Triggers")]
    public GameObject lastTouchedTowerZone;
    #region ANIMATOR
    private Animator animator;
    #endregion

    [SerializeField] private Vector2 _moveDirection = Vector2.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        gameManager = FindFirstObjectByType<GameManager>();
        cam = FindFirstObjectByType<Camera>();
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

        animator.SetFloat("moveY", _moveDirection.y);
        animator.SetFloat("moveX", _moveDirection.x);

    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(_moveDirection.x, _moveDirection.y).normalized * _moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            TowerZone towerZone = collision.GetComponent<TowerZone>();
            lastTouchedTowerZone = towerZone.gameObject;
            Vector3 instantiatePosition = cam.WorldToScreenPoint(collision.transform.position);
            if(towerZone.isEmpty)
            {
                gameManager.DrawUITowerBuilderCombat(instantiatePosition);
                Debug.Log("On Tower Zone Empty");
            }
            else
            {
                gameManager.DrawUITowerManagerCombat(instantiatePosition);
                Debug.Log("On Tower Zone Full");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            TowerZone towerZone = collision.GetComponent<TowerZone>();
            if(towerZone.isEmpty)
            {
                gameManager.DestroyUITowerBuilderCombat();
                lastTouchedTowerZone = null;
                Debug.Log("Left Empty Tower Zone");
            }
            else
            {
                gameManager.DestroyUITowerManagerCombat();
                lastTouchedTowerZone = null;
                Debug.Log("Left Full Tower Zone");
            }
        }
    }
}
