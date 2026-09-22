using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ParkingSlot : MonoBehaviour
{
    [SerializeField] private GameObject winEffectPrefab;
    private TextMeshProUGUI textP;

    private BoxCollider boxCollider;
    private bool isOccupied = false;
    [HideInInspector] public CarController currentParkedCar;
    private GameObject currentFX;

    [Header("Định danh màu ô đỗ (Decoupled Color)")]
    [Tooltip("Màu ô đỗ yêu cầu (Red, Blue, Yellow, Green...). Chỉ xe có cùng ColorType mới được đỗ.")]
    [SerializeField] private ColorType slotColorType = ColorType.Red;
    public ColorType SlotColorType { get => slotColorType; set { slotColorType = value; ApplySlotVisuals(); } }

    [Tooltip("Mã cặp kết nối trực tiếp với điểm xuất phát CarStartPoint (nếu > 0)")]
    [SerializeField] private int pairId = 0;
    public int PairId { get => pairId; set => pairId = value; }

    [Header("Màu ô đỗ xe")]
    [Tooltip("Màu chữ P khi đã đỗ xe thành công")]
    [SerializeField] private Color parkedTextColor = Color.white;

    private SpriteRenderer[] slotSpriteRenderers;
    private Dictionary<SpriteRenderer, Color> initialSpriteColors = new Dictionary<SpriteRenderer, Color>();
    private Dictionary<Renderer, Color> initialMeshColors = new Dictionary<Renderer, Color>();
    private Dictionary<UnityEngine.UI.Image, Color> initialImageColors = new Dictionary<UnityEngine.UI.Image, Color>();
    private Color initialTextColor = Color.white;

    private void OnValidate()
    {
        ApplySlotVisuals();
    }

    private void Awake()
    {
        boxCollider = this.GetComponent<BoxCollider>();
        textP = this.GetComponentInChildren<TextMeshProUGUI>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        GameManager.Instance.RegisterSlot(this);

        InitColors();
        ApplySlotVisuals();
    }

    private void Start()
    {
        if (winEffectPrefab != null)
        {
            ObjectPool.Prewarm(winEffectPrefab, 2);
        }

        ApplySlotVisuals();
    }

    public void ApplySlotVisuals()
    {
        if (textP == null) textP = this.GetComponentInChildren<TextMeshProUGUI>();
        if (textP != null && slotColorType != ColorType.None)
        {
            textP.color = ColorPalette.GetColor(slotColorType);
            initialTextColor = textP.color;
        }
    }

    private void InitColors()
    {
        if (slotSpriteRenderers == null || slotSpriteRenderers.Length == 0)
        {
            slotSpriteRenderers = this.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (slotSpriteRenderers != null)
        {
            foreach (var sr in slotSpriteRenderers)
            {
                if (sr != null && !initialSpriteColors.ContainsKey(sr))
                {
                    initialSpriteColors[sr] = sr.color;
                }
            }
        }

        var allRenderers = this.GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRenderers)
        {
            if (r != null && !(r is SpriteRenderer) && !(r is ParticleSystemRenderer))
            {
                if (r.sharedMaterial != null && !initialMeshColors.ContainsKey(r))
                {
                    Color c = Color.white;
                    if (r.sharedMaterial.HasProperty("_BaseColor"))
                        c = r.sharedMaterial.GetColor("_BaseColor");
                    else if (r.sharedMaterial.HasProperty("_Color"))
                        c = r.sharedMaterial.GetColor("_Color");
                    initialMeshColors[r] = c;
                }
            }
        }

        var images = this.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
            if (img != null && !initialImageColors.ContainsKey(img))
            {
                initialImageColors[img] = img.color;
            }
        }

        if (textP != null)
        {
            initialTextColor = textP.color;
        }
    }

    private void OnDestroy()
    {
        if (!GameManager.IsApplicationQuitting && GameManager.HasInstance)
        {
            GameManager.Instance.UnregisterSlot(this);
        }
    }

    public bool IsInside(Vector3 point)
    {
        if (boxCollider != null)
        {
            Vector3 local = this.transform.InverseTransformPoint(point);
            Vector3 half = boxCollider.size * 0.5f;
            return Mathf.Abs(local.x - boxCollider.center.x) <= half.x &&
                   Mathf.Abs(local.z - boxCollider.center.z) <= half.z;
        }

        Vector2 p2D = new Vector2(point.x, point.z);
        Vector2 slot2D = new Vector2(this.transform.position.x, this.transform.position.z);
        return Vector2.Distance(p2D, slot2D) < 1.2f;
    }

    public void CompleteParking(CarController car)
    {
        if (car == null) return;
        if (isOccupied || car.isParked) return;

        if (slotColorType != ColorType.None && car.CarColorType != ColorType.None && car.CarColorType != slotColorType)
        {
            Debug.LogWarning($"<color=orange>⚠️ [ParkingSlot {gameObject.name}] Xe {car.name} (Màu {car.CarColorType}) không đúng màu ô đỗ (Màu {slotColorType})!</color>");
            return;
        }

        isOccupied = true;
        currentParkedCar = car;
        car.isParked = true;
        car.currentSlot = this;
        car.TotalDrawnDistance = car.GetTotalPathLength();

        if (currentFX != null)
        {
            ObjectPool.Despawn(currentFX);
            currentFX = null;
        }

        if (winEffectPrefab != null)
        {
            Vector3 effectPos = this.transform.position + Vector3.up * 0.4f;
            currentFX = ObjectPool.Spawn(winEffectPrefab, effectPos, Quaternion.identity);

            if (currentFX != null)
            {
                Color carColor = car.GetCarBodyColor();
                var particleSystems = currentFX.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    var main = ps.main;
                    if (ps.gameObject.name.Contains("Beam") || ps.gameObject.name.Contains("Glow"))
                    {
                        Color tint = carColor;
                        tint.a = 0.75f;
                        main.startColor = tint;
                    }
                    else if (ps.gameObject.name.Contains("Spark"))
                    {
                        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, carColor);
                    }
                }
            }

            ObjectPool.Despawn(currentFX, 4f);
        }

        ApplySlotColor(car.GetCarBodyColor());

        car.StopIdleAnimation();
        car.transform.DOPunchScale(new Vector3(0.3f, -0.25f, 0.3f), 0.5f, vibrato: 7)
            .OnComplete(() => car.StartIdleAnimation());

        if (Camera.main != null)
        {
            Camera.main.DOShakePosition(0.25f, strength: 0.25f, vibrato: 14);
        }

        Debug.Log($"<color=#00FF7F>🏁🎉 [Xe {car.name}] ĐÃ VỀ ĐÍCH ĐÚNG Ô MÀU {slotColorType}! Sau: {car.TotalDrawnDistance:F2} mét!</color>");

        GameManager.Instance.OnCarParked(car);
    }

    private void ApplySlotColor(Color targetColor)
    {
        float duration = 0.35f;

        if (slotSpriteRenderers != null)
        {
            foreach (var sr in slotSpriteRenderers)
            {
                if (sr != null)
                {
                    sr.DOKill();
                    sr.DOColor(targetColor, duration);
                }
            }
        }

        foreach (var r in initialMeshColors.Keys)
        {
            if (r != null && r.material != null)
            {
                r.material.DOKill();
                if (r.material.HasProperty("_BaseColor"))
                    r.material.DOColor(targetColor, "_BaseColor", duration);
                else if (r.material.HasProperty("_Color"))
                    r.material.DOColor(targetColor, "_Color", duration);
            }
        }

        foreach (var img in initialImageColors.Keys)
        {
            if (img != null)
            {
                img.DOKill();
                img.DOColor(targetColor, duration);
            }
        }

        if (textP != null)
        {
            textP.DOKill();
            textP.DOColor(parkedTextColor, duration);
        }

        transform.DOKill(complete: true);
        transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0.08f), duration, vibrato: 6, elasticity: 0.5f);
    }

    public void ResetSlot()
    {
        isOccupied = false;
        currentParkedCar = null;
        if (currentFX != null)
        {
            ObjectPool.Despawn(currentFX);
            currentFX = null;
        }

        ResetSlotColor();
    }

    private void ResetSlotColor()
    {
        float duration = 0.25f;

        foreach (var kvp in initialSpriteColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.DOKill();
                kvp.Key.DOColor(kvp.Value, duration);
            }
        }

        foreach (var kvp in initialMeshColors)
        {
            if (kvp.Key != null && kvp.Key.material != null)
            {
                kvp.Key.material.DOKill();
                if (kvp.Key.material.HasProperty("_BaseColor"))
                    kvp.Key.material.DOColor(kvp.Value, "_BaseColor", duration);
                else if (kvp.Key.material.HasProperty("_Color"))
                    kvp.Key.material.DOColor(kvp.Value, "_Color", duration);
            }
        }

        foreach (var kvp in initialImageColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.DOKill();
                kvp.Key.DOColor(kvp.Value, duration);
            }
        }

        if (textP != null)
        {
            textP.DOKill();
            Color defaultPColor = (slotColorType != ColorType.None) ? ColorPalette.GetColor(slotColorType) : initialTextColor;
            textP.DOColor(defaultPColor, duration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CarController car = other.GetComponentInParent<CarController>();
        if (car != null && !car.isFlyingBack && !car.isParked && !isOccupied)
        {
            if (IsInside(car.transform.position))
            {
                if (slotColorType == ColorType.None || car.CarColorType == ColorType.None || car.CarColorType == slotColorType)
                {
                    CompleteParking(car);
                }
            }
        }
    }
}