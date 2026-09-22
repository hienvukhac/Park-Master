#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateCoinPickupFX : MonoBehaviour
{
    private const string PREFAB_DIR = "Assets/Prefabs";
    private const string COIN_VFX_PATH = "Assets/Prefabs/CoinPickupFX.prefab";
    private const string FLY_COIN_PATH = "Assets/Prefabs/UIFlyCoin.prefab";

    [InitializeOnLoadMethod]
    private static void AutoRunOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(COIN_VFX_PATH))
            {
                SetupEntireCoinSystem();
            }
        };
    }

    [MenuItem("Tools/Cài Đặt Hệ Thống Ăn Xu")]
    public static void SetupEntireCoinSystem()
    {
        if (!Directory.Exists(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
        }

        GameObject vfxPrefab = GenerateCoinPickupVFXPrefab();

        GameObject flyPrefab = GenerateUIFlyCoinPrefab();

        UpdateCoinPrefab(vfxPrefab);

        SetupCoinManagerInScene(flyPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=#00FF7F><b>[CoinSystem] 🎉 CÀI ĐẶT HỆ THỐNG ĂN XU HOÀN TẤT THÀNH CÔNG!</b></color>\n" +
                  "- Đã tạo hiệu ứng hạt 3D: " + COIN_VFX_PATH + "\n" +
                  "- Đã tạo Prefab UI bay: " + FLY_COIN_PATH + "\n" +
                  "- Đã cập nhật Coin.prefab với SphereCollider (Trigger) và CoinItem\n" +
                  "- Đã cấu hình CoinManager trong Scene liên kết với TextMeshPro và CoinIcon!");
    }

    public static GameObject GenerateCoinPickupVFXPrefab()
    {
        GameObject rootFX = new GameObject("CoinPickupFX");

        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WinParticle.mat");
        if (particleMat == null)
        {
            particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                                     ?? Shader.Find("Universal Render Pipeline/Unlit") 
                                     ?? Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(particleMat, "Assets/Materials/Mat_CoinParticle.mat");
        }

        ParticleSystem ps = rootFX.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
        main.gravityModifier = 0.8f;
        main.stopAction = ParticleSystemStopAction.None;

        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);

        Gradient goldGrad = new Gradient();
        goldGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0.0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.1f), 0.5f),
                new GradientColorKey(new Color(1f, 0.5f, 0.05f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        main.startColor = goldGrad;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 35) });

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.7f, 0.8f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.drag = 1.8f;

        var renderer = rootFX.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = particleMat;

        GameObject waveObj = new GameObject("ShockwaveRing");
        waveObj.transform.SetParent(rootFX.transform, false);
        waveObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem wavePS = waveObj.AddComponent<ParticleSystem>();

        var wMain = wavePS.main;
        wMain.duration = 0.5f;
        wMain.loop = false;
        wMain.startLifetime = 0.35f;
        wMain.startSpeed = 0f;
        wMain.startSize = 0.3f;
        wMain.startColor = new Color(1f, 0.9f, 0.3f, 0.9f);

        var wEmission = wavePS.emission;
        wEmission.rateOverTime = 0;
        wEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

        var wSizeOverLife = wavePS.sizeOverLifetime;
        wSizeOverLife.enabled = true;
        AnimationCurve wCurve = new AnimationCurve();
        wCurve.AddKey(0f, 0.3f);
        wCurve.AddKey(1f, 3.2f);
        wSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, wCurve);

        var wColorOverLife = wavePS.colorOverLifetime;
        wColorOverLife.enabled = true;
        Gradient wGrad = new Gradient();
        wGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        wColorOverLife.color = wGrad;

        var wRenderer = waveObj.GetComponent<ParticleSystemRenderer>();
        wRenderer.sharedMaterial = particleMat;

        GameObject flashObj = new GameObject("FlashGlow");
        flashObj.transform.SetParent(rootFX.transform, false);
        ParticleSystem flashPS = flashObj.AddComponent<ParticleSystem>();

        var fMain = flashPS.main;
        fMain.duration = 0.3f;
        fMain.loop = false;
        fMain.startLifetime = 0.2f;
        fMain.startSpeed = 0f;
        fMain.startSize = 1.8f;
        fMain.startColor = new Color(1f, 1f, 0.8f, 0.95f);

        var fEmission = flashPS.emission;
        fEmission.rateOverTime = 0;
        fEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

        var fSizeOverLife = flashPS.sizeOverLifetime;
        fSizeOverLife.enabled = true;
        AnimationCurve fCurve = new AnimationCurve();
        fCurve.AddKey(0f, 0.2f);
        fCurve.AddKey(0.15f, 1.0f);
        fCurve.AddKey(1f, 0f);
        fSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, fCurve);

        var fRenderer = flashObj.GetComponent<ParticleSystemRenderer>();
        fRenderer.sharedMaterial = particleMat;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootFX, COIN_VFX_PATH);
        DestroyImmediate(rootFX);
        return savedPrefab;
    }

    public static GameObject GenerateUIFlyCoinPrefab()
    {
        GameObject flyObj = new GameObject("UIFlyCoin");
        RectTransform rt = flyObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(75f, 75f);

        CanvasRenderer cr = flyObj.AddComponent<CanvasRenderer>();
        Image img = flyObj.AddComponent<Image>();
        img.raycastTarget = false;

        Sprite coinSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/coin.png");
        if (coinSp == null)
        {
            string path = AssetDatabase.GUIDToAssetPath("e59c27c7fc3810f4ebf094a1dfc16dea");
            if (!string.IsNullOrEmpty(path))
            {
                coinSp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }

        if (coinSp != null)
        {
            img.sprite = coinSp;
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(flyObj, FLY_COIN_PATH);
        DestroyImmediate(flyObj);
        return savedPrefab;
    }

    public static void UpdateCoinPrefab(GameObject vfxPrefab)
    {
        string coinPrefabPath = "Assets/Prefabs/Coin.prefab";
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(coinPrefabPath);
        if (coinPrefab == null)
        {
            Debug.LogWarning("[CoinSystem] Không tìm thấy " + coinPrefabPath);
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(coinPrefabPath);

        SphereCollider sc = prefabRoot.GetComponent<SphereCollider>();
        if (sc == null)
        {
            sc = prefabRoot.AddComponent<SphereCollider>();
        }
        sc.isTrigger = true;
        sc.radius = 0.9f;
        sc.center = Vector3.zero;

        Rigidbody rb = prefabRoot.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = prefabRoot.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        CoinItem coinItem = prefabRoot.GetComponent<CoinItem>();
        if (coinItem == null)
        {
            coinItem = prefabRoot.AddComponent<CoinItem>();
        }

        SerializedObject so = new SerializedObject(coinItem);
        SerializedProperty vfxProp = so.FindProperty("pickupVfxPrefab");
        if (vfxProp != null && vfxPrefab != null)
        {
            vfxProp.objectReferenceValue = vfxPrefab;
        }
        so.ApplyModifiedProperties();

        prefabRoot.tag = "Coin";

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, coinPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    public static void SetupCoinManagerInScene(GameObject flyPrefab)
    {
        CoinManager cm = Object.FindFirstObjectByType<CoinManager>();
        if (cm == null)
        {
            GameObject cmObj = new GameObject("CoinManager");
            cm = cmObj.AddComponent<CoinManager>();
            Undo.RegisterCreatedObjectUndo(cmObj, "Create CoinManager");
        }

        SerializedObject so = new SerializedObject(cm);

        GameObject coinValueObj = GameObject.Find("CoinValue");
        if (coinValueObj != null)
        {
            TextMeshProUGUI tmp = coinValueObj.GetComponent<TextMeshProUGUI>();
            SerializedProperty textProp = so.FindProperty("coinCountText");
            if (textProp != null && tmp != null)
            {
                textProp.objectReferenceValue = tmp;
            }
        }

        GameObject coinIconObj = GameObject.Find("CoinIcon");
        if (coinIconObj != null)
        {
            RectTransform iconRt = coinIconObj.GetComponent<RectTransform>();
            SerializedProperty iconProp = so.FindProperty("targetIconRect");
            if (iconProp != null && iconRt != null)
            {
                iconProp.objectReferenceValue = iconRt;
            }
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            SerializedProperty canvasProp = so.FindProperty("canvas");
            if (canvasProp != null)
            {
                canvasProp.objectReferenceValue = canvas;
            }
        }

        if (flyPrefab != null)
        {
            SerializedProperty flyProp = so.FindProperty("flyCoinUIPrefab");
            if (flyProp != null)
            {
                flyProp.objectReferenceValue = flyPrefab;
            }
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif
