using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LineDrawer : MonoBehaviour
{
    [Header("Cấu hình vẽ")]
    [SerializeField] private float minDistance = 0.2f;
    [SerializeField] private bool autoDriveOnRelease = true;

    [Header("Cấu hình giới hạn mặt đường (Tag: Road)")]
    [Tooltip("Khoảng cách đệm từ mép đường để toàn bộ nét vẽ không bị tràn ra bãi cỏ")]
    [SerializeField] private float roadEdgePadding = 0.2f;
    [SerializeField] private BoxCollider roadCollider;

    public static bool IsDrawingBlocked = false;

    private readonly List<Collider> roadColliders = new List<Collider>();
    private readonly RaycastHit[] raycastHitCache = new RaycastHit[32];
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);
    private PointerEventData pointerEventDataCache;

    private Camera mainCamera;
    private CarController currentCar;
    private CarController candidateCar;
    private Vector2 pointerDownScreenPos;
    private bool isDragging;
    private bool isCandidateFromStart;

    private Plane roadPlane;
    private float drawPlaneY = 2.05f;
    private bool isDrawingFromStart;
    private bool hasReachedTarget;
    private ParkingSlot[] allParkingSlots;
    private Collider currentActiveRoadCollider;

    private void Start()
    {
        IsDrawingBlocked = false;
        EnsureCamera();
        RefreshRoadColliders();
        UpdateDrawPlane();
        RefreshParkingSlots();
    }

    private void Update() => HandleDrawing();

    private void EnsureCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
    }

    public void RefreshRoadColliders()
    {
        roadColliders.Clear();

        void AddColliders(GameObject go)
        {
            if (go == null) return;
            foreach (var c in go.GetComponentsInChildren<Collider>())
            {
                if (c != null && c.enabled && !roadColliders.Contains(c))
                    roadColliders.Add(c);
            }
        }

        try
        {
            var roadObjs = GameObject.FindGameObjectsWithTag("Road");
            if (roadObjs != null)
            {
                foreach (var r in roadObjs) AddColliders(r);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LineDrawer] Lỗi quét tag Road: {ex.Message}");
        }

        if (roadColliders.Count == 0 && roadCollider != null && roadCollider.enabled)
            roadColliders.Add(roadCollider);

        if (roadColliders.Count == 0)
            AddColliders(GameObject.Find("Road"));

        if (roadCollider == null && roadColliders.Count > 0)
        {
            foreach (var c in roadColliders)
            {
                if (c is BoxCollider bc) { roadCollider = bc; break; }
            }
        }

        UpdateDrawPlane();
    }

    public void RefreshParkingSlots()
    {
        allParkingSlots = (GameManager.HasInstance && GameManager.Instance.allSlots != null)
            ? GameManager.Instance.allSlots.ToArray()
            : FindObjectsByType<ParkingSlot>(FindObjectsSortMode.None);
    }

    private void UpdateDrawPlane()
    {
        float roadTopY = 1.96f;

        if (roadColliders.Count > 0)
        {
            float maxY = float.MinValue;
            foreach (var col in roadColliders)
            {
                if (col != null) maxY = Mathf.Max(maxY, col.bounds.max.y);
            }
            if (maxY > float.MinValue) roadTopY = maxY;
        }
        else if (GameManager.HasInstance && GameManager.Instance.allCars != null && GameManager.Instance.allCars.Count > 0)
        {
            var firstCar = GameManager.Instance.allCars[0];
            if (firstCar != null) roadTopY = firstCar.transform.position.y;
        }

        drawPlaneY = roadTopY + 0.08f;
        roadPlane = new Plane(Vector3.up, new Vector3(0f, drawPlaneY, 0f));
    }

    private static float SqrDistXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private List<CarController> GetAllCars() =>
        GameManager.HasInstance && GameManager.Instance.allCars != null
            ? GameManager.Instance.allCars
            : new List<CarController>(FindObjectsByType<CarController>(FindObjectsSortMode.None));

    private void HandleDrawing()
    {
        if (IsDrawingBlocked) return;
        EnsureCamera();
        if (mainCamera == null) return;

        if (GetPointerDown(out Vector2 startPos))
            HandlePointerDown(startPos);
        else if (GetPointerHeld(out Vector2 heldPos) && (currentCar != null || candidateCar != null))
            HandlePointerHeld(heldPos);
        else if (GetPointerUp(out _))
            HandlePointerUp();
    }

    private void HandlePointerDown(Vector2 startMousePos)
    {
        if (IsPointerOverInteractiveUI(startMousePos)) return;

        hasReachedTarget = false;
        isDragging = false;
        currentCar = null;
        candidateCar = null;
        UpdateDrawPlane();

        var allCars = GetAllCars();
        bool anyCarCrashed = allCars.Exists(c => c != null && c.isCrashed);
        Ray ray = mainCamera.ScreenPointToRay(startMousePos);

        if (anyCarCrashed || (GameManager.HasInstance && GameManager.Instance.CurrentState == GamePlayState.Failed))
        {
            HandleCrashResetRay(ray, allCars);
            return;
        }

        CarController targetCar = null;
        bool clickedOnStartRing = false;

        int hitCount = Physics.RaycastNonAlloc(ray, raycastHitCache, 200f);
        for (int i = 0; i < hitCount; i++)
        {
            var col = raycastHitCache[i].collider;
            var sp = col.GetComponentInParent<CarStartPoint>();
            if (sp != null && sp.car != null)
            {
                targetCar = sp.car;
                clickedOnStartRing = true;
                break;
            }

            var cc = col.GetComponentInParent<CarController>();
            if (cc != null)
            {
                targetCar = cc;
                break;
            }
        }

        if (targetCar == null && roadPlane.Raycast(ray, out float enterDist))
        {
            Vector3 hitPoint = ray.GetPoint(enterDist);
            const float startRingRadiusSqr = 1.35f * 1.35f;
            const float carBodyRadiusSqr = 1.45f * 1.45f;

            float bestDistSqr = float.MaxValue;
            foreach (var car in allCars)
            {
                if (car == null) continue;
                float dRing = SqrDistXZ(hitPoint, car.startPosition);
                float dCar = SqrDistXZ(hitPoint, car.transform.position);

                if (dRing <= startRingRadiusSqr && dRing < bestDistSqr)
                {
                    bestDistSqr = dRing;
                    targetCar = car;
                    clickedOnStartRing = true;
                }
                if (dCar <= carBodyRadiusSqr && dCar < bestDistSqr)
                {
                    bestDistSqr = dCar;
                    targetCar = car;
                    clickedOnStartRing = false;
                }
            }
        }

        if (targetCar == null) return;

        candidateCar = targetCar;
        pointerDownScreenPos = startMousePos;
        isCandidateFromStart = clickedOnStartRing || ((targetCar.transform.position - targetCar.startPosition).sqrMagnitude < 0.36f);
    }

    private void StartDrawingCar(CarController car, bool fromStartPos)
    {
        currentCar = car;
        currentCar.isDrawingPath = true;
        isDrawingFromStart = fromStartPos;
        currentActiveRoadCollider = GetRoadColliderAt(car.transform.position) ?? GetNearestRoadCollider(car.transform.position);

        currentCar.ClearPath();
        currentCar.ApplyBodyColorToLine();
        Vector3 startPt = fromStartPos ? car.startPosition : car.transform.position;
        currentCar.AddPoint(startPt, drawPlaneY);
    }

    private void HandleCrashResetRay(Ray crashRay, List<CarController> allCars)
    {
        bool hitStartPoint = false;
        int count = Physics.RaycastNonAlloc(crashRay, raycastHitCache, 200f);
        for (int i = 0; i < count; i++)
        {
            var col = raycastHitCache[i].collider;
            if (col.GetComponentInParent<CarStartPoint>() != null || col.GetComponentInParent<StartPointRing>() != null)
            {
                hitStartPoint = true;
                break;
            }
        }

        if (!hitStartPoint && roadPlane.Raycast(crashRay, out float crashEnterDist))
        {
            Vector3 hitPoint = crashRay.GetPoint(crashEnterDist);
            const float crashThresholdSqr = 0.85f * 0.85f;
            foreach (var c in allCars)
            {
                if (c != null && SqrDistXZ(hitPoint, c.startPosition) <= crashThresholdSqr)
                {
                    hitStartPoint = true;
                    break;
                }
            }
        }

        if (hitStartPoint)
        {
            if (GameManager.HasInstance)
                GameManager.Instance.ResetAllCarsAfterCrash();
            else
                foreach (var c in allCars) { if (c != null) { c.ClearPath(); c.ResetToStartSmooth(); } }
            currentCar = null;
            candidateCar = null;
            isDragging = false;
        }
    }

    private void HandlePointerHeld(Vector2 currentMousePos)
    {
        if (hasReachedTarget) return;

        if (!isDragging)
        {
            if (candidateCar == null) return;

            if (Vector2.Distance(currentMousePos, pointerDownScreenPos) < 8f)
                return;

            isDragging = true;
            currentCar = candidateCar;
            candidateCar = null;

            if (isCandidateFromStart)
            {
                StartDrawingCar(currentCar, true);
            }
            else
            {
                currentCar.isDrawingPath = true;
                currentActiveRoadCollider = GetRoadColliderAt(currentCar.transform.position) ?? GetNearestRoadCollider(currentCar.transform.position);
                isDrawingFromStart = false;
                currentCar.ReleaseFromSlot();
                currentCar.ApplyBodyColorToLine();

                if (currentCar.pathPoints.Count == 0)
                    currentCar.AddPoint(currentCar.transform.position, drawPlaneY);
                else
                    currentCar.pathPoints[currentCar.pathPoints.Count - 1] = new Vector3(currentCar.transform.position.x, drawPlaneY, currentCar.transform.position.z);
            }
        }

        if (currentCar == null) return;

        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
        if (!roadPlane.Raycast(ray, out float enterDistance)) return;

        Vector3 rawHitPoint = ray.GetPoint(enterDistance);
        Collider hitRoadCol = GetRoadColliderAt(rawHitPoint);
        Vector3 roadPoint;

        if (hitRoadCol != null)
        {
            if (currentActiveRoadCollider == null || hitRoadCol == currentActiveRoadCollider)
            {
                currentActiveRoadCollider = hitRoadCol;
                roadPoint = ClampPointToCollider(rawHitPoint, currentActiveRoadCollider);
            }
            else
            {
                Vector3 lastPt = currentCar.pathPoints.Count > 0 ? currentCar.pathPoints[currentCar.pathPoints.Count - 1] : currentCar.transform.position;
                Vector3 candidateClamped = ClampPointToCollider(rawHitPoint, hitRoadCol);

                if (IsSegmentOnRoad(lastPt, candidateClamped) || (lastPt - candidateClamped).sqrMagnitude <= 0.36f)
                {
                    currentActiveRoadCollider = hitRoadCol;
                    roadPoint = candidateClamped;
                }
                else
                {
                    roadPoint = ClampPointToCollider(rawHitPoint, currentActiveRoadCollider);
                }
            }
        }
        else
        {
            if (currentActiveRoadCollider == null)
            {
                Vector3 lastPt = currentCar.pathPoints.Count > 0 ? currentCar.pathPoints[currentCar.pathPoints.Count - 1] : currentCar.transform.position;
                currentActiveRoadCollider = GetNearestRoadCollider(lastPt);
            }

            if (currentActiveRoadCollider != null && currentCar.pathPoints.Count > 0)
            {
                Vector3 lastPt = currentCar.pathPoints[currentCar.pathPoints.Count - 1];
                Collider bestAdjacent = FindBestAdjacentCollider(currentActiveRoadCollider, rawHitPoint, lastPt);
                if (bestAdjacent != null) currentActiveRoadCollider = bestAdjacent;
            }

            roadPoint = currentActiveRoadCollider != null
                ? ClampPointToCollider(rawHitPoint, currentActiveRoadCollider)
                : ClampPointToRoad(rawHitPoint);
        }

        ParkingSlot targetSlot = GetSlotAtPoint(rawHitPoint, currentCar) ?? GetSlotAtPoint(roadPoint, currentCar);
        if (targetSlot != null)
        {
            Vector3 slotCenter = new Vector3(targetSlot.transform.position.x, drawPlaneY, targetSlot.transform.position.z);
            currentCar.AddPoint(slotCenter, drawPlaneY);
            hasReachedTarget = true;

            if (currentCar.pathPoints.Count >= 2)
            {
                Vector3 initialDir = currentCar.pathPoints[1] - currentCar.pathPoints[0];
                initialDir.y = 0;
                if (initialDir.sqrMagnitude > 0.005f)
                {
                    currentCar.transform.rotation = Quaternion.LookRotation(initialDir);
                }
            }
            return;
        }

        float minDistanceSqr = minDistance * minDistance;
        if (currentCar.pathPoints.Count > 0)
        {
            Vector3 lastPoint = currentCar.pathPoints[currentCar.pathPoints.Count - 1];
            float distSqr = (roadPoint - lastPoint).sqrMagnitude;

            if (distSqr >= minDistanceSqr)
            {
                float dist = Mathf.Sqrt(distSqr);
                if (dist > minDistance * 1.5f)
                {
                    int steps = Mathf.Min(12, Mathf.CeilToInt(dist / minDistance));
                    for (int s = 1; s <= steps; s++)
                    {
                        Vector3 interPoint = Vector3.Lerp(lastPoint, roadPoint, (float)s / steps);
                        Vector3 clampedInter = ClampPointToRoad(interPoint);
                        if (IsPointSupportedByRoad(clampedInter))
                        {
                            currentCar.AddPoint(clampedInter, drawPlaneY);
                        }
                    }
                }
                else if (IsSegmentOnRoad(lastPoint, roadPoint) || IsPointSupportedByRoad(roadPoint))
                {
                    currentCar.AddPoint(roadPoint, drawPlaneY);
                }

                if (currentCar.pathPoints.Count >= 2)
                {
                    Vector3 initialDir = currentCar.pathPoints[1] - currentCar.pathPoints[0];
                    initialDir.y = 0;
                    if (initialDir.sqrMagnitude > 0.005f)
                    {
                        currentCar.transform.rotation = Quaternion.LookRotation(initialDir);
                    }
                }
            }
        }
        else if (IsPointSupportedByRoad(roadPoint))
        {
            currentCar.AddPoint(roadPoint, drawPlaneY);
        }
    }

    private void HandlePointerUp()
    {
        hasReachedTarget = false;
        currentActiveRoadCollider = null;

        if (!isDragging)
        {
            if (candidateCar != null)
            {
                bool isCarAway = (candidateCar.transform.position - candidateCar.startPosition).sqrMagnitude > 0.25f;
                if (isCarAway && isCandidateFromStart)
                {
                    candidateCar.ClearPath();
                    candidateCar.ResetToStartSmooth();
                }
            }
            candidateCar = null;
            currentCar = null;
            return;
        }

        isDragging = false;
        candidateCar = null;
        if (currentCar == null) return;

        currentCar.isDrawingPath = false;
        CarController carToDrive = currentCar;
        currentCar = null;

        if (carToDrive.pathPoints.Count <= 1)
        {
            carToDrive.ClearPath();
            return;
        }

        if (!autoDriveOnRelease) return;

        var allCars = new List<CarController>(GetAllCars());
        allCars.RemoveAll(c => c == null || !c.gameObject.activeInHierarchy || c.GetComponentInParent<Canvas>() != null);

        bool allCarsHavePath = allCars.Count > 0 && allCars.TrueForAll(c => c != null && c.pathPoints != null && c.pathPoints.Count > 1);

        if (!allCarsHavePath || allCars.Count <= 1)
        {
            if (isDrawingFromStart)
                carToDrive.DriveWhenReady();
            else
                carToDrive.ResetToStartSmooth(() => carToDrive.DriveWhenReady(), 0.35f);
            return;
        }

        void LaunchAllCars()
        {
            if (GameManager.HasInstance) GameManager.Instance.StartAllCars();
            else allCars.ForEach(c => { if (c != null) c.DriveWhenReady(); });
        }

        var carsToReset = allCars.FindAll(c => c != null && (Vector3.Distance(c.transform.position, c.startPosition) > 0.25f || c.IsDriving || c.isParked || c.isFlyingBack));

        if (carsToReset.Count == 0)
        {
            LaunchAllCars();
        }
        else
        {
            int pending = carsToReset.Count;
            foreach (var c in carsToReset)
            {
                c.StopDriving();
                c.ResetToStartSmooth(() =>
                {
                    if (--pending <= 0) LaunchAllCars();
                }, 0.35f);
            }
        }
    }

    #region Input Helpers

    private enum PointerState { Down, Held, Up }

    private bool CheckPointerInput(PointerState state, out Vector2 screenPos)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
        {
            var p = Touchscreen.current.primaryTouch.press;
            bool active = state == PointerState.Down ? p.wasPressedThisFrame : state == PointerState.Held ? p.isPressed : p.wasReleasedThisFrame;
            if (active)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null)
        {
            var b = Mouse.current.leftButton;
            bool active = state == PointerState.Down ? b.wasPressedThisFrame : state == PointerState.Held ? b.isPressed : b.wasReleasedThisFrame;
            if (active)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
        }

        if (Pointer.current != null)
        {
            var p = Pointer.current.press;
            bool active = state == PointerState.Down ? p.wasPressedThisFrame : state == PointerState.Held ? p.isPressed : p.wasReleasedThisFrame;
            if (active)
            {
                screenPos = Pointer.current.position.ReadValue();
                return true;
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            bool legacy = state == PointerState.Down ? Input.GetMouseButtonDown(0)
                        : state == PointerState.Held ? Input.GetMouseButton(0)
                        : Input.GetMouseButtonUp(0);
            if (legacy)
            {
                screenPos = Input.mousePosition;
                return true;
            }
        }
        catch { }
#endif

        screenPos = Vector2.zero;
        return false;
    }

    private bool GetPointerDown(out Vector2 pos) => CheckPointerInput(PointerState.Down, out pos);
    private bool GetPointerHeld(out Vector2 pos) => CheckPointerInput(PointerState.Held, out pos);
    private bool GetPointerUp(out Vector2 pos) => CheckPointerInput(PointerState.Up, out pos);

    #endregion

    private ParkingSlot GetSlotAtPoint(Vector3 point, CarController car = null)
    {
        if (allParkingSlots == null || allParkingSlots.Length == 0)
            RefreshParkingSlots();

        if (allParkingSlots != null)
        {
            foreach (var slot in allParkingSlots)
            {
                if (slot != null && slot.IsInside(point))
                {
                    if (car == null || slot.SlotColorType == ColorType.None || car.CarColorType == ColorType.None || slot.SlotColorType == car.CarColorType)
                        return slot;
                }
            }
        }
        return null;
    }

    public bool IsInsideRoad(Vector3 worldPoint) => IsPointSupportedByRoad(worldPoint, false);

    public bool IsPointSupportedByRoad(Vector3 worldPoint, bool allowOverlapMargin = true)
    {
        if (roadColliders.Count == 0)
        {
            RefreshRoadColliders();
            if (roadColliders.Count == 0) return true;
        }

        float pad = allowOverlapMargin ? -0.08f : roadEdgePadding;

        foreach (var col in roadColliders)
        {
            if (col == null || !col.enabled) continue;

            if (col is BoxCollider box)
            {
                Vector3 localPoint = box.transform.InverseTransformPoint(worldPoint);
                Vector3 scale = box.transform.lossyScale;
                float padX = pad / Mathf.Max(Mathf.Abs(scale.x), 0.001f);
                float padZ = pad / Mathf.Max(Mathf.Abs(scale.z), 0.001f);

                float limitX = Mathf.Max(0.01f, box.size.x * 0.5f - padX);
                float limitZ = Mathf.Max(0.01f, box.size.z * 0.5f - padZ);

                if (Mathf.Abs(localPoint.x - box.center.x) <= limitX &&
                    Mathf.Abs(localPoint.z - box.center.z) <= limitZ)
                    return true;
            }
            else
            {
                Vector3 closest = col.ClosestPoint(worldPoint);
                closest.y = worldPoint.y;
                float threshold = allowOverlapMargin ? 0.3f : Mathf.Max(0.05f, roadEdgePadding);
                if ((worldPoint - closest).sqrMagnitude <= threshold * threshold)
                    return true;
            }
        }

        return false;
    }

    public bool IsSegmentOnRoad(Vector3 p1, Vector3 p2)
    {
        float distSqr = SqrDistXZ(p1, p2);
        float dist = Mathf.Sqrt(distSqr);
        int samples = Mathf.Max(2, Mathf.CeilToInt(dist / 0.08f));
        for (int i = 1; i <= samples; i++)
        {
            Vector3 sample = Vector3.Lerp(p1, p2, (float)i / samples);
            if (!IsPointSupportedByRoad(sample, true))
                return false;
        }
        return true;
    }

    public Vector3 ClampPointToRoad(Vector3 worldPoint)
    {
        if (roadColliders.Count == 0)
        {
            RefreshRoadColliders();
            if (roadColliders.Count == 0) return worldPoint;
        }

        if (IsInsideRoad(worldPoint))
        {
            worldPoint.y = drawPlaneY;
            return worldPoint;
        }

        Collider bestCollider = null;
        float minSqrDist = float.MaxValue;
        Vector3 searchOrigin = (currentCar != null && currentCar.pathPoints.Count > 0)
                             ? currentCar.pathPoints[currentCar.pathPoints.Count - 1]
                             : worldPoint;

        foreach (var col in roadColliders)
        {
            if (col == null || !col.enabled) continue;
            Vector3 testClosest = col.ClosestPoint(worldPoint);
            float d2 = SqrDistXZ(worldPoint, testClosest);

            if (currentCar != null && currentCar.pathPoints.Count > 0)
                d2 += Vector3.Distance(testClosest, searchOrigin) * 2.0f;

            if (d2 < minSqrDist)
            {
                minSqrDist = d2;
                bestCollider = col;
            }
        }

        return bestCollider != null ? ClampPointToCollider(worldPoint, bestCollider) : new Vector3(worldPoint.x, drawPlaneY, worldPoint.z);
    }

    public Collider GetRoadColliderAt(Vector3 worldPoint, float tolerance = 0.05f)
    {
        if (roadColliders.Count == 0) RefreshRoadColliders();

        float tolSqr = (tolerance + 0.05f) * (tolerance + 0.05f);
        foreach (var col in roadColliders)
        {
            if (col == null || !col.enabled) continue;

            if (col is BoxCollider box)
            {
                Vector3 localPoint = box.transform.InverseTransformPoint(worldPoint);
                if (Mathf.Abs(localPoint.x - box.center.x) <= box.size.x * 0.5f + tolerance &&
                    Mathf.Abs(localPoint.z - box.center.z) <= box.size.z * 0.5f + tolerance)
                    return col;
            }
            else if (SqrDistXZ(worldPoint, col.ClosestPoint(worldPoint)) <= tolSqr)
            {
                return col;
            }
        }
        return null;
    }

    public Collider GetNearestRoadCollider(Vector3 worldPoint)
    {
        if (roadColliders.Count == 0) RefreshRoadColliders();

        Collider best = null;
        float minD2 = float.MaxValue;
        foreach (var c in roadColliders)
        {
            if (c == null || !c.enabled) continue;
            float d2 = SqrDistXZ(worldPoint, c.ClosestPoint(worldPoint));
            if (d2 < minD2)
            {
                minD2 = d2;
                best = c;
            }
        }
        return best;
    }

    public Vector3 ClampPointToCollider(Vector3 worldPoint, Collider col)
    {
        if (col == null)
            return new Vector3(worldPoint.x, drawPlaneY, worldPoint.z);

        if (col is BoxCollider box)
        {
            Vector3 localPoint = box.transform.InverseTransformPoint(worldPoint);
            Vector3 scale = box.transform.lossyScale;
            float padLocalX = roadEdgePadding / Mathf.Max(Mathf.Abs(scale.x), 0.001f);
            float padLocalZ = roadEdgePadding / Mathf.Max(Mathf.Abs(scale.z), 0.001f);

            float limitX = Mathf.Max(0.01f, box.size.x * 0.5f - padLocalX);
            float limitZ = Mathf.Max(0.01f, box.size.z * 0.5f - padLocalZ);

            localPoint.x = Mathf.Clamp(localPoint.x, box.center.x - limitX, box.center.x + limitX);
            localPoint.z = Mathf.Clamp(localPoint.z, box.center.z - limitZ, box.center.z + limitZ);

            Vector3 clamped = box.transform.TransformPoint(localPoint);
            clamped.y = drawPlaneY;
            return clamped;
        }

        Vector3 closest = col.ClosestPoint(worldPoint);
        closest.y = drawPlaneY;
        return closest;
    }

    private Collider FindBestAdjacentCollider(Collider currentCol, Vector3 rawHitPoint, Vector3 lastPoint)
    {
        if (currentCol == null || roadColliders.Count <= 1) return currentCol;

        Bounds expandedBounds = currentCol.bounds;
        expandedBounds.Expand(0.3f);

        Vector3 currentClamped = ClampPointToCollider(rawHitPoint, currentCol);
        Collider bestCol = currentCol;
        float bestScore = Vector3.Distance(rawHitPoint, currentClamped);

        foreach (var col in roadColliders)
        {
            if (col == null || col == currentCol || !col.enabled || !expandedBounds.Intersects(col.bounds)) continue;

            Vector3 candidateClamped = ClampPointToCollider(rawHitPoint, col);
            if ((lastPoint - candidateClamped).sqrMagnitude <= 0.2025f)
            {
                float distToRaw = Vector3.Distance(rawHitPoint, candidateClamped);
                if (distToRaw < bestScore - 0.05f)
                {
                    bestScore = distToRaw;
                    bestCol = col;
                }
            }
        }

        return bestCol;
    }

    private bool IsPointerOverInteractiveUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        pointerEventDataCache ??= new PointerEventData(EventSystem.current);
        pointerEventDataCache.position = screenPos;
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventDataCache, uiRaycastResults);

        foreach (var r in uiRaycastResults)
        {
            if (r.gameObject == null) continue;
            var c = r.gameObject.GetComponentInParent<Canvas>();
            if (c != null && c.renderMode == RenderMode.WorldSpace) continue;

            if (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                return true;
        }
        return false;
    }
}