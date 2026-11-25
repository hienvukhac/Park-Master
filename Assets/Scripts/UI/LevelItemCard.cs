using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum LevelCardState
{
    Completed,
    Current,
    Locked
}

public class LevelItemCard : MonoBehaviour
{
    [Header("References — Có thể kéo thả hoặc để trống tự tìm")]
    [SerializeField] private Image           imgPreview;
    [SerializeField] private TextMeshProUGUI txtLevelName;
    [SerializeField] private GameObject      goStarsGroup;
    [SerializeField] private Image[]         starImages;
    [SerializeField] private GameObject      goLengthRow;
    [SerializeField] private TextMeshProUGUI txtLength;
    [SerializeField] private TextMeshProUGUI txtLengthValue;
    [SerializeField] private Button          btnPlay;

    [Header("Overlays từ Prefab")]
    [Tooltip("Object dấu '?' bên trong PreviewImage hoặc root card")]
    [SerializeField] private GameObject      questionOverlay;
    [Tooltip("Object chữ hoặc badge 'NEW'")]
    [SerializeField] private GameObject      newBadge;

    [Header("Star Sprites (tùy chọn)")]
    [SerializeField] private Sprite starGoldSprite;
    [SerializeField] private Sprite starGraySprite;

    private int            _idx;
    private LevelCardState _state;

    private void Awake()
    {
        AutoFindReferences();
    }

    public void Setup(int levelIndex, LevelCardState state,
                      int starsEarned = 0, float recordLength = 0f,
                      Sprite mapSprite = null)
    {
        _idx   = levelIndex;
        _state = state;

        ApplyBackground(state);
        ApplyThumbnail(state, mapSprite);
        ApplyLevelName(state, levelIndex);
        ApplyStars(state, starsEarned);
        ApplyLengthRow(state, recordLength);
        ApplyOverlays(state);
        ApplyButton(state);
        ApplyAnimation(state);
    }

    private void ApplyBackground(LevelCardState state)
    {
        var bg = GetComponent<Image>();
        if (bg == null) return;
        bg.color = state switch
        {
            LevelCardState.Completed => Color.white,
            LevelCardState.Current   => Color.white,
            LevelCardState.Locked    => new Color(0.6f, 0.6f, 0.6f),
            _                        => Color.white
        };
    }

    private void ApplyThumbnail(LevelCardState state, Sprite mapSprite)
    {
        if (imgPreview == null) return;

        if (mapSprite != null)
            imgPreview.sprite = mapSprite;

        imgPreview.color = state == LevelCardState.Completed
            ? Color.white
            : new Color(0.25f, 0.25f, 0.25f, 1f);
    }

    private void ApplyLevelName(LevelCardState state, int levelIndex)
    {
        if (txtLevelName == null) return;

        txtLevelName.gameObject.SetActive(state != LevelCardState.Locked);

        if (state != LevelCardState.Locked)
            txtLevelName.text = $"LEVEL {levelIndex}";
    }

    private void ApplyStars(LevelCardState state, int earned)
    {
        if (goStarsGroup == null) return;

        goStarsGroup.SetActive(state != LevelCardState.Locked);

        if (starImages == null) return;
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;

            bool filled = (state == LevelCardState.Completed) && (i < earned);

            if (starGoldSprite != null && starGraySprite != null)
            {
                starImages[i].sprite = filled ? starGoldSprite : starGraySprite;
                starImages[i].color  = Color.white;
            }
            else
            {

                starImages[i].color = filled
                    ? new Color(1f, 0.85f, 0f)
                    : new Color(0.35f, 0.32f, 0.32f, 0.6f);
            }
        }
    }

    private void ApplyLengthRow(LevelCardState state, float recordLength)
    {
        if (goLengthRow == null) return;

        goLengthRow.SetActive(state != LevelCardState.Locked);

        if (state == LevelCardState.Locked) return;

        if (txtLength != null)
            txtLength.color = state == LevelCardState.Completed
                ? new Color(0.85f, 0.1f, 0.1f)
                : Color.black;

        if (txtLengthValue != null)
        {
            if (state == LevelCardState.Completed && recordLength > 0f)
            {
                txtLengthValue.text  = $"{recordLength:0.#}m";
                txtLengthValue.color = new Color(0.85f, 0.1f, 0.1f);
            }
            else
            {
                txtLengthValue.text  = "-- m";
                txtLengthValue.color = Color.black;
            }
        }
    }

    private void ApplyOverlays(LevelCardState state)
    {

        if (questionOverlay != null)
            questionOverlay.SetActive(state != LevelCardState.Completed);

        if (newBadge != null)
            newBadge.SetActive(state == LevelCardState.Current);
    }

    private void ApplyButton(LevelCardState state)
    {
        if (btnPlay == null) return;
        btnPlay.interactable = state != LevelCardState.Locked;
        btnPlay.onClick.RemoveAllListeners();
        if (state != LevelCardState.Locked)
            btnPlay.onClick.AddListener(OnClickPlay);
    }

    private void ApplyAnimation(LevelCardState state)
    {
        DOTween.Kill(transform);
        transform.localScale = Vector3.one;

        if (state == LevelCardState.Current)
        {
            transform.DOScale(new Vector3(1.03f, 1.03f, 1f), 0.6f)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetEase(Ease.InOutSine)
                     .SetUpdate(true);
        }
    }

    private void OnClickPlay()
    {
        DOTween.Kill(transform);
        transform.localScale = Vector3.one;

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadLevel(_idx);

        if (LevelSelectUI.Instance != null)
            LevelSelectUI.Instance.CloseLevelSelect();
    }

    private void AutoFindReferences()
    {
        if (imgPreview == null)
        {
            var t = transform.Find("PreviewImage");
            if (t) imgPreview = t.GetComponent<Image>();
        }

        if (txtLevelName == null)
        {
            var t = transform.Find("TextLevelName");
            if (t) txtLevelName = t.GetComponent<TextMeshProUGUI>();
        }

        if (goStarsGroup == null)
        {
            var t = transform.Find("StartsGroup") ?? transform.Find("StarsGroup");
            if (t) goStarsGroup = t.gameObject;
        }

        if ((starImages == null || starImages.Length == 0) && goStarsGroup != null)
        {
            var tStars = goStarsGroup.transform;
            starImages = new Image[3];
            for (int i = 1; i <= 3; i++)
            {
                var s = tStars.Find($"Start {i}") ?? tStars.Find($"Star {i}");
                if (s) starImages[i - 1] = s.GetComponent<Image>();
            }
        }

        if (goLengthRow == null)
        {
            var t = transform.Find("TextLengthName");
            if (t)
            {
                goLengthRow = t.gameObject;
                txtLength   = t.GetComponent<TextMeshProUGUI>();

                var vt = t.Find("Value");
                if (vt) txtLengthValue = vt.GetComponent<TextMeshProUGUI>();
            }
        }

        if (btnPlay == null)
            btnPlay = GetComponent<Button>();

        if (questionOverlay == null)
        {
            if (imgPreview != null)
            {
                var q = imgPreview.transform.Find("QuestionMark") ?? imgPreview.transform.Find("_QuestionMark");
                if (q != null) questionOverlay = q.gameObject;
            }

            if (questionOverlay == null)
            {
                var q = transform.Find("QuestionMark") ?? transform.Find("_QuestionMark");
                if (q != null) questionOverlay = q.gameObject;
            }
        }

        if (newBadge == null)
        {
            var nb = transform.Find("NewBadge") ?? transform.Find("_NewBadge");
            if (nb != null) newBadge = nb.gameObject;
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}
