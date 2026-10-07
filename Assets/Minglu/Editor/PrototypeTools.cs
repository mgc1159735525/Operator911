using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class PrototypeTools
    {
        public const string ScenePath = "Assets/Minglu/Scenes/MingluPrototype.unity";

        [MenuItem("Minglu/打开八回合原型")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) CreateScene();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Minglu/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Prototype Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f, .065f, .075f);
            var view = new GameObject("Minglu Prototype").AddComponent<MingluPrototype>();
            view.chineseFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/simhei.ttf");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Minglu/验证核心规则")]
        public static void Verify()
        {
            int passed = 0;
            Action<bool, string> check = (condition, title) =>
            {
                if (!condition) throw new Exception("FAILED: " + title);
                passed++; Debug.Log("PASS: " + title);
            };
            var s = PrototypeRules.NewRun(false);
            check(PrototypeRules.Assign(s, 0, Duty.Sabotage) == "", "First intel reservation");
            check(PrototypeRules.Assign(s, 1, Duty.Sabotage) != "", "Cannot double spend intel");
            PrototypeRules.Assign(s, 0, Duty.Recon);
            PrototypeRules.Assign(s, 1, Duty.Recon);
            PrototypeRules.Assign(s, 2, Duty.Recon);
            check(PrototypeRules.Assign(s, 3, Duty.Recon) != "", "AP limit and rollback");
            check(s.agents[3].order == Duty.Rest && PrototypeRules.UsedAP(s) == 3, "Rejected order leaves state intact");

            s = PrototypeRules.NewRun(true);
            check(PrototypeRules.Assign(s, 0, Duty.Recon) != "", "Captured agent cannot act");
            check(PrototypeRules.Assign(s, 2, Duty.Rescue, 3) != "", "Only captured targets can be rescued");
            PrototypeRules.Resolve(s);
            check(s.agents[0].interrogation == 1 && s.agents[1].exposure == 0, "No early leak");
            PrototypeRules.Resolve(s);
            check(s.agents[1].exposure == 40 && s.agents[3].exposure == 0, "Leak confined to direct line");
            PrototypeRules.Resolve(s);
            check(s.agents[1].exposure == 25, "Leak applied only once");
            PrototypeRules.Resolve(s);
            check(s.agents[0].condition == Condition.Lost, "Prisoner lost on fourth interrogation");

            s = PrototypeRules.NewRun(true);
            PrototypeRules.Assign(s, 1, Duty.CutContact);
            PrototypeRules.Resolve(s); PrototypeRules.Resolve(s);
            check(s.agents[1].handler == -1 && s.agents[1].exposure == 0, "Transfer stops old address leak");

            // Find a known successful rescue seed without substituting the real resolver.
            bool rescued = false;
            for (uint seed = 1; seed < 100 && !rescued; seed++)
            {
                s = PrototypeRules.NewRun(true, seed); s.agents[0].interrogation = 1;
                PrototypeRules.Assign(s, 3, Duty.Rescue, 0); PrototypeRules.Resolve(s);
                if (s.agents[0].condition == Condition.Active)
                {
                    rescued = true;
                    check(s.agents[0].interrogation == 0 && s.agents[1].exposure == 0, "Rescue cancels same-turn interrogation");
                    check(s.agents[0].stamina == 50, "Rescued agent does not also rest this turn");
                }
            }
            check(rescued, "Successful rescue path reachable");

            s = PrototypeRules.NewRun(false); s.agents[0].exposure = 90;
            PrototypeRules.Resolve(s);
            check(s.agents[0].condition == Condition.Captured && s.agents[0].interrogation == 0, "New capture does not interrogate immediately");

            s = PrototypeRules.NewRun(false);
            for (int i = 0; i < 8; i++) PrototypeRules.Resolve(s);
            check(s.ended && !s.won && s.turn == 8, "Eight-turn defeat");
            string ended = JsonUtility.ToJson(s); PrototypeRules.Resolve(s);
            check(JsonUtility.ToJson(s) == ended, "Ended run cannot advance");
            s = PrototypeRules.NewRun(false); s.progress = PrototypeRules.Goal;
            PrototypeRules.Resolve(s); check(s.ended && s.won, "Goal victory");
            s = PrototypeRules.NewRun(true);
            foreach (var a in s.agents) a.condition = Condition.Captured;
            PrototypeRules.Resolve(s); check(s.ended && !s.won, "No active member defeat");

            string path = Path.Combine(Path.GetTempPath(), "minglu-test-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                s = PrototypeRules.NewRun(false, 124);
                PrototypeRules.Assign(s, 0, Duty.Recon); PrototypeRules.Assign(s, 1, Duty.Sabotage);
                PrototypeSave.Write(s, path);
                var loaded = PrototypeSave.Read(path);
                PrototypeRules.Resolve(s); PrototypeRules.Resolve(loaded);
                check(JsonUtility.ToJson(s) == JsonUtility.ToJson(loaded), "Save/load retains orders and exact random continuation");
                PrototypeSave.Write(s, path);
                check(File.Exists(path + ".bak") && PrototypeSave.Read(path).turn == s.turn, "Atomic replacement and backup");
                File.WriteAllText(path, "{}");
                string recoveryMessage;
                var recovered = PrototypeSave.ReadWithRecovery(path,out recoveryMessage);
                check(recoveryMessage.Contains("备份") && JsonUtility.ToJson(recovered)==JsonUtility.ToJson(PrototypeSave.Read(path+".bak")), "Malformed primary recovers its valid backup");
                check(File.ReadAllText(path)=="{}", "Recovery preserves the damaged primary for diagnosis");
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file);
            }
            // Exercise many complete runs, checking resource and time invariants.
            int victories = 0;
            for (uint seed = 1; seed <= 200; seed++)
            {
                s = PrototypeRules.NewRun(false, seed);
                for (int turn = 0; turn < 8 && !s.ended; turn++)
                {
                    foreach (var a in s.agents.Where(a => a.condition == Condition.Active))
                    {
                        if (a.exposure >= 45 || a.stamina < 50) continue;
                        PrototypeRules.Assign(s, a.id, PrototypeRules.ReservedIntel(s) < s.intelligence ? Duty.Sabotage : Duty.Recon);
                    }
                    PrototypeRules.Resolve(s);
                    if (s.intelligence < 0 || s.agents.Any(a => a.stamina < 0 || a.stamina > 100 || a.exposure < 0 || a.exposure > 100))
                        throw new Exception("Simulation invariant failure, seed " + seed);
                }
                if (!s.ended) throw new Exception("Run did not end, seed " + seed);
                if (s.won) victories++;
            }
            check(victories > 0 && victories < 200, "Simulation includes reachable wins and losses");
            Debug.Log("MINGLU_VERIFY_SUCCESS: " + passed + " checks; 200 simulated runs, " + victories + " policy wins. Not a player win-rate estimate.");
        }

        public static void BatchVerify()
        {
            try
            {
                Verify();
                if (!File.Exists(ScenePath)) CreateScene();
                Debug.Log("MINGLU_BATCH_SUCCESS");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        [MenuItem("Minglu/构建Windows试玩版")]
        public static void BatchBuild()
        {
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/MingluCity/MingluCity.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Prototype player build failed: " + report.summary.result);
            Debug.Log("MINGLU_BUILD_SUCCESS");
        }
    }
}



