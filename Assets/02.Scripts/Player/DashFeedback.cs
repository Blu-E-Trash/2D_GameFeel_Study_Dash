using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class DashFeedback : MonoBehaviour
{
    [Header("Feedback Options (Controlled by UI)")]
    public bool useDashSound = true;
    public bool useAfterImage = true;
    public bool useTrail = true;
    public bool useCameraMovement = true;

    [Header("Components & References")]
    public DashSystem dashSystem;
    public SpriteRenderer playerSprite;
    public AudioSource audioSource;
    public AudioClip dashClip;
    public TrailRenderer trailRenderer;
    public CinemachineImpulseSource cameraImpulse;

    [Header("After Image Settings")]
    public float afterImageInterval = 0.05f;
    public float afterImageLifetime = 0.3f;

    [Tooltip("Teleport 대시에서 이동 경로 위에 잔상을 찍는 간격(거리)")]
    public float teleportAfterImageSpacing = 0.8f;

    private Coroutine afterImageCoroutine;

    private void OnEnable()
    {
        if (dashSystem != null)
        {
            dashSystem.OnDashStart += PlayStartFeedback;
            dashSystem.OnDashEnd += PlayEndFeedback;
            dashSystem.OnTeleport += PlayTeleportAfterImages;
        }
    }

    private void OnDisable()
    {
        if (dashSystem != null)
        {
            dashSystem.OnDashStart -= PlayStartFeedback;
            dashSystem.OnDashEnd -= PlayEndFeedback;
            dashSystem.OnTeleport -= PlayTeleportAfterImages;
        }
    }

    private void Start()
    {
        if (trailRenderer != null)
            trailRenderer.emitting = false;
    }

    private void PlayStartFeedback()
    {
        // 1. 대시 사운드
        if (useDashSound && audioSource != null && dashClip != null)
        {
            audioSource.PlayOneShot(dashClip);
        }

        // 2. 궤적 (Trail)
        if (useTrail && trailRenderer != null)
        {
            trailRenderer.emitting = true;
        }

        // 3. 카메라 무브먼트 (화면 흔들림)
        if (useCameraMovement && cameraImpulse != null)
        {
            cameraImpulse.GenerateImpulse();
        }

        // 4. 잔상 (After Image)
        // Actual Movement: 이동하는 동안 일정 간격으로 잔상 생성
        // Teleport: 이동 과정이 없으므로 OnTeleport에서 경로 위에 한 번에 생성 (PlayTeleportAfterImages)
        if (useAfterImage && dashSystem.currentMovementType == DashSystem.MovementType.ActualMovement)
        {
            if (afterImageCoroutine != null) StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = StartCoroutine(SpawnAfterImagesRoutine());
        }
    }

    private void PlayEndFeedback()
    {
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        if (afterImageCoroutine != null)
        {
            StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = null;
        }
    }

    private IEnumerator SpawnAfterImagesRoutine()
    {
        while (true)
        {
            CreateGhostSprite(playerSprite.transform.position, 0.5f, afterImageLifetime);
            yield return new WaitForSeconds(afterImageInterval);
        }
    }

    // 출발점 → 도착점 사이에 잔상을 나눠 찍는다.
    // 출발점 쪽은 옅고 빨리 사라지며, 도착점에 가까울수록 진하고 오래 남아 이동 방향이 읽히도록 한다.
    private void PlayTeleportAfterImages(Vector2 origin, Vector2 direction, float distance)
    {
        if (!useAfterImage || playerSprite == null) return;
        if (distance <= 0.01f) return;

        // 스프라이트가 리지드바디 원점에서 떨어져 있는 만큼 보정 (둘 다 아직 순간이동 전 위치 기준)
        Vector3 spriteOffset = playerSprite.transform.position - dashSystem.transform.position;

        int count = Mathf.Max(2, Mathf.RoundToInt(distance / Mathf.Max(0.1f, teleportAfterImageSpacing)));

        // i = 0 은 출발점, 도착점(i = count)은 플레이어 본체이므로 제외
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            Vector3 pos = (Vector3)(origin + direction * (distance * t)) + spriteOffset;
            float alpha = Mathf.Lerp(0.2f, 0.6f, t);
            float lifetime = afterImageLifetime * Mathf.Lerp(0.6f, 1.2f, t);
            CreateGhostSprite(pos, alpha, lifetime);
        }
    }

    private void CreateGhostSprite(Vector3 position, float alpha, float lifetime)
    {
        if (playerSprite == null) return;

        GameObject ghostObj = new GameObject("DashAfterImage");
        ghostObj.transform.position = position;
        ghostObj.transform.rotation = playerSprite.transform.rotation;
        ghostObj.transform.localScale = playerSprite.transform.lossyScale;

        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sprite = playerSprite.sprite;
        ghostSr.flipX = playerSprite.flipX;
        ghostSr.color = new Color(1f, 1f, 1f, alpha);
        ghostSr.sortingLayerID = playerSprite.sortingLayerID;
        ghostSr.sortingOrder = playerSprite.sortingOrder - 1;

        StartCoroutine(FadeOutGhostRoutine(ghostSr, ghostObj, lifetime));
    }

    private IEnumerator FadeOutGhostRoutine(SpriteRenderer sr, GameObject obj, float lifetime)
    {
        float timer = 0f;
        Color startColor = sr.color;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / lifetime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(obj);
    }
}
