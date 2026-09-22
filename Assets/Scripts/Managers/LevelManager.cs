using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    public static LevelManager Instance => instance;

    [Header("Danh sách Level Prefabs (Ưu tiên theo thứ tự)")]
    [Tooltip("Kéo các Prefab Level_01, Level_02... vào đây (hoặc để trống để tự động tìm)")]
    [SerializeField] private List<GameObject> levelPrefabs = new List<GameObject>();

    [Header("UI Text")]
    [Tooltip("TextMeshPro hiển thị 'Level 1' trên đỉnh màn hình (tự động tìm nếu để trống)")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Cấu hình tiến trình Level")]
    [Tooltip("Nếu bật, game sẽ luôn bắt đầu từ Level 1 mỗi lần mở game thay vì load từ tiến trình đã lưu")]
    [SerializeField] private bool alwaysStartFromLevel1 = false;

    private int currentLevel = 1;
    public int CurrentLevel => currentLevel;

    private GameObject currentLevelInstance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        AutoFindReferences();
    }

    private void Start()
    {
        currentLevel = alwaysStartFromLevel1 ? 1 : PlayerPrefs.GetInt("ParkMaster_CurrentLevel", 1);
        LoadLevel(currentLevel);
    }

    private void AutoFindReferences()
    {
        if (levelText == null)
        {
            GameObject hudObj = GameObject.Find("HUD") ?? GameObject.Find("HUB");
            if (hudObj != null)
            {
                Transform tLevel = hudObj.transform.Find("Level");
                if (tLevel != null)
                {
                    levelText = tLevel.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var tmps = hudObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps)
                    {
                        if (t != null && t.gameObject.name.ToLower().Contains("level"))
                        {
                            levelText = t;
                            break;
                        }
                    }
                }
            }

            if (levelText == null)
            {
                TextMeshProUGUI[] allTmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in allTmps)
                {
                    if (t == null) continue;
                    string n = t.gameObject.name.ToLower();
                    if (n.Contains("complete") || n.Contains("victory") || n.Contains("shop") || n.Contains("coin") || n.Contains("card"))
                    {
                        continue;
                    }

                    if (n == "level" || n == "txt_level" || n == "leveltext" || n == "txtlevel")
                    {
                        levelText = t;
                        break;
                    }
                }
            }
        }

        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            GameObject[] resLevels = Resources.LoadAll<GameObject>("Levels");
            if (resLevels != null && resLevels.Length > 0)
            {
                levelPrefabs = new List<GameObject>(resLevels);
                levelPrefabs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            }
        }

#if UNITY_EDITOR
        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            levelPrefabs = new List<GameObject>();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("Level_ t:Prefab", new[] { "Assets/Prefabs/Levels", "Assets/Resources/Levels" });
            foreach (var g in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                GameObject p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null && !levelPrefabs.Contains(p))
                {
                    levelPrefabs.Add(p);
                }
            }

            levelPrefabs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }
#endif
    }

    public int GetTotalLevelsCount()
    {
        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            AutoFindReferences();
        }

        return (levelPrefabs != null) ? levelPrefabs.Count : 0;
    }

    public void LoadLevel(int levelNum)
    {
        int total = GetTotalLevelsCount();
        if (total > 0 && levelNum > total)
        {
            levelNum = 1;
        }

        currentLevel = Mathf.Max(1, levelNum);
        PlayerPrefs.SetInt("ParkMaster_CurrentLevel", currentLevel);
        PlayerPrefs.Save();

        UpdateLevelText();

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
        CleanupExistingSceneLevels();

        GameObject prefabToSpawn = null;

        if (total > 0 && levelPrefabs != null && levelPrefabs.Count >= currentLevel)
        {
            prefabToSpawn = levelPrefabs[currentLevel - 1];
        }
        else
        {
            prefabToSpawn = Resources.Load<GameObject>($"Levels/Level_{currentLevel:00}") 
                         ?? Resources.Load<GameObject>($"Levels/Level_{currentLevel}");
        }

#if UNITY_EDITOR
        if (prefabToSpawn == null)
        {
            string pPath = $"Assets/Prefabs/Levels/Level_{currentLevel:00}.prefab";
            prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            if (prefabToSpawn == null)
            {
                pPath = $"Assets/Prefabs/Levels/Level_{currentLevel}.prefab";
                prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            }
        }
#endif

        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = prefabToSpawn.transform.position;
            Quaternion spawnRot = prefabToSpawn.transform.rotation;
            currentLevelInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            currentLevelInstance.name = $"--- CURRENT_LEVEL_{currentLevel} ({prefabToSpawn.name}) ---";
        }
        else
        {
            Debug.LogError($"[LevelManager] Không tìm thấy Prefab cho Level {currentLevel}!");
            return;
        }

        AlignCameraToLevel(currentLevelInstance);

        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnLevelLoaded(currentLevelInstance);
        }

        var lineDrawer = FindFirstObjectByType<LineDrawer>();
        if (lineDrawer != null)
        {
            lineDrawer.RefreshRoadColliders();
            lineDrawer.RefreshParkingSlots();
        }

        Debug.Log($"<color=#00FF7F>🏁🎉 [LevelManager] ĐÃ LOAD LEVEL {currentLevel} ({prefabToSpawn.name}) THÀNH CÔNG!</color>");
    }

    private void CleanupExistingSceneLevels()
    {
        var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in allRoots)
        {
            if (root == null || root == this.gameObject) continue;
            string n = root.name.ToLower();
            if ((n.StartsWith("level_") || n.StartsWith("level ") || n.Contains("current_level")) && !n.Contains("manager"))
            {
                Destroy(root);
            }
        }
    }

    private void AlignCameraToLevel(GameObject levelObj)
    {
        if (levelObj == null) return;
        Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Bounds levelBounds = new Bounds();
        bool hasBounds = false;

        Collider[] cols = levelObj.GetComponentsInChildren<Collider>();
        foreach (var c in cols)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            string n = c.gameObject.name.ToLower();
            if (n.Contains("road") || n.Contains("grass") || n.Contains("slot") || n.Contains("parking"))
            {
                if (!hasBounds)
                {
                    levelBounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    levelBounds.Encapsulate(c.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            Renderer[] rends = levelObj.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r is LineRenderer) continue;
                if (!hasBounds)
                {
                    levelBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    levelBounds.Encapsulate(r.bounds);
                }
            }
        }

        if (hasBounds)
        {
            Vector3 center = levelBounds.center;
            float pitchAngle = cam.transform.eulerAngles.x;
            if (pitchAngle > 10f && pitchAngle < 89f)
            {
                float camY = cam.transform.position.y;
                float heightDiff = camY - center.y;
                float deltaZ = heightDiff / Mathf.Tan(pitchAngle * Mathf.Deg2Rad);

                Vector3 newCamPos = cam.transform.position;
                newCamPos.x = center.x;
                newCamPos.z = center.z - deltaZ;
                cam.transform.position = newCamPos;
            }
        }
    }

    public void NextLevel()
    {
        int total = GetTotalLevelsCount();
        if (total > 0 && currentLevel >= total)
        {
            Debug.Log($"<color=#FFD700>🎉 ĐÃ VƯỢT QUA MÀN CUỐI CÙNG (Level {currentLevel}/{total})! Tự động quay về Level 1.</color>");
            LoadLevel(1);
        }
        else
        {
            LoadLevel(currentLevel + 1);
        }
    }

    public void RestartLevel()
    {
        LoadLevel(currentLevel);
    }

    private void UpdateLevelText()
    {
        if (levelText == null)
        {
            AutoFindReferences();
        }

        if (levelText != null)
        {
            levelText.text = $"Level {currentLevel}";
        }
    }
}
