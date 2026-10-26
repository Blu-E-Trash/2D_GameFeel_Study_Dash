using TMPro;
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
    public CanvasGroup teleportCollisionGroup;
    public Toggle togglePushOut;
    public Toggle toggleBlock;

    [Header("UI - Feedback (Checkbox)")]
    public Toggle toggleDashSound;
    public Toggle toggleAfterImage;
    public Toggle toggleTrail;
    public Toggle toggleCamera;

    [Header("UI - Gameplay (Checkbox)")]
    public Toggle toggleInvincibility;
    public Toggle toggleAttackDuringDash;

    [Header("UI - Debug Info")]
    public TextMeshProUGUI debugText;

    [Header("UI - Dash Tuning (Slider)")]
    public Slider sliderDistance;
    public Slider sliderDuration;
    public Slider sliderCooldown;
    public Text labelDistance;
    public Text labelDuration;
    public Text labelCooldown;

    [Header("UI - Test Stats")]
    public Text statsText;
    public Button buttonResetStats;

    // --- 테스트 기록 ---
    private int dashCount;
    private int dodgeSuccessCount;
    private int dodgeFailCount;
    private int enemyHitCount;
    private int enemyHitsThisDash;
    private int enemyHitsLastDash;
    private float lastDashDistance;
    private Vector2 dashStartPosition;

    private void OnEnable()
    {
        if (dashSystem != null)
        {
            dashSystem.OnDashStart += HandleDashStartStats;
            dashSystem.OnDashEnd += HandleDashEndStats;
        }
        if (playerController != null) playerController.OnDodgeResult += HandleDodgeResult;
        EnemyController.OnAnyEnemyHit += HandleEnemyHit;
    }

    private void OnDisable()
    {
        if (dashSystem != null)
        {
            dashSystem.OnDashStart -= HandleDashStartStats;
            dashSystem.OnDashEnd -= HandleDashEndStats;
        }
        if (playerController != null) playerController.OnDodgeResult -= HandleDodgeResult;
        EnemyController.OnAnyEnemyHit -= HandleEnemyHit;
    }

    private void Start()
    {
        // 1. 시스템 스크립트의 기본값을 읽어와서 UI 토글에 자동 반영
        toggleActualMovement.isOn = (dashSystem.currentMovementType == DashSystem.MovementType.ActualMovement);
        toggleTeleport.isOn = (dashSystem.currentMovementType == DashSystem.MovementType.Teleport);

        togglePushOut.isOn = (dashSystem.currentTeleportCollision == DashSystem.TeleportCollisionType.PushOut);
        toggleBlock.isOn = (dashSystem.currentTeleportCollision == DashSystem.TeleportCollisionType.Block);

        toggleDashSound.isOn = dashFeedback.useDashSound;
        toggleAfterImage.isOn = dashFeedback.useAfterImage;
        toggleTrail.isOn = dashFeedback.useTrail;
        toggleCamera.isOn = dashFeedback.useCameraMovement;

        toggleInvincibility.isOn = playerController.useDashInvincibility;
        toggleAttackDuringDash.isOn = playerController.useAttackDuringDash;

        // 슬라이더: 현재 DashSystem 값으로 시작 (범위는 인스펙터의 Min/Max Value)
        if (sliderDistance != null) sliderDistance.value = dashSystem.dashDistance;
        if (sliderDuration != null) sliderDuration.value = dashSystem.dashDuration;
        if (sliderCooldown != null) sliderCooldown.value = dashSystem.dashCooldown;

        // 2. 이벤트 리스너 연결 (UI를 클릭할 때마다 함수가 실행되도록 설정)
        toggleActualMovement.onValueChanged.AddListener(delegate { OnMovementTypeChanged(); });
        toggleTeleport.onValueChanged.AddListener(delegate { OnMovementTypeChanged(); });

        togglePushOut.onValueChanged.AddListener(delegate { OnTeleportCollisionChanged(); });
        toggleBlock.onValueChanged.AddListener(delegate { OnTeleportCollisionChanged(); });

        toggleDashSound.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleAfterImage.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleTrail.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });
        toggleCamera.onValueChanged.AddListener(delegate { OnFeedbackChanged(); });

        toggleInvincibility.onValueChanged.AddListener(delegate { OnGameplayChanged(); });
        toggleAttackDuringDash.onValueChanged.AddListener(delegate { OnGameplayChanged(); });

        if (sliderDistance != null) sliderDistance.onValueChanged.AddListener(delegate { OnTuningChanged(); });
        if (sliderDuration != null) sliderDuration.onValueChanged.AddListener(delegate { OnTuningChanged(); });
        if (sliderCooldown != null) sliderCooldown.onValueChanged.AddListener(delegate { OnTuningChanged(); });

        if (buttonResetStats != null) buttonResetStats.onClick.AddListener(ResetStats);

        // 3. UI 상태에 맞게 게임 로직을 한 번 더 확실하게 동기화 및 패널 비활성화 등 처리
        OnMovementTypeChanged();
        OnTeleportCollisionChanged();
        OnFeedbackChanged();
        OnGameplayChanged();
        OnTuningChanged();
        RefreshStatsText();
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
    }

    private void OnGameplayChanged()
    {
        playerController.useDashInvincibility = toggleInvincibility.isOn;
        playerController.useAttackDuringDash = toggleAttackDuringDash.isOn;
    }

    // 슬라이더 값 → DashSystem 반영 (다음 대시부터 적용)
    private void OnTuningChanged()
    {
        if (sliderDistance != null) dashSystem.dashDistance = sliderDistance.value;
        if (sliderDuration != null) dashSystem.dashDuration = sliderDuration.value;
        if (sliderCooldown != null) dashSystem.dashCooldown = sliderCooldown.value;

        if (labelDistance != null) labelDistance.text = $"Distance {dashSystem.dashDistance:F1}";
        if (labelDuration != null) labelDuration.text = $"Duration {dashSystem.dashDuration:F2}s";
        if (labelCooldown != null) labelCooldown.text = $"Cooldown {dashSystem.dashCooldown:F2}s";
    }

    // --- 테스트 기록 ---
    private void HandleDashStartStats()
    {
        dashCount++;
        enemyHitsThisDash = 0;
        dashStartPosition = dashSystem.transform.position;
        RefreshStatsText();
    }

    private void HandleDashEndStats()
    {
        // 실제로 이동한 수평 거리 (벽 막힘·끼임 확인용)
        lastDashDistance = Mathf.Abs(dashSystem.transform.position.x - dashStartPosition.x);
        enemyHitsLastDash = enemyHitsThisDash;
        RefreshStatsText();
    }

    private void HandleDodgeResult(bool isSuccess)
    {
        if (isSuccess) dodgeSuccessCount++;
        else dodgeFailCount++;
        RefreshStatsText();
    }

    private void HandleEnemyHit(EnemyController enemy)
    {
        enemyHitCount++;
        enemyHitsThisDash++;
        RefreshStatsText();
    }

    private void ResetStats()
    {
        dashCount = 0;
        dodgeSuccessCount = 0;
        dodgeFailCount = 0;
        enemyHitCount = 0;
        enemyHitsThisDash = 0;
        enemyHitsLastDash = 0;
        lastDashDistance = 0f;
        RefreshStatsText();
    }

    private void RefreshStatsText()
    {
        if (statsText == null) return;

        statsText.text = $"Dash {dashCount}   Dist {lastDashDistance:F2}\n" +
                         $"Dodge {dodgeSuccessCount} / Hit {dodgeFailCount}\n" +
                         $"Enemy Hits {enemyHitCount} (last {enemyHitsLastDash})";
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
