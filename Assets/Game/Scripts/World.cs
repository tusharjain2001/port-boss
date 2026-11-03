using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Walt's Landing: grandpa's pier, his shack and the bit of shore (Blender: art-src/m1/build_boat_landing.py).
    public class Landing : MonoBehaviour
    {
        public static Landing I;
        public Transform Moor, Spawn, LadderTop, LadderBottom;

        public static Landing Create(GameObject prefab, Material toon)
        {
            if (prefab == null) return Placeholder(toon);
            var g = Instantiate(prefab); g.name = "WaltsLanding";
            Util.Colliders(g); Util.Toon(g, toon);
            // A sea floor under the landing that slopes away into the deep (look audit 3 Nov: where the modelled seabed ends,
            // ~4 m down, the sea turned bottomless at once, and the water drew that step as a hard dark oval by the pier end).
            // Flat just under the land's lowest point (-4 m) beneath the model, then down to -30 m over 80 m all round.
            var lb = new Bounds(g.transform.position, Vector3.zero); foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) lb.Encapsulate(r.bounds);
            var bed = new GameObject("SeabedFill"); bed.AddComponent<MeshFilter>().sharedMesh = Seabed(lb, -4.3f, -30f, 80f);
            var br = bed.AddComponent<MeshRenderer>(); var bm = new Material(toon); bm.SetColor("_BaseColor", Ponyo.Hex("7C7690")); br.sharedMaterial = bm; br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bed.transform.SetParent(g.transform, true);
            var l = g.AddComponent<Landing>(); I = l;
            l.Moor = Util.Find(g.transform, "socket_moor"); l.Spawn = Util.Find(g.transform, "socket_spawn");
            l.LadderTop = Util.Find(g.transform, "socket_pier_ladder_top"); l.LadderBottom = Util.Find(g.transform, "socket_pier_ladder_bottom");
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return l;
        }

        // world-space: the land's footprint flat at `top`, a skirt out `wide` m on every side sloping down to `deep`
        static Mesh Seabed(Bounds b, float top, float deep, float wide)
        {
            Vector3 C(float x, float z, float y) => new(x, y, z);
            float x0 = b.min.x, x1 = b.max.x, z0 = b.min.z, z1 = b.max.z;
            var v = new[] { C(x0, z0, top), C(x0, z1, top), C(x1, z1, top), C(x1, z0, top),
                            C(x0 - wide, z0 - wide, deep), C(x0 - wide, z1 + wide, deep), C(x1 + wide, z1 + wide, deep), C(x1 + wide, z0 - wide, deep) };
            var t = new List<int> { 0, 1, 2, 0, 2, 3 };
            for (int k = 0; k < 4; k++) { int a = k, b2 = (k + 1) % 4, c = 4 + (k + 1) % 4, d = 4 + k; t.AddRange(new[] { a, d, c, a, c, b2 }); }
            for (int i = 0; i < t.Count; i += 3) // every face up (seen from above)
                if (Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y < 0) (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]);
            var m = new Mesh { name = "seabed", vertices = v, triangles = t.ToArray() }; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // A plain plank pier when the Blender landing isn't in the build (so the boat and fishing can still be tested).
        static Landing Placeholder(Material toon)
        {
            var g = new GameObject("WaltsLanding (placeholder)"); var l = g.AddComponent<Landing>(); I = l;
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube); deck.transform.SetParent(g.transform); deck.transform.localPosition = new Vector3(0, 1.15f, -6); deck.transform.localScale = new Vector3(2.6f, 0.3f, 32);
            deck.GetComponent<MeshRenderer>().sharedMaterial = new Material(toon) { color = Color.white }; deck.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", Ponyo.Hex("B9825A"));
            Transform T(string n, Vector3 p, float yaw = 180) { var t = new GameObject(n).transform; t.SetParent(g.transform); t.localPosition = p; t.localRotation = Quaternion.Euler(0, yaw, 0); return t; }
            l.Moor = T("socket_moor", new Vector3(2.9f, 0, -17)); l.Spawn = T("socket_spawn", new Vector3(0, 1.32f, -15));
            l.LadderTop = T("socket_pier_ladder_top", new Vector3(1.3f, 1.3f, -21)); l.LadderBottom = T("socket_pier_ladder_bottom", new Vector3(1.5f, -0.6f, -21));
            return l;
        }
    }

    // Splashes, ripple rings, droplets: small pooled effects.
    public static class Fx
    {
        static ParticleSystem drops;
        static readonly List<(Transform t, float born, float life, float size)> rings = new();
        static Material ringMat;

        static ParticleSystem Drops
        {
            get
            {
                if (drops != null) return drops;
                var g = new GameObject("fx.drops");
                drops = g.AddComponent<ParticleSystem>();
                var m = drops.main; m.loop = false; m.playOnAwake = false; m.simulationSpace = ParticleSystemSimulationSpace.World;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f); m.gravityModifier = 1.2f; m.maxParticles = 600; m.startColor = new Color(1, 1, 1, 0.9f);
                var e = drops.emission; e.rateOverTime = 0;
                var sh = drops.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 28; sh.radius = 0.08f; sh.rotation = new Vector3(-90, 0, 0);
                var sz = drops.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.3f));
                var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Util.Soft(Color.white); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 1.2f; r.maxParticleSize = 0.02f;
                return drops;
            }
        }

        // A splash crown (fishing §3.4: 12 droplets) plus a ring on the water.
        public static void Splash(Vector3 at, float size, int count = 12)
        {
            var d = Drops;
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            d.transform.position = at;
            var m = d.main; m.startSpeedMultiplier = 1f + size * 0.6f;
            d.Emit(ep, Mathf.RoundToInt(count * Mathf.Max(0.5f, size)));
            Ring(at, 0.6f * size + 0.4f, 0.8f);
        }

        // Dust off the feet (sprint steps, a skid, landings): a few pale soft puffs that spread low, swell and fade.
        static ParticleSystem dust;
        public static void Dust(Vector3 at, int count, float size = 1)
        {
            if (dust == null)
            {
                var g = new GameObject("fx.dust");
                dust = g.AddComponent<ParticleSystem>();
                var m = dust.main; m.loop = false; m.playOnAwake = false; m.simulationSpace = ParticleSystemSimulationSpace.World;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
                m.gravityModifier = -0.04f; m.maxParticles = 300; m.startColor = new Color(0.95f, 0.9f, 0.8f, 0.5f);
                var e = dust.emission; e.rateOverTime = 0;
                var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.12f; sh.rotation = new Vector3(-90, 0, 0);
                var sz = dust.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 0.6f, 1, 1.6f));
                var col = dust.colorOverLifetime; col.enabled = true; var gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); col.color = gr;
                var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Util.Soft(Color.white); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var mm = dust.main; mm.startSize = new ParticleSystem.MinMaxCurve(0.14f * size, 0.26f * size);
            dust.transform.position = at;
            dust.Emit(new ParticleSystem.EmitParams { applyShapeToPosition = true }, count);
        }

        // An expanding ring on the water that fades out (ripples, bobber land, nibbles).
        public static void Ring(Vector3 at, float diameter, float life, float alpha = 0.8f)
        {
            if (ringMat == null) ringMat = Util.Soft(Color.white, Util.Ring);
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(g.GetComponent<Collider>());
            g.name = "ring"; g.transform.position = at + Vector3.up * 0.03f; g.transform.rotation = Quaternion.Euler(90, 0, 0);
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = ringMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", new Color(1, 1, 1, alpha)); r.SetPropertyBlock(mpb);
            rings.Add((g.transform, Time.time, life, diameter));
        }

        public static void Tick()
        {
            var mpb = new MaterialPropertyBlock();
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var (t, born, life, size) = rings[i];
                float u = (Time.time - born) / life;
                if (t == null || u >= 1) { if (t != null) Object.Destroy(t.gameObject); rings.RemoveAt(i); continue; }
                float s = Mathf.Lerp(0.35f, 1f, 1 - (1 - u) * (1 - u)) * size;
                t.localScale = new Vector3(s, s, 1);
                var p = t.position; p.y = Sea.Height(p) + 0.03f; t.position = p;
                var r = t.GetComponent<MeshRenderer>(); r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(1, 1, 1, (1 - u) * 0.5f)); r.SetPropertyBlock(mpb);
            }
        }
    }

    // A fish lying on the deck or the pier (dropped from a full hotbar): it flops, anyone can pick it up for 120 s.
    public class FishPickup : MonoBehaviour
    {
        public static readonly List<FishPickup> All = new();
        public Fish F; float born; Renderer r; MaterialPropertyBlock mpb;

        public static FishPickup Drop(Fish f, Vector3 at, Quaternion rot)
        {
            var g = FishView.Make(f, null); g.name = "pickup " + f.Species.Id;
            g.transform.SetPositionAndRotation(at, rot * Quaternion.Euler(0, 0, 90));
            var p = g.AddComponent<FishPickup>(); p.F = f; p.born = Time.time; p.r = g.GetComponentInChildren<Renderer>(); p.mpb = new MaterialPropertyBlock();
            var s = Skiff.I; if (s != null && s.Inside(at, 0.2f)) g.transform.SetParent(s.transform, true);
            All.Add(p);
            return p;
        }

        void Update()
        {
            float age = Time.time - born;
            r.GetPropertyBlock(mpb); mpb.SetFloat("_Swim", Mathf.Lerp(0.12f, 0.02f, Mathf.Clamp01(age / 20f))); mpb.SetFloat("_SwimFreq", 1.6f); r.SetPropertyBlock(mpb);
            if (age > 120f)
            {
                // co-op: the host's book decides (and tells everyone); solo, here
                if (Coop.On) { if (Coop.IsHost) { born = Time.time; Coop.Expire(this); } return; }
                Alerts.Push("Deck", F.Species.Name + " slipped back into the sea.", Alerts.Kind.Info); Destroy(gameObject);
            }
        }
        void OnDestroy() => All.Remove(this);

        public void Take(Angler a)
        {
            if (Coop.On && F.Id != 0) { Coop.Pickup(F.Id); return; } // the host's book: first come (it goes on every machine)
            int slot = a.Bar.Add(F);
            if (slot < 0) return;
            Hud.I?.FlyIn(transform.position, slot);
            Snd.Play2D(Synth.Thup, 0.6f);
            Destroy(gameObject);
        }

        public static FishPickup Nearest(Vector3 p, float r)
        {
            FishPickup best = null; float bd = r * r;
            foreach (var f in All) { float d = (f.transform.position - p).sqrMagnitude; if (d < bd) { bd = d; best = f; } }
            return best;
        }
    }
}
