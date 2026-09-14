using System;
using System.Collections;
using UnityEngine;

public class DashSystem : MonoBehaviour
{
    public enum MovementType { ActualMovement, Teleport }
    public enum TeleportCollisionType { PushOut, Block }
    public enum DashState { Idle, Dashing, Cooldown }

    [Header("Dash Settings")]
    public float dashDistance = 5f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.6f;
    public LayerMask obstacleLayer; // 벽(Ground, Thin Wall, Thick Wall)을 인식할 레이어

    [Header("Current Options (Controlled by UI)")]
    public MovementType currentMovementType = MovementType.ActualMovement;
    public TeleportCollisionType currentTeleportCollision = TeleportCollisionType.PushOut;

    // 현재 상태 읽기 전용 속성 (디버그 및 다른 스크립트에서 참조)
    public DashState CurrentState { get; private set; } = DashState.Idle;

    // 피드백 시스템에 전달할 이벤트 (Action)
    public event Action OnDashStart;
    public event Action OnDashEnd;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private float originalGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();
        originalGravity = rb.gravityScale;
    }

    // PlayerController에서 방향(키보드 좌/우)을 담아 호출하는 함수
    public void ExecuteDash(Vector2 direction)
    {
        if (CurrentState != DashState.Idle) return; // 쿨타임 중이거나 이미 대시 중이면 무시

        StartCoroutine(DashRoutine(direction.normalized));
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        // 1. 대시 시작 상태 돌입
        CurrentState = DashState.Dashing;
        OnDashStart?.Invoke(); // 피드백 스크립트에 "대시 시작" 알림

        // 2. 방식에 따른 이동 처리
        if (currentMovementType == MovementType.ActualMovement)
        {
            yield return StartCoroutine(ActualMovementDash(direction));
        }
        else
        {
            TeleportDash(direction);
            // 순간이동은 시간이 0초 걸리지만, 
            // '대시 중 상태(무적 판정 등)' 유지를 위해 duration만큼 대기
            yield return new WaitForSeconds(dashDuration);
        }

        // 3. 대시 종료 및 쿨타임 돌입
        OnDashEnd?.Invoke(); // 피드백 스크립트에 "대시 종료" 알림
        CurrentState = DashState.Cooldown;

        yield return new WaitForSeconds(dashCooldown);

        // 4. 일반 상태로 복귀
        CurrentState = DashState.Idle;
    }

    // --- [실험 1] 실제 이동형 대시 로직 ---
    private IEnumerator ActualMovementDash(Vector2 direction)
    {
        rb.gravityScale = 0f; // 대시 중 공중에서 떨어지지 않게 중력 무시

        // 시간(Duration)과 거리(Distance)를 기반으로 속도(Velocity) 계산
        float dashSpeed = dashDistance / dashDuration;
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            rb.linearVelocity = direction * dashSpeed; // 강제 가속 이동
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 대시 종료 후 감속 및 물리 초기화
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
    }

    // --- [실험 3] 순간이동형 대시 및 충돌 처리 로직 ---
    private void TeleportDash(Vector2 direction)
    {
        // BoxCast를 사용하여 플레이어 형태 그대로 전방 충돌 검사
        Vector2 origin = rb.position;
        Vector2 size = boxCol.size;

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, dashDistance, obstacleLayer);

        if (hit.collider != null) // 벽에 부딪힘
        {
            if (currentTeleportCollision == TeleportCollisionType.Block)
            {
                // [Block 옵션]: 벽 안으로 갈 수 없으니 이동 자체를 제한 (제자리에 남음)
                return;
            }
            else if (currentTeleportCollision == TeleportCollisionType.PushOut)
            {
                // [Push Out 옵션]: 벽과 겹치지 않는 바로 앞 위치로 보정
                // 충돌 지점에서 플레이어 너비의 절반만큼 뒤로 물러난 위치 계산
                float safeDistance = hit.distance - 0.01f; // 약간의 여백(0.01) 부여하여 벽에 끼임 방지
                Vector2 safePosition = origin + (direction * safeDistance);
                rb.position = safePosition;
            }
        }
        else // 부딪힌 벽이 없음 (정상 이동)
        {
            rb.position = origin + (direction * dashDistance);
        }
    }
}