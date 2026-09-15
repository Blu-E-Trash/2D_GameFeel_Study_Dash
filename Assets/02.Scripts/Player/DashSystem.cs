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
    public event Action<Vector2, Vector2, float> OnTeleport;

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

    private void TeleportDash(Vector2 direction)
    {
        Vector2 origin = rb.position;
        Vector2 size = boxCol.size;
        Vector2 targetPos = origin + (direction * dashDistance);

        float actualDistance = dashDistance; // 실제 이동할 거리 저장용 변수 추가

        if (currentTeleportCollision == TeleportCollisionType.Block)
        {
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, dashDistance, obstacleLayer);
            if (hit.collider != null)
            {
                float safeDistance = Mathf.Max(0f, hit.distance - 0.01f);
                rb.position = origin + (direction * safeDistance);
                actualDistance = safeDistance; // 벽에 막혔으니 거리를 갱신
            }
            else rb.position = targetPos;
        }
        else if (currentTeleportCollision == TeleportCollisionType.PushOut)
        {
            Vector2 checkSize = size * 0.95f;
            Collider2D overlapCol = Physics2D.OverlapBox(targetPos, checkSize, 0f, obstacleLayer);
            if (overlapCol != null)
            {
                RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, direction, dashDistance, obstacleLayer);
                if (hit.collider != null)
                {
                    float safeDistance = Mathf.Max(0f, hit.distance - 0.01f);
                    rb.position = origin + (direction * safeDistance);
                    actualDistance = safeDistance; // 벽 바깥으로 밀려났으니 거리를 갱신
                }
            }
            else rb.position = targetPos;
        }

        // --- 함수의 맨 마지막에 아래 이벤트 실행 코드 추가 ---
        // 텔레포트가 완료되면, 어디서, 어느 방향으로, 얼만큼 이동했는지 방송합니다.
        OnTeleport?.Invoke(origin, direction, actualDistance);
    }
}