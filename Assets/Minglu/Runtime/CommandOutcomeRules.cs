using System.Collections.Generic;
using System.Linq;

namespace Minglu
{
    public static partial class CommandRules
    {
        // Per-resolution observations, not another save schema or RNG stream.
        sealed class TaskOutcome { public int task;public string body; }
        sealed class NightOutcomes
        {
            public int night,firstTask,delivered,completed,gate;
            public Dictionary<int,int> levels,actors,statuses;
            public readonly List<TaskOutcome> results=new List<TaskOutcome>();
        }
        static NightOutcomes CaptureNightOutcomes(RunState s)
        {
            var c=s.command;
            return new NightOutcomes{night=s.turn,firstTask=c.nextTask,delivered=c.delivered,completed=c.completed,gate=c.gate,
                levels=c.people.ToDictionary(p=>p.id,p=>p.level),actors=c.tasks.ToDictionary(t=>t.id,t=>t.actor),statuses=c.tasks.ToDictionary(t=>t.id,t=>t.status)};
        }
        static void RecordWithdrawnOutcomes(RunState s,NightOutcomes before)
        {
            foreach(var t in s.command.tasks.Where(t=>before.actors.ContainsKey(t.id)&&before.actors[t.id]>=0&&before.statuses[t.id]==0&&t.actor<0))
            {
                int actor=before.actors[t.id];string reason=Availability(s,actor);
                before.results.Add(new TaskOutcome{task=t.id,body=Agent(s,actor).name+" · 「"+t.title+"」未执行："+(reason==""?"授权条件已变化":reason)+"。\n未扣除任务投入，原授权已撤回。"});
            }
        }
        static string TaskOutcomeNext(RunState s,CommandTask task)
        {
            if(s.ended)return "本局已结束，可在结局复盘中回看判定。";
            if(task.status==2)return "此任务已截止，不能继续授权；请安排其他目标。";
            if(!TaskCanFinish(s,task))return "剩余夜数不足以完成此任务，请安排其他目标。";
            if(task.reconComplete)return "下一步：授权执行目标，调查优势已保留；第"+task.deadline+"夜截止。";
            return "下一步：重新比较人选并签发；既有阶段保留，第"+task.deadline+"夜截止。";
        }
        static void PublishNightOutcomes(RunState s,NightOutcomes before)
        {
            var c=s.command;var messages=new List<string>();
            foreach(var outcome in before.results)
            {
                var task=c.tasks.First(t=>t.id==outcome.task);
                string expiry=task.status==2?"\n本夜任务到期：额外压力+3。":"";
                messages.Add(outcome.body+expiry+"\n"+TaskOutcomeNext(s,task));
            }
            var fresh=c.tasks.Where(t=>t.id>=before.firstTask).ToList();
            foreach(var t in fresh)messages.Add(t.origin+"：「"+t.title+"」 · 第"+t.deadline+"夜截止");
            foreach(var p in c.people.Where(p=>before.levels.ContainsKey(p.id)&&p.level>before.levels[p.id]))messages.Add(Agent(s,p.id).name+"的表身份升至 Lv."+p.level+"，可承担更高门槛任务。");
            if(c.delivered>before.delivered)messages.Add("情报送达 "+(c.delivered-before.delivered)+"份，上级已收到。成果与银两已结算。");
            if(c.completed>before.completed)foreach(var t in c.tasks.Where(t=>t.status==1&&before.statuses.ContainsKey(t.id)&&before.statuses[t.id]==0))
                messages.Add(Agent(s,before.actors[t.id]).name+"完成「"+t.title+"」："+TaskReward(t)+"。"+(s.ended?"已记入本局复盘。":"可重新安排该成员。"));
            foreach(var t in c.tasks.Where(t=>t.status==2&&before.statuses.ContainsKey(t.id)&&before.statuses[t.id]==0&&!before.results.Any(r=>r.task==t.id)))
                messages.Add("任务到期：「"+t.title+"」，压力+3；此目标不能再授权。");
            if(c.gate>before.gate)messages.Add("阶段目标达成，新的上级要求已更新。");
            if(messages.Count==0)return;
            var retry=before.results.Select(r=>c.tasks.First(t=>t.id==r.task)).FirstOrDefault(t=>t.status==0&&TaskCanFinish(s,t));
            int taskId=s.ended?-1:retry!=null?retry.id:fresh.Count>0?fresh[0].id:-1;
            Notice(s,new CommandNotice{title="夜间要报 · 第"+before.night+"夜",night=before.night,body=string.Join("\n\n",messages),tab=taskId>=0?0:4,task=taskId});
        }
        public static string RouteFailureRetryText(RunState s,CommandRoute route,int slot)
        {
            if(s.turn>=s.config.Value("turns"))return "本局已无后续行动夜，不会自动重试";
            if(slot!=0)return "下夜自动重试（若仍启用且成员可出勤）";
            // A failed observation already used this period. Both the current
            // and legacy schedules observe only every other night.
            int next=s.turn+1;
            int parity=s.command.legacySourceSchedule?route.id%2:(route.id+1)%2;
            if(next%2!=parity)next++;
            return next>s.config.Value("turns")?"本局已无后续观察周期，不会自动重试":"第"+next+"夜进入下个观察周期，再自动尝试产出（若仍启用且成员可出勤）";
        }
    }
}
