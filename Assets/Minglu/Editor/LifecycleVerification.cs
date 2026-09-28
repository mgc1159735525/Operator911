using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class LifecycleVerification
    {
        static int passed;
        static void Check(bool condition,string title){if(!condition)throw new Exception("LIFECYCLE_FAIL: "+title);passed++;Debug.Log("LIFECYCLE_PASS: "+title);}
        public static void Verify()
        {
            passed=0;var c=ConfigStore.Load();string original=JsonUtility.ToJson(c);
            var s=RunLifecycle.Create(c,911,1,0,false);var t=RunLifecycle.Create(c,911,1,0,false);
            Check(JsonUtility.ToJson(s.config)==JsonUtility.ToJson(t.config)&&s.random==t.random,"Seeded initialization deterministic");
            Check(s.runId!=t.runId,"Same-seed attempts have distinct identities");
            Check(original==JsonUtility.ToJson(c),"Run initialization preserves source config");
            Check(s.agents.All(a=>a.condition==Condition.Active&&a.stamina==100&&a.exposure==0&&a.order==Duty.Rest&&a.consecutiveShifts==0)&&s.turn==1&&s.progress==0&&s.completedMissions.Count==0,"New run clears all per-run progress");
            var scout=RunLifecycle.Create(c,912,0,1,false);
            Check(scout.config.Value("goal")==20&&scout.intelligence==3&&scout.agents.All(a=>a.exposure==10),"Easy and intel doctrine initialized");
            var hidden=RunLifecycle.Create(c,913,2,2,true);
            Check(hidden.config.Value("goal")==32&&hidden.pressure==12&&hidden.agents[0].condition==Condition.Captured&&hidden.agents.Where(a=>a.condition==Condition.Active).All(a=>a.stamina==85),"Hard covert crisis initialization");
            Check(!s.config.districts.Select(d=>d.risk).SequenceEqual(scout.config.districts.Select(d=>d.risk)),"Different seeds vary world risks");
            var p=new PlayerProfile();Check(!RunLifecycle.Record(p,s),"Unfinished run never archived");s.ended=true;s.won=false;
            Check(RunLifecycle.Record(p,s)&&p.records.Count==1,"Finished run archived");
            Check(!RunLifecycle.Record(p,s)&&p.records.Count==1,"Ending archive is idempotent");
            string path=Path.Combine(Path.GetTempPath(),"minglu-lifecycle-"+Guid.NewGuid().ToString("N"));
            try
            {
                p.volume=.3f;p.sound=false;RunLifecycle.WriteProfile(p,path);var loaded=RunLifecycle.ReadProfile(path);
                Check(loaded.records.Count==1&&!loaded.sound&&loaded.volume==.3f,"Settings and archive roundtrip");
                PrototypeSave.Write(t,path+".run");var run=PrototypeSave.Read(path+".run");
                Check(run.runId==t.runId&&run.initialSeed==911&&run.roguelikeRun,"Saved run initialization retained");
                PrototypeRules.Assign(t,1,Duty.Recon);PrototypeRules.Assign(run,1,Duty.Recon);PrototypeRules.Resolve(t);PrototypeRules.Resolve(run);
                Check(JsonUtility.ToJson(t)==JsonUtility.ToJson(run),"Continued run retains exact outcome");
                s=RunLifecycle.Create(c,911,1,0,false);s.ended=true;RunLifecycle.Record(p,s);
                Check(p.records.Count==2,"Separate attempts on same seed both archived");
            }
            finally{foreach(var file in new[]{path,path+".bak",path+".tmp",path+".run"})if(File.Exists(file))File.Delete(file);}
            for(uint seed=1;seed<=30;seed++)for(int difficulty=0;difficulty<3;difficulty++)for(int doctrine=0;doctrine<3;doctrine++)ConfigStore.Validate(RunLifecycle.Create(c,seed,difficulty,doctrine,seed%2==0).config);
            Check(true,"270 seed difficulty doctrine combinations validate");
            Debug.Log("MINGLU_LIFECYCLE_SUCCESS: "+passed+" checks; 270 initialization combinations.");
        }
        public static void BatchDeliver()
        {
            try{Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
