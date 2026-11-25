#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PlayerPrefsViewerWindow : EditorWindow
{
    private Vector2 scrollPos;
    private int selectedTab = 0;
    private readonly string[] tabs = new string[] { "💰 Tiền Tệ (Coins)", "🏆 Màn Chơi (Levels)", "🚗 Xe (Cars)", "🛠️ Tất Cả / Tùy Chỉnh" };

    private int totalCoins;

    private int currentLevel;
    private int maxUnlockedLevel;
    private int totalLevelsCount = 5;

    private string customKeyName = "";
    private string customKeyValue = "";
    private int customKeyType = 0;

    [MenuItem("Tools/Park Master/Quản Lý Dữ Liệu Người Chơi (PlayerPrefs Viewer) %#p")]
    public static void OpenWindow()
    {
        var win = GetWindow<PlayerPrefsViewerWindow>("PlayerPrefs Viewer");
        win.minSize = new Vector2(480, 520);
        win.Show();
    }

    private void OnEnable()
    {
        LoadAllData();
    }

    private void LoadAllData()
    {
        totalCoins = PlayerPrefs.GetInt("ParkMaster_TotalCoins", 0);
        currentLevel = PlayerPrefs.GetInt("ParkMaster_CurrentLevel", 1);
        maxUnlockedLevel = PlayerPrefs.GetInt("ParkMaster_MaxUnlockedLevel", 1);

        if (LevelManager.Instance != null)
        {
            totalLevelsCount = Mathf.Max(5, LevelManager.Instance.GetTotalLevelsCount());
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        DrawTopToolbar();
        EditorGUILayout.Space(6);

        selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(30));
        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (selectedTab)
        {
            case 0:
                DrawCoinsTab();
                break;
            case 1:
                DrawLevelsTab();
                break;
            case 2:
                DrawCarsTab();
                break;
            case 3:
                DrawCustomTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTopToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("🔄 Làm Mới (Refresh)", EditorStyles.toolbarButton))
        {
            LoadAllData();
            GUI.FocusControl(null);
        }

        if (GUILayout.Button("💾 Lưu Thay Đổi (Save)", EditorStyles.toolbarButton))
        {
            PlayerPrefs.Save();
            ShowNotification(new GUIContent("Đã lưu PlayerPrefs!"));
        }

        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑️ Xóa Hết (Reset All)", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("Xác nhận xóa hết dữ liệu",
                "Bạn có chắc chắn muốn xóa TOÀN BỘ PlayerPrefs?\nTiền sẽ về 0, Level về 1 và khóa hết xe.", "Xóa", "Hủy"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                LoadAllData();
                ShowNotification(new GUIContent("Đã reset toàn bộ PlayerPrefs!"));
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCoinsTab()
    {
        EditorGUILayout.LabelField("💰 QUẢN LÝ TIỀN XU (COINS)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Key: 'ParkMaster_TotalCoins'", MessageType.None);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tổng số xu hiện tại:", GUILayout.Width(140));
        int newCoins = EditorGUILayout.IntField(totalCoins);
        if (newCoins != totalCoins)
        {
            totalCoins = Mathf.Max(0, newCoins);
            PlayerPrefs.SetInt("ParkMaster_TotalCoins", totalCoins);
            PlayerPrefs.Save();
            if (CoinManager.Instance != null) CoinManager.Instance.UpdateCoinTextInstant();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 100 Xu", GUILayout.Height(26)))
        {
            totalCoins += 100;
            PlayerPrefs.SetInt("ParkMaster_TotalCoins", totalCoins);
            PlayerPrefs.Save();
            if (CoinManager.Instance != null) CoinManager.Instance.UpdateCoinTextInstant();
        }
        if (GUILayout.Button("+ 1,000 Xu", GUILayout.Height(26)))
        {
            totalCoins += 1000;
            PlayerPrefs.SetInt("ParkMaster_TotalCoins", totalCoins);
            PlayerPrefs.Save();
            if (CoinManager.Instance != null) CoinManager.Instance.UpdateCoinTextInstant();
        }
        if (GUILayout.Button("+ 10,000 Xu", GUILayout.Height(26)))
        {
            totalCoins += 10000;
            PlayerPrefs.SetInt("ParkMaster_TotalCoins", totalCoins);
            PlayerPrefs.Save();
            if (CoinManager.Instance != null) CoinManager.Instance.UpdateCoinTextInstant();
        }
        if (GUILayout.Button("Đặt về 0", GUILayout.Height(26)))
        {
            totalCoins = 0;
            PlayerPrefs.SetInt("ParkMaster_TotalCoins", totalCoins);
            PlayerPrefs.Save();
            if (CoinManager.Instance != null) CoinManager.Instance.UpdateCoinTextInstant();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawLevelsTab()
    {
        EditorGUILayout.LabelField("🏆 TIẾN TRÌNH MÀN CHƠI (LEVELS)", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Màn chơi đang chọn (Current):", GUILayout.Width(200));
        int newCurr = EditorGUILayout.IntSlider(currentLevel, 1, Mathf.Max(1, totalLevelsCount));
        if (newCurr != currentLevel)
        {
            currentLevel = newCurr;
            PlayerPrefs.SetInt("ParkMaster_CurrentLevel", currentLevel);
            PlayerPrefs.Save();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Màn mở khóa cao nhất (Max Unlocked):", GUILayout.Width(200));
        int newMax = EditorGUILayout.IntSlider(maxUnlockedLevel, 1, Mathf.Max(1, totalLevelsCount));
        if (newMax != maxUnlockedLevel)
        {
            maxUnlockedLevel = newMax;
            PlayerPrefs.SetInt("ParkMaster_MaxUnlockedLevel", maxUnlockedLevel);
            PlayerPrefs.Save();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mở Khóa Tất Cả Level", GUILayout.Height(24)))
        {
            maxUnlockedLevel = totalLevelsCount;
            PlayerPrefs.SetInt("ParkMaster_MaxUnlockedLevel", maxUnlockedLevel);
            for (int i = 1; i <= totalLevelsCount; i++)
            {
                if (PlayerPrefs.GetInt("ParkMaster_Stars_L" + i, 0) == 0)
                    PlayerPrefs.SetInt("ParkMaster_Stars_L" + i, 3);
            }
            PlayerPrefs.Save();
            ShowNotification(new GUIContent("Đã mở khóa tất cả Level!"));
        }

        if (GUILayout.Button("Reset Tiến Trình Level", GUILayout.Height(24)))
        {
            for (int i = 1; i <= 50; i++)
            {
                PlayerPrefs.DeleteKey("ParkMaster_Stars_L" + i);
                PlayerPrefs.DeleteKey("ParkMaster_Len_L" + i);
            }
            currentLevel = 1;
            maxUnlockedLevel = 1;
            PlayerPrefs.SetInt("ParkMaster_CurrentLevel", 1);
            PlayerPrefs.SetInt("ParkMaster_MaxUnlockedLevel", 1);
            PlayerPrefs.Save();
            ShowNotification(new GUIContent("Đã reset Level về 1!"));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Danh Sách Chi Tiết Từng Level:", EditorStyles.boldLabel);

        for (int i = 1; i <= totalLevelsCount; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            bool isUnlocked = i <= maxUnlockedLevel;
            GUI.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.7f);

            EditorGUILayout.LabelField($"Level {i:00}", GUILayout.Width(60));

            int stars = PlayerPrefs.GetInt("ParkMaster_Stars_L" + i, 0);
            string starStr = stars switch
            {
                3 => "⭐⭐⭐ (3 Sao)",
                2 => "⭐⭐ (2 Sao)",
                1 => "⭐ (1 Sao)",
                _ => "⚪ (0 Sao)"
            };
            EditorGUILayout.LabelField(starStr, GUILayout.Width(100));

            float len = PlayerPrefs.GetFloat("ParkMaster_Len_L" + i, 0f);
            EditorGUILayout.LabelField(len > 0 ? $"{len:F1}m" : "-- m", GUILayout.Width(70));

            GUI.color = Color.white;
            if (GUILayout.Button("⭐+1", GUILayout.Width(45)))
            {
                int nextStars = (stars + 1) > 3 ? 0 : stars + 1;
                PlayerPrefs.SetInt("ParkMaster_Stars_L" + i, nextStars);
                if (nextStars > 0 && i >= maxUnlockedLevel)
                {
                    maxUnlockedLevel = Mathf.Min(totalLevelsCount, i + 1);
                    PlayerPrefs.SetInt("ParkMaster_MaxUnlockedLevel", maxUnlockedLevel);
                }
                PlayerPrefs.Save();
            }

            if (GUILayout.Button("Chơi Màn Này", GUILayout.Width(90)))
            {
                currentLevel = i;
                PlayerPrefs.SetInt("ParkMaster_CurrentLevel", currentLevel);
                PlayerPrefs.Save();
                if (LevelManager.Instance != null && Application.isPlaying)
                {
                    LevelManager.Instance.LoadLevel(i);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCarsTab()
    {
        EditorGUILayout.LabelField("🚗 QUẢN LÝ XE & SHOP", EditorStyles.boldLabel);

        int selectedId = PlayerPrefs.GetInt("SelectedCarID", 1001);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Xe đang chọn (SelectedCarID):", GUILayout.Width(200));
        int newId = EditorGUILayout.IntField(selectedId);
        if (newId != selectedId)
        {
            PlayerPrefs.SetInt("SelectedCarID", newId);
            PlayerPrefs.Save();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mở Khóa Tất Cả Xe", GUILayout.Height(24)))
        {
            SetAllCarsUnlocked(true);
            ShowNotification(new GUIContent("Đã mở khóa toàn bộ xe!"));
        }
        if (GUILayout.Button("Khóa Tất Cả Xe (Trừ xe đầu)", GUILayout.Height(24)))
        {
            SetAllCarsUnlocked(false);
            ShowNotification(new GUIContent("Đã khóa lại toàn bộ xe!"));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Danh sách xe từ CarDatabase:", EditorStyles.boldLabel);

        CarDatabase db = (GameManager.HasInstance && GameManager.Instance.CarDatabase != null)
                        ? GameManager.Instance.CarDatabase
                        : Resources.Load<CarDatabase>("CarDatabase");

        if (db != null && db.CarList != null)
        {
            foreach (var car in db.CarList)
            {
                if (car == null) continue;
                bool isUnlocked = car.IsUnlocked();
                bool isCurrent = car.Id == selectedId;

                EditorGUILayout.BeginHorizontal("box");
                if (isCurrent) GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);

                EditorGUILayout.LabelField($"ID: {car.Id}", GUILayout.Width(65));
                EditorGUILayout.LabelField(car.Name, GUILayout.Width(130));

                GUI.color = isUnlocked ? Color.green : Color.gray;
                EditorGUILayout.LabelField(isUnlocked ? "ĐÃ MỞ" : "ĐANG KHÓA", GUILayout.Width(80));
                GUI.color = Color.white;

                if (!car.IsDefaultUnlocked)
                {
                    if (GUILayout.Button(isUnlocked ? "Khóa" : "Mở Khóa", GUILayout.Width(75)))
                    {
                        PlayerPrefs.SetInt("Car_Unlocked" + car.Id, isUnlocked ? 0 : 1);
                        PlayerPrefs.Save();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("(Mặc định)", GUILayout.Width(75));
                }

                if (GUILayout.Button("Chọn Xe", GUILayout.Width(70)))
                {
                    PlayerPrefs.SetInt("SelectedCarID", car.Id);
                    PlayerPrefs.Save();
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Không tìm thấy CarDatabase trong Resources!", MessageType.Info);
        }
    }

    private void SetAllCarsUnlocked(bool unlock)
    {
        CarDatabase db = (GameManager.HasInstance && GameManager.Instance.CarDatabase != null)
                        ? GameManager.Instance.CarDatabase
                        : Resources.Load<CarDatabase>("CarDatabase");
        if (db != null && db.CarList != null)
        {
            foreach (var car in db.CarList)
            {
                if (car != null && !car.IsDefaultUnlocked)
                {
                    PlayerPrefs.SetInt("Car_Unlocked" + car.Id, unlock ? 1 : 0);
                }
            }
            PlayerPrefs.Save();
        }
    }

    private void DrawCustomTab()
    {
        EditorGUILayout.LabelField("🛠️ TÙY CHỈNH KEY BẤT KỲ", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Nhập tên Key chính xác trong PlayerPrefs để đọc, sửa hoặc xóa.", MessageType.Info);

        EditorGUILayout.BeginVertical("box");
        customKeyName = EditorGUILayout.TextField("Tên Key:", customKeyName);
        customKeyType = EditorGUILayout.Popup("Loại dữ liệu:", customKeyType, new string[] { "Số nguyên (int)", "Số thực (float)", "Chuỗi (string)" });
        customKeyValue = EditorGUILayout.TextField("Giá trị:", customKeyValue);

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Đọc Giá Trị (Get)", GUILayout.Height(24)))
        {
            if (!string.IsNullOrEmpty(customKeyName))
            {
                if (PlayerPrefs.HasKey(customKeyName))
                {
                    customKeyValue = customKeyType switch
                    {
                        0 => PlayerPrefs.GetInt(customKeyName).ToString(),
                        1 => PlayerPrefs.GetFloat(customKeyName).ToString("F2"),
                        2 => PlayerPrefs.GetString(customKeyName),
                        _ => ""
                    };
                    ShowNotification(new GUIContent("Đã đọc được giá trị!"));
                }
                else
                {
                    EditorUtility.DisplayDialog("Thông báo", $"Key '{customKeyName}' chưa tồn tại trong PlayerPrefs!", "OK");
                }
            }
        }

        if (GUILayout.Button("Ghi / Cập Nhật (Set)", GUILayout.Height(24)))
        {
            if (!string.IsNullOrEmpty(customKeyName))
            {
                try
                {
                    switch (customKeyType)
                    {
                        case 0:
                            PlayerPrefs.SetInt(customKeyName, int.Parse(customKeyValue));
                            break;
                        case 1:
                            PlayerPrefs.SetFloat(customKeyName, float.Parse(customKeyValue));
                            break;
                        case 2:
                            PlayerPrefs.SetString(customKeyName, customKeyValue);
                            break;
                    }
                    PlayerPrefs.Save();
                    LoadAllData();
                    ShowNotification(new GUIContent($"Đã lưu Key: {customKeyName}"));
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("Lỗi định dạng", $"Giá trị không hợp lệ: {ex.Message}", "OK");
                }
            }
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Xóa Key Này", GUILayout.Height(24)))
        {
            if (!string.IsNullOrEmpty(customKeyName) && PlayerPrefs.HasKey(customKeyName))
            {
                PlayerPrefs.DeleteKey(customKeyName);
                PlayerPrefs.Save();
                customKeyValue = "";
                LoadAllData();
                ShowNotification(new GUIContent($"Đã xóa Key: {customKeyName}"));
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
}
#endif
