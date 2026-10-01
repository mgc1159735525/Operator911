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
        static void VerifyRecon()
        {
            var s=Fresh();var t=s.command.tasks[0];var a=s.agents[0];
            t.level=4;t.profession="药师";t.required=6;t.skill=1;t.deadline=8;t.reconnaissance=true;
            a.intel=6;a.stealth=1;s.random=1;
            Check(CommandRules.ActionSkill(t)==0&&CommandRules.ActionRequired(t)==1,"Recon uses intelligence and one success");
            Check(CommandRules.Assign(s,a.id,t.id)=="","Analyst may investigate without target execution identity");
            int progress=s.progress,completed=s.command.completed,intel=s.intelligence,money=s.money;
            t.plan=1;
            var saved=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));CommandRules.Validate(saved);
            Check(saved.command.tasks[0].reconnaissance&&CommandRules.ReservedMoney(saved)==4,"Recon order and reservation survive reload");
            CommandRules.Resolve(s);CommandRules.Resolve(saved);
            Check(t.reconComplete&&!t.reconnaissance&&t.actor==-1,"Recon result returns target to delegation");
            Check(t.status==0&&t.stepsDone==0&&s.progress==progress&&s.command.completed==completed&&s.intelligence==intel,"Recon cannot complete stages, grant rewards or bypass intel chain");
            Check(s.money==money-4&&a.stamina==100-CommandRules.ActionStaminaCost(s,a),"Recon consumes one worker night and its budget once");
            Check(JsonUtility.ToJson(s.command)==JsonUtility.ToJson(saved.command)&&s.random==saved.random,"Recon save preserves deterministic execution");
            t.level=1;t.profession="";t.required=1;t.plan=0;
            var other=s.agents[1];int prepared=CommandRules.TaskDice(s,other,t),exposure=CommandRules.TaskExposure(s,other,t);
            t.reconComplete=false;
            Check(prepared==CommandRules.TaskDice(s,other,t)+1&&exposure==Math.Max(1,CommandRules.TaskExposure(s,other,t)-3),"Target preparation transfers to another executor with matching forecast");
            t.reconComplete=true;t.reconnaissance=true;
            Check(CommandRules.Assign(s,other.id,t.id).Contains("已调查"),"Cannot stack completed reconnaissance");
            t.reconComplete=false;t.deadline=s.turn;
            Check(CommandRules.Assign(s,other.id,t.id).Contains("剩余夜数"),"Recon cannot consume last executable night");
            s=Fresh();t=s.command.tasks[0];t.reconnaissance=true;t.deadline=8;t.plan=0;t.intelSupport=false;a=s.agents[0];a.intel=1;s.random=2;
            // Seed 2 yields a failed first d6; further random draws are irrelevant.
            Check(CommandRules.Assign(s,a.id,t.id)=="","Failed recon fixture authorizes");
            CommandRules.Resolve(s);
            Check(!t.reconComplete&&!t.reconnaissance&&t.actor==-1&&t.status==0&&t.lastResult.Contains("调查未成"),"Failed investigation gives no bonus and remains retryable");
        }
        public static void Verify()
        {
            checks=0;var s=Fresh();var t=s.command.tasks.First(x=>x.level==1);var a=s.agents[0];
            int baseDice=CommandRules.TaskDice(s,a,t);t.plan=1;Check(CommandRules.TaskDice(s,a,t)==baseDice+1,"Preparation contributes one die");
            t.intelSupport=true;s.intelligence=1;s.money=4;Check(CommandRules.Assign(s,a.id,t.id)=="","Reserve affordable plan");
            Check(CommandRules.ReservedMoney(s)==4&&CommandRules.ReservedIntel(s)==1&&s.money==4&&s.intelligence==1,"Reservation does not consume inventory");
            var other=s.command.tasks.First(x=>x.id!=t.id);other.plan=1;other.intelSupport=true;
            Check(CommandRules.TaskResourceError(s,other)!="","No resource double booking");
            t.actor=-1;Check(CommandRules.TaskResourceError(s,other)=="","Cancel releases resources");
            s=Fresh();t=s.command.tasks[0];t.plan=1;t.intelSupport=true;s.intelligence=2;CommandRules.Assign(s,0,t.id);CommandRules.SetPolicy(s,0,1);
            Check(t.actor==-1&&CommandRules.ReservedMoney(s)==0&&CommandRules.ReservedIntel(s)==0,"Life-priority policy releases orders and both budgets immediately");
            s=Fresh();t=s.command.tasks[0];t.plan=1;t.intelSupport=true;s.intelligence=2;CommandRules.Assign(s,0,t.id);s.command.eventKind=1;s.command.eventActor=0;CommandRules.Choose(s,true);
            Check(t.actor==-1&&CommandRules.ReservedMoney(s)==0&&CommandRules.ReservedIntel(s)==0,"Family leave event releases reservations before returning to planning");
            s=Fresh();t=s.command.tasks[0];t.plan=1;t.intelSupport=true;s.intelligence=2;CommandRules.Assign(s,0,t.id);s.command.people[0].loyalty=0;s.command.eventActor=0;
            for(int i=0;i<30&&s.agents[0].condition==Condition.Active;i++){s.command.eventKind=3;CommandRules.Choose(s,false);}
            Check(s.agents[0].condition==Condition.Lost&&t.actor==-1&&CommandRules.ReservedMoney(s)==0&&CommandRules.ReservedIntel(s)==0,"Betrayal releases stranded reservations immediately");
            s=Fresh();t=s.command.tasks[0];t.plan=1;t.required=1;t.actor=0;s.agents[0].intel=s.agents[0].stealth=s.agents[0].persuade=s.agents[0].organize=6;
            other=s.command.tasks[1];other.plan=1;other.actor=1;s.agents[1].stamina=0;s.money=4;CommandRules.Resolve(s);
            Check(t.lastResult.Contains("骰")&&other.actor==-1,"Unavailable later actor releases budget before earlier valid order resolves");
            s=Fresh();s.command.tasks.Clear();s.command.nextTask=100;foreach(var p in s.command.people)p.level=1;s.command.people[0].level=4;s.agents[0].condition=Condition.Lost;
            s.command.config.templates=new System.Collections.Generic.List<CommandTemplate>{new CommandTemplate{title="高阶身份任务",body="检查准入",level=4}};CommandRules.AddTask(s,"上级指示",-1);
            Check(s.command.tasks.Count==0,"Departed member does not unlock new identity-gated tasks");
            s.agents[0].condition=Condition.Active;CommandRules.AddTask(s,"上级指示",-1);Check(s.command.tasks.Count==1,"Active qualified member still unlocks task");
            s=Fresh();t=s.command.tasks[0];t.stepsRequired=2;t.deadline=s.turn;t.plan=1;int beforeMoney=s.money;
            Check(CommandRules.Assign(s,0,t.id).Contains("剩余夜数")&&t.actor==-1&&s.money==beforeMoney,"Unfinishable stages cannot consume authorization or money");
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
            s=Fresh();s.command.missionRevision=0;s.command.tasks[0].plan=0;s.command.tasks[0].intelSupport=false;CommandRules.Validate(s);Check(s.command.missionRevision==2&&s.command.config.templates.Count==22,"Old mission config migrates");
            var config=JsonUtility.FromJson<CommandConfig>(Resources.Load<TextAsset>("Minglu/Config/command_config").text);config.templates[0].followup=config.templates[0].title;bool rejected=false;try{CommandRules.ValidateConfig(config);}catch{rejected=true;}Check(rejected,"Cyclic followups rejected");
            VerifyRecon();File.Delete(file);if(File.Exists(file+".bak"))File.Delete(file+".bak");Debug.Log("MINGLU_MISSION_VERIFY_SUCCESS: "+checks+" mission checks");
        }
        public static void BatchDeliver(){try{Verify();CommandVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
