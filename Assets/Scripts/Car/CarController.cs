using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; 

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Tốc độ xe")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotateSpeed = 15f;

    [Header("Hiệu ứng va chạm")]
    [Tooltip("Prefab hiệu ứng tia lửa khi đâm nhau (kéo từ Project vào đây)")]
    [SerializeField] private GameObject crashFxPrefab;

    [Header("Hiệu ứng bay về xuất phát")]
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private float arcHeight = 1.5f;

    [Header("Hiệu ứng khi idle ")]
    [Tooltip("Thời gian 1 chu kỳ co giãn (giây)")]
    [UnityEngine.Serialization.FormerlySerializedAs("idleSpeed")]
    [SerializeField] private float idleDuration = 0.35f;
    [Tooltip("Độ giãn thêm theo trục Y ")]
    [SerializeField] private float stretchY = 0.15f;
    [Tooltip("Độ co hẹp theo trục X và Z khi giãn ")]
    [SerializeField] private float squashXZ = 0.05f;

    [SerializeField] private Renderer bodyRenderer;

    [Header("Màu sắc xe & đường line (Decoupled Color)")]
    [Tooltip("Định danh màu của xe (Red, Blue, Yellow, Green...)")]
    [SerializeField] private ColorType carColorType = ColorType.Red;
    public ColorType CarColorType { get => carColorType; set => carColorType = value; }

    [Tooltip("Màu tùy chỉnh thủ công cho xe ")]
    [SerializeField] private Color customCarColor = Color.clear;
    private Color assignedColor = Color.clear;

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer lineRenderer;

    [HideInInspector]
    public List<Vector3> pathPoints = new List<Vector3>();

    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public Quaternion startRotation;

    public bool isFlyingBack { get; private set; } = false;
    public bool isCrashed { get; private set; } = false;
    private bool isDriving = false;
    public bool IsDriving => isDriving;
    private int currentPointIndex = 0;
    private Coroutine flyCoroutine;
    private StartPointRing startRing;

    private Rigidbody rb;
    private BoxCollider boxCollider;

    private Tween idleTween;
    private Vector3 initialScale;

    private float lastFinishedDistance = 0f;
    public float TotalDrawnDistance
    {
        get
        {
            float len = GetTotalPathLength();
            return len > 0.05f ? len : lastFinishedDistance;
        }
        set
        {
            lastFinishedDistance = value;
        }
    }

    public bool isDrawingPath { get; set; } = false;
    public bool isParked { get; set; } = false;
    [HideInInspector] public ParkingSlot currentSlot;

    public void ReleaseFromSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.ResetSlot();
            currentSlot = null;
        }

        var slots = GameManager.Instance.allSlots;
        foreach (var slot in slots)
        {
            if (slot != null && slot.currentParkedCar == this)
            {
                slot.ResetSlot();
            }
        }
        isParked = false;
    }

    private void Awake()
    {
        if (this.GetComponentInParent<Canvas>() != null) return;

        startPosition = this.transform.position;
        startRotation = this.transform.rotation;

        rb = this.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        boxCollider = this.GetComponent<BoxCollider>();

        if (lineRenderer == null)
            lineRenderer = this.GetComponentInChildren<LineRenderer>();

        if (lineRenderer != null)
            lineRenderer.positionCount = 0;

        FindBestBodyRenderer();
        ApplyBodyColorToLine();

        if (GameManager.HasInstance)
        {
            GameManager.Instance.RegisterCar(this);
        }
    }

    private void FindBestBodyRenderer()
    {
        if (bodyRenderer != null) return;

        Renderer[] allRends = this.GetComponentsInChildren<Renderer>(true);
        Renderer largestRenderer = null;
        float maxVolume = 0f;

        foreach (var r in allRends)
        {
            if (r == null || r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer) continue;

            string objName = r.gameObject.name.ToLower();
            if (objName.Contains("body") || objName.Contains("chassis") || objName.Contains("exterior"))
            {
                bodyRenderer = r;
                return;
            }

            if (r.sharedMaterials != null)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null && (m.name.ToLower().Contains("paint") || m.name.ToLower().Contains("body") || m.name.ToLower().Contains("car")))
                    {
                        bodyRenderer = r;
                        return;
                    }
                }
            }

            Bounds b = r.bounds;
            float vol = b.size.x * b.size.y * b.size.z;
            if (vol > maxVolume)
            {
                maxVolume = vol;
                largestRenderer = r;
            }
        }

        if (bodyRenderer == null)
        {
            bodyRenderer = largestRenderer;
        }
    }

    private void Start()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        CreateStartPointTrigger();
        StartIdleAnimation(); 
    }

    public void StartIdleAnimation()
    {
        StopIdleAnimation();

        if (initialScale == Vector3.zero)
            initialScale = this.transform.localScale;

        this.transform.localScale= initialScale;

        Vector3 targetScale = new Vector3(
            initialScale.x * (1f - squashXZ),
            initialScale.y * (1f + stretchY),
            initialScale.z * (1f - squashXZ)
        );

        idleTween = transform.DOScale(targetScale, idleDuration).SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopIdleAnimation()
    {
        if (idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
            idleTween = null;
        }

        if (initialScale != Vector3.zero)
            transform.localScale = initialScale;
    }

    private void OnDestroy()
    {
        if (idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
            idleTween = null;
        }

        if (!GameManager.IsApplicationQuitting && GameManager.HasInstance)
        {
            GameManager.Instance.UnregisterCar(this);
        }
    }

    private void CreateStartPointTrigger()
    {
        CarStartPoint targetMarker = null;
        CarStartPoint[] existingMarkers = FindObjectsByType<CarStartPoint>(FindObjectsSortMode.None);
        foreach (var em in existingMarkers)
        {
            if (em != null)
            {
                if (em.car == this)
                {
                    targetMarker = em;
                    break;
                }
                else if (Vector3.Distance(em.transform.position, startPosition) < 0.5f)
                {
                    targetMarker = em;
                    targetMarker.car = this;
                    break;
                }
            }
        }

        GameObject startMarker = null;
        if (targetMarker != null)
        {
            startMarker = targetMarker.gameObject;
        }
        else
        {
            startMarker = new GameObject($"{this.gameObject.name}_StartPoint");
            startMarker.transform.position = startPosition;
            startMarker.transform.rotation = startRotation;

            CarStartPoint sp = startMarker.AddComponent<CarStartPoint>();
            sp.colorType = this.CarColorType;
            sp.car = this;
        }

        BoxCollider markerBox = startMarker.GetComponent<BoxCollider>();
        if (markerBox == null)
        {
            markerBox = startMarker.AddComponent<BoxCollider>();
            markerBox.isTrigger = true;
            markerBox.size = new Vector3(1.2f, 0.6f, 2.2f);
            markerBox.center = new Vector3(0f, 0.3f, 0f);
        }

        StartPointRing existingRing = startMarker.GetComponentInChildren<StartPointRing>();
        if (existingRing == null)
        {
            GameObject ringObj = new GameObject("RingVisual");
            ringObj.transform.SetParent(startMarker.transform, false);
            ringObj.transform.localPosition = Vector3.zero;

            StartPointRing ring = ringObj.AddComponent<StartPointRing>();
            ring.car = this;
            startRing = ring;
            ring.SetColor(GetCarBodyColor());
        }
        else
        {
            existingRing.car = this;
            startRing = existingRing;
            existingRing.SetColor(GetCarBodyColor());
        }
    }


    public void ApplyBodyColorToLine()
    {
        if (lineRenderer == null) return;

        Color bodyColor = GetCarBodyColor();
        bodyColor.a = 1.0f;  

        float brightness = (bodyColor.r + bodyColor.g + bodyColor.b) / 3f;
        if (brightness > 0.85f || brightness < 0.2f)
        {
            bodyColor = new Color(1f, 0.75f, 0.05f, 1f);  
        }

        lineRenderer.startColor = bodyColor;
        lineRenderer.endColor = bodyColor;

        Material lineMat = Application.isPlaying ? lineRenderer.material : lineRenderer.sharedMaterial;
        if (lineMat != null)
        {
            if (lineMat.HasProperty("_BaseColor"))
                lineMat.SetColor("_BaseColor", bodyColor);
            if (lineMat.HasProperty("_Color"))
                lineMat.SetColor("_Color", bodyColor);
            lineMat.color = bodyColor;
        }
    }

    public void ResetToStartSmooth(Action onComplete = null, float customDuration = -1f)
    {
        isDriving = false;
        isCrashed = false;
        isParked = false;
        isDrawingPath = false;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        ReleaseFromSlot();
        StopIdleAnimation();
        transform.DOKill();
        if (initialScale != Vector3.zero)
            transform.localScale = initialScale;

        if (Vector3.Distance(transform.position, startPosition) < 0.2f && Quaternion.Angle(transform.rotation, startRotation) < 5f)
        {
            this.transform.position = startPosition;
            this.transform.rotation = startRotation;
            StartIdleAnimation();
            onComplete?.Invoke();
            return;
        }

        float duration = (customDuration > 0f) ? customDuration : flyDuration;
        if (flyCoroutine != null) StopCoroutine(flyCoroutine);
        flyCoroutine = StartCoroutine(FlyRoutine(onComplete, duration));
    }

    private IEnumerator FlyRoutine(Action onComplete, float duration)
    {
        isFlyingBack = true;
        Vector3 initialPos = this.transform.position;
        Quaternion initialRot = this.transform.rotation;
        float elapsed = 0f;
        float currentArc = (duration < flyDuration) ? (arcHeight * 0.6f) : arcHeight;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPos = Vector3.Lerp(initialPos, startPosition, smoothT);
            currentPos.y += 4f * currentArc * smoothT * (1f - smoothT);

            this.transform.position = currentPos;
            this.transform.rotation = Quaternion.Slerp(initialRot, startRotation, smoothT);

            yield return null;
        }

        this.transform.position = startPosition;
        this.transform.rotation = startRotation;
        isFlyingBack = false;
        flyCoroutine = null;

        StartIdleAnimation(); 
        onComplete?.Invoke();
    }

    public void DriveWhenReady()
    {
        if (isFlyingBack)
            StartCoroutine(WaitFlyThenDrive());
        else
            StartDriving();
    }

    private IEnumerator WaitFlyThenDrive()
    {
        while (isFlyingBack)
            yield return null;
        StartDriving();
    }

    public void ClearPath()
    {
        isDriving = false;
        isCrashed = false;
        pathPoints.Clear();
        lastFinishedDistance = 0f;
        ReleaseFromSlot();
        if (lineRenderer != null) lineRenderer.positionCount = 0;
    }

    public float GetTotalPathLength()
    {
        float total = 0f;
        if (pathPoints == null || pathPoints.Count < 2) return 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            total += Vector3.Distance(
                new Vector3(pathPoints[i].x, 0, pathPoints[i].z),
                new Vector3(pathPoints[i + 1].x, 0, pathPoints[i + 1].z)
            );
        }
        return total;
    }

    public void AddPoint(Vector3 newPoint, float fixedY)
    {
        newPoint.y = fixedY;

        if (pathPoints.Count > 0)
        {
            Vector3 last = pathPoints[pathPoints.Count - 1];
            if (Vector3.Distance(newPoint, last) < 0.1f) return;
        }

        pathPoints.Add(newPoint);

        if (pathPoints.Count >= 3)
        {
            int i = pathPoints.Count - 2;
            Vector3 smoothed = (pathPoints[i - 1] + pathPoints[i] * 2f + pathPoints[i + 1]) / 4f;
            smoothed.y = fixedY;
            pathPoints[i] = smoothed;
        }

        TotalDrawnDistance = GetTotalPathLength();

        RefreshLineRenderer();
    }

    private void RefreshLineRenderer()
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPositions(pathPoints.ToArray());
    }

    public void StopDriving()
    {
        isDriving = false;
    }

    public void StartDriving()
    {
        if (pathPoints.Count > 1)
        {
            currentPointIndex = 0;
            isDriving = true;
            isDrawingPath = false;
            isParked = false;
            StopIdleAnimation();

            Vector3 initialDir = pathPoints[1] - pathPoints[0];
            initialDir.y = 0;
            if (initialDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(initialDir);
            }
        }
    }

    private void Update()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (!isDriving || isFlyingBack || isCrashed) return;

        if (currentPointIndex < pathPoints.Count)
        {
            Vector3 target = pathPoints[currentPointIndex];
            target.y = this.transform.position.y;

            this.transform.position = Vector3.MoveTowards(this.transform.position, target, moveSpeed * Time.deltaTime);

            Vector3 direction = target - this.transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            float arriveThreshold = (currentPointIndex == pathPoints.Count - 1) ? 0.04f : 0.12f;
            if (Vector3.Distance(this.transform.position, target) < arriveThreshold)
            {
                currentPointIndex++;
            }
        }
        else
        {
            isDriving = false;
            CheckArrivalAtParkingSlot();
            StartIdleAnimation();
        }
    }

    private void CheckArrivalAtParkingSlot()
    {
        var slots = GameManager.Instance.allSlots;
        foreach (var slot in slots)
        {
            if (slot != null && slot.IsInside(this.transform.position))
            {
                if (slot.SlotColorType == ColorType.None || this.CarColorType == ColorType.None || slot.SlotColorType == this.CarColorType)
                {
                    slot.CompleteParking(this);
                    return;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GetComponentInParent<Canvas>() != null) return;
        HandleCollision(other.gameObject, other.transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (GetComponentInParent<Canvas>() != null) return;
        Vector3 pt = (collision.contacts != null && collision.contacts.Length > 0) ? collision.contacts[0].point : collision.transform.position;
        HandleCollision(collision.gameObject, pt);
    }

    private void HandleCollision(GameObject otherObj, Vector3 contactPoint)
    {
        if (!isDriving || isFlyingBack || isCrashed) return;

        CarController otherCar = otherObj.GetComponentInParent<CarController>();
        if (otherCar != null && otherCar != this && !otherCar.isFlyingBack)
        {
            Vector3 midPoint = (transform.position + otherCar.transform.position) * 0.5f;
            this.Crash(otherCar, midPoint);
            otherCar.Crash(this, midPoint);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.OnCarCrashed(this, otherCar);
            }
        }
    }

    public void Crash(CarController otherCar, Vector3 contactPoint)
    {
        if (isCrashed) return;
        isCrashed = true;
        isDriving = false;
        isParked = false;
        isDrawingPath = false;

        StopIdleAnimation();
        transform.DOKill();

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            Vector3 myPos = transform.position;
            Vector3 otherPos = (otherCar != null) ? otherCar.transform.position : contactPoint;
            Vector3 awayDir = myPos - otherPos;
            awayDir.y = 0;
            if (awayDir.sqrMagnitude < 0.01f)
            {
                awayDir = (transform.right + Vector3.forward * 0.3f).normalized;
            }
            else
            {
                awayDir.Normalize();
            }

            float flingForce = UnityEngine.Random.Range(7.5f, 10.0f);
            float upForce = UnityEngine.Random.Range(4.8f, 6.5f);
            Vector3 impulse = awayDir * flingForce + Vector3.up * upForce;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(impulse, ForceMode.Impulse);

            Vector3 torque = new Vector3(
                UnityEngine.Random.Range(-18f, 18f),
                UnityEngine.Random.Range(-28f, 28f),
                UnityEngine.Random.Range(-18f, 18f)
            );
            rb.AddTorque(torque, ForceMode.Impulse);
        }

        SpawnCrashEffects(contactPoint != Vector3.zero ? contactPoint : transform.position);
    }

    private void SpawnCrashEffects(Vector3 pos)
    {
        Vector3 spawnPos = pos + Vector3.up * 0.4f;
        if (crashFxPrefab != null)
        {
            Instantiate(crashFxPrefab, spawnPos, Quaternion.identity);
        }
    }

    private static MaterialPropertyBlock carMeshPropBlock;

    private void OnValidate()
    {
        if (carColorType != ColorType.None)
        {
            Color c = (customCarColor.a > 0.05f) ? customCarColor : ColorPalette.GetColor(carColorType);
            ApplyBodyColorToLine();
            ApplyColorToCarMesh(c);
        }
    }

    public void SetCarColorType(ColorType type)
    {
        carColorType = type;
        SetCarColor(ColorPalette.GetColor(type));
    }

    public void SetCarColor(Color color)
    {
        assignedColor = color;
        ApplyBodyColorToLine();
        ApplyColorToCarMesh(color);

        if (startRing != null)
        {
            startRing.SetColor(GetCarBodyColor());
        }
    }

    public void ApplyColorToCarMesh(Color color)
    {
        if (carMeshPropBlock == null) carMeshPropBlock = new MaterialPropertyBlock();

        FindBestBodyRenderer();

        Renderer[] allRends = this.GetComponentsInChildren<Renderer>(true);
        bool paintedAny = false;

        foreach (var r in allRends)
        {
            if (r == null || r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer) continue;

            bool isPaint = false;
            if (r == bodyRenderer || r.gameObject.name.ToLower().Contains("body") || r.gameObject.name.ToLower().Contains("chassis"))
            {
                isPaint = true;
            }
            else if (r.sharedMaterials != null)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null)
                    {
                        string mName = m.name.ToLower();
                        if (mName.Contains("paint") || mName.Contains("body") || mName.Contains("color") || mName.Contains("car") || mName.Contains("skin"))
                        {
                            isPaint = true;
                            break;
                        }
                    }
                }
            }

            if (isPaint)
            {
                r.GetPropertyBlock(carMeshPropBlock);
                carMeshPropBlock.SetColor("_BaseColor", color);
                carMeshPropBlock.SetColor("_Color", color);
                r.SetPropertyBlock(carMeshPropBlock);

                if (Application.isPlaying && r.materials != null)
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat == null) continue;
                        string matName = mat.name.ToLower();
                        if (matName.Contains("glass") || matName.Contains("wheel") || matName.Contains("tire") || matName.Contains("rubber") || matName.Contains("light") || matName.Contains("black"))
                        {
                            continue;
                        }
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                        mat.color = color;
                    }
                }

                paintedAny = true;
            }
        }

        if (!paintedAny && bodyRenderer != null)
        {
            bodyRenderer.GetPropertyBlock(carMeshPropBlock);
            carMeshPropBlock.SetColor("_BaseColor", color);
            carMeshPropBlock.SetColor("_Color", color);
            bodyRenderer.SetPropertyBlock(carMeshPropBlock);

            if (Application.isPlaying && bodyRenderer.materials != null)
            {
                foreach (var mat in bodyRenderer.materials)
                {
                    if (mat != null)
                    {
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                        mat.color = color;
                    }
                }
            }
        }
    }

    public Color GetCarBodyColor()
    {
        if (assignedColor.a > 0.05f) return assignedColor;

        if (customCarColor.a > 0.05f) return customCarColor;

        if (carColorType != ColorType.None)
        {
            return ColorPalette.GetColor(carColorType);
        }

        CarDatabase db = (GameManager.HasInstance && GameManager.Instance.CarDatabase != null) 
                       ? GameManager.Instance.CarDatabase 
                       : Resources.Load<CarDatabase>("CarDatabase");
        if (db != null && db.CarList != null)
        {
            string cleanName = this.gameObject.name.Replace("(Clone)", "").Trim();
            var match = db.CarList.Find(c => cleanName.Equals(c.Name, StringComparison.OrdinalIgnoreCase)
                                          || cleanName.StartsWith(c.Name, StringComparison.OrdinalIgnoreCase)
                                          || (c.Car3dPrefab != null && cleanName.Contains(c.Car3dPrefab.name)));
            if (match != null && match.CarColor.a > 0.05f)
            {
                return match.CarColor;
            }
        }

        FindBestBodyRenderer();

        if (bodyRenderer != null)
        {
            Material mat = Application.isPlaying ? bodyRenderer.material : bodyRenderer.sharedMaterial;
            if (mat != null)
            {
                Color c = Color.white;
                if (mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) c = mat.GetColor("_Color");
                else c = mat.color;

                if (c.r < 0.15f && c.g < 0.15f && c.b < 0.15f)
                {
                    return new Color(1f, 0.8f, 0f, 1f);
                }
                return c;
            }
        }

        return new Color(1f, 0.25f, 0.25f, 1f);
    }
}