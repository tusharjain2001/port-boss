using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PortBoss.EditorTools
{
    // Toon Harbor Pack (paid, gitignored under Assets/ThirdParty/): move it out of its import folder and survey it.
    // Batchmode: -executeMethod PortBoss.EditorTools.ToonHarborTools.MoveToThirdParty | .Survey -thOut <dir>
    public static class ToonHarborTools
    {
        public const string Root = "Assets/ThirdParty/ToonHarbor";

        public static void MoveToThirdParty()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty")) AssetDatabase.CreateFolder("Assets", "ThirdParty");
            var err = AssetDatabase.MoveAsset("Assets/Toon Harbor Pack", Root);
            Debug.Log("[TH] move: " + (string.IsNullOrEmpty(err) ? "ok" : err));
        }

        static string Arg(string name)
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }

        static void Render(Camera cam, string path, int w = 1600, int h = 900)
        {
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); rt.Release();
        }

        public static void Survey()
        {
            var outDir = Arg("-thOut") ?? "th_survey";
            Directory.CreateDirectory(outDir);
            var log = new StringBuilder();

            // Demo scenes: what the pack's own look is made of.
            foreach (var scenePath in AssetDatabase.FindAssets("t:Scene", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                log.AppendLine($"== {scenePath}");
                log.AppendLine($"skybox={(RenderSettings.skybox ? RenderSettings.skybox.name + " / " + RenderSettings.skybox.shader.name : "-")} ambientMode={RenderSettings.ambientMode} ambSky={RenderSettings.ambientSkyColor} ambEq={RenderSettings.ambientEquatorColor} ambGround={RenderSettings.ambientGroundColor} ambLight={RenderSettings.ambientLight} ambIntensity={RenderSettings.ambientIntensity}");
                log.AppendLine($"fog={RenderSettings.fog} mode={RenderSettings.fogMode} color={RenderSettings.fogColor} density={RenderSettings.fogDensity} start={RenderSettings.fogStartDistance} end={RenderSettings.fogEndDistance}");
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    log.AppendLine($"light {l.name} type={l.type} color={l.color} intensity={l.intensity} rot={l.transform.eulerAngles} shadows={l.shadows} strength={l.shadowStrength}");
                foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                {
                    log.AppendLine($"volume {v.name} global={v.isGlobal} profile={(v.sharedProfile ? AssetDatabase.GetAssetPath(v.sharedProfile) : "-")}");
                    if (v.sharedProfile) foreach (var c in v.sharedProfile.components)
                        {
                            log.Append($"  {c.GetType().Name} active={c.active}:");
                            foreach (var p in c.parameters) if (p.overrideState) log.Append($" {p}");
                            log.AppendLine();
                        }
                }
                // Water and the shaders in use.
                var shaders = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        shaders[m.shader.name] = shaders.TryGetValue(m.shader.name, out var n) ? n + 1 : 1;
                        if (m.shader.name.Contains("Water") && !log.ToString().Contains("water material " + m.name))
                        {
                            log.Append($"water material {m.name} on {r.name} pos={r.transform.position} scale={r.transform.lossyScale}:");
                            for (int i = 0; i < m.shader.GetPropertyCount(); i++)
                            {
                                var pn = m.shader.GetPropertyName(i);
                                switch (m.shader.GetPropertyType(i))
                                {
                                    case ShaderPropertyType.Color: log.Append($" {pn}={m.GetColor(pn)}"); break;
                                    case ShaderPropertyType.Float: case ShaderPropertyType.Range: log.Append($" {pn}={m.GetFloat(pn)}"); break;
                                    case ShaderPropertyType.Vector: log.Append($" {pn}={m.GetVector(pn)}"); break;
                                    case ShaderPropertyType.Texture: log.Append($" {pn}={(m.GetTexture(pn) ? m.GetTexture(pn).name : "-")}"); break;
                                }
                            }
                            log.AppendLine();
                        }
                    }
                log.AppendLine("shaders: " + string.Join(", ", shaders.Select(kv => $"{kv.Key}x{kv.Value}")));
                var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                int k = 0;
                foreach (var cam in cams)
                {
                    log.AppendLine($"camera {cam.name} pos={cam.transform.position} rot={cam.transform.eulerAngles} fov={cam.fieldOfView}");
                    Render(cam, Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(scenePath)}_cam{k++}.png"));
                }
            }

            // Characters: rig, animator, avatar, and a line-up shot of every variant.
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var chars = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs/Characters Variants", Root + "/Prefabs/Characters Source" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            float x = 0;
            foreach (var p in chars)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                var anim = prefab.GetComponentInChildren<Animator>();
                var smr = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                log.AppendLine($"char {Path.GetFileNameWithoutExtension(p)} animator={(anim ? (anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "no-ctrl") : "none")} avatar={(anim && anim.avatar ? anim.avatar.name + (anim.avatar.isHuman ? " HUMAN" : " generic") : "-")} skinned={smr.Length} bones={(smr.Length > 0 ? smr[0].bones.Length : 0)} mats={string.Join("/", smr.SelectMany(s => s.sharedMaterials).Where(m => m).Select(m => m.name + ":" + m.shader.name).Distinct())}");
                if (!p.Contains("Variants")) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = new Vector3(x, 0, 0); go.transform.rotation = Quaternion.Euler(0, 180, 0);
                x += 1.1f;
            }
            foreach (var fbx in AssetDatabase.FindAssets("Armature t:Model", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
                log.AppendLine($"rig {fbx} animationType={imp.animationType} clips={string.Join(",", AnimationUtility.GetAnimationClips(AssetDatabase.LoadAssetAtPath<GameObject>(fbx)).Select(c => c.name))}");
            }
            var camGo = Camera.main;
            camGo.transform.position = new Vector3((x - 1.1f) / 2, 1.3f, -9.5f);
            camGo.transform.LookAt(new Vector3((x - 1.1f) / 2, 0.9f, 0));
            camGo.fieldOfView = 55; camGo.backgroundColor = new Color(0.55f, 0.7f, 0.85f); camGo.clearFlags = CameraClearFlags.SolidColor;
            Render(camGo, Path.Combine(outDir, "characters_lineup.png"), 2400, 900);
            camGo.transform.position = new Vector3(1.6f, 1.2f, -3.2f); camGo.transform.LookAt(new Vector3(1.6f, 0.95f, 0));
            Render(camGo, Path.Combine(outDir, "characters_closeup.png"));

            File.WriteAllText(Path.Combine(outDir, "survey.txt"), log.ToString());
            Debug.Log("[TH] survey written to " + outDir);
        }
    }
}
