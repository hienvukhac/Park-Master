using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarShopManager : MonoBehaviour
{
    public static CarShopManager Instance { get; private set; }

    [Header("Dữ liệu xe")]
    [SerializeField] private CarDatabase carDatabase;
    public CarDatabase CarDatabase => carDatabase;
    [SerializeField] private Sprite lockedCarSprite;
    private List<CarData> carList => carDatabase != null ? carDatabase.CarList : new List<CarData>();

    [Header("Lưới Grid & Prefab")]
    [SerializeField] private Transform gridCarsContainer;   
    [SerializeField] private GameObject carSlotPrefab;     

    [Header("Phân trang (9 xe / trang)")]
    [SerializeField] private Button btnNextPage;
    [SerializeField] private List<Image> pageDots = new List<Image>();
    private int currentPage = 0;
    private const int CARS_PER_PAGE = 9;

    [Header("Nút Mua Ngẫu Nhiên 100 Coin")]
    [SerializeField] private Button btnUnlockRandom;
    [SerializeField] private int unlockPrice = 100;

    private int selectedCarId = 0;
    private readonly List<CarSlotUI> activeSlots = new List<CarSlotUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureDatabase();
    }

    private void Start()
    {
        EnsureDatabase();

        int defaultId = (carList != null && carList.Count > 0) ? carList[0].Id : 1001;
        selectedCarId = PlayerPrefs.GetInt("SelectedCarID", defaultId);

        if (btnNextPage != null)
        {
            btnNextPage.onClick.RemoveListener(NextPage);
            btnNextPage.onClick.AddListener(NextPage);
        }

        if (btnUnlockRandom != null)
        {
            btnUnlockRandom.onClick.RemoveListener(OnUnlockRandomClick);
            btnUnlockRandom.onClick.AddListener(OnUnlockRandomClick);
        }

        RefreshShop();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsureDatabase()
    {
        if (carDatabase == null)
            carDatabase = Resources.Load<CarDatabase>("CarDatabase");

        if (carSlotPrefab == null)
            carSlotPrefab = Resources.Load<GameObject>("CarSlotPrefab");

        if (lockedCarSprite == null)
            lockedCarSprite = Resources.Load<Sprite>("car-lock-icon");
    }

    public void RefreshShop()
    {
        EnsureDatabase();

        if (gridCarsContainer == null)
        {
            Debug.LogWarning("[CarShopManager] Chưa gán gridCarsContainer (Grid_Cars)!");
            return;
        }

        int defaultId = (carList != null && carList.Count > 0) ? carList[0].Id : 1001;
        selectedCarId = PlayerPrefs.GetInt("SelectedCarID", defaultId);

        foreach (Transform child in gridCarsContainer)
        {
            Destroy(child.gameObject);
        }
        activeSlots.Clear();

        int startIndex = currentPage * CARS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + CARS_PER_PAGE, carList.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            CarData car = carList[i];
            if (car == null) continue;

            if (carSlotPrefab != null)
            {
                try
                {
                    GameObject slotObj = Instantiate(carSlotPrefab, gridCarsContainer);
                    CarSlotUI slotUI = slotObj.GetComponent<CarSlotUI>();
                    if (slotUI != null)
                    {
                        bool isSelected = (car.Id == selectedCarId);
                        slotUI.Setup(car, isSelected, lockedCarSprite, OnCarSelected);
                        activeSlots.Add(slotUI);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CarShopManager] Lỗi tạo slot xe {car.Name}: {ex.Message}");
                }
            }
        }

        AutoResizeGridContainer();
        UpdatePageDots();
    }

    private void AutoResizeGridContainer()
    {
        if (gridCarsContainer == null) return;

        RectTransform rt = gridCarsContainer as RectTransform;
        if (rt == null) return;

        GridLayoutGroup glg = gridCarsContainer.GetComponent<GridLayoutGroup>();
        if (glg == null) return;

        int totalSlots = activeSlots.Count;
        if (totalSlots == 0) return;

        int columns = (glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            ? glg.constraintCount
            : Mathf.Max(1, 3);

        int rows = Mathf.CeilToInt((float)totalSlots / columns);
        float neededHeight = glg.padding.top + glg.padding.bottom
                           + rows * glg.cellSize.y
                           + Mathf.Max(0, rows - 1) * glg.spacing.y;

        Vector2 sd = rt.sizeDelta;
        sd.y = neededHeight;
        rt.sizeDelta = sd;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void OnCarSelected(int carId)
    {
        selectedCarId = carId;
        PlayerPrefs.SetInt("SelectedCarID", selectedCarId);
        PlayerPrefs.Save();

        RefreshShop();

        if (GameManager.HasInstance)
        {
            GameManager.Instance.UpdateEquippedCar(carId);
        }
    }

    private void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)carList.Count / CARS_PER_PAGE);
        if (totalPages <= 1) return;

        currentPage++;
        if (currentPage >= totalPages) currentPage = 0;

        RefreshShop();
    }

    private void UpdatePageDots()
    {
        for (int i = 0; i < pageDots.Count; i++)
        {
            if (pageDots[i] != null)
            {
                pageDots[i].color = (i == currentPage) ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
        }
    }

    private void OnUnlockRandomClick()
    {
        if (CoinManager.Instance == null || CoinManager.Instance.TotalCoins < unlockPrice)
        {
            Debug.LogWarning("Không đủ 100 coin để mở xe!");
            return;
        }

        List<CarData> lockedCars = new List<CarData>();
        foreach (var car in carList)
        {
            if (car != null && !car.IsUnlocked()) lockedCars.Add(car);
        }

        if (lockedCars.Count == 0)
        {
            Debug.Log("Bạn đã mở khóa toàn bộ xe trong game!");
            return;
        }

        CoinManager.Instance.AddCoins(-unlockPrice);

        int randomIndex = UnityEngine.Random.Range(0, lockedCars.Count);
        CarData wonCar = lockedCars[randomIndex];

        wonCar.Unlock();
        OnCarSelected(wonCar.Id);

        Debug.Log("🎉 Mở khóa thành công: " + wonCar.Name);
    }
}