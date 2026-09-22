using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarDatabase", menuName = "ParkMaster/Car Database")]
public class CarDatabase : ScriptableObject
{
    [Header("Danh Sách Toàn Bộ Xe Trong Game")]
    [SerializeField] private List<CarData> carList = new List<CarData>();

    public List<CarData> CarList => carList;
    public int CarCount => carList != null ? carList.Count : 0;

    public CarData GetCarByIndex(int index)
    {
        if (carList != null && index >= 0 && index < carList.Count)
            return carList[index];
        return null;
    }

    public CarData GetCarById(int id)
    {
        if (carList == null) return null;
        return carList.Find(c => c.Id == id);
    }
}