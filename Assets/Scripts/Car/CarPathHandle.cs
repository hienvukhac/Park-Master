using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CarPathHandle : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [HideInInspector] public List<Vector3> pathPoints = new List<Vector3>();

    private float lastFinishedDistance = 0f;
    public float TotalDrawDistance
    {
        get
        {
            float length = GetTotalPathLength();
            return length > 0.05 ? length : lastFinishedDistance;
        }

        set
        {
            lastFinishedDistance = value;
        }
    }

    private void Awake()
    {
        if(lineRenderer == null)
            lineRenderer = this.GetComponentInChildren<LineRenderer>();

        if (lineRenderer != null)
            lineRenderer.positionCount = 0;

    }

    private Vector3[] positionsBuffer;
    private Material runtimeLineMaterial;

    private void OnDestroy()
    {
        if (runtimeLineMaterial != null)
        {
            Destroy(runtimeLineMaterial);
            runtimeLineMaterial = null;
        }
    }

    public void AddPoints(Vector3 newPoint, float fixedY)
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
            Vector3 smoothed = (pathPoints[i - 1] + pathPoints[i] * 2 + pathPoints[i + 1]) / 4f;
            smoothed.y = fixedY;
            pathPoints[i] = smoothed;
        }

        TotalDrawDistance = GetTotalPathLength();
        RefreshLineRenderer();
    }

    public float GetTotalPathLength()
    {
        if (pathPoints == null || pathPoints.Count < 2) return 0f;

        float total = 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            float dx = pathPoints[i + 1].x - pathPoints[i].x;
            float dz = pathPoints[i + 1].z - pathPoints[i].z;
            total += Mathf.Sqrt(dx * dx + dz * dz);
        }

        return total;
    }

    public void RefreshLineRenderer()
    {
        if (lineRenderer == null) return;

        int count = pathPoints.Count;
        lineRenderer.positionCount = count;

        if (count == 0) return;

        if (positionsBuffer == null || positionsBuffer.Length < count)
        {
            int newCap = Mathf.Max(count, (positionsBuffer?.Length ?? 0) * 2);
            positionsBuffer = new Vector3[newCap];
        }

        for (int i = 0; i < count; i++)
        {
            positionsBuffer[i] = pathPoints[i];
        }

        lineRenderer.SetPositions(positionsBuffer);
    }

    public void ClearPath()
    {
        pathPoints.Clear();
        lastFinishedDistance = 0f;
        if (lineRenderer != null) lineRenderer.positionCount = 0;
    }

    public void ApplyColorToLine(Color bodyColor)
    {
        if (lineRenderer == null) return;

        bodyColor.a = 1f;
        float brightness = (bodyColor.r + bodyColor.g + bodyColor.b) / 3f;
        if (brightness > 0.85f || brightness < 0.2f)
        {
            bodyColor = new Color(1f, 0.75f, 0.05f, 1f);
        }

        lineRenderer.startColor = bodyColor;
        lineRenderer.endColor = bodyColor;

        if (Application.isPlaying)
        {
            if (runtimeLineMaterial == null)
            {
                runtimeLineMaterial = lineRenderer.material;
            }
            if (runtimeLineMaterial != null)
            {
                if (runtimeLineMaterial.HasProperty("_BaseColor")) runtimeLineMaterial.SetColor("_BaseColor", bodyColor);
                if (runtimeLineMaterial.HasProperty("_Color")) runtimeLineMaterial.SetColor("_Color", bodyColor);
                runtimeLineMaterial.color = bodyColor;
            }
        }
        else if (lineRenderer.sharedMaterial != null)
        {
            Material sharedMat = lineRenderer.sharedMaterial;
            if (sharedMat.HasProperty("_BaseColor")) sharedMat.SetColor("_BaseColor", bodyColor);
            if (sharedMat.HasProperty("_Color")) sharedMat.SetColor("_Color", bodyColor);
            sharedMat.color = bodyColor;
        }
    }
}
