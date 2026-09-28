using System;
using System.Linq;
namespace Minglu
{
    [Serializable] public class SecurityState { public int trace,district,generation=1; public bool closed; }
    public static class SecurityRules
    {
        public static void Ensure(RunState s){if(s.security==null)s.security=new SecurityState();}
        public static void Validate(RunState s){Ensure(s);if(s.security.trace<0||s.security.trace>100||s.security.generation<1||!s.config.districts.Any(d=>d.id==s.security.district))throw new Exception("交通站安全数据无效");}
        public static string AssignMove(RunState s,Agent a,int district)
        {
            if(!s.config.districts.Any(d=>d.id==district)||district==s.security.district)return "请选择另一地区作为新交通站。";
            if(!s.security.closed&&s.security.trace==0)return "追查为零，无需花钱转移。";
            if(s.agents.Any(x=>x.id!=a.id&&CourierRules.IsCourier(x.organizationDuty)))return "转移需暂停今夜其他交通工作，请先撤回相关指令。";
            var old=a.organizationDuty;var order=a.order;a.organizationDuty=OrganizationDuty.RelocateStation;a.order=Duty.Rest;
            if(PrototypeRules.UsedAP(s)>s.config.Value("ap")||OrganizationRules.ReservedMoney(s)>s.money){a.organizationDuty=old;a.order=order;return "转移需要1AP、8银两，请释放预留资源或等收入到账。";}
            a.courierTarget=district;a.district=district;a.mission="";a.site="";a.target=-1;a.candidateTarget=-1;return "";
        }
        public static void Move(RunState s,Agent a)
        {
            s.money-=8;s.security.district=a.courierTarget;s.security.generation++;s.security.trace=Math.Max(0,s.security.trace-45);s.security.closed=false;
            a.knownStation=s.security.generation;a.stamina=Math.Max(0,a.stamina-s.courier.staminaCost);a.spirit=Math.Max(0,a.spirit-s.organization.actionSpirit);
            s.journal.Add("["+s.turn+"] "+a.name+"完成交通站转移：银两−8，追查−45。旧地址失效，联络恢复；已入站密件随站转移。");
        }
        public static void Disclose(RunState s,Agent a)
        {
            if(!s.courierEnabled)return;Ensure(s);
            if(a.knownStation!=s.security.generation)return;
            s.security.trace=Math.Min(100,s.security.trace+35);
            s.journal.Add("["+s.turn+"] "+a.name+"供出其掌握的现交通站地址，敌方追查+35；未掌握的其他来源没有因此暴露。");
        }
        public static void EndNight(RunState s)
        {
            if(!s.courierEnabled)return;Ensure(s);
            if(s.security.closed)return;
            bool traffic=s.agents.Any(a=>a.organizationDuty==OrganizationDuty.ReceivePacket||a.organizationDuty==OrganizationDuty.SendPacket);
            if(!traffic&&s.security.trace<100)s.security.trace=Math.Max(0,s.security.trace-2);
            if(s.security.trace<100)return;
            s.security.closed=true;int lost=0;
            foreach(var p in s.dispatchPackets.Where(p=>p.status==DispatchStatus.Received)){p.status=DispatchStatus.Expired;lost++;}
            s.journal.Add("["+s.turn+"] 敌方追查达到100，交通站被搜查，"+lost+"份在站密件销毁。来源仍保留；下一夜安排转移交通站恢复接收和外送。");
        }
    }
}
