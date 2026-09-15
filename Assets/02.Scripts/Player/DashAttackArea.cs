using UnityEngine;

public class DashAttackArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // (기존 코드 유지) Actual Movement용 물리 충돌 공격
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamageFromPlayer();
        }
    }

    // Teleport용 수동 궤적 공격 (경로 상의 적들을 스캔)
    public void SweepAttack(Vector2 origin, Vector2 direction, float distance)
    {
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null) return;

        // 시작 위치부터 도착 위치까지 상자(Box) 형태로 검사하여 닿은 모든 것을 가져옵니다.
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, boxCol.size, 0f, direction, distance);

        foreach (RaycastHit2D hit in hits)
        {
            // 부딪힌 것들 중 'Enemy' 태그를 가진 적을 찾아 대미지를 줍니다.
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamageFromPlayer();
                }
            }
        }
    }
}