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
            var l = g.AddComponent<Landing>(); I = l;
            l.Moor = Util.Find(g.transform, "socket_moor"); l.Spawn = Util.Find(g.transform, "socket_spawn");
            l.LadderTop = Util.Find(g.transform, "socket_pier_ladder_top"); l.LadderBottom = Util.Find(g.transform, "socket_pier_ladder_bottom");
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return l;
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
                var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Util.ParticleMat; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 1.2f; r.maxParticleSize = 0.02f;
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

        // An expanding ring on the water that fades out (ripples, bobber land, nibbles).
        public static void Ring(Vector3 at, float diameter, float life, float alpha = 0.8f)
        {
            if (ringMat == null) { ringMat = new Material(Util.ParticleMat); ringMat.SetTexture("_BaseMap", Util.Ring); }
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
                var r = t.GetComponent<MeshRenderer>(); r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(1, 1, 1, (1 - u) * 0.75f)); r.SetPropertyBlock(mpb);
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
            if (age > 120f) { Alerts.Push("Deck", F.Species.Name + " slipped back into the sea.", Alerts.Kind.Info); Destroy(gameObject); }
        }
        void OnDestroy() => All.Remove(this);

        public void Take(Angler a)
        {
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
