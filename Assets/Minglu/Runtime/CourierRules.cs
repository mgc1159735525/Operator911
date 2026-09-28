using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public enum DispatchStatus { Waiting, Received, Delivered, Expired }
    [Serializable] public class SourceContact { public int id,district,next,produced;public string code,office;public bool active; }
    [Serializable] public class DispatchPacket
    {
        public int id,source,district,created,expires,received,delivered,value,progress,money,heat;
        public string sender,title,body,feedback;public DispatchStatus status;
    }
    [Serializable] public class DispatchTemplate { public string title,body,feedback;public int value,progress,money,heat; }
    [Serializable] public class CourierConfig
    {
        public int cycle=3,lifetime=5,expandCost=20,expandDeliveries=2,staminaCost=12,exposure=6;
        public List<SourceContact> sources=new List<SourceContact>();public List<DispatchTemplate> templates=new List<DispatchTemplate>();
    }
    public static class CourierRules
    {
        public static CourierConfig Load(){var asset=Resources.Load<TextAsset>("Minglu/Config/courier_config");if(asset==null)throw new InvalidOperationException("缺少情报交通配置。");return JsonUtility.FromJson<CourierConfig>(asset.text);}
        public static void Initialize(RunState s)
        {
            if(!s.campaignEnabled||s.courierEnabled)throw new InvalidOperationException("情报交通需在新战役初始化一次。");
            s.security=new SecurityState();s.courier=Load();ValidateConfig(s.courier,s.config);s.courierEnabled=true;
            var source=s.courier.sources[0];source.active=true;source.next=s.turn+s.courier.cycle;
            Produce(s,source.id,source.code,source.district,source.produced++);
            Log(s,"你是新京地区地下工作的负责人。首份密件已经备妥：指定交通员接收，次夜再安排外送；本人不占外勤名额。");
        }
        public static int Delivered(RunState s){return s.dispatchPackets.Count(p=>p.status==DispatchStatus.Delivered);}
        public static string OrderName(OrganizationDuty duty){return duty==OrganizationDuty.RelocateStation?"转移交通站":duty==OrganizationDuty.ReceivePacket?"接收密件":duty==OrganizationDuty.SendPacket?"外送情报":"建立潜伏联络";}
        public static bool IsCourier(OrganizationDuty d){return d==OrganizationDuty.ReceivePacket||d==OrganizationDuty.SendPacket||d==OrganizationDuty.ExpandNetwork||d==OrganizationDuty.RelocateStation;}
        public static int ReservedMoney(RunState s){return s.courierEnabled?s.agents.Count(a=>a.condition==Condition.Active&&a.organizationDuty==OrganizationDuty.ExpandNetwork)*s.courier.expandCost+s.agents.Count(a=>a.condition==Condition.Active&&a.organizationDuty==OrganizationDuty.RelocateStation)*8:0;}
        public static string Assign(RunState s,int agentId,OrganizationDuty duty,int target)
        {
            var a=s.agents.Find(x=>x.id==agentId);
            if(!s.courierEnabled||!IsCourier(duty)||s.ended||!string.IsNullOrEmpty(s.pendingEvent)||a==null||a.condition!=Condition.Active)return "当前无法安排交通工作。";
            if(a.order!=Duty.Rest||(a.organizationDuty!=OrganizationDuty.None&&(a.organizationDuty!=duty||a.courierTarget!=target)))return "该成员已有指令，请先撤回或选择空闲成员，避免覆盖原任务。";
            if(a.spirit<15||a.stamina<s.courier.staminaCost)return "执行人需要先休整，精神或体力不足。";
            SecurityRules.Ensure(s);
            int district=s.security.district;
            if(duty==OrganizationDuty.RelocateStation) return SecurityRules.AssignMove(s,a,target);
            if(s.agents.Any(x=>x.id!=a.id&&x.organizationDuty==OrganizationDuty.RelocateStation))return "今夜正在转移交通站；请次夜安排交通。";
            if(s.security.closed&&duty!=OrganizationDuty.ExpandNetwork)return "交通站已中断，请先转移交通站恢复联络。";
            if(duty==OrganizationDuty.ExpandNetwork)
            {
                var source=s.courier.sources.Find(x=>x.id==target);
                if(source==null||source.active)return "该潜伏联络已建立或不存在。";
                int needed=s.courier.sources.Count(x=>x.active)*s.courier.expandDeliveries;
                if(Delivered(s)<needed)return "需累计外送"+needed+"份情报，才能获准接入下一条潜伏线。";
                if(s.agents.Any(x=>x.id!=a.id&&x.organizationDuty==duty))return "本夜已有成员负责扩展潜伏联络。";
                district=source.district;
            }
            else
            {
                var packet=s.dispatchPackets.Find(x=>x.id==target);
                if(packet==null||packet.expires<s.turn||packet.status!=(duty==OrganizationDuty.ReceivePacket?DispatchStatus.Waiting:DispatchStatus.Received))return "密件状态不符：先接收，再外送；过期密件不可使用。";
                if(s.agents.Any(x=>x.id!=a.id&&IsCourier(x.organizationDuty)&&x.organizationDuty!=OrganizationDuty.ExpandNetwork&&x.organizationDuty!=OrganizationDuty.RelocateStation&&x.courierTarget==target))return "该密件已有交通员负责，请先撤回原指令。";
                if(duty==OrganizationDuty.ReceivePacket && (packet.expires<=s.turn||s.turn>=s.config.Value("turns")))return "来不及外送：接收和外送各需一夜，请处理其他目标。";
                district=duty==OrganizationDuty.ReceivePacket?packet.district:s.security.district;
            }
            var oldDuty=a.organizationDuty;var oldOrder=a.order;
            a.organizationDuty=duty;a.order=Duty.Rest;
            if(PrototypeRules.UsedAP(s)>s.config.Value("ap")||OrganizationRules.ReservedMoney(s)>s.money)
            {a.organizationDuty=oldDuty;a.order=oldOrder;return "行动点或未预留银两不足。";}
            a.courierTarget=target;a.district=district;a.mission="";a.site="";a.target=-1;a.candidateTarget=-1;return "";
        }
        public static bool Execute(RunState s,Agent a)
        {
            if(!s.courierEnabled||!IsCourier(a.organizationDuty))return false;
            if(a.organizationDuty==OrganizationDuty.RelocateStation){SecurityRules.Move(s,a);return true;}
            SecurityRules.Ensure(s);
            var c=s.courier;a.stamina=Math.Max(0,a.stamina-c.staminaCost);a.spirit=Math.Max(0,a.spirit-s.organization.actionSpirit);
            int exposure=c.exposure+s.config.District(a.district).risk+CityContentRules.Exposure(s,a.district);a.exposure=Math.Min(100,a.exposure+exposure);
            if(a.organizationDuty==OrganizationDuty.ExpandNetwork)
            {
                var source=c.sources.First(x=>x.id==a.courierTarget);s.money-=c.expandCost;source.active=true;source.next=s.turn+c.cycle;
                Log(s,a.name+"建立了与「"+source.code+"」的联系："+source.office+"。银两−"+c.expandCost+"；预计第"+source.next+"夜产出消息。");
            }
            else
            {
                var p=s.dispatchPackets.First(x=>x.id==a.courierTarget);
                a.knownStation=s.security.generation;s.security.trace=Math.Min(100,s.security.trace+8);
                if(a.organizationDuty==OrganizationDuty.ReceivePacket)
                {p.status=DispatchStatus.Received;p.received=s.turn;Log(s,a.name+"接回「"+p.title+"」，密件现存交通站，下一夜可安排外送。内容："+p.body);}
                else
                {
                    p.status=DispatchStatus.Delivered;p.delivered=s.turn;s.intelligence+=p.value;s.progress+=p.progress;s.money+=p.money;
                    if(s.cityEnabled){var q=s.quarters.First(x=>x.district==p.district);q.heat=Math.Max(0,Math.Min(100,q.heat+p.heat));}
                    Log(s,"送达回执 / "+a.name+"："+p.feedback+" 情报+"+p.value+"，成果+"+p.progress+"，银两+"+p.money+"，来源区热度"+p.heat.ToString("+0;-0;0")+"。");
                }
            }
            Log(s,a.name+"完成交通工作：体力−"+c.staminaCost+"，暴露+"+exposure+"。");return true;
        }
        static void Produce(RunState s,int source,string sender,int district,int index)
        {
            var c=s.courier;var t=c.templates[(index+Math.Abs(source))%c.templates.Count];
            s.dispatchPackets.Add(new DispatchPacket{id=s.nextDispatchId++,source=source,sender=sender,district=district,created=s.turn,expires=s.turn+c.lifetime,title=t.title,body=t.body,feedback=t.feedback,value=t.value,progress=t.progress,money=t.money,heat=t.heat});
            Log(s,"「"+sender+"」有一份待接收密件。地点："+s.config.District(district).name+"；第"+(s.turn+c.lifetime)+"夜结束失效。内容须接回后查看。");
        }
        public static void FromSpy(RunState s,Agent a){Produce(s,-a.id-1,a.name,s.agents.First(x=>x.id==a.id).district,a.spyNights/s.organization.spyInterval);}
        public static void EndNight(RunState s)
        {
            if(!s.courierEnabled)return;
            foreach(var p in s.dispatchPackets.Where(x=>(x.status==DispatchStatus.Waiting||x.status==DispatchStatus.Received)&&x.expires<=s.turn))
            {p.status=DispatchStatus.Expired;Log(s,"密件失效："+p.title+"。没有获得送达收益；联络线仍保留。");}
            foreach(var source in s.courier.sources.Where(x=>x.active&&x.next<=s.turn))
            {Produce(s,source.id,source.code,source.district,source.produced++);source.next=s.turn+Math.Max(2,s.courier.cycle+(source.produced%3)-1);}
        }
        public static void ValidateConfig(CourierConfig c,GameConfig g)
        {
            if(c==null||c.cycle<2||c.cycle>6||c.lifetime<2||c.lifetime>8||c.expandCost<1||c.expandCost>100||c.expandDeliveries<1||c.expandDeliveries>5||c.staminaCost<1||c.staminaCost>30||c.exposure<0||c.exposure>20||c.sources==null||c.sources.Count<1||c.sources.Count>6||c.sources.Any(x=>x==null||x.id<0||string.IsNullOrWhiteSpace(x.code)||string.IsNullOrWhiteSpace(x.office)||!g.districts.Any(d=>d.id==x.district))||c.sources.Select(x=>x.id).Distinct().Count()!=c.sources.Count||c.templates==null||c.templates.Count==0||c.templates.Any(x=>x==null||string.IsNullOrWhiteSpace(x.title)||string.IsNullOrWhiteSpace(x.body)||string.IsNullOrWhiteSpace(x.feedback)||x.value<0||x.value>10||x.progress<0||x.progress>10||x.money<0||x.money>20||x.heat>0||x.heat< -20))throw new InvalidOperationException("情报交通配置无效。");
        }
        public static void ValidateSave(RunState s)
        {
            if(!s.courierEnabled)return;SecurityRules.Validate(s);ValidateConfig(s.courier,s.config);
            if(s.dispatchPackets==null||s.nextDispatchId<0||s.dispatchPackets.Any(x=>x==null)||s.dispatchPackets.Select(x=>x.id).Distinct().Count()!=s.dispatchPackets.Count||s.dispatchPackets.Any(p=>p==null||p.id<0||p.id>=s.nextDispatchId||!Enum.IsDefined(typeof(DispatchStatus),p.status)||p.created<1||p.created>s.turn||p.expires<p.created||!s.config.districts.Any(d=>d.id==p.district)||p.value<0||p.progress<0||p.money<0||p.received<0||p.received>s.turn||p.delivered<0||p.delivered>s.turn||((p.status==DispatchStatus.Received||p.status==DispatchStatus.Delivered)&&p.received==0)||(p.status==DispatchStatus.Delivered&&p.delivered==0)))throw new InvalidOperationException("密件存档无效。");
            foreach(var a in s.agents.Where(x=>IsCourier(x.organizationDuty)))
            {
                var clone=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));
                if(Assign(clone,a.id,a.organizationDuty,a.courierTarget)!="")throw new InvalidOperationException("交通指令存档无效。");
            }
        }
        static void Log(RunState s,string line){s.journal.Add("["+s.turn+"] "+line);}
    }
}
