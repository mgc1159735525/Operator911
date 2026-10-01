using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class ExperienceVerification
    {
        static int checks;
        static void Check(bool condition,string label){if(!condition)throw new Exception("Experience: "+label);checks++;}
        static RunState Fresh(uint seed=911,bool shortRun=false,int difficulty=1)
        {
            var config=shortRun?ConfigStore.Load():CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load());
            var s=RunLifecycle.Create(config,seed,difficulty,0,false);CommandRules.Initialize(s);s.command.config.eventChance=0;return s;
        }
        static CommandRoute Chain(RunState s)
        {
            var r=s.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;return r;
        }
        static void Strong(RunState s)
        {
            foreach(var a in s.agents)a.intel=a.stealth=a.persuade=a.organize=6;
            s.command.tasks.Clear();s.random=1;
        }
        static void VerifyTimingAndMigration()
        {
            var s=Fresh();Strong(s);var r=Chain(s);
            Check(CommandRules.SourceWorksTonight(s,r),"New first line observes on the first night");
            CommandRules.Resolve(s);Check(r.waiting==1&&r.atRelay==0&&r.delivered==0,"First night production stays upstream");
            s.random=1;CommandRules.Resolve(s);Check(r.waiting==0&&r.atRelay==1&&r.delivered==0,"Second night advances one hop only");
            s.random=1;CommandRules.Resolve(s);Check(r.delivered==1&&s.intelligence==1,"Third night courier delivers first packet");
            for(int difficulty=0;difficulty<3;difficulty++)
            {
                var a=Fresh(911,true,difficulty);var b=Fresh(912,true,difficulty);
                Check(CommandRules.FinalDeliveries(a)==2+difficulty&&CommandRules.FinalScore(a)==16+4*difficulty,"Short goals have their own difficulty budget");
                Check(CommandRules.FinalDeliveries(a)==CommandRules.FinalDeliveries(b)&&CommandRules.FinalScore(a)==CommandRules.FinalScore(b),"Seeds do not secretly change short-run goals");
                a=Fresh(911,false,difficulty);b=Fresh(912,false,difficulty);
                Check(CommandRules.FinalDeliveries(a)==6+difficulty&&CommandRules.FinalScore(a)==30+5*difficulty&&CommandRules.SecondGateDeliveries(a)==3+difficulty,"Long goals follow declared difficulty");
                Check(CommandRules.FinalDeliveries(a)==CommandRules.FinalDeliveries(b),"Long seed affects situation rather than hard target");
            }
            s=Fresh(911);r=Chain(s);s.command.experienceRevision=0;s.command.variant=2;s.command.missionRevision=1;
            s.command.config.templates.RemoveAll(t=>t.sourceInstitution>0);var pending=s.command.tasks[0];pending.reward=17;pending.money=19;
            uint random=s.random;CommandRules.Validate(s);
            Check(s.command.legacySourceSchedule&&!CommandRules.SourceWorksTonight(s,r)&&CommandRules.FinalDeliveries(s)==9&&CommandRules.FinalScore(s)==45&&CommandRules.SecondGateDeliveries(s)==6,"Legacy run keeps original goal and even-night first-line cadence");
            Check(s.random==random&&pending.reward==17&&pending.money==19&&s.command.config.templates.Count==22,"Migration adds future content without rerolling RNG or rewriting authorized rewards");
            string once=JsonUtility.ToJson(s);CommandRules.Validate(s);Check(once==JsonUtility.ToJson(s)&&s.random==random,"Migration is idempotent");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-experience-check.json");
            PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);
            Check(loaded.command.legacySourceSchedule&&CommandRules.FinalDeliveries(loaded)==9&&loaded.random==random,"Saved migrated cadence and goals persist");
            CommandRules.Resolve(s);CommandRules.Resolve(loaded);Check(s.random==loaded.random&&JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command),"Migrated load and in-memory run execute the same future");
            File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");
        }
        static void VerifyHandoffs()
        {
            var s=Fresh();var r=Chain(s);r.waiting=3;r.atRelay=2;r.age=4;
            Check(CommandRules.ReplaceRouteMember(s,r,1,3)==""&&!r.enabled&&r.waiting==3&&r.atRelay==2&&r.age==4,"Normal liaison handoff preserves both inventories and their age");
            var before=JsonUtility.ToJson(r);Check(CommandRules.ReplaceRouteMember(s,r,0,999)!=""&&before==JsonUtility.ToJson(r),"Unknown actor cannot mutate a route");
            Check(CommandRules.ReplaceRouteMember(s,r,2,3)!="","A member cannot occupy two slots");
            s.command.people[4].institution=0;Check(CommandRules.ReplaceRouteMember(s,r,0,4).Contains("敌方机构"),"Replacement source still needs institutional access");
            var other=s.command.routes[1];other.source=4;other.liaison=5;other.courier=6;other.enabled=true;
            Check(CommandRules.ReplaceRouteMember(s,r,2,5).Contains("另一条"),"Cross-line reuse is rejected");
            for(int slot=0;slot<3;slot++)
            {
                s=Fresh();r=Chain(s);r.waiting=3;r.atRelay=2;r.age=4;s.turn=4;
                CommandRules.CaptureForCommand(s,slot);
                Check(r.waiting==(slot<=1?0:3)&&r.atRelay==(slot>=1?0:2)&&r.age==4&&!r.enabled,"Capture damages only the compromised slot's reachable stages: "+slot);
                Check(CommandRules.RouteReplacementLoss(s,r,slot).Contains("失陷损失已处理"),"Handoff explains already-resolved compromise instead of claiming a normal member");
                s.command.people[3].institution=1;
                int waiting=r.waiting,relay=r.atRelay;
                Check(CommandRules.ReplaceRouteMember(s,r,slot,3)==""&&r.waiting==waiting&&r.atRelay==relay&&r.age==4,"Replacing already-compromised slot does not damage surviving inventory twice: "+slot);
            }
            s=Fresh();r=Chain(s);r.waiting=3;r.atRelay=2;r.age=4;s.agents[2].condition=Condition.Lost;
            Check(CommandRules.RouteReplacementLoss(s,r,2).Contains("受损2份")&&CommandRules.ReplaceRouteMember(s,r,2,3)==""&&r.waiting==3&&r.atRelay==0,"Old/externally marked lost member receives stage-local damage on handoff");
            s=Fresh();r=Chain(s);s.turn=4;CommandRules.CaptureForCommand(s,0);
            var rescue=s.command.tasks.Single(t=>t.operation==2&&t.target==0);s.agents[3].stealth=30;
            Check(CommandRules.Assign(s,3,rescue.id)=="","Rescue first stage can be authorized");CommandRules.Resolve(s);
            Check(CommandRules.Assign(s,3,rescue.id)=="","Rescue second stage can be authorized");CommandRules.Resolve(s);
            Check(s.agents[0].condition==Condition.Active,"Captured source returns through actual rescue resolution");
            r.waiting=4;r.atRelay=3;r.enabled=true;CommandRules.CaptureForCommand(s,0);
            Check(r.waiting==0&&r.atRelay==3,"A fresh capture after rescue applies damage again rather than reusing an old incident bit");
        }
        static void VerifyPeopleAndEvents()
        {
            var s=Fresh();Strong(s);var r=Chain(s);var a=s.agents[0];a.intel=5;a.skillExperience[0]=3;
            CommandRules.Resolve(s);Check(a.intel==6&&a.skillExperience[0]==0,"Successful source work earns the same skill advancement as completed tasks");
            s=Fresh();var p=s.command.people[0];p.loyalty=100;
            Check(CommandRules.BetrayalChance(p)==0,"Maximum trust has no random betrayal");
            p.loyalty=20;int low=CommandRules.BetrayalChance(p);p.loyalty=70;int high=CommandRules.BetrayalChance(p);
            Check(low>high&&high>0,"Betrayal risk falls continuously with trust");
            p.loyalty=100;s.command.eventKind=3;s.command.eventActor=0;CommandRules.Choose(s,false);
            Check(s.agents[0].condition==Condition.Active&&p.loyalty==100,"Existing high-trust event safely honors continued trust");
            p.temper="果断";Check(CommandRules.ActionStaminaCost(s,s.agents[0])==12,"Decisive temperament reduces effort cost");
            p.temper="谨慎";var t=s.command.tasks[0];int cautious=CommandRules.TaskExposure(s,s.agents[0],t);p.temper="热心";
            Check(CommandRules.TaskExposure(s,s.agents[0],t)>=cautious&&CommandRules.TemperEffect(p).Contains("35%"),"Other temperaments retain their declared effects");
            s=Fresh();s.turn=3;s.pressure=0;s.command.tasks.Clear();s.command.config.eventChance=100;foreach(var who in s.command.people){who.loyalty=100;who.neglect=0;}
            CommandRules.Resolve(s);Check(s.command.eventKind==0,"Healthy relationships and low pressure do not manufacture a contextless crisis");
            s=Fresh();s.turn=3;s.pressure=40;s.command.tasks.Clear();s.command.config.eventChance=100;foreach(var who in s.command.people)who.loyalty=100;
            CommandRules.Resolve(s);Check(s.command.eventKind==2&&s.command.eventText.Contains("压力已达"),"Pressure-led purge explains its actual trigger");
        }
        static void VerifyInstitutionContent()
        {
            for(int institution=1;institution<=4;institution++)
            {
                var s=Fresh();s.command.tasks.Clear();s.command.nextTask=100;var person=s.command.people[0];person.institution=institution;person.level=1;
                CommandRules.AddTask(s,"内线提议",person.id);
                Check(s.command.tasks.Count==1&&s.command.tasks[0].institution==institution&&s.command.tasks[0].source==person.id,"Institution source selects its own authored discovery: "+institution);
                var task=s.command.tasks[0];task.status=1;int district=task.district;CommandRules.Resolve(s);
                var follow=s.command.tasks.Single(x=>x.title==task.followup);
                Check(follow.institution==institution&&follow.source==person.id&&follow.district==district&&follow.intelReward==0,"Institution followup keeps its source and district without free intelligence: "+institution);
                Check(institution==1?follow.heartsReward==10:institution==2?follow.money==20:institution==3?follow.pressureRelief==10:follow.traceRelief==10,"Institution lines change distinct organizational outcomes: "+institution);
            }
            var civilian=Fresh();civilian.command.tasks.Clear();civilian.command.nextTask=100;var reporter=civilian.command.people[0];reporter.institution=0;reporter.profession="药师";reporter.level=1;
            foreach(var p in civilian.command.people.Skip(1)){p.profession="书吏";p.level=4;}
            for(int i=0;i<12;i++)CommandRules.AddTask(civilian,"成员发现",reporter.id);
            Check(civilian.command.tasks.Count>0&&civilian.command.tasks.All(t=>t.level<=reporter.level&&(t.profession==""||t.profession==reporter.profession)&&t.institution==0),"Civilian proposals respect the actual reporter instead of unrelated qualified members");
            civilian.command.tasks.Clear();CommandRules.AddTask(civilian,"内线提议",reporter.id);Check(civilian.command.tasks.Count==0,"Civilian cannot generate an institutional discovery");
            civilian.agents[0].condition=Condition.Lost;CommandRules.AddTask(civilian,"突发事件",reporter.id);
            Check(civilian.command.tasks.Count==1&&civilian.command.tasks[0].source==reporter.id,"A lost member can remain the cause of an emergency handled by the surviving organization");
        }
        static void SampleRuns()
        {
            foreach(bool shortRun in new[]{true,false})
            {
                int wins=0,deliveries=0,firstTotal=0,firstCount=0;
                for(uint seed=1;seed<=12;seed++)
                {
                    var s=Fresh(seed,shortRun);s.command.config.eventChance=28;Chain(s);int first=-1;
                    for(int guard=0;guard<40&&!s.ended;guard++)
                    {
                        if(s.command.eventKind!=0)CommandRules.Choose(s,s.money>=10);
                        foreach(var task in s.command.tasks.Where(t=>t.status==0&&t.actor<0).OrderBy(t=>t.deadline))
                        {
                            var actor=s.agents.Where(a=>a.id>=3&&CommandRules.TaskError(s,a.id,task)=="").OrderByDescending(a=>CommandRules.TaskDice(s,a,task)).FirstOrDefault();
                            if(actor!=null)CommandRules.Assign(s,actor.id,task.id);
                        }
                        int night=s.turn;CommandRules.Resolve(s);CommandRules.Validate(s);s.command.notices.Clear();
                        if(first<0&&s.command.delivered>0)first=night;
                    }
                    Check(s.ended,"Fixed single-line sample completes within the declared window");
                    wins+=s.won?1:0;deliveries+=s.command.delivered;if(first>=0){firstTotal+=first;firstCount++;}
                }
                Debug.Log("MINGLU_EXPERIENCE_SAMPLE: "+(shortRun?8:20)+" nights / fixed single-line, three mobile workers, no active rotation or alliances / "+wins+" wins of 12 seeds / deliveries "+deliveries+" / first delivery avg "+(firstCount==0?"none":((float)firstTotal/firstCount).ToString("0.00"))+" / diagnostic script only, NOT player win rate");
            }
        }
        [MenuItem("Minglu/验证首局与组织体验")]
        public static void Verify()
        {
            checks=0;VerifyTimingAndMigration();VerifyHandoffs();VerifyPeopleAndEvents();VerifyInstitutionContent();SampleRuns();
            Debug.Log("MINGLU_EXPERIENCE_VERIFY_SUCCESS: "+checks+" checks");
        }
    }
}
