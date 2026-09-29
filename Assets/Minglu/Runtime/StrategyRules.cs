using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Minglu
{
    [Serializable] public class StrategyConfig
    {
        public int warning=65,capture=85,raid=70,graceNight=4,rescueWindow=4,allianceThreshold=50,allianceGain=25,relocateCost=10;
    }
    [Serializable] public class CommandStrategy
    {
        public int version=1,trace,raidCooldown,baseDistrict,hearts=30,raids;
        public bool raidPending;
        public int[] alliances=new int[4];
        public StrategyConfig config;
    }
    public static partial class CommandRules
    {
        public static readonly string[] Factions={"商会","街坊互助会","书院","渡口行会"};
        public static readonly string[] AllianceEffects={"每夜经费+4","每夜民心+2、压力−2","情报类任务+1骰","潜行类任务+1骰"};
        public static void EnsureStrategy(RunState s)
        {
            if(s.command.strategy!=null)return;
            var asset=Resources.Load<TextAsset>("Minglu/Config/strategy_config");
            s.command.strategy=new CommandStrategy{baseDistrict=s.config.districts[0].id,config=JsonUtility.FromJson<StrategyConfig>(asset.text)};
            // Old saves gain a full warning window, never an immediate arrest on load.
            foreach(var p in s.command.people){p.warningTurn=-1;p.capturedTurn=-1;}
        }
        public static void ValidateStrategy(RunState s)
        {
            EnsureStrategy(s);var x=s.command.strategy;var c=x.config;
            if(x.version!=1||x.alliances==null||x.alliances.Length!=4||x.alliances.Any(v=>v<0||v>100)||x.trace<0||x.trace>100||x.hearts<0||x.hearts>100||c==null||c.warning<1||c.capture<=c.warning||c.capture>100||c.raid<10||c.raid>100||c.graceNight<2||c.rescueWindow<2||c.rescueWindow>8||c.allianceThreshold<1||c.allianceThreshold>100||c.allianceGain<1||c.allianceGain>50||c.relocateCost<0||c.relocateCost>50||!s.config.districts.Any(d=>d.id==x.baseDistrict))throw new Exception("地区经营数据无效");
            foreach(var t in s.command.tasks)if(t.stepsRequired<1||t.stepsRequired>3||t.stepsDone<0||t.stepsDone>t.stepsRequired||t.operation<0||t.operation>3||t.operation==1&&(t.target<0||t.target>3)||t.operation==2&&!s.agents.Any(a=>a.id==t.target))throw new Exception("多阶段任务数据无效");
        }
        public static int AllianceDice(RunState s,CommandTask t)
        {
            EnsureStrategy(s);var x=s.command.strategy;
            return (t.skill==0&&x.alliances[2]>=x.config.allianceThreshold||t.skill==1&&x.alliances[3]>=x.config.allianceThreshold)?1:0;
        }
        public static string OperationReward(CommandTask t)
        {return t.operation==1?" / 联盟信任提升":t.operation==2?" / 救回被捕成员":t.operation==3?" / 民心+10、追查−8":"";}
        public static string StartCivicTask(RunState s,int faction)
        {
            EnsureStrategy(s);if(s.ended||s.command.eventKind!=0)return "先处理待决事件或结束界面";
            if(faction < -1||faction>3)return "未知地区事务";
            int operation=faction<0?3:1;
            if(s.command.tasks.Any(t=>t.status==0&&t.operation==operation&&t.target==faction))return "已有同类委托，请前往任务指挥安排负责人";
            if(faction>=0&&s.command.strategy.alliances[faction]>=100)return "该势力信任已满";
            if(s.command.tasks.Count(t=>t.status==0)>=9)return "待办已满9项，先完成现有事项";
            var template=new CommandTemplate{title=faction<0?"街坊互助行动":"联络"+Factions[faction],body=faction<0?"安排成员处理街坊困难，民心+10、追查−8。":"两次成功后信任+"+s.command.strategy.config.allianceGain+"，达"+s.command.strategy.config.allianceThreshold+"解锁长期收益。每次需另行授权，可换人。",category=faction<0?"民心":"联盟",skill=faction<0?3:2,required=1,reward=3,money=0};
            CreateTask(s,template,"地区事务",-1,s.command.strategy.baseDistrict);var t=s.command.tasks.Last();t.operation=operation;t.target=faction;t.stepsRequired=faction<0?1:2;t.deadline=Math.Min(s.config.Value("turns"),s.turn+5);
            return "";
        }
        static void CompleteOperation(RunState s,CommandTask t)
        {
            var x=s.command.strategy;
            if(t.operation==1){int old=x.alliances[t.target];x.alliances[t.target]=Math.Min(100,old+x.config.allianceGain);Log(s,Factions[t.target]+"信任 "+old+" → "+x.alliances[t.target]+"；达到"+x.config.allianceThreshold+"解锁："+AllianceEffects[t.target]);}
            if(t.operation==3){x.hearts=Math.Min(100,x.hearts+10);x.trace=Math.Max(0,x.trace-8);}
            if(t.operation==2)
            {
                var a=Agent(s,t.target);if(a.condition!=Condition.Captured)return;
                a.condition=Condition.Active;a.exposure=35;a.stamina=Math.Max(40,a.stamina);a.spirit=Math.Max(40,a.spirit);
                var p=Person(s,a.id);p.capturedTurn=-1;p.warningTurn=-1;p.leaveUntil=s.turn+1;p.loyalty=Math.Min(100,p.loyalty+10);
                Log(s,a.name+"获救，休养至第"+(p.leaveUntil+1)+"夜恢复；原联络链保持暂停，请自行重新启用。");
            }
        }
        public static void CaptureForCommand(RunState s,int id)
        {
            EnsureStrategy(s);var a=Agent(s,id);if(a.condition!=Condition.Active)return;var p=Person(s,id);a.condition=Condition.Captured;p.capturedTurn=s.turn;p.resisted=0;
            foreach(var t in s.command.tasks.Where(t=>t.status==0&&t.actor==id))t.actor=-1;
            foreach(var r in s.command.routes.Where(r=>new[]{r.source,r.liaison,r.courier}.Contains(id))){r.enabled=false;r.last=a.name+"被捕，此线暂停；积压继续计时，请替换岗位或营救。";}
            CreateTask(s,new CommandTemplate{title="营救"+a.name,body="先核实关押线索，再组织撤离。两次成功后归队；每夜需重新授权，可以更换负责人。",category="紧急救援",skill=1,required=1,reward=5,money=0},"成员被捕",id,s.command.strategy.baseDistrict);
            var rescue=s.command.tasks.Last();rescue.operation=2;rescue.target=id;rescue.stepsRequired=2;rescue.deadline=Math.Min(s.config.Value("turns"),s.turn+s.command.strategy.config.rescueWindow);
            Notice(s,new CommandNotice{title="成员被捕 · 尚有救援窗口",body=a.name+"被捕。已撤回其任务并释放预算。\n营救需两次成功，第"+rescue.deadline+"夜截止；可在任务页比较可用人选。",tab=0,task=rescue.id,actor=s.agents.FirstOrDefault(w=>w.condition==Condition.Active)?.id??id});
            Log(s,a.name+"被捕，生成两阶段营救；救援不占普通任务名额限制。");
        }
        static void StrategyNight(RunState s,HashSet<int> busy)
        {
            EnsureStrategy(s);var x=s.command.strategy;var cfg=x.config;
            if(x.alliances[0]>=cfg.allianceThreshold){s.money+=4;Log(s,"商会支援：经费+4。");}
            if(x.alliances[1]>=cfg.allianceThreshold){x.hearts=Math.Min(100,x.hearts+2);s.pressure=Math.Max(0,s.pressure-2);}
            int delta=busy.Count==0?-5:busy.Count+(s.pressure>=60?2:0);if(x.hearts>=60)delta-=2;
            x.trace=Mathf.Clamp(x.trace+delta,0,100);
            if(s.turn>=cfg.graceNight)
            foreach(var p in s.command.people.ToList())
            {
                var a=Agent(s,p.id);
                if(a.condition==Condition.Captured)
                {
                    var rescue=s.command.tasks.FirstOrDefault(t=>t.operation==2&&t.target==a.id&&t.status==0);
                    if(rescue==null){a.condition=Condition.Lost;Log(s,a.name+"救援窗口结束，永久离队。");Notice(s,new CommandNotice{title="救援窗口结束",body=a.name+"已失去联系。检查失效岗位，其他联络链仍可继续工作。",tab=1});continue;}
                    if(s.turn>p.capturedTurn&&Next(s,100)>=p.loyalty)
                    {
                        foreach(var r in s.command.routes.Where(r=>new[]{r.source,r.liaison,r.courier}.Contains(a.id)))foreach(int other in new[]{r.source,r.liaison,r.courier}.Where(id=>id>=0&&id!=a.id).Distinct())Agent(s,other).exposure=Math.Min(100,Agent(s,other).exposure+8);
                        x.trace=Math.Min(100,x.trace+5);Log(s,a.name+"未能守住口供：只牵连其认识的同线成员，暴露+8；追查+5。");
                    }else if(s.turn>p.capturedTurn){p.resisted++;Log(s,a.name+"守住了本夜审讯，仍待营救。");}
                    continue;
                }
                if(a.condition!=Condition.Active)continue;
                if(a.exposure<cfg.warning){p.warningTurn=-1;continue;}
                if(p.warningTurn<0){p.warningTurn=s.turn;Notice(s,new CommandNotice{title="成员追查预警",body=a.name+"暴露已达"+a.exposure+"。达到"+cfg.capture+"且下夜仍未脱险将被捕。\n可安排生活优先，并让机动成员接替岗位；休养每夜降低10暴露。",tab=2,actor=a.id});}
                else if(a.exposure>=cfg.capture&&p.warningTurn<s.turn)CaptureForCommand(s,a.id);
            }
            if(x.trace>=cfg.raid&&s.turn>=x.raidCooldown&&s.turn>=cfg.graceNight)x.raidPending=true;
        }
        static void ArmRaid(RunState s)
        {
            var x=s.command.strategy;if(x==null||!x.raidPending||s.command.eventKind!=0||s.ended)return;
            s.command.eventKind=4;s.command.eventActor=s.agents.First(a=>a.condition==Condition.Active).id;
            s.command.eventText="据点周边出现持续蹲守。请在下一夜行动前安排撤离，避免让预警直接变成无提示损失。";
        }
        public static string ResolveRaid(RunState s,bool relocate)
        {
            if(s.command.eventKind!=4)return "当前没有待处理的据点撤离";
            var x=s.command.strategy;int cost=x.config.relocateCost;
            if(relocate&&s.money-ReservedMoney(s)<cost)return "未预留经费不足；可选择销毁中继情报并撤离";
            int lost=0;if(relocate)s.money-=cost;else foreach(var r in s.command.routes){lost+=r.atRelay;r.atRelay=0;}
            int index=s.config.districts.FindIndex(d=>d.id==x.baseDistrict);x.baseDistrict=s.config.districts[(index+1)%s.config.districts.Count].id;
            x.trace=Math.Max(0,x.trace-45);x.raidCooldown=s.turn+3;x.raidPending=false;x.raids++;s.command.pauseUntil=s.turn;
            s.command.eventKind=0;s.command.eventText="";
            string result=relocate?"支出"+cost+"银两，保留情报。":"销毁中继情报"+lost+"份，无银两消耗。";
            Log(s,result+"据点已转移；追查−45，链条本夜休整。");Notice(s,new CommandNotice{title="据点已撤离",body=result+"\n新据点："+s.config.District(x.baseDistrict).name+"。追查降低45，今夜链条暂停，任务仍可执行。",tab=5},true);return "";
        }
        public static string NightWarnings(RunState s)
        {
            var rows=new List<string>();
            foreach(var t in s.command.tasks.Where(t=>t.status==0&&t.deadline==s.turn))rows.Add("今夜截止："+t.title+(t.actor<0?"（尚未授权）":""));
            foreach(var r in s.command.routes.Where(r=>r.enabled))
            {
                var conflict=s.command.tasks.Where(t=>t.status==0&&t.actor>=0&&new[]{r.source,r.liaison,r.courier}.Contains(t.actor)).Select(t=>Agent(s,t.actor).name).Distinct().ToArray();
                if(conflict.Length>0)rows.Add("链"+(r.id+1)+"岗位等待："+string.Join("、",conflict)+"优先执行任务");
                if(r.age>=5&&r.waiting+r.atRelay>0)rows.Add("链"+(r.id+1)+"积压即将失效，请检查接头与外送岗位");
            }
            foreach(var a in s.agents.Where(a=>a.condition==Condition.Active&&a.exposure>=s.command.strategy.config.warning))rows.Add(a.name+"暴露"+a.exposure+"：建议换岗休养");
            return rows.Count==0?"没有紧急风险。空闲成员将自动工作与休养。":string.Join("\n",rows);
        }
    }
}
