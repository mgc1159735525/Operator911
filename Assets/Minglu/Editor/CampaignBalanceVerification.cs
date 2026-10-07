using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    // Diagnostic management policy, not a prediction of human player win rates.
    // It only uses choices exposed to the player. No stats, dice, money, task
    // results, event odds or campaign thresholds are replaced by the fixture.
    public static class CampaignBalanceVerification
    {
        sealed class Assignment
        {
            public int actor=-1,plan;
            public bool support;
            public double score=double.MinValue,probability;
        }
        static int checks,retryRecoveries,restRecoveries;
        static void Check(bool condition,string label)
        {if(!condition)throw new Exception("Campaign balance: "+label);checks++;}
        static RunState Fresh(uint seed,int difficulty)
        {
            var config=CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load());
            var s=RunLifecycle.Create(config,seed,difficulty,0,false);
            CommandRules.Initialize(s);CommandRules.InitializeCommandCampaign(s);
            return s;
        }
        static int Member(CommandRoute r,int slot){return slot==0?r.source:slot==1?r.liaison:r.courier;}
        static bool Working(RunState s,int actor)
        {
            return s.command.routes.Any(r=>Enumerable.Range(0,3).Any(slot=>Member(r,slot)==actor&&CommandRules.RouteSlotHasWork(s,r,slot)));
        }
        static void ResolveEvents(RunState s)
        {
            for(int guard=0;s.command.eventKind!=0&&guard<8;guard++)
            {
                int cost=s.command.eventKind==1?6:s.command.eventKind==2?10:s.command.eventKind==4?s.command.strategy.config.relocateCost:0;
                string error=CommandRules.Choose(s,s.money-CommandRules.ReservedMoney(s)>=cost);
                Check(error=="","A contextual event always has an affordable response");
            }
            Check(s.command.eventKind==0,"Chained events resolve without a decision softlock");
        }
        static void ScheduleRest(RunState s,HashSet<int> rested)
        {
            foreach(var a in s.agents.Where(a=>a.condition==Condition.Active))
            {
                var p=CommandRules.Person(s,a.id);
                bool rest=a.exposure>=55||a.stamina<45||a.spirit<40||p.neglect>=2;
                CommandRules.SetPolicy(s,a.id,rest?1:0);
                if(rest)rested.Add(a.id);
                else if(rested.Remove(a.id))restRecoveries++;
            }
        }
        static void StaffRoutes(RunState s)
        {
            if(s.command.delivered>=CommandRules.FinalDeliveries(s))
            {
                foreach(var r in s.command.routes)r.enabled=false;
                return;
            }
            foreach(var route in s.command.routes.OrderBy(r=>r.id))
            {
                for(int slot=0;slot<3;slot++)
                {
                    int current=Member(route,slot);
                    // Maintain continuity; change a post only when unavailable.
                    if(current>=0&&CommandRules.Availability(s,current)==""&&(slot!=0||CommandRules.SourceError(s,current)=="")
                        &&!s.command.routes.Any(other=>other!=route&&other.enabled&&Enumerable.Range(0,3).Any(k=>Member(other,k)==current)))continue;
                    int skill=slot==0?0:slot==1?2:1;
                    var candidate=s.agents.Where(a=>CommandRules.Availability(s,a.id)==""&&(slot!=0||CommandRules.SourceError(s,a.id)==""))
                        .Where(a=>!Enumerable.Range(0,3).Any(other=>other!=slot&&Member(route,other)==a.id))
                        .Where(a=>!s.command.routes.Any(other=>other!=route&&other.enabled&&Enumerable.Range(0,3).Any(k=>Member(other,k)==a.id)))
                        .OrderByDescending(a=>CommandRules.Dice(s,a,skill)*10-a.exposure*.1-CommandRules.Person(s,a.id).neglect*2).FirstOrDefault();
                    if(candidate!=null)Check(CommandRules.ReplaceRouteMember(s,route,slot,candidate.id)=="","Roster choices use the same legal handoff as the UI");
                    if(slot==0&&route.source<0)break;
                }
                if(CommandRules.RouteError(s,route)=="")route.enabled=true;
            }
        }
        static void PlanSecondSource(RunState s)
        {
            if(s.turn<3||s.turn>9||s.command.delivered>=CommandRules.FinalDeliveries(s)||s.command.people.Count(p=>CommandRules.HasInstitution(p)&&CommandRules.Agent(s,p.id).condition==Condition.Active)>=2)return;
            if(s.command.tasks.Any(t=>t.status==0&&t.operation==4))return;
            var person=s.agents.Where(a=>CommandRules.Availability(s,a.id)==""&&!CommandRules.HasInstitution(CommandRules.Person(s,a.id)))
                .Where(a=>!s.command.routes.Any(r=>r.enabled&&Enumerable.Range(0,3).Any(slot=>Member(r,slot)==a.id)))
                .OrderByDescending(a=>a.persuade+a.intel).FirstOrDefault();
            if(person==null)return;
            var chapter=CommandRules.CampaignCurrentChapter(s);
            int institution=chapter==null?1:chapter.approaches[0].evidenceInstitution;
            if(CommandRules.EmploymentError(s,person.id,institution)=="")Check(CommandRules.StartEmployment(s,person.id,institution)=="","A missing second source is obtained through an actual employment mission");
        }
        static Assignment BestAssignment(RunState s,CommandTask task,bool essential)
        {
            var best=new Assignment();int oldPlan=task.plan;bool oldSupport=task.intelSupport;
            // Compare only publicly visible forecasts. Retain a full night's
            // upkeep and ten silver for a contextual protection choice.
            int reserve=s.agents.Count(a=>a.condition==Condition.Active)*2+10;
            for(int plan=0;plan<=1;plan++)for(int support=0;support<=1;support++)
            {
                if(plan==1&&s.money-CommandRules.ReservedMoney(s,task.id)<reserve+4)continue;
                if(support==1&&(!essential||s.intelligence-CommandRules.ReservedIntel(s,task.id)<1))continue;
                task.plan=plan;task.intelSupport=support==1;
                foreach(var a in s.agents)
                {
                    if(CommandRules.TaskError(s,a.id,task)!="")continue;
                    double probability=DispatchRules.Probability(CommandRules.TaskDice(s,a,task),CommandRules.ActionRequired(task));
                    double score=probability*100-a.exposure*.25-CommandRules.TaskExposure(s,a,task)*.5
                        -(Working(s,a.id)?(essential?48:100):0)-plan*3-support*2-CommandRules.Person(s,a.id).neglect*4;
                    if(score>best.score)best=new Assignment{actor=a.id,plan=plan,support=support==1,score=score,probability=probability};
                }
            }
            task.plan=oldPlan;task.intelSupport=oldSupport;return best;
        }
        static bool AssignTask(RunState s,CommandTask task,bool essential)
        {
            if(task==null||task.status!=0||task.actor>=0)return false;
            var choice=BestAssignment(s,task,essential);
            if(choice.actor<0||choice.probability<(essential?.45:.7)||(!essential&&Working(s,choice.actor)))return false;
            task.plan=choice.plan;task.intelSupport=choice.support;
            Check(CommandRules.Assign(s,choice.actor,task.id)=="","Public forecast and actual authorization agree");
            return true;
        }
        static void PlanTasks(RunState s)
        {
            if(s.turn==1)
            {
                var first=s.command.tasks.Where(t=>t.status==0&&t.level==1).OrderByDescending(t=>BestAssignment(s,t,true).score).FirstOrDefault();
                Check(AssignTask(s,first,true),"The untouched random opening allows one introductory assignment");
                return;
            }
            var chapter=CommandRules.CampaignCurrentChapter(s);
            if(chapter!=null)
            {
                int approach=CommandRules.CampaignApproachError(s,0)==""?0:1;
                if(CommandRules.CampaignApproachError(s,approach)=="")Check(CommandRules.StartCampaignApproach(s,approach)=="","A chapter has a legal available approach");
            }
            var campaign=s.command.tasks.FirstOrDefault(t=>t.id==s.command.campaign.activeTask&&t.status==0);
            var rescues=s.command.tasks.Where(t=>t.status==0&&t.operation==2).OrderBy(t=>t.deadline).ToArray();
            foreach(var rescue in rescues)AssignTask(s,rescue,true);
            AssignTask(s,campaign,true);
            foreach(var job in s.command.tasks.Where(t=>t.status==0&&t.operation==4).ToArray())AssignTask(s,job,true);

            // Two or three explicit commissions per night keep spare members
            // available for the standing network and their public identities.
            foreach(var task in s.command.tasks.Where(t=>t.status==0&&t.actor<0&&string.IsNullOrEmpty(t.campaignKey)&&t.operation!=4&&t.operation!=2)
                .OrderBy(t=>t.deadline).ThenByDescending(t=>t.pressureRelief+t.traceRelief+t.money*.3).ToArray())
            {
                if(s.command.tasks.Count(t=>t.status==0&&t.actor>=0)>=3)break;
                AssignTask(s,task,false);
            }
        }
        static void CheckSameFuture(RunState s,RunState loaded)
        {
            Check(s.random==loaded.random&&s.money==loaded.money&&s.intelligence==loaded.intelligence&&s.progress==loaded.progress&&s.pressure==loaded.pressure
                &&s.turn==loaded.turn&&s.ended==loaded.ended&&s.won==loaded.won
                &&JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command)
                &&s.agents.Select(JsonUtility.ToJson).SequenceEqual(loaded.agents.Select(JsonUtility.ToJson)),"Real campaign saves preserve the exact state and random continuation");
        }
        static string Missing(RunState s)
        {
            var parts=new List<string>();
            if(!CommandRules.CampaignReadyForVictory(s))parts.Add("章节"+s.command.campaign.chapter+"/3");
            if(s.command.delivered<CommandRules.FinalDeliveries(s))parts.Add("送达"+s.command.delivered+"/"+CommandRules.FinalDeliveries(s));
            if(s.progress<CommandRules.FinalScore(s))parts.Add("成果"+s.progress+"/"+CommandRules.FinalScore(s));
            if(!s.command.people.Any(p=>p.level>=CommandRules.FinalLevel(s)&&CommandRules.Agent(s,p.id).condition==Condition.Active))parts.Add("身份等级未达标");
            if(s.agents.Count(a=>a.condition==Condition.Active)<3)parts.Add("可用人员不足");
            return s.won?"完整胜利":string.Join("、",parts);
        }
        [MenuItem("Minglu/验证完整战役经营与数值诊断")]
        public static void Verify()
        {
            checks=retryRecoveries=restRecoveries=0;
            string path=Path.Combine(Application.temporaryCachePath,"minglu-campaign-balance-"+Guid.NewGuid().ToString("N")+".json");
            var difficultyWins=new List<int>();
            try
            {
                for(int difficulty=0;difficulty<3;difficulty++)
                {
                    int wins=0,firstSum=0,firstCount=0,chapters=0,lost=0,deliveries=0,finishedNights=0;
                    var reasons=new Dictionary<string,int>();
                    for(uint seed=1;seed<=20;seed++)
                    {
                        var s=Fresh(seed,difficulty);var rested=new HashSet<int>();var failedTasks=new HashSet<int>();var recoveredTasks=new HashSet<int>();
                        int first=-1;
                        for(int guard=0;guard<30&&!s.ended;guard++)
                        {
                            ResolveEvents(s);ScheduleRest(s,rested);
                            if(s.turn>=2){StaffRoutes(s);PlanSecondSource(s);}
                            PlanTasks(s);s.command.notices.Clear();
                            int night=s.turn;RunState loaded=null;
                            if(seed<=2)
                            {
                                PrototypeSave.Write(s,path);loaded=PrototypeSave.Read(path);CheckSameFuture(s,loaded);
                            }
                            CommandRules.Resolve(s);
                            if(loaded!=null){CommandRules.Resolve(loaded);CheckSameFuture(s,loaded);s=loaded;}
                            CommandRules.Validate(s);
                            Check(s.money>=0&&s.intelligence>=0&&s.agents.All(a=>a.stamina>=0&&a.stamina<=100&&a.exposure>=0&&a.exposure<=100),"Real-turn resource and member invariants hold");
                            if(first<0&&s.command.delivered>0)first=night;
                            foreach(var task in s.command.tasks)
                            {
                                if(task.lastResult.Contains("未成"))failedTasks.Add(task.id);
                                if(task.status==1&&failedTasks.Contains(task.id)&&recoveredTasks.Add(task.id))retryRecoveries++;
                            }
                        }
                        Check(s.ended&&s.turn<=20,"Each actual campaign terminates within its declared window");
                        if(s.won)Check(CommandRules.CampaignReadyForVictory(s)&&s.command.campaign.chapter==3&&s.command.delivered>=CommandRules.FinalDeliveries(s),"A reported win really completed all chapters and the organization's goals");
                        wins+=s.won?1:0;chapters+=s.command.campaign.chapter;deliveries+=s.command.delivered;finishedNights+=s.turn;
                        lost+=s.agents.Count(a=>a.condition!=Condition.Active);if(first>=0){firstSum+=first;firstCount++;}
                        string reason=Missing(s);if(!reasons.ContainsKey(reason))reasons[reason]=0;reasons[reason]++;
                        Debug.Log("MINGLU_CAMPAIGN_BALANCE_RUN: difficulty "+difficulty+" / seed "+seed+" / scenario "+s.command.campaign.scenario+" / end "+s.turn+" / first delivery "+first+" / chapters "+s.command.campaign.chapter+" / delivered "+s.command.delivered+" / unavailable "+s.agents.Count(a=>a.condition!=Condition.Active)+" / "+reason);
                    }
                    difficultyWins.Add(wins);
                    Debug.Log("MINGLU_CAMPAIGN_BALANCE_SAMPLE: "+RunLifecycle.DifficultyName(difficulty)+" / "+wins+" wins of 20 / first delivery avg "+(firstCount==0?"none":((double)firstSum/firstCount).ToString("0.00"))+" / chapters "+chapters+" of 60 / deliveries "+deliveries+" / unavailable at ending "+lost+" / mean ending night "+(finishedNights/20d).ToString("0.00")+" / reasons "+string.Join("; ",reasons.Select(r=>r.Key+"="+r.Value))+" / diagnostic management policy, NOT player win rate");
                }
                Check(difficultyWins.All(w=>w>0),"Every difficulty has a reachable full victory under legal management; inspect per-run reasons if any has zero wins");
                Check(retryRecoveries>0&&restRecoveries>0,"Real failures can recover through another authorized attempt, and rest returns members to work");
                Debug.Log("MINGLU_CAMPAIGN_BALANCE_VERIFY_SUCCESS: "+checks+" checks / 60 actual campaigns / retry recoveries "+retryRecoveries+" / rest returns "+restRecoveries+" / 6 campaigns save-roundtripped every turn / no altered stats, resources, RNG or odds");
            }
            finally
            {
                foreach(string file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);
            }
        }
    }
}
