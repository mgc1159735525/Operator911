using System;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        void OrganizationAction(OrganizationDuty duty,int candidate=-1)
        {
            string error=OrganizationRules.Assign(State,selectedAgent,duty,selectedDistrict,candidate);
            toast=error==""?"组织指令已预留，封缄后执行；可从右侧撤回。":error;Draw();
        }
        void DrawOrganization()
        {
            var p=Box("OrganizationPanel",root,288,186,868,570,Panel);
            if(!State.organizationEnabled){Label(p,"此旧局未启用组织经营，请从主菜单开始新局。",24,24,820,80,25,Gold);return;}
            var c=State.organization;var actor=State.agents.First(a=>a.id==selectedAgent);
            Label(p,"组织事务",22,14,280,36,25,Gold);
            Label(p,"银两 "+State.money+"（招募预留 "+OrganizationRules.ReservedMoney(State)+"）  成员 "+OrganizationRules.Living(State)+" / "+c.capacity,250,20,590,28,19,Teal);
            Button("OrganizationDistrict",p,"接头区域："+State.config.District(selectedDistrict).name,22,65,270,39,()=>{int index=State.config.districts.FindIndex(d=>d.id==selectedDistrict);selectedDistrict=State.config.districts[(index+1)%State.config.districts.Count].id;selectedMission="";Draw();});
            Label(p,"当前执行人："+actor.name+" / "+Status(actor),310,74,515,30,19);
            var spyCopy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));var courierCopy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
            string spyError=OrganizationRules.Assign(spyCopy,actor.id,OrganizationDuty.Infiltrate,selectedDistrict);
            string courierError=OrganizationRules.Assign(courierCopy,actor.id,OrganizationDuty.Deliver,selectedDistrict);
            Button("StartInfiltration",p,"潜伏此区 · 1AP",22,115,252,42,()=>OrganizationAction(OrganizationDuty.Infiltrate),spyError=="",true);
            if(State.courierEnabled)Button("CourierBoard",p,"情报交通 · 按密件派人",288,115,264,42,()=>{tab=6;Draw();});
            else Button("DeliverIntel",p,"递送此区情报 · 1AP",288,115,264,42,()=>OrganizationAction(OrganizationDuty.Deliver),courierError=="");
            Button("OrganizationMap",p,"查看地图",566,115,270,42,()=>{tab=0;Draw();});
            string hints="潜伏"+c.spyDuration+"夜，每"+c.spyInterval+"夜产出；精神不足15只能休整。";
            if(spyError!="")hints="潜伏："+spyError;
            if(courierError!="")hints+="\n递送："+courierError;
            Label(p,hints,22,166,812,55,16,Muted);
            var content=Scroll(p,"OrganizationScroll",22,231,824,317,330+State.agents.Count*91+Math.Max(1,State.packets.Count)*58);
            Label(content,"接引候选人 / 每人"+c.recruitCost+"银两 + 1AP，本夜结算后加入",0,0,795,29,20,Gold);
            int n=0;
            foreach(var candidate in State.candidates)
            {
                var reserved=State.agents.FirstOrDefault(a=>a.organizationDuty==OrganizationDuty.Recruit&&a.candidateTarget==candidate.id);
                if(candidate.appearance!=null)DrawLook(content,candidate.appearance,0,34+n*54,39,50);
                Label(content,candidate.name+(candidate.appearance==null?"":candidate.appearance.sex==0?"（男）":"（女）")+" · "+candidate.role+"  情报"+candidate.intel+" / 潜行"+candidate.stealth,48,40+n*54,492,32,17);
                var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));string error=OrganizationRules.Assign(copy,actor.id,OrganizationDuty.Recruit,selectedDistrict,candidate.id);
                Button("Recruit_"+candidate.id,content,candidate.hired?"已加入":reserved!=null?reserved.name+"接引":"安排接引",576,34+n*54,210,40,()=>OrganizationAction(OrganizationDuty.Recruit,candidate.id),!candidate.hired&&reserved==null&&error=="");n++;
            }
            float y=214;
            Label(content,"身份与精神 / 每夜收入"+c.income+"，每位维持身份需"+c.maintenance+"银两",0,y,795,30,20,Gold);y+=43;
            foreach(var a in State.agents)
            {
                Label(content,a.name+" · "+(a.condition==Condition.Captured||a.condition==Condition.Lost?Status(a):OrganizationRules.OrderName(State,a))+"\n精神 "+a.spirit+" / 可疑度 "+a.suspicion+(a.suspicion>=60?"  正受盘查":""),0,y,545,61,18,a.suspicion>=60?Red:TextColor);
                if(a.condition==Condition.Undercover)
                    Button("Recall_"+a.id,content,a.recall?"取消接应":"接应撤回 · 1AP",576,y,210,42,()=>{toast=OrganizationRules.Recall(State,a.id);if(toast=="")toast="接应计划已更新，结算时生效。";Draw();},!State.ended&&string.IsNullOrEmpty(State.pendingEvent));
                else
                    Button("Maintain_"+a.id,content,a.maintainCover?"身份维持：开":"身份维持：关",576,y,210,42,()=>{toast=OrganizationRules.Maintain(State,a.id);if(toast=="")toast="身份维持选择已更新；结算时扣费。";Draw();},a.condition==Condition.Active&&!State.ended&&string.IsNullOrEmpty(State.pendingEvent));
                y+=91;
            }
            Label(content,"待递送情报 / 尚未计入可用情报",0,y,790,31,20,Gold);y+=43;
            if(State.packets.Count==0)Label(content,"暂无缓存。潜伏产出后，派另一名成员到对应区域递送。",0,y,790,55,18,Muted);
            foreach(var packet in State.packets)
            {
                Label(content,packet.kind+" · "+State.config.District(packet.district).name+" / 可用情报+"+packet.value+" / 第"+packet.expires+"夜结束失效",0,y,790,48,18,packet.expires==State.turn?Red:Teal);y+=58;
            }
        }
    }
}
