using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class CarMenuUI : MonoBehaviour
{
    [Header("Menu Panel (CarMenu_Panel hoặc Bottom_Shop)")]
    [SerializeField] private GameObject menuPanel;

    [Header("Các nút bấm")]
    [SerializeField] private Button btnOpenCarShop;
    [SerializeField] private Button btnClose;

    [Header("Thời gian hiệu ứng")]
    [SerializeField] private float animDuration = 0.25f;

    private CanvasGroup canvasGroup;
    private Tween scaleTween;
    private Tween fadeTween;
    private bool isOpen = false;
    public bool IsOpen => isOpen;
    public static CarMenuUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupComponents();
        HideImmediate();
    }

    private void SetupComponents()
    {
        if (menuPanel == null)
            menuPanel = this.gameObject;

        canvasGroup = menuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = menuPanel.AddComponent<CanvasGroup>();

        if (btnOpenCarShop != null)
        {
            btnOpenCarShop.onClick.RemoveListener(OpenMenu);
            btnOpenCarShop.onClick.AddListener(OpenMenu);
        }

        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(CloseMenu);
            btnClose.onClick.AddListener(CloseMenu);
        }
    }

    public void OpenMenu()
    {
        LineDrawer.IsDrawingBlocked = true;
        isOpen = true;
        KillTweens();

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.SetAsLastSibling();
            menuPanel.transform.localScale = Vector3.one;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (CarShopManager.Instance != null)
        {
            CarShopManager.Instance.RefreshShop();
        }

        Transform popupTransform = null;
        if (menuPanel != null)
        {
            popupTransform = menuPanel.transform.Find("Bottom_Shop");
            if (popupTransform == null) popupTransform = menuPanel.transform;
        }

        if (popupTransform != null && animDuration > 0.05f)
        {
            popupTransform.localScale = new Vector3(0.85f, 0.85f, 1f);
            scaleTween = popupTransform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else if (popupTransform != null)
        {
            popupTransform.localScale = Vector3.one;
        }

        if (canvasGroup != null && animDuration > 0.05f)
        {
            canvasGroup.alpha = 0.5f;
            fadeTween = canvasGroup.DOFade(1f, animDuration).SetUpdate(true);
        }
    }

    public void CloseMenu()
    {
        isOpen = false;
        KillTweens();

        Transform popupTransform = null;
        if (menuPanel != null)
        {
            popupTransform = menuPanel.transform.Find("Bottom_Shop");
            if (popupTransform == null) popupTransform = menuPanel.transform;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (popupTransform != null && animDuration > 0.05f)
        {
            if (canvasGroup != null)
            {
                fadeTween = canvasGroup.DOFade(0f, animDuration * 0.8f).SetUpdate(true);
            }

            scaleTween = popupTransform.DOScale(new Vector3(0.85f, 0.85f, 1f), animDuration * 0.8f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                CheckUnblockDrawing();
                if (menuPanel != null) menuPanel.SetActive(false);
            });
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (menuPanel != null) menuPanel.SetActive(false);
            CheckUnblockDrawing();
        }
    }

    public void HideImmediate()
    {
        isOpen = false;
        KillTweens();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (menuPanel != null)
        {
            menuPanel.transform.localScale = Vector3.one;
            menuPanel.SetActive(false);
        }

        CheckUnblockDrawing();
    }

    private void CheckUnblockDrawing()
    {
        var levelSelect = LevelSelectUI.Instance != null ? LevelSelectUI.Instance : FindFirstObjectByType<LevelSelectUI>();
        if (levelSelect == null || !levelSelect.IsOpen)
        {
            LineDrawer.IsDrawingBlocked = false;
        }
    }

    private void KillTweens()
    {
        if (scaleTween != null && scaleTween.IsActive()) scaleTween.Kill();
        if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();
    }

    private void OnDestroy()
    {
        if (btnOpenCarShop != null) btnOpenCarShop.onClick.RemoveListener(OpenMenu);
        if (btnClose != null) btnClose.onClick.RemoveListener(CloseMenu);
        KillTweens();
    }
}
