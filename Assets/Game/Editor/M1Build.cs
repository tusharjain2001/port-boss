using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PortBoss.M1;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PortBoss.EditorTools
{
    // Builds M1 (boat + fishing) headless:
    //   Unity -batchmode -quit -projectPath . -executeMethod PortBoss.EditorTools.M1Build.SetupAndBuild -pbOut <dir> [-pbDev]
    // Imports and sets up everything from code (no hand-edited scene): fonts, our Blender FBX (boat, landing, 15 fish),
    // the Mixamo clips (gitignored, Manan's downloads), the Ponyo pipeline (PC copy + outline pass), M1Assets, the scene.
    public static class M1Build
    {
        const string Root = "Assets/Game", Scene = "Assets/Scenes/M1.unity", Settings = Root + "/Settings";
        const string Roster = "C:/Users/asus/Desktop/Port Boss/plan/gdd/fishing/fish-roster.csv";

        [MenuItem("Port Boss/M1 setup")]
        public static void Setup()
        {
            Dir(Settings); Dir(Root + "/Resources"); Dir(Root + "/Data");
            // the fish roster from the design sheet (data only: change the sheet, rebuild)
            var csvPath = Root + "/Data/fish-roster.csv";
            var src = Environment.GetEnvironmentVariable("PB_ROSTER") ?? Roster;
            if (File.Exists(src)) { File.Copy(src, csvPath, true); Debug.Log("[M1Build] roster copied from " + src); }
            AssetDatabase.Refresh();

            var a = ScriptableObject.CreateInstance<M1Assets>();
            a.FishCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
            a.Toon = Shader.Find("PortBoss/M1/Toon"); a.Water = Shader.Find("PortBoss/M1/Water"); a.Sky = Shader.Find("PortBoss/M1/Sky");
            a.Particle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Font F(string n) => AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/" + n);
            a.Display = F("Fraunces-SemiBold.ttf"); a.DisplayBold = F("Fraunces-Bold.ttf"); a.Ui = F("ZenMaruGothic-Medium.ttf"); a.UiBold = F("ZenMaruGothic-Bold.ttf"); a.UiBlack = F("ZenMaruGothic-Black.ttf");

            a.Boat = Model(Root + "/Art/PB_Boat_Biscuit_T1.fbx"); a.Landing = Model(Root + "/Art/PB_Landing_Walts.fbx");
            var ids = Directory.Exists(Root + "/Art/Fish") ? Directory.GetFiles(Root + "/Art/Fish", "PB_Fish_*.fbx").Select(p => p.Replace('\\', '/')).ToArray() : new string[0];
            a.FishIds = ids.Select(p => Path.GetFileNameWithoutExtension(p).Substring("PB_Fish_".Length)).ToArray();
            a.Fish = ids.Select(Model).ToArray();
            Debug.Log($"[M1Build] boat {(a.Boat != null)}, landing {(a.Landing != null)}, fish {a.Fish.Count(f => f != null)}: {string.Join(" ", a.FishIds)}");

            // our dock worker + the Mixamo clips: reuse the old build's importer setup (reflection: they're private there)
            var pb = typeof(PortBossBuild);
            a.Worker = (GameObject)pb.GetMethod("OurWorker", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            var skin = ScriptableObject.CreateInstance<Skin>();
            pb.GetMethod("MixamoClips", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { skin });
            a.Idle = skin.Idle; a.Walk = skin.Walk; a.Run = skin.Run; a.Sprint = skin.Sprint; a.Jump = skin.Jump; a.Land = skin.Land;
            a.SwimIdle = skin.SwimIdle; a.Swim = skin.Swim; a.Climb = skin.Climb; a.Drive = skin.Drive;
            Debug.Log($"[M1Build] clips idle={a.Idle?.name} walk={a.Walk?.name} run={a.Run?.name} swim={a.Swim?.name} tread={a.SwimIdle?.name} climb={a.Climb?.name}");

            AudioClip Au(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/" + n);
            a.Waves = Au("waves.mp3"); a.Splash = Au("splash.ogg"); a.Gull = Au("gull.wav"); a.Thud = Au("thud.ogg"); a.Kaching = Au("kaching.ogg"); a.Click = Au("click.ogg");
            a.Steps = Enumerable.Range(0, 5).Select(i => Au($"step{i}.ogg")).Where(c => c != null).ToArray();

            a.Pipeline = Pipeline();
            // runtime-made materials don't tell the build which shader variants they need: this asset keeps the
            // transparent particle variant in (without it splashes, wakes and halos drew as opaque squares)
            const string sp = Settings + "/M1_SoftParticle.mat";
            AssetDatabase.DeleteAsset(sp);
            var pm = new Material(a.Particle) { name = "M1_SoftParticle" };
            pm.SetFloat("_Surface", 1); pm.SetFloat("_Blend", 0); pm.SetFloat("_ZWrite", 0);
            pm.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); pm.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            pm.SetOverrideTag("RenderType", "Transparent"); pm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); pm.renderQueue = 3000;
            AssetDatabase.CreateAsset(pm, sp);
            a.SoftParticle = pm;
            const string ap = Root + "/Resources/M1Assets.asset";
            AssetDatabase.DeleteAsset(ap); AssetDatabase.CreateAsset(a, ap);
            AssetDatabase.SaveAssets();

            // the scene: one object, the rest is built at runtime
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Game").AddComponent<Game>();
            EditorSceneManager.SaveScene(sc, Scene);
            Debug.Log("[M1Build] setup done");
        }

        static GameObject Model(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi) { Debug.LogWarning("[M1Build] missing " + path); return null; }
            if (mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importAnimation || mi.importCameras || mi.importLights || !mi.isReadable)
            {
                mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.isReadable = true; // collider meshes are read at runtime
                mi.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // PC pipeline copy + a second renderer for the icon camera, the outline pass (Ponyo: 1.2 px warm brown, 75 %).
        static UniversalRenderPipelineAsset Pipeline()
        {
            string rp = Settings + "/M1_RPAsset.asset", rd = Settings + "/M1_Renderer.asset", ri = Settings + "/M1_IconRenderer.asset", om = Settings + "/M1_Outline.mat";
            foreach (var p in new[] { rp, rd, ri, om }) AssetDatabase.DeleteAsset(p);
            AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", rd);
            AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", ri);
            AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset", rp);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rd);
            var icon = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ri);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rp);
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            list.arraySize = 2; list.GetArrayElementAtIndex(0).objectReferenceValue = renderer; list.GetArrayElementAtIndex(1).objectReferenceValue = icon;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.FindProperty("m_SupportsHDR").boolValue = true;
            so.FindProperty("m_ShadowDistance").floatValue = 90;
            so.ApplyModifiedPropertiesWithoutUndo();

            var mat = new Material(Shader.Find("PortBoss/M1/Outline"));
            mat.SetColor("_OutlineColor", new Color(0.5f, 0.36f, 0.32f, 0.75f)); mat.SetFloat("_Thickness", 1.2f); mat.SetFloat("_DepthSens", 10); mat.SetFloat("_NormalSens", 1.6f); mat.SetFloat("_FadeDist", 220);
            AssetDatabase.CreateAsset(mat, om);
            var f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            f.name = "PonyoOutline"; f.passMaterial = mat;
            f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
            f.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color;
            AssetDatabase.AddObjectToAsset(f, renderer);
            renderer.rendererFeatures.Add(f);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long lid);
            var rso = new SerializedObject(renderer);
            var map = rso.FindProperty("m_RendererFeatureMap");
            map.InsertArrayElementAtIndex(map.arraySize); map.GetArrayElementAtIndex(map.arraySize - 1).longValue = lid;
            rso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer); AssetDatabase.SaveAssets();
            return asset;
        }

        public static void Build()
        {
            var outDir = Arg("-pbOut") ?? "Builds/m1";
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(outDir, "PortBoss.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = Array.IndexOf(Environment.GetCommandLineArgs(), "-pbDev") >= 0 ? BuildOptions.Development : BuildOptions.None,
            };
            var r = BuildPipeline.BuildPlayer(opts);
            if (File.Exists("steam_appid.txt")) File.Copy("steam_appid.txt", Path.Combine(outDir, "steam_appid.txt"), true);
            Debug.Log($"[M1Build] build {r.summary.result} {r.summary.totalSize / 1e6:0}MB errors={r.summary.totalErrors} -> {opts.locationPathName}");
            if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        public static void SetupAndBuild() { Setup(); Build(); }

        static void Dir(string p) { if (!AssetDatabase.IsValidFolder(p)) Directory.CreateDirectory(p); }
        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
