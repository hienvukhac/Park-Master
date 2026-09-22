#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InspectCarMeshes
{
    [MenuItem("Tools/Inspect All Scene Cars")]
    public static void InspectAllCars()
    {
        CarController[] cars = Object.FindObjectsByType<CarController>(FindObjectsSortMode.None);
        Debug.Log($"[InspectCarMeshes] Tìm thấy {cars.Length} xe trong Scene.");
    }
}
#endif
