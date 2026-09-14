using UnityEngine;

[RequireComponent(typeof(DashSystem))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Gameplay Options (Controlled by UI)")]
    public bool useDashInvincibility = true;
    public bool useAttackDuringDash = false;

    [Header("References")]
    public Collider2D dashAttackArea; // 대시 중 적을 공격할 트리거 영역 (자식 오브젝트)

    private Rigidbody2D rb;
    private DashSystem dashSystem;
    private SpriteRenderer spriteRenderer;

    // 현재 플레이어가 무적 상태인지 (대시 중에만 true가 될 수 있음)
    public bool IsCurrentlyInvincible { get; private set; } = false;

    // 플레이어가 바라보는 방향 (1: 오른쪽, -1: 왼쪽)
    private float facingDirection = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dashSystem = GetComponent<DashSystem>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 대시 중 공격 영역은 기본적으로 꺼둡니다.
        if (dashAttackArea != null)
            dashAttackArea.enabled = false;
    }

    private void OnEnable()
    {
        // DashSystem의 이벤트에 상태 변화 함수들을 연결(구독)합니다.
        dashSystem.OnDashStart += HandleDashStart;
        dashSystem.OnDashEnd += HandleDashEnd;
    }

    private void OnDisable()
    {
        dashSystem.OnDashStart -= HandleDashStart;
        dashSystem.OnDashEnd -= HandleDashEnd;
    }

    private void Update()
    {
        // 대시 중이 아닐 때만 일반 이동 및 대시 입력 처리
        if (dashSystem.CurrentState == DashSystem.DashState.Idle || dashSystem.CurrentState == DashSystem.DashState.Cooldown)
        {
            HandleMovementInput();
            HandleDashInput();
        }
    }

    private void HandleMovementInput()
    {
        float moveInput = Input.GetAxisRaw("Horizontal"); // A/D 또는 좌/우 화살표 (-1, 0, 1)

        // 이동 처리 (velocity의 y값은 유지하여 중력 적용)
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 바라보는 방향 갱신 및 캐릭터 스프라이트 뒤집기 (Flip)
        if (moveInput != 0)
        {
            facingDirection = Mathf.Sign(moveInput);
            if (spriteRenderer != null)
                spriteRenderer.flipX = (facingDirection < 0);
        }
    }

    private void HandleDashInput()
    {
        // Left Shift 키를 누르면 대시 발동
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            // 현재 바라보는 방향으로 대시 실행
            Vector2 dashDir = new Vector2(facingDirection, 0f);
            dashSystem.ExecuteDash(dashDir);
        }
    }

    // --- 대시 이벤트 처리 (무적 및 공격 옵션 적용) ---
    private void HandleDashStart()
    {
        // [실험 4] 대시 무적 옵션이 켜져있다면 무적 상태로 돌입
        if (useDashInvincibility)
        {
            IsCurrentlyInvincible = true;
        }

        // [실험 5] 대시 중 공격 옵션이 켜져있다면 공격 판정(Collider) 활성화
        if (useAttackDuringDash && dashAttackArea != null)
        {
            dashAttackArea.enabled = true;
        }
    }

    private void HandleDashEnd()
    {
        // 대시가 끝나면 무조건 무적 및 공격 판정 해제
        IsCurrentlyInvincible = false;

        if (dashAttackArea != null)
        {
            dashAttackArea.enabled = false;
        }
    }

    // --- 적(Enemy)으로부터 공격을 받을 때 호출되는 함수 ---
    public void TakeDamage(bool isUndodgeableAttack = false)
    {
        // 회피 불가능한 공격이 아니면서, 현재 무적 상태라면 데미지 무시
        if (!isUndodgeableAttack && IsCurrentlyInvincible)
        {
            Debug.Log("플레이어: 대시 무적으로 공격을 회피했습니다!");
            return;
        }

        Debug.Log("플레이어: 피격당했습니다! (대시 중이 아니거나 무적 옵션이 꺼져있음)");
        // 실제 게임이라면 여기서 체력 감소, 피격 이펙트 등이 실행됩니다.
    }
}