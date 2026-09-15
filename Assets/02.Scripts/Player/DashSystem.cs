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
    public LayerMask obstacleLayer;

    [Header("Current Options (Controlled by UI)")]
    public MovementType currentMovementType = MovementType.ActualMovement;
    public TeleportCollisionType currentTeleportCollision = TeleportCollisionType.PushOut;

    public DashState CurrentState { get; private set; } = DashState.Idle;

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

    public void ExecuteDash(Vector2 direction)
    {
        if (CurrentState != DashState.Idle) return;

        StartCoroutine(DashRoutine(direction.normalized));
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        CurrentState = DashState.Dashing;
        OnDashStart?.Invoke();

        if (currentMovementType == MovementType.ActualMovement)
        {
            yield return StartCoroutine(ActualMovementDash(direction));
        }
        else
        {
            TeleportDash(direction);
            yield return new WaitForSeconds(dashDuration);
        }

        OnDashEnd?.Invoke();
        CurrentState = DashState.Cooldown;

        yield return new WaitForSeconds(dashCooldown);

        CurrentState = DashState.Idle;
    }

    private IEnumerator ActualMovementDash(Vector2 direction)
    {
        rb.gravityScale = 0f;
        float dashSpeed = dashDistance / dashDuration;
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            rb.linearVelocity = direction * dashSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
    }

    // --- 수정된 순간이동 및 충돌 처리 로직 ---
    private void TeleportDash(Vector2 direction)
    {
        Vector2 origin = rb.position;
        Vector2 size = boxCol.size;

        // 대시가 끝났을 때 플레이어가 서있을 '목표 위치'
        Vector2 targetPos = origin + (direction * dashDistance);

        if (currentTeleportCollision == TeleportCollisionType.Block)
        {
            // [Block]: 경로 전체를 검사. 얇은 벽이든 두꺼운 벽이든 무조건 부딪힌 벽 바로 앞에 멈춤.
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, dashDistance, obstacleLayer);

            if (hit.collider != null)
            {
                float safeDistance = Mathf.Max(0f, hit.distance - 0.01f); // 벽 앞 위치 계산
                rb.position = origin + (direction * safeDistance);
            }
            else
            {
                rb.position = targetPos; // 부딪힌 게 없으면 정상 이동
            }
        }
        else if (currentTeleportCollision == TeleportCollisionType.PushOut)
        {
            // [Push Out]: 목표 위치에 벽이 있는지만 검사. 약간 작게 검사하여 테두리 끼임 방지
            Vector2 checkSize = size * 0.95f;
            Collider2D overlapCol = Physics2D.OverlapBox(targetPos, checkSize, 0f, obstacleLayer);

            if (overlapCol != null)
            {
                // 목표 위치가 두꺼운 벽 안쪽이라면, 진입한 벽면 바로 앞으로 밀어냄(Push Out)
                RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, dashDistance, obstacleLayer);
                if (hit.collider != null)
                {
                    float safeDistance = Mathf.Max(0f, hit.distance - 0.01f);
                    rb.position = origin + (direction * safeDistance);
                }
            }
            else
            {
                // 목표 위치가 비어있다면 그대로 텔레포트 (경로상의 얇은 벽 관통 성공!)
                rb.position = targetPos;
            }
        }
    }
}