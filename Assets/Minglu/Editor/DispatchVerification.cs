using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class DispatchVerification
    {
        static int passed;
        static void Check(bool condition, string label) { if (!condition) throw new Exception("DISPATCH_FAIL: " + label); passed++; Debug.Log("DISPATCH_PASS: " + label); }
        static RunState Fresh(uint seed=7) { return PrototypeRules.NewRun(false, seed, ConfigStore.Load(), false, true); }
        static bool Reject(Action action) { try { action(); return false; } catch { return true; } }
        [MenuItem("Minglu/验证限时派遣")]
        public static void Verify()
        {
            passed=0;
            var s=Fresh();
            Check(DispatchRules.Open(s).Select(m=>m.id).SequenceEqual(new[]{"cargo","ledger"}),"Deadline queue ordering");
            Check(DispatchRules.Assign(s,1,"trail")!="","Locked follow-up rejected");
            Check(DispatchRules.Assign(s,0,"cargo")=="","Task assigned with ordinary resource reservation");
            string before=JsonUtility.ToJson(s);
            Check(DispatchRules.Assign(s,1,"cargo")!=""&&before==JsonUtility.ToJson(s),"Duplicate assignment atomic rejection");
            Check(DispatchRules.Verify(s,"ledger")!=""&&s.intelligence==1,"Verification cannot spend reserved intel");
            PrototypeRules.Assign(s,0,Duty.Rest);
            Check(s.agents[0].mission==""&&PrototypeRules.ReservedIntel(s)==0,"Cancel releases task and intel");
            Check(DispatchRules.Verify(s,"ledger")==""&&s.intelligence==0,"Verification spends one intel");
            Check(DispatchRules.Verify(s,"ledger")!=""&&s.intelligence==0,"Verification not repeatable");
            Check(DispatchRules.Dice(s,s.agents[1],Duty.Recon,"ledger")==5,"Verified task adds one die");
            Check(Math.Abs(DispatchRules.Probability(4,2)-.6875)<.000001&&DispatchRules.Probability(1,2)==0,"Exact probability agrees with binomial result");
            Check(DispatchRules.Recommend(s,s.config.missions.First(m=>m.id=="ledger")).First().id==1,"Specialist recommendation");
            DispatchRules.Assign(s,1,"ledger");
            Check(DispatchRules.Recommend(s,s.config.missions.First(m=>m.id=="ledger")).All(a=>a.id!=1),"Recommendation never replaces occupied staff");
            s=Fresh();PrototypeRules.Resolve(s);
            Check(s.expiredMissions.Contains("cargo")&&s.pressure==23,"Deadline penalty applied before patrol");
            Check(DispatchRules.Assign(s,0,"cargo")!="","Expired mission cannot be assigned");
            PrototypeRules.Resolve(s);
            Check(s.expiredMissions.Count==2&&s.pressure==32,"Second deadline penalty and no duplicate expiry");
            s=Fresh();var agent=s.agents[1];agent.consecutiveShifts=2;
            Check(PrototypeRules.Dice(agent,Duty.Recon)==3,"Third consecutive shift loses a die");
            PrototypeRules.Resolve(s);Check(agent.consecutiveShifts==0&&PrototypeRules.Dice(agent,Duty.Recon)==4,"Full rest clears consecutive shift penalty");
            s=Fresh();s.agents[1].stamina=35;s.agents[1].exposure=60;s.agents[1].consecutiveShifts=2;
            Check(PrototypeRules.Dice(s.agents[1],Duty.Recon)==1,"Stacked penalties never reduce dice below one");
            s=Fresh();s.config.Action(Duty.Recon).required=0;DispatchRules.Assign(s,1,"ledger");PrototypeRules.Resolve(s);
            Check(s.completedMissions.Contains("ledger")&&s.intelligence==5&&s.progress==3,"Successful task applies normal and bonus rewards once");
            PrototypeRules.Resolve(s);
            Check(DispatchRules.Open(s).Any(m=>m.id=="trail"),"Completed lead unlocks timed follow-up");
            s=Fresh();s.config.Action(Duty.Sabotage).required=8;DispatchRules.Assign(s,0,"cargo");PrototypeRules.Resolve(s);
            Check(!s.completedMissions.Contains("cargo")&&s.expiredMissions.Contains("cargo")&&s.intelligence==0,"Failure consumes resources and expires on deadline");
            s=Fresh();s.config.Action(Duty.Recon).required=8;DispatchRules.Assign(s,1,"ledger");PrototypeRules.Resolve(s);
            Check(DispatchRules.Open(s).Any(m=>m.id=="ledger")&&DispatchRules.Assign(s,1,"ledger")=="","Failure can retry before inclusive deadline");
            s=Fresh();s.pendingEvent="curfew";before=JsonUtility.ToJson(s);
            Check(DispatchRules.Verify(s,"ledger")!=""&&DispatchRules.Assign(s,1,"ledger")!=""&&before==JsonUtility.ToJson(s),"Story blocks dispatch mutations");
            s=Fresh();s.config.missions[0].prerequisite="trail";
            Check(Reject(()=>ConfigStore.Validate(s.config)),"Cyclic or later prerequisite rejected");
            s=Fresh();s.config.missions[0].district=99;
            Check(Reject(()=>ConfigStore.Validate(s.config)),"Unknown district rejected");
            Check(ConfigTableTools.Read("outputs/minglu-v02/明路地下_游戏配置.xlsx").missions.Count==0,"Old six-sheet workbook remains importable");
            string save=Path.Combine(Path.GetTempPath(),"dispatch-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                s=Fresh();DispatchRules.Verify(s,"ledger");DispatchRules.Assign(s,1,"ledger");s.agents[1].consecutiveShifts=2;
                PrototypeSave.Write(s,save);var loaded=PrototypeSave.Read(save);
                Check(JsonUtility.ToJson(s)==JsonUtility.ToJson(loaded),"Save retains task orders verification and fatigue");
                PrototypeRules.Resolve(s);PrototypeRules.Resolve(loaded);
                Check(JsonUtility.ToJson(s)==JsonUtility.ToJson(loaded),"Loaded task resolves with identical random sequence");
                loaded.completedMissions.Add("unknown");PrototypeSave.Write(loaded,save);
                Check(Reject(()=>PrototypeSave.Read(save)),"Invalid saved task reference rejected");
            }
            finally { foreach(var p in new[]{save,save+".bak",save+".tmp"}) if(File.Exists(p)) File.Delete(p); }
            int wins=0,done=0;
            for(uint seed=1;seed<=300;seed++)
            {
                s=PrototypeRules.NewRun(seed%2==0,seed,ConfigStore.Load(),true,true);
                while(!s.ended)
                {
                    if(!string.IsNullOrEmpty(s.pendingEvent))PrototypeRules.ChooseEvent(s,false);
                    if(s.ended)break;
                    foreach(var m in DispatchRules.Open(s).ToList())
                    {
                        var a=DispatchRules.Recommend(s,m).FirstOrDefault(x=>x.exposure<45&&x.stamina>=50&&x.consecutiveShifts<2);
                        if(a!=null)DispatchRules.Assign(s,a.id,m.id);
                    }
                    foreach(var a in s.agents.Where(a=>a.condition==Condition.Active&&a.order==Duty.Rest&&a.exposure<40&&a.stamina>=50&&a.consecutiveShifts<2))
                        PrototypeRules.Assign(s,a.id,s.intelligence>PrototypeRules.ReservedIntel(s)?Duty.Sabotage:Duty.Recon);
                    PrototypeRules.Resolve(s);
                    DispatchRules.ValidateSave(s);
                    if(s.intelligence<0||s.pressure>100||s.agents.Any(a=>a.stamina<0||a.exposure>100)||s.turn>8)throw new Exception("Dispatch simulation invariant "+seed);
                }
                if(s.won)wins++;done+=s.completedMissions.Count;
            }
            Check(done>0&&wins>0&&wins<300,"New-system simulations include completion wins and losses");
            Debug.Log("MINGLU_DISPATCH_VERIFY_SUCCESS: "+passed+" checks; 300 runs; "+wins+" policy wins; "+done+" completed missions. Not player win-rate evidence.");
        }
    }
}
