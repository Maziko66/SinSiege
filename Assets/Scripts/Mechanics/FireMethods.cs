using UnityEngine;

public static class FireMethods
{
    public enum TargetTag
    {
        Enemy,
        EnemyGround,
        EnemyAir
    }
    
    public static string GetTargetTagString(TargetTag targetTag)
    {
        return targetTag.ToString();
    }

    public enum FireMode
    {
        Homing,
        Direct
    }

    public static int GetFireMode(FireMode mode)
    {
        switch (mode)
        {
            case FireMode.Homing:
                return 0;
            case FireMode.Direct:
                return 1;
            default:
                return -1;
        }
    }
    
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
    /// <param name="firedFrom">Defines the firing mode (0 = homing, 1 = direct).</param>
    /// <param name="bulletPrefab">The bullet prefab to instantiate.</param>
    /// <param name="transform">The transform from which the bullet is fired.</param>
    /// <param name="bulletSpeed">Speed of the bullet.</param>
    /// <param name="attackDamage">Damage dealt by the bullet.</param>
    /// <param name="targetVector3">The target position in world space.</param>
    /// <param name="bulletHealth">Health of the bullet.</param>
    /// <param name="targetTag">Tag of object to seek.</param>
    /// <param name="towerGeneric">Source towergeneric. Default value "null".</param>
    /// <param name="target">The main target the bullet is directed at. Default value "null".</param>
    /// <param name="bulletCount">Number of bullets fired (used in spread mode). Default value "1".</param>
    /// <param name="spreadAngle">Angle spread between bullets (used in spread mode). Default value "0f".</param>
    /// <param name="isSpinning">Checks bullet's isSpinning bool. Default value "false".</param>
    /// <param name="startPosition">Start position of bullet (Vector2).</param>
    public static void BulletFire(int firedFrom, 
                                    Bullet bulletPrefab,
                                    Transform transform,
                                    float bulletSpeed,
                                    float attackDamage,
                                    Vector3 targetVector3,
                                    int bulletHealth,
                                    string targetTag,
                                    TowerGeneric towerGeneric = null,
                                    Transform target = null,
                                    int bulletCount = 1,
                                    float spreadAngle = 0f,
                                    bool isSpinning = false,
                                    Vector2? startPosition = null)
    {
        if (firedFrom == 0)
        {
            Bullet bullet = Object.Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            bullet.SetBulletStats(bulletSpeed, attackDamage, bulletHealth, transform.position, target.position, firedFrom, targetTag, 1f, isSpinning);
            bullet.SetTowerGeneric(towerGeneric);
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
                //bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1);
                bullet.SetBulletStats(bulletSpeed, attackDamage, bulletHealth, transform.position, direction, firedFrom, targetTag);
                bullet.SetTowerGeneric(towerGeneric);
                startAngle -= spreadAngle;
            }
        }
        else if (firedFrom == 2)
        {
            float startAngle = (bulletCount * spreadAngle) / 2 - 5;
        
            for (int i = 0; i < bulletCount; i++)
            {
                Vector2 pos = startPosition ?? Vector2.zero;
                Bullet bullet = Object.Instantiate(bulletPrefab, pos, Quaternion.identity);
                Vector2 direction = ((Vector2)targetVector3 - (Vector2)transform.position).normalized;
                direction = FireMethods.RotateVector2(direction, startAngle);
                //bullet.SetBulletStats(bulletSpeed, attackDamage, 1, transform.position, direction, 1);
                bullet.SetBulletStats(bulletSpeed, attackDamage, bulletHealth, transform.position, direction, firedFrom, targetTag);
                bullet.SetTowerGeneric(towerGeneric);
                startAngle -= spreadAngle;
            }
        }
    }
}