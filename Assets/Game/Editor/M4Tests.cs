using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace PortBoss.M1
{
    // M4 EditMode suites (gdd systems §5.2): the contract and money rules, the fish ledger, the save migrations.
    // Unity: Test Runner (EditMode), or batch: -runTests -testPlatform EditMode (tools/m4test.sh). The same checks run
    // without Unity in tools/companycheck.
    public class M4Tests
    {
        [Test] public void MariContractRules() => Assert.AreEqual("", Company.MariTest());

        [TestCase(1), TestCase(7), TestCase(12345)]
        public void MoneyNeverNegativeLedgerSums(int seed) { var r = Company.SelfTest(20000, seed); StringAssert.StartsWith("PASS", r, r); }

        [TestCase(1), TestCase(3)]
        public void FishLedgerOnePlaceEach(int seed) { var r = Ledger.SelfTest(5000, seed); StringAssert.StartsWith("PASS", r, r); }

        [Test]
        public void SpendNeverGoesNegative()
        {
            var c = new Company();
            Assert.IsFalse(c.Spend("t1", "Pip", "too much", Company.StartCash + 1, 420));
            Assert.AreEqual(Company.StartCash, c.Cash);
            Assert.IsTrue(c.Spend("t2", "Pip", "worms", 5, 421));
            Assert.IsFalse(c.Spend("t2", "Pip", "worms again (replay)", 5, 422));
            Assert.AreEqual(Company.StartCash - 5, c.Cash);
            Assert.AreEqual("", c.Check());
        }

        [Test]
        public void PrattPaysSixtyMariPaysAll()
        {
            Assert.AreEqual(6, Market.Price(10, 0, Buyer.Pratt, 0));
            Assert.AreEqual(10, Market.Price(10, 0, Buyer.Mari, 0));
            Assert.AreEqual(4, Market.Price(10, 0, Buyer.Pratt, 30), "the 31st of a kind in a day: saturation at its 60% floor, $10 x 0.6 x 0.6 = $3.60 -> $4");
            Assert.AreEqual(5, Market.Price(10, 1, Buyer.Pratt, 0), "a Good (day-old) fish: x0.85");
        }

        static string Fixture(string name) => Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", name);

        [Test]
        public void SaveV1MigratesToV2()
        {
            var o = Save.Parse(File.ReadAllBytes(Fixture("save-v1.json")));
            Assert.AreEqual(Save.Schema, Json.I(o, "schema"));
            var c = Company.Read(Json.O(o, "company"));
            Assert.AreEqual(132, c.Cash);
            Assert.AreEqual("", c.Check(), "the migrated Ledger sums to the bank");
            Assert.AreEqual(4, c.KitOf("name:Manan").Bait[0]);
            var players = Json.O(o, "players"); Assert.IsTrue(players.ContainsKey("name:Manan"));
            Assert.AreEqual(2, Json.L((Dictionary<string, object>)players["name:Manan"], "bar").Count);
            // and it writes and reads back the same
            var a = Json.Write(o); Assert.AreEqual(a, Json.Write(Json.Parse(a)));
        }

        [Test]
        public void SaveRefusesANewerSchema()
        {
            var o = new Dictionary<string, object> { ["schema"] = (double)(Save.Schema + 1) };
            Assert.Throws<System.Exception>(() => Save.Migrate(o));
        }

        [Test]
        public void GzipAndCrcRoundTrip()
        {
            var c = new Company(); c.Earn("t", "Pip", "Sold 1 Mackerel · Pratt", 6, 500);
            var json = c.ToJson(); var gz = Save.Gzip(json);
            Assert.AreEqual(json, Save.Gunzip(gz));
            Assert.AreEqual(0xCBF43926u, Save.Crc32(System.Text.Encoding.ASCII.GetBytes("123456789")), "CRC-32 check value");
            Assert.AreEqual(json, Company.Read((Dictionary<string, object>)Json.Parse(json)).ToJson());
        }

        // batch: -executeMethod PortBoss.M1.M4Tests.RunAll (without the Test Runner)
        public static void RunAll()
        {
            int fails = 0; var t = new M4Tests();
            void Do(string n, System.Action a) { try { a(); UnityEngine.Debug.Log($"[PB] m4test: ok {n}"); } catch (System.Exception e) { fails++; UnityEngine.Debug.Log($"[PB] m4test: FAIL {n}: {e.Message}"); } }
            Do("MariContractRules", t.MariContractRules);
            foreach (var s in new[] { 1, 7, 12345 }) Do("MoneyNeverNegativeLedgerSums " + s, () => t.MoneyNeverNegativeLedgerSums(s));
            foreach (var s in new[] { 1, 3 }) Do("FishLedgerOnePlaceEach " + s, () => t.FishLedgerOnePlaceEach(s));
            Do("SpendNeverGoesNegative", t.SpendNeverGoesNegative);
            Do("PrattPaysSixtyMariPaysAll", t.PrattPaysSixtyMariPaysAll);
            Do("SaveV1MigratesToV2", t.SaveV1MigratesToV2);
            Do("SaveRefusesANewerSchema", t.SaveRefusesANewerSchema);
            Do("GzipAndCrcRoundTrip", t.GzipAndCrcRoundTrip);
            UnityEngine.Debug.Log($"[PB] RESULT m4test: {(fails == 0 ? "PASS" : fails + " FAIL")}");
            UnityEditor.EditorApplication.Exit(fails == 0 ? 0 : 1);
        }
    }
}
