using TMPro; // TextMeshPro 사용
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Target Systems")]
    public DashSystem dashSystem;
    public DashFeedback dashFeedback;
    public PlayerController playerController;

    [Header("UI - Movement Type (Radio)")]
    public Toggle toggleActualMovement;
    public Toggle toggleTeleport;

    [Header("UI - Teleport Collision (Radio)")]
    public CanvasGroup teleportCollisionGroup; // 순간이동이 아닐 때 비활성화(반투명) 처리용
    public Toggle togglePushOut;
    public Toggle toggleBlock;

    [Header("UI - Feedback (Checkbox)")]
    public Toggle toggleDashSound;
    public Toggle toggleAfterImage;
    public Toggle toggleTrail;
    public Toggle toggleParticle;
    public Toggle toggleCamera;
    public Toggle toggleAnimation;

    [Header("UI - Gameplay (Checkbox)")]
    public Toggle toggleInvincibility;
    public Toggle toggleAttackDuringDash;

    [Header("UI - Debug Info")]
    public TextMeshProUGUI debugText;

    private void Start()
    {
        // 1. 이벤트 리스너 연결 (UI를 클릭할 때마다 함수가 실행되도록 설정)
        toggleActualMovement.onValueChanged.AddListener(delegate { OnMovementTypeChanged(); });
        toggleTeleport.onValueChanged.AddListener(delegate { OnMovementTypeChanged(); });

        togglePushOut.onValueChanged.AddListener(delegate { OnTeleportCollisionChanged(); });
        toggleBlock.onValueChanged.AddListener(delegate { OnTeleportCollisionChanged(); });

        toggleDashSound.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleAfterImage.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleTrail.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleParticle.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleCamera.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleAnimation.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });

        toggleInvincibility.onValueChanged.AddListener(delegate { OnGameplayChanged(); });
        toggleAttackDuringDash.onValueChanged.AddListener(delegate { OnGameplayChanged(); });

        // 2. 시작 시 초기 UI 상태를 스크립트에 한 번 동기화
        OnMovementTypeChanged();
        OnTeleportCollisionChanged();
        OnFeedbackChanged();
        OnGameplayChanged();
    }

    // --- 라디오 버튼 변경 처리 (Movement Type) ---
    private void OnMovementTypeChanged()
    {
        if (toggleActualMovement.isOn)
        {
            dashSystem.currentMovementType = DashSystem.MovementType.ActualMovement;

            // 순간이동형이 아니면 충돌 옵션 패널을 비활성화하고 반투명하게 만듦
            if (teleportCollisionGroup != null)
            {
                teleportCollisionGroup.alpha = 0.5f;
                teleportCollisionGroup.interactable = false;
            }
        }
        else if (toggleTeleport.isOn)
        {
            dashSystem.currentMovementType = DashSystem.MovementType.Teleport;

            // 순간이동형이면 충돌 옵션 패널 활성화
            if (teleportCollisionGroup != null)
            {
                teleportCollisionGroup.alpha = 1f;
                teleportCollisionGroup.interactable = true;
            }
        }
    }

    // --- 라디오 버튼 변경 처리 (Teleport Collision) ---
    private void OnTeleportCollisionChanged()
    {
        if (togglePushOut.isOn) dashSystem.currentTeleportCollision = DashSystem.TeleportCollisionType.PushOut;
        else if (toggleBlock.isOn) dashSystem.currentTeleportCollision = DashSystem.TeleportCollisionType.Block;
    }

    // --- 피드백 체크박스 변경 처리 ---
    private void OnFeedbackChanged()
    {
        dashFeedback.useDashSound = toggleDashSound.isOn;
        dashFeedback.useAfterImage = toggleAfterImage.isOn;
        dashFeedback.useTrail = toggleTrail.isOn;
        dashFeedback.useParticle = toggleParticle.isOn;
        dashFeedback.useCameraMovement = toggleCamera.isOn;
        dashFeedback.useAnimation = toggleAnimation.isOn;
    }

    // --- 게임플레이 체크박스 변경 처리 ---
    private void OnGameplayChanged()
    {
        playerController.useDashInvincibility = toggleInvincibility.isOn;
        playerController.useAttackDuringDash = toggleAttackDuringDash.isOn;
    }

    // --- 매 프레임 디버그 텍스트 갱신 ---
    private void Update()
    {
        if (debugText == null || dashSystem == null) return;

        // 기획서 13번 명세에 맞춘 디버그 텍스트 포맷팅
        string dashTypeStr = dashSystem.currentMovementType == DashSystem.MovementType.ActualMovement ? "Actual Movement" : "Teleport";
        string invStr = playerController.useDashInvincibility ? "ON" : "OFF";
        string atkStr = playerController.useAttackDuringDash ? "ON" : "OFF";

        string info = $"Dash Type      : {dashTypeStr}\n" +
                      $"Dash State     : {dashSystem.CurrentState}\n" +
                      $"Distance       : {dashSystem.dashDistance:F1}\n" +
                      $"Duration       : {dashSystem.dashDuration:F2}s\n" +
                      $"Cooldown       : {dashSystem.dashCooldown:F2}s\n" +
                      $"Invincible     : {invStr}\n" +
                      $"Attack         : {atkStr}";

        // 순간이동형일 경우 충돌 처리 방식 추가 출력
        if (dashSystem.currentMovementType == DashSystem.MovementType.Teleport)
        {
            string colStr = dashSystem.currentTeleportCollision == DashSystem.TeleportCollisionType.PushOut ? "Push Out" : "Block";
            info += $"\nTeleport Collision : {colStr}";
        }

        debugText.text = info;
    }
}