using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PortBoss.M1
{
    // M5 test switches (gdd systems "M5"; tools/m5test.sh):
    //   -pb-m5test <dir|none>   solo: the shop transaction suite, the set-spot validator (every buyable has one spot, no
    //                           overlaps, the navmesh stays connected, no spot blocks a walkway or a berth), the paint
    //                           channel test (4 channels x every colour, no extra materials), every shop / installed item /
    //                           paint job / outfit as a still (day and night, dry and wet). -pb-hold s: a pause on each
    //   -pb-m5coop host|rider   (two processes, -pb-netsim 150 2) wardrobe uniqueness, concurrent buys of one item, a
    //                           duplicate transaction, Ask the crew (OK and Not now on the phone card), and everyone seeing
    //                           every purchase, paint job and outfit change
    //   -pb-m5film <dir>        the 60-90 s M5 film (with -pb-film): shops, a paint job, an install, the wardrobe
    public partial class Bot
    {
        IEnumerator M5Tests()
        {
            if (Arg("-pb-m5test") != null) return M5Test(Arg("-pb-m5test"));
            if (Arg("-pb-m5coop") != null) return M5Coop(Arg("-pb-m5coop"));
            if (Arg("-pb-m5film") != null) return M5Film();
            return null;
        }
        int m5Fails;
        void M5(bool ok, string what) { Debug.Log($"[PB] m5: {(ok ? "ok" : "FAIL")} {what}"); if (!ok) m5Fails++; }
        IEnumerator M5Shot(string n, float settle = 0.5f) { for (float t = 0; t < settle; t += Time.unscaledDeltaTime) yield return null; if (shots != null) yield return Shot(n); if (hold > 0) yield return new WaitForSecondsRealtime(hold); }
        static Company Co => Firm.C;
        static void JumpTo(int day, double hour) { var t = DayTime.At(day, hour * 60); if (t > Sky.Minutes) Sky.Jump(t); }
        static int Today => DayTime.Day(Sky.Minutes);

        // stand at a counter, facing it
        IEnumerator AtShop(Works.Counter5 s)
        {
            var d = s.At.position - s.Front; d.y = 0; float yaw = Quaternion.LookRotation(d).eulerAngles.y;
            me.Teleport(s.Front + Vector3.up * 0.05f, yaw); CameraRig.I.Yaw = yaw; CameraRig.I.Pitch = 14; CameraRig.I.Cut();
            yield return new WaitForSeconds(0.6f);
        }
        // a free camera on something (the rig off), for the boat / item stills
        void LookAt(Vector3 target, Vector3 from) { var cam = CameraRig.I; cam.enabled = false; cam.Cam.transform.position = from; cam.Cam.transform.LookAt(target); }
        void RigOn() { CameraRig.I.enabled = true; CameraRig.I.Cut(); }
        IEnumerator BoatShot(Skiff b, string n, float side = 1, float dist = 9, float up = 3.2f)
        {
            var t = b.transform; var c = t.position + Vector3.up * 0.9f;
            // the asked-for side first, then the others: the first view with nothing (pier, lamp, shed) between it and the boat
            Vector3 pick = c + t.right * side * dist * 0.8f + t.forward * dist * 0.55f + Vector3.up * up;
            foreach (var (sx, fz) in new[] { (side, 0.55f), (-side, 0.55f), (side, -0.55f), (-side, -0.55f) })
            {
                var from = c + t.right * sx * dist * 0.8f + t.forward * dist * fz + Vector3.up * up; bool clear = true;
                foreach (var h in Physics.RaycastAll(from, c - from, Vector3.Distance(from, c))) if (!h.collider.transform.IsChildOf(t)) { clear = false; break; }
                if (clear) { pick = from; break; }
            }
            LookAt(c, pick); yield return M5Shot(n, 0.6f);
        }
        static List<Renderer> PaintedParts(Skiff b) { var l = new List<Renderer>(); foreach (var r in b.GetComponentsInChildren<Renderer>(true)) if (r is MeshRenderer && Channels.Has(r)) l.Add(r); return l; }
        static Bounds BoundsOf(GameObject g)
        {
            Bounds b = default; bool any = false;
            foreach (var r in g.GetComponentsInChildren<Renderer>()) { if (r is ParticleSystemRenderer || !r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return any ? b : new Bounds(g.transform.position, Vector3.zero);
        }

        // ================================================================ solo: the suites and the stills
        IEnumerator M5Test(string dir)
        {
            Ctl.Bot = true; Sleep.Off = true; shots = dir == "none" ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            hold = ArgF("-pb-hold", 0);
            Works.OnLog += x => Debug.Log("[PB] m5: works " + x);
            yield return new WaitForSeconds(1.5f);
            var W = Works.I; M5(W != null && W.Shops.Count == 3, $"three shops ({W?.Shops.Count})");
            if (W == null) { Debug.Log("[PB] RESULT m5test: FAIL (no Works)"); Application.Quit(); yield break; }
            M5(Works.NavOk, "the town navmesh is built");
            JumpTo(1, 10); yield return null;

            // ---------------- 1. the shop transaction suite (solo half; the co-op half is -pb-m5coop)
            int cash0 = Co.Cash; string why;
            bool okK = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "boat:kestrel", 0, false, out why);
            M5(!okK && Co.Cash == cash0, $"the Kestrel at ${cash0}: refused, \"{why}\", the bank unchanged");
            Co.Earn(Firm.I.NewTx("tester"), Works.MyName, "Tester cash (M5 test)", 60000, Sky.Minutes); Co.Act = 2; Firm.I.Dirty = true;
            int c1 = Co.Cash; string tx = Firm.I.NewTx("m5");
            bool a1 = Works.HostBuy(tx, Works.MyKey, Works.MyName, "furn:plant", 0, false, out var w1);
            bool a2 = Works.HostBuy(tx, Works.MyKey, Works.MyName, "furn:plant", 0, false, out var w2);
            M5(a1 && !a2 && Co.Cash == c1 - 30, $"one transaction id twice (a retry): bought once (${c1} -> ${Co.Cash}), the second \"{w2}\"");
            bool a3 = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "furn:plant", 0, false, out var w3);
            M5(!a3 && Co.Cash == c1 - 30, $"an owned item again with a new id: refused \"{w3}\"");
            bool a4 = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "office:2", 0, false, out var w4);
            M5(!a4 && w4 != "", $"the Harbour Office at Act 2: refused \"{w4}\"");
            bool a5 = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "dock:b3", 0, false, out var w5);
            M5(!a5 && w5 != "", $"berth B3 before B2: refused \"{w5}\"");
            M5(!Co.NeedsAsk("boat:kestrel", 0, Works.MyKey, false), "solo: never Ask the crew");
            M5(Co.Check() == "" && Co.CheckM5() == "", $"the ledger sums ({Co.Check()}{Co.CheckM5()})");

            // ---------------- 2. every shop: the counter, the keeper, every tab (stills)
            foreach (var s in W.Shops)
            {
                yield return AtShop(s);
                M5(W.Near == s, $"{s.Name}: standing at the counter ({(s.Real ? "map anchor" : "temporary spot")})");
                M5(s.Keeper != null && s.Keeper.gameObject.activeInHierarchy, $"{s.Name}: {s.Owner} is at the counter at 10:00");
                { var back = s.Front - s.At.position; back.y = 0; back = back.normalized; var side = Vector3.Cross(Vector3.up, back); LookAt(s.At.position + Vector3.up * 1.4f, s.Front + back * 4.5f + side * 2.2f + Vector3.up * 2.6f); yield return M5Shot($"world-{s.Id}", 1.0f); RigOn(); yield return null; }
                YShop.Show(s); yield return null;
                M5(YShop.Open, $"{s.Name}: E opens the shop");
                var cats = new List<string>(); foreach (var w in Catalog.Of(s.S)) if (!cats.Contains(w.Cat)) cats.Add(w.Cat);
                foreach (var cat in cats) { YShop.I.Tab(cat); yield return M5Shot($"shop-{s.Id}-{cat.ToLowerInvariant().Replace(' ', '-').Replace("&", "and")}"); }
                if (s.S == Store.Doyle)
                {
                    YShop.I.Pick(Catalog.Get("paint:biscuit:0")); YShop.I.Swatch(5); yield return M5Shot("shop-doyle-paint-tryon", 0.8f);
                    M5(Works.TryPaint is { } tp && tp.boat == "biscuit" && tp.sw == 5, "trying a hull colour shows it on Biscuit (the live preview)");
                    YShop.I.Pick(Catalog.Get("paint:biscuit:1")); YShop.I.PatternOf(3); YShop.I.Swatch(12); yield return M5Shot("shop-doyle-stripe-tryon", 0.8f);
                    YShop.I.Pick(Catalog.Get("boat:kestrel")); yield return M5Shot("shop-doyle-kestrel", 0.8f);
                }
                if (s.S == Store.Hartley)
                {
                    YShop.I.Pick(Catalog.Get("wear:captain")); yield return M5Shot("shop-hartleys-mirror-captain", 0.8f);
                    M5(Wardrobe.TryOn == "wear:captain", "Hartley's mirror: the cap tried on me");
                    YShop.I.Pick(Catalog.Get("wear:hawaiian")); yield return M5Shot("shop-hartleys-mirror-hawaiian", 0.8f);
                }
                YShop.I.Hide(); yield return null;
                M5(Works.TryPaint == null && Wardrobe.TryOn == null, $"{s.Name}: closing clears the try-on");
            }
            // closed: Plank & Post at 19:00, a closed sign, the keeper gone in
            JumpTo(1, 19.25); yield return new WaitForSeconds(0.3f);
            var plank = W.Shops.Find(x => x.S == Store.Plank); yield return AtShop(plank);
            var pr = Works.Prompt(me); M5(pr != null && pr.Contains("·") && !YShop.Open && !plank.IsOpen(Sky.Minutes), $"Plank & Post after 18:00: \"{pr}\"");
            M5(plank.Keeper == null || !plank.Keeper.Visible, "Ingrid has gone in");

            // ---------------- 3. the set-spot validator: before
            JumpTo(2, 10); yield return new WaitForSeconds(0.5f);
            var spawn = Landing.I != null && Landing.I.Spawn != null ? Landing.I.Spawn.position : me.transform.position;
            var targets = new List<(string n, Vector3 p)>();
            foreach (var s in W.Shops) targets.Add(("counter " + s.Id, s.Front));
            if (W.WarehouseDoor is { } wd) targets.Add(("warehouse door", wd));
            if (Landing.I != null && Landing.I.LadderTop != null) targets.Add(("Biscuit's ladder", Landing.I.LadderTop.position));
            if (W.KestrelSlip != null) targets.Add(("Doyle's pontoon", W.KestrelSlip.position - W.KestrelSlip.right * 2.3f));
            var basePaths = new Dictionary<string, List<Vector3>>();
            foreach (var (n, p) in targets) { var path = Works.Path(spawn, p); bool full = path.Count > 2 || (path.Count == 2 && Vector3.Distance(path[0], path[1]) < 40); basePaths[n] = path; M5(Works.NavOk && path.Count >= 2, $"walk: the spawn to the {n}: {Works.Length(path):F0} m, {path.Count} corners"); }
            // every buyable has one spot
            var buyable = Catalog.All.Where(w => w.Lock == null && (w.At == Store.Plank || w.Id == "boat:kestrel")).ToList();
            var bySpot = new Dictionary<string, List<string>>(); var missing = new List<string>();
            foreach (var w in buyable)
            {
                string sp = w.Spot; if (sp == null) { missing.Add(w.Id + " (none)"); continue; }
                bool has = sp == "office" ? W.Office != null || W.Spots.ContainsKey("office") : sp == "doyle_slip" ? W.KestrelSlip != null : W.Spots.ContainsKey(sp);
                if (!has && w.Needs == "office:1" && M1Assets.I.M5Prefab("Bld_office1") is { } o1) has = Util.Find(o1.transform, sp) != null;   // comes with the patched office
                if (!has && !(w.Id == "furn:balcony")) missing.Add($"{w.Id} ({sp})");
                string fam = w.Id.StartsWith("wh:") || w.Id.StartsWith("office:") ? w.Id.Split(':')[0] : w.Id;
                if (!bySpot.TryGetValue(sp, out var l)) bySpot[sp] = l = new List<string>(); if (!l.Contains(fam)) l.Add(fam);
            }
            M5(missing.Count == 0, $"every buyable ({buyable.Count}) has its set spot{(missing.Count > 0 ? ": missing " + string.Join(", ", missing) : "")}");
            var shared = bySpot.Where(kv => kv.Value.Count > 1).Select(kv => kv.Key + ": " + string.Join("+", kv.Value)).ToList();
            M5(shared.Count == 0, $"one item per spot{(shared.Count > 0 ? ": shared " + string.Join("; ", shared) : "")}");

            // ---------------- 4. installs: furniture now (pop), builds overnight (07:00 timelapse)
            var now = buyable.Where(w => w.Fit == Fit.Now && w.Id != "furn:plant").ToList();
            var night = buyable.Where(w => w.Fit == Fit.Overnight && !w.Id.StartsWith("office:") && w.Id != "furn:balcony").ToList();
            foreach (var w in now) { if (w.Needs != null && !Co.Has(w.Needs)) continue; bool ok = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, w.Id, 0, false, out var wy); if (!ok) M5(false, $"buy {w.Id}: {wy}"); }
            yield return new WaitForSeconds(0.6f);
            int popped = now.Count(w => W.IsShown(w.Id));
            M5(popped == now.Count(w => Co.Has(w.Id)), $"furniture installs at once: {popped} shown of {now.Count(w => Co.Has(w.Id))} bought");
            var chair = W.ShownOf("furn:chair"); if (chair != null) { { var cp = chair.transform.position; var mid = W.Office != null ? BoundsOf(W.Office.gameObject).center : cp + chair.transform.forward; var inw = mid - cp; inw.y = 0; inw = inw.sqrMagnitude > 0.01f ? inw.normalized : chair.transform.forward; LookAt(cp + Vector3.up * 0.5f, cp + inw * 2.6f + Vector3.up * 1.7f); } yield return M5Shot("installed-chair-pop", 0.12f); yield return M5Shot("installed-chair-settled", 0.8f); RigOn(); }
            // two rounds: B3 needs B2, the lamps need the one before
            for (int round = 0; round < 4; round++)
            {
                foreach (var w in night) { if (Co.Has(w.Id) || Co.Ordered(w.Id) || (w.Needs != null && !Co.Has(w.Needs))) continue; bool ok = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, w.Id, 0, false, out var wy); if (!ok) Debug.Log($"[PB] m5: order {w.Id}: {wy}"); }
                M5(night.Where(w => Co.Ordered(w.Id)).All(w => !W.IsShown(w.Id)), $"round {round}: the overnight builds wait for 07:00 ({Co.Orders.Count} orders)");
                yield return null;
                // the morning: overnight builds land and play their timelapse
                JumpTo(Today + 1, 7.0); yield return new WaitForSeconds(0.35f);
                if (round == 0 && W.ShownOf("dock:planks") is { } pl) { LookAt(pl.transform.position + Vector3.up * 0.3f, pl.transform.position + new Vector3(3, 3, 3)); yield return M5Shot("installed-planks-timelapse", 0.1f); yield return M5Shot("installed-planks", 2.0f); RigOn(); }
                yield return new WaitForSeconds(1.0f); JumpTo(Today, 10);
            }
            var stillOrdered = night.Where(w => !Co.Has(w.Id)).Select(w => w.Id).ToList();
            M5(stillOrdered.Count == 0, $"every overnight build installed by 07:00 ({night.Count}){(stillOrdered.Count > 0 ? ": not " + string.Join(", ", stillOrdered) : "")}");
            M5(Co.WhTier == 2 && Co.ColdSlots == Company.Cold2, $"the warehouse is T2 with the cold room (tier {Co.WhTier}, {Co.ColdSlots} cold)");
            // the office tiers (the shack becomes Walt's office)
            Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "office:1", 0, false, out var wo); JumpTo(Today + 1, 7.0); yield return new WaitForSeconds(2.5f); JumpTo(Today, 10);
            M5(Co.OfficeTier == 1, $"the office T1 is built overnight ({(wo == "" ? "ordered" : wo)})");
            // what needs the patched office (the bunk, the double bed, the map table): now it fits
            var later = now.Where(w => w.Needs == "office:1").ToList();
            foreach (var w in later) { bool ok = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, w.Id, 0, false, out var wy); if (!ok) M5(false, $"buy {w.Id}: {wy}"); }
            yield return new WaitForSeconds(0.6f);
            M5(later.Count == 3 && later.All(w => W.ShownOf(w.Id) != null), $"with the office T1: {string.Join(", ", later.Select(w => w.Id + (W.ShownOf(w.Id) != null ? " in its spot" : " MISSING")))}");
            // every item in its spot, still in place, after the office tier moved the furniture over
            yield return new WaitForSeconds(0.6f);
            // overlaps
            var items = W.ShownAll.Where(kv => kv.Value != null && !kv.Key.StartsWith("up:") && !kv.Key.StartsWith("rep:")).ToList();
            var bad = new List<string>();
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = BoundsOf(items[i].Value); var b = BoundsOf(items[j].Value); a.Expand(-0.06f); b.Expand(-0.06f);
                    if (a.size.sqrMagnitude > 0 && b.size.sqrMagnitude > 0 && a.Intersects(b)) bad.Add(items[i].Key + "/" + items[j].Key);
                }
            M5(bad.Count == 0, $"no two installed items overlap ({items.Count} items){(bad.Count > 0 ? ": " + string.Join(", ", bad) : "")}");
            // walkways: the old walks don't run through anything standing; berths stay clear
            var blocks = new List<string>();
            foreach (var kv in basePaths)
            {
                var p = kv.Value; float len = Works.Length(p);
                for (float d = 0.5f; d < len - 0.5f; d += 0.25f)
                {
                    var q = Works.Along(p, d, out _);
                    foreach (var it in items)
                    {
                        if (it.Key.StartsWith("dock:planks") || it.Key.StartsWith("dock:b")) continue;   // floor boards: walked on
                        var bb = BoundsOf(it.Value); if (bb.max.y < q.y + 0.3f || bb.min.y > q.y + 1.6f) continue;
                        bb.Expand(new Vector3(0.2f, 0, 0.2f)); if (bb.Contains(new Vector3(q.x, Mathf.Clamp(q.y + 0.8f, bb.min.y, bb.max.y), q.z))) { string k = $"{it.Key} on the walk to the {kv.Key}"; if (!blocks.Contains(k)) blocks.Add(k); }
                    }
                }
            }
            var berths = new List<(string n, Transform t)>(); if (Landing.I != null && Landing.I.Moor != null) berths.Add(("Biscuit's berth", Landing.I.Moor)); if (W.KestrelBerth != null) berths.Add(("the Kestrel's berth", W.KestrelBerth));
            foreach (var (n, t) in berths)
            {
                var box = new Bounds(t.position + Vector3.up * 1.2f, Vector3.zero); foreach (var cn in new[] { new Vector3(-1.4f, -1, -4), new Vector3(1.4f, 2.4f, 4) }) box.Encapsulate(t.TransformPoint(cn));
                foreach (var it in items) if (BoundsOf(it.Value).Intersects(box)) blocks.Add($"{it.Key} in {n}");
            }
            M5(blocks.Count == 0, $"no spot blocks a walkway or a berth{(blocks.Count > 0 ? ": " + string.Join("; ", blocks) : "")}");
            // the navmesh, rebuilt with everything installed, still connects
            W.RebuildNav(); yield return null;
            foreach (var (n, p) in targets)
            {
                var path = Works.Path(spawn, p); float l0 = Works.Length(basePaths[n]), l1 = Works.Length(path);
                M5(Works.NavOk && path.Count >= 2 && l1 < l0 * 1.6f + 4, $"after the installs: the spawn to the {n} still walks ({l0:F0} -> {l1:F0} m)");
            }

            // ---------------- 5. Doyle's: repairs, upgrades, paint (the crew walks over and paints), the Kestrel
            var bis = Skiff.At(0); var bst = Co.BoatOf("biscuit");
            float top0 = bis.Top;
            bool r1 = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "rep:biscuit:engine", 0, false, out var rw1);
            M5(r1 && Co.Ordered("rep:biscuit:engine") && !bst.Engine, $"the engine rebuild is ordered for 07:00 ({rw1})");
            int cr = Co.Cash; bool r2 = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "rep:biscuit:hull", 0, true, out var rw2);
            yield return null; yield return null;
            M5(r2 && bst.Hull && cr - Co.Cash == 240 && bis.Sound, $"the hull patch, rush: done now at double price (${cr - Co.Cash}), Biscuit sound {bis.Sound}");
            JumpTo(Today + 1, 7.0); yield return null; yield return null; JumpTo(Today, 10); yield return null;
            M5(bst.Engine && Mathf.Abs(bis.Top - 6) < 0.01f, $"07:00: the engine rebuilt, top speed {top0:F1} -> {bis.Top:F1} m/s");
            Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "up:biscuit:engine1", 0, false, out _);
            Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "up:biscuit:lights", 0, false, out _);
            Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "up:biscuit:pump", 0, false, out _);
            yield return new WaitForSeconds(0.5f);
            M5(Mathf.Abs(bis.Top - 6.9f) < 0.01f && bis.Pump, $"Engine I: {bis.Top:F2} m/s (6 x 1.15); the bilge pump {(bis.Pump ? "on" : "off")}");
            M5(W.IsShown("up:biscuit:lights") && W.IsShown("up:biscuit:pump"), "the deck lights and the pump are on her");
            yield return BoatShot(bis, "upgrade-biscuit-lights-pump"); RigOn();

            // paint: the hull, a stripe pattern, the trim, the deck, each by Doyle's crew
            M5(bis.Moored, "Biscuit is moored at the landing (Doyle's crew paints her there)");
            var crew = W.Crew; var home = crew.Select(n => n.transform.position).ToList();
            foreach (var (ch, arg, label) in new[] { (0, 3, "hull"), (1, 3 * 100 + 0, "stripe"), (2, 6, "trim"), (3, 1, "deck") })
            {
                Time.timeScale = ch == 0 ? 1 : 4;
                double t0 = Sky.Minutes;
                bool ok = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, $"paint:biscuit:{ch}", arg, false, out var pw);
                var job = bst.Job; M5(ok && job != null && job.Ch == ch, $"paint the {label}: the job is on ({pw}), walk {job?.Walk:F0} + paint {job?.Paint:F0} min");
                if (job == null) continue;
                // walking over
                for (float k = 0; k < 300 && Sky.Minutes < job.Start + job.Walk * 0.5; k += Time.unscaledDeltaTime) yield return null;
                float moved = crew.Count > 0 ? Vector3.Distance(crew[0].transform.position, home[0]) : 0;
                M5(moved > 2, $"{label}: Doyle's lad walks over ({moved:F1} m from the slipway)");
                if (ch == 0) { var c = crew[0].transform.position; LookAt(c + Vector3.up, c + (bis.transform.position - c).normalized * -5 + Vector3.up * 3); yield return M5Shot("paint-crew-walking", 0.2f); RigOn(); }
                // painting: the brush line halfway
                for (float k = 0; k < 300 && job.Progress(Sky.Minutes) < 0.5; k += Time.unscaledDeltaTime) yield return null;
                M5(crew.Count == 0 || Vector3.Distance(crew[0].transform.position, bis.transform.position) < 6, $"{label}: painting beside her ({(crew.Count > 0 ? Vector3.Distance(crew[0].transform.position, bis.transform.position) : 0):F1} m)");
                yield return BoatShot(bis, $"paint-{label}-half", 1, 7, 2.4f); RigOn();
                for (float k = 0; k < 300 && Sky.Minutes < job.End + job.Walk; k += Time.unscaledDeltaTime) yield return null;
                yield return null;
                Time.timeScale = 1;
                M5(bst.Paint[ch] == arg % 100, $"{label}: the new colour stays ({(ch == 3 ? Catalog.StainName[bst.Paint[ch]] : Catalog.SwatchName[bst.Paint[ch]])})");
            }
            yield return BoatShot(bis, "paint-biscuit-done"); RigOn();
            // the trim again, then back to the colour before: free
            Time.timeScale = 4;
            foreach (var (sw, free) in new[] { (10, false), (6, true) })
            {
                int cb = Co.Cash; bool okb = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "paint:biscuit:2", sw, false, out var fw);
                M5(okb && (cb - Co.Cash == (free ? 0 : Catalog.TrimPrice)), $"the trim {Catalog.SwatchName[sw]}: ${cb - Co.Cash}{(free ? " (the one before: free)" : "")} {fw}");
                for (float k = 0; k < 300 && bst.Job != null && Sky.Minutes < bst.Job.End + bst.Job.Walk; k += Time.unscaledDeltaTime) yield return null;
            }
            Time.timeScale = 1;

            // the Kestrel: walk aboard before buying, can't drive her; bought: she's at the landing and drives
            var kes = W.Kestrel; M5(kes != null, "the Kestrel waits at Doyle's pontoon");
            if (kes != null)
            {
                M5(!Works.CanDrive(kes), "not hers yet: the helm refuses");
                me.Teleport(kes.transform.TransformPoint(new Vector3(0, 1.2f, -1.5f)), kes.transform.eulerAngles.y); yield return new WaitForSeconds(1.0f);
                M5(me.OnBoat, $"walked aboard the Kestrel before buying (on a boat: {me.OnBoat})");
                CameraRig.I.Yaw = kes.transform.eulerAngles.y + 160; yield return M5Shot("kestrel-aboard-before-buying", 0.8f);
                me.Teleport(spawn + Vector3.up * 0.1f, 0); yield return new WaitForSeconds(0.6f);
                LookAt(kes.transform.position + Vector3.up, kes.transform.position + kes.transform.right * -9 + kes.transform.forward * 6 + Vector3.up * 4); yield return M5Shot("kestrel-at-doyles", 0.6f); RigOn();
                JumpTo(Today + 1, 10); yield return null;   // Doyle's open (the paint jobs ran into the evening)
                bool kb = Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "boat:kestrel", 0, false, out var kw);
                yield return new WaitForSeconds(1.0f);
                M5(kb && Works.CanDrive(kes) && W.KestrelBerth != null && Vector3.Distance(kes.transform.position, W.KestrelBerth.position) < 3, $"bought ({kw}): she's at the landing ({(W.KestrelBerth != null ? Vector3.Distance(kes.transform.position, W.KestrelBerth.position) : -1):F1} m from her berth) and drives");
                yield return BoatShot(kes, "kestrel-at-the-landing", -1, 12, 4); RigOn();
            }

            // ---------------- 6. the paint channel test: 4 channels x every colour, no extra materials
            foreach (var b in Skiff.All)
            {
                var rs = PaintedParts(b); var mats = rs.Select(r => r.sharedMaterials).ToList(); var names = new HashSet<string>(); foreach (var ms in mats) foreach (var m in ms) if (m != null) names.Add(m.name);
                var has = new int[5]; foreach (var r in rs) { var inf = Channels.Read(r.GetComponent<MeshFilter>().sharedMesh); for (int c = 1; c <= 4; c++) has[c] += inf.Count[c]; }
                M5(rs.Count > 0 && has[1] > 0 && has[2] > 0 && has[3] > 0 && has[4] > 0, $"{b.Name}: {rs.Count} painted parts, channel faces hull {has[1]} stripe {has[2]} trim {has[3]} deck {has[4]}");
                var mpb = new MaterialPropertyBlock(); int checks = 0, wrong = 0;
                for (int ch = 0; ch < 4; ch++)
                {
                    int n = ch == 3 ? Catalog.Stain.Length : Catalog.Swatches;
                    for (int sw = 0; sw < n; sw++)
                        for (int pat = 0; pat < (ch == 1 ? Catalog.Patterns : 1); pat++)
                        {
                            var st = Co.BoatOf(b.Id).Copy(); st.Job = null; st.Paint[ch] = sw; if (ch == 1) st.Pattern = pat;
                            Channels.Boat(rs, st, Sky.Minutes, Vector3.forward);
                            foreach (var r in rs)
                            {
                                var inf = Channels.Read(r.GetComponent<MeshFilter>().sharedMesh); if (inf.Count[ch + 1] == 0) continue;
                                r.GetPropertyBlock(mpb); var v = mpb.GetVector("_Ch" + (ch + 1)); var want = Channels.BoatColour(ch, sw, inf); checks++;
                                bool okc = Mathf.Abs(v.x - want.r) < 1e-4f && Mathf.Abs(v.y - want.g) < 1e-4f && Mathf.Abs(v.z - want.b) < 1e-4f;
                                if (ch == 1) okc &= Mathf.Approximately(mpb.GetVector("_Pt2").w, pat <= 0 ? 0 : pat + 1);
                                if (!okc) wrong++;
                            }
                        }
                }
                bool same = true; for (int i = 0; i < rs.Count; i++) { var now_ = rs[i].sharedMaterials; same &= now_.Length == mats[i].Length; for (int k = 0; k < now_.Length && same; k++) same &= now_[k] == mats[i][k]; }
                M5(wrong == 0 && checks > 0, $"{b.Name}: 4 channels x {Catalog.Swatches}/{Catalog.Stain.Length} colours x {Catalog.Patterns} stripe patterns: {checks} channel values set, {wrong} wrong");
                M5(same, $"{b.Name}: no extra materials (still {names.Count}: {string.Join(", ", names)})");
            }

            // ---------------- 7. paint stills: both boats, three schemes, day and night, dry and wet
            if (shots != null)
            {
                var schemes = new[] { (new[] { 3, 12, 6, 1 }, 3, "harbour"), (new[] { 9, 0, 22, 4 }, 5, "sunset"), (new[] { 18, 21, 14, 7 }, 7, "deep") };
                foreach (var (light, h, wx) in new[] { ("day", 11.0, 1), ("day-wet", 11.5, 4), ("night", 22.0, 1), ("night-wet", 22.5, 4) })
                {
                    Sky.ForceWx = wx; JumpTo(light.StartsWith("night") ? Today : Today + 1, h); yield return new WaitForSeconds(2.5f);
                    foreach (var b in Skiff.All)
                        foreach (var (p, pat, name) in schemes)
                        {
                            var st = Co.BoatOf(b.Id); for (int c = 0; c < 4; c++) st.Paint[c] = p[c]; st.Pattern = pat; st.Job = null;
                            yield return BoatShot(b, $"paint-{b.Id}-{name}-{light}", 1, b.Id == "kestrel" ? 11 : 8, 3);
                        }
                }
                RigOn(); Sky.ForceWx = -1;
            }

            // ---------------- 8. the wardrobe: the crew pick, clothes bought and worn, a dye
            JumpTo(Today + 1, 10); yield return new WaitForSeconds(0.5f);
            var har = W.Shops.Find(x => x.S == Store.Hartley); yield return AtShop(har);
            YShop.Show(har); yield return null;
            int ch0 = Co.Cash; YShop.I.BuyNow("wear:captain"); yield return M5Shot("shop-hartleys-bought", 0.9f);
            var mine = Co.OutfitOf(Works.MyKey);
            M5(mine.Owned.Contains("wear:captain") && mine.Wear[0] == "wear:captain" && ch0 - Co.Cash == 250, $"bought and wearing the captain's cap (${ch0 - Co.Cash})");
            YShop.I.BuyNow("wear:hawaiian"); YShop.I.BuyNow("dye", 1 * 100 + 9); yield return new WaitForSeconds(0.5f);
            M5(mine.Wear[1] == "wear:hawaiian" && mine.Dye[1] == 9, $"the Hawaiian shirt, dyed {Catalog.SwatchName[9]} (dye {mine.Dye[1]})");
            YShop.I.Hide(); yield return new WaitForSeconds(0.4f);
            M5(Wardrobe.HatOf(me.BodyGo) == "wear:captain", $"the cap is on my head ({Wardrobe.HatOf(me.BodyGo)})");
            Wardrobe.Show(); yield return null; M5(Wardrobe.Open, "the wardrobe opens");
            yield return M5Shot("wardrobe-mine", 1.0f);
            for (int p = 0; p < 6; p++) { Wardrobe.I.Tap("crew " + Catalog.Preset[p]); yield return M5Shot($"wardrobe-preset-{Catalog.Preset[p].ToLowerInvariant()}", 0.9f); }
            Wardrobe.I.Tap("crew Sully"); yield return new WaitForSeconds(0.6f);   // a second tap on the shown one: be them
            M5(Co.OutfitOf(Works.MyKey).Preset == 5 && Angler.Body == 5, $"picked Sully in the wardrobe (preset {Co.OutfitOf(Works.MyKey).Preset}, body {Angler.Body})");
            Wardrobe.I.Tap("crew Pip"); yield return new WaitForSeconds(0.4f); Wardrobe.I.Tap("crew Pip"); yield return new WaitForSeconds(0.4f);
            foreach (var t in new[] { "Hat", "Top", "Over", "Legs", "Boots", "Gloves" }) { Wardrobe.I.Tap("tab " + t); yield return M5Shot($"wardrobe-tab-{t.ToLowerInvariant()}", 0.4f); }
            Wardrobe.I.Hide(); yield return null;
            Debug.Log($"[PB] m5: bank ${Co.Cash}, ledger {Co.Ledger.Count} lines, owned {Co.Owned.Count}");
            M5(Co.Check() == "" && Co.CheckM5() == "", $"the ledger sums at the end ({Co.Check()}{Co.CheckM5()})");
            Debug.Log($"[PB] RESULT m5test: {(m5Fails == 0 ? "PASS" : m5Fails + " FAIL")}");
            Application.Quit();
        }

        // ================================================================ co-op: two processes (tools/m5test.sh, netsim)
        IEnumerator M5Coop(string role)
        {
            Ctl.Bot = true; Sleep.Off = true; shots = Arg("-pb-m5shots"); if (shots != null) Directory.CreateDirectory(shots);
            Works.OnLog += x => Debug.Log("[PB] m5: works " + x);
            for (float t = 0; t < 40 && !(Coop.Joined && Coop.I.Roster.Count >= 2); t += Time.unscaledDeltaTime) yield return null;
            M5(Coop.Joined && Coop.I.Roster.Count >= 2, $"the crew is together ({(Coop.I != null ? Coop.I.Roster.Count : 0)})");
            if (!Coop.Joined) { Debug.Log("[PB] RESULT m5coop: FAIL (never connected)"); Application.Quit(); yield break; }
            yield return new WaitForSeconds(3f);
            IEnumerator Until(System.Func<bool> c, float secs) { for (float t = 0; t < secs && !c(); t += Time.unscaledDeltaTime) yield return null; }
            Coop.Entry Mine() => Coop.I.Roster.Find(e => e.Id == Coop.Me);
            Coop.Entry Other() => Coop.I.Roster.Find(e => e.Id != Coop.Me);
            int Lines(string what) => Co.Ledger.Count(e => e.What == what);
            if (role == "host")
            {
                var W = Works.I; string rk = Other().Key ?? Coop.MyKey(Other().Name), rn = Other().Name;
                Co.Earn(Firm.I.NewTx("tester"), Works.MyName, "Tester cash (M5 co-op)", 20000, Sky.Minutes); Co.Act = 2; Firm.I.Dirty = true;   // ask limit $5,000 (a quarter of the bank)
                // ---- 1. wardrobe uniqueness: I'm Pip; the rider tries Pip (taken), then picks Rani
                Works.Pick(0); yield return Until(() => Mine().Crew == 0, 5);
                M5(Mine().Crew == 0, "I picked Pip");
                yield return Until(() => Other().Crew == 3, 40);
                M5(Other().Crew == 3 && Co.OutfitOf(rk).Preset == 3, $"{rn} couldn't be Pip and is Rani (roster {Other().Crew}, outfit {Co.OutfitOf(rk).Preset})");
                M5(Coop.I.Roster.Select(e => e.Crew).Distinct().Count() == Coop.I.Roster.Count, "two players never share a crew member");
                yield return Until(() => Coop.I.Mates.TryGetValue(Other().Id, out var m) && m != null && m.Crew == 3, 10);
                M5(Coop.I.Mates.TryGetValue(Other().Id, out var mate) && mate != null && mate.Crew == 3, "here, their body is Rani");
                // ---- 2. a purchase everyone sees: the chair (instant), a paint job, my new cap
                yield return new WaitForSeconds(2f);
                Works.Buy("furn:chair", 0); Works.Buy("wear:captain", 0); Works.Buy("paint:biscuit:0", 9);
                M5(Co.Owned.Contains("furn:chair") && Co.OutfitOf(Works.MyKey).Wear[0] == "wear:captain" && Co.BoatOf("biscuit").Job != null, "I bought the chair, the cap and a hull paint job");
                // ---- 3. concurrent buys: both of us buy the desk at 11:00 game time; the rider's retry (one tx twice)
                yield return Until(() => DayTime.OfDay(Sky.Minutes) >= 11 * 60, 200);
                Works.Buy("furn:desk", 0);
                yield return new WaitForSeconds(6f);
                M5(Lines("Walt's desk, mended") == 1 && Co.Owned.Contains("furn:desk"), $"the desk, bought by both at once: one ledger line ({Lines("Walt's desk, mended")})");
                M5(Lines("Shelves") == 1, $"{rn}'s shelves sent twice with one id: one line ({Lines("Shelves")})");
                // ---- 4. Ask the crew: OK on my phone
                yield return Until(() => Co.Asks.Exists(a => a.Key == rk && a.Item == "boat:kestrel" && a.St == AskSt.Open), 40);
                var ask = Co.Asks.Find(a => a.Key == rk && a.Item == "boat:kestrel");
                M5(ask != null && ask.Price == 5500, $"{rn} asked the crew for the Kestrel (${ask?.Price}, over the ${Company.AskLimit(Co.Cash)} limit)");
                Phone.I.Show(Phone.Scr.Thread, rn); yield return null; yield return null;
                if (shots != null) yield return M5Shot("coop-ask-card-host", 0.5f);
                M5(Phone.I.Tap("OK, buy it"), "OK, buy it on my phone");
                yield return null; yield return null;
                M5(ask != null && ask.St == AskSt.Approved && Co.Owned.Contains("boat:kestrel"), $"the Kestrel is the crew's ({ask?.St}, by {ask?.Answer})");
                if (shots != null) yield return M5Shot("coop-ask-card-host-ok", 0.5f);
                Phone.I.Hide();
                // ---- 5. Not now
                yield return Until(() => Co.Asks.Exists(a => a.Key == rk && a.Item == "office:1" && a.St == AskSt.Open), 40);
                var ask2 = Co.Asks.Find(a => a.Key == rk && a.Item == "office:1");
                Phone.I.Show(Phone.Scr.Thread, rn); yield return null; yield return null;
                M5(ask2 != null && Phone.I.Tap("Not now"), "Not now on my phone");
                yield return null; M5(ask2 != null && ask2.St == AskSt.Declined && !Co.Ordered("office:1"), "the office isn't bought");
                Phone.I.Hide();
                // ---- 6. the rider's cap: I see it on them
                yield return Until(() => Co.OutfitOf(rk).Wear[0] == "wear:souwester", 40);
                yield return Until(() => Coop.I.Mates.TryGetValue(Other().Id, out var m) && Wardrobe.HatOf(m.BodyGo) == "wear:souwester", 10);
                M5(Coop.I.Mates.TryGetValue(Other().Id, out var m2) && Wardrobe.HatOf(m2.BodyGo) == "wear:souwester", $"{rn}'s new sou'wester, on their head here");
                M5(Co.Check() == "" && Co.CheckM5() == "", $"the ledger sums ({Co.Check()}{Co.CheckM5()})");
                yield return new WaitForSeconds(10f);
                Debug.Log($"[PB] m5: host final ${Co.Cash}, ledger {Co.Ledger.Count}");
                Debug.Log($"[PB] RESULT m5coop host: {(m5Fails == 0 ? "PASS" : m5Fails + " FAIL")}");
            }
            else
            {
                var W = Works.I; string hn = Other().Name;
                // ---- 1. Pip is taken: the wardrobe's pick is refused, Rani isn't
                yield return Until(() => Other().Crew == 0, 30);
                Wardrobe.Show(); yield return new WaitForSeconds(0.6f);
                if (shots != null) yield return M5Shot("coop-wardrobe-pip-taken", 0.6f);
                Wardrobe.I.Tap("crew Pip"); yield return new WaitForSeconds(0.4f); Wardrobe.I.Tap("crew Pip"); yield return new WaitForSeconds(3f);
                M5(Mine().Crew != 0, $"Pip is taken by {hn}: I stay {Mine().Crew}");
                Wardrobe.I.Tap("crew Rani"); yield return new WaitForSeconds(0.4f); Wardrobe.I.Tap("crew Rani"); yield return Until(() => Mine().Crew == 3, 20);
                M5(Mine().Crew == 3 && Angler.Body == 3, $"I'm Rani (roster {Mine().Crew}, body {Angler.Body})");
                Wardrobe.I.Hide();
                // ---- 2. the host's buys reach me: the chair pops in, the paint job runs here, the host's cap
                yield return Until(() => W.IsShown("furn:chair") && Co.BoatOf("biscuit").Job != null, 60);
                M5(W.IsShown("furn:chair"), "the host's chair is in its spot here");
                var job = Co.BoatOf("biscuit").Job; M5(job != null && job.To == 9, $"the host's paint job runs here too ({job?.To})");
                yield return Until(() => Coop.I.Mates.TryGetValue(Other().Id, out var m) && Wardrobe.HatOf(m.BodyGo) == "wear:captain", 20);
                M5(Coop.I.Mates.TryGetValue(Other().Id, out var hm) && Wardrobe.HatOf(hm.BodyGo) == "wear:captain", $"{hn}'s captain's cap, on their head here");
                if (job != null)
                {
                    yield return Until(() => job.Progress(Sky.Minutes) >= 0.4, 120);
                    M5(W.Crew.Count > 0 && Vector3.Distance(W.Crew[0].transform.position, Skiff.At(0).transform.position) < 6, "Doyle's lad paints Biscuit here, in step with the host's clock");
                    if (shots != null) yield return BoatShot(Skiff.At(0), "coop-paint-seen-by-rider", 1, 7, 2.6f); RigOn();
                }
                // ---- 3. the desk at 11:00, the same moment as the host; the shelves, one tx twice (a retry)
                yield return Until(() => DayTime.OfDay(Sky.Minutes) >= 11 * 60, 200);
                Works.Buy("furn:desk", 0);
                string tx = Firm.I.NewTx("m5"); CoopM5.Ask(0, tx, "furn:shelves", 0, false); CoopM5.Ask(0, tx, "furn:shelves", 0, false);
                yield return new WaitForSeconds(6f);
                M5(Co.Owned.Contains("furn:desk") && Lines("Walt's desk, mended") == 1, $"one desk, one line in the bank here ({Lines("Walt's desk, mended")})");
                // ---- 4. the Kestrel is over the limit: Ask the crew; the host says OK on their phone
                Works.Buy("boat:kestrel", 0);
                yield return Until(() => Co.Asks.Exists(a => a.Item == "boat:kestrel"), 15);
                Phone.I.Show(Phone.Scr.Thread, Firm.MyName); yield return null; yield return null;
                if (shots != null) yield return M5Shot("coop-ask-card-asker", 0.5f);
                M5(Co.Asks.Exists(a => a.Item == "boat:kestrel"), "my ask is in the crew's phone");
                yield return Until(() => Co.Owned.Contains("boat:kestrel"), 40);
                M5(Co.Owned.Contains("boat:kestrel"), "the host said OK: the Kestrel is ours");
                yield return new WaitForSeconds(1.5f);
                M5(W.Kestrel != null && W.KestrelBerth != null && Vector3.Distance(W.Kestrel.transform.position, W.KestrelBerth.position) < 3.5f, "and she's at the landing here");
                if (shots != null) yield return M5Shot("coop-ask-card-asker-ok", 0.3f);
                Phone.I.Hide();
                // ---- 5. the office: Not now
                Works.AskCrew("office:1", 0);   // under the limit: asked on purpose
                yield return Until(() => Co.Asks.Exists(a => a.Item == "office:1" && a.St == AskSt.Declined), 40);
                M5(Co.Asks.Exists(a => a.Item == "office:1" && a.St == AskSt.Declined), $"{hn} said not now to the office");
                // ---- 6. my sou'wester (the host sees it)
                Works.Buy("wear:souwester", 0);
                yield return Until(() => Co.OutfitOf(Firm.MyKey).Wear[0] == "wear:souwester", 15);
                yield return new WaitForSeconds(0.5f);
                M5(Wardrobe.HatOf(me.BodyGo) == "wear:souwester", "my sou'wester, on my head");
                yield return new WaitForSeconds(8f);
                Debug.Log($"[PB] RESULT m5coop rider: {(m5Fails == 0 ? "PASS" : m5Fails + " FAIL")}");
            }
            yield return new WaitForSeconds(2f);
            Application.Quit();
        }

        // ================================================================ the M5 film (-pb-film records it)
        IEnumerator M5Film()
        {
            Ctl.Bot = true; Sleep.Off = true; shots = null; var W = Works.I;
            Co.Earn(Firm.I.NewTx("tester"), Works.MyName, "Tester cash (film)", 30000, Sky.Minutes); Co.Act = 2; Firm.I.Dirty = true;
            var bis = Skiff.At(0); var bst = Co.BoatOf("biscuit");
            // 1. open on the payoff: Biscuit freshly painted at golden hour, the Kestrel behind (0-6 s)
            JumpTo(1, 17.2); bst.Paint = new[] { 9, 0, 22, 4 }; bst.Pattern = 5; yield return null;
            for (float t = 0; t < 6; t += Time.deltaTime) { var c = bis.transform.position + Vector3.up; float a = t * 0.12f; LookAt(c, c + Quaternion.Euler(0, 40 + a * 57, 0) * new Vector3(0, 2.6f, 8)); yield return null; }
            bst.Paint = new[] { -1, -1, -1, -1 }; bst.Pattern = 0; JumpTo(2, 10); RigOn();
            // 2. Doyle's counter, the shop: the hull swatches tried on her live (6-22 s)
            var doyle = W.Shops.Find(s => s.S == Store.Doyle); yield return AtShop(doyle);
            CameraRig.I.Yaw += 160; yield return new WaitForSeconds(2.5f); CameraRig.I.Yaw -= 160; yield return new WaitForSeconds(0.8f);
            YShop.Show(doyle); yield return new WaitForSeconds(1.5f); YShop.I.Tab("Paint"); yield return new WaitForSeconds(1f);
            YShop.I.Pick(Catalog.Get("paint:biscuit:0")); yield return new WaitForSeconds(1f);
            foreach (var sw in new[] { 0, 5, 9, 3 }) { YShop.I.Swatch(sw); yield return new WaitForSeconds(1.3f); }
            YShop.I.BuyNow("paint:biscuit:0", 3); yield return new WaitForSeconds(2.5f); YShop.I.Hide();
            // 3. Doyle's lads walk over and paint her, the brush line stern to bow (22-40 s, time sped up)
            var job = bst.Job; Time.timeScale = 3;
            for (float t = 0; job != null && t < 200 && Sky.Minutes < job.End; t += Time.deltaTime)
            {
                bool walking = Sky.Minutes < job.Start + job.Walk; var lad = W.Crew.Count > 0 ? W.Crew[0].transform.position : bis.transform.position;
                if (walking) LookAt(lad + Vector3.up, lad + new Vector3(4, 3, 4)); else { var c = bis.transform.position + Vector3.up * 0.8f; LookAt(c, c + bis.transform.right * 6 + bis.transform.forward * 3 + Vector3.up * 2.4f); }
                yield return null;
            }
            Time.timeScale = 1; yield return new WaitForSeconds(1.5f); RigOn();
            // 4. Plank & Post: a chair and the bunk pop in, then a night: the warehouse T1 timelapse at 07:00 (40-62 s)
            var plank = W.Shops.Find(s => s.S == Store.Plank); yield return AtShop(plank);
            YShop.Show(plank); yield return new WaitForSeconds(1.5f); YShop.I.Tab("Furniture"); yield return new WaitForSeconds(1.2f);
            YShop.I.BuyNow("furn:chair"); yield return new WaitForSeconds(2f); YShop.I.Tab("Storage"); yield return new WaitForSeconds(1.2f);
            YShop.I.BuyNow("wh:1"); yield return new WaitForSeconds(2f); YShop.I.Hide();
            var chair = W.ShownOf("furn:chair");
            if (chair != null) { Co.Owned.Remove("furn:chair"); yield return new WaitForSeconds(0.3f); var tc = chair.transform; var at = tc.position; Co.Owned.Add("furn:chair"); Co.Fresh.Add("furn:chair"); for (float t = 0; t < 2.5f; t += Time.deltaTime) { LookAt(at + Vector3.up * 0.5f, at + new Vector3(2, 1.6f, 2)); yield return null; } }
            JumpTo(Today + 1, 6.95); yield return new WaitForSeconds(0.2f);
            var wh = W.Warehouse != null ? W.Warehouse.position : me.transform.position;
            for (float t = 0; t < 7; t += Time.deltaTime) { LookAt(wh + Vector3.up * 2, wh + new Vector3(-9, 6, -9) + new Vector3(t * 0.3f, 0, 0)); yield return null; }
            RigOn(); JumpTo(Today, 10);
            // 5. Hartley's mirror and the wardrobe: hats tried on, the six crew (62-82 s)
            var har = W.Shops.Find(s => s.S == Store.Hartley); yield return AtShop(har);
            YShop.Show(har); yield return new WaitForSeconds(1.2f);
            foreach (var id in new[] { "wear:captain", "wear:souwester", "wear:straw", "wear:hawaiian" }) { YShop.I.Pick(Catalog.Get(id)); yield return new WaitForSeconds(1.4f); }
            YShop.I.BuyNow("wear:captain"); yield return new WaitForSeconds(2f); YShop.I.Hide();
            Wardrobe.Show(); yield return new WaitForSeconds(1.2f);
            for (int p = 1; p < 6; p++) { Wardrobe.I.Tap("crew " + Catalog.Preset[p]); yield return new WaitForSeconds(1.1f); }
            Wardrobe.I.Tap("crew Pip"); yield return new WaitForSeconds(1.2f); Wardrobe.I.Hide();
            // 6. out: the Kestrel bought and at the landing beside Biscuit (82-90 s)
            Works.HostBuy(Firm.I.NewTx("m5"), Works.MyKey, Works.MyName, "boat:kestrel", 0, false, out _); JumpTo(Today, 17.6);
            yield return new WaitForSeconds(0.8f);
            for (float t = 0; t < 7; t += Time.deltaTime) { var c = (bis.transform.position + (W.Kestrel != null ? W.Kestrel.transform.position : bis.transform.position)) / 2 + Vector3.up; LookAt(c, c + Quaternion.Euler(0, -30 + t * 6, 0) * new Vector3(0, 5, -16)); yield return null; }
            Debug.Log("[PB] RESULT m5film: done");
            Application.Quit();
        }
    }
}
