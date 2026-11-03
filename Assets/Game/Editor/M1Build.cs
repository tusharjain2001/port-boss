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
            // map B, Crescent Bay (art-src/world/build_island.py -- export): the cells, the props, the anchors, the seabed
            const string wd = Root + "/Art/World";
            a.Cells = Directory.Exists(wd) ? Directory.GetFiles(wd, "PB_Island_c*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p).Select(Model).ToArray() : new GameObject[0];
            a.WorldProps = Model(wd + "/PB_World_Props.fbx"); a.WorldJson = AssetDatabase.LoadAssetAtPath<TextAsset>(wd + "/PB_World.json");
            a.Seabed = SeabedTex(wd + "/PB_Seabed.png");
            Debug.Log($"[M1Build] map: {a.Cells.Count(c => c != null)} cells, props {(a.WorldProps != null)}, json {(a.WorldJson != null)}, seabed {(a.Seabed != null ? a.Seabed.format.ToString() : "missing")}");

            // our dock worker + the Mixamo clips: reuse the old build's importer setup (reflection: they're private there)
            var pb = typeof(PortBossBuild);
            a.Worker = (GameObject)pb.GetMethod("OurWorker", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            var skin = ScriptableObject.CreateInstance<Skin>();
            pb.GetMethod("MixamoClips", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { skin });
            a.Idle = skin.Idle; a.Walk = skin.Walk; a.Run = skin.Run; a.Sprint = skin.Sprint; a.Jump = skin.Jump; a.Land = skin.Land;
            a.SwimIdle = skin.SwimIdle; a.Swim = skin.Swim; a.Climb = skin.Climb; a.Drive = skin.Drive;
            M1Anim.Make(a.Worker, a.Idle, a);
            a.JumpUp = Mixamo("Jumping Up"); a.Throw = Mixamo("Frisbee Throw"); a.GetUpBack = Mixamo("Getting Up"); a.GetUpFront = Mixamo("Standing Up"); a.FallIn = Mixamo("Falling Into Pool");
            Debug.Log($"[M1Build] feel clips: jump {a.JumpUp?.length:F2} s, throw {a.Throw?.length:F2} s, get up (back) {a.GetUpBack?.length:F2} s, (front) {a.GetUpFront?.length:F2} s, fall in {a.FallIn?.length:F2} s");
            // the crew (art-src/kit/build_kit.py), each with how low its upper arms may go (round and broad bodies: higher)
            var crew = new (string name, float armDown)[] { ("Pip", -25), ("Bruno", -30), ("Stretch", -55), ("Rani", -40), ("Gus", -55), ("Sully", -35) }
                .Where(c => File.Exists(Root + "/Art/PB_Char_" + c.name + ".fbx")).ToArray();
            a.Crew = crew.Select(c => Character(Root + "/Art/PB_Char_" + c.name + ".fbx", c.armDown)).ToArray();
            a.CrewNames = crew.Select(c => c.name).ToArray();
            a.CrewBelly = a.Crew.Select(Belly).ToArray();
            a.Pip = crew.Length > 0 && crew[0].name == "Pip" ? a.Crew[0] : null;
            Debug.Log("[M1Build] crew: " + string.Join(", ", a.CrewNames));
            if (a.Pip != null) Feel(a);
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

        // The seabed: 16-bit, linear, read on the CPU too (Seabed.cs), clamped, bilinear, no mips, no compression
        static Texture2D SeabedTex(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) { Debug.LogWarning("[M1Build] missing " + path); return null; }
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = false; ti.mipmapEnabled = false; ti.isReadable = true;
            ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Bilinear; ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaSource = TextureImporterAlphaSource.None; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 2048;
            var ps = ti.GetPlatformTextureSettings("Standalone"); ps.overridden = true; ps.format = TextureImporterFormat.R16; ps.maxTextureSize = 2048; ps.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SetPlatformTextureSettings(ps); ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // A body's belly, from its bind-pose mesh: how far it sticks out in front (+Z in the imported prefab) and half its
        // width, between 45% and 62% of its height (the game holds the rod just in front of it).
        static Vector2 Belly(GameObject prefab)
        {
            if (prefab == null) return Vector2.zero;
            var go = UnityEngine.Object.Instantiate(prefab); go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var head = go.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                float h = (head.position.y - go.transform.position.y) / 0.82f, front = 0, half = 0;
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    foreach (var v in r.sharedMesh.vertices)
                    {
                        var p = go.transform.InverseTransformPoint(r.transform.TransformPoint(v));
                        if (p.y < 0.45f * h || p.y > 0.62f * h || Mathf.Abs(p.x) > 0.35f * h) continue;
                        front = Mathf.Max(front, p.z); half = Mathf.Max(half, Mathf.Abs(p.x));
                    }
                Debug.Log($"[M1Build] {prefab.name}: height {h:F2} m, belly {front:F3} m out, {half:F3} m half-wide");
                return new Vector2(front, half);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        // One more Mixamo clip (gitignored, Assets/ThirdParty/Mixamo), imported like the old build's: on X Bot's avatar,
        // turned round to face +Z like our bodies, the height kept in the pose, no root motion across the ground.
        static AnimationClip Mixamo(string name, bool loop = false)
        {
            string dir = "Assets/ThirdParty/Mixamo", path = dir + "/X Bot@" + name + ".fbx";
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi) { Debug.LogWarning("[M1Build] missing " + path); return null; }
            var rig = AssetDatabase.LoadAssetAtPath<Avatar>(dir + "/X Bot.fbx");
            mi.animationType = ModelImporterAnimationType.Human;
            if (rig != null) { mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; mi.sourceAvatar = rig; }
            mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false;
            var takes = mi.defaultClipAnimations;
            foreach (var c in takes) { c.loopTime = loop; c.lockRootRotation = c.lockRootHeightY = true; c.lockRootPositionXZ = false; c.keepOriginalOrientation = false; c.rotationOffset = 180f; c.keepOriginalPositionY = true; }
            mi.clipAnimations = takes; mi.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__"));
        }

        // What the game needs to know about the clips it drives itself, sampled on Pip. AnimationMode moves the object by a
        // clip's root motion, which the game never applies, so bones are measured from the object: the hips' lift above
        // standing (Jumping Up, Falling Into Pool), and the get-up's start (hips, hips -> head) and creep along the ground.
        static void Feel(M1Assets a)
        {
            var go = UnityEngine.Object.Instantiate(a.Pip); go.hideFlags = HideFlags.HideAndDontSave;
            var an = go.GetComponentInChildren<Animator>();
            Transform hips = an.GetBoneTransform(HumanBodyBones.Hips), head = an.GetBoneTransform(HumanBodyBones.Head);
            AnimationMode.StartAnimationMode();
            try
            {
                Vector3 At(AnimationClip c, float t, Transform b) { AnimationMode.SampleAnimationClip(an.gameObject, c, t); return b.position - go.transform.position; }
                float rest = At(a.Idle, 0, hips).y;
                AnimationCurve Lift(AnimationClip c)
                {
                    var k = new AnimationCurve();
                    if (c != null) for (float t = 0; t <= c.length + 1e-4f; t += 1 / 30f) k.AddKey(t, At(c, t, hips).y - rest);
                    return k;
                }
                a.JumpLift = Lift(a.JumpUp); a.FallLift = Lift(a.FallIn);
                // the get-ups, both starting face down: Getting Up (pushes up onto the knees, 0.95-4.9 s at 1.8x = 2.2 s) and
                // Standing Up (rolls up more slowly, 3.4-8.7 s at 2.2x = 2.4 s)
                var ups = new System.Collections.Generic.List<M1Assets.GetUpClip>();
                foreach (var (c, from, to, speed) in new[] { (a.GetUpBack, 0.95f, 4.9f, 1.8f), (a.GetUpFront, 3.4f, 8.7f, 2.2f) })
                {
                    if (c == null) continue;
                    var g = new M1Assets.GetUpClip { Clip = c, From = from, To = to, Speed = speed, MoveX = new AnimationCurve(), MoveZ = new AnimationCurve() };
                    g.Hips = At(c, from, hips); g.Spine = At(c, from, head) - g.Hips;
                    AnimationMode.SampleAnimationClip(an.gameObject, c, from); var g0 = go.transform.position;
                    for (float t = from; t <= to + 1e-4f; t += 1 / 30f)
                    {
                        AnimationMode.SampleAnimationClip(an.gameObject, c, t);
                        var d = go.transform.position - g0; g.MoveX.AddKey(t, d.x); g.MoveZ.AddKey(t, d.z);
                    }
                    ups.Add(g);
                    Debug.Log($"[M1Build] get-up {c.name}: from hips {g.Hips} spine {g.Spine}, creeps {g.MoveZ.Evaluate(to):F2} m");
                }
                a.GetUps = ups.ToArray();
                Debug.Log($"[M1Build] feel: rest hips {rest:F2} m, jump lifts {a.JumpLift.Evaluate(0.83f):F2} at the top, {a.GetUps.Length} get-ups");
            }
            finally { AnimationMode.StopAnimationMode(); UnityEngine.Object.DestroyImmediate(go); }
        }

        // A character from the kit (art-src/kit/build_kit.py): a Humanoid on X Bot's joints, so the Mixamo clips play on it.
        // armDown: the lowest the upper arms go (the muscle "Arm Down-Up" range, default -60): a round body keeps every
        // clip's arms out from the belly instead of through it (the joints stay Mixamo-shaped, editor-kit §2b).
        static GameObject Character(string path, float armDown = -60)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi) { Debug.LogWarning("[M1Build] missing " + path); return null; }
            if (mi.animationType != ModelImporterAnimationType.Human || mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importAnimation || !mi.isReadable)
            {
                mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.importBlendShapes = false;
                mi.isReadable = true; // co-op repaints the player colour (armband, back patch) from the vertex colours at runtime
                mi.SaveAndReimport();
            }
            var hd = mi.humanDescription; var hb = hd.human; bool changed = false;
            for (int i = 0; i < hb.Length; i++)
            {
                if (hb[i].humanName != "LeftUpperArm" && hb[i].humanName != "RightUpperArm") continue;
                var want = new HumanLimit { useDefaultValues = false, min = new Vector3(-90, -100, armDown), max = new Vector3(90, 100, 100), center = Vector3.zero };
                if (!hb[i].limit.useDefaultValues && hb[i].limit.min == want.min) continue;
                hb[i].limit = want; changed = true;
            }
            if (changed) { hd.human = hb; mi.humanDescription = hd; mi.SaveAndReimport(); }
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            Debug.Log($"[M1Build] {Path.GetFileName(path)}: avatar valid={avatar != null && avatar.isValid} human={avatar != null && avatar.isHuman}");
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
