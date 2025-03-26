using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private GameManager _gameManager;
    private BuildManager _buildManager;
    private Rigidbody2D _rb;
    private Camera _cam;
    [SerializeField] private GameObject _buildCameraObject;
    private Rigidbody2D _buildCameraObjectRb;
    
    [Header("Util")]
    public InputAction playerControls;
    public InputAction playerSprint;
    
    
    public GraphicRaycaster raycaster;
    [SerializeField] private float buildCameraSpeed = 15f;
    private int _layerMaskTowerZone;
    [SerializeField] private bool _isSprinting;
    
    [Header("Equipment")]
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Fists fists;
    
    [Header("Character Stats")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;

    [Header("Triggers")]
    public GameObject lastTouchedTowerZone;

    #region ANIMATOR
    
    private Animator _animator;
    
    #endregion

    private Vector2 _moveDirection = Vector2.zero;

    public bool isPaused;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _gameManager = FindFirstObjectByType<GameManager>();
        _buildManager = FindFirstObjectByType<BuildManager>();
        _cam = FindFirstObjectByType<Camera>();

        _buildCameraObjectRb = _buildCameraObject.GetComponent<Rigidbody2D>();
        
        _layerMaskTowerZone = 1 << LayerMask.NameToLayer("TowerZone");
    }

    private void OnEnable()
    {
        playerControls.Enable();
        playerSprint.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
        playerSprint.Disable();
    }

    private void Update()
    {
        if(isPaused) {return;}
        
        _moveDirection = playerControls.ReadValue<Vector2>();
        _isSprinting = playerSprint.IsPressed();
        
        if(_gameManager.onBuildMenu) {return;}
        _animator.SetFloat("moveY", _moveDirection.y);
        _animator.SetFloat("moveX", _moveDirection.x);

    }

    private void FixedUpdate()
    {
        if (_gameManager.onBuildMenu)
        {
            _buildCameraObjectRb.linearVelocity = new Vector2(_moveDirection.x, _moveDirection.y).normalized * buildCameraSpeed;
            _rb.linearVelocity = Vector2.zero;
            Debug.Log("moving cam");
            return;
        }
        if(isPaused) {return;}

        if (_isSprinting)
        {
            _rb.linearVelocity = new Vector2(_moveDirection.x, _moveDirection.y).normalized * (moveSpeed + sprintSpeed);
        }
        else
        {
            _rb.linearVelocity = new Vector2(_moveDirection.x, _moveDirection.y).normalized * moveSpeed;
        }
        
    }

    #region CONTROLS
    private void OnAttack()
    {
        if(isPaused || IsMouseOverIgnoredUI()) {return;}

        if (_gameManager.onBuildMenu)
        {
            //Debug.Log("on attack build menu");
            Vector2 buildModeMousePos = Mouse.current.position.ReadValue();
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(buildModeMousePos);

            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, _layerMaskTowerZone);

            if (hit.collider == null)
            {
                DeactivateBuildUIs();
                return;
            }
            
            if (hit.collider.CompareTag("Tower Zone"))
            {
                lastTouchedTowerZone = hit.collider.gameObject;
                BuildManagerState(hit.collider, true, true);
                
                //Debug.Log("Clicked on: " + hit.collider.gameObject.name);

                // Add your custom logic here
                // Example: Call a method on the clicked object
                //hit.collider.GetComponent<TowerZone>()?.OnClick();
            }
            else
            {
                DeactivateBuildUIs();
            }
            return;
        }
        
        System.Diagnostics.Debug.Assert(Camera.main != null, "Camera.main != null");
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        shotgun.Fire(mousePosition);
        //Debug.Log("fire");
    }

    private void OnSecondaryAttack()
    {
        if(isPaused || IsMouseOverIgnoredUI() || _gameManager.onBuildMenu) {return;}
        System.Diagnostics.Debug.Assert(Camera.main != null, "Camera.main != null");
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f; // Ensure it's in 2D space
        fists.Attack(mousePosition);
        //Debug.Log("secondary");
    }

    private void OnPause()
    {
        Debug.Log("On Pause");
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
        Debug.Log(Time.timeScale);
    }

    private void OnBuild()
    {
        if(isPaused) {return;}
        lastTouchedTowerZone = null;
        _buildManager.TowerZoneExpSliderActive(false);
        _gameManager.SwapBuildAndCombatMenu();
    }
    
    #endregion

    private void BuildManagerState(Collider2D col, bool activate, bool calledFromBuildMenu = false)
    {
        if (calledFromBuildMenu)
        {
            DeactivateBuildUIs();
        }
        if (activate)
        {
            TowerZone towerZone = col.GetComponent<TowerZone>();
            lastTouchedTowerZone = towerZone.gameObject;
            //Vector3 instantiatePosition = _cam.WorldToScreenPoint(collision.transform.position);
            //Vector3 instantiatePosition = _cam.WorldToScreenPoint(transform.position);
            if(towerZone.isEmpty)
            {
                _buildManager.DrawUITowerBuilderCombat(calledFromBuildMenu);
                Debug.Log("On Tower Zone Empty");
            }
            else
            {
                _buildManager.DrawUITowerManagerCombat(calledFromBuildMenu);
                Debug.Log("On Tower Zone Full");
            }
        }
        else
        {
            TowerZone towerZone = col.GetComponent<TowerZone>();
            if(towerZone.isEmpty)
            {
                _buildManager.DestroyUITowerBuilderCombat();
                lastTouchedTowerZone = null;
                Debug.Log("Left Empty Tower Zone");
            }
            else
            {
                _buildManager.DestroyUITowerManagerCombat();
                lastTouchedTowerZone = null;
                Debug.Log("Left Full Tower Zone");
            }
        }
    }

    private void DeactivateBuildUIs()
    {
        _buildManager.DestroyUITowerBuilderCombat();
        _buildManager.DestroyUITowerManagerCombat();
        //_buildManager.TowerZoneExpSliderActive(false);
        lastTouchedTowerZone = null;
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            // TowerZone towerZone = collision.GetComponent<TowerZone>();
            // lastTouchedTowerZone = towerZone.gameObject;
            // //Vector3 instantiatePosition = _cam.WorldToScreenPoint(collision.transform.position);
            // //Vector3 instantiatePosition = _cam.WorldToScreenPoint(transform.position);
            // if(towerZone.isEmpty)
            // {
            //     _buildManager.DrawUITowerBuilderCombat();
            //     Debug.Log("On Tower Zone Empty");
            // }
            // else
            // {
            //     _buildManager.DrawUITowerManagerCombat();
            //     Debug.Log("On Tower Zone Full");
            // }
            BuildManagerState(collision, true);
            if (!_gameManager.onBuildMenu)
            {
                _buildManager.TowerZoneExpSliderActive(true);
            }
            
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Tower Zone"))
        {
            // TowerZone towerZone = collision.GetComponent<TowerZone>();
            // if(towerZone.isEmpty)
            // {
            //     _buildManager.DestroyUITowerBuilderCombat();
            //     lastTouchedTowerZone = null;
            //     Debug.Log("Left Empty Tower Zone");
            // }
            // else
            // {
            //     _buildManager.DestroyUITowerManagerCombat();
            //     lastTouchedTowerZone = null;
            //     Debug.Log("Left Full Tower Zone");
            // }
            BuildManagerState(collision, false);
            _buildManager.TowerZoneExpSliderActive(false);
        }
    }
    
    private bool IsMouseOverIgnoredUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, results);
        
        //Debug.Log("Raycast results count: " + results.Count);
        foreach (RaycastResult result in results)
        {
            //Debug.Log("Hit: " + result.gameObject.name + " | Tag: " + result.gameObject.tag);
        }
        
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("IgnoredUI"))
            {
                //Debug.Log("Ignored UI: " + result.gameObject.name);
                return true;
            }
        }

        //Debug.Log("No ignored UI detected.");
        return false;
    }

    public void SetMoveDirection(Vector2 direction)
    {
        _moveDirection = direction;
    }

    public void AnimatorToIdle()
    {
        _animator.SetFloat("moveX", 0);
        _animator.SetFloat("moveY", 0);
    }
}
