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
            var part = Mat("Particle", Shader.Find("Universal Render Pipeline/Particles/Unlit"), m => m.SetColor("_BaseColor", Color.black));
            var waterShader = Shader.Find("PortBoss/Water");
            if (waterShader == null) Debug.LogError("[PBBuild] PortBoss/Water shader missing, falling back to Lit");
            var water = waterShader != null ? Mat("Water", waterShader, m => { })
                : Mat("Water", lit, m => { m.SetColor("_BaseColor", new Color(0.13f, 0.3f, 0.36f)); m.SetFloat("_Smoothness", 0.88f); });
            var cloud = Mat("Cloud", Shader.Find("Universal Render Pipeline/Unlit"), m => m.SetColor("_BaseColor", Color.white));

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
                Box(g, new Vector3(0, 0.36f, 0.05f), new Vector3(1.2f, 0.1f, 1.3f));
                Box(g, new Vector3(0.62f, 0.5f, 0.05f), new Vector3(0.05f, 0.25f, 1.3f));
                Box(g, new Vector3(-0.62f, 0.5f, 0.05f), new Vector3(0.05f, 0.25f, 1.3f));
                Box(g, new Vector3(0, 0.5f, 0.72f), new Vector3(1.25f, 0.25f, 0.05f));
                Box(g, new Vector3(0, 0.9f, -0.7f), new Vector3(1.2f, 0.5f, 0.08f));
                foreach (var x in new[] { -0.55f, 0.55f })
                    foreach (var z in new[] { -0.5f, 0.55f })
                    {
                        var s = g.AddComponent<SphereCollider>(); s.center = new Vector3(x, 0.18f, z); s.radius = 0.18f; s.sharedMaterial = wheelMat;
                    }
                g.AddComponent<Trolley>();
            });
            var game = Prefab("PortGame", g => { g.AddComponent<NetworkObject>(); g.AddComponent<PortGame>(); });
            var player = Prefab("Player", g =>
            {
                Net(g, NetworkTransform.AuthorityModes.Owner, false);
                var cc = g.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.35f; cc.slopeLimit = 50f; cc.skinWidth = 0.04f;
                g.AddComponent<Player>();
            });

            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            foreach (var p in new[] { crate, boat, trolley, game }) list.Add(new NetworkPrefab { Prefab = p });
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

            ui.TitleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/AlfaSlabOne-Regular.ttf");
            ui.Models = models.ToArray(); ui.ModelKit = kits.ToArray(); ui.KitMats = kitMats.ToArray();
            ui.CratePrefab = crate; ui.BoatPrefab = boat; ui.TrolleyPrefab = trolley; ui.PortGamePrefab = game;
            ui.LitMat = litMat; ui.EmissiveMat = emis; ui.ParticleMat = part; ui.WaterMat = water; ui.CloudMat = cloud;
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

        static void Box(GameObject g, Vector3 c, Vector3 s) { var b = g.AddComponent<BoxCollider>(); b.center = c; b.size = s; }

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
