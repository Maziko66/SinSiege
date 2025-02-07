using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource sfxFire;
    [SerializeField] private AudioSource sfxReload;
    [SerializeField] private float sfxReloadDelay = 0.3f;
    [SerializeField] private Bullet bullet;
    
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private float spreadAngle = 10f;
    [SerializeField] private float bulletMass = 1f;
    
    private float _cooldown;
    private bool _reloaded;

    private void Update()
    {
        if (_cooldown >= 0f)
        {
            _cooldown -= Time.deltaTime;
            
        }
        if (!_reloaded && _cooldown <= sfxReloadDelay)
        {
            sfxReload.Play();
            _reloaded = true;
        }
        
        
    }

    public void Fire(Vector3 targetVector3)
    {
        if (_cooldown <= 0)
        {
            Bullet bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            Vector2 direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
            bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
            
            bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
            direction = RotateVector2(direction, spreadAngle);
            bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
            
            bullet = Instantiate(this.bullet, transform.position, Quaternion.identity);
            direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
            direction = RotateVector2(direction, -spreadAngle);
            bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1, bulletMass);
            
            sfxFire.Play();
            
            _cooldown = attackInterval;
            _reloaded = false;
        }
        else
        {
            Debug.Log("Shotgun is on cooldown: " + _cooldown);
        }
    }
    
    private Vector2 RotateVector2(Vector2 vector, float angle)
    {
        float radianAngle = Mathf.Deg2Rad * angle; // Convert angle to radians
        float cosine = Mathf.Cos(radianAngle);
        float sine = Mathf.Sin(radianAngle);
    
        // Apply the rotation matrix to the vector
        return new Vector2(
            cosine * vector.x - sine * vector.y,
            sine * vector.x + cosine * vector.y
        );
    }
}
