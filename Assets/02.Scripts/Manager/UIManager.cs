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
    public CanvasGroup teleportCollisionGroup; // 사진에 질문하신 그 칸입니다!
    public Toggle togglePushOut;
    public Toggle toggleBlock;

    [Header("UI - Feedback (Checkbox)")]
    public Toggle toggleDashSound;
    public Toggle toggleAfterImage;
    public Toggle toggleTrail;
    public Toggle toggleCamera;
    // 파티클, 애니메이션 토글 변수 제거됨

    [Header("UI - Gameplay (Checkbox)")]
    public Toggle toggleInvincibility;
    public Toggle toggleAttackDuringDash;

    [Header("UI - Debug Info")]
    public TextMeshProUGUI debugText;

    private void Start()
    {
        // 1. 이벤트 리스너 연결
        toggleActualMovement.onValueChanged.AddListener(delegate { OnMovementTypeChanged(); });
        toggleTeleport.onValueChanged.AddListener(delegate { OnMovementTypeChanged(); });

        togglePushOut.onValueChanged.AddListener(delegate { OnTeleportCollisionChanged(); });
        toggleBlock.onValueChanged.AddListener(delegate { OnTeleportCollisionChanged(); });

        toggleDashSound.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleAfterImage.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleTrail.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleCamera.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        // 파티클, 애니메이션 리스너 제거됨

        toggleInvincibility.onValueChanged.AddListener(delegate { OnGameplayChanged(); });
        toggleAttackDuringDash.onValueChanged.AddListener(delegate { OnGameplayChanged(); });

        // 2. 시작 시 초기 UI 상태 동기화
        OnMovementTypeChanged();
        OnTeleportCollisionChanged();
        OnFeedbackChanged();
        OnGameplayChanged();
    }

    private void OnMovementTypeChanged()
    {
        if (toggleActualMovement.isOn)
        {
            dashSystem.currentMovementType = DashSystem.MovementType.ActualMovement;

            if (teleportCollisionGroup != null)
            {
                teleportCollisionGroup.alpha = 0.5f;
                teleportCollisionGroup.interactable = false;
            }
        }
        else if (toggleTeleport.isOn)
        {
            dashSystem.currentMovementType = DashSystem.MovementType.Teleport;

            if (teleportCollisionGroup != null)
            {
                teleportCollisionGroup.alpha = 1f;
                teleportCollisionGroup.interactable = true;
            }
        }
    }

    private void OnTeleportCollisionChanged()
    {
        if (togglePushOut.isOn) dashSystem.currentTeleportCollision = DashSystem.TeleportCollisionType.PushOut;
        else if (toggleBlock.isOn) dashSystem.currentTeleportCollision = DashSystem.TeleportCollisionType.Block;
    }

    private void OnFeedbackChanged()
    {
        dashFeedback.useDashSound = toggleDashSound.isOn;
        dashFeedback.useAfterImage = toggleAfterImage.isOn;
        dashFeedback.useTrail = toggleTrail.isOn;
        dashFeedback.useCameraMovement = toggleCamera.isOn;
        // 파티클, 애니메이션 전달 로직 제거됨
    }

    private void OnGameplayChanged()
    {
        playerController.useDashInvincibility = toggleInvincibility.isOn;
        playerController.useAttackDuringDash = toggleAttackDuringDash.isOn;
    }

    private void Update()
    {
        if (debugText == null || dashSystem == null) return;

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

        if (dashSystem.currentMovementType == DashSystem.MovementType.Teleport)
        {
            string colStr = dashSystem.currentTeleportCollision == DashSystem.TeleportCollisionType.PushOut ? "Push Out" : "Block";
            info += $"\nTeleport Collision : {colStr}";
        }

        debugText.text = info;
    }
}