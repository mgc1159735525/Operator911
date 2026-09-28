using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class MissionVerification
    {
        static int checks;
        static void Check(bool ok,string label){if(!ok)throw new Exception("Mission: "+label);checks++;}
        static RunState Fresh(){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),911,1,0,false);CommandRules.Initialize(s);s.command.config.eventChance=0;return s;}
        public static void Verify()
        {
            checks=0;var s=Fresh();var t=s.command.tasks.First(x=>x.level==1);var a=s.agents[0];
            int baseDice=CommandRules.TaskDice(s,a,t);t.plan=1;Check(CommandRules.TaskDice(s,a,t)==baseDice+1,"Preparation contributes one die");
            t.intelSupport=true;s.intelligence=1;s.money=4;Check(CommandRules.Assign(s,a.id,t.id)=="","Reserve affordable plan");
            Check(CommandRules.ReservedMoney(s)==4&&CommandRules.ReservedIntel(s)==1&&s.money==4&&s.intelligence==1,"Reservation does not consume inventory");
            var other=s.command.tasks.First(x=>x.id!=t.id);other.plan=1;other.intelSupport=true;
            Check(CommandRules.TaskResourceError(s,other)!="","No resource double booking");
            t.actor=-1;Check(CommandRules.TaskResourceError(s,other)=="","Cancel releases resources");
            s=Fresh();t=s.command.tasks.First(x=>x.level==1);a=s.agents[0];t.required=6;t.plan=1;t.intelSupport=true;s.intelligence=1;s.money=20;a.intel=a.stealth=a.persuade=a.organize=1;
            Check(CommandRules.Assign(s,a.id,t.id)!="","Zero probability is blocked before spending");
            t.required=1;a.intel=a.stealth=a.persuade=a.organize=6;Check(CommandRules.Assign(s,a.id,t.id)=="","Assisted task authorizes");
            int expectedDice=CommandRules.TaskDice(s,a,t);CommandRules.Resolve(s);
            Check(t.lastResult.Contains(expectedDice+"骰"),"Displayed dice used for execution");Check(t.actor==-1,"Execution clears assignment");
            Check(s.intelligence==(t.status==1?t.intelReward:0),"Support charged once with success reward explicit");
            s=Fresh();var parent=s.command.tasks[0];parent.followup="应对临检调动";parent.status=1;parent.followupSpawned=false;int district=parent.district;
            CommandRules.Resolve(s);var child=s.command.tasks.Single(x=>x.title==parent.followup);Check(child.district==district&&child.actor==-1,"Followup inherits district and needs fresh delegation");
            string file=Path.Combine(Application.temporaryCachePath,"minglu-mission-check.json");PrototypeSave.Write(s,file);var loaded=PrototypeSave.Read(file);
            CommandRules.Resolve(loaded);Check(loaded.command.tasks.Count(x=>x.title==parent.followup)==1,"Reload does not duplicate followup");
            s=Fresh();t=s.command.tasks[0];t.plan=1;t.intelSupport=true;s.intelligence=3;CommandRules.Assign(s,0,t.id);PrototypeSave.Write(s,file);loaded=PrototypeSave.Read(file);
            Check(CommandRules.ReservedMoney(loaded)==4&&CommandRules.ReservedIntel(loaded)==1,"Reservations survive reload");
            CommandRules.Resolve(s);CommandRules.Resolve(loaded);Check(s.random==loaded.random&&JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command),"Mission save preserves deterministic future");
            s=Fresh();s.command.missionRevision=0;s.command.tasks[0].plan=0;s.command.tasks[0].intelSupport=false;CommandRules.Validate(s);Check(s.command.missionRevision==1&&s.command.config.templates.Count==14,"Old mission config migrates");
            var config=JsonUtility.FromJson<CommandConfig>(Resources.Load<TextAsset>("Minglu/Config/command_config").text);config.templates[0].followup=config.templates[0].title;bool rejected=false;try{CommandRules.ValidateConfig(config);}catch{rejected=true;}Check(rejected,"Cyclic followups rejected");
            File.Delete(file);if(File.Exists(file+".bak"))File.Delete(file+".bak");Debug.Log("MINGLU_MISSION_VERIFY_SUCCESS: "+checks+" mission checks");
        }
        public static void BatchDeliver(){try{Verify();CommandVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
