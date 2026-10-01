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
            Label(panel,"地方事务",28,20,554,42,32,Gold);
            Button("StrategyDetails",panel,"安全规则  ›",618,28,192,32,()=>{modal="strategyDetails";Draw();});
            Label(panel,"据点："+State.config.District(x.baseDistrict).name+" · 追查 "+x.trace+" / "+c.raid+"触发撤离 · 民心 "+x.hearts,28,80,782,26,17,x.trace>=c.raid-15?Red:TextColor);
            Label(panel,x.trace>=c.raid-15?"据点接近暴露，优先轮休并为转移预留经费。":"选择盟友发起委托，完成后获得长期支援。",28,124,782,27,16,x.trace>=c.raid-15?Red:Muted);
            for(int i=0;i<4;i++)
            {
                int id=i,y=182+i*72;Label(panel,CommandRules.Factions[i]+" · 信任 "+x.alliances[i]+" / "+c.allianceThreshold,28,y,460,26,20,Gold);
                Label(panel,(x.alliances[i]>=c.allianceThreshold?"已生效：":"达标后：")+CommandRules.AllianceEffects[i],28,y+34,460,24,16,Muted);
                bool pending=State.command.tasks.Any(t=>t.status==0&&t.operation==1&&t.target==id);
                Button("AllianceTask_"+i,panel,pending?"查看委托  ›":x.alliances[i]>=100?"信任已满":"发起联络 · 2次成功",516,y+9,294,40,()=>OpenCivicTask(id),pending||x.alliances[i]<100);
            }
            Rule(panel,28,476,782);
            Button("CommunityTask",panel,"街坊互助 · 民心+10 / 追查−8",28,492,782,40,()=>OpenCivicTask(-1));
            Label(panel,"发起仅创建待办；签发后预留资源，结束今夜执行。",28,548,782,24,15,Muted);
        }
        void OpenCivicTask(int faction)
        {
            var existing=State.command.tasks.FirstOrDefault(t=>t.status==0&&t.operation==(faction<0?3:1)&&t.target==faction);
            toast=existing==null?CommandRules.StartCivicTask(State,faction):"";
            if(toast==""){FocusCommandTask((existing??State.command.tasks.Last()).id);toast="委托已定位：比较成员、配置方案，再授权。";Save(false);}Draw();
        }
        void GoNextCommand()
        {
            var step=NextCommandStep();if(!step.enabled)return;
            if(step.task>=0)FocusCommandTask(step.task);
            else{commandTab=step.page;modal=step.dialog;if(step.actor>=0)selectedAgent=step.actor;}
            Draw();
        }
        void DrawCandidateComparison()
        {
            var t=State.command.tasks.FirstOrDefault(v=>v.id==commandTask&&v.status==0);if(t==null){modal="";return;}
            var p=CommandModal("比较负责人 / "+t.title,776);
            Label(p,(t.reconnaissance?"调查看情报能力":"本阶段看"+CommandRules.Skills[t.skill])+"。先列可用且不打断今晚传递的人，再比较成功率。\n成功率更高，也可能占用关键岗位；由你决定如何取舍。",28,90,844,53,18,Muted);
            var people=AssignmentCandidates(t).ToList();
            var list=Scroll(p,"CandidateList",28,159,844,500,people.Count*150);
            for(int i=0;i<people.Count;i++)
            {
                var a=people[i];var person=CommandRules.Person(State,a.id);string error=CommandRules.TaskError(State,a.id,t);int dice=CommandRules.TaskDice(State,a,t),y=i*150;
                Portrait(list,a,0,y+7,50,73);
                Label(list,a.name+" · "+person.profession+" "+person.level+"级",68,y+3,442,28,21,Gold);
                Label(list,"体力 "+a.stamina+" / 精神 "+a.spirit+" / 连勤 "+person.neglect+"夜",68,y+37,442,24,17,Muted);
                Label(list,AssignmentReason(a,t),68,y+69,442,48,18,error!=""||AssignmentChainImpact(a.id)>0?Red:Muted).name="CandidateReason_"+a.id;
                Label(list,(DispatchRules.Probability(dice,CommandRules.ActionRequired(t))*100).ToString("0.#")+"%",526,y+5,134,33,25,TextColor);
                Label(list,dice+"骰 / 需"+CommandRules.ActionRequired(t)+"成功",526,y+44,134,25,17,Muted);
                Label(list,"暴露 "+a.exposure+" → "+Mathf.Min(100,a.exposure+CommandRules.TaskExposure(State,a,t)),526,y+80,270,23,17,Muted);
                Label(list,person.neglect>=2?"继续工作将积压家事；可在档案安排生活优先。":t.reconnaissance?"调查不增加行动经验；本夜不积累表身份经验。":"完成任务获得行动经验；本夜不积累表身份经验。",68,y+120,724,22,16,person.neglect>=2?Red:Muted);
                Button("CompareSelect_"+a.id,list,"选此人",678,y+18,134,42,()=>{selectedAgent=a.id;modal="missionPlan";Draw();},error=="");
                Rule(list,0,y+147,812);
            }
            Button("CompareBack",p,"返回任务",28,699,276,48,()=>{modal="missionPlan";Draw();});
        }
        void RequestRouteReplacement(CommandRoute r,int slot)
        {
            int old=slot==0?r.source:slot==1?r.liaison:r.courier;if(old==selectedAgent)return;
            pendingRoute=r.id;pendingSlot=slot;pendingMember=selectedAgent;
            if(r.waiting+r.atRelay>0){modal="routeReplace";Draw();}else ApplyRouteReplacement();
        }
        void ApplyRouteReplacement()
        {
            var r=State.command.routes.First(v=>v.id==pendingRoute);string error=CommandRules.ReplaceRouteMember(State,r,pendingSlot,pendingMember);
            if(error!=""){modal="";toast=error;Draw();return;}
            modal="";toast="岗位已交接，链条暂停；检查今晚预测后重新启用。";Save(false);Draw();
        }
        void DrawRouteReplacement()
        {
            var r=State.command.routes.First(v=>v.id==pendingRoute);var p=CommandModal("交接岗位 / 核对情报去向",460);
            Label(p,"新任"+new[]{"内线","接头人","交通员"}[pendingSlot]+"："+CommandName(pendingMember),28,102,844,34,23,Gold);
            Label(p,CommandRules.RouteReplacementLoss(State,r,pendingSlot),28,160,844,112,21).name="RouteReplacementConsequences";
            Label(p,"交接不推进时间。换岗后本线暂停，由你检查并重新启用。",28,288,844,50,18,Muted);
            Button("RouteReplaceCancel",p,"取消 · 保持原岗位",28,378,404,52,()=>{modal="";Draw();});
            Button("RouteReplaceConfirm",p,"确认交接",450,378,422,52,ApplyRouteReplacement);
        }
        void DrawRaidEvent()
        {
            var x=State.command.strategy;var p=CommandModal("据点追查 / 决定撤离方式",590);
            Label(p,State.config.District(x.baseDistrict).name+"据点 · 追查 "+x.trace,28,95,844,35,24,Gold);
            Label(p,"敌方持续蹲守，组织等待撤离指示。\n两种方式都会转移据点、追查−45，并暂停今夜链条。\n已授权任务仍会执行。",28,145,844,105,22);
            int lost=State.command.routes.Sum(r=>r.atRelay),free=State.money-CommandRules.ReservedMoney(State);
            Label(p,"有序转移：支出"+x.config.relocateCost+"银两，保留全部情报。\n销毁撤离：不花钱，放弃中继情报"+lost+"份，内线待接情报保留。\n可用经费 "+free+"（已扣除任务预留）。",28,270,844,135,21,Teal);
            ActionTicket("CommandEventSafe",p,"有序转移",x.config.relocateCost+" 银两",28,480,404,64,()=>{toast=CommandRules.ResolveRaid(State,true);Save(false);Draw();},free>=x.config.relocateCost);
            Button("CommandEventRisk",p,"销毁中继情报并撤离",450,480,422,64,()=>{toast=CommandRules.ResolveRaid(State,false);Save(false);Draw();});
        }
    }
}
