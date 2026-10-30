using System;
using System.IO;
using System.Reflection;
using PortBoss.Net;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PortBoss.EditorTools
{
    // Generates materials, network prefabs and the Port scene from code, then builds.
    // Batchmode: Unity -batchmode -quit -projectPath . -executeMethod PortBoss.EditorTools.PortBossBuild.SetupAndBuild -pbOut <dir> [-pbDev]
    public static class PortBossBuild
    {
        const string Root = "Assets/PortBoss";
        const string ScenePath = "Assets/Scenes/Port.unity";

        [MenuItem("Port Boss/Setup scene + prefabs")]
        public static void Setup()
        {
            Dir(Root); Dir(Root + "/Materials"); Dir(Root + "/Prefabs");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var litMat = Mat("Lit", lit, m => { m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.15f); });
            var emis = Mat("Emissive", lit, m => { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; });
            // Soft round dot for every particle (splash, wake, rain, flies); each system tints it with its startColor.
            var dot = new Texture2D(64, 64, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    dot.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(1, 0, Mathf.InverseLerp(0.45f, 1f, d))));
                }
            dot.Apply();
            AssetDatabase.DeleteAsset(Root + "/Materials/Dot.asset");
            AssetDatabase.CreateAsset(dot, Root + "/Materials/Dot.asset");
            var part = Mat("Particle", Shader.Find("Universal Render Pipeline/Particles/Unlit"), m =>
            {
                m.SetColor("_BaseColor", Color.white); m.SetTexture("_BaseMap", dot);
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_ZWrite", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
            });
            var waterShader = Shader.Find("PortBoss/Water");
            if (waterShader == null) Debug.LogError("[PBBuild] PortBoss/Water shader missing, falling back to Lit");
            var water = waterShader != null ? Mat("Water", waterShader, m => { })
                : Mat("Water", lit, m => { m.SetColor("_BaseColor", new Color(0.13f, 0.3f, 0.36f)); m.SetFloat("_Smoothness", 0.88f); });
            var cloud = Mat("Cloud", Shader.Find("PortBoss/ToonCloud") ?? Shader.Find("Universal Render Pipeline/Unlit"), m => m.SetColor("_BaseColor", Color.white));
            var land = Mat("Land", Shader.Find("PortBoss/ToonLand"), m => { });
            var spark = Mat("Spark", Shader.Find("PortBoss/Spark"), m => m.SetTexture("_BaseMap", dot));

            var crate = Prefab("Crate", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Server, true);
                var rb = g.AddComponent<Rigidbody>(); rb.mass = 7f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var bc = g.AddComponent<BoxCollider>(); bc.size = Vector3.one * Crate.Size;
                g.AddComponent<Crate>();
            });
            var boat = Prefab("Boat", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Server, false);
                var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
                g.AddComponent<Boat>();
            });
            var wheelMat = new PhysicsMaterial("wheel") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0 };
            AssetDatabase.CreateAsset(wheelMat, Root + "/Materials/Wheel.physicMaterial");
            var trolley = Prefab("Trolley", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Server, false);
                var rb = g.AddComponent<Rigidbody>(); rb.mass = 30f; rb.linearDamping = 0.35f; rb.angularDamping = 1.5f; rb.centerOfMass = new Vector3(0, 0.25f, 0);
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Box(g, new Vector3(0, 0.37f, 0.02f), new Vector3(1.22f, 0.12f, 1.38f));
                Box(g, new Vector3(0.6f, 0.62f, 0.02f), new Vector3(0.06f, 0.38f, 1.38f));
                Box(g, new Vector3(-0.6f, 0.62f, 0.02f), new Vector3(0.06f, 0.38f, 1.38f));
                Box(g, new Vector3(0, 0.62f, 0.68f), new Vector3(1.22f, 0.38f, 0.06f));
                Box(g, new Vector3(0, 0.85f, -0.76f), new Vector3(1.2f, 0.5f, 0.08f));
                foreach (var x in new[] { -0.5f, 0.5f })
                    foreach (var z in new[] { -0.5f, 0.52f })
                    {
                        var s = g.AddComponent<SphereCollider>(); s.center = new Vector3(x, 0.17f, z); s.radius = 0.17f; s.sharedMaterial = wheelMat;
                    }
                g.AddComponent<Trolley>();
            });
            var game = Prefab("PortGame", g => { g.AddComponent<NetworkObject>(); g.AddComponent<PortGame>(); });
            var slick = new PhysicsMaterial("slick") { dynamicFriction = 0.05f, staticFriction = 0.05f, frictionCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(slick, Root + "/Materials/Slick.physicMaterial");
            var forklift = Prefab("Forklift", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Owner, false);
                var rb = g.AddComponent<Rigidbody>(); rb.mass = 1400f; rb.angularDamping = 1.2f; rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var nrb = g.AddComponent<NetworkRigidbody>(); nrb.UseRigidBodyForMotion = true;
                Box(g, new Vector3(0, 0.62f, -0.1f), new Vector3(1.25f, 0.75f, 1.9f)).sharedMaterial = slick;
                Box(g, new Vector3(0, 0.75f, -1.05f), new Vector3(1.3f, 0.9f, 0.45f));
                Box(g, new Vector3(0, 1.45f, 0.95f), new Vector3(0.9f, 2.6f, 0.12f));
                Box(g, new Vector3(0, 2.2f, -0.15f), new Vector3(1.2f, 0.06f, 1.3f));
                foreach (var x in new[] { -0.6f, 0.6f })
                    foreach (var z in new[] { -0.7f, 0.55f })
                    { var s = g.AddComponent<SphereCollider>(); s.center = new Vector3(x, 0.32f, z); s.radius = 0.32f; s.sharedMaterial = slick; }
                g.AddComponent<Forklift>();
            });
            var pallet = Prefab("Pallet", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Server, true);
                var rb = g.AddComponent<Rigidbody>(); rb.mass = 25f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Box(g, new Vector3(0, Pallet.H / 2, 0), new Vector3(Pallet.W, Pallet.H, Pallet.D));
                g.AddComponent<Pallet>();
            });
            var player = Prefab("Player", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Owner, false);
                var cc = g.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.04f;
                g.AddComponent<Player>();
            });

            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            foreach (var p in new[] { crate, boat, trolley, game, forklift, pallet }) list.Add(new NetworkPrefab { Prefab = p });
            AssetDatabase.CreateAsset(list, Root + "/NetworkPrefabs.asset");

            // Scene: NetworkManager + bootstrap. Everything else is built at runtime.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var nmGo = new GameObject("NetworkManager");
            var nm = nmGo.AddComponent<NetworkManager>();
            var utp = nmGo.AddComponent<UnityTransport>();
            nmGo.AddComponent<FacepunchTransport>();
            nm.NetworkConfig ??= new NetworkConfig();
            nm.NetworkConfig.NetworkTransport = utp;
            nm.NetworkConfig.PlayerPrefab = player;
            nm.NetworkConfig.TickRate = 30;
            nm.NetworkConfig.ConnectionApproval = false;
            nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            // Imported models (Kenney CC0 + own-made): one muted kit material per texture atlas.
            var models = new System.Collections.Generic.List<GameObject>();
            var kits = new System.Collections.Generic.List<int>();
            var kitMats = new System.Collections.Generic.List<Material>();
            foreach (var kitDir in new[] { "Assets/Art/Kenney/watercraft", "Assets/Art/Kenney/pirate", "Assets/Art/Kenney/survival", "Assets/Art/Kenney/city", "Assets/Art/Own" })
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kitDir + "/Textures/colormap.png");
                var kitName = Path.GetFileName(kitDir);
                kitMats.Add(Mat("Kit_" + kitName, lit, m => { if (tex != null) m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", tex != null ? new Color(0.86f, 0.82f, 0.76f) : new Color(0.92f, 0.92f, 0.9f)); m.SetFloat("_Smoothness", 0.1f); }));
                foreach (var f in Directory.GetFiles(kitDir, "*.fbx"))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace(Path.DirectorySeparatorChar, '/'));
                    if (go == null) continue;
                    models.Add(go); kits.Add(kitMats.Count - 1);
                    var b = new Bounds(); bool first = true;
                    foreach (var r in go.GetComponentsInChildren<MeshFilter>()) { var mb = r.sharedMesh.bounds; var wb = new Bounds(r.transform.TransformPoint(mb.center), Vector3.Scale(mb.size, r.transform.lossyScale)); if (first) { b = wb; first = false; } else b.Encapsulate(wb); }
                    Debug.Log($"[PBBuild] model {go.name} size {b.size:F2} center {b.center:F2}");
                }
            }
            var ui = new GameObject("Game").AddComponent<GameUI>();

            ui.TitleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka-Bold.ttf");
            ui.BodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Fredoka-SemiBold.ttf");
            ui.Models = models.ToArray(); ui.ModelKit = kits.ToArray(); ui.KitMats = kitMats.ToArray();
            ui.CratePrefab = crate; ui.BoatPrefab = boat; ui.TrolleyPrefab = trolley; ui.PortGamePrefab = game; ui.ForkliftPrefab = forklift; ui.PalletPrefab = pallet;
            ui.LitMat = litMat; ui.EmissiveMat = emis; ui.ParticleMat = part; ui.WaterMat = water; ui.CloudMat = cloud; ui.LandMat = land; ui.SparkMat = spark;
            ui.Skin = MakeSkin();
            Dir("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            // Long ambience loop streams instead of sitting decompressed in memory.
            if (AssetImporter.GetAtPath("Assets/Resources/Audio/waves.mp3") is AudioImporter ai)
            {
                var ss = ai.defaultSampleSettings; ss.loadType = AudioClipLoadType.Streaming; ai.defaultSampleSettings = ss; ai.SaveAndReimport();
            }

            PlayerSettings.productName = "Port Boss";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.companyName = "Port Boss";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.usePlayerLog = true;
            AssetDatabase.SaveAssets();
            Debug.Log($"[PBBuild] setup done; hashes crate={Hash(crate)} boat={Hash(boat)} trolley={Hash(trolley)} game={Hash(game)} player={Hash(player)}");
        }

        public static void Build()
        {
            var outDir = Arg("-pbOut") ?? "Builds/PortBoss";
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outDir, "PortBoss.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = Array.IndexOf(Environment.GetCommandLineArgs(), "-pbDev") >= 0 ? BuildOptions.Development : BuildOptions.None,
            };
            var r = BuildPipeline.BuildPlayer(opts);
            File.Copy("steam_appid.txt", Path.Combine(outDir, "steam_appid.txt"), true);
            Debug.Log($"[PBBuild] build {r.summary.result} {r.summary.totalSize / 1e6:0}MB errors={r.summary.totalErrors} -> {opts.locationPathName}");
            if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        public static void SetupAndBuild() { Setup(); Build(); }

        // ---- imported art ----
        // ---- helpers ----
        static void Dir(string p) { if (!AssetDatabase.IsValidFolder(p)) { var parent = Path.GetDirectoryName(p).Replace('\\', '/'); AssetDatabase.CreateFolder(parent, Path.GetFileName(p)); } }

        static Material Mat(string name, Shader sh, Action<Material> init)
        {
            var path = $"{Root}/Materials/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(sh);
            init(m);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void Net(GameObject g, NetworkTransform.AuthorityModes mode, bool switchSpace)
        {
            g.AddComponent<NetworkObject>();
            var nt = g.AddComponent<NetworkTransform>();
            nt.AuthorityMode = mode;
            nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
            nt.SwitchTransformSpaceWhenParented = switchSpace;
            nt.Interpolate = true;
        }

        static BoxCollider Box(GameObject g, Vector3 c, Vector3 s) { var b = g.AddComponent<BoxCollider>(); b.center = c; b.size = s; return b; }

        // Clip root settings: rotation and height stay baked into the pose, but forward travel is NOT baked (it would
        // carry the body ahead of the player and snap it back every loop); it becomes root motion, which the game ignores,
        // so every clip plays in place under the player.
        // Mixamo clips take over from UAL1 when they are in the project: Manan downloads them (plan/mixamo-list.md, FBX for
        // Unity, without skin, In Place) into Assets/ThirdParty/Mixamo, gitignored like the pack. Found by Mixamo's file
        // names; each is set up as Humanoid with the root baked into the pose.
        const string Mixamo = "Assets/ThirdParty/Mixamo";
        static void MixamoClips(Skin k)
        {
            if (!AssetDatabase.IsValidFolder(Mixamo)) return;
            var files = System.IO.Directory.GetFiles(Mixamo, "*.fbx");
            AnimationClip M(string name, bool loop)
            {
                // "Standard Walk.fbx", or "Standard Walk (1).fbx" when downloaded twice.
                foreach (var f in files)
                {
                    var n = System.IO.Path.GetFileNameWithoutExtension(f);
                    if (!n.Equals(name, StringComparison.OrdinalIgnoreCase) && !n.StartsWith(name + " (", StringComparison.OrdinalIgnoreCase)) continue;
                    var path = f.Replace('\\', '/');
                    if (AssetImporter.GetAtPath(path) is not ModelImporter mi) continue;
                    mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false;
                    var takes = mi.defaultClipAnimations;
                    foreach (var c in takes) { c.loopTime = loop; c.lockRootRotation = c.lockRootHeightY = true; c.lockRootPositionXZ = false; c.keepOriginalOrientation = c.keepOriginalPositionY = true; }
                    mi.clipAnimations = takes; mi.SaveAndReimport();
                    foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path)) if (a is AnimationClip clip && !clip.name.StartsWith("__")) return clip;
                }
                return null;
            }
            k.Idle = M("Breathing Idle", true) ?? k.Idle;
            k.Walk = M("Standard Walk", true) ?? k.Walk;
            k.Jog = M("Jogging", true) ?? k.Jog;
            k.Run = M("Standard Run", true) ?? k.Run;
            k.Sprint = M("Sprint", true) ?? M("Fast Run", true) ?? k.Sprint;
            k.Jump = M("Falling Idle", true) ?? k.Jump;
            k.Land = M("Falling To Landing", false) ?? k.Land;
            k.SwimIdle = M("Treading Water", true) ?? k.SwimIdle;
            k.Swim = M("Swimming", true) ?? k.Swim;
            k.Push = M("Pushing", true) ?? k.Push;
            k.Drive = M("Driving", true) ?? k.Drive;
            k.Climb = M("Climbing Ladder", true) ?? k.Climb;
            k.Carry = M("Box Idle", true) ?? k.Carry;
        }

        // The Toon Harbor skin: filled only when the (paid, gitignored) pack is in the project; empty slots = our own art.
        const string TH = "Assets/ThirdParty/ToonHarbor";
        static Skin MakeSkin()
        {
            const string path = "Assets/PortBoss/Skin.asset";
            AssetDatabase.DeleteAsset(path);
            var k = ScriptableObject.CreateInstance<Skin>();
            if (AssetDatabase.IsValidFolder(TH))
            {
                T L<T>(string p) where T : UnityEngine.Object
                {
                    var a = AssetDatabase.LoadAssetAtPath<T>(TH + "/" + p);
                    if (a == null) Debug.LogWarning("[PBBuild] skin: missing " + p);
                    return a;
                }
                GameObject P(string n) => L<GameObject>("Prefabs/" + n + ".prefab");
                GameObject[] Ps(params string[] n) => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(System.Linq.Enumerable.Select(n, P), g => g != null));
                k.Toon = L<Material>("Models/Materials/Atlas_1A_D.mat");
                k.ToonWater = L<Material>("Models/Materials/Water_1A_D.mat");
                k.Sky = L<Material>("Skybox/TH_Skybox_01A.mat");
                k.Post = L<UnityEngine.Rendering.VolumeProfile>("Profiles/TH_Sunny_Day_01.asset");
                k.Players = Ps("Characters Variants/TH_Male_Character_01A", "Characters Variants/TH_Female_Character_02C", "Characters Variants/TH_Male_Character_01B", "Characters Variants/TH_Female_Character_03B");
                k.Buyer = P("Characters Variants/TH_Male_Character_02A"); k.Rival = P("Characters Variants/TH_Male_Character_03A");
                k.Crate = P("Cargo/TH_Shipping_Wooden_Crate_02B");
                k.BigCrates = Ps("Cargo/TH_Shipping_Wooden_Crate_02A", "Cargo/TH_Shipping_Wooden_Crate_02C", "Cargo/TH_Shipping_Wooden_Crate_01B");
                k.Barrels = Ps("Props/TH_Barrel_01A", "Props/TH_Barrel_02A", "Props/TH_Barrel_07B", "Props/TH_Barrel_04B");
                k.Pallets = Ps("Cargo/TH_Wooden_Pallet_01A", "Cargo/TH_Wooden_Pallet_02A");
                k.Dinghies = Ps("Ships/Boats/TH_Boat_Small_02B", "Ships/Boats/TH_Boat_Small_01C", "Ships/Boats/TH_Rusty_Boat_01A");
                k.Shed = P("Metal Structures/TH_Metal_Structure_Kit_Building_20A");
                k.Stall = P("Concrete Buildings/TH_Concrete_Building_Kit_10A");
                k.Speedboat = P("Ships/Boats/TH_Boat_Medium_01A");
                k.Containers = Ps("Cargo/TH_Shipping_Container_02A", "Cargo/TH_Shipping_Container_02C", "Cargo/TH_Shipping_Container_02D", "Cargo/TH_Shipping_Container_02E", "Cargo/TH_Shipping_Container_02F", "Cargo/TH_Shipping_Container_02B");
                k.Cranes = Ps("Cranes/TH_Harbor_Crane_01A", "Cranes/TH_Harbor_Crane_01C");
                k.Freighters = Ps("Ships/Ships/TH_Freighter_01A_Loaded", "Ships/Ships/TH_Freighter_03A", "Ships/Ships/TH_Tugboat_01A");
                k.Warehouses = Ps("Metal Structures/TH_Metal_Structure_Kit_Building_12A", "Metal Structures/TH_Metal_Structure_Kit_Building_16A", "Metal Structures/TH_Metal_Structure_Kit_Building_14B");
                // Locomotion: CC0 Quaternius UAL1 clips, retargeted through Unity's Humanoid rig onto the pack's workers.
                const string ual = "Assets/Art/Anim/UAL1_Standard.fbx";
                if (AssetImporter.GetAtPath(ual) is ModelImporter mi)
                {
                    if (mi.animationType != ModelImporterAnimationType.Human || mi.materialImportMode != ModelImporterMaterialImportMode.None)
                    {
                        mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false;
                        mi.SaveAndReimport();
                    }
                    var takes = mi.defaultClipAnimations;
                    foreach (var c in takes) { c.loopTime = c.name.ToLower().Contains("loop") || c.name.ToLower().Contains("idle") || c.name.ToLower().Contains("walk") || c.name.ToLower().Contains("jog") || c.name.ToLower().Contains("sprint") || c.name.ToLower().Contains("swim"); c.lockRootRotation = c.lockRootHeightY = true; c.lockRootPositionXZ = false; c.keepOriginalOrientation = c.keepOriginalPositionY = true; }
                    mi.clipAnimations = takes; mi.SaveAndReimport();
                    var clips = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OfType<AnimationClip>(AssetDatabase.LoadAllAssetsAtPath(ual)));
                    Debug.Log("[PBBuild] UAL1 clips: " + string.Join(", ", System.Linq.Enumerable.Select(clips, c => c.name)));
                    AnimationClip C(params string[] want)
                    {
                        // Takes are named "Armature|Idle_Loop": match the part after the bar exactly.
                        foreach (var w in want) foreach (var c in clips) if (!c.name.StartsWith("__") && c.name.Substring(c.name.LastIndexOf('|') + 1).Equals(w, StringComparison.OrdinalIgnoreCase)) return c;
                        return null;
                    }
                    k.Idle = C("Idle_Loop"); k.Walk = C("Walk_Loop"); k.Run = C("Jog_Fwd_Loop", "Sprint_Loop"); k.Sprint = C("Sprint_Loop", "Jog_Fwd_Loop");
                    k.Jump = C("Jump_Loop"); k.Swim = C("Swim_Fwd_Loop"); k.SwimIdle = C("Swim_Idle_Loop"); k.Drive = C("Driving_Loop"); k.Push = C("Push_Loop");
                    k.Land = C("Jump_Land");
                }
                MixamoClips(k);
                Debug.Log($"[PBBuild] skin clips idle={k.Idle?.name} walk={k.Walk?.name} jog={k.Jog?.name} run={k.Run?.name} sprint={k.Sprint?.name} jump={k.Jump?.name} land={k.Land?.name} swim={k.Swim?.name} tread={k.SwimIdle?.name} push={k.Push?.name} climb={k.Climb?.name} carry={k.Carry?.name}");
                // Harbour map library: short name → pack prefab (HarbourMap.cs asks for these by name).
                var lib = new System.Collections.Generic.List<(string, string)>
                {
                    ("slab", "Concrete Structure Elements/TH_Ground_Concrete_Base_01A_Regular"), ("slabB", "Concrete Structure Elements/TH_Ground_Concrete_Base_01B_Regular"),
                    ("quayEdge", "Concrete Structure Elements/TH_Ground_Concrete_Block_Edge_01A"), ("quayCorner", "Concrete Structure Elements/TH_Ground_Concrete_Block_Corner_01A"),
                    ("pile", "Concrete Structure Elements/TH_Ground_Concrete_Block_Pole_01A"),
                    ("tetraA", "Props/TH_Concrete_Tetrapod_01A"), ("tetraB", "Props/TH_Concrete_Tetrapod_01B"),
                    ("lighthouse", "Props/TH_Lighthouse_01A"), ("streetlight", "Road System/TH_Streetlight_01A"), ("streetlight2", "Props/TH_Streetlight_03A"),
                    ("gantry", "Cranes/TH_Gantry_Crane_01A"), ("gantryB", "Cranes/TH_Gantry_Crane_01C"), ("harbourCrane", "Cranes/TH_Harbor_Crane_01B"), ("smallCrane", "Cranes/TH_Crane_01A"),
                    ("whYellow", "Metal Structures/TH_Metal_Structure_Kit_Building_12A"), ("whWhite", "Metal Structures/TH_Metal_Structure_Kit_Building_12B"), ("whRed", "Metal Structures/TH_Metal_Structure_Kit_Building_12C"),
                    ("whBlue", "Metal Structures/TH_Metal_Structure_Kit_Building_16A"), ("whBig", "Metal Structures/TH_Metal_Structure_Kit_Building_02A"), ("whGreen", "Metal Structures/TH_Metal_Structure_Kit_Building_14C"),
                    ("office", "Concrete Buildings/TH_Concrete_Building_Kit_07A"), ("office2", "Concrete Buildings/TH_Concrete_Building_Kit_03A"), ("cabin", "Concrete Buildings/TH_Concrete_Building_Kit_11A"),
                    ("truck", "Vehicles/TH_Truck_Cabin_01A"), ("truckB", "Vehicles/TH_Truck_Cabin_02B"), ("trailer", "Vehicles/TH_Truck_Trailer_01A"), ("forklift", "Vehicles/TH_Forklift_01B"),
                    ("tug", "Ships/Ships/TH_Tugboat_01A"), ("tugB", "Ships/Ships/TH_Tugboat_04A"), ("freighter", "Ships/Ships/TH_Freighter_01A_Loaded"), ("freighterB", "Ships/Ships/TH_Freighter_03A"),
                    ("freighterL", "Ships/Ships/TH_Freighter_01A_Loaded"), ("freighter01", "Ships/Ships/TH_Freighter_01A"), ("freighter05", "Ships/Ships/TH_Freighter_05A"),
                    ("freighter05L", "Ships/Ships/TH_Freighter_05A_Loaded"), ("freighter09", "Ships/Ships/TH_Freighter_09A"),
                    ("marketHall", "Metal Structures/TH_Metal_Structure_Kit_Building_22A"), ("booth", "Concrete Buildings/TH_Concrete_Building_Kit_10A"),
                    ("shelves", "Interior Elements/TH_Shelves_03A"), ("fridge", "Interior Elements/TH_Kitchen_Fridge_01A"), ("chalkboard", "Interior Elements/TH_Chalkboard_01A"),
                    ("cfenceB", "Props/TH_Concrete_Fence_02A"), ("wallLight", "Props/TH_Wall_Light_01A"), ("trashcan", "Interior Elements/TH_Trashcan_01A"),
                    ("tugBow", "Ships/Ship Elements/TH_Tugboat_Bow_01A"), ("tugStern", "Ships/Ship Elements/TH_Tugboat_Stern_01A"), ("tugBridge", "Ships/Ship Elements/TH_Tugboat_Bridge_01A"),
                    ("woodStack", "Construction Materials/TH_Wood_Stack_02A"), ("cageBarrels", "Cargo/TH_Cage_Barrels_01A"), ("canopy", "Metal Structures/TH_Metal_Structure_Kit_Building_18B"),
                };
                for (int c = 0; c < 6; c++) lib.Add(("cont" + c, "Cargo/TH_Shipping_Container_02" + (char)('A' + c)));
                foreach (var (npc, v) in new[] { ("npc0", "Male_Character_01C"), ("npc1", "Male_Character_02B"), ("npc2", "Female_Character_01A"), ("npc3", "Male_Character_03A"),
                                               ("npc4", "Male_Character_01D"), ("npc5", "Female_Character_02A"), ("npc6", "Male_Character_02C"), ("npc7", "Male_Character_03C") })
                    lib.Add((npc, "Characters Variants/TH_" + v));
                for (int c = 0; c < 6; c++) lib.Add(("city" + c, "Concrete Buildings/TH_Concrete_Building_Kit_" + new[] { "02A", "04A", "05A", "06A", "08A", "09A" }[c]));
                lib.AddRange(new[] {
                    ("road", "Road System/TH_Industrial_Road_Straight_01A"), ("rail", "Road System/TH_Industrial_Road_Tracks_01A"), ("barrier", "Road System/TH_Road_Barrier_01A"),
                    ("tank", "Pipe System/TH_Liquid_Tank_01A"), ("tankB", "Pipe System/TH_Liquid_Tank_01B"), ("tankC", "Pipe System/TH_Liquid_Tank_01C"),
                    ("pipe", "Pipe System/TH_Giant_Pipe_Straight_Long_01A"), ("pipeSup", "Pipe System/TH_Giant_Pipe_Straight_Support_01A"),
                    ("fence", "Props/TH_Metal_Fence_01A_Module"), ("cfence", "Props/TH_Concrete_Fence_01A"), ("advertBroken", "Props/TH_Advertising_Panel_01A_Damaged"),
                    ("cableWaste", "Props/TH_Metal_Cable_Waste_01A"), ("cableWasteB", "Props/TH_Metal_Cable_Waste_01C"), ("planks", "Props/TH_Plank_Pile_01A"),
                    ("bag", "Props/TH_Plastic_Bag_01A"), ("wallY", "Metal Structure Elements 1/TH_Metal_Wall_01E"), ("tile1", "Concrete Structure Elements/TH_Ground_Concrete_Tile_01A"), ("tile3", "Concrete Structure Elements/TH_Ground_Concrete_Tile_03A"), ("tile6", "Concrete Structure Elements/TH_Ground_Concrete_Tile_06A"), ("shed07", "Metal Structures/TH_Metal_Structure_Kit_Building_07A"), ("gasBarrel", "Props/TH_Gas_Barrel_01A"), ("gasBarrelB", "Props/TH_Gas_Barrel_01B"), ("ropeB", "Props/TH_Rope_Pile_2A"), ("plankB", "Props/TH_Plank_Pile_02A"), ("barrelA", "Props/TH_Barrel_03A"), ("barrelB", "Props/TH_Barrel_05B"), ("roofSheet", "Metal Structure Elements 1/TH_Metal_Roof_04A"), ("mpole", "Metal Structure Elements 1/TH_Metal_Pole_01A"), ("wallY2", "Metal Structure Elements 1/TH_Metal_Wall_01F"), ("extinguisher", "Props/TH_Fire_Extinguisher_01A_Fixed"), ("lamp", "Props/TH_Ceiling_Fixture_01A"), ("powerbox2", "Props/TH_Powerbox_02A"), ("box", "Props/TH_Box_02A"), ("boxB", "Props/TH_Box_03A"), ("rope", "Props/TH_Rope_Pile_1A"), ("bollard", "Props/TH_Rope_Pole_01A"), ("powerbox", "Props/TH_Powerbox_01A"),
                    ("sand", "Props/TH_Sand_Patch_01A"), ("epole", "Props/TH_Electric_Pole_01A"), ("wreck", "Ships/Boats/TH_Boat_Small_01B"),
                    ("pallet", "Cargo/TH_Wooden_Pallet_02B"), ("crateBig", "Cargo/TH_Shipping_Wooden_Crate_03A"), ("metalCrate", "Cargo/TH_Shipping_Metal_Crate_01A") });
                var names = new System.Collections.Generic.List<string>(); var prefabs = new System.Collections.Generic.List<GameObject>();
                foreach (var (n, pth) in lib) { var g = P(pth); if (g != null) { names.Add(n); prefabs.Add(g); } }
                k.LibNames = names.ToArray(); k.Lib = prefabs.ToArray();
                Debug.Log($"[PBBuild] skin: Toon Harbor ON, map library {names.Count}/{lib.Count}");
            }
            else Debug.Log("[PBBuild] skin: Toon Harbor not in the project, using own art");
            AssetDatabase.CreateAsset(k, path);
            return k;
        }

        static GameObject Prefab(string name, Action<GameObject> build)
        {
            var path = $"{Root}/Prefabs/{name}.prefab";
            AssetDatabase.DeleteAsset(path);
            var g = new GameObject(name);
            build(g);
            var asset = PrefabUtility.SaveAsPrefabAsset(g, path);
            UnityEngine.Object.DestroyImmediate(g);
            // NetworkObject hashes come from OnValidate on the asset; make sure it ran.
            var no = asset.GetComponent<NetworkObject>();
            typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(no, null);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static uint Hash(GameObject g) => (uint)typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(g.GetComponent<NetworkObject>());

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
