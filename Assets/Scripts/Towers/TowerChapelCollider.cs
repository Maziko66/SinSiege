using UnityEngine;

public class TowerChapelCollider : MonoBehaviour
{
    [SerializeField] private TowerChapel towerChapel;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.ReduceHealth(towerChapel.GetDamage());
            enemy.CheckHealth();
        }
    }
}
