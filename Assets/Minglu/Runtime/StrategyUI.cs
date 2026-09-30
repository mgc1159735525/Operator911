using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        int pendingRoute=-1,pendingSlot,pendingMember;
        void DrawStrategy(Transform panel)
        {
            var x=State.command.strategy;var c=x.config;
            Label(panel,"地区经营 / 安全与盟友",20,14,570,34,24,Gold);
            Button("StrategyDetails",panel,"查看安全规则  ›",608,14,190,34,()=>{modal="strategyDetails";Draw();});
            Label(panel,"据点："+State.config.District(x.baseDistrict).name+"  ·  追查 "+x.trace+" / "+c.raid+"触发撤离  ·  民心 "+x.hearts,20,57,798,30,19,x.trace>=c.raid-15?Red:Teal);
            Label(panel,x.trace>=c.raid-15?"据点接近暴露。优先轮休，并为转移预留经费。":"选择盟友发起委托，完成后获得长期支援。",20,101,798,39,18,x.trace>=c.raid-15?Red:Muted);
            for(int i=0;i<4;i++)
            {
                int id=i,y=171+i*72;Label(panel,CommandRules.Factions[i]+" · 信任 "+x.alliances[i]+" / "+c.allianceThreshold,20,y,470,26,20,Gold);
                Label(panel,(x.alliances[i]>=c.allianceThreshold?"已生效：":"达标后：")+CommandRules.AllianceEffects[i],20,y+29,470,25,16,Teal);
                bool pending=State.command.tasks.Any(t=>t.status==0&&t.operation==1&&t.target==id);
                Button("AllianceTask_"+i,panel,pending?"查看进行中的委托":x.alliances[i]>=100?"信任已满 · 收益持续":"发起联络 · 2次成功",516,y+7,282,43,()=>OpenCivicTask(id),pending||x.alliances[i]<100);
            }
            Button("CommunityTask",panel,"安排街坊互助 · 民心+10 / 追查−8",20,480,778,44,()=>OpenCivicTask(-1));
            Label(panel,"发起委托不花钱、不推进时间。到任务页选择负责人；每阶段可换人，完成全部阶段才获奖励。",20,535,790,45,16,Muted);
        }
        void OpenCivicTask(int faction)
        {
            var existing=State.command.tasks.FirstOrDefault(t=>t.status==0&&t.operation==(faction<0?3:1)&&t.target==faction);
            toast=existing==null?CommandRules.StartCivicTask(State,faction):"";
            if(toast==""){FocusCommandTask((existing??State.command.tasks.Last()).id);toast="委托已定位：比较成员、配置方案，再授权。";Save(false);}Draw();
        }
        void GoNextCommand()
        {
            var urgent=State.command.tasks.Where(t=>t.status==0&&t.actor<0).OrderBy(t=>t.operation==2?0:1).ThenBy(t=>t.deadline).FirstOrDefault();
            var watched=State.agents.FirstOrDefault(a=>NeedsAttention(a)&&a.exposure>=State.command.strategy.config.warning);
            if(watched!=null){commandTab=2;selectedAgent=watched.id;}
            else if(urgent!=null&&urgent.operation==2){FocusCommandTask(urgent.id);}
            else if(!HasActiveInsider()){var job=State.command.tasks.FirstOrDefault(t=>t.operation==4&&t.status==0);if(job!=null){FocusCommandTask(job.id);selectedAgent=job.target;}else{commandTab=2;selectedAgent=State.agents.First(a=>a.condition==Condition.Active).id;}}
            else if(!State.command.routes.Any(r=>r.enabled)||State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)!=""))commandTab=1;
            else if(State.command.people.Any(p=>p.neglect>=3&&NeedsAttention(CommandRules.Agent(State,p.id)))){commandTab=2;selectedAgent=State.command.people.First(p=>p.neglect>=3&&NeedsAttention(CommandRules.Agent(State,p.id))).id;}
            else if(urgent!=null){FocusCommandTask(urgent.id);}
            else modal="commandConfirm";
            Draw();
        }
        void DrawCandidateComparison()
        {
            var t=State.command.tasks.FirstOrDefault(v=>v.id==commandTask&&v.status==0);if(t==null){modal="";return;}
            var p=CommandModal("比较负责人 / "+t.title,730);
            Label(p,"优先展示可用成员，再按成功率排序；高成功率不一定值得占用接头岗位。",28,78,844,46,18,Muted);
            var people=State.agents.OrderBy(a=>CommandRules.TaskError(State,a.id,t)==""?0:1).ThenByDescending(a=>CommandRules.TaskDice(State,a,t)).ToList();
            var list=Scroll(p,"CandidateList",28,138,844,480,people.Count*110);
            for(int i=0;i<people.Count;i++)
            {
                var a=people[i];string error=CommandRules.TaskError(State,a.id,t);int dice=CommandRules.TaskDice(State,a,t),y=i*110;
                Label(list,a.name+" · "+CommandRules.Person(State,a.id).profession+" Lv."+CommandRules.Person(State,a.id).level+" · "+dice+"骰 / "+(DispatchRules.Probability(dice,t.required)*100).ToString("0.#")+"%",10,y,600,32,20,Gold);
                Label(list,error!=""?error:CommandRole(a.id)=="机动成员"?"可用 · 不占用运行中的链条岗位":"可用 · "+CommandRole(a.id)+"，授权后本夜该岗位等待",10,y+38,598,58,17,error==""?Teal:Red);
                Button("CompareSelect_"+a.id,list,"选此人",635,y+18,160,46,()=>{selectedAgent=a.id;modal="missionPlan";Draw();},error=="");
            }
            Button("CompareBack",p,"返回任务 · 不更改授权",28,652,844,48,()=>{modal="missionPlan";Draw();});
        }
        void RequestRouteReplacement(CommandRoute r,int slot)
        {
            int old=slot==0?r.source:slot==1?r.liaison:r.courier;if(old==selectedAgent)return;
            pendingRoute=r.id;pendingSlot=slot;pendingMember=selectedAgent;
            if(r.waiting+r.atRelay>0){modal="routeReplace";Draw();}else ApplyRouteReplacement();
        }
        void ApplyRouteReplacement()
        {
            var r=State.command.routes.First(v=>v.id==pendingRoute);string error=RouteCandidateError(r,pendingSlot,pendingMember);if(error!=""){modal="";toast=error;Draw();return;}r.enabled=false;r.waiting=0;r.atRelay=0;r.age=0;
            if(pendingSlot==0)r.source=pendingMember;else if(pendingSlot==1)r.liaison=pendingMember;else r.courier=pendingMember;
            modal="";toast="岗位已修改，链条暂停；检查成员后重新启用。";Save(false);Draw();
        }
        void DrawRouteReplacement()
        {
            var r=State.command.routes.First(v=>v.id==pendingRoute);var p=CommandModal("更换岗位会放弃旧链条积压",420);
            Label(p,"将换入："+CommandName(pendingMember)+"\n待接 "+r.waiting+"份 + 中继 "+r.atRelay+"份将清空，链条暂停。\n如果只是安排休息，可以取消后暂停此线，保留积压。",28,105,844,180,22);
            Button("RouteReplaceCancel",p,"取消 · 保留岗位与情报",28,325,404,52,()=>{modal="";Draw();});
            Button("RouteReplaceConfirm",p,"确认更换并放弃积压",450,325,422,52,ApplyRouteReplacement);
        }
        void DrawRaidEvent()
        {
            var x=State.command.strategy;var p=CommandModal("据点追查 / 决定撤离方式",590);
            Label(p,State.config.District(x.baseDistrict).name+"据点 · 追查 "+x.trace,28,95,844,35,24,Gold);
            Label(p,"敌方持续蹲守，组织等待撤离指示。两种方式都会转移据点、追查−45、今夜链条暂停。已授权任务仍会执行。",28,145,844,105,22);
            int lost=State.command.routes.Sum(r=>r.atRelay),free=State.money-CommandRules.ReservedMoney(State);
            Label(p,"有序转移：支出"+x.config.relocateCost+"银两，保留全部情报。\n销毁撤离：不花钱，放弃中继情报"+lost+"份，内线待接情报保留。\n可用经费 "+free+"（已扣除任务预留）。",28,270,844,135,21,Teal);
            ActionTicket("CommandEventSafe",p,"有序转移",x.config.relocateCost+" 银两",28,480,404,64,()=>{toast=CommandRules.ResolveRaid(State,true);Save(false);Draw();},free>=x.config.relocateCost);
            Button("CommandEventRisk",p,"销毁中继情报并撤离",450,480,422,64,()=>{toast=CommandRules.ResolveRaid(State,false);Save(false);Draw();});
        }
    }
}
