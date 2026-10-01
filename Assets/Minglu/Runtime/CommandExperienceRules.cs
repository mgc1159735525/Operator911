using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public static partial class CommandRules
    {
        // Goals are snapshotted per run. Loading a previous run never changes its contract or RNG.
        public static void InitializeExperience(RunState s)
        {
            var c=s.command;bool shortRun=s.config.Value("turns")<=8;int difficulty=Mathf.Clamp(s.difficulty,0,2);
            c.experienceRevision=1;c.legacySourceSchedule=false;
            c.goalDeliveries=(shortRun?2:6)+difficulty;
            c.goalScore=shortRun?16+difficulty*4:30+difficulty*5;
            c.goalLevel=shortRun?2:3;c.secondGateDeliveries=shortRun?2:3+difficulty;
        }
        public static void EnsureExperience(RunState s)
        {
            var c=s.command;if(c.experienceRevision>=1)return;
            bool shortRun=s.config.Value("turns")<=8;
            c.goalDeliveries=(shortRun?3:7)+c.variant;
            c.goalScore=(shortRun?18:35)+c.variant*5+(s.difficulty-1)*5;
            c.goalLevel=shortRun?2:3;c.secondGateDeliveries=shortRun?2:4+c.variant;
            c.legacySourceSchedule=true;c.experienceRevision=1;
        }
        public static string NewRunGoalText(int turns,int difficulty)
        {
            bool shortRun=turns<=8;difficulty=Mathf.Clamp(difficulty,0,2);
            return "送达"+((shortRun?2:6)+difficulty)+"份 · 成果"+(shortRun?16+difficulty*4:30+difficulty*5)+" · 至少1人身份"+(shortRun?2:3)+"级";
        }
        public static int SecondGateDeliveries(RunState s){EnsureExperience(s);return s.command.secondGateDeliveries;}
        public static bool SourceWorksTonight(RunState s,CommandRoute r)
        {
            EnsureExperience(s);return s.turn%2==(s.command.legacySourceSchedule?r.id%2:(r.id+1)%2);
        }
        public static int BetrayalChance(CommandPerson p){return p==null?0:Mathf.RoundToInt((100-Mathf.Clamp(p.loyalty,0,100))*.6f);}
        public static int ActionStaminaCost(RunState s,Agent a){return Person(s,a.id).temper=="果断"?12:14;}
        public static string TemperEffect(CommandPerson p)
        {
            return p.temper=="谨慎"?"谨慎：行动基础暴露−2":p.temper=="热心"?"热心：主动政策下，上报机会概率35%（通常22%）":"果断：每次地下行动体力消耗−2";
        }
        static void GainSkillExperience(RunState s,Agent a,int skill)
        {
            if(a.skillExperience==null||a.skillExperience.Length!=4)a.skillExperience=new int[4];
            int value=new[]{a.intel,a.stealth,a.persuade,a.organize}[skill];
            if(value>=6){a.skillExperience[skill]=0;return;}
            if(++a.skillExperience[skill]<4)return;
            a.skillExperience[skill]=0;
            if(skill==0)a.intel++;else if(skill==1)a.stealth++;else if(skill==2)a.persuade++;else a.organize++;
            Log(s,a.name+"的"+Skills[skill]+"提升至"+(value+1)+"：任务和岗位成功都会积累经验。");
        }
        static int RouteMember(CommandRoute r,int slot){return slot==0?r.source:slot==1?r.liaison:r.courier;}
        static bool SlotCompromised(RunState s,CommandRoute r,int slot)
        {
            var a=s.agents.FirstOrDefault(x=>x.id==RouteMember(r,slot));
            return a!=null&&a.condition!=Condition.Active&&(r.compromisedSlots&(1<<slot))==0;
        }
        public static string RouteReplacementLoss(RunState s,CommandRoute r,int slot)
        {
            if(r==null||slot<0||slot>2)return "无效岗位";
            var previous=s.agents.FirstOrDefault(a=>a.id==RouteMember(r,slot));
            if(previous!=null&&previous.condition!=Condition.Active&&!SlotCompromised(s,r,slot))return "失陷损失已处理：剩余待接"+r.waiting+"份、中继"+r.atRelay+"份交接保留；已积压"+r.age+"夜继续计时。换岗后重新启用。";
            if(!SlotCompromised(s,r,slot))return "正常交接：保留待接"+r.waiting+"份、中继"+r.atRelay+"份；已积压"+r.age+"夜继续计时。换岗后重新启用。";
            int lost=slot==0?r.waiting:slot==1?r.waiting+r.atRelay:r.atRelay;
            return "原成员已失陷："+(slot==0?"待接":slot==1?"待接与中继":"中继")+"情报受损"+lost+"份；其他环节保留，换岗后重新启用。";
        }
        static void ApplySlotDamage(RunState s,CommandRoute r,int slot)
        {
            if(!SlotCompromised(s,r,slot))return;
            if(slot<=1)r.waiting=0;if(slot>=1)r.atRelay=0;r.compromisedSlots|=1<<slot;
        }
        static void DisruptRoutes(RunState s,int id,string reason)
        {
            foreach(var r in s.command.routes.Where(r=>new[]{r.source,r.liaison,r.courier}.Contains(id)))
            {
                int slot=r.source==id?0:r.liaison==id?1:2;
                // This entry point is a new Active -> compromised incident, including recapture after rescue.
                r.compromisedSlots&=~(1<<slot);string consequence=RouteReplacementLoss(s,r,slot);
                ApplySlotDamage(s,r,slot);r.enabled=false;r.last=reason+"；"+consequence;Log(s,"联络链"+(r.id+1)+"："+r.last);
            }
        }
        public static string ReplaceRouteMember(RunState s,CommandRoute r,int slot,int id)
        {
            if(s==null||s.command==null||r==null||!s.command.routes.Contains(r)||slot<0||slot>2)return "无效联络链或岗位";
            var a=s.agents.FirstOrDefault(x=>x.id==id);if(a==null||!s.command.people.Any(p=>p.id==id))return "成员不存在";
            string error=Availability(s,id);if(error!="")return error;
            if(slot==0&&(error=SourceError(s,id))!="")return error;
            for(int other=0;other<3;other++)if(other!=slot&&RouteMember(r,other)==id)return "三个岗位需要不同成员";
            if(s.command.routes.Any(other=>other!=r&&other.enabled&&new[]{other.source,other.liaison,other.courier}.Contains(id)))return "成员已在另一条启用链，请先完成原岗位交接";
            if(RouteMember(r,slot)==id)return "";
            string note=RouteReplacementLoss(s,r,slot);ApplySlotDamage(s,r,slot);
            if(slot==0)r.source=id;else if(slot==1)r.liaison=id;else r.courier=id;
            r.compromisedSlots&=~(1<<slot);r.enabled=false;r.last=a.name+"接任"+new[]{"内线","接头人","交通员"}[slot]+"；"+note;
            Log(s,"联络链"+(r.id+1)+"交接："+r.last);return "";
        }
        sealed class EventCandidate
        {
            public int kind,actor,weight;public string reason;
        }
        static void TryContextEvent(RunState s)
        {
            var c=s.command;if(s.turn<=3||Next(s,100)>=c.config.eventChance)return;
            var choices=new List<EventCandidate>();
            foreach(var p in c.people.Where(p=>Agent(s,p.id).condition==Condition.Active))
            {
                if(p.neglect>=2)choices.Add(new EventCandidate{kind=1,actor=p.id,weight=p.neglect*4,reason=p.code+"已连续"+p.neglect+"夜无暇处理「"+p.life+"」，申请一夜处理家事。"});
                if(p.loyalty<80)choices.Add(new EventCandidate{kind=3,actor=p.id,weight=100-p.loyalty,reason=p.code+"当前信任"+p.loyalty+"，对近期组织安排提出疑虑。继续留任的叛离风险为"+BetrayalChance(p)+"%；改善信任会降低风险。"});
            }
            var active=c.people.FirstOrDefault(p=>Agent(s,p.id).condition==Condition.Active);
            if(active!=null&&s.pressure>=25)choices.Add(new EventCandidate{kind=2,actor=active.id,weight=s.pressure-15,reason="敌方压力已达"+s.pressure+"，本区清查范围正在扩大。继续行动还是暂时分散休整？"});
            if(choices.Count==0)return;
            int roll=Next(s,choices.Sum(x=>x.weight));var chosen=choices[0];
            foreach(var choice in choices){roll-=choice.weight;if(roll<0){chosen=choice;break;}}
            c.eventKind=chosen.kind;c.eventActor=chosen.actor;c.eventText=chosen.reason;Log(s,"待决事件："+c.eventText);
        }
    }
}
