using System;
using System.Linq;

namespace Minglu
{
    public static partial class CommandRules
    {
        // One timing vocabulary for the map, task list and command guidance.
        public static int TaskNightsLeft(RunState s,CommandTask task)
        {return Math.Max(0,Math.Min(task.deadline,s.config.Value("turns"))-s.turn+1);}
        public static int TaskAttemptsNeeded(CommandTask task)
        {return Math.Max(1,task.stepsRequired-task.stepsDone)+(task.reconnaissance?1:0);}
        public static bool TaskCanFinish(RunState s,CommandTask task)
        {return task!=null&&task.status==0&&TaskNightsLeft(s,task)>=TaskAttemptsNeeded(task);}
        public static bool TaskUrgent(RunState s,CommandTask task)
        {return TaskCanFinish(s,task)&&TaskNightsLeft(s,task)==TaskAttemptsNeeded(task);}
        public static string TaskTimingText(RunState s,CommandTask task)
        {
            if(task.status!=0)return task.status==1?"已完成":"已结束";
            int nights=TaskNightsLeft(s,task),attempts=TaskAttemptsNeeded(task);
            if(nights<attempts)return "来不及 · 余"+nights+"夜 / 至少需"+attempts+"夜";
            if(nights==attempts)return (task.actor<0?"今夜必须开始":"今夜按计划执行")+" · 无重试余量";
            return "余"+nights+"夜 · 最少"+attempts+"夜";
        }
        public static bool CanStaffTask(RunState s,CommandTask task)
        {
            // TaskError normally ensures strategy initialization. A read-only
            // query must not initialize or migrate a partially loaded save.
            return s!=null&&s.command!=null&&s.command.strategy!=null&&TaskCanFinish(s,task)
                &&s.agents.Any(a=>TaskError(s,a.id,task)=="");
        }
        public static bool RouteSlotHasWork(RunState s,CommandRoute route,int slot)
        {
            if(s==null||s.command==null||route==null||slot<0||slot>2||!route.enabled||s.command.pauseUntil>=s.turn||RouteError(s,route)!="")return false;
            return slot==0?SourceWorksTonight(s,route):slot==1?route.waiting>0:route.atRelay>0;
        }
    }
}
