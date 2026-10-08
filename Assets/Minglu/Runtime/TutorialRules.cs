using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    [Serializable] public class TutorialState
    {
        // Disabled is the safe default if Unity materializes an omitted inline
        // object in an older ordinary save. Only Create opts into a lesson.
        public bool enabled;
        public int revision=1,stage,introShownStage=-1;
        public bool inspectedMember,comparedMember,reportRead,lifeArranged,eventHandled,eventOffered,completed;
        public int firstTaskId=-1,secondTaskId=-1,sourceId=-1,liaisonId=-1,courierId=-1,freeActorId=-1,restActorId=-1;
        public int resolvedNights,lastResolvedNight,resolvingNight;
        public string completionSummary="";
    }

    public static class TutorialRules
    {
        public const uint Seed=43;
        public const int Nights=8;
        public static bool IsTutorial(RunState s){return s!=null&&s.tutorial!=null&&s.tutorial.enabled;}
        public static RunState Create(GameConfig source)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var config=JsonUtility.FromJson<GameConfig>(JsonUtility.ToJson(source));
            config.settings.First(x=>x.id=="turns").value=Nights;
            // Legacy action events/missions are not part of command mode. A
            // campaign-shaped input must not leave deadlines beyond this lesson.
            config.events.Clear();config.missions.Clear();
            var s=RunLifecycle.Create(config,Seed,0,0,false);
            CommandRules.Initialize(s);
            s.tutorial=new TutorialState{enabled=true,sourceId=0,liaisonId=1,courierId=2,freeActorId=3,restActorId=3};
            s.runId="tutorial-"+s.runId;
            var c=s.command;
            c.config.eventChance=0;c.config.taskInterval=10;
            // Reuse published task requirements and rewards. The lesson removes
            // follow-up clutter, not rolls, costs, failures, or intel provenance.
            string[] names={"核实异常调动","争取街坊支持","政务署人事异动"};
            c.config.templates=c.config.templates.Where(t=>names.Contains(t.title)).Select(t=>JsonUtility.FromJson<CommandTemplate>(JsonUtility.ToJson(t))).ToList();
            foreach(var template in c.config.templates)template.followup="";
            c.tasks.Clear();c.nextTask=0;c.notices.Clear();c.noticeArchive.Clear();s.journal.Clear();
            foreach(string title in names.Take(2))
            {
                var template=c.config.templates.First(t=>t.title==title);
                CommandRules.CreateTask(s,template,title==names[0]?"上级指示":"成员发现",title==names[0]?-1:4,s.config.districts[0].id);
                c.tasks.Last().deadline=Nights;
            }
            s.tutorial.firstTaskId=c.tasks[0].id;s.tutorial.secondTaskId=c.tasks[1].id;
            CommandRules.ValidateConfig(c.config);
            CommandRules.Log(s,"八夜指挥练习：成员行动、骰子判定、身份日常与机构联络链均使用正式规则。完成至少2项任务、送达1份机构情报并处理一次事件；第8夜统一复盘。教学进度独立保存，不记录正式战绩。");
            Validate(s);return s;
        }
        public static void Refresh(RunState s)
        {
            if(!IsTutorial(s))return;
            var t=s.tutorial;var first=s.command.tasks.FirstOrDefault(task=>task.id==t.firstTaskId);
            // Facts accumulate. A resolved failure clears actor in normal rules,
            // but must not send the learner back to selecting a first member.
            bool advance=true;
            while(advance&&t.stage<6)
            {
                switch(t.stage)
                {
                    case 0: advance=t.inspectedMember;break;
                    case 1: advance=t.comparedMember&&first!=null&&(first.actor>=0||!string.IsNullOrEmpty(first.lastResult));break;
                    case 2: advance=t.resolvedNights>0&&first!=null&&!string.IsNullOrEmpty(first.lastResult);break;
                    case 3: advance=t.reportRead;break;
                    case 4:
                        advance=s.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(s,r)=="");
                        if(advance)
                        {
                            // The learner may choose different valid personnel.
                            // Do not then force a member of that very chain into
                            // life priority and silently stop their delivery.
                            var routePeople=new HashSet<int>(s.command.routes.Where(r=>r.enabled).SelectMany(r=>new[]{r.source,r.liaison,r.courier}));
                            var reserve=s.agents.Where(a=>a.condition==Condition.Active&&!routePeople.Contains(a.id))
                                .OrderBy(a=>s.command.tasks.Any(task=>task.status==0&&task.actor==a.id)?1:0)
                                .ThenBy(a=>a.id==t.freeActorId?0:1).ThenBy(a=>a.stamina).ThenBy(a=>a.id).FirstOrDefault();
                            // Keep the chain lesson open if no reserve exists;
                            // never silently reassign or pause an occupied role.
                            if(reserve==null)advance=false;else t.restActorId=reserve.id;
                        }
                        break;
                    case 5: advance=t.lifeArranged;break;
                    default: advance=false;break;
                }
                if(advance)t.stage++;
            }
        }
        public static void BeforeResolve(RunState s)
        {
            if(!IsTutorial(s)||s.ended||s.command.eventKind!=0)return;
            Refresh(s);s.tutorial.resolvingNight=s.turn;
        }
        public static void AfterResolve(RunState s)
        {
            if(!IsTutorial(s))return;
            var t=s.tutorial;int night=t.resolvingNight;
            if(night<=t.lastResolvedNight)return;
            t.lastResolvedNight=night;t.resolvedNights=night;t.resolvingNight=0;
            Refresh(s);
            if(night>=Nights||s.ended)
            {
                var missing=new List<string>();
                if(s.command.completed<2)missing.Add("完成2项任务（当前"+s.command.completed+"）");
                if(s.command.delivered<1)missing.Add("由机构联络链真实送达1份情报");
                if(!t.eventHandled)missing.Add("处理一次家事事件");
                if(!t.inspectedMember)missing.Add("查看成员档案");
                if(!t.comparedMember)missing.Add("比较任务人选与能力骰数");
                if(!t.reportRead)missing.Add("阅读执行报告");
                if(!t.lifeArranged)missing.Add("安排一次成员生活优先");
                if(night<Nights)missing.Add("完成八夜练习");
                t.completed=missing.Count==0;s.ended=true;s.won=t.completed;
                t.completionSummary=t.completed?"八夜练习完成：你已通过成员能力安排任务、读懂执行反馈、建立机构传递链并照顾成员生活。正式行动会有更多任务、机构与风险。":"练习结束，仍可继续学习："+string.Join("；",missing)+"。可以重新练习，正式进度与战绩保持独立。";
                s.ending=t.completionSummary;
                return;
            }
            // A scheduled, mild story event uses the existing decision and
            // leave rules. Never replace another pending incident or resolution.
            if(!t.eventOffered&&s.turn>=4&&s.turn<=5&&s.command.eventKind==0)
            {
                var actor=s.agents.FirstOrDefault(a=>a.id==t.restActorId&&a.condition==Condition.Active)
                    ??s.agents.FirstOrDefault(a=>a.condition==Condition.Active);
                if(actor!=null)
                {
                    t.eventOffered=true;s.command.eventKind=1;s.command.eventActor=actor.id;
                    s.command.eventText=actor.name+"家中临时有急事，希望今夜处理家事。拨款能额外改善信任；经费不足也可以准假。两种方案均会让本人今夜离岗，请查看安排是否受影响。";
                    CommandRules.Log(s,"教学事件："+s.command.eventText);
                }
            }
        }
        public static void RecordEventDecision(RunState s)
        {
            if(IsTutorial(s)&&s.tutorial.eventOffered&&s.command.eventKind==1)
                s.tutorial.eventHandled=true;
        }
        public static void Validate(RunState s)
        {
            var t=s==null?null:s.tutorial;if(t==null)return;
            if(!t.enabled)
            {
                if((s.runId??"").StartsWith("tutorial-",StringComparison.Ordinal)||t.stage!=0||t.resolvedNights!=0||t.lastResolvedNight!=0||t.resolvingNight!=0||t.inspectedMember||t.comparedMember||t.reportRead||t.lifeArranged||t.eventHandled||t.eventOffered||t.completed||!string.IsNullOrEmpty(t.completionSummary))throw new InvalidOperationException("未启用的教学数据含有练习进度。");
                return;
            }
            if(t.revision!=1||s.command==null||!s.roguelikeRun||s.initialSeed!=Seed||s.config.Value("turns")!=Nights||CommandRules.HasCommandCampaign(s)||t.stage<0||t.stage>6||t.introShownStage < -1||t.introShownStage>6||t.resolvedNights<0||t.resolvedNights>Nights||t.lastResolvedNight!=t.resolvedNights||t.resolvingNight!=0)throw new InvalidOperationException("教学关卡版本、阶段或回合数据无效。");
            if((s.ended?s.turn:s.turn-1)!=t.resolvedNights)throw new InvalidOperationException("教学结算次数与当前夜数不一致。");
            int[] people={t.sourceId,t.liaisonId,t.courierId,t.freeActorId,t.restActorId};
            if(people.Any(id=>id<0||!s.agents.Any(a=>a.id==id)||!s.command.people.Any(p=>p.id==id))||new[]{t.sourceId,t.liaisonId,t.courierId}.Distinct().Count()!=3||!s.command.tasks.Any(task=>task.id==t.firstTaskId)||!s.command.tasks.Any(task=>task.id==t.secondTaskId)||t.firstTaskId==t.secondTaskId)throw new InvalidOperationException("教学任务或成员引用无效。");
            if(t.eventHandled&&!t.eventOffered||s.ended&&(s.won!=t.completed||string.IsNullOrEmpty(t.completionSummary))||t.completed&&(!s.ended||!s.won||t.resolvedNights!=Nights||s.command.completed<2||s.command.delivered<1||!t.eventHandled||!t.inspectedMember||!t.comparedMember||!t.reportRead||!t.lifeArranged))throw new InvalidOperationException("教学完成数据与真实成果不一致。");
        }
    }
}
