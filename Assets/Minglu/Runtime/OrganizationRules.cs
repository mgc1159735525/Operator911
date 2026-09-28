using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public enum OrganizationDuty { None, Recruit, Infiltrate, Deliver, ReceivePacket, SendPacket, ExpandNetwork, RelocateStation }
    [Serializable] public class OrganizationConfig
    {
        public int startingMoney=36, income=6, maintenance=2, neglectSuspicion=12, maintainReduction=6;
        public int recruitCost=12, capacity=6, restSpirit=18, actionSpirit=8, spySpirit=6;
        public int spyDuration=3, spyInterval=2, intelLifetime=3, packetValue=3;
    }
    [Serializable] public class RecruitCandidate
    {
        public int id, intel, stealth, portrait;
        public Appearance appearance;
        public string name, role, bio;
        public bool hired;
    }
    [Serializable] public class IntelPacket
    {
        public int id, source, district, expires, value;
        public string kind;
    }
    public static class OrganizationRules
    {
        public static OrganizationConfig LoadConfig()
        {
            var asset=Resources.Load<TextAsset>("Minglu/Config/organization_config");
            var c=asset==null?new OrganizationConfig():JsonUtility.FromJson<OrganizationConfig>(asset.text);ValidateConfig(c);return c;
        }
        public static void ValidateConfig(OrganizationConfig c)
        {
            if(c==null||c.startingMoney<0||c.startingMoney>1000||c.income<0||c.income>100||c.maintenance<0||c.maintenance>20||c.recruitCost<1||c.recruitCost>100||c.capacity<4||c.capacity>8||c.spyDuration<2||c.spyDuration>8||c.spyInterval<1||c.spyInterval>c.spyDuration||c.intelLifetime<1||c.intelLifetime>8||c.packetValue<1||c.packetValue>10||new[]{c.neglectSuspicion,c.maintainReduction,c.restSpirit,c.actionSpirit,c.spySpirit}.Any(v=>v<0||v>40))throw new InvalidOperationException("组织规则参数越界。");
        }
        public static void Initialize(RunState s)
        {
            s.organizationEnabled=true;s.organization=LoadConfig();s.money=s.organization.startingMoney;
            string[] names={"红绡","陆平","许清和"},roles={"药铺学徒","车夫","抄书人"};
            uint rng=s.initialSeed;
            for(int i=0;i<3;i++)
            {
                rng=rng*1664525u+1013904223u;
                s.candidates.Add(new RecruitCandidate{id=i,name=names[i],role=roles[i],bio="经联络人引荐加入。需要有人担任上线，先以日常身份在城中立足。",intel=2+(int)(rng%3),stealth=2+(int)((rng>>8)%3),portrait=i==0?1:i==1?2:0,appearance=AppearanceRules.ForRecruit(s.initialSeed,i)});
                var candidate=s.candidates.Last();
                string[] male={"陆平","许清和","程砚","周槐","沈知远","方朔"},female={"红绡","许晚晴","程素秋","周映雪","沈南枝","方玉宁"};
                candidate.name=(candidate.appearance.sex==0?male:female)[(int)((s.initialSeed+(uint)(i*7))%6)];
            }
        }
        public static int ReservedMoney(RunState s){return s.organizationEnabled?s.agents.Count(a=>a.condition==Condition.Active&&a.organizationDuty==OrganizationDuty.Recruit)*s.organization.recruitCost+CourierRules.ReservedMoney(s):0;}
        public static int Living(RunState s){return s.agents.Count(a=>a.condition!=Condition.Lost);}
        public static string Assign(RunState s,int id,OrganizationDuty duty,int district=0,int candidate=-1)
        {
            var a=s.agents.Find(x=>x.id==id);
            if(!s.organizationEnabled||s.ended||!string.IsNullOrEmpty(s.pendingEvent)||a==null||a.condition!=Condition.Active)return "当前成员或阶段无法安排组织行动。";
            if(!Enum.IsDefined(typeof(OrganizationDuty),duty)||duty==OrganizationDuty.None||!s.config.districts.Any(d=>d.id==district))return "组织行动或区域无效。";
            if(CourierRules.IsCourier(duty)||(s.courierEnabled&&duty==OrganizationDuty.Deliver))return "请在情报交通页按密件安排接收、外送或建立联络。";
            if(a.spirit<15)return "精神濒临崩溃，需要休整后再外出。";
            if(duty==OrganizationDuty.Recruit)
            {
                if(!s.candidates.Any(c=>c.id==candidate&&!c.hired))return "该候选人已加入或不存在。";
                if(s.agents.Any(x=>x.id!=id&&x.organizationDuty==duty&&x.candidateTarget==candidate))return "已有人负责接引此人。";
                int reservations=s.agents.Count(x=>x.id!=id&&x.organizationDuty==duty);
                if(Living(s)+reservations>=s.organization.capacity||s.agents.Count+reservations>=8)return "组织容量不足。";
            }
            if(duty==OrganizationDuty.Infiltrate&&(a.stamina<50||a.spirit<40||a.suspicion>=60))return "潜伏需要体力≥50、精神≥40、可疑度<60。";
            if(duty==OrganizationDuty.Infiltrate&&s.turn+s.organization.spyInterval>s.config.Value("turns"))return "剩余夜数不足以产出首份情报，请改派其他行动。";
            if(duty==OrganizationDuty.Deliver)
            {
                if(!s.packets.Any(p=>p.district==district&&p.expires>=s.turn))return "此区没有可递送的有效情报。";
                if(s.agents.Any(x=>x.id!=id&&x.organizationDuty==duty&&x.district==district))return "此区已有递送负责人。";
            }
            var old=a.organizationDuty;var oldOrder=a.order;
            a.organizationDuty=duty;a.order=Duty.Rest;
            if(PrototypeRules.UsedAP(s)>s.config.Value("ap")||ReservedMoney(s)>s.money){a.organizationDuty=old;a.order=oldOrder;return "行动点或银两不足（招募费用需预留）。";}
            a.district=district;a.candidateTarget=candidate;a.target=-1;a.mission="";a.site="";a.courierTarget=-1;
            return "";
        }
        public static string Maintain(RunState s,int id)
        {
            var a=s.agents.Find(x=>x.id==id);
            if(!s.organizationEnabled||s.ended||!string.IsNullOrEmpty(s.pendingEvent)||a==null||a.condition!=Condition.Active)return "当前无法调整身份维持。";
            a.maintainCover=!a.maintainCover;return "";
        }
        public static string Recall(RunState s,int id)
        {
            var a=s.agents.Find(x=>x.id==id);
            if(s.ended||!string.IsNullOrEmpty(s.pendingEvent)||a==null||a.condition!=Condition.Undercover)return "此人目前不在潜伏。";
            a.recall=!a.recall;
            if(PrototypeRules.UsedAP(s)>s.config.Value("ap")){a.recall=false;return "接应撤回需要1行动点。";}
            return "";
        }
        public static string OrderName(RunState s,Agent a)
        {
            if(CourierRules.IsCourier(a.organizationDuty))return CourierRules.OrderName(a.organizationDuty);
            if(a.condition==Condition.Undercover)return a.recall?"接应撤回":"潜伏 "+a.spyNights+"夜";
            return a.organizationDuty==OrganizationDuty.None?s.config.Action(a.order).name:a.organizationDuty==OrganizationDuty.Recruit?"接引新成员":a.organizationDuty==OrganizationDuty.Infiltrate?"进入长期潜伏":"递送情报";
        }
        public static bool Execute(RunState s,Agent a)
        {
            if(CourierRules.Execute(s,a))return true;
            if(!s.organizationEnabled||a.organizationDuty==OrganizationDuty.None)return false;
            a.spirit=Math.Max(0,a.spirit-s.organization.actionSpirit);a.stamina=Math.Max(0,a.stamina-10);
            if(a.organizationDuty==OrganizationDuty.Recruit)
            {
                var c=s.candidates.First(x=>x.id==a.candidateTarget);
                if(!c.hired&&Living(s)<s.organization.capacity&&s.agents.Count<8&&s.money>=s.organization.recruitCost)
                {
                    s.money-=s.organization.recruitCost;c.hired=true;int id=s.agents.Max(x=>x.id)+1;
                    s.config.agents.Add(new AgentRow{id=id,name=c.name,role=c.role,bio=c.bio,intel=c.intel,stealth=c.stealth,portrait=c.portrait,handler=a.id});
                    s.agents.Add(new Agent{id=id,name=c.name,intel=c.intel,stealth=c.stealth,handler=a.id,district=a.district,appearance=AppearanceRules.Copy(c.appearance??AppearanceRules.ForRecruit(s.initialSeed,c.id))});
                    CampaignRules.Recruit(s,s.agents.Last(),c.role);
                    Log(s,a.name+"接引"+c.name+"加入，银两-"+s.organization.recruitCost+"。下夜可派遣，上线为"+a.name+"。");
                }
            }
            else if(a.organizationDuty==OrganizationDuty.Infiltrate)
            {
                a.condition=Condition.Undercover;a.spyStarted=s.turn;a.spyNights=0;
                Log(s,a.name+"进入"+s.config.District(a.district).name+"潜伏，期间不能执行普通任务，可安排接应撤回。");
            }
            else
            {
                var packets=s.packets.Where(p=>p.district==a.district&&p.expires>=s.turn).ToList();
                int value=packets.Sum(p=>p.value);s.intelligence+=value;
                foreach(var p in packets)s.packets.Remove(p);
                a.exposure=Math.Min(100,a.exposure+10);
                Log(s,a.name+"递送"+packets.Count+"份情报，转换为"+value+"份可用情报；暴露+10。");
            }
            return true;
        }
        public static void EndNight(RunState s,List<Agent> activeAtStart)
        {
            if(!s.organizationEnabled)return;
            var c=s.organization;
            foreach(var p in s.packets.Where(p=>p.expires<=s.turn).ToList()){s.packets.Remove(p);Log(s,p.kind+"情报到期作废；区域："+s.config.District(p.district).name+"。");}
            foreach(var a in activeAtStart)
            {
                if(a.organizationDuty==OrganizationDuty.None)a.spirit=Math.Max(0,Math.Min(100,a.spirit+(a.order==Duty.Rest?c.restSpirit:-c.actionSpirit)));
                if(a.condition==Condition.Undercover)continue;
                if(a.maintainCover&&s.money>=c.maintenance){s.money-=c.maintenance;a.suspicion=Math.Max(0,a.suspicion-c.maintainReduction);}
                else{a.suspicion=Math.Min(100,a.suspicion+c.neglectSuspicion);Log(s,a.name+"未维持表身份，可疑度+"+c.neglectSuspicion+"。");}
                if(a.suspicion>=60){a.exposure=Math.Min(100,a.exposure+10);Log(s,a.name+"身份遭到盘查，暴露+10。");}
                if(a.suspicion>=100)a.exposure=100;
            }
            foreach(var a in s.agents.Where(a=>a.condition==Condition.Undercover).ToList())
            {
                if(a.spyStarted==s.turn)continue;
                if(a.recall){a.condition=Condition.Active;a.recall=false;a.consecutiveShifts=0;Log(s,a.name+"在接应下撤回，本夜不再执行其他行动。");continue;}
                a.spyNights++;a.spirit=Math.Max(0,a.spirit-c.spySpirit);a.stamina=Math.Max(0,a.stamina-5);a.exposure=Math.Min(100,a.exposure+8);a.suspicion=Math.Min(100,a.suspicion+4);
                if(a.exposure>=s.config.Value("captureThreshold")||a.suspicion>=100)
                {
                    s.packets.RemoveAll(p=>p.source==a.id);if(s.courierEnabled)foreach(var packet in s.dispatchPackets.Where(p=>p.source==-a.id-1&&p.status==DispatchStatus.Waiting))packet.status=DispatchStatus.Expired;PrototypeRules.Capture(s,a);Log(s,"潜伏被识破，尚未接回的消息失效；已入站密件仍可外送。");continue;
                }
                if(a.spyNights%c.spyInterval==0)
                {
                    string kind=a.district==2||a.district==3?"军事":"治安";
                    if(s.courierEnabled)CourierRules.FromSpy(s,a);
                    else s.packets.Add(new IntelPacket{id=s.nextPacketId++,source=a.id,district=a.district,expires=s.turn+c.intelLifetime,value=c.packetValue,kind=kind});
                    if(!s.courierEnabled)Log(s,a.name+"留下"+kind+"情报，需另派成员到"+s.config.District(a.district).name+"递送；第"+(s.turn+c.intelLifetime)+"夜结束失效。");
                }
                if(a.spyNights>=c.spyDuration||a.spirit<15){a.condition=Condition.Active;a.consecutiveShifts=0;Log(s,a.name+"结束潜伏归队，下夜可再次派遣。");}
            }
            s.money+=c.income;Log(s,"日常经营收入+"+c.income+"银两，结余"+s.money+"。本夜新产出的情报下夜才能递送。");
        }
        public static void ValidateSave(RunState s)
        {
            if(!s.organizationEnabled)return;
            ValidateConfig(s.organization);
            if(s.candidates!=null)foreach(var c in s.candidates)if(c!=null)AppearanceRules.Validate(c.appearance);
            if(s.money<0||s.candidates==null||s.packets==null||s.nextPacketId<0||s.agents.Any(a=>a.spirit<0||a.spirit>100||a.suspicion<0||a.suspicion>100||!Enum.IsDefined(typeof(OrganizationDuty),a.organizationDuty)||a.spyNights<0||a.spyNights>s.organization.spyDuration||a.spyStarted<0||a.spyStarted>s.turn))throw new InvalidOperationException("组织存档状态不合法。");
            if(s.candidates.Any(c=>c==null)||s.candidates.Select(c=>c.id).Distinct().Count()!=s.candidates.Count||s.packets.Any(p=>p==null||p.value<1||p.value>10||!s.agents.Any(a=>a.id==p.source)||!s.config.districts.Any(d=>d.id==p.district)||p.id<0||p.id>=s.nextPacketId)||s.packets.Select(p=>p.id).Distinct().Count()!=s.packets.Count)throw new InvalidOperationException("候选人或情报引用无效。");
            if(ReservedMoney(s)>s.money||s.agents.Any(a=>a.organizationDuty!=OrganizationDuty.None&&(a.condition!=Condition.Active||a.order!=Duty.Rest||!string.IsNullOrEmpty(a.mission)))||s.agents.Any(a=>a.recall&&a.condition!=Condition.Undercover))throw new InvalidOperationException("组织派遣存档无效。");
            if(s.agents.Any(a=>a.organizationDuty==OrganizationDuty.Recruit&&!s.candidates.Any(c=>c.id==a.candidateTarget&&!c.hired)))throw new InvalidOperationException("招募目标无效。");
        }
        static void Log(RunState s,string message){s.journal.Add("["+s.turn+"] "+message);}
    }
}
