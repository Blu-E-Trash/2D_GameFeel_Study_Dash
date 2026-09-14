using UnityEngine;

public class DashAttackArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 닿은 오브젝트가 적(Enemy)인지 확인
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamageFromPlayer();
            }
        }
    }
}