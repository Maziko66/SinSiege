using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private GameManager _gameManager;
    private BuildManager _buildManager;
    private MouseManager _mouseManager;
    private Rigidbody2D _rb;
    private Camera _cam;
    private CinemachineBrain _brain;
    
    [SerializeField] private GameObject _buildCameraObject;
    private Rigidbody2D _buildCameraObjectRb;
    
    [Header("Util")]
    public InputAction playerControls;
    public InputAction playerSprint;
    public InputAction playerScroll;
    
    [SerializeField] private PlayerCollectionAoe _playerCollectionAoe;

    [SerializeField] private float cameraZoomAmount = 2f;
    [SerializeField] private float cameraZoomCapMin = 6f, cameraZoomCapMax = 20f;
    [SerializeField] private float cameraBuildZoomCapMin = 10f, cameraBuildZoomCapMax = 32f;
    
    
    public GraphicRaycaster raycaster;
    [SerializeField] private float buildCameraSpeed = 15f;
    private int _layerMaskTowerZone;
    private int _layerMaskItem;
    
    
    [SerializeField] private bool _isSprinting;
    [SerializeField] private bool _isWalking;
    
    [Header("Equipment")]
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Vector3 shotgunPositionRight;
    [SerializeField] private Vector3 shotgunPositionLeft;
    [SerializeField] private Vector3 shotgunPositionUp;
    [SerializeField] private Vector3 shotgunPositionDown;
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
    
    private bool _shouldInvalidateConfiner = false;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        
        _gameManager = FindFirstObjectByType<GameManager>();
        _buildManager = FindFirstObjectByType<BuildManager>();
        _cam = FindFirstObjectByType<Camera>();
        _mouseManager = FindFirstObjectByType<MouseManager>();
        
        _brain = _cam.gameObject.GetComponent<CinemachineBrain>();

        _buildCameraObjectRb = _buildCameraObject.GetComponent<Rigidbody2D>();
        
        _layerMaskTowerZone = 1 << LayerMask.NameToLayer("TowerZone");
        _layerMaskItem = 1 << LayerMask.NameToLayer("Item");
    }

    private void OnEnable()
    {
        playerControls.Enable();
        playerSprint.Enable();
        playerScroll.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
        playerSprint.Disable();
        playerScroll.Disable();
    }

    private void Update()
    {
        if(isPaused) {return;}
        
        _moveDirection = playerControls.ReadValue<Vector2>();

        if (_moveDirection != Vector2.zero)
        {
            _isWalking = true;
        }
        else
        {
            _isWalking = false;
        }
        
        _isSprinting = playerSprint.IsPressed();

        if (playerScroll.ReadValue<float>() != 0)
        {
            //Debug.Log("scroll in update");
            CameraZoom(playerScroll.ReadValue<float>());
        }
        
        if(_gameManager.onBuildMenu) {return;}
        
        //_animator.SetFloat("moveX", _moveDirection.x);
        //_animator.SetFloat("moveY", _moveDirection.y);
        
        _animator.SetFloat("moveX", _mouseManager.MouseDirectionX());
        _animator.SetFloat("moveY", _mouseManager.MouseDirectionY());
        
        _animator.SetBool("isWalking", _isWalking);
        _animator.SetBool("isSprinting", _isSprinting);

    }
    private void LateUpdate()
    {
        if (_shouldInvalidateConfiner)
        {
            CinemachineCamera activeCam = GetActiveCinemachineCamera();
            if (activeCam != null)
            {
                CinemachineConfiner2D confiner = activeCam.GetComponent<CinemachineConfiner2D>();
                if (confiner != null)
                {
                    confiner.InvalidateCache();
                }
            }

            _shouldInvalidateConfiner = false;
        }
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
        if(isPaused || IsMouseOverIgnoredUI() || _isSprinting) {return;}

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
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        
        RaycastHit2D hitItem = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, _layerMaskItem);
        //Debug.Log(hitItem.collider);
        
        if (hitItem.collider != null && hitItem.collider.CompareTag("Clickable"))
        {
            Debug.Log("clickable");
            Clickable clickable = hitItem.collider.gameObject.GetComponent<Clickable>();
            clickable.HandleClick();
            return;
        }
        
        
        
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
    
    #region UTIL

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
                //Debug.Log("On Tower Zone Empty");
            }
            else
            {
                _buildManager.DrawUITowerManagerCombat(calledFromBuildMenu);
                //Debug.Log("On Tower Zone Full");
            }
        }
        else
        {
            TowerZone towerZone = col.GetComponent<TowerZone>();
            if(towerZone.isEmpty)
            {
                _buildManager.DestroyUITowerBuilderCombat();
                lastTouchedTowerZone = null;
                //Debug.Log("Left Empty Tower Zone");
            }
            else
            {
                _buildManager.DestroyUITowerManagerCombat();
                lastTouchedTowerZone = null;
                //Debug.Log("Left Full Tower Zone");
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

    private void CameraZoom(float input)
    {
        CinemachineCamera activeCam = GetActiveCinemachineCamera();
        if (activeCam != null)
        {
            if (_gameManager.onBuildMenu)
            {
                float newValueBuild = Mathf.Clamp(activeCam.Lens.OrthographicSize - (input * cameraZoomAmount), cameraBuildZoomCapMin, cameraBuildZoomCapMax);
                activeCam.Lens.OrthographicSize = newValueBuild;
            }
            else
            {
                float newValue = Mathf.Clamp(activeCam.Lens.OrthographicSize - (input * cameraZoomAmount), cameraZoomCapMin, cameraZoomCapMax);
                activeCam.Lens.OrthographicSize = newValue;
            }

            _shouldInvalidateConfiner = true;
        }
    }
    
    private CinemachineCamera GetActiveCinemachineCamera()
    {
        if (_brain != null && _brain.ActiveVirtualCamera is CinemachineCamera vCam)
        {
            //Debug.Log("returning vcam");
            return vCam;
        }
        //Debug.Log("returning null, " + _brain + ", " + _brain.ActiveVirtualCamera);
        return null;
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
    
    #endregion
    
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