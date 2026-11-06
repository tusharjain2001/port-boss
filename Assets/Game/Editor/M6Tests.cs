using NUnit.Framework;

namespace PortBoss.M1
{
    // M6 EditMode suite (gdd systems "M6", economy §4.5): Rosie's board, hiring, the 07:00 wages, the IOU rule (one day on
    // trust, then they quit; rehire costs double for 7 days), one boat per worker, the save round trip, and 4,000 seeded
    // shifts against the economy model. Batch: -executeMethod PortBoss.M1.M6Tests.RunAll (tools/m6test.sh). The same
    // checks run without Unity in tools/companycheck.
    public class M6Tests
    {
        [Test] public void WagesIouAndHiringRules() => Assert.AreEqual("", Company.CrewTest());

        [Test]
        public void ShiftModelMatchesTheTable()
        {
            // economy §4.5: a skill-3 deckhand on Biscuit, a day shift, no engine upgrade: ~19 fish
            var w = new Worker { Fishing = 3, Sea = 3, Quirk = "" };
            Assert.AreEqual(19.25, Company.Expected(w, "biscuit", false), 0.05);
        }

        public static void RunAll()
        {
            int fails = 0; var t = new M6Tests();
            void Do(string n, System.Action a) { try { a(); UnityEngine.Debug.Log($"[PB] m6test: ok {n}"); } catch (System.Exception e) { fails++; UnityEngine.Debug.Log($"[PB] m6test: FAIL {n}: {e.Message}"); } }
            Do("WagesIouAndHiringRules", t.WagesIouAndHiringRules);
            Do("ShiftModelMatchesTheTable", t.ShiftModelMatchesTheTable);
            UnityEngine.Debug.Log($"[PB] RESULT m6edit: {(fails == 0 ? "PASS" : fails + " FAIL")}");
            UnityEditor.EditorApplication.Exit(fails == 0 ? 0 : 1);
        }
    }
}
