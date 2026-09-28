using System;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public static partial class CommandRules
    {
        public static readonly string[] Plans={"常规","筹备","紧急"};
        public static int PlanCost(CommandTask t){return t.plan==1?4:0;}
        public static int ReservedMoney(RunState s,int except=-1){return s.command.tasks.Where(t=>t.status==0&&t.actor>=0&&t.id!=except).Sum(PlanCost);}
        public static int ReservedIntel(RunState s,int except=-1){return s.command.tasks.Count(t=>t.status==0&&t.actor>=0&&t.id!=except&&t.intelSupport);}
        public static int DistrictPenalty(RunState s,CommandTask t){return t.level<=1?0:Mathf.Clamp(s.config.District(t.district).difficulty,0,1);}
        public static int TaskDice(RunState s,Agent a,CommandTask t){return Math.Max(1,Dice(s,a,t.skill)+(t.plan==0?0:1)+(t.intelSupport?1:0)-DistrictPenalty(s,t));}
        public static int TaskExposure(RunState s,Agent a,CommandTask t){return Math.Max(1,(Person(s,a.id).temper=="谨慎"?3:5)+Mathf.Clamp(s.config.District(t.district).risk,0,6)+(t.plan==1?-2:t.plan==2?8:0));}
        public static string TaskReward(CommandTask t){return "成果+"+t.reward+" / 银两+"+t.money+(t.intelReward>0?" / 情报+"+t.intelReward:"")+(t.pressureRelief>0?" / 压力−"+t.pressureRelief:"");}
        public static string TaskResourceError(RunState s,CommandTask t)
        {
            if(s.money-ReservedMoney(s,t.id)<PlanCost(t))return "筹备需4银两（已授权任务的预算已预留）；撤回其他授权或改常规方案";
            if(t.intelSupport&&s.intelligence-ReservedIntel(s,t.id)<1)return "缺少未预留情报；关闭情报支援，或等待联络链送达";
            return "";
        }
        public static void CreateTask(RunState s,CommandTemplate template,string origin,int source,int district)
        {
            s.command.tasks.Add(new CommandTask{id=s.command.nextTask++,title=template.title,body=template.body,origin=origin,source=source,skill=template.skill,required=template.required,level=template.level,profession=template.profession,reward=template.reward,money=template.money,district=district,deadline=Math.Min(s.config.Value("turns"),s.turn+3+Next(s,3)),category=template.category,followup=template.followup,intelReward=template.intelReward,pressureRelief=template.pressureRelief,failExposure=template.failExposure});
        }
        static void SpawnFollowups(RunState s)
        {
            foreach(var parent in s.command.tasks.Where(t=>t.status==1&&!t.followupSpawned&&!string.IsNullOrEmpty(t.followup)).ToList())
            {
                if(s.command.tasks.Count(t=>t.status==0)>=9)break;
                var next=s.command.config.templates.FirstOrDefault(t=>t.title==parent.followup);
                if(next==null){parent.followupSpawned=true;continue;}
                CreateTask(s,next,"线索推进："+parent.title,parent.source,parent.district);parent.followupSpawned=true;
                Log(s,"调查有了进展：「"+parent.title+"」解锁「"+next.title+"」，目标地区保持一致。");
            }
        }
        static void UpgradeMissions(RunState s)
        {
            if(s.command.missionRevision>=1)return;
            var updated=JsonUtility.FromJson<CommandConfig>(Resources.Load<TextAsset>("Minglu/Config/command_config").text);
            s.command.config.templates=updated.templates;
            foreach(var t in s.command.tasks.Where(t=>t.status==0))
            {
                var match=updated.templates.FirstOrDefault(x=>x.title==t.title);if(match==null)continue;
                t.category=match.category;t.followup=match.followup;t.intelReward=match.intelReward;t.pressureRelief=match.pressureRelief;t.failExposure=match.failExposure;
            }
            s.command.missionRevision=1;
        }
        static void ValidateMissionTemplates(CommandConfig c)
        {
            if(c.templates.Select(t=>t.title).Distinct().Count()!=c.templates.Count)throw new Exception("任务标题必须唯一");
            foreach(var t in c.templates)
            {
                if(t.intelReward<0||t.intelReward>5||t.pressureRelief<0||t.pressureRelief>20||t.failExposure<0||t.failExposure>20)throw new Exception("任务后果字段超出范围");
                var seen=new System.Collections.Generic.HashSet<string>();var cursor=t;
                while(!string.IsNullOrEmpty(cursor.followup))
                {
                    if(!seen.Add(cursor.title))throw new Exception("后续任务形成循环");
                    cursor=c.templates.FirstOrDefault(x=>x.title==cursor.followup);if(cursor==null)throw new Exception("找不到后续任务");
                }
            }
            if(!c.templates.Any(t=>!t.followupOnly&&t.level==1&&string.IsNullOrEmpty(t.profession)))throw new Exception("需保留可自由生成的入门任务");
        }
    }
}
