using System;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        string CourierError(int agent,OrganizationDuty duty,int target){var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));return CourierRules.Assign(copy,agent,duty,target);}
        Agent CourierCandidate(OrganizationDuty duty,int target){return State.agents.Where(a=>a.condition==Condition.Active&&a.order==Duty.Rest&&a.organizationDuty==OrganizationDuty.None&&CourierError(a.id,duty,target)=="").OrderBy(a=>a.exposure).ThenByDescending(a=>a.stamina).FirstOrDefault();}
        bool PacketOrdered(DispatchPacket p){return State.agents.Any(a=>(a.organizationDuty==OrganizationDuty.ReceivePacket||a.organizationDuty==OrganizationDuty.SendPacket)&&a.courierTarget==p.id);}
        string SafetySummary(){SecurityRules.Ensure(State);return "交通站："+State.config.District(State.security.district).name+" · "+(State.security.closed?"中断，需转移":"追查 "+State.security.trace+" / 100")+"\n每次接收/外送+8；现址被供出+35；满100遭搜查。";}
        void DrawGuidedOrders()
        {
            SecurityRules.Ensure(State);
            var p=Box("GuidedOrders",root,1172,134,404,622,Panel);
            Label(p,"今夜该做什么",20,16,364,36,25,Gold);
            Label(p,SafetySummary(),20,61,364,76,17,State.security.trace>=65||State.security.closed?Red:Teal);
            Bar(p,20,145,364,State.security.trace,100,Red);
            var packet=State.dispatchPackets.Where(x=>(x.status==DispatchStatus.Waiting||x.status==DispatchStatus.Received)&&!PacketOrdered(x)&&x.expires>=State.turn&&(x.status==DispatchStatus.Received||x.expires>State.turn&&State.turn<State.config.Value("turns"))).OrderBy(x=>x.expires).ThenByDescending(x=>x.status).FirstOrDefault();
            var main=State.config.missions.FirstOrDefault(m=>new[]{"shelter","network","garrison","uprising"}.Contains(m.id)&&!State.completedMissions.Contains(m.id));
            bool urgentMain=main!=null&&main.deadline<=State.turn+1&&DispatchRules.Open(State).Any(m=>m.id==main.id)&&!State.agents.Any(a=>a.mission==main.id)&&!State.security.closed;
            bool move=State.security.closed||State.security.trace>=65;
            int destination=State.config.districts.Where(d=>d.id!=State.security.district).OrderBy(d=>d.risk).First().id;
            var duty=move?OrganizationDuty.RelocateStation:packet!=null&&packet.status==DispatchStatus.Received?OrganizationDuty.SendPacket:OrganizationDuty.ReceivePacket;
            int target=move?destination:packet==null?-1:packet.id;
            var candidate=target<0?null:CourierCandidate(duty,target);
            string title=move?"优先：转移交通站":packet==null?"交通待办已安排 / 暂无可送密件":duty==OrganizationDuty.SendPacket?"下一步：外送已入站密件":"下一步：接回待办密件";
            if(urgentMain)title="优先：主线即将截止";
            Label(p,title,20,165,364,55,21,Gold);
            string detail=move?"消耗1AP、8银两、"+State.courier.staminaCost+"体力。追查−45，恢复联络；今夜暂停其他交通工作。":packet==null?"检查下方主线目标，再结算今夜。未派遣的人自动休整，不必强行用完行动点。":duty==OrganizationDuty.ReceivePacket?"密件 #"+packet.id+" · 第"+packet.expires+"夜截止\n1AP、"+State.courier.staminaCost+"体力。今夜接回，次夜外送后才获得收益。":"「"+packet.title+"」\n1AP、"+State.courier.staminaCost+"体力。送达：成果+"+packet.progress+"、情报+"+packet.value+"、银两+"+packet.money+"。";
            if(urgentMain)detail=main.title+" · 第"+main.deadline+"夜截止\n这是胜利前置任务，错过会中断起义路线。先安排主线，再处理交通工作。";
            Label(p,detail,20,225,364,99,17);
            Label(p,urgentMain?"点击下方选择主线与人选，在右侧查看成功率和成本后签发。":candidate==null?(target<0?"可以推进主线或封缄结算。":"没有可直接派出的空闲成员。请检查体力、AP、银两或撤回冲突指令。"):("推荐 "+candidate.name+" · 暴露"+candidate.exposure+" / 体力"+candidate.stamina+"\n优先空闲且低暴露成员，不覆盖已有任务。"),20,332,364,66,16,Muted);
            Button("GuideAssign",p,urgentMain?"前往主线并推荐人选":candidate==null?"当前无法安排":("安排"+candidate.name+" · "+CourierRules.OrderName(duty)),20,404,364,43,()=>{if(urgentMain){tab=3;SelectMission(main,true);}else{selectedAgent=candidate.id;CourierAction(duty,target);}},urgentMain||candidate!=null,true);
            var owner=State.agents.FirstOrDefault(a=>CourierRules.IsCourier(a.organizationDuty));
            Button("GuideCancel",p,owner==null?"暂无交通指令":"撤回 "+owner.name,20,456,174,33,()=>{toast=PrototypeRules.Assign(State,owner.id,Duty.Rest);Draw();},owner!=null);
            var mover=CourierCandidate(OrganizationDuty.RelocateStation,destination);
            Button("GuideMove",p,"提前转移 · 8银",204,456,180,33,()=>{selectedAgent=mover.id;CourierAction(OrganizationDuty.RelocateStation,destination);},mover!=null);
            bool open=main!=null&&DispatchRules.Open(State).Any(m=>m.id==main.id);
            string goal=main==null?"主线已完成，继续取得行动成果。":State.expiredMissions.Contains(main.id)?"主线已中断："+main.title:"胜利必做："+main.title+" · "+main.start+"—"+main.deadline+"夜";
            Label(p,goal,20,503,364,44,17,Gold);
            Button("GuideMain",p,open?"前往主线 · 推荐人选":"查看战役目标与时间",20,558,364,43,()=>{tab=open?3:5;if(open)SelectMission(main,true);else Draw();});
        }
    }
}
