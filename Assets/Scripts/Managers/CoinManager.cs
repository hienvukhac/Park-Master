using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CoinManager : MonoBehaviour
{
    private static CoinManager instance;
    public static CoinManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CoinManager>();
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("--- COIN_MANAGER ---");
                    instance = managerObj.AddComponent<CoinManager>();
                }
            }
            return instance;
        }
    }

    [Header("UI TextMeshPro")]
    [Tooltip("TextMeshPro hiển thị tổng số xu (tự động tìm 'CoinValue' nếu để trống)")]
    [SerializeField] private TextMeshProUGUI coinCountText;

    [Tooltip("Vị trí Icon xu trên UI để đồng xu bay tới (tự động tìm 'CoinIcon' nếu để trống)")]
    [SerializeField] private RectTransform targetIconRect;

    [Tooltip("Canvas chứa giao diện HUD")]
    [SerializeField] private Canvas canvas;

    [Header("Hiệu ứng Xu Bay (Fly to UI)")]
    [Tooltip("Prefab UI của đồng xu bay (tùy chọn, nếu không có sẽ tự tạo từ CoinIcon Sprite)")]
    [SerializeField] private GameObject flyCoinUIPrefab;

    [Tooltip("Sprite hình đồng xu để hiển thị khi bay về UI")]
    [SerializeField] private Sprite coinSprite;

    [Tooltip("Thời gian đồng xu bay từ đường đua về UI (giây)")]
    [SerializeField] private float flyDuration = 0.6f;

    [Tooltip("Độ nảy võng hình vòng cung khi bay")]
    [SerializeField] private float flyJumpPower = 140f;

    [Tooltip("Kiểu gia tốc bay (InBack / InQuad tạo cảm giác lao nhanh về đích rất đã mắt)")]
    [SerializeField] private Ease flyEase = Ease.InBack;

    [Header("Hiệu ứng Phản hồi UI (Punch Scale)")]
    [Tooltip("Độ rung nảy của Icon và Text khi xu chạm tới đích")]
    [SerializeField] private float punchScaleAmount = 0.35f;

    [Tooltip("Thời gian rung nảy")]
    [SerializeField] private float punchDuration = 0.22f;

    [Tooltip("Màu chữ lóe sáng chớp nhoáng khi tăng xu")]
    [SerializeField] private Color flashColor = new Color(1f, 0.92f, 0.25f, 1f);

    [Header("Âm thanh thu thập")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip coinCollectClip;
    [Tooltip("Tự động tăng cao độ (pitch) khi nhặt liên tục nhiều xu")]
    [SerializeField] private bool pitchShift = true;
    [SerializeField] private float pitchStep = 0.05f;
    [SerializeField] private float maxPitch = 1.4f;

    [Header("Dữ liệu Xu")]
    [SerializeField] private int totalCoins = 0;
    [SerializeField] private bool saveToPlayerPrefs = true;
    [SerializeField] private string playerPrefsKey = "ParkMaster_TotalCoins";

    public int TotalCoins => totalCoins;

    private float currentPitch = 1.0f;
    private float lastCollectTime = 0f;
    private Tween textPunchTween;
    private Tween iconPunchTween;
    private Color originalTextColor = Color.white;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        FindUIReferences();

        if (audioSource == null)
        {
            audioSource = this.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = this.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (coinCollectClip == null)
        {
            coinCollectClip = CreateProceduralCoinChime();
        }

        if (saveToPlayerPrefs)
        {
            totalCoins = PlayerPrefs.GetInt(playerPrefsKey, 0);
        }

        UpdateCoinTextInstant();
    }

    private void Start()
    {
        FindUIReferences();
        UpdateCoinTextInstant();
    }

    public void FindUIReferences()
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (coinCountText == null)
        {
            GameObject coinValObj = GameObject.Find("CoinValue");
            if (coinValObj != null)
            {
                coinCountText = coinValObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                coinCountText = FindFirstObjectByType<TextMeshProUGUI>();
            }
        }

        if (coinCountText != null)
        {
            originalTextColor = coinCountText.color;
        }

        if (targetIconRect == null)
        {
            GameObject coinIconObj = GameObject.Find("CoinIcon");
            if (coinIconObj != null)
            {
                targetIconRect = coinIconObj.GetComponent<RectTransform>();
            }
            else if (coinCountText != null)
            {
                targetIconRect = coinCountText.rectTransform;
            }
        }

        if (coinSprite == null && targetIconRect != null)
        {
            Image iconImg = targetIconRect.GetComponent<Image>();
            if (iconImg != null && iconImg.sprite != null)
            {
                coinSprite = iconImg.sprite;
            }
        }
    }

    public void CollectCoin(CoinItem coinItem, Vector3 worldPosition, int amount = 1, bool playFlyEffect = true)
    {
        if (playFlyEffect && targetIconRect != null && Camera.main != null && canvas != null)
        {
            StartCoroutine(SpawnFlyingUICoinRoutine(worldPosition, amount));
        }
        else
        {
            AddCoins(amount);
        }
    }

    private IEnumerator SpawnFlyingUICoinRoutine(Vector3 worldPosition, int amount)
    {
        Camera cam = Camera.main;
        if (cam == null || canvas == null || targetIconRect == null)
        {
            AddCoins(amount);
            yield break;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCam, out Vector2 startLocalPos);

        GameObject flyObj = null;
        if (flyCoinUIPrefab != null)
        {
            flyObj = Instantiate(flyCoinUIPrefab, canvas.transform);
        }
        else
        {
            flyObj = new GameObject("FlyingCoin_UI");
            flyObj.transform.SetParent(canvas.transform, false);
            Image img = flyObj.AddComponent<Image>();
            if (coinSprite != null)
            {
                img.sprite = coinSprite;
            }
            img.raycastTarget = false;
        }

        RectTransform flyRt = flyObj.GetComponent<RectTransform>();
        flyRt.SetAsLastSibling();
        flyRt.sizeDelta = new Vector2(70f, 70f);
        flyRt.anchoredPosition = startLocalPos;
        flyRt.localScale = Vector3.zero;

        flyRt.DOScale(Vector3.one * 1.15f, 0.12f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.08f);

        if (flyRt == null) yield break;

        Vector3 targetWorldPos = targetIconRect.position;

        Vector3 randomOffset = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0f);

        DG.Tweening.Sequence flySeq = DOTween.Sequence();
        flySeq.Append(flyRt.DOJump(targetWorldPos + randomOffset, flyJumpPower, 1, flyDuration).SetEase(flyEase));
        flySeq.Join(flyRt.DOScale(Vector3.one * 0.75f, flyDuration).SetEase(Ease.InSine));
        flySeq.OnComplete(() =>
        {
            if (flyObj != null)
            {
                Destroy(flyObj);
            }

            AddCoins(amount);
        });
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;

        if (saveToPlayerPrefs)
        {
            PlayerPrefs.SetInt(playerPrefsKey, totalCoins);
            PlayerPrefs.Save();
        }

        PlayPunchEffect();
        PlayCollectSound();
        UpdateCoinTextInstant();
    }

    private void PlayPunchEffect()
    {
        if (targetIconRect != null)
        {
            if (iconPunchTween != null && iconPunchTween.IsActive()) iconPunchTween.Kill();
            targetIconRect.localScale = Vector3.one;
            iconPunchTween = targetIconRect.DOPunchScale(Vector3.one * punchScaleAmount, punchDuration, 8, 0.5f);
        }

        if (coinCountText != null)
        {
            if (textPunchTween != null && textPunchTween.IsActive()) textPunchTween.Kill();
            coinCountText.transform.localScale = Vector3.one;
            textPunchTween = coinCountText.transform.DOPunchScale(Vector3.one * (punchScaleAmount * 0.8f), punchDuration, 8, 0.5f);

            coinCountText.color = flashColor;
            coinCountText.DOColor(originalTextColor, punchDuration * 1.5f);
        }
    }

    private void PlayCollectSound()
    {
        if (audioSource == null || coinCollectClip == null) return;

        if (pitchShift)
        {
            float now = Time.time;
            if (now - lastCollectTime < 0.4f)
            {
                currentPitch = Mathf.Min(currentPitch + pitchStep, maxPitch);
            }
            else
            {
                currentPitch = 1.0f;
            }
            lastCollectTime = now;
            audioSource.pitch = currentPitch;
        }
        else
        {
            audioSource.pitch = 1.0f;
        }

        audioSource.PlayOneShot(coinCollectClip);
    }

    public void UpdateCoinTextInstant()
    {
        if (coinCountText != null)
        {
            coinCountText.text = totalCoins.ToString();
        }
    }

    public void SetCoins(int newAmount)
    {
        totalCoins = Mathf.Max(0, newAmount);
        if (saveToPlayerPrefs)
        {
            PlayerPrefs.SetInt(playerPrefsKey, totalCoins);
            PlayerPrefs.Save();
        }
        UpdateCoinTextInstant();
    }

    [ContextMenu("Reset All Saved Coins")]
    public void ResetAllSavedCoins()
    {
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();
        totalCoins = 0;
        UpdateCoinTextInstant();
        Debug.Log("<color=yellow>💰 [CoinManager] Đã đặt lại tổng số xu về 0!</color>");
    }

    private AudioClip CreateProceduralCoinChime()
    {
        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = (t < 0.08f) ? 987.77f : 1318.51f;
            float envelope = Mathf.Exp(-t * 14f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("ProceduralCoinChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
