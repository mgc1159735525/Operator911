using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class CityVerification
    {
        static int passed;
        static void Check(bool yes, string label) { if (!yes) throw new Exception("CITY_TEST_FAILED: " + label); passed++; Debug.Log("CITY_PASS: " + label); }
        static GameConfig Clone(GameConfig c) { return JsonUtility.FromJson<GameConfig>(JsonUtility.ToJson(c)); }
        static bool Reject(Action action) { try { action(); return false; } catch { return true; } }

        [MenuItem("Minglu/验证城市与配置")]
        public static void Verify()
        {
            passed = 0;
            PrototypeTools.Verify();
            var c = ConfigStore.Load();
            var xlsx = ConfigTableTools.Read("outputs/minglu-v03/明路地下_游戏配置.xlsx");
            var csv = ConfigTableTools.Read("outputs/minglu-v03/csv");
            Check(JsonUtility.ToJson(xlsx) == JsonUtility.ToJson(csv), "XLSX and CSV produce identical config");
            Check(JsonUtility.ToJson(c) == JsonUtility.ToJson(xlsx), "Imported runtime config matches workbook");
            var q = ConfigTableTools.ParseCsv("id,text\r\n1,\"comma,quote\"\"and\nnewline\"\r\n");
            Check(q[1][1] == "comma,quote\"and\nnewline", "CSV escaped quote, comma and embedded newline");
            Check(Reject(() => ConfigTableTools.ParseCsv("id\n\"open")), "Unclosed CSV rejected");
            var invalid = Clone(c); invalid.agents[1].id = invalid.agents[0].id;
            Check(Reject(() => ConfigStore.Validate(invalid)), "Duplicate agent ID rejected");
            invalid = Clone(c); invalid.agents[0].handler = 1;
            Check(Reject(() => ConfigStore.Validate(invalid)), "Handler cycle rejected");
            invalid = Clone(c); invalid.agents[0].handler = 99;
            Check(Reject(() => ConfigStore.Validate(invalid)), "Missing handler rejected");
            invalid = Clone(c); invalid.districts[0].x = float.NaN;
            Check(Reject(() => ConfigStore.Validate(invalid)), "NaN map coordinates rejected");
            invalid = Clone(c); invalid.events[0].intelB = -5;
            Check(Reject(() => ConfigStore.Validate(invalid)), "Event without affordable fallback rejected");
            invalid = Clone(c); invalid.Action(Duty.Rest).progressReward = 5;
            Check(Reject(() => ConfigStore.Validate(invalid)), "Unsupported recovery reward field rejected");
            var s = PrototypeRules.NewRun(true, 7, Clone(c), true);
            Check(PrototypeRules.Assign(s, 1, Duty.Rescue, 0, 5) != "", "Rescue cannot target another district");
            PrototypeRules.Resolve(s);
            Check(s.pendingEvent == "curfew", "Scheduled story event appears at turn two");
            string frozen = JsonUtility.ToJson(s); PrototypeRules.Resolve(s);
            Check(JsonUtility.ToJson(s) == frozen, "Pending event blocks turn advancement");
            Check(PrototypeRules.Assign(s, 1, Duty.Recon) != "", "Pending event blocks assignments");
            s.intelligence = 0;
            Check(PrototypeRules.ChooseEvent(s, true) != "", "Unaffordable event option rejected");
            Check(PrototypeRules.ChooseEvent(s, false) == "" && s.pendingEvent == "", "Free event option resolves");
            int pressure = s.pressure;
            Check(PrototypeRules.ChooseEvent(s, false) != "" && s.pressure == pressure, "Event effects cannot repeat");

            var plain = PrototypeRules.NewRun(false, 8, Clone(c)); var bonus = PrototypeRules.NewRun(false, 8, Clone(c));
            PrototypeRules.Assign(plain, 1, Duty.Recon, -1, 0); PrototypeRules.Assign(bonus, 1, Duty.Recon, -1, 1);
            PrototypeRules.Resolve(plain); PrototypeRules.Resolve(bonus);
            Check(bonus.agents[1].exposure - plain.agents[1].exposure == c.District(1).risk, "District risk affects actual exposure");
            Check(bonus.intelligence == plain.intelligence + c.District(1).reconBonus, "District bonus affects actual intel");

            string directory = Path.Combine(Path.GetTempPath(), "minglu-table-check-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                ConfigTableTools.Export(c, directory);
                Check(JsonUtility.ToJson(ConfigTableTools.Read(directory)) == JsonUtility.ToJson(c), "CSV export/reimport roundtrip");
                string settings = Path.Combine(directory, "Settings.csv"); File.AppendAllText(settings, "\"ap\",\"9\",\"duplicate\"\n");
                string before = File.ReadAllText(ConfigTableTools.Output);
                Check(Reject(() => { var candidate = ConfigTableTools.Read(directory); ConfigTableTools.Publish(candidate); }), "Bad import stops before publication");
                Check(before == File.ReadAllText(ConfigTableTools.Output), "Rejected import preserves runtime file");
                string save = Path.Combine(directory, "snapshot.json");
                s = PrototypeRules.NewRun(false, 9, Clone(c), true); PrototypeRules.Resolve(s); PrototypeSave.Write(s, save);
                var loaded = PrototypeSave.Read(save);
                c.settings.First(x => x.id == "goal").value = 99;
                Check(loaded.config.Value("goal") == 24 && loaded.pendingEvent == "curfew", "Save retains original config and pending event");
            }
            finally { foreach (string file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }

            c = ConfigStore.Load(); int wins = 0;
            for (uint seed = 1; seed <= 300; seed++)
            {
                s = PrototypeRules.NewRun(seed % 2 == 0, seed, Clone(c), true);
                for (int turn = 0; turn < c.Value("turns") && !s.ended; turn++)
                {
                    if (!string.IsNullOrEmpty(s.pendingEvent)) PrototypeRules.ChooseEvent(s, false);
                    foreach (var a in s.agents.Where(a => a.condition == Condition.Active))
                    {
                        if (a.exposure >= 45 || a.stamina < 50) continue;
                        if (a.handler >= 0 && s.agents.Any(p => p.id == a.handler && p.condition == Condition.Captured)) PrototypeRules.Assign(s, a.id, Duty.CutContact);
                        else PrototypeRules.Assign(s, a.id, PrototypeRules.ReservedIntel(s) < s.intelligence ? Duty.Sabotage : Duty.Recon, -1, (int)(seed % 6));
                    }
                    PrototypeRules.Resolve(s);
                    if (s.intelligence < 0 || s.agents.Any(a => a.exposure < 0 || a.exposure > 100 || a.stamina < 0 || a.stamina > 100)) throw new Exception("City invariant seed " + seed);
                }
                if (!s.ended) throw new Exception("City run did not end: " + seed);
                if (s.won) wins++;
            }
            Check(wins > 0 && wins < 300, "City simulation contains wins and losses");
            Debug.Log("MINGLU_CITY_VERIFY_SUCCESS: " + passed + " additional checks; 300 mixed city runs, " + wins + " fixed-policy wins, not player balance evidence.");
        }

        public static void BatchDeliver()
        {
            try
            {
                ConfigTableTools.BatchImport(); Verify(); DispatchVerification.Verify(); PrototypeTools.BatchBuild();
                Debug.Log("MINGLU_CITY_DELIVER_SUCCESS"); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
