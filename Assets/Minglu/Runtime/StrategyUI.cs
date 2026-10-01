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
            var p=CommandModal("比较负责人 / "+t.title,706);
            Label(p,"当前指令："+(t.reconnaissance?"调查目标 · 使用情报能力":"执行目标 · 使用"+CommandRules.Skills[t.skill])+"。按可用性与成功率排序，留意岗位代价。",28,96,844,38,17,Muted);
            var people=State.agents.OrderBy(a=>CommandRules.TaskError(State,a.id,t)==""?0:1).ThenByDescending(a=>CommandRules.TaskDice(State,a,t)).ToList();
            var list=Scroll(p,"CandidateList",28,146,844,440,people.Count*126);
            for(int i=0;i<people.Count;i++)
            {
                var a=people[i];string error=CommandRules.TaskError(State,a.id,t);int dice=CommandRules.TaskDice(State,a,t),y=i*126;
                Portrait(list,a,0,y+6,50,73);
                Label(list,a.name+" · "+CommandRules.Person(State,a.id).profession+" "+CommandRules.Person(State,a.id).level+"级",66,y+3,424,28,20,Gold);
                Label(list,"情报 "+a.intel+" / 潜行 "+a.stealth+" / 说服 "+a.persuade+" / 组织 "+a.organize+" · 体力 "+a.stamina,66,y+35,424,24,13,Muted);
                Label(list,(DispatchRules.Probability(dice,CommandRules.ActionRequired(t))*100).ToString("0.#")+"%",512,y+6,130,32,24,TextColor);
                Label(list,dice+" 骰 / 需 "+CommandRules.ActionRequired(t)+" 成功",512,y+46,130,22,14,Muted);
                Label(list,"暴露 "+a.exposure+" → "+UnityEngine.Mathf.Min(100,a.exposure+CommandRules.TaskExposure(State,a,t)),512,y+76,140,22,13,Muted);
                Label(list,error!=""?error:CommandRole(a.id)=="机动成员"?"可用 · 不占用联络岗位":"可用 · "+CommandRole(a.id)+"，本夜该岗位等待",66,y+65,424,52,15,error==""?Muted:Red);
                Button("CompareSelect_"+a.id,list,"选此人",668,y+21,144,40,()=>{selectedAgent=a.id;modal="missionPlan";Draw();},error=="");
                Rule(list,0,y+122,812);
            }
            Button("CompareBack",p,"返回任务",28,626,276,48,()=>{modal="missionPlan";Draw();});
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
            Label(p,"敌方持续蹲守，组织等待撤离指示。\n两种方式都会转移据点、追查−45，并暂停今夜链条。\n已授权任务仍会执行。",28,145,844,105,22);
            int lost=State.command.routes.Sum(r=>r.atRelay),free=State.money-CommandRules.ReservedMoney(State);
            Label(p,"有序转移：支出"+x.config.relocateCost+"银两，保留全部情报。\n销毁撤离：不花钱，放弃中继情报"+lost+"份，内线待接情报保留。\n可用经费 "+free+"（已扣除任务预留）。",28,270,844,135,21,Teal);
            ActionTicket("CommandEventSafe",p,"有序转移",x.config.relocateCost+" 银两",28,480,404,64,()=>{toast=CommandRules.ResolveRaid(State,true);Save(false);Draw();},free>=x.config.relocateCost);
            Button("CommandEventRisk",p,"销毁中继情报并撤离",450,480,422,64,()=>{toast=CommandRules.ResolveRaid(State,false);Save(false);Draw();});
        }
    }
}
