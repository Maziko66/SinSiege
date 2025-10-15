using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Arm Stuff")]
    [SerializeField] private List<Transform> attackPoints;
    [SerializeField] private List<Animator> armAnimators;
    [SerializeField] private float cooldownArmAttack = 2f;
    private float _timerArmAttack;
    [SerializeField] private int armIndex;
    [SerializeField] private int armSingleAttackMax = 5;
    [SerializeField] private int armAttackCounter;
    [SerializeField] private bool canDoubleArmAttack;

    private bool initSingleAttack;
    private bool initDoubleAttack;
    
    [SerializeField] private Bullet bullet;
    private Material _matFlashDamaged;
    [SerializeField] private Material _matOriginal;
    
    private Rigidbody2D rb;
    private Animator _animator;
    private Canvas _canvas;
    private GameManager _gameManager;
    private WaveManager _waveManager;
    private Player _player;
    
    
    [SerializeField] private float moveSpeed;
    [SerializeField] private float health = 15f;
    [SerializeField] private int attackDamage;
    [SerializeField] private float exp = 1;

    

    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletDamage;
    [SerializeField] private int bulletCount;
    [SerializeField] private float spreadAngle;

    [SerializeField] private List<GameObject> limbs;
    
    private List<SpriteRenderer> _childSpriteRenderers = new List<SpriteRenderer>();
    
    public bool followPlayer;
    
    private Vector2 _target;
    
    static readonly int HashArmAttack = Animator.StringToHash("MammonAttack");
    static readonly int HashArmsIdle   = Animator.StringToHash("MammonArmsIdle");
    
    private void Awake()
    {
        foreach (GameObject limb in limbs)
        {
            _childSpriteRenderers.Add(limb.GetComponent<SpriteRenderer>());
        }
        
        _matFlashDamaged = Resources.Load<Material>("Materials/FlashDamaged");
        
        _canvas = FindFirstObjectByType<Canvas>();
        _gameManager = FindFirstObjectByType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        
    }
    
    private void Start()
    {
        _player = _gameManager.GetPlayer();

        _timerArmAttack = cooldownArmAttack;
    }
    
    private void Update()
    {
        if (followPlayer)
        {
            if(_player == null) { return; }
            _target = _player.transform.position;
        }

        // if (!canDoubleArmAttack)
        // {
        //     _timerArmAttack -= Time.deltaTime;    
        // }
        
        // _timerArmAttack -= Time.deltaTime; 

        // if (_timerArmAttack <= 0)
        // {
        //     return;
        // }
        
        if (canDoubleArmAttack)
        {
            if (initDoubleAttack)
            {
                return;
            }
            StartCoroutine(DoubleAttack());
        }
        else
        {
            if (initSingleAttack)
            {
                return;
            }
            StartCoroutine(SingleAttack());
        }
    }
    
    private void LateUpdate()
    {
         // _spriteRenderer.sortingOrder = Mathf.RoundToInt(-_spriteRenderer.gameObject.transform.position.y * 100); // FIX HERE
         
         // if (_isDamaged && sliderHealth)
         // {
         //     Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + sliderOffset);
         //    
         //     sliderHealth.transform.position = screenPosition;
         // }
    }
    
    private void FixedUpdate()
    {
        Movement();
    }
    
    private void Movement()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, _target, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }
    
    public void ReduceHealth(float damage)
    {
        // if (damage > 0 && !_isDamaged)
        // {
        //     _isDamaged = true;
        //     sliderHealth = Instantiate(prefabSliderHealth.gameObject, _canvas.transform).GetComponent<Slider>();
        //     sliderHealth.maxValue = _health;
        //     //Debug.Log("slider instantiated");
        // }
        
        StartCoroutine(FlashWhite());
        health -= damage;
        //Debug.Log("-damage");
        //sliderHealth.value = _health;
    }
    
    IEnumerator FlashWhite()
    {
        foreach (SpriteRenderer childSpriteRenderer in _childSpriteRenderers)
        {
            //childSpriteRenderer.material = _matFlashDamaged;
            childSpriteRenderer.color = Color.red;
        }
        
        yield return new WaitForSeconds(0.12f);
        
        foreach (SpriteRenderer childSpriteRenderer in _childSpriteRenderers)
        {
            // childSpriteRenderer.material = _matOriginal;
            childSpriteRenderer.color = Color.white;
        }
    }

    private void PlayAttackAnimation(int arm = 0)
    {
        armAnimators[arm].Play("MammonAttack");
    }

    public void ArmHit(int armIndex = 0)
    {
        Vector2 pos = attackPoints[armIndex].position;
        FireMethods.BulletFire(2, bullet, transform, bulletSpeed, bulletDamage, /*targetVector3*/ pos, 1, "Player", null, null, bulletCount, spreadAngle, false, pos);
        SoundManager.Instance.PlaySound(SoundManager.Instance.sfxMammonArmHit);
    }

    
    private IEnumerator DoubleAttack()
    {
        initDoubleAttack = true;
        
        yield return new WaitForSeconds(cooldownArmAttack);
        
        var a0 = armAnimators[0];
        var a1 = armAnimators[1];
        
        for (int i = 0; i < 4; i++)
        {
            a0.Play(HashArmAttack, 0, 0f);
            a1.Play(HashArmAttack, 0, 0f);
            yield return new WaitForSeconds(a0.GetCurrentAnimatorStateInfo(0).length);
            
            // yield return new WaitUntil(() =>
            //     a0.GetCurrentAnimatorStateInfo(0).shortNameHash == HashAttack &&
            //     a1.GetCurrentAnimatorStateInfo(0).shortNameHash == HashAttack &&
            //     a0.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f &&
            //     a1.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
        }

        canDoubleArmAttack = false;
        
        a0.Play(HashArmsIdle, 0, 0f);
        a1.Play(HashArmsIdle, 0, 0f);

        initDoubleAttack = false;
    }
    
    private IEnumerator SingleAttack()
    {
        initSingleAttack = true;
        
        yield return new WaitForSeconds(cooldownArmAttack);
        
        for (int i = 0; i < 5; i++)
        {
            //Debug.Log("i: " + i + ". Arm index: " + armIndex + ". Playing Single Attack Anim.");
            
            var anim = armAnimators[armIndex];
            
            anim.Play(HashArmAttack,0 , 0f);
            
            // yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            
            // wait for the attack to complete exactly once
            yield return new WaitUntil(() =>
                anim.GetCurrentAnimatorStateInfo(0).shortNameHash == HashArmAttack &&
                anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
            
            //Debug.Log("Waited for seconds Single Attack, going idle");
            
            anim.Play(HashArmsIdle, 0, 0f);
            SwapCurrentArm();
        }

        canDoubleArmAttack = true;
        initSingleAttack = false;
    }

    private void SwapCurrentArm()
    {
        armIndex ^= 1;
    }
}

