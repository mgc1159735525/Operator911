using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class InstitutionVerification
    {
        static int checks;
        static void Check(bool value,string reason){if(!value)throw new Exception("Institution: "+reason);checks++;}
        static RunState Fresh(uint seed=911){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),seed,1,0,false);CommandRules.Initialize(s);s.command.config.eventChance=0;return s;}
        public static void Verify()
        {
            checks=0;
            for(uint seed=1;seed<=60;seed++)
            {var run=Fresh(seed);Check(CommandRules.HasInstitution(run.command.people[0])&&run.intelligence==0,"Every start has an insider and no free intelligence");}
            var s=Fresh();var r=s.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;
            foreach(var p in s.command.people)p.institution=0;
            r.waiting=1;r.atRelay=1;
            Check(CommandRules.RouteError(s,r).Contains("敌方机构"),"Civilian cannot be an intelligence source");
            CommandRules.Resolve(s);Check(r.waiting==1&&r.atRelay==1&&s.intelligence==0&&s.command.delivered==0,"Invalid source blocks production and delivery even if a route is forced on");
            s=Fresh();s.command.tasks.Clear();s.command.people[0].institution=1;
            foreach(var p in s.command.people)p.policy=2;
            for(int n=0;n<3;n++)CommandRules.Resolve(s);
            Check(s.intelligence==0&&s.command.routes.All(x=>x.waiting+x.atRelay==0),"Daily work and unsolicited civilian proposals do not create intelligence");
            s=Fresh();s.command.tasks.Clear();CommandRules.CreateTask(s,new CommandTemplate{title="普通任务",body="故意注入旧奖励",intelReward=5},"检查",-1,s.config.districts[0].id);
            var t=s.command.tasks.Last();Check(t.intelReward==0&&!CommandRules.TaskReward(t).Contains("情报+"),"Authoring legacy reward cannot promise direct intelligence");
            t.intelReward=5;s.agents[0].intel=30;CommandRules.Assign(s,0,t.id);CommandRules.Resolve(s);
            Check(t.status==1&&s.intelligence==0,"Even an injected legacy reward cannot bypass delivery");
            s=Fresh();s.command.people[3].institution=0;int count=s.command.tasks.Count,money=s.money;
            Check(CommandRules.StartEmployment(s,3,3)==""&&s.money==money,"Creating employment task consumes no resources");
            t=s.command.tasks.Last();Check(!CommandRules.HasInstitution(s.command.people[3])&&t.actor==-1,"Creation alone grants no access");
            Check(CommandRules.StartEmployment(s,3,1)!=""&&s.command.tasks.Count==count+1,"Duplicate applications are blocked");
            Check(CommandRules.Assign(s,0,t.id)!="","A different member cannot earn the applicant's job");
            s.agents[3].persuade=30;Check(CommandRules.Assign(s,3,t.id)=="","Applicant can be authorized without intelligence");
            string file=Path.Combine(Application.temporaryCachePath,"minglu-institution-check.json");
            PrototypeSave.Write(s,file);var loaded=PrototypeSave.Read(file);
            CommandRules.Resolve(s);CommandRules.Resolve(loaded);
            Check(t.status==1&&s.command.people[3].institution==3&&s.intelligence==0,"Successful employment grants access only");
            Check(JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command),"Employment assignment and deterministic future survive reload");
            Check(CommandRules.StartEmployment(s,3,1)!="","Existing insider cannot instantly change employer");
            s=Fresh();s.turn=s.config.Value("turns")-2;s.command.people[3].institution=0;
            Check(CommandRules.StartEmployment(s,3,1)!="","No futile application when delivery window is gone");
            s=Fresh();s.command.tasks.Clear();r=s.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;
            s.command.people[1].institution=s.command.people[2].institution=0;s.turn=2;
            foreach(var a in s.agents)a.intel=a.persuade=a.stealth=30;
            CommandRules.Resolve(s);Check(r.waiting==1&&r.atRelay==0&&s.intelligence==0,"Insider produces pending information, not inventory");
            CommandRules.Resolve(s);Check(r.waiting==0&&r.atRelay==1&&s.intelligence==0,"Civilian liaison moves it one hop");
            CommandRules.Resolve(s);Check(r.delivered==1&&s.intelligence==1,"Civilian courier alone makes delivered intelligence usable");
            s=Fresh();s.command.institutionRevision=0;foreach(var p in s.command.people)p.institution=0;r=s.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;r.waiting=2;r.atRelay=1;s.intelligence=4;s.command.delivered=3;s.command.tasks[0].intelReward=2;
            uint rng=s.random;CommandRules.Validate(s);var json=JsonUtility.ToJson(s.command);
            Check(!r.enabled&&r.waiting+r.atRelay==0&&s.intelligence==4&&s.command.delivered==3,"Migration clears unverified pending packets, preserves delivered history");
            CommandRules.Validate(s);Check(json==JsonUtility.ToJson(s.command)&&s.random==rng,"Migration is idempotent and consumes no random draws");
            Check(CommandRules.StartEmployment(s,0,1)=="","Legacy team can recover a valid source via employment");
            File.Delete(file);if(File.Exists(file+".bak"))File.Delete(file+".bak");
            Debug.Log("MINGLU_INSTITUTION_VERIFY_SUCCESS: "+checks+" checks");
        }
        public static void BatchDeliver(){try{Verify();StrategyVerification.Verify();MissionVerification.Verify();CommandVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
