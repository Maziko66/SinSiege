using System;
using System.Collections;
using UnityEngine;

public class CrossBulletImpact : MonoBehaviour
{
    private CircleCollider2D collider;
    
    [Header("References")]
    [SerializeField] private Transform maskObject; 

    [Header("Settings")]
    [Tooltip("Lower number = Faster")] 
    [SerializeField] private float smoothTime = 0.1f; 
    [SerializeField] private float maskSmoothTime = 0.2f;
    [SerializeField] private float startingScale = 0f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private float lifetime = 2f;

    public string targetTag = "Enemy";
    public float damage = 0;

    private void Start()
    {
        StartCoroutine(ImpactSequence());
    }
    
    IEnumerator ImpactSequence()
    {
        float tinyScale = 0.01f;
        
        transform.localScale = new Vector3(startingScale, startingScale, 1);
        maskObject.localScale = new Vector3(tinyScale, tinyScale, 1);
    
        Vector3 parentTarget = new Vector3(maxScale, maxScale, 1);
        Vector3 maskTarget = Vector3.one * 1.1f;
        Vector3 velocity = Vector3.zero;
        
        float startTime = Time.time;
        
        while (transform.localScale.x < maxScale - 0.05f)
        {
            transform.localScale = Vector3.SmoothDamp(transform.localScale, parentTarget, ref velocity, smoothTime);
            yield return null;
        }
        
        transform.localScale = parentTarget; 
        velocity = Vector3.zero; // Reset momentum
        
        while (maskObject.localScale.x < 1.0f)
        {
            maskObject.localScale = Vector3.SmoothDamp(maskObject.localScale, maskTarget, ref velocity, maskSmoothTime);
            
            if (Time.time - startTime > lifetime) break;
            
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (targetTag == "Enemy" || other.gameObject.CompareTag(targetTag) || other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            if (enemy)
            {
                if (SoundManager.Instance != null && gameObject.scene.isLoaded)
                {
                    //SoundManager.Instance.PlaySound(SoundManager.Instance.sfxCoinPickup, transform.position);
                    SoundManager.Instance.PlaySound(SoundManager.Instance.sfxEnemyDamage);
                }
                enemy.ReduceHealth(damage);
                enemy.CheckHealth();
                //Debug.Log("hit enemy");
            }
        }
    }
}