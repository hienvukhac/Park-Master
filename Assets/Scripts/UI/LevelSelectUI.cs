using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LevelSelectUI : MonoBehaviour
{
    public static LevelSelectUI Instance { get; private set; }

    [SerializeField] private GameObject levelSelectedPanel;
    [SerializeField] private Transform popupTransform;

    [SerializeField] private Button btnMenuLevel;
    [SerializeField] private Button btnCloseLevel;

    [SerializeField] private float animDuration = 0.25f;

    private CanvasGroup canvasGroup;
    private Tween scaleTween;
    private Tween fadeTween;
    private bool isOpen = false;
    public bool IsOpen => isOpen;

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

    private void Start()
    {
        SetupButtons();
    }

    private void SetupComponents()
    {
        if (levelSelectedPanel == null)
            levelSelectedPanel = this.gameObject;

        if (popupTransform == null && levelSelectedPanel != null)
        {
            Transform found = levelSelectedPanel.transform.Find("Popup_Window")
                           ?? levelSelectedPanel.transform.Find("Popup")
                           ?? levelSelectedPanel.transform.Find("Window");
            popupTransform = found != null ? found : levelSelectedPanel.transform;
        }

        if (levelSelectedPanel != null)
        {
            canvasGroup = levelSelectedPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = levelSelectedPanel.AddComponent<CanvasGroup>();
        }
    }

    public void SetupButtons()
    {
        if (btnMenuLevel != null)
        {
            btnMenuLevel.onClick.RemoveListener(OpenMenu);
            btnMenuLevel.onClick.AddListener(OpenMenu);
        }
        else
        {
            Debug.LogWarning("[LevelSelectUI] Chưa gán 'Btn Menu Level' trong Inspector!");
        }

        if (btnCloseLevel != null)
        {
            btnCloseLevel.onClick.RemoveListener(CloseMenu);
            btnCloseLevel.onClick.AddListener(CloseMenu);
        }
        else
        {
            Debug.LogWarning("[LevelSelectUI] Chưa gán 'Btn Close Level' trong Inspector!");
        }
    }

    public void OpenLevelSelect() => OpenMenu();
    public void CloseLevelSelect() => CloseMenu();

    public void OpenMenu()
    {
        LineDrawer.IsDrawingBlocked = true;
        isOpen = true;
        KillTweens();

        if (levelSelectedPanel != null)
        {
            levelSelectedPanel.SetActive(true);
            levelSelectedPanel.transform.SetAsLastSibling();
            levelSelectedPanel.transform.localScale = Vector3.one;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
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

        var scrollMgr = GetComponentInChildren<LevelScrollManager>(true)
                     ?? FindFirstObjectByType<LevelScrollManager>(FindObjectsInactive.Include);
        if (scrollMgr != null) scrollMgr.RefreshCards();
    }

    public void CloseMenu()
    {
        isOpen = false;
        KillTweens();

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
                if (levelSelectedPanel != null) levelSelectedPanel.SetActive(false);
            });
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (levelSelectedPanel != null) levelSelectedPanel.SetActive(false);
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

        if (levelSelectedPanel != null)
        {
            levelSelectedPanel.SetActive(false);
        }

        CheckUnblockDrawing();
    }

    private void CheckUnblockDrawing()
    {
        var carMenu = FindFirstObjectByType<CarMenuUI>();
        if (carMenu == null || !carMenu.IsOpen)
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
        if (btnMenuLevel != null) btnMenuLevel.onClick.RemoveListener(OpenMenu);
        if (btnCloseLevel != null) btnCloseLevel.onClick.RemoveListener(CloseMenu);
        KillTweens();
    }
}
