using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CarVisual : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;

    [Tooltip("Loại của màu xe")]
    [SerializeField] private ColorType carColorType = ColorType.Red;
    public ColorType CarColorType { get => carColorType; set => carColorType = value; }

    [Header("Tùy chỉnh màu thủ công")]
    [SerializeField] private Color customCarColor = Color.clear;
    private Color assignedColor = Color.clear;

    private static MaterialPropertyBlock materialPropertyBlock;

    public void InitVisual(Action<Color> onColorFound)
    {
        FindBestBodyRenderer();
        onColorFound?.Invoke(GetCarBodyColor());
    }

    public void FindBestBodyRenderer()
    {
        if (bodyRenderer != null) return;

        Renderer[] allRends = this.GetComponentsInChildren<Renderer>(true);
        Renderer largestRenderer = null;
        float maxVolume = 0;

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
                    if (m != null &&
                        (m.name.ToLower().Contains("paint") ||
                        m.name.ToLower().Contains("body") ||
                        m.name.ToLower().Contains("car")))
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
            bodyRenderer = largestRenderer;
    }

    public void SetCarColor(Color color)
    {
        assignedColor = color;
        ApplyColorToCarMesh(color);
    }

    public void ApplyColorToCarMesh(Color color)
    {
        if (materialPropertyBlock == null) materialPropertyBlock = new MaterialPropertyBlock();
        FindBestBodyRenderer();

        Renderer[] allRends = this.GetComponentsInChildren<Renderer>(true);
        bool paintedAny = false;

        foreach (var r in allRends)
        {
            if (r == null || r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer) continue;
            bool isPaint = false;

            if (r == bodyRenderer ||
                r.gameObject.name.ToLower().Contains("body") ||
                r.gameObject.name.ToLower().Contains("chassis"))
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
                        if (mName.Contains("paint") ||
                            mName.Contains("body") ||
                            mName.Contains("color") ||
                            mName.Contains("car") ||
                            mName.Contains("skin"))
                        {
                            isPaint = true;
                            break;
                        }
                    }
                }
            }

            if (isPaint)
            {
                r.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor("_BaseColor", color);
                materialPropertyBlock.SetColor("_Color", color);
                r.SetPropertyBlock(materialPropertyBlock);

                paintedAny = true;
            }
        }

        if (!paintedAny && bodyRenderer != null)
        {
            bodyRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetColor("_BaseColor", color);
            materialPropertyBlock.SetColor("_Color", color);
            bodyRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    public Color GetCarBodyColor()
    {
        if (assignedColor.a > 0.05f) return assignedColor;
        if (customCarColor.a > 0.05f) return customCarColor;
        if (carColorType != ColorType.None) return ColorPalette.GetColor(carColorType);

        CarDatabase db = (GameManager.HasInstance && GameManager.Instance.CarDatabase != null)
                        ? GameManager.Instance.CarDatabase
                        : Resources.Load<CarDatabase>("CarDatabase");

        if (db != null && db.CarList != null)
        {
            string cleanName = this.gameObject.name.Replace("(Clone)", "").Trim();
            var match = db.CarList.Find(c => cleanName.Equals(c.Name, StringComparison.OrdinalIgnoreCase)
                                           || cleanName.StartsWith(c.Name, StringComparison.OrdinalIgnoreCase)
                                           || (c.Car3dPrefab != null && cleanName.Contains(c.Car3dPrefab.name)));
            if (match != null && match.CarColor.a > 0.05f) return match.CarColor;
        }

        FindBestBodyRenderer();
        if (bodyRenderer != null)
        {
            Material mat = bodyRenderer.sharedMaterial;
            if (mat != null)
            {
                Color c = (mat.HasProperty("_BaseColor")) ? mat.GetColor("_BaseColor") :
                          (mat.HasProperty("_Color")) ? mat.GetColor("_Color") : mat.color;
                if (c.r < 0.15f && c.g < 0.15f && c.b < 0.15f) return new Color(1f, 0.8f, 0f, 1f);
                return c;
            }
        }
        return new Color(1f, 0.25f, 0.25f, 1f);
    }
}
