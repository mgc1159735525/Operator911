using System;
using System.Linq;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class CommandWorkOverviewVerification
    {
        static int checks;
        static void Check(bool condition,string message)
        {if(!condition)throw new Exception("Work overview: "+message);checks++;}
        static RunState Fresh()
        {
            var state=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load()),911,1,0,false);
            CommandRules.Initialize(state);state.turn=6;state.command.config.eventChance=0;state.command.tasks.Clear();
            foreach(var member in state.agents)member.intel=member.stealth=member.persuade=member.organize=6;
            return state;
        }
        static CommandTask Task(RunState state,string title="核实线索")
        {
            CommandRules.CreateTask(state,new CommandTemplate{title=title,body="用于核对今夜安排",required=1,skill=0},"上级指示",-1,0);
            return state.command.tasks.Last();
        }
        static CommandWorkOverview ReadOnlyOverview(RunState state)
        {
            string before=JsonUtility.ToJson(state);uint random=state.random;
            var result=CommandRules.BuildWorkOverview(state);
            Check(before==JsonUtility.ToJson(state)&&random==state.random,"Summary does not change state, assignments, reservations or RNG");
            return result;
        }
        public static void Verify()
        {
            checks=0;VerifyContinuation();VerifyAssignmentsAndConflict();VerifyRisks();VerifyPureLegacyRead();
            Debug.Log("MINGLU_WORK_OVERVIEW_VERIFY_SUCCESS: "+checks+" checks; middle-game continuation, assignment timing, current-work conflicts, pending risks and read-only legacy guards");
        }
        static void VerifyContinuation()
        {
            var s=Fresh();Check(CommandRules.StartCivicTask(s,0)=="","Create actual multi-stage civic task");
            var task=s.command.tasks.Last();s.random=1;
            Check(CommandRules.Assign(s,3,task.id)=="","Authorize real first stage");CommandRules.Resolve(s);
            Check(s.turn==7&&task.stepsDone==1&&task.actor<0,"Middle-game resolution leaves second stage awaiting an explicit decision");
            var result=ReadOnlyOverview(s);
            Check(result.continuation.Any(r=>r.taskId==task.id&&r.detail.Contains("1 / 2")&&r.detail.Contains("尚未签发")),"Completed stage appears in continuation with actual progress");
            var freshTask=Task(s,"尚未开始的机会");
            result=ReadOnlyOverview(s);Check(result.continuation.All(r=>r.taskId!=freshTask.id),"Unattempted task is not mislabeled as a continuation");
            Check(CommandRules.Assign(s,3,task.id)=="","Authorize second stage");result=ReadOnlyOverview(s);
            Check(result.authorizedCount==1&&result.assigned.Any(r=>r.taskId==task.id&&r.detail.Contains("第2 / 2阶段"))&&result.continuation.All(r=>r.taskId!=task.id),"Signing continuation moves it to tonight's assignments without auto-executing");

            s=Fresh();task=Task(s);task.reconnaissance=true;s.random=1;
            Check(CommandRules.Assign(s,3,task.id)=="","Authorize investigation");CommandRules.Resolve(s);result=ReadOnlyOverview(s);
            Check(task.reconComplete&&task.stepsDone==0&&result.continuation.Any(r=>r.taskId==task.id&&r.detail.Contains("调查已完成")),"Investigation result requests execution without falsely claiming a task stage");
            task.reconComplete=false;task.lastResult="第6夜：未成，期限内可重新授权";result=ReadOnlyOverview(s);
            Check(result.continuation.Any(r=>r.taskId==task.id&&r.detail.Contains("上次未成")),"Unsuccessful attempt remains visible for retry");
            task.status=1;result=ReadOnlyOverview(s);Check(result.continuation.All(r=>r.taskId!=task.id),"Finished task is excluded from continuation");
        }
        static void VerifyAssignmentsAndConflict()
        {
            var s=Fresh();var task=Task(s);task.plan=1;s.intelligence=2;task.intelSupport=true;
            Check(CommandRules.Assign(s,1,task.id)=="","Authorize liaison for a task");
            var route=s.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;route.waiting=1;
            var result=ReadOnlyOverview(s);
            Check(result.authorizedCount==1&&result.activeRouteCount==1&&result.assigned.Single().actorId==1,"Overview reports actual authorization and route counts");
            Check(result.blocked.Any(r=>r.routeId==0&&r.slot==1&&r.actorId==1&&r.detail.Contains("优先执行")&&r.detail.Contains(task.title)),"Liaison conflict names both the actor and authorized task");
            Check(CommandRules.ReservedMoney(s)==4&&CommandRules.ReservedIntel(s)==1,"Summary preserves reserved resources");
            route.waiting=0;result=ReadOnlyOverview(s);
            Check(result.blocked.All(r=>r.routeId!=0||r.slot!=1),"No invented liaison conflict when there is no packet to meet");
            task.actor=-1;Check(CommandRules.Assign(s,0,task.id)=="","Authorize source during a quiet source night");result=ReadOnlyOverview(s);
            Check(result.blocked.All(r=>r.routeId!=0||r.slot!=0),"Source task does not block a nonexistent production cycle");
            s.turn=7;result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.routeId==0&&r.slot==0&&r.actorId==0),"Production-night source conflict appears when work is due");
            s.money=0;result=ReadOnlyOverview(s);
            Check(result.authorizedCount==1&&result.assigned.Count==0&&result.blocked.Any(r=>r.taskId==task.id&&r.detail.Contains("授权暂不能执行")),"Invalid authorization is visible and counted without promising execution");
            Check(result.blocked.All(r=>r.routeId!=0||r.slot!=0),"Unexecutable task does not falsely consume a route member");
        }
        static void VerifyRisks()
        {
            var s=Fresh();var task=Task(s);task.deadline=s.turn;
            var result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.taskId==task.id&&r.warning&&r.title.Contains("最后开工")),"Unassigned last-chance task appears in risk summary");
            Check(CommandRules.Assign(s,3,task.id)=="","Authorize last-chance task");result=ReadOnlyOverview(s);
            Check(result.assigned.Any(r=>r.taskId==task.id&&r.detail.Contains("无重试余量"))&&!result.blocked.Any(r=>r.taskId==task.id),"Already signed urgent task stays in assignments with its timing caveat");
            task.actor=-1;task.stepsRequired=2;result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.taskId==task.id&&r.detail.Contains("来不及")),"Insufficient remaining nights is explicit");
            var route=s.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.waiting=2;route.atRelay=1;route.age=5;route.enabled=false;
            result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.routeId==0&&r.title.Contains("已暂停"))&&result.blocked.Any(r=>r.routeId==0&&r.title.Contains("即将失效")),"Disabled route still reports queued inventory and aging risk");
            route.enabled=true;s.command.pauseUntil=s.turn;result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.routeId==0&&r.title.Contains("本夜休整")),"Global concealment or relocation pause stays visible");
            s.command.pauseUntil=0;CommandRules.SetPolicy(s,1,1);result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.routeId==0&&r.slot==1&&r.detail.Contains("生活优先")),"Living obligations explain a due liaison interruption");
            s.agents[1].exposure=95;CommandRules.Person(s,1).warningTurn=s.turn-1;result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.actorId==1&&r.detail.Contains("休养后仍达到捕获阈值")),"Rest does not erase an unresolved capture threshold warning");
            CommandRules.CaptureForCommand(s,2);result=ReadOnlyOverview(s);
            var rescue=s.command.tasks.Last();
            Check(result.blocked.Any(r=>r.actorId==2&&r.taskId==rescue.id&&r.warning),"Captured member links to the actual rescue task");
            s.command.eventKind=1;result=ReadOnlyOverview(s);
            Check(result.pauseReason.Contains("待决事件")&&result.assigned.Count==0,"Pending decision does not claim that work will execute");
        }
        static void VerifyPureLegacyRead()
        {
            var s=Fresh();s.command.strategy=null;var result=ReadOnlyOverview(s);
            Check(result.pauseReason!=""&&s.command.strategy==null,"Uninitialized strategy is not silently initialized by a summary");
            s=Fresh();s.command.experienceRevision=0;result=ReadOnlyOverview(s);
            Check(result.pauseReason!=""&&s.command.experienceRevision==0,"Legacy source schedule is not silently migrated by a summary");
            s=Fresh();s.command.legacySourceSchedule=true;var route=s.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;
            var task=Task(s);Check(CommandRules.Assign(s,0,task.id)=="","Authorize legacy source");
            result=ReadOnlyOverview(s);
            Check(result.blocked.Any(r=>r.routeId==0&&r.slot==0),"Validated legacy even-night production is retained in conflict prediction");
            s.ended=true;result=ReadOnlyOverview(s);Check(result.pauseReason.Contains("已结束"),"Ended run cannot show live execution promises");
        }
    }
}
