using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        int courierPage, selectedPacket;
        string PacketStatus(DispatchPacket p){return p.status==DispatchStatus.Waiting?"待接收":p.status==DispatchStatus.Received?"已入站 / 待外送":p.status==DispatchStatus.Delivered?"已送达":"已失效";}
        void CourierAction(OrganizationDuty duty,int target)
        {
            toast=CourierRules.Assign(State,selectedAgent,duty,target);
            if(toast=="")toast="交通指令已安排，封缄结算时执行；可在右侧撤回。";
            Draw();
        }
        void DrawCourier()
        {
            var p=Box("CourierPanel",root,288,186,868,570,Panel);
            Label(p,"接收 → 次夜外送 → 获得回执",22,14,470,35,22,Gold);
            Label(p,"潜伏联络 "+State.courier.sources.Count(x=>x.active)+" / "+State.courier.sources.Count+"    累计送达 "+CourierRules.Delivered(State)+" 份",500,21,342,30,16,Teal);
            Label(p,"你统筹地区工作。左侧选人 → 接收密件 → 封缄结算 → 次夜安排外送。",22,58,824,30,17,Muted);
            Button("CourierInbox",p,"待办密件",22,99,256,38,()=>{courierPage=0;Draw();},true,courierPage==0);
            Button("CourierSources",p,"潜伏联络",292,99,256,38,()=>{courierPage=1;Draw();},true,courierPage==1);
            Button("CourierReceipts",p,"回执与失效",562,99,280,38,()=>{courierPage=2;Draw();},true,courierPage==2);
            var actor=State.agents.First(x=>x.id==selectedAgent);
            Label(p,"执行人："+actor.name+"  / 每项 1AP、体力−"+State.courier.staminaCost+"，增加所在地暴露",22,148,820,30,17,Muted);
            if(courierPage==1)
            {
                var content=Scroll(p,"CourierSourceScroll",22,191,824,357,State.courier.sources.Count*155);
                int i=0;
                foreach(var source in State.courier.sources)
                {
                    float y=i++*155;
                    Label(content,"「"+source.code+"」 · "+State.config.District(source.district).name,0,y,780,30,22,Gold);
                    Label(content,source.office,0,y+35,780,30,18);
                    var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
                    string error=CourierRules.Assign(copy,actor.id,OrganizationDuty.ExpandNetwork,source.id);
                    Label(content,source.active?"已接通 · 预计第"+source.next+"夜结束产出；消息间隔随工作变化。":error==""?"接入费用 "+State.courier.expandCost+" 银两，结算后持续产出。":error,0,y+72,540,63,16,Muted);
                    Button("CourierExpand_"+source.id,content,source.active?"联络已建立":"安排建立联络",555,y+78,237,40,()=>CourierAction(OrganizationDuty.ExpandNetwork,source.id),error=="");
                }
                return;
            }
            var packets=State.dispatchPackets.Where(x=>courierPage==0?(x.status==DispatchStatus.Waiting||x.status==DispatchStatus.Received):(x.status==DispatchStatus.Delivered||x.status==DispatchStatus.Expired)).OrderBy(x=>x.expires).ToList();
            var list=Scroll(p,"CourierPacketScroll"+courierPage,22,191,824,357,Mathf.Max(100,packets.Count*224));
            if(packets.Count==0)Label(list,courierPage==0?"暂无待办。潜伏者会持续产出消息，请同时处理城市事务。":"尚无回执。接收后再派交通员外送，送达才会获得收益。",0,10,790,80,19,Muted);
            int row=0;
            foreach(var packet in packets)
            {
                float y=row++*224;bool sealedPacket=packet.received==0;
                Label(list,(sealedPacket?"未拆密件 #"+packet.id:packet.title)+" · "+PacketStatus(packet),0,y,790,33,21,packet.status==DispatchStatus.Expired?Red:Gold);
                Label(list,"来源「"+packet.sender+"」 / "+State.config.District(packet.district).name+" / 第"+packet.expires+"夜结束失效",0,y+39,790,29,17,Muted);
                var owner=State.agents.FirstOrDefault(x=>CourierRules.IsCourier(x.organizationDuty)&&x.organizationDuty!=OrganizationDuty.ExpandNetwork&&x.organizationDuty!=OrganizationDuty.RelocateStation&&x.courierTarget==packet.id);
                Label(list,owner!=null?"已安排："+owner.name+" · "+CourierRules.OrderName(owner.organizationDuty):sealedPacket?"内容尚未接回，需先指派交通员接收。":packet.status==DispatchStatus.Delivered?packet.feedback:packet.body,0,y+74,790,48,17);
                var duty=packet.status==DispatchStatus.Waiting?OrganizationDuty.ReceivePacket:OrganizationDuty.SendPacket;
                var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));string error=CourierRules.Assign(copy,actor.id,duty,packet.id);
                if(courierPage==0)Label(list,owner!=null?"已占用人手与1AP；点击右下角封缄后执行。":error!=""?error:"可安排：1AP / 体力−"+State.courier.staminaCost+" / 追查+8",0,y+177,790,39,16,error!=""&&owner==null?Red:Teal);
                Button("CourierDetails_"+packet.id,list,"查看密件",0,y+132,190,37,()=>{selectedPacket=packet.id;modal="dispatchPacket";Draw();});
                Button("CourierMap_"+packet.id,list,"地图位置",204,y+132,190,37,()=>{selectedDistrict=packet.status==DispatchStatus.Received?State.security.district:packet.district;tab=0;Draw();});
                Button("CourierAssign_"+packet.id,list,owner!=null?"已安排 "+owner.name:CourierRules.OrderName(duty)+" · 1AP",410,y+132,380,37,()=>CourierAction(duty,packet.id),error==""&&owner==null&&courierPage==0,true);
            }
        }
        void DrawDispatchPacket(Transform p)
        {
            var packet=State.dispatchPackets.FirstOrDefault(x=>x.id==selectedPacket);if(packet==null)return;
            Label(p,packet.received==0?"密件尚未拆阅":packet.title,28,26,710,48,28,Gold);
            Label(p,"「"+packet.sender+"」 · "+PacketStatus(packet)+"\n第"+packet.created+"夜产生 / 第"+packet.expires+"夜结束失效",28,96,840,70,20,Teal);
            Label(p,packet.received==0?"接回后才能获知正文。你负责决定人手与优先级，交通员承担接收和外送工作。":packet.body,28,187,840,120,23);
            Label(p,packet.received==0?"每项交通工作占用 1AP，消耗体力并增加暴露；可轮换不同成员。":packet.status==DispatchStatus.Delivered?"送达回执："+packet.feedback:"外送收益：情报 +"+packet.value+" / 成果 +"+packet.progress+" / 银两 +"+packet.money+" / 来源区热度 "+packet.heat,28,335,840,110,21,Gold);
            Label(p,"接收与外送各需一夜；最晚可在截止当夜外送。交通工作与其他任务共用成员和行动点。",28,480,840,75,18,Muted);
        }
    }
}
