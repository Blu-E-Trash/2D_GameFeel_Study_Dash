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

    [Tooltip("순간이동 충돌 검사용 상자를 이만큼 안쪽으로 줄입니다. 발밑 바닥(같은 Obstacle 레이어)이 0거리 충돌로 잡히는 것을 막습니다.")]
    public float collisionSkin = 0.05f;

    [Header("Current Options (Controlled by UI)")]
    public MovementType currentMovementType = MovementType.ActualMovement;
    public TeleportCollisionType currentTeleportCollision = TeleportCollisionType.PushOut;

    public DashState CurrentState { get; private set; } = DashState.Idle;

    public event Action OnDashStart;
    public event Action OnDashEnd;
    public event Action<Vector2, Vector2, float> OnTeleport;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private float originalGravity;

    private const float SafetyGap = 0.01f;      // 벽 앞에 남기는 여유 거리
    private const float PushOutSearchStep = 0.05f;

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

    private void TeleportDash(Vector2 direction)
    {
        Vector2 origin = rb.position;

        // 콜라이더의 실제 월드 크기·중심(오프셋, 스케일 반영)을 기준으로 검사하고,
        // 바닥에 닿지 않도록 skin만큼 줄인 상자를 사용한다.
        Bounds bounds = boxCol.bounds;
        Vector2 castOrigin = bounds.center;
        Vector2 castSize = (Vector2)bounds.size - Vector2.one * (collisionSkin * 2f);

        float actualDistance = dashDistance;

        if (currentTeleportCollision == TeleportCollisionType.Block)
        {
            // 경로 전체 검사: 장애물이 있으면 그 앞에서 멈춤
            if (TryGetBlockingDistance(castOrigin, castSize, direction, dashDistance, out float hitDistance))
                actualDistance = ToSafeDistance(hitDistance);
        }
        else // PushOut
        {
            // 도착 지점만 검사: 비어 있으면 중간의 얇은 벽은 통과
            Vector2 targetCenter = castOrigin + direction * dashDistance;
            if (Physics2D.OverlapBox(targetCenter, castSize, 0f, obstacleLayer) != null)
            {
                if (TryGetBlockingDistance(castOrigin, castSize, direction, dashDistance, out float hitDistance))
                    actualDistance = ToSafeDistance(hitDistance);
                else
                    actualDistance = FindFreeDistanceBackward(castOrigin, castSize, direction, dashDistance);
            }
        }

        rb.position = origin + direction * actualDistance;

        // 순간이동 직전의 이동 속도가 남아 대시 시간 동안 추가로 미끄러지지 않도록 수평 속도 제거
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // 텔레포트가 완료되면, 어디서, 어느 방향으로, 얼만큼 이동했는지 방송합니다.
        OnTeleport?.Invoke(origin, direction, actualDistance);
    }

    // 진행 방향을 마주 보는 장애물 면만 '막힘'으로 인정한다. (발밑 바닥처럼 옆으로 스치는 면은 무시)
    private bool TryGetBlockingDistance(Vector2 castOrigin, Vector2 castSize, Vector2 direction, float distance, out float hitDistance)
    {
        RaycastHit2D[] hits = Physics2D.BoxCastAll(castOrigin, castSize, 0f, direction, distance, obstacleLayer);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            if (Vector2.Dot(hit.normal, direction) > -0.5f) continue;

            hitDistance = hit.distance;
            return true;
        }

        hitDistance = 0f;
        return false;
    }

    // 검사 상자는 skin만큼 작으므로, 실제 콜라이더 앞면 기준으로 거리를 되돌린다.
    private float ToSafeDistance(float castHitDistance)
    {
        return Mathf.Max(0f, castHitDistance - collisionSkin - SafetyGap);
    }

    // 예비 처리: 도착 지점이 막혀 있는데 경로 검사에서 원인을 못 찾은 경우,
    // 도착 지점에서 출발점 쪽으로 조금씩 되돌아오며 처음으로 비어 있는 위치를 찾는다.
    private float FindFreeDistanceBackward(Vector2 castOrigin, Vector2 castSize, Vector2 direction, float distance)
    {
        for (float d = distance; d > 0f; d -= PushOutSearchStep)
        {
            if (Physics2D.OverlapBox(castOrigin + direction * d, castSize, 0f, obstacleLayer) == null)
                return d;
        }
        return 0f;
    }
}
