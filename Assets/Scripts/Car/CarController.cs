using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CarMovement), typeof(CarPathHandle), typeof(CarVisual))]
[RequireComponent(typeof(CarAnimation), typeof(CarCollision))]
public class CarController : MonoBehaviour
{
    [field: SerializeField] public CarMovement Movement { get; private set; }
    [field: SerializeField] public CarPathHandle PathHandler { get; private set; }
    [field: SerializeField] public CarVisual Visual { get; private set; }
    [field: SerializeField] public CarAnimation Animation { get; private set; }
    [field: SerializeField] public CarCollision Collision { get; private set; }

    [HideInInspector] public Vector3 startPosition;
    [HideInInspector] public Quaternion startRotation;
    private StartPointRing startPointRing;

    public bool isDrawingPath { get; set; } = false;
    public bool isParked { get; set; } = false;

    [HideInInspector] public ParkingSlot currentSlot;

    public bool IsDriving => Movement != null && Movement.IsDriving;
    public bool isFlyingBack => Animation != null && Animation.IsFlyingBack;
    public bool IsFlyingBack => isFlyingBack;
    public bool isCrashed => Collision != null && Collision.IsCrashed;
    public bool IsCrashed => isCrashed;

    public List<Vector3> pathPoints
    {
        get => PathHandler != null ? PathHandler.pathPoints : null;
        set
        {
            if (PathHandler != null)
            {
                PathHandler.pathPoints = value;
                PathHandler.RefreshLineRenderer();
            }
        }
    }

    public float TotalDrawnDistance
    {
        get => PathHandler != null ? PathHandler.TotalDrawDistance : 0f;
        set { if (PathHandler != null) PathHandler.TotalDrawDistance = value; }
    }
    public float TotalDrawDistance
    {
        get => TotalDrawnDistance;
        set => TotalDrawnDistance = value;
    }

    public ColorType CarColorType
    {
        get => Visual != null ? Visual.CarColorType : ColorType.Red;
        set { if (Visual != null) Visual.CarColorType = value; }
    }

    private void Awake()
    {
        if (this.GetComponentInParent<Canvas>() != null) return;

        startPosition = this.transform.position;
        startRotation = this.transform.rotation;

        Movement = this.GetComponent<CarMovement>();
        PathHandler = this.GetComponent<CarPathHandle>();
        Visual = this.GetComponent<CarVisual>();
        Animation = this.GetComponent<CarAnimation>();
        Collision = this.GetComponent<CarCollision>();

        Movement.OnPathComplete += HandlePathCompleted;
        Collision.OnCarCrashedEvent += HandleCrashCallback;

        Visual.InitVisual(color => PathHandler.ApplyColorToLine(color));

        if (GameManager.HasInstance)
            GameManager.Instance.RegisterCar(this);
    }

    private void Start()
    {
        if (this.GetComponentInParent<Canvas>() != null) return;
        CreateStartPointTrigger();
        Animation.StartIdleAnimation();
    }

    private void OnDestroy()
    {
        if (!GameManager.IsApplicationQuitting && GameManager.HasInstance)
            GameManager.Instance.UnregisterCar(this);
    }

    private void CreateStartPointTrigger()
    {
        CarStartPoint targetMarker = null;
        CarStartPoint[] existingMarkers = FindObjectsByType<CarStartPoint>(FindObjectsSortMode.None);

        foreach (var em in existingMarkers)
        {
            if (em != null && (em.car == this || Vector3.Distance(em.transform.position, startPosition) < 0.5f))
            {
                targetMarker = em;
                targetMarker.car = this;
                break;
            }
        }

        GameObject startMarker = null;
        if (targetMarker != null)
        {
            startMarker = targetMarker.gameObject;
        }
        else
        {
            startMarker = new GameObject($"{gameObject.name}_StartPoint");
            startMarker.transform.position = startPosition;
            startMarker.transform.rotation = startRotation;

            var sp = startMarker.AddComponent<CarStartPoint>();
            sp.colorType = this.CarColorType;
            sp.car = this;
        }

        BoxCollider markerBox = startMarker.GetComponent<BoxCollider>();
        if (markerBox == null)
        {
            markerBox = startMarker.AddComponent<BoxCollider>();
        }
        markerBox.isTrigger = true;
        markerBox.size = new Vector3(1.2f, 0.6f, 2.2f);
        markerBox.center = new Vector3(0f, 0.3f, 0f);

        StartPointRing existingRing = startMarker.GetComponentInChildren<StartPointRing>();
        if (existingRing == null)
        {
            startPointRing = CreateRingObject(startMarker);
        }
        else
        {
            startPointRing = existingRing;
        }

        startPointRing.car = this;
        startPointRing.SetColor(Visual.GetCarBodyColor());
    }

    private StartPointRing CreateRingObject(GameObject parent)
    {
        GameObject ringObj = new GameObject("RingVisual");
        ringObj.transform.SetParent(parent.transform, false);

        ringObj.transform.localPosition = Vector3.zero;
        ringObj.transform.localRotation = Quaternion.identity;
        return ringObj.AddComponent<StartPointRing>();
    }
    public void StartDriving()
    {
        if (PathHandler.pathPoints.Count > 1)
        {
            isDrawingPath = false;
            isParked = false;
            Animation.StopIdleAnimation();
            Movement.StartMovement(PathHandler.pathPoints);
        }
    }

    public void StopDriving() => Movement.StopMovement();

    public void DriveWhenReady()
    {
        if (isFlyingBack) StartCoroutine(WaitFlyThenDrive());
        else StartDriving();
    }

    private IEnumerator WaitFlyThenDrive()
    {
        while (isFlyingBack) yield return null;
        StartDriving();
    }

    public void AddPoint(Vector3 newPoint, float fixedY) => PathHandler.AddPoints(newPoint, fixedY);

    public void ClearPath()
    {
        Movement.StopMovement();
        PathHandler.ClearPath();
        ReleaseFromSlot();
    }

    public void ResetToStartSmooth(Action onComplete = null, float customDuration = -1f)
    {
        Movement.StopMovement();
        Collision.ResetPhysicState();
        isParked = false;
        isDrawingPath = false;
        ReleaseFromSlot();
        Animation.ResetScaleImmediate();

        if (Vector3.Distance(transform.position, startPosition) < 0.2f && Quaternion.Angle(transform.rotation, startRotation) < 5f)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            Animation.StartIdleAnimation();
            onComplete?.Invoke();
            return;
        }
        Animation.FlyTo(startPosition, startRotation, onComplete, customDuration);
    }

    public void Crash(CarController otherCar, Vector3 contactPoint)
    {
        if (isCrashed) return;
        Movement.StopMovement();
        isParked = false;
        isDrawingPath = false;
        Animation.StopIdleAnimation();
        Animation.ResetScaleImmediate();
        Collision.TriggerCrash(otherCar, contactPoint);
    }

    public void ReleaseFromSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.ResetSlot();
            currentSlot = null;
        }
        if (GameManager.HasInstance && GameManager.Instance.allSlots != null)
        {
            foreach (var slot in GameManager.Instance.allSlots)
            {
                if (slot != null && slot.currentParkedCar == this)
                    slot.ResetSlot();
            }
        }
        isParked = false;
    }

    public void SetCarColor(Color color)
    {
        Visual.SetCarColor(color);
        PathHandler.ApplyColorToLine(color);
        if (startPointRing != null) startPointRing.SetColor(color);
    }

    public void SetCarColorType(ColorType type)
    {
        CarColorType = type;
        SetCarColor(ColorPalette.GetColor(type));
    }

    public Color GetCarBodyColor() => Visual.GetCarBodyColor();
    public void ApplyBodyColorToLine() => PathHandler.ApplyColorToLine(Visual.GetCarBodyColor());

    public float GetTotalPathLength() => PathHandler != null ? PathHandler.GetTotalPathLength() : 0f;
    public float getTotalPathLength() => GetTotalPathLength();
    public void StartIdleAnimation() => Animation?.StartIdleAnimation();
    public void StopIdleAnimation() => Animation?.StopIdleAnimation();
    public void ApplyColorToCarMesh(Color color) => Visual?.ApplyColorToCarMesh(color);

    private void HandlePathCompleted()
    {
        CheckArrivalAtParkingSlot();
        Animation.StartIdleAnimation();
    }

    private void CheckArrivalAtParkingSlot()
    {
        if (!GameManager.HasInstance || GameManager.Instance.allSlots == null) return;
        foreach (var slot in GameManager.Instance.allSlots)
        {
            if (slot != null && slot.IsInside(transform.position))
            {
                if (slot.SlotColorType == ColorType.None || CarColorType == ColorType.None || slot.SlotColorType == CarColorType)
                {
                    slot.CompleteParking(this);
                    return;
                }
            }
        }
    }

    private void HandleCrashCallback(CarController otherCar, Vector3 contactPoint)
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnCarCrashed(this, otherCar);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GetComponentInParent<Canvas>() != null || !IsDriving || isFlyingBack || isCrashed) return;
        HandleCollision(other.gameObject, other.transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (GetComponentInParent<Canvas>() != null || !IsDriving || isFlyingBack || isCrashed) return;
        Vector3 pt = (collision.contacts != null && collision.contacts.Length > 0) ? collision.contacts[0].point : collision.transform.position;
        HandleCollision(collision.gameObject, pt);
    }

    private void HandleCollision(GameObject otherObj, Vector3 contactPoint)
    {
        CarController otherCar = otherObj.GetComponentInParent<CarController>();
        if (otherCar != null && otherCar != this && !otherCar.isFlyingBack)
        {
            Vector3 midPoint = (transform.position + otherCar.transform.position) * 0.5f;
            this.Crash(otherCar, midPoint);
            otherCar.Crash(this, midPoint);
        }
    }
}
