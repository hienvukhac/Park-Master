using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LevelCompleteUI : MonoBehaviour
{
    private static LevelCompleteUI instance;
    public static LevelCompleteUI Instance => instance;

    [Header("Giao diện Chữ")]
    [Tooltip("Đối tượng Text 'LEVEL COMPLETE' (nếu để trống script sẽ tự động tạo)")]
    [SerializeField] private RectTransform textRect;
    [SerializeField] private TextMeshProUGUI levelCompleteTMP;

    [Header("Cấu hình")]
    [Tooltip("Thời gian xuất hiện hiệu ứng (giây)")]
    [SerializeField] private float displayDuration = 2.0f;
    [Tooltip("Vị trí chếch lên phía trên")]
    [SerializeField] private float posYOffset = 180f;

    [Header("Pháo giấy 2 bên mép màn hình")]
    [SerializeField] private ParticleSystem leftConfetti;
    [SerializeField] private ParticleSystem rightConfetti;

    private CanvasGroup canvasGroup;
    private bool isShowing = false;
    private Tween scaleTween;
    private Tween rotTween;
    private Tween fadeTween;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        EnsureUIComponents();
        HideImmediate();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Start()
    {
        SubscribeEvents();

        if (leftConfetti == null || rightConfetti == null)
        {
            BuildConfettiCannons();
        }
    }

    private void SubscribeEvents()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnLevelVictoryEvent -= OnLevelVictory;
            GameManager.Instance.OnLevelVictoryEvent += OnLevelVictory;
            GameManager.Instance.OnLevelResetEvent -= OnLevelReset;
            GameManager.Instance.OnLevelResetEvent += OnLevelReset;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!GameManager.IsApplicationQuitting && GameManager.HasInstance)
        {
            GameManager.Instance.OnLevelVictoryEvent -= OnLevelVictory;
            GameManager.Instance.OnLevelResetEvent -= OnLevelReset;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        UnsubscribeEvents();
        KillAllTweens();
    }

    private void EnsureUIComponents()
    {
        if (textRect == null)
        {
            Transform found = transform.Find("Txt_LevelComplete");
            if (found != null && found.GetComponent<RectTransform>() != null)
            {
                textRect = found as RectTransform;
            }
            else
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform foundOnCanvas = canvas.transform.Find("Txt_LevelComplete");
                    if (foundOnCanvas != null && foundOnCanvas.GetComponent<RectTransform>() != null)
                    {
                        textRect = foundOnCanvas as RectTransform;
                    }
                }
            }
        }

        if (textRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)
            {
                GameObject txtObj = new GameObject("Txt_LevelComplete", typeof(RectTransform));
                txtObj.transform.SetParent(canvas.transform, false);

                textRect = txtObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(0f, posYOffset);
                textRect.sizeDelta = new Vector2(600f, 130f);

                levelCompleteTMP = txtObj.AddComponent<TextMeshProUGUI>();
                levelCompleteTMP.text = "LEVEL COMPLETE";
                levelCompleteTMP.fontSize = 54;
                levelCompleteTMP.fontStyle = FontStyles.Bold;
                levelCompleteTMP.alignment = TextAlignmentOptions.Center;
                levelCompleteTMP.raycastTarget = false;

                levelCompleteTMP.enableVertexGradient = true;
                levelCompleteTMP.colorGradient = new VertexGradient(
                    new Color(1f, 0.96f, 0.46f, 1f),
                    new Color(1f, 0.96f, 0.46f, 1f),
                    new Color(1f, 0.62f, 0.0f, 1f),
                    new Color(1f, 0.62f, 0.0f, 1f)
                );
            }
        }

        if (textRect != null && levelCompleteTMP == null)
        {
            levelCompleteTMP = textRect.GetComponent<TextMeshProUGUI>();
        }

        if (textRect != null)
        {
            canvasGroup = textRect.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = textRect.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnLevelVictory()
    {
        ShowVictory();
    }

    private void OnLevelReset()
    {
        StopAllCoroutines();
        HideImmediate();
    }

    [ContextMenu("Test Victory Effect")]
    public void ShowVictory()
    {
        if (isShowing) return;
        Debug.Log("<color=#00FF7F>🎉 [LevelCompleteUI] Bắt đầu hiển thị hiệu ứng LEVEL COMPLETE!</color>");
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        isShowing = true;
        EnsureUIComponents();

        PositionCannonsToScreenEdges();
        if (leftConfetti != null) leftConfetti.Play();
        if (rightConfetti != null) rightConfetti.Play();

        if (textRect != null)
        {
            KillAllTweens();

            textRect.gameObject.SetActive(true);
            textRect.anchoredPosition = new Vector2(0, posYOffset);
            textRect.localScale = Vector3.zero;

            if (canvasGroup != null) canvasGroup.alpha = 1f;

            scaleTween = textRect.DOScale(Vector3.one, 0.42f).SetEase(Ease.OutBack).SetUpdate(true);

            rotTween = textRect.DOPunchRotation(new Vector3(0, 0, 4.5f), 0.5f, vibrato: 5, elasticity: 0.35f)
                .SetDelay(0.2f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(displayDuration);

        LevelManager levelMgr = LevelManager.Instance ?? FindFirstObjectByType<LevelManager>();

        if (canvasGroup != null)
        {
            fadeTween = canvasGroup.DOFade(0f, 0.35f).SetUpdate(true).OnComplete(() =>
            {
                HideImmediate();
                if (levelMgr != null)
                {
                    levelMgr.NextLevel();
                }
            });
        }
        else
        {
            HideImmediate();
            if (levelMgr != null)
            {
                levelMgr.NextLevel();
            }
        }
    }

    public void HideImmediate()
    {
        isShowing = false;
        KillAllTweens();

        if (textRect != null)
        {
            textRect.localScale = Vector3.zero;
            textRect.gameObject.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (leftConfetti != null) leftConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (rightConfetti != null) rightConfetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void KillAllTweens()
    {
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        if (rotTween != null && rotTween.IsActive()) rotTween.Kill();
        if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();
    }

    private void PositionCannonsToScreenEdges()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        float dist = 4.0f;

        if (leftConfetti != null)
        {
            Vector3 leftWorld = cam.ViewportToWorldPoint(new Vector3(0.02f, 0.32f, dist));
            leftConfetti.transform.position = leftWorld;
            Vector3 targetRight = cam.ViewportToWorldPoint(new Vector3(0.68f, 0.85f, dist));
            leftConfetti.transform.rotation = Quaternion.LookRotation(targetRight - leftWorld);
        }

        if (rightConfetti != null)
        {
            Vector3 rightWorld = cam.ViewportToWorldPoint(new Vector3(0.98f, 0.32f, dist));
            rightConfetti.transform.position = rightWorld;
            Vector3 targetLeft = cam.ViewportToWorldPoint(new Vector3(0.32f, 0.85f, dist));
            rightConfetti.transform.rotation = Quaternion.LookRotation(targetLeft - rightWorld);
        }
    }

    public void BuildConfettiCannons()
    {
        if (leftConfetti == null)
        {
            leftConfetti = CreateSingleCannon("LeftConfettiCannon");
        }

        if (rightConfetti == null)
        {
            rightConfetti = CreateSingleCannon("RightConfettiCannon");
        }
    }

    private ParticleSystem CreateSingleCannon(string cannonName)
    {
        GameObject go = new GameObject(cannonName);
        go.SetActive(false);
        go.transform.SetParent(this.transform);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 10.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.gravityModifier = 0.88f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        Gradient multiColor = new Gradient();
        multiColor.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.22f, 0.22f), 0.0f),
                new GradientColorKey(new Color(1f, 0.86f, 0.1f), 0.25f),
                new GradientColorKey(new Color(0.18f, 0.9f, 0.35f), 0.50f),
                new GradientColorKey(new Color(0.2f, 0.65f, 1f), 0.75f),
                new GradientColorKey(new Color(1f, 0.35f, 0.85f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(multiColor) { mode = ParticleSystemGradientMode.RandomColor };

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 45),
            new ParticleSystem.Burst(0.18f, 35)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.1f;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.separateAxes = true;
        rotOverLife.x = new ParticleSystem.MinMaxCurve(4f, 9f);
        rotOverLife.y = new ParticleSystem.MinMaxCurve(4f, 9f);
        rotOverLife.z = new ParticleSystem.MinMaxCurve(4f, 9f);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fadeGrad = new Gradient();
        fadeGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = fadeGrad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Material squareMat = Resources.Load<Material>("Mat_ParkingWin");
        if (squareMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Mobile/Particles/Additive");
            if (shader != null) squareMat = new Material(shader);
        }
        if (squareMat != null) renderer.sharedMaterial = squareMat;

        go.SetActive(true);
        return ps;
    }
}
