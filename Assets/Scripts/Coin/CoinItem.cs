using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(BoxCollider))]
public class CoinItem : MonoBehaviour
{
    [Header("Cấu hình Xu")]
    [Tooltip("Giá trị của đồng xu này ")]
    [SerializeField] private int coinValue = 1;

    [Tooltip("Tự động hồi sinh đồng xu khi xe quay về vạch xuất phát hoặc chơi lại")]
    [SerializeField] private bool respawnOnReset = true;

    [Header("Hiệu ứng khi ăn xu")]
    [Tooltip("Prefab hiệu ứng hạt 3D tại vị trí ăn xu")]
    [SerializeField] private GameObject pickupVfxPrefab;

    [Tooltip("Hiệu ứng xu bay uốn lượn về icon UI ")]
    [SerializeField] private bool flyToUI = true;

    [Tooltip("Thời gian nảy phồng rồi thu nhỏ biến mất của đồng xu 3D (giây)")]
    [SerializeField] private float popDuration = 0.25f;

    private BoxCollider col;
    private Vector3 initialScale;
    private Vector3 initialPosition;
    private bool isCollected = false;
    private Tween popTween;

    public int CoinValue => coinValue;
    public bool IsCollected => isCollected;

    private void Awake()
    {
        col = this.GetComponent<BoxCollider>();
        col.isTrigger = true;

        initialScale = this.transform.localScale;
        initialPosition = this.transform.position;
    }

    private void Start()
    {
        if (initialScale == Vector3.zero)
            initialScale = this.transform.localScale;
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (!GameManager.IsApplicationQuitting && GameManager.HasInstance)
        {
            GameManager.Instance.OnLevelResetEvent += HandleReset;
            GameManager.Instance.OnLevelFailedEvent += HandleReset;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!GameManager.IsApplicationQuitting && GameManager.HasInstance)
        {
            GameManager.Instance.OnLevelResetEvent -= HandleReset;
            GameManager.Instance.OnLevelFailedEvent -= HandleReset;
        }
    }

    private void HandleReset()
    {
        if (respawnOnReset && isCollected)
        {
            ResetCoin();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        CarController car = other.GetComponentInParent<CarController>();
        if (car == null) car = other.GetComponent<CarController>();

        if (car != null && !car.isFlyingBack)
        {
            Collect(car);
        }
    }

    public void Collect(CarController car)
    {
        if (isCollected) return;
        isCollected = true;

        if (col != null) col.enabled = false;

        Vector3 collectPos = transform.position;

        SpawnPickupVFX(collectPos);

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CollectCoin(this, collectPos, coinValue, flyToUI);
        }

        if (popTween != null && popTween.IsActive()) popTween.Kill();

        transform.DOKill();
        DG.Tweening.Sequence popSeq = DOTween.Sequence();
        popTween = popSeq;
        popSeq.Append(transform.DOScale(initialScale * 1.35f, popDuration * 0.4f).SetEase(Ease.OutQuad));
        popSeq.Append(transform.DOScale(Vector3.zero, popDuration * 0.6f).SetEase(Ease.InBack));
        popSeq.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void SpawnPickupVFX(Vector3 position)
    {
        if (pickupVfxPrefab != null)
        {
            GameObject fx = ObjectPool.Spawn(pickupVfxPrefab, position, Quaternion.identity);
            if (fx != null)
            {
                ObjectPool.Despawn(fx, 1.8f);
            }
        }
    }

    public void ResetCoin()
    {
        if (popTween != null && popTween.IsActive()) popTween.Kill();
        this.transform.DOKill();

        gameObject.SetActive(true);
        if (initialScale != Vector3.zero)
            this.transform.localScale = initialScale;

        isCollected = false;
        if (col != null) col.enabled = true;
    }

    private void OnDestroy()
    {
        this.transform.DOKill();
        UnsubscribeEvents();
    }
}
