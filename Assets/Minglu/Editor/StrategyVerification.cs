using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace Minglu.EditorTools
{
    public static class StrategyVerification
    {
        static int checks;
        static void Check(bool value,string reason){if(!value)throw new Exception("Strategy: "+reason);checks++;}
        static RunState Fresh(){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),911,1,0,false);CommandRules.Initialize(s);s.command.config.eventChance=0;return s;}
        static void Prepare(RunState s,int actor){var a=s.agents[actor];a.intel=a.stealth=a.persuade=a.organize=6;a.stamina=a.spirit=100;s.command.people[actor].leaveUntil=0;s.command.people[actor].policy=0;}
        public static void Verify()
        {
            checks=0;var s=Fresh();Check(s.command.strategy!=null,"New game strategy initialized");
            s.command.strategy=null;CommandRules.Validate(s);Check(s.command.strategy!=null&&s.command.people.All(p=>p.warningTurn==-1),"Old save gets warning grace");
            int money=s.money;Check(CommandRules.StartCivicTask(s,0)==""&&s.money==money,"Create is free");
            var t=s.command.tasks.Last();Check(t.stepsRequired==2&&t.actor==-1,"Two-stage delegation");Check(CommandRules.StartCivicTask(s,0)!="","Duplicate blocked");
            Prepare(s,3);CommandRules.Assign(s,3,t.id);CommandRules.Resolve(s);Check(t.stepsDone==1&&t.status==0&&t.actor==-1&&s.command.strategy.alliances[0]==0,"First step only and reauthorize");
            for(int n=0;n<4&&t.status==0;n++){Prepare(s,3);CommandRules.Assign(s,3,t.id);CommandRules.Resolve(s);}
            Check(t.status==1&&s.command.strategy.alliances[0]==25,"Final step grants relationship once");
            s=Fresh();s.turn=s.config.Value("turns");int count=s.command.tasks.Count;
            Check(CommandRules.StartCivicTask(s,0).Contains("至少两夜")&&s.command.tasks.Count==count,"Final-night alliance cannot create an impossible two-stage commitment");
            Check(CommandRules.StartCivicTask(s,-1)==""&&s.command.tasks.Last().stepsRequired==1,"Single-night community action remains available in finale");
            s=Fresh();s.command.strategy.alliances[2]=50;t=s.command.tasks[0];t.skill=0;Check(CommandRules.AllianceDice(s,t)==1,"Alliance affects displayed dice");
            s=Fresh();s.command.strategy.alliances[0]=50;money=s.money;CommandRules.Resolve(s);Check(s.money==money+4,"Recurring alliance income");
            s=Fresh();s.turn=4;s.agents[0].exposure=100;CommandRules.Resolve(s);Check(s.agents[0].condition==Condition.Active&&s.command.people[0].warningTurn==4,"No instant arrest before warning");
            CommandRules.Resolve(s);Check(s.agents[0].condition==Condition.Active,"Rest can avoid arrest");
            s=Fresh();s.turn=4;var r=s.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;r.atRelay=1;
            CommandRules.CaptureForCommand(s,0);Check(s.agents[0].condition==Condition.Captured&&!r.enabled&&r.atRelay==1,"Capture pauses only known line and keeps packets");
            var rescue=s.command.tasks.Last();Check(rescue.operation==2&&rescue.stepsRequired==2,"Rescue task exists");Check(CommandRules.Availability(s,0)!="","Prisoner cannot act");
            for(int n=0;n<4&&rescue.status==0;n++){Prepare(s,3);CommandRules.Assign(s,3,rescue.id);CommandRules.Resolve(s);}
            Check(rescue.status==1&&s.agents[0].condition==Condition.Active&&!r.enabled,"Rescue restores person without auto-enabling route");
            s=Fresh();s.turn=s.config.Value("turns")-1;CommandRules.CaptureForCommand(s,0);rescue=s.command.tasks.Last();s.turn++;Prepare(s,3);
            Check(rescue.operation==2&&rescue.stepsRequired==1&&CommandRules.Assign(s,3,rescue.id)=="","Penultimate-night capture offers a reachable final-night emergency rescue");
            s=Fresh();s.turn=s.config.Value("turns");count=s.command.tasks.Count;CommandRules.CaptureForCommand(s,0);
            Check(s.command.tasks.Count==count&&s.command.notices.Count==0,"Capture after the final action does not promise an unavailable future rescue");
            s=Fresh();s.turn=4;CommandRules.CaptureForCommand(s,0);s.command.people[0].loyalty=100;for(int n=0;n<5&&!s.ended;n++)CommandRules.Resolve(s);Check(s.agents[0].condition==Condition.Lost,"Missed rescue window is permanent");
            s=Fresh();s.turn=4;s.command.strategy.trace=75;CommandRules.Resolve(s);Check(s.command.eventKind==4,"Trace threshold triggers raid decision");
            s.command.routes[0].atRelay=3;money=s.money;CommandRules.ResolveRaid(s,true);Check(s.money==money-10&&s.command.routes[0].atRelay==3&&s.command.pauseUntil==s.turn,"Paid relocation preserves intel");
            money=s.money;Check(CommandRules.ResolveRaid(s,true)!=""&&s.money==money,"Resolved raid cannot charge twice");
            s=Fresh();s.command.eventKind=4;s.money=0;s.command.routes[0].waiting=2;s.command.routes[0].atRelay=3;Check(CommandRules.ResolveRaid(s,true)!="","Unaffordable relocation does not mutate");
            Check(CommandRules.ResolveRaid(s,false)==""&&s.command.routes[0].waiting==2&&s.command.routes[0].atRelay==0,"Free escape only destroys relay inventory");
            s=Fresh();t=s.command.tasks[0];t.deadline=s.turn;Check(CommandRules.NightWarnings(s).Contains(t.title),"Deadline warning names actual task");
            string file=Path.Combine(Application.temporaryCachePath,"minglu-strategy-check.json");CommandRules.StartCivicTask(s,2);PrototypeSave.Write(s,file);var loaded=PrototypeSave.Read(file);
            Check(JsonUtility.ToJson(s.command.strategy)==JsonUtility.ToJson(loaded.command.strategy),"Strategy save preserved");CommandRules.Resolve(s);CommandRules.Resolve(loaded);Check(JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command),"Reload preserves future");
            var legacy=Fresh();legacy.command.strategy=null;string oldJson=JsonUtility.ToJson(legacy).Replace("\"stepsRequired\":1,","");File.WriteAllText(file,oldJson);loaded=PrototypeSave.Read(file);Check(loaded.command.strategy!=null&&loaded.command.tasks.All(task=>task.stepsRequired==1),"Legacy JSON missing new fields migrates");
            File.Delete(file);if(File.Exists(file+".bak"))File.Delete(file+".bak");
            var risk=Fresh();risk.agents[0].exposure=95;CommandRules.SetPolicy(risk,0,1);Check(CommandRules.NightWarnings(risk).Contains("休养后仍达到捕获阈值"),"Rest warning preserves unresolved capture danger");Debug.Log("MINGLU_STRATEGY_VERIFY_SUCCESS: "+checks+" checks");
        }
        public static void BatchDeliver(){try{Verify();MissionVerification.Verify();CommandVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
