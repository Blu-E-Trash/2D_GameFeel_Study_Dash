using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum AttackType { Normal, Undodgeable }

    [Header("Enemy Attack Settings")]
    public float idleTime = 2f;          // 대기 시간
    public float warningTime = 0.5f;     // 1단계: 느낌표(!) 표시 시간
    public float blinkTime = 0.5f;       // 2단계: 범위 깜빡이는 총 시간
    public float blinkInterval = 0.1f;   // 범위 깜빡임 간격
    public float attackTime = 0.2f;      // 3단계: 실제 타격 판정 시간

    [Tooltip("Normal: 대시 무적으로 회피 가능 / Undodgeable: 회피 불가능")]
    public AttackType currentAttackType = AttackType.Normal;

    [Header("References")]
    public SpriteRenderer visualSprite;        // 적 본체
    public SpriteRenderer warningSprite;       // 느낌표(!) 표시용
    public SpriteRenderer attackRangeSprite;   // 타격 범위 깜빡임 표시용
    public Collider2D attackHitbox;            // 실제 타격 판정 트리거

    [Header("Feedback")]
    public Color hitFlashColor = Color.white;  // 피격 시 번쩍일 색상
    private Color originalVisualColor;

    private void Start()
    {
        if (visualSprite != null) originalVisualColor = visualSprite.color;

        // 시작 시 전조와 판정 끄기
        if (warningSprite != null) warningSprite.enabled = false;
        if (attackRangeSprite != null) attackRangeSprite.enabled = false;
        if (attackHitbox != null) attackHitbox.enabled = false;

        StartCoroutine(AttackPatternRoutine());
    }

    private IEnumerator AttackPatternRoutine()
    {
        while (true)
        {
            // 1. Idle (대기)
            yield return new WaitForSeconds(idleTime);

            // 2. Warning (느낌표)
            if (warningSprite != null)
            {
                warningSprite.enabled = true;
                yield return new WaitForSeconds(warningTime);
                warningSprite.enabled = false;
            }

            // 3. Telegraph Blinking (범위 깜빡임)
            if (attackRangeSprite != null)
            {
                float timer = 0f;
                bool isVisible = true;

                while (timer < blinkTime)
                {
                    attackRangeSprite.enabled = isVisible;
                    isVisible = !isVisible;

                    yield return new WaitForSeconds(blinkInterval);
                    timer += blinkInterval;
                }

                attackRangeSprite.enabled = false;
            }

            // 4. Attack (타격 판정)
            if (attackHitbox != null) attackHitbox.enabled = true;

            yield return new WaitForSeconds(attackTime);

            // 5. Attack End
            if (attackHitbox != null) attackHitbox.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                bool isUndodgeable = (currentAttackType == AttackType.Undodgeable);
                player.TakeDamage(isUndodgeable);
            }
        }
    }

    public void TakeDamageFromPlayer()
    {
        Debug.Log("적: 플레이어의 대시 공격에 피격당했습니다!");
        if (visualSprite != null)
        {
            StartCoroutine(HitFlashRoutine());
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        visualSprite.color = hitFlashColor;
        yield return new WaitForSeconds(0.1f);
        visualSprite.color = originalVisualColor;
    }
}