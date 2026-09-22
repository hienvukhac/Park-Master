#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateWinEffectPrefab : MonoBehaviour
{
    [MenuItem("Tools/Tạo Hiệu Ứng Ô P ")]
    public static void GenerateWinFX()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");

        GameObject rootFX = new GameObject("ParkingWinEffect");

        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ParkingWin.mat");
        if (particleMat == null)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Mobile/Particles/Additive");
            particleMat = new Material(particleShader);

            Texture2D squareTex = BuildSoftSquareTexture();
            string sqTexPath = "Assets/Materials/Tex_SquareSparkle.png";
            File.WriteAllBytes(sqTexPath, squareTex.EncodeToPNG());
            AssetDatabase.ImportAsset(sqTexPath);
            particleMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sqTexPath);

            AssetDatabase.CreateAsset(particleMat, "Assets/Materials/Mat_ParkingWin.mat");
        }

        Material beamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_LightBeam.mat");
        if (beamMat == null)
        {
            Shader beamShader = Shader.Find("Particles/Additive")
                ?? Shader.Find("Mobile/Particles/Additive")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            beamMat = new Material(beamShader);

            Texture2D gradTex = BuildBeamGradientTexture();
            string texPath = "Assets/Materials/Tex_BeamGradient.png";
            File.WriteAllBytes(texPath, gradTex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            beamMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (beamMat.HasProperty("_TintColor"))
                beamMat.SetColor("_TintColor", new Color(0.55f, 0.9f, 1f, 0.6f));

            AssetDatabase.CreateAsset(beamMat, "Assets/Materials/Mat_LightBeam.mat");
        }

        Mesh cylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);

        GameObject beamObj = new GameObject("LightBeam");
        beamObj.transform.SetParent(rootFX.transform, false);
        ParticleSystem beamPS = beamObj.AddComponent<ParticleSystem>();

        var bMain = beamPS.main;
        bMain.duration = 1.2f;
        bMain.loop = false;
        bMain.startLifetime = 1.0f;
        bMain.startSpeed = 0f;
        bMain.startSize = 1f;
        bMain.startColor = new Color(0.55f, 0.9f, 1f, 0.55f);
        bMain.simulationSpace = ParticleSystemSimulationSpace.World;
        bMain.gravityModifier = 0f;
        bMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var bEmission = beamPS.emission;
        bEmission.rateOverTime = 0;
        bEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var bShape = beamPS.shape;
        bShape.enabled = false;

        var bSize = beamPS.sizeOverLifetime;
        bSize.enabled = true;
        bSize.separateAxes = true;
        AnimationCurve growY = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(1f, 1f));
        AnimationCurve steadyXZ = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        bSize.y = new ParticleSystem.MinMaxCurve(3.2f, growY);
        bSize.x = new ParticleSystem.MinMaxCurve(0.5f, steadyXZ);
        bSize.z = new ParticleSystem.MinMaxCurve(0.5f, steadyXZ);

        var bColor = beamPS.colorOverLifetime;
        bColor.enabled = true;
        Gradient beamGrad = new Gradient();
        beamGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.9f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.15f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        bColor.color = beamGrad;

        var bRotation = beamPS.rotationOverLifetime;
        bRotation.enabled = true;
        bRotation.separateAxes = true;
        bRotation.y = new ParticleSystem.MinMaxCurve(25f * Mathf.Deg2Rad);
        bRotation.x = new ParticleSystem.MinMaxCurve(0f);
        bRotation.z = new ParticleSystem.MinMaxCurve(0f);

        var bRenderer = beamObj.GetComponent<ParticleSystemRenderer>();
        bRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        bRenderer.mesh = cylinderMesh;
        bRenderer.sharedMaterial = beamMat;
        bRenderer.alignment = ParticleSystemRenderSpace.World;
        bRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject flashObj = new GameObject("GroundGlow");
        flashObj.transform.SetParent(rootFX.transform, false);
        ParticleSystem flashPS = flashObj.AddComponent<ParticleSystem>();

        var fMain = flashPS.main;
        fMain.duration = 0.6f;
        fMain.loop = false;
        fMain.startLifetime = 0.5f;
        fMain.startSpeed = 0f;
        fMain.startSize = 3f;
        fMain.startColor = new Color(0.6f, 0.9f, 1f, 0.8f);
        fMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var fEmission = flashPS.emission;
        fEmission.rateOverTime = 0;
        fEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

        var fSizeOverLife = flashPS.sizeOverLifetime;
        fSizeOverLife.enabled = true;
        AnimationCurve fCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
        fSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, fCurve);

        var fRenderer = flashObj.GetComponent<ParticleSystemRenderer>();
        fRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        fRenderer.sharedMaterial = particleMat;

        GameObject sparkObj = new GameObject("SquareSparkles");
        sparkObj.transform.SetParent(rootFX.transform, false);
        ParticleSystem sparkPS = sparkObj.AddComponent<ParticleSystem>();

        var pMain = sparkPS.main;
        pMain.duration = 1.0f;
        pMain.loop = false;
        pMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        pMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        pMain.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f); 
        pMain.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.85f, 0.3f));
        pMain.gravityModifier = 0.6f;  
        pMain.startRotation3D = false;
        pMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        pMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var pShape = sparkPS.shape;
        pShape.shapeType = ParticleSystemShapeType.Cone;
        pShape.angle = 30f;
        pShape.radius = 0.15f;
        pShape.rotation = new Vector3(-90f, 0f, 0f);  

        var pEmission = sparkPS.emission;
        pEmission.rateOverTime = 0;
        pEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 24) });

        var pRotOverLife = sparkPS.rotationOverLifetime;
        pRotOverLife.enabled = true;
        pRotOverLife.z = new ParticleSystem.MinMaxCurve(180f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

        var pColorOverLife = sparkPS.colorOverLifetime;
        pColorOverLife.enabled = true;
        Gradient sparkGrad = new Gradient();
        sparkGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.3f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        pColorOverLife.color = sparkGrad;

        var pRenderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        pRenderer.renderMode = ParticleSystemRenderMode.Billboard;  
        pRenderer.sharedMaterial = particleMat;

        string prefabPath = "Assets/Prefabs/ParkingWinEffect.prefab";
        PrefabUtility.SaveAsPrefabAsset(rootFX, prefabPath);
        DestroyImmediate(rootFX);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=cyan>✨ Đã tạo lại Prefab hiệu ứng ô P (cột trụ sáng + hạt vuông bắn lên, an toàn với camera top-down) tại: " + prefabPath + "</color>");
    }

    private static void EnsureFolder(string assetsRelativePath)
    {
        string full = Application.dataPath + assetsRelativePath.Substring("Assets".Length);
        if (!Directory.Exists(full)) Directory.CreateDirectory(full);
    }

    private static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(temp);
        return mesh;
    }

    private static Texture2D BuildBeamGradientTexture()
    {
        int h = 64;
        Texture2D tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            float alpha = Mathf.SmoothStep(1f, 0f, t);
            Color c = new Color(1f, 1f, 1f, alpha);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D BuildSoftSquareTexture()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float edge = size * 0.12f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Min(x, size - 1 - x);
                float dy = Mathf.Min(y, size - 1 - y);
                float d = Mathf.Min(dx, dy);
                float alpha = Mathf.Clamp01(d / edge);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}
#endif