using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M5 paint and dye channels (systems §4.8.2): the PB_Toon shader tints faces by the channel in their vertex alpha
    // (255 - 16 * ch) from per-renderer properties, so a boat's 4 paint channels and a body's 6 clothes channels never need
    // another material. Here: what each channel looks like now (its own luminance, so painted faces keep their shading),
    // and setting a renderer's channels from a boat's paint (with Doyle's crew's sweep) or a player's look.
    public static class Channels
    {
        static readonly int[] chId = new int[9], ptId = new int[9];
        static readonly int sweepId = Shader.PropertyToID("_Sweep"), axisId = Shader.PropertyToID("_SweepAxis"), hideId = Shader.PropertyToID("_ChHide"),
            patUvId = Shader.PropertyToID("_PatUV"), patKId = Shader.PropertyToID("_PatK");
        static Channels() { for (int c = 1; c <= 8; c++) { chId[c] = Shader.PropertyToID("_Ch" + c); ptId[c] = Shader.PropertyToID("_Pt" + c); } }

        public static int Of(Color32 c) { int ch = (int)System.Math.Round((255 - c.a) / 16.0); return ch >= 1 && ch <= 8 ? ch : 0; }
        public static byte Alpha(int ch) => (byte)(255 - 16 * ch);

        // per mesh: each channel's mean linear colour + luminance, and the mesh's extent along +Z (the sweep)
        public class Info { public Color[] Mean = new Color[9]; public float[] Lum = new float[9]; public int[] Count = new int[9]; public float Z0, Z1; }
        static readonly Dictionary<Mesh, Info> infos = new();
        public static Info Read(Mesh m)
        {
            if (m == null) return null;
            if (infos.TryGetValue(m, out var inf)) return inf;
            inf = new Info(); infos[m] = inf;
            if (!m.isReadable) return inf;
            var cols = m.colors32; var v = m.vertices; var sum = new Vector3[9];
            inf.Z0 = float.MaxValue; inf.Z1 = float.MinValue;
            for (int i = 0; i < cols.Length && i < v.Length; i++)
            {
                int c = Of(cols[i]); var lin = ((Color)cols[i]).linear;
                inf.Z0 = Mathf.Min(inf.Z0, v[i].z); inf.Z1 = Mathf.Max(inf.Z1, v[i].z);
                if (c == 0) continue;
                sum[c] += new Vector3(lin.r, lin.g, lin.b); inf.Count[c]++;
            }
            for (int c = 1; c <= 8; c++)
            {
                if (inf.Count[c] == 0) continue;
                var mc = sum[c] / inf.Count[c]; inf.Mean[c] = new Color(mc.x, mc.y, mc.z);
                inf.Lum[c] = Mathf.Max(0.02f, mc.x * 0.299f + mc.y * 0.587f + mc.z * 0.114f);
            }
            return inf;
        }
        static Mesh MeshOf(Renderer r) => r is SkinnedMeshRenderer s ? s.sharedMesh : r.TryGetComponent<MeshFilter>(out var f) ? f.sharedMesh : null;
        public static bool Has(Renderer r) { var i = Read(MeshOf(r)); if (i == null) return false; for (int c = 1; c <= 8; c++) if (i.Count[c] > 0) return true; return false; }

        public static Color Lin(string hex) => Ponyo.Hex(hex).linear;
        static Vector4 V(Color lin, float a) => new(lin.r, lin.g, lin.b, a);

        // ---- boats: 1 Hull, 2 Stripe, 3 Trim, 4 Deck; old paint (during the sweep) in 5..8
        static readonly MaterialPropertyBlock mpb = new();
        public static Color BoatColour(int ch, int sw, Info inf)
        {
            if (sw < 0) return inf != null ? inf.Mean[ch + 1] : Color.white;
            return Lin(ch == 3 ? Catalog.Stain[sw % Catalog.Stain.Length] : Catalog.Swatch[sw % Catalog.Swatches]);
        }
        public static void Boat(IEnumerable<Renderer> rs, BoatSt st, double now, Vector3 sweepAxis)
        {
            foreach (var r in rs)
            {
                var inf = Read(MeshOf(r)); if (inf == null || !Has(r)) continue;
                r.GetPropertyBlock(mpb);
                var job = st.Job; float prog = job != null ? (float)job.Progress(now) : 1;
                bool sweeping = job != null && now < job.End;
                for (int ch = 0; ch < 4; ch++)
                {
                    int c = ch + 1; if (inf.Count[c] == 0) continue;
                    int cur = st.Paint[ch], pat = st.Pattern;
                    int old = sweeping && job.Ch == ch ? job.From : cur, oldPat = sweeping && job.Ch == ch ? job.PatFrom : pat;
                    if (sweeping && job.Ch == ch) { cur = job.To; pat = job.PatTo; }
                    mpb.SetVector(chId[c], cur < 0 ? Vector4.zero : V(BoatColour(ch, cur, inf), inf.Lum[c]));
                    mpb.SetVector(chId[c + 4], old < 0 ? V(inf.Mean[c], inf.Lum[c]) : V(BoatColour(ch, old, inf), inf.Lum[c]));
                    // the stripe's pattern mixes in the hull's colour (what's painted between its lines)
                    if (ch == 1)
                    {
                        var hull = BoatColour(0, st.Paint[0], inf);
                        mpb.SetVector(ptId[2], new Vector4(hull.r, hull.g, hull.b, cur < 0 || pat <= 0 ? 0 : pat + 1));
                        mpb.SetVector(ptId[6], new Vector4(hull.r, hull.g, hull.b, old < 0 || oldPat <= 0 ? 0 : oldPat + 1));
                    }
                }
                // the brush line runs stern to bow over the job's painting time (0 = none: everything shows the new paint)
                mpb.SetVector(sweepId, sweeping ? new Vector4(prog, inf.Z0, inf.Z1, job.Ch + 1) : new Vector4(1, 0, 1, 0));
                mpb.SetVector(axisId, sweepAxis);
                r.SetPropertyBlock(mpb);
            }
        }

        // ---- clothes: 1 Hat .. 6 Gloves (dye + pattern), hidden channels (a garment swapped for another)
        public struct Garment { public int Ch; public Color Col; public bool Dye; public int Pat; public Color Pat2; }
        public static void Clothes(Renderer r, Garment[] gs, int hideMask, Vector4 patK)
        {
            var inf = Read(MeshOf(r)); if (inf == null) return;
            r.GetPropertyBlock(mpb);
            for (int c = 1; c <= 8; c++) { mpb.SetVector(chId[c], Vector4.zero); mpb.SetVector(ptId[c], Vector4.zero); }
            if (gs != null) foreach (var g in gs)
                {
                    if (g.Ch < 1 || g.Ch > 8 || inf.Count[g.Ch] == 0) continue;
                    if (g.Dye) mpb.SetVector(chId[g.Ch], V(g.Col, inf.Lum[g.Ch]));
                    else if (g.Pat > 0) mpb.SetVector(chId[g.Ch], V(inf.Mean[g.Ch], inf.Lum[g.Ch]));   // the garment's own colour under a pattern
                    if (g.Pat > 0) mpb.SetVector(ptId[g.Ch], new Vector4(g.Pat2.r, g.Pat2.g, g.Pat2.b, g.Pat));
                }
            mpb.SetFloat(hideId, hideMask); mpb.SetFloat(patUvId, 1); mpb.SetVector(patKId, patK);
            r.SetPropertyBlock(mpb);
        }
    }
}
