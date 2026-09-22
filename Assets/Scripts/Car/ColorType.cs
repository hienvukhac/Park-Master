using UnityEngine;

public enum ColorType
{
    None = 0,
    Red = 1,
    Blue = 2,
    Yellow = 3,
    Green = 4,
    Orange = 5,
    Purple = 6,
    Pink = 7,
    Cyan = 8
}

public static class ColorPalette
{
    private static readonly Color RedColor    = new Color(1.0f, 0.22f, 0.22f, 1.0f);  
    private static readonly Color BlueColor   = new Color(0.18f, 0.55f, 1.0f, 1.0f);  
    private static readonly Color YellowColor = new Color(1.0f, 0.85f, 0.1f, 1.0f);   
    private static readonly Color GreenColor  = new Color(0.18f, 0.85f, 0.35f, 1.0f);  
    private static readonly Color OrangeColor = new Color(1.0f, 0.52f, 0.1f, 1.0f);  
    private static readonly Color PurpleColor = new Color(0.7f, 0.25f, 0.95f, 1.0f); 
    private static readonly Color PinkColor   = new Color(1.0f, 0.4f, 0.75f, 1.0f);   
    private static readonly Color CyanColor   = new Color(0.1f, 0.9f, 0.95f, 1.0f);  

    public static Color GetColor(ColorType type)
    {
        switch (type)
        {
            case ColorType.Red:    return RedColor;
            case ColorType.Blue:   return BlueColor;
            case ColorType.Yellow: return YellowColor;
            case ColorType.Green:  return GreenColor;
            case ColorType.Orange: return OrangeColor;
            case ColorType.Purple: return PurpleColor;
            case ColorType.Pink:   return PinkColor;
            case ColorType.Cyan:   return CyanColor;
            case ColorType.None:
            default:
                return Color.white;
        }
    }

    public static string GetColorHex(ColorType type)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(GetColor(type));
    }
}
