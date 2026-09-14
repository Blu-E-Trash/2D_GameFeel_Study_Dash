using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum AttackType { Normal, Undodgeable }

    [Header("Enemy Attack Settings")]
    public float idleTime = 2f;        // 대기 시간
    public float telegraphTime = 0.5f; // 공격 전조 시간
    public float attackTime = 0.2f;    // 실제 타격 판정이 켜져있는 시간

    [Tooltip("Normal: 대시 무적으로 회피 가능 / Undodgeable: 회피 불가능")]
    public AttackType currentAttackType = AttackType.Normal;

    [Header("References")]
    public SpriteRenderer visualSprite;    // 적의 기본 외형
    public SpriteRenderer telegraphSprite; // 전조 범위를 표시할 반투명 스프라이트
    public Collider2D attackHitbox;        // 실제 타격 판정 트리거 (자식 오브젝트)

    [Header("Colors (For Visual Feedback)")]
    public Color normalTelegraphColor = new Color(1f, 0.5f, 0f, 0.5f); // 주황색
    public Color undodgeableTelegraphColor = new Color(0.5f, 0f, 1f, 0.5f); // 보라색
    public Color hitFlashColor = Color.white; // 피격 시 번쩍일 색상

    private Color originalVisualColor;

    private void Start()
    {
        if (visualSprite != null) originalVisualColor = visualSprite.color;

        // 시작 시 전조와 타격 판정은 꺼둡니다.
        if (telegraphSprite != null) telegraphSprite.enabled = false;
        if (attackHitbox != null) attackHitbox.enabled = false;

        StartCoroutine(AttackPatternRoutine());
    }

    // --- 기획 8번: 대기 -> 전조 -> 공격 패턴 ---
    private IEnumerator AttackPatternRoutine()
    {
        while (true)
        {
            // 1. Idle (대기)
            yield return new WaitForSeconds(idleTime);

            // 2. Telegraph (공격 전조 표시)
            if (telegraphSprite != null)
            {
                // 공격 타입에 따라 전조 색상을 다르게 표시합니다.
                telegraphSprite.color = (currentAttackType == AttackType.Normal) ? normalTelegraphColor : undodgeableTelegraphColor;
                telegraphSprite.enabled = true;
            }
            yield return new WaitForSeconds(telegraphTime);

            // 3. Attack (실제 공격 판정 활성화)
            if (telegraphSprite != null) telegraphSprite.enabled = false;
            if (attackHitbox != null) attackHitbox.enabled = true;

            yield return new WaitForSeconds(attackTime);

            // 4. Attack End (공격 판정 비활성화 및 다시 대기로)
            if (attackHitbox != null) attackHitbox.enabled = false;
        }
    }

    // --- 적의 공격이 플레이어에게 닿았을 때 ---
    // 자식 오브젝트인 attackHitbox(Trigger)가 플레이어와 겹치면 이 함수가 호출됩니다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                bool isUndodgeable = (currentAttackType == AttackType.Undodgeable);
                player.TakeDamage(isUndodgeable); // 플레이어의 피격 함수 호출
            }
        }
    }

    // --- 플레이어의 대시 공격(Attack During Dash)에 적이 맞았을 때 ---
    public void TakeDamageFromPlayer()
    {
        Debug.Log("적: 플레이어의 대시 공격에 피격당했습니다!");
        if (visualSprite != null)
        {
            StartCoroutine(HitFlashRoutine());
        }
    }

    // 피격 시 하얗게 번쩍이는 시각적 피드백
    private IEnumerator HitFlashRoutine()
    {
        visualSprite.color = hitFlashColor;
        yield return new WaitForSeconds(0.1f);
        visualSprite.color = originalVisualColor;
    }
}