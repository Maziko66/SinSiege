using UnityEngine;

public class TowerChapelCollider : MonoBehaviour
{
    [SerializeField] private TowerChapel towerChapel;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            enemy.ReduceHealth(towerChapel.GetDamage());
            enemy.CheckHealth();
            Debug.Log("Tower Chapel Collided");
        }
    }
}
