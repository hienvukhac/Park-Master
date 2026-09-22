#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetupParkMasterUI : EditorWindow
{
    [MenuItem("Tools/Cài Đặt Hiệu Ứng Level Complete")]
    public static void GenerateLevelCompleteUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        GameObject winTextObj = GetOrCreateUIGameObject(canvas.transform, "Txt_LevelComplete");
        RectTransform winTextRt = winTextObj.GetComponent<RectTransform>();
        winTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        winTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        winTextRt.pivot = new Vector2(0.5f, 0.5f);
        winTextRt.anchoredPosition = new Vector2(0f, 180f);
        winTextRt.sizeDelta = new Vector2(650f, 140f);

        TextMeshProUGUI winTMP = winTextObj.GetComponent<TextMeshProUGUI>() ?? winTextObj.AddComponent<TextMeshProUGUI>();
        winTMP.text = "LEVEL COMPLETE";
        winTMP.fontSize = 54;
        winTMP.fontStyle = FontStyles.Bold;
        winTMP.alignment = TextAlignmentOptions.Center;
        winTMP.raycastTarget = false;

        winTMP.enableVertexGradient = true;
        winTMP.colorGradient = new VertexGradient(
            new Color(1f, 0.96f, 0.46f, 1f),
            new Color(1f, 0.96f, 0.46f, 1f),
            new Color(1f, 0.62f, 0.0f, 1f),
            new Color(1f, 0.62f, 0.0f, 1f)
        );

        LevelCompleteUI winScript = canvas.GetComponent<LevelCompleteUI>() ?? canvas.gameObject.AddComponent<LevelCompleteUI>();
        SerializedObject winSo = new SerializedObject(winScript);
        winSo.FindProperty("textRect").objectReferenceValue = winTextRt;
        winSo.FindProperty("levelCompleteTMP").objectReferenceValue = winTMP;
        winSo.ApplyModifiedProperties();

        winTextObj.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("<color=#00FF7F>✅🎉 ĐÃ CÀI ĐẶT THÀNH CÔNG: Hiệu Ứng Level Complete (Chữ Vàng Óng + Pháo Giấy 2 Bên)!</color>");
        EditorUtility.DisplayDialog("Cài đặt thành công!",
            "Đã cài đặt thành công:\n- Chữ 'LEVEL COMPLETE' màu vàng óng (chếch lên trên một chút)\n- 2 vòi pháo giấy nhỏ ti ti bắn 2 bên mép màn hình trong đúng 2 giây.\n\nPhần Menu Car được giữ nguyên theo thiết lập ban đầu của bạn!\nHãy bấm Play để thử nghiệm.", "OK");
    }

    private static GameObject GetOrCreateUIGameObject(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            if (existing.GetComponent<RectTransform>() != null)
            {
                return existing.gameObject;
            }
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    [MenuItem("Tools/Park Master/Xóa Toàn Bộ Dữ Liệu Lưu (Reset PlayerPrefs)")]
    public static void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<color=green>[PlayerPrefs] Đã xóa toàn bộ dữ liệu đã lưu (Coin = 0, Level = 1, Xe reset về mặc định)!</color>");
        EditorUtility.DisplayDialog("Thành công", "Đã xóa toàn bộ PlayerPrefs!\nKhi chạy lại game hoặc build mới, Coin sẽ về 0 và Level về 1.", "OK");
    }
}
#endif
