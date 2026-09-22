using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public enum GamePlayState
{
    Drawing,     
    Driving,    
    Victory,     
    Failed       
}

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    private static bool isApplicationQuitting = false;
    public static bool IsApplicationQuitting => isApplicationQuitting;
    public static bool HasInstance => !isApplicationQuitting && instance != null;

    public static GameManager Instance
    {
        get
        {
            if (isApplicationQuitting) return null;

            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
            }
            return instance;
        }
    }

    [Header("Trạng thái game")]
    [SerializeField] private GamePlayState currentState = GamePlayState.Drawing;
    public GamePlayState CurrentState => currentState;
    public void SetState(GamePlayState state) => currentState = state;

    [Header("Car Database & Prefab Mặc Định")]
    [SerializeField] private CarDatabase carDatabase;
    [SerializeField] private GameObject defaultCarPrefab;
    public CarDatabase CarDatabase => carDatabase;
    public GameObject DefaultCarPrefab => defaultCarPrefab;

    [Header("Danh sách đối tượng trong Level")]
    public List<CarController> allCars = new List<CarController>();
    public List<ParkingSlot> allSlots = new List<ParkingSlot>();

    public event Action OnLevelVictoryEvent;
    public event Action OnLevelFailedEvent;
    public event Action OnLevelResetEvent;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

#if UNITY_STANDALONE
        Screen.SetResolution(540, 960, FullScreenMode.Windowed);
#endif

        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        EnsureCanvasScaler();
        EnsureLevelManager();
    }

    private void EnsureCanvasScaler()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
        {
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f; 
        }
    }

    private void EnsureLevelManager()
    {
        if (LevelManager.Instance == null)
        {
            LevelManager lm = FindFirstObjectByType<LevelManager>();
            if (lm == null)
            {
                gameObject.AddComponent<LevelManager>();
            }
        }
    }

    public void RefreshLevelObjects()
    {
        OnLevelLoaded(null);
    }

    public void OnLevelLoaded(GameObject levelObj = null)
    {
        currentState = GamePlayState.Drawing;

        if (LevelCompleteUI.Instance != null)
        {
            LevelCompleteUI.Instance.HideImmediate();
        }

        CarController[] existingCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (var c in existingCars)
        {
            if (c != null && c.GetComponentInParent<Canvas>() == null)
            {
                Destroy(c.gameObject);
            }
        }
        allCars.Clear();

        ParkingSlot[] foundSlots = (levelObj != null) 
            ? levelObj.GetComponentsInChildren<ParkingSlot>(true) 
            : FindObjectsByType<ParkingSlot>(FindObjectsSortMode.None);
        allSlots = new List<ParkingSlot>(foundSlots);

        List<CarStartPoint> startPoints = new List<CarStartPoint>();
        CarStartPoint[] foundSp = (levelObj != null)
            ? levelObj.GetComponentsInChildren<CarStartPoint>(true)
            : FindObjectsByType<CarStartPoint>(FindObjectsSortMode.None);

        if (foundSp != null && foundSp.Length > 0)
        {
            foreach (var sp in foundSp)
            {
                if (sp == null) continue;
                CarStartPoint[] childSps = sp.GetComponentsInChildren<CarStartPoint>(true);
                if (childSps.Length > 1 && childSps[0] == sp)
                {
                    continue;
                }

                if (!startPoints.Contains(sp))
                {
                    startPoints.Add(sp);
                }
            }
        }

        if (startPoints.Count == 0)
        {
            Transform[] allTrans = (levelObj != null)
                ? levelObj.GetComponentsInChildren<Transform>(true)
                : FindObjectsByType<Transform>(FindObjectsSortMode.None);

            foreach (var t in allTrans)
            {
                if (t == null) continue;
                if (t.childCount > 0 && t.name.ToUpper().Contains("STARTPOINTS")) continue;

                string n = t.name.ToLower();
                if ((n.Contains("car") && (n.Contains("start") || n.Contains("point") || n.Contains("spawn"))) || n.Contains("startpoint") || n.Contains("pointstart"))
                {
                    CarStartPoint autoSp = t.GetComponent<CarStartPoint>();
                    if (autoSp == null) autoSp = t.gameObject.AddComponent<CarStartPoint>();
                    if (!startPoints.Contains(autoSp)) startPoints.Add(autoSp);
                }
            }
        }

        List<CarStartPoint> uniqueStartPoints = new List<CarStartPoint>();
        foreach (var sp in startPoints)
        {
            if (sp == null) continue;
            bool duplicate = false;
            foreach (var u in uniqueStartPoints)
            {
                if (Vector3.Distance(sp.transform.position, u.transform.position) < 0.3f)
                {
                    duplicate = true;
                    break;
                }
            }
            if (!duplicate)
            {
                uniqueStartPoints.Add(sp);
            }
        }
        startPoints = uniqueStartPoints;

        PairStartPointsWithSlots(startPoints, allSlots);

        if (startPoints.Count > 0)
        {
            Transform carsParent = levelObj != null ? levelObj.transform : this.transform;
            CarDatabase db = GetActiveCarDatabase();

            int selectedId = PlayerPrefs.GetInt("SelectedCarID", 1001);
            CarData playerCarData = (db != null && db.CarList != null && db.CarList.Count > 0)
                                  ? (db.CarList.Find(c => c.Id == selectedId) ?? db.CarList[0])
                                  : null;

            for (int i = 0; i < startPoints.Count; i++)
            {
                CarStartPoint sp = startPoints[i];
                if (sp == null) continue;

                GameObject prefabToSpawn = null;
                string carName = $"Car_{i + 1}_{sp.colorType}";

                if (playerCarData != null && playerCarData.Car3dPrefab != null)
                {
                    prefabToSpawn = playerCarData.Car3dPrefab;
                    carName = $"{playerCarData.Name}_{sp.colorType}";
                }
                else if (db != null && db.CarList != null && db.CarList.Count > 0 && db.CarList[0].Car3dPrefab != null)
                {
                    prefabToSpawn = db.CarList[0].Car3dPrefab;
                    carName = $"{db.CarList[0].Name}_{sp.colorType}";
                }

                if (prefabToSpawn == null)
                {
                    prefabToSpawn = defaultCarPrefab 
                                 ?? Resources.Load<GameObject>("DefaultCar") 
                                 ?? Resources.Load<GameObject>("Audi R8 Lievery 1")
                                 ?? Resources.Load<GameObject>("Audi R8");
                }

#if UNITY_EDITOR
                if (prefabToSpawn == null)
                {
                    prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CarS/Audi R8 Lievery 1.prefab")
                                 ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CarS/Audi R8.prefab")
                                 ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Cars/Audi R8/Models/Audi R8.fbx");
                }
#endif

                if (prefabToSpawn != null)
                {
                    Vector3 spawnPos = sp.transform.position;

                    GameObject carObj = Instantiate(prefabToSpawn, carsParent);
                    carObj.name = carName;
                    carObj.transform.position = spawnPos;
                    carObj.transform.rotation = sp.transform.rotation;

                    Vector3 targetScale = prefabToSpawn.transform.localScale;
                    if (targetScale == Vector3.zero) targetScale = Vector3.one;
                    carObj.transform.localScale = targetScale;
                    SetLayerRecursively(carObj, 3);

                    var carCtrl = carObj.GetComponent<CarController>() ?? carObj.AddComponent<CarController>();

                    if (carCtrl != null)
                    {
                        carCtrl.startPosition = sp.transform.position;
                        carCtrl.startRotation = sp.transform.rotation;
                        carCtrl.SetCarColorType(sp.colorType);

                        sp.car = carCtrl;

                        if (!allCars.Contains(carCtrl))
                        {
                            allCars.Add(carCtrl);
                        }
                    }
                }
            }

            Debug.Log($"<color=#00FF7F>[GameManager] Đã sinh {allCars.Count} xe cùng loại '{playerCarData?.Name}' cho {startPoints.Count} điểm xuất phát!</color>");
        }
    }

    private void PairStartPointsWithSlots(List<CarStartPoint> startPoints, List<ParkingSlot> slots)
    {
        if (startPoints == null || startPoints.Count == 0 || slots == null || slots.Count == 0) return;

        HashSet<ParkingSlot> pairedSlots = new HashSet<ParkingSlot>();
        HashSet<CarStartPoint> pairedPoints = new HashSet<CarStartPoint>();

        foreach (var sp in startPoints)
        {
            if (sp == null || sp.pairId <= 0) continue;
            foreach (var slot in slots)
            {
                if (slot == null || pairedSlots.Contains(slot)) continue;
                if (slot.PairId == sp.pairId)
                {
                    sp.colorType = slot.SlotColorType;
                    pairedPoints.Add(sp);
                    pairedSlots.Add(slot);
                    break;
                }
            }
        }

        foreach (var sp in startPoints)
        {
            if (sp == null || pairedPoints.Contains(sp) || sp.colorType == ColorType.None) continue;
            foreach (var slot in slots)
            {
                if (slot == null || pairedSlots.Contains(slot)) continue;
                if (slot.SlotColorType == sp.colorType)
                {
                    pairedPoints.Add(sp);
                    pairedSlots.Add(slot);
                    break;
                }
            }
        }

        List<ParkingSlot> remainingSlots = new List<ParkingSlot>();
        foreach (var slot in slots)
        {
            if (slot != null && !pairedSlots.Contains(slot)) remainingSlots.Add(slot);
        }

        int slotIdx = 0;
        foreach (var sp in startPoints)
        {
            if (sp == null || pairedPoints.Contains(sp)) continue;
            if (slotIdx < remainingSlots.Count)
            {
                var slot = remainingSlots[slotIdx++];
                sp.colorType = slot.SlotColorType;
                pairedPoints.Add(sp);
                pairedSlots.Add(slot);
            }
            else
            {
                if (sp.colorType == ColorType.None)
                {
                    sp.colorType = (ColorType)((startPoints.IndexOf(sp) % 8) + 1);
                }
            }
        }
    }

    public CarDatabase GetActiveCarDatabase()
    {
        if (carDatabase != null) return carDatabase;
        if (CarShopManager.Instance != null && CarShopManager.Instance.CarDatabase != null)
            return CarShopManager.Instance.CarDatabase;
        return Resources.Load<CarDatabase>("CarDatabase");
    }

    public void UpdateEquippedCar(int carId)
    {
        CarDatabase db = GetActiveCarDatabase();
        CarData carData = db != null ? db.GetCarById(carId) : null;
        if (carData == null || carData.Car3dPrefab == null) return;

        if (allCars != null && allCars.Count > 0)
        {
            List<CarController> oldCarsList = new List<CarController>(allCars);
            allCars.Clear();

            foreach (var oldCar in oldCarsList)
            {
                if (oldCar == null) continue;

                Vector3 worldPos = oldCar.transform.position;
                Quaternion worldRot = oldCar.transform.rotation;
                Transform parent = oldCar.transform.parent;

                Vector3 savedStartPos = oldCar.startPosition;
                Quaternion savedStartRot = oldCar.startRotation;
                List<Vector3> savedPath = new List<Vector3>(oldCar.pathPoints);
                bool savedIsParked = oldCar.isParked;
                ParkingSlot savedSlot = oldCar.currentSlot;
                ColorType targetColor = oldCar.CarColorType;

                Destroy(oldCar.gameObject);

                GameObject newCarObj = Instantiate(carData.Car3dPrefab, parent);
                newCarObj.name = $"{carData.Name}_{targetColor}";
                newCarObj.transform.position = worldPos;
                newCarObj.transform.rotation = worldRot;

                Vector3 prefabScale = carData.Car3dPrefab.transform.localScale;
                if (prefabScale == Vector3.zero) prefabScale = Vector3.one;
                newCarObj.transform.localScale = prefabScale;
                SetLayerRecursively(newCarObj, 3);

                CarController newCtrl = newCarObj.GetComponent<CarController>() ?? newCarObj.AddComponent<CarController>();

                if (newCtrl != null)
                {
                    newCtrl.startPosition = savedStartPos;
                    newCtrl.startRotation = savedStartRot;
                    newCtrl.isParked = savedIsParked;
                    newCtrl.currentSlot = savedSlot;

                    if (savedSlot != null && savedSlot.currentParkedCar == oldCar)
                    {
                        savedSlot.currentParkedCar = newCtrl;
                    }

                    CarStartPoint[] allSp = FindObjectsByType<CarStartPoint>(FindObjectsSortMode.None);
                    foreach (var sp in allSp)
                    {
                        if (sp != null && (sp.car == oldCar || Vector3.Distance(sp.transform.position, savedStartPos) < 0.5f))
                        {
                            sp.car = newCtrl;
                            if (sp.colorType != ColorType.None)
                            {
                                targetColor = sp.colorType;
                            }
                        }
                    }

                    newCtrl.SetCarColorType(targetColor);

                    if (savedPath.Count > 0)
                    {
                        newCtrl.pathPoints = new List<Vector3>(savedPath);
                        Vector3 lastP = savedPath[savedPath.Count - 1];
                        newCtrl.pathPoints.RemoveAt(savedPath.Count - 1);
                        newCtrl.AddPoint(lastP, savedPath[0].y);
                    }

                    if (!allCars.Contains(newCtrl))
                    {
                        allCars.Add(newCtrl);
                    }
                }
            }

            Debug.Log($"<color=#00FF7F>[GameManager] Đã đồng bộ tất cả {allCars.Count} xe trong màn sang cùng loại xe '{carData.Name}'</color>");
            return;
        }

        RefreshLevelObjects();
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void RegisterCar(CarController car)
    {
        if (car == null) return;
        if (car.GetComponentInParent<Canvas>() != null) return;

        if (!allCars.Contains(car))
        {
            allCars.Add(car);
        }
    }

    public void UnregisterCar(CarController car)
    {
        if (car != null && allCars.Contains(car))
        {
            allCars.Remove(car);
        }
    }

    public void RegisterSlot(ParkingSlot slot)
    {
        if (slot != null && !allSlots.Contains(slot))
        {
            allSlots.Add(slot);
        }
    }

    public void UnregisterSlot(ParkingSlot slot)
    {
        if (slot != null && allSlots.Contains(slot))
        {
            allSlots.Remove(slot);
        }
    }

    public void OnCarParked(CarController car)
    {
        if (currentState == GamePlayState.Victory) return;

        allCars.RemoveAll(c => c == null || !c.gameObject.activeInHierarchy || c.GetComponentInParent<Canvas>() != null);

        bool allParked = true;
        foreach (var c in allCars)
        {
            if (c != null && !c.isParked)
            {
                allParked = false;
                break;
            }
        }

        Debug.Log($"<color=yellow>[GameManager] OnCarParked: Xe '{car.name}' đã đỗ thành công. Tổng số xe trong màn: {allCars.Count}, Tất cả đã đỗ: {allParked}</color>");

        if (allParked && allCars.Count > 0)
        {
            TriggerVictory();
        }
    }

    private void TriggerVictory()
    {
        if (currentState == GamePlayState.Victory) return;
        currentState = GamePlayState.Victory;
        Debug.Log("<color=#00FF7F>🏆🎉 LEVEL CLEARED! TẤT CẢ XE ĐÃ VỀ ĐÍCH THÀNH CÔNG! 🎉🏆</color>");
        OnLevelVictoryEvent?.Invoke();
    }

    public void OnCarCrashed(CarController car1, CarController car2 = null)
    {
        if (currentState == GamePlayState.Victory || currentState == GamePlayState.Failed) return;

        currentState = GamePlayState.Failed;
        Debug.Log("<color=#FF4444>💥 TAI NẠN XE! MÀN CHƠI THẤT BẠI! Chạm vào 1 trong các vị trí xuất phát để thử lại 💥</color>");

        if (Camera.main != null)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(0.35f, strength: 0.35f, vibrato: 20, randomness: 90f);
        }

        OnLevelFailedEvent?.Invoke();
    }

    public void StartAllCars()
    {
        currentState = GamePlayState.Driving;
        foreach (var car in allCars)
        {
            if (car != null && car.pathPoints.Count > 1)
            {
                car.StartDriving();
            }
        }
    }

    public void ResetAllCarsAfterCrash()
    {
        ResetAllCars();
    }

    public void ResetAllCars()
    {
        currentState = GamePlayState.Drawing;
        foreach (var car in allCars)
        {
            if (car != null)
            {
                car.ClearPath();
                car.ResetToStartSmooth();
            }
        }
        OnLevelResetEvent?.Invoke();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartAllCars();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}