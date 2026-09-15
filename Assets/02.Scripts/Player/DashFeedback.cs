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

    private Coroutine afterImageCoroutine;

    private void OnEnable()
    {
        if (dashSystem != null)
        {
            dashSystem.OnDashStart += PlayStartFeedback;
            dashSystem.OnDashEnd += PlayEndFeedback;
        }
    }

    private void OnDisable()
    {
        if (dashSystem != null)
        {
            dashSystem.OnDashStart -= PlayStartFeedback;
            dashSystem.OnDashEnd -= PlayEndFeedback;
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
        if (useCameraMovement)
        {
            if (cameraImpulse != null) cameraImpulse.GenerateImpulse();
            Debug.Log("카메라 피드백 작동");
        }

        // 4. 잔상 (After Image) 
        if (useAfterImage)
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
            CreateGhostSprite();
            yield return new WaitForSeconds(afterImageInterval);
        }
    }

    private void CreateGhostSprite()
    {
        if (playerSprite == null) return;

        GameObject ghostObj = new GameObject("DashAfterImage");
        ghostObj.transform.position = playerSprite.transform.position;
        ghostObj.transform.rotation = playerSprite.transform.rotation;
        ghostObj.transform.localScale = playerSprite.transform.lossyScale;

        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sprite = playerSprite.sprite;
        ghostSr.flipX = playerSprite.flipX;
        ghostSr.color = new Color(1f, 1f, 1f, 0.5f);
        ghostSr.sortingLayerID = playerSprite.sortingLayerID;
        ghostSr.sortingOrder = playerSprite.sortingOrder - 1;

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

        Destroy(obj);
    }
}