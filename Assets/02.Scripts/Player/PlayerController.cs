using System.Collections;
using TMPro; // UI 텍스트 사용을 위해 추가
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
    public Collider2D dashAttackArea;

    [Header("Dodge Result UI")]
    public GameObject dodgeUIPanel;      // 만들어둔 판넬 오브젝트 연결
    public TextMeshProUGUI dodgeText;    // 판넬 하위의 텍스트 오브젝트 연결

    private Rigidbody2D rb;
    private DashSystem dashSystem;
    private SpriteRenderer spriteRenderer;

    public bool IsCurrentlyInvincible { get; private set; } = false;
    private float facingDirection = 1f;
    private Coroutine dodgeUICoroutine; // UI 켜짐/꺼짐 코루틴 관리용

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dashSystem = GetComponent<DashSystem>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (dashAttackArea != null) dashAttackArea.enabled = false;

        // 시작할 때 UI 판넬을 확실히 꺼둡니다.
        if (dodgeUIPanel != null) dodgeUIPanel.SetActive(false);
    }

    private void OnEnable()
    {
        dashSystem.OnDashStart += HandleDashStart;
        dashSystem.OnDashEnd += HandleDashEnd;
        dashSystem.OnTeleport += HandleTeleport;
    }

    private void OnDisable()
    {
        dashSystem.OnDashStart -= HandleDashStart;
        dashSystem.OnDashEnd -= HandleDashEnd;
        dashSystem.OnTeleport -= HandleTeleport;
    }

    private void Update()
    {
        if (dashSystem.CurrentState == DashSystem.DashState.Idle || dashSystem.CurrentState == DashSystem.DashState.Cooldown)
        {
            HandleMovementInput();
            HandleDashInput();
        }
    }

    private void HandleMovementInput()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (moveInput != 0)
        {
            facingDirection = Mathf.Sign(moveInput);
            if (spriteRenderer != null) spriteRenderer.flipX = (facingDirection < 0);
        }
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Vector2 dashDir = new Vector2(facingDirection, 0f);
            dashSystem.ExecuteDash(dashDir);
        }
    }

    private void HandleDashStart()
    {
        if (useDashInvincibility) IsCurrentlyInvincible = true;
        if (useAttackDuringDash && dashAttackArea != null) dashAttackArea.enabled = true;
    }

    private void HandleDashEnd()
    {
        IsCurrentlyInvincible = false;
        if (dashAttackArea != null) dashAttackArea.enabled = false;
    }
    private void HandleTeleport(Vector2 origin, Vector2 dir, float dist)
    {
        // 대시 중 공격 옵션이 켜져 있을 때만 실행
        if (useAttackDuringDash && dashAttackArea != null)
        {
            DashAttackArea attackArea = dashAttackArea.GetComponent<DashAttackArea>();
            if (attackArea != null)
            {
                // DashAttackArea에게 지나온 궤적을 휩쓸어서 공격하라고 명령!
                attackArea.SweepAttack(origin, dir, dist);
            }
        }
    }

    // --- 수정된 피격 및 회피 판정 로직 ---
    public void TakeDamage(bool isUndodgeableAttack = false)
    {
        if (!isUndodgeableAttack && IsCurrentlyInvincible)
        {
            Debug.Log("회피 성공!");
            ShowDodgeResult(true);
            return;
        }

        Debug.Log("피격 (회피 실패)");
        ShowDodgeResult(false);
    }

    private void ShowDodgeResult(bool isSuccess)
    {
        if (dodgeUIPanel == null || dodgeText == null) return;

        if (dodgeUICoroutine != null) StopCoroutine(dodgeUICoroutine);
        dodgeUICoroutine = StartCoroutine(DodgeUIRoutine(isSuccess));
    }

    private IEnumerator DodgeUIRoutine(bool isSuccess)
    {
        dodgeUIPanel.SetActive(true);

        if (isSuccess)
        {
            dodgeText.text = "Success";
            dodgeText.color = Color.green;
        }
        else
        {
            dodgeText.text = "Fail";
            dodgeText.color = Color.red;
        }

        yield return new WaitForSeconds(1f); // 1초 뒤에 다시 패널을 끕니다.
        dodgeUIPanel.SetActive(false);
    }
}