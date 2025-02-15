using UnityEngine;

public static class FireMethods
{
    public static Vector2 RotateVector2(Vector2 vector, float angle)
    {
        float radianAngle = Mathf.Deg2Rad * angle;
        float cosine = Mathf.Cos(radianAngle);
        float sine = Mathf.Sin(radianAngle);
        
        return new Vector2(
            cosine * vector.x - sine * vector.y,
            sine * vector.x + cosine * vector.y
        );
    }
    
    /// <summary>
    /// Fires bullets from a given position towards a target.
    /// </summary>
    /// <param name="firedFrom">Defines the firing mode (0 = direct, 1 = spread).</param>
    /// <param name="bulletPrefab">The bullet prefab to instantiate.</param>
    /// <param name="transform">The transform from which the bullet is fired.</param>
    /// <param name="bulletSpeed">Speed of the bullet.</param>
    /// <param name="attackDamage">Damage dealt by the bullet.</param>
    /// <param name="target">The main target the bullet is directed at.</param>
    /// <param name="targetVector3">The target position in world space.</param>
    /// <param name="bulletCount">Number of bullets fired (used in spread mode).</param>
    /// <param name="spreadAngle">Angle spread between bullets (used in spread mode).</param>
    public static void BulletFire(int firedFrom, Bullet bulletPrefab, Transform transform, float bulletSpeed, float attackDamage,
                                  Vector3 targetVector3, Transform target = null, int bulletCount = 1, float spreadAngle = 0f)
    {
        if (firedFrom == 0)
        {
            Bullet bullet = Object.Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, target.position, 0);
            bullet.SetTarget(target);
        }
        else if (firedFrom == 1)
        {
            float startAngle = (bulletCount * spreadAngle) / 2 - 5;
        
            for (int i = 0; i < bulletCount; i++)
            {
                Bullet bullet = Object.Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                Vector2 direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
                direction = FireMethods.RotateVector2(direction, startAngle);
                bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1);
                startAngle -= spreadAngle;
            }
        }
    }
}
