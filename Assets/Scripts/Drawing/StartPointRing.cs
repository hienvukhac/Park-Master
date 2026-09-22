using UnityEngine;

public class StartPointRing : MonoBehaviour
{
    [Header("Kích thước vòng tròn")]
    public float radius = 0.6f;        
    public float ringWidth = 0.1f;      
    public int segments = 50;           

    [Header("Hiệu ứng nhấp nháy (Pulsing)")]
    public float pulseSpeed = 20f;     
    public float minScale = 0.95f;     
    public float maxScale = 1.18f;      
    public float minAlpha = 0.4f;
    public float maxAlpha = 1.0f;

    [HideInInspector] public CarController car;
    private LineRenderer ringLine;
    private Color baseColor = Color.white;

    private void Awake()
    {
        SetupRing();
    }

    private void SetupRing()
    {
        ringLine = this.gameObject.AddComponent<LineRenderer>();
        ringLine.useWorldSpace = false;
        ringLine.loop = true;
        ringLine.startWidth = ringWidth;
        ringLine.endWidth = ringWidth;
        ringLine.positionCount = segments;
        ringLine.numCornerVertices = 4;
        ringLine.numCapVertices = 4;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        ringLine.material = mat;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            ringLine.SetPosition(i, new Vector3(x, 0.03f, z));
        }

        ringLine.enabled = false;
    }


    public void SetColor(Color color)
    {
        baseColor = color;
        if (ringLine != null)
        {
            ringLine.startColor = color;
            ringLine.endColor = color;
            if (ringLine.material != null)
            {
                if (ringLine.material.HasProperty("_BaseColor")) ringLine.material.SetColor("_BaseColor", color);
                if (ringLine.material.HasProperty("_Color")) ringLine.material.SetColor("_Color", color);
                ringLine.material.color = color;
            }
        }
    }

    private void Update()
    {
        if (car == null) return;

        bool isCarAwayFromStart = Vector3.Distance(car.transform.position, car.startPosition) > 0.5f || car.isCrashed;
        if (ringLine != null) ringLine.enabled = isCarAwayFromStart;

        if (isCarAwayFromStart)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = new Vector3(scale, 1f, scale);

            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            Color c = baseColor;
            c.a = alpha;

            if (ringLine != null)
            {
                ringLine.startColor = c;
                ringLine.endColor = c;
                if (ringLine.material != null)
                {
                    if (ringLine.material.HasProperty("_BaseColor")) ringLine.material.SetColor("_BaseColor", c);
                    if (ringLine.material.HasProperty("_Color")) ringLine.material.SetColor("_Color", c);
                    ringLine.material.color = c;
                }
            }
        }
    }
}