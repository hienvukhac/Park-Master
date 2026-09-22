using UnityEngine;

[System.Serializable]
public class CarData
{
    [SerializeField] private int id;
    [SerializeField] private string name;
    [Tooltip("Sprite icon hiển thị trong ô Grid Menu Shop")]
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject car3DPrefab;
    [SerializeField] private bool isDefaultUnlocked;
    [Tooltip("Định danh màu mặc định của xe (Red, Blue, Yellow, Green...)")]
    [SerializeField] private ColorType defaultColorType = ColorType.Red;

    public int Id => id;
    public string Name => name;
    public Sprite Icon => icon;
    public GameObject Car3dPrefab => car3DPrefab;
    public bool IsDefaultUnlocked => isDefaultUnlocked;
    public ColorType ColorType => defaultColorType;
    public Color CarColor => ColorPalette.GetColor(defaultColorType);

    public void SetIcon(Sprite newIcon)
    {
        icon = newIcon;
    }

    public void SetColorType(ColorType type)
    {
        defaultColorType = type;
    }

    public bool IsUnlocked()
    {
        if (isDefaultUnlocked) return true;
        return PlayerPrefs.GetInt("Car_Unlocked" + id, 0) == 1;
    }

    public void Unlock()
    {
        PlayerPrefs.SetInt("Car_Unlocked" + id, 1);
        PlayerPrefs.Save();
    }
    
}
