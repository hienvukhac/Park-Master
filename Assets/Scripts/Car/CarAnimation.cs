using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CarAnimation : MonoBehaviour
{
    [Header("Idle Animation")]
    [SerializeField] private float idleDuration = 0.35f;

    [SerializeField] private float stretchY = 0.15f;
    [SerializeField] private float squashXZ = 0.05f;

    [Header("Return-to-Start Arc Animation")]
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private float arcHeight = 1.6f;

    public bool IsFlyingBack { get; private set; } = false;

    private Tween idleTween;
    private Vector3 initialScale;
    private Coroutine flyCoroutine;

    void Start()
    {
        initialScale = this.transform.localScale;
    }

    public void StartIdleAnimation()
    {
        StopIdleAnimation();
        if(initialScale == Vector3.zero)
        {
            initialScale = this.transform.localScale;
        }

        Vector3 targetScale = new Vector3(
            initialScale.x * (1f - squashXZ),
            initialScale.y * (1f + stretchY),
            initialScale.z * (1f - squashXZ)
        );

        idleTween = this.transform.DOScale(targetScale, idleDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopIdleAnimation()
    {
        if(idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
            idleTween = null;
        }

        if(initialScale != Vector3.zero)
        {
            this.transform.localScale = initialScale;
        }
    }

    public void ResetScaleImmediate()
    {
        this.transform.DOKill();
        if (initialScale != Vector3.zero)
            this.transform.localScale = initialScale;
    }

    public void FlyTo(Vector3 targetPos, Quaternion targetRot, Action onComplete = null, float customDuration = -1f)
    {
        float duration = (customDuration > 0f) ? customDuration : flyDuration;
        if (flyCoroutine != null) StopCoroutine(flyCoroutine);
        flyCoroutine = StartCoroutine(FlyRoutine(targetPos, targetRot, onComplete, duration));
    }
    private IEnumerator FlyRoutine(Vector3 targetPos, Quaternion targetRot, Action onComplete, float duration)
    {
        IsFlyingBack = true;
        Vector3 initialPos = transform.position;
        Quaternion initialRot = transform.rotation;
        float elapsed = 0f;
        float currentArc = (duration < flyDuration) ? (arcHeight * 0.6f) : arcHeight;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPos = Vector3.Lerp(initialPos, targetPos, smoothT);

            currentPos.y += 4f * currentArc * smoothT * (1f - smoothT);
            transform.position = currentPos;
            transform.rotation = Quaternion.Slerp(initialRot, targetRot, smoothT);
            yield return null;
        }
        transform.position = targetPos;
        transform.rotation = targetRot;
        IsFlyingBack = false;
        flyCoroutine = null;

        StartIdleAnimation();
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }
        StopIdleAnimation();
        this.transform.DOKill();
    }
}
