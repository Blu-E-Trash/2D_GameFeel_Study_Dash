using UnityEngine;

public class EnemyAttackArea : MonoBehaviour
{
    // 부모에 있는 적 컨트롤러 (공격 타입이 Normal인지 Undodgeable인지 알기 위해)
    public EnemyController enemyController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이 공격 범위(Hitbox)에 플레이어가 닿았을 때만 대미지를 줌
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && enemyController != null)
            {
                bool isUndodgeable = (enemyController.currentAttackType == EnemyController.AttackType.Undodgeable);
                player.TakeDamage(isUndodgeable);
            }
        }
    }
}