using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class DashFeedback : MonoBehaviour
{
    [Header("Feedback Options (Controlled by UI)")]
    public bool useDashSound = true;
    public bool useAfterImage = true;
    public bool useTrail = true;
    public bool useParticle = true;
    public bool useCameraMovement = true;
    public bool useAnimation = true;

    [Header("Components & References")]
    public DashSystem dashSystem;
    public SpriteRenderer playerSprite;
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip dashClip;
    public TrailRenderer trailRenderer;
    public ParticleSystem dashParticle;
    public CinemachineImpulseSource cameraImpulse; // 카메라 흔들림 효과용

    [Header("After Image Settings")]
    public float afterImageInterval = 0.05f; // 잔상 생성 간격
    public float afterImageLifetime = 0.3f;  // 잔상이 사라지는 데 걸리는 시간

    private Coroutine afterImageCoroutine;

    private void OnEnable()
    {
        if (dashSystem != null)
        {
            // DashSystem의 이벤트 구독
            dashSystem.OnDashStart += PlayStartFeedback;
            dashSystem.OnDashEnd += PlayEndFeedback;
        }
    }

    private void OnDisable()
    {
        if (dashSystem != null)
        {
            // 이벤트 구독 해제
            dashSystem.OnDashStart -= PlayStartFeedback;
            dashSystem.OnDashEnd -= PlayEndFeedback;
        }
    }

    private void Start()
    {
        // 시작 시 트레일은 꺼둡니다.
        if (trailRenderer != null)
            trailRenderer.emitting = false;
    }

    // --- 대시 시작 시 호출되는 피드백 ---
    private void PlayStartFeedback()
    {
        // 1. 대시 사운드
        if (useDashSound && audioSource != null && dashClip != null)
        {
            audioSource.PlayOneShot(dashClip);
        }

        // 2. 파티클 이펙트
        if (useParticle && dashParticle != null)
        {
            dashParticle.Play();
        }

        // 3. 궤적 (Trail)
        if (useTrail && trailRenderer != null)
        {
            trailRenderer.emitting = true;
        }

        // 4. 애니메이션
        if (useAnimation && animator != null)
        {
            animator.SetTrigger("Dash");
        }

        // 5. 카메라 무브먼트 (화면 흔들림)
        if (useCameraMovement && cameraImpulse != null)
        {
            cameraImpulse.GenerateImpulse();
        }

        // 6. 잔상 (After Image) 생성 코루틴 시작
        if (useAfterImage)
        {
            if (afterImageCoroutine != null) StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = StartCoroutine(SpawnAfterImagesRoutine());
        }
    }

    // --- 대시 종료 시 호출되는 피드백 ---
    private void PlayEndFeedback()
    {
        // 트레일 끄기
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        // 파티클 정지
        if (dashParticle != null)
        {
            dashParticle.Stop();
        }

        // 잔상 생성 중지
        if (afterImageCoroutine != null)
        {
            StopCoroutine(afterImageCoroutine);
            afterImageCoroutine = null;
        }
    }

    // --- 2D 잔상(Sprite Ghosting) 생성 로직 ---
    private IEnumerator SpawnAfterImagesRoutine()
    {
        while (true)
        {
            CreateGhostSprite();
            yield return new WaitForSeconds(afterImageInterval);
        }
    }

    private void CreateGhostSprite()
    {
        if (playerSprite == null) return;

        // 1. 잔상을 표시할 빈 게임 오브젝트 생성
        GameObject ghostObj = new GameObject("DashAfterImage");
        ghostObj.transform.position = playerSprite.transform.position;
        ghostObj.transform.rotation = playerSprite.transform.rotation;
        ghostObj.transform.localScale = playerSprite.transform.lossyScale;

        // 2. SpriteRenderer 복사 및 설정
        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sprite = playerSprite.sprite;
        ghostSr.flipX = playerSprite.flipX;
        ghostSr.color = new Color(1f, 1f, 1f, 0.5f); // 반투명하게 시작
        ghostSr.sortingLayerID = playerSprite.sortingLayerID;
        ghostSr.sortingOrder = playerSprite.sortingOrder - 1; // 플레이어보다 뒤에 그려짐

        // 3. 서서히 사라지는(Fade Out) 처리 코루틴 실행
        StartCoroutine(FadeOutGhostRoutine(ghostSr, ghostObj));
    }

    private IEnumerator FadeOutGhostRoutine(SpriteRenderer sr, GameObject obj)
    {
        float timer = 0f;
        Color startColor = sr.color;

        while (timer < afterImageLifetime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / afterImageLifetime);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // 완전히 사라지면 오브젝트 파괴
        Destroy(obj);
    }
}