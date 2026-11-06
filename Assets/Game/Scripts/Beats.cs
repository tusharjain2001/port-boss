using System;
using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M7 · the first hour's text beats (gdd/world §9.1), the ones whose words and moment the script gives and whose
    // systems exist. The host's company sends each once per world (saved in "beats"); the company syncs, so every
    // player's phone gets them. A beat whose day has passed (a later save) never fires late.
    //   walt-biscuit  Biscuit's engine first runs under a hand        Walt: "Is that my Biscuit"
    //   walt-pratt    Day 1, 13:00                                    Walt: "Pratt'll lowball you. …"
    //   walt-lights   Day 1, 21:00                                    Walt: "lights out here. Dont stay up"
    //   fergus-hull   Day 2, 11:00, Biscuit's hull not patched yet   Fergus: "Hull's still weeping. $120" + a Pay card
    //   dot-rain      the first rain from Day 2                      Dot: "Some fish like the rain"
    //   june-bamboo   Day 2, 20:00, nobody past Walt's bamboo          June: "Not on Walt's bamboo, love. Fibreglass."
    //   kai-surf      from Day 2, a player within 300 m of Breaker Point   Kai: "come surf sometime"
    public partial class Company
    {
        public readonly HashSet<string> Beats = new();
        public const string Walt = "Grandpa Walt", JuneName = "June Pike", FergusName = "Fergus Doyle", DotName = "Dot", KaiName = "Kai Rennick";
        public const string HullOffer = "buy:rep:biscuit:hull";
        static readonly Vector3 BreakerPoint = new(-40, 0, -1450);   // the map's label (svg 2800, 4000)
        static float nearT;

        partial void BeatsTick(double m)
        {
            int day = DayTime.Day(m); double h = DayTime.OfDay(m) / 60.0;
            var biscuit = Skiff.Of("biscuit");
            Beat("walt-biscuit", biscuit != null && biscuit.AtHelm && biscuit.Notch != 0, () => Text(Walt, "Is that my Biscuit", m));
            Beat("walt-pratt", day == 1 && h >= 13, () => Text(Walt, "Pratt'll lowball you. Sell a few anyway, the engine won't fix itself.", m));
            Beat("walt-lights", day == 1 && h >= 21, () => Text(Walt, "lights out here. Dont stay up", m));
            Beat("fergus-hull", day == 2 && h >= 11 && !BoatOf("biscuit").Hull, () => Text(FergusName, "Hull's still weeping. $120", m, offer: HullOffer));
            Beat("dot-rain", day >= 2 && Raining(m), () => Text(DotName, "Some fish like the rain", m));
            Beat("june-bamboo", day == 2 && h >= 20 && OnBamboo(), () => Text(JuneName, "Not on Walt's bamboo, love. Fibreglass.", m));
            if (day >= 2 && !Beats.Contains("kai-surf") && (nearT -= Time.unscaledDeltaTime) <= 0)
            {
                nearT = 1;   // once a second: is anyone near Breaker Point?
                foreach (var a in UnityEngine.Object.FindObjectsByType<Angler>(FindObjectsSortMode.None))
                {
                    var d = a.transform.position - BreakerPoint; d.y = 0;
                    if (d.sqrMagnitude < 300 * 300) { Beat("kai-surf", true, () => Text(KaiName, "come surf sometime", m)); break; }
                }
            }
        }
        void Beat(string id, bool when, Action say) { if (when && Beats.Add(id)) say(); }
        bool OnBamboo() { foreach (var k in Kits.Values) if (k.Rod > 1) return false; return true; }   // nobody has a better rod yet
        static bool Raining(double m) { var (d, b) = Weather.BlockAt(m); var w = Sky.W.Block(d, b); return w == Wx.Rain || w == Wx.Storm; }

        partial void WriteM7(Json.W w) { var l = new List<string>(Beats); l.Sort(StringComparer.Ordinal); w.K("beats").Arr(); foreach (var b in l) w.V(b); w.End(); }
        partial void ReadM7(Dictionary<string, object> o) { if (o.TryGetValue("beats", out var bo) && bo is List<object> l) foreach (var b in l) if (b is string s) Beats.Add(s); }
    }
}
