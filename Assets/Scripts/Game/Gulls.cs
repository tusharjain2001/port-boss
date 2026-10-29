using UnityEngine;

namespace PortBoss
{
    // Ambient gulls (local to each peer, not networked): circle over the pier, glide, land on posts and lamps,
    // squawk now and then. Own-made gull model with Wing_L/Wing_R children for flapping.
    public class Gull : MonoBehaviour
    {
        // Perches: lamp heads, pier posts, the buyer's awning pole, bollards.
        static readonly Vector3[] Perches =
        {
            new(3.5f, 4.65f, -1.5f), new(-15.5f, 4.65f, -1.5f), new(-2.6f, 4.85f, 29.5f),
            new(-2.9f, 0.55f, 13.5f), new(2.9f, 0.55f, 19.5f), new(-2.9f, 0.55f, 25.5f), new(2.9f, 0.55f, 7.5f),
            new(2.75f, 0.62f, 22f), new(-5.4f, 2.62f, -5.4f), new(9f, 3.85f, -9f),
        };

        enum St { Circle, Glide, Land, Perched, TakeOff }
        St st;
        Transform wingL, wingR;
        bool toon;
        Quaternion baseL, baseR;
        Vector3 axisL, axisR;
        Vector3 center, perch, vel;
        Transform thiefTarget; // a crate this gull is raiding
        static readonly System.Collections.Generic.List<Gull> all = new();

        public static Transform AnyPerched() => all.Find(g => g.st == St.Perched)?.transform; // test shots

        // Host told everyone: send the nearest free gull to raid this crate.
        public static void Raid(Transform crate)
        {
            Gull best = null; float bd = 1e9f;
            foreach (var g in all)
                if (g.thiefTarget == null && g.st != St.Perched) { float d = (g.transform.position - crate.position).sqrMagnitude; if (d < bd) { bd = d; best = g; } }
            if (best == null) return;
            best.thiefTarget = crate;
            best.st = St.Land; best.timer = 12f;
            Sfx.Play("gull", best.transform.position, 0.7f, 1.2f, true);
        }
        float radius, height, angle, speed, timer, flap;

        public static void Spawn(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var g = new GameObject("Gull").AddComponent<Gull>();
                var m = Skin.On ? ToonGull(g.transform) : Art.Model("gull", g.transform, Vector3.zero, 0, 0.95f);
                if (m == null) { Destroy(g.gameObject); return; }
                g.toon = Skin.On;
                g.wingL = FindDeep(m.transform, "Wing_L");
                g.wingR = FindDeep(m.transform, "Wing_R");
                g.Init(i);
                if (g.wingL != null) { g.baseL = g.wingL.localRotation; g.axisL = g.wingL.parent.InverseTransformDirection(g.transform.forward); }
                if (g.wingR != null) { g.baseR = g.wingR.localRotation; g.axisR = g.wingR.parent.InverseTransformDirection(g.transform.forward); }
            }
        }

        // A rounder gull in the pack's toon style: egg body, round head, hooked yellow beak, grey wings with black tips
        // on Wing_L / Wing_R pivots (the flap code finds them by name).
        static GameObject ToonGull(Transform parent)
        {
            var m = new GameObject("gullModel").transform;
            m.SetParent(parent, false);
            Color white = Art.Hex("#f4f4f0"), grey = Art.Hex("#9aa3ab"), black = Art.Hex("#262626"), yellow = Art.Hex("#f2c230");
            Art.Prim(PrimitiveType.Sphere, m, Vector3.zero, Quaternion.identity, new Vector3(0.19f, 0.17f, 0.44f), white, false, "body");
            Art.Prim(PrimitiveType.Sphere, m, new Vector3(0, 0.08f, 0.2f), Quaternion.identity, new Vector3(0.14f, 0.13f, 0.15f), white, false, "head");
            Art.Prim(PrimitiveType.Capsule, m, new Vector3(0, 0.065f, 0.3f), Quaternion.Euler(100, 0, 0), new Vector3(0.035f, 0.05f, 0.03f), yellow, false, "beak");
            foreach (var x in new[] { -0.05f, 0.05f })
                Art.Prim(PrimitiveType.Sphere, m, new Vector3(x, 0.11f, 0.255f), Quaternion.identity, Vector3.one * 0.024f, black, false, "eye");
            Art.Prim(PrimitiveType.Cube, m, new Vector3(0, 0.03f, -0.24f), Quaternion.Euler(-8, 0, 0), new Vector3(0.11f, 0.025f, 0.12f), grey, false, "tail");
            foreach (var side in new[] { -1f, 1f })
            {
                var w = new GameObject(side < 0 ? "Wing_L" : "Wing_R").transform;
                w.SetParent(m, false); w.localPosition = new Vector3(side * 0.07f, 0.05f, 0.02f);
                Art.Prim(PrimitiveType.Cube, w, new Vector3(side * 0.22f, 0, 0), Quaternion.Euler(0, side * -8f, 0), new Vector3(0.42f, 0.022f, 0.16f), grey, false, "wing");
                Art.Prim(PrimitiveType.Cube, w, new Vector3(side * 0.47f, -0.005f, -0.035f), Quaternion.Euler(0, side * -18f, 0), new Vector3(0.15f, 0.02f, 0.1f), black, false, "wingtip");
            }
            return m.gameObject;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
            return null;
        }

        void OnDestroy() => all.Remove(this);

        void Init(int i)
        {
            all.Add(this);
            center = new Vector3(Random.Range(-10f, 15f), 0, Random.Range(0f, 30f));
            radius = Random.Range(8f, 22f);
            height = Random.Range(7f, 16f);
            angle = Random.Range(0, 360f);
            speed = Random.Range(18f, 30f) * (i % 2 == 0 ? 1 : -1); // degrees per second
            timer = Random.Range(8f, 30f);
            transform.position = Orbit();
        }

        // In rough weather the gulls clear off: up and away, gone from the harbour.
        Vector3 Orbit() => center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, height + Mathf.Sin(Time.time * 0.4f + radius) * 1.5f + Hide * 140f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius + Hide * 260f);
        static float Hide => Mathf.Clamp01((Storm.Level - 0.5f) / 0.25f);

        void Update()
        {
            float dt = Time.deltaTime;
            timer -= dt;
            Vector3 target = transform.position;
            bool flapping = true;
            switch (st)
            {
                case St.Circle:
                case St.Glide:
                    angle += speed * dt;
                    target = Orbit();
                    flapping = st == St.Circle;
                    if (timer < 0)
                    {
                        // Mostly keep circling; sometimes glide; sometimes go and sit on something.
                        float r = Random.value;
                        if (r < 0.35f && Hide <= 0) { st = St.Land; perch = Perches[Random.Range(0, Perches.Length)]; timer = 12f; }
                        else { st = r < 0.7f ? St.Glide : St.Circle; timer = Random.Range(5f, 12f); }
                    }
                    break;
                case St.Land:
                    if (thiefTarget != null) perch = thiefTarget.position + Vector3.up * (Crate.Size * 0.5f + 0.05f);
                    target = perch + Vector3.up * 0.05f;
                    flapping = (transform.position - target).sqrMagnitude < 9f;
                    if ((transform.position - target).sqrMagnitude < 0.02f || timer < 0) { st = St.Perched; timer = thiefTarget != null ? 16f : Random.Range(6f, 16f); }
                    break;
                case St.Perched:
                    if (thiefTarget != null) perch = thiefTarget.position + Vector3.up * (Crate.Size * 0.5f + 0.05f);
                    target = perch + Vector3.up * 0.05f;
                    flapping = false;
                    bool raidDone = thiefTarget != null && timer < 16f - 6f;
                    if (timer < 0 || PlayerNear() || raidDone)
                    {
                        // A thief that wasn't shooed off flies away with a fish in its beak.
                        if (raidDone && !PlayerNear()) Destroy(Art.Prim(PrimitiveType.Capsule, transform, new Vector3(0, -0.12f, 0.25f), Quaternion.Euler(90, 0, 0), new Vector3(0.08f, 0.14f, 0.06f), Art.Fish, false, "stolen_fish"), 8f);
                        thiefTarget = null;
                        st = St.TakeOff; timer = 2.5f; Sfx.Play("gull", transform.position, 0.6f, 1f, true);
                    }
                    break;
                case St.TakeOff:
                    target = transform.position + Vector3.up * 4f + transform.forward * 3f;
                    if (timer < 0) { st = St.Circle; timer = Random.Range(8f, 20f); angle = Mathf.Atan2(transform.position.z - center.z, transform.position.x - center.x) * Mathf.Rad2Deg; }
                    break;
            }

            if (st == St.Perched) { transform.position = target; vel = Vector3.zero; }
            else
            {
                var want = (target - transform.position) / Mathf.Max(dt, 1e-3f);
                vel = Vector3.Lerp(vel, Vector3.ClampMagnitude(want, st == St.Land ? 7f : 12f), 1 - Mathf.Exp(-3f * dt));
                transform.position += vel * dt;
                var flat = new Vector3(vel.x, vel.y * 0.3f, vel.z);
                if (flat.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat) * Quaternion.Euler(0, 0, -Mathf.Clamp(speed, -30, 30) * 0.8f), 1 - Mathf.Exp(-4f * dt));
            }

            // Wings: flap when climbing/landing, hold out when gliding, fold when perched.
            flap += dt * (flapping ? 11f : 0f);
            float a = st == St.Perched ? 70f : flapping ? Mathf.Sin(flap) * 38f : 6f;
            if (toon && st == St.Perched)
            {
                // Folded: swept back along the body.
                if (wingL != null) { wingL.localRotation = Quaternion.AngleAxis(-84f, Vector3.up) * baseL; wingL.localScale = new Vector3(0.62f, 1, 1); }
                if (wingR != null) { wingR.localRotation = Quaternion.AngleAxis(84f, Vector3.up) * baseR; wingR.localScale = new Vector3(0.62f, 1, 1); }
            }
            else
            {
                if (wingL != null) { wingL.localRotation = Quaternion.AngleAxis(-a, axisL) * baseL; wingL.localScale = Vector3.one; }
                if (wingR != null) { wingR.localRotation = Quaternion.AngleAxis(a, axisR) * baseR; wingR.localScale = Vector3.one; }
            }

            if (st != St.Perched && Random.value < dt / 25f) Sfx.Play("gull", transform.position, 0.45f, Random.Range(0.9f, 1.15f), true);
        }

        bool PlayerNear()
        {
            foreach (var p in Player.All) if ((p.transform.position - transform.position).sqrMagnitude < 9f) return true;
            return false;
        }
    }

    // Distant ship crawling across the horizon; wraps around.
    public class Drifter : MonoBehaviour
    {
        public float Speed = 3f, MinX = -500f, MaxX = 700f;
        void Update()
        {
            transform.position += Vector3.right * (Speed * Time.deltaTime);
            if (transform.position.x > MaxX) transform.position = new Vector3(MinX, transform.position.y, transform.position.z);
        }
    }
}
