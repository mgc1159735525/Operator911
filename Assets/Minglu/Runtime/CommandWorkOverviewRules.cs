using System.Collections.Generic;
using System.Linq;

namespace Minglu
{
    // A presentation snapshot only. None of these rows is part of the save model.
    public sealed class CommandWorkOverviewItem
    {
        public string title="",detail="";
        public int taskId=-1,actorId=-1,routeId=-1,slot=-1;
        public bool warning;
    }
    public sealed class CommandWorkOverview
    {
        public int authorizedCount,activeRouteCount;
        public string pauseReason="";
        public readonly List<CommandWorkOverviewItem> assigned=new List<CommandWorkOverviewItem>();
        public readonly List<CommandWorkOverviewItem> continuation=new List<CommandWorkOverviewItem>();
        public readonly List<CommandWorkOverviewItem> blocked=new List<CommandWorkOverviewItem>();
    }

    public static partial class CommandRules
    {
        static string WorkStage(CommandTask task)
        {
            if(task.reconnaissance)return "调查目标；本夜不推进执行阶段";
            return task.stepsRequired>1?"执行第"+(task.stepsDone+1)+" / "+task.stepsRequired+"阶段；"+(task.stepsDone+1<task.stepsRequired?"后续仍需另行签发":"成功后完成任务"):"执行目标";
        }
        static string WorkContinuation(CommandTask task)
        {
            if(task.stepsDone>0)return "已完成 "+task.stepsDone+" / "+task.stepsRequired+" 阶段；下一阶段尚未签发";
            if(task.reconComplete)return "调查已完成；正式执行尚未签发";
            return "上次未成；尚未重新签发";
        }
        static string WorkStaffingReason(RunState s,CommandTask task)
        {
            var candidates=s.agents.Where(a=>a.condition==Condition.Active).ToList();
            if(candidates.Count==0)return "没有仍在组织的可行动成员";
            var reasons=candidates.Select(a=>TaskError(s,a.id,task)).Where(e=>e!="").GroupBy(e=>e)
                .OrderByDescending(g=>g.Count()).Select(g=>g.Key).ToList();
            return string.Join("；",reasons.Take(2))+(reasons.Count>2?"；可打开任务比较其他限制":"");
        }
        static string WorkBacklog(CommandRoute route)
        {return "待接 "+route.waiting+" 份 / 中继 "+route.atRelay+" 份";}

        public static CommandWorkOverview BuildWorkOverview(RunState s)
        {
            var result=new CommandWorkOverview();
            // TaskError and SourceWorksTonight contain legacy Ensure* calls.
            // A summary must not initialize or migrate a partly loaded run.
            if(s==null||s.command==null||s.config==null||s.command.strategy==null||s.command.strategy.config==null||s.command.experienceRevision<1)
            {result.pauseReason="对局数据尚未初始化，请完成读档后再查看安排。";return result;}
            var c=s.command;
            result.authorizedCount=c.tasks.Count(t=>t.status==0&&t.actor>=0);
            result.activeRouteCount=c.routes.Count(r=>r.enabled);
            if(s.ended){result.pauseReason="本局已结束，可在复盘中查看结果。";return result;}
            if(c.eventKind!=0){result.pauseReason="先处理待决事件；当前指示和联络岗位暂不执行。";return result;}

            var validAssignments=new HashSet<int>();var reportedPeople=new HashSet<int>();
            foreach(var task in c.tasks.Where(t=>t.status==0).OrderBy(t=>t.deadline).ThenBy(t=>t.id))
            {
                if(task.actor>=0)
                {
                    string error=TaskError(s,task.actor,task);
                    var actor=s.agents.FirstOrDefault(a=>a.id==task.actor);
                    var row=new CommandWorkOverviewItem{title=(actor==null?"未找到负责人":actor.name)+" → "+task.title,taskId=task.id,actorId=task.actor,
                        detail=error==""?WorkStage(task)+"\n"+TaskTimingText(s,task):"授权暂不能执行："+error,warning=error!=""};
                    if(error==""){result.assigned.Add(row);validAssignments.Add(task.actor);}
                    else{result.blocked.Add(row);reportedPeople.Add(task.actor);}
                    continue;
                }
                if(!TaskCanFinish(s,task))
                {result.blocked.Add(new CommandWorkOverviewItem{title=task.title,taskId=task.id,detail=TaskTimingText(s,task)+"；现有阶段保留，当前窗口已不足。",warning=true});continue;}
                bool canStaff=CanStaffTask(s,task);
                bool needsContinuation=task.stepsDone>0||task.reconComplete||!string.IsNullOrEmpty(task.lastResult);
                if(needsContinuation)
                    result.continuation.Add(new CommandWorkOverviewItem{title=task.title,taskId=task.id,
                        detail=WorkContinuation(task)+"\n"+TaskTimingText(s,task)+(canStaff?"；可比较负责人。":"；当前无人可执行。"),warning=!canStaff||TaskUrgent(s,task)});
                if(!canStaff)
                    result.blocked.Add(new CommandWorkOverviewItem{title=task.title+" · 暂无人选",taskId=task.id,detail=WorkStaffingReason(s,task),warning=true});
                else if(TaskUrgent(s,task))
                    result.blocked.Add(new CommandWorkOverviewItem{title=task.title+" · 今夜最后开工",taskId=task.id,detail=TaskTimingText(s,task)+"；尚未签发，继续下一夜将失去完成窗口。",warning=true});
            }

            foreach(var route in c.routes)
            {
                string prefix="联络链 "+(route.id+1);int backlog=route.waiting+route.atRelay;
                if(!route.enabled)
                {
                    if(backlog>0)result.blocked.Add(new CommandWorkOverviewItem{title=prefix+" · 已暂停",routeId=route.id,
                        detail=WorkBacklog(route)+"；暂停仍继续计时，确认人员后可重新启用。",warning=route.age>=5});
                }
                else
                {
                    string routeError=RouteError(s,route);
                    if(routeError!="")result.blocked.Add(new CommandWorkOverviewItem{title=prefix+" · 岗位未就绪",routeId=route.id,detail=routeError,warning=true});
                    else if(c.pauseUntil>=s.turn)result.blocked.Add(new CommandWorkOverviewItem{title=prefix+" · 本夜休整",routeId=route.id,
                        detail="隐蔽或撤离安排生效，本夜不产出、接头或外送；"+WorkBacklog(route)+"，积压仍计时。",warning=backlog>0&&route.age>=5});
                    else for(int slot=0;slot<3;slot++)
                    {
                        if(!RouteSlotHasWork(s,route,slot))continue;
                        int actorId=slot==0?route.source:slot==1?route.liaison:route.courier;
                        string unavailable=Availability(s,actorId);
                        var assignment=validAssignments.Contains(actorId)?c.tasks.First(t=>t.status==0&&t.actor==actorId):null;
                        if(unavailable==""&&assignment==null)continue;
                        string action=slot==0?"产出":slot==1?"接头":"外送";
                        result.blocked.Add(new CommandWorkOverviewItem{title=Agent(s,actorId).name+" · "+prefix+" / "+action,routeId=route.id,slot=slot,actorId=actorId,
                            detail=unavailable!=""?unavailable+"；本夜"+action+"等待。":"优先执行「"+assignment.title+"」；本夜"+action+"等待，可撤回任务或更换岗位。",warning=true});
                        reportedPeople.Add(actorId);
                    }
                }
                if(backlog>0&&route.age>=5)
                    result.blocked.Add(new CommandWorkOverviewItem{title=prefix+" · 积压即将失效",routeId=route.id,
                        detail=WorkBacklog(route)+"；已积压 "+route.age+" 夜，请检查接头与外送。持续未送达会使积压失效。",warning=true});
            }
            foreach(var actor in s.agents.Where(a=>a.condition==Condition.Active||a.condition==Condition.Captured))
            {
                if(!reportedPeople.Contains(actor.id))
                {
                    string unavailable=Availability(s,actor.id);
                    if(unavailable!="")
                    {
                        var rescue=c.tasks.FirstOrDefault(t=>t.status==0&&t.operation==2&&t.target==actor.id);
                        result.blocked.Add(new CommandWorkOverviewItem{title=actor.name+" · 本夜不可派遣",actorId=actor.id,taskId=rescue==null?-1:rescue.id,
                            detail=unavailable,warning=actor.condition==Condition.Captured});
                    }
                }
                if(actor.condition!=Condition.Active||actor.exposure<c.strategy.config.warning)continue;
                var person=Person(s,actor.id);bool resting=person.policy==1||person.leaveUntil>=s.turn;
                string recovery=resting?"已安排休养，预计暴露降低10。"+(actor.exposure-10>=c.strategy.config.capture?"休养后仍达到捕获阈值，已被预警者仍可能被捕。":"休养仍需本夜结算后生效。"):
                    "建议换岗休养；达到捕获阈值且上夜已被预警者可能被捕。";
                result.blocked.Add(new CommandWorkOverviewItem{title=actor.name+" · 暴露 "+actor.exposure,actorId=actor.id,
                    detail=recovery,warning=true});
            }
            return result;
        }
    }
}
