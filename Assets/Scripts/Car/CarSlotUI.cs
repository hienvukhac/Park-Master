using System;
using UnityEngine;
using UnityEngine.UI;

public class CarSlotUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image carIconImage;
    [SerializeField] private Image lockIconImage;
    [SerializeField] private Button slotButton;

    [Header("Card Background Sprites (Tùy chọn)")]
    [SerializeField] private Sprite normalCardSprite;
    [SerializeField] private Sprite selectedCardSprite;

    [Header("Color")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.02f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    private CarData currentCarData;
    private Action<int> onSelectCallback;

    private void Awake()
    {
        if (cardBackground == null)
            cardBackground = this.GetComponent<Image>();
        if (slotButton == null)
            slotButton = this.GetComponent<Button>();

        if (carIconImage == null)
        {
            Transform iconTrans = transform.Find("Icon_Car") ?? transform.Find("CarIcon") ?? transform.Find("Icon");
            if (iconTrans != null)
            {
                carIconImage = iconTrans.GetComponent<Image>();
            }
            else
            {
                Image[] imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img != cardBackground && img != lockIconImage)
                    {
                        carIconImage = img;
                        break;
                    }
                }
            }
        }

        if (lockIconImage == null)
        {
            Transform lockTrans = transform.Find("LockIcon") ?? transform.Find("Icon_Lock") ?? transform.Find("Lock");
            if (lockTrans != null)
            {
                lockIconImage = lockTrans.GetComponent<Image>();
            }
        }

        if (carIconImage != null) carIconImage.raycastTarget = false;
        if (lockIconImage != null) lockIconImage.raycastTarget = false;
    }

    public void Setup(CarData data, bool isSelected, Sprite lockedSprite, Action<int> onSelect)
    {
        currentCarData = data;
        onSelectCallback = onSelect;

        if (carIconImage == null || cardBackground == null)
        {
            Awake();
        }

        bool isUnlocked = data.IsUnlocked();

        if (isUnlocked)
        {
            if (carIconImage != null)
            {
                carIconImage.gameObject.SetActive(true);

                if (data.Icon != null)
                {
                    carIconImage.sprite = data.Icon;
                    carIconImage.color = Color.white;
                }
                else
                {
                    carIconImage.color = data.CarColor;
                }
            }

            if (lockIconImage != null)
            {
                lockIconImage.gameObject.SetActive(false);
            }
        }
        else
        {
            Sprite lockSp = lockedSprite;
            if (lockSp == null)
            {
                lockSp = Resources.Load<Sprite>("car-lock-icon");
#if UNITY_EDITOR
                if (lockSp == null)
                {
                    lockSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Icons/car-lọck-icon.png");
                }
#endif
            }

            if (carIconImage != null)
            {
                carIconImage.gameObject.SetActive(true);
                if (lockSp != null)
                {
                    carIconImage.sprite = lockSp;
                    carIconImage.color = Color.white;
                }
                else
                {
                    carIconImage.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                }
            }

            if (lockIconImage != null)
            {
                lockIconImage.gameObject.SetActive(true);
                if (lockSp != null)
                {
                    lockIconImage.sprite = lockSp;
                    lockIconImage.color = Color.white;
                }
            }
        }

        if (cardBackground != null)
        {
            cardBackground.color = isSelected ? selectedColor : normalColor;
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() =>
            {
                if (isUnlocked)
                {
                    onSelectCallback?.Invoke(data.Id);
                }
                else
                {
                    Debug.Log($"Xe '{data.Name}' đang bị khóa");
                }
            });
        }
    }
}