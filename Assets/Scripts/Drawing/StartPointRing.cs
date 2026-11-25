using UnityEngine;

public class StartPointRing : MonoBehaviour
{
    [Header("Kích thước vòng tròn")]
    public float radius = 0.65f;
    public float ringWidth = 0.12f;
    public int segments = 50;

    [Header("Hiệu ứng nhấp nháy (Pulsing)")]
    public float pulseSpeed = 10f;
    public float minScale = 0.9f;
    public float maxScale = 1.15f;
    public float minAlpha = 0.4f;
    public float maxAlpha = 1.0f;

    [HideInInspector] public CarController car;
    private LineRenderer ringLine;
    private Color baseColor = Color.white;

    private void Awake()
    {
        ringLine = this.GetComponent<LineRenderer>();
        if (ringLine == null) ringLine = gameObject.AddComponent<LineRenderer>();

        ringLine.useWorldSpace = false;
        ringLine.loop = true;
        ringLine.startWidth = ringWidth;
        ringLine.endWidth = ringWidth;
        ringLine.positionCount = segments;
        ringLine.numCornerVertices = 4;
        ringLine.numCapVertices = 4;
        ringLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringLine.receiveShadows = false;
        ringLine.sortingOrder = 50;

        EnsureMaterial();

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            ringLine.SetPosition(i, new Vector3(x, 0.05f, z));
        }

        ringLine.enabled = false;
    }

    private Material runtimeMaterial;

    public void SetColor(Color color)
    {
        baseColor = color;
        EnsureMaterial();
        UpdateLineColor(baseColor);
    }

    private void EnsureMaterial()
    {
        if (ringLine == null) return;
        if (runtimeMaterial != null) return;

        if (car != null)
        {
            LineRenderer carLine = car.GetComponentInChildren<LineRenderer>();
            if (carLine != null && carLine.sharedMaterial != null)
            {
                runtimeMaterial = new Material(carLine.sharedMaterial);
                ringLine.material = runtimeMaterial;
                return;
            }
        }

        if (ringLine.sharedMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Color");
            if (s != null)
            {
                runtimeMaterial = new Material(s);
                ringLine.material = runtimeMaterial;
            }
        }
        else if (Application.isPlaying)
        {
            runtimeMaterial = ringLine.material;
        }
    }

    private void UpdateLineColor(Color c)
    {
        if (ringLine == null) return;
        ringLine.startColor = c;
        ringLine.endColor = c;

        Material mat = runtimeMaterial != null ? runtimeMaterial : (Application.isPlaying ? ringLine.material : ringLine.sharedMaterial);
        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            mat.color = c;
        }
    }

    private void Update()
    {
        if (car == null) return;

        float dx = car.transform.position.x - car.startPosition.x;
        float dz = car.transform.position.z - car.startPosition.z;
        float sqrDist = dx * dx + dz * dz;

        bool isCarAwayFromStart = sqrDist > 0.16f || car.IsCrashed;

        if (ringLine != null && ringLine.enabled != isCarAwayFromStart)
        {
            ringLine.enabled = isCarAwayFromStart;
        }

        if (isCarAwayFromStart)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = new Vector3(scale, 1f, scale);

            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            Color c = baseColor;
            c.a = alpha;
            UpdateLineColor(c);
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}
