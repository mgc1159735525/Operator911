using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawMissionPlan()
        {
            var t=State.command.tasks.FirstOrDefault(x=>x.id==commandTask&&x.status==0);
            if(t==null){modal="";return;}
            var a=CommandRules.Agent(State,selectedAgent);var p=CommandModal("任务方案 / "+t.title,730);
            Label(p,t.origin+(t.source>=0?" / 提报人："+CommandName(t.source):"")+" · "+State.config.District(t.district).name+" · 第"+t.deadline+"夜截止",28,77,844,30,18,Muted);
            string[] descriptions={"常规\n无额外预算 / 基础骰池","充分筹备\n4银两 / +1骰 / 暴露−2","紧急推进\n+1骰 / 暴露+8 / 压力+2"};
            for(int i=0;i<3;i++){int plan=i;Button("MissionPlan_"+i,p,descriptions[i],28+i*284,121,272,73,()=>{t.plan=plan;Draw();},t.actor<0,t.plan==i);}
            Button("MissionIntel",p,t.intelSupport?"情报支援：开启 · 1份情报换+1骰":"情报支援：关闭 · 可选1份情报换+1骰",28,210,844,43,()=>{t.intelSupport=!t.intelSupport;Draw();},t.actor<0,t.intelSupport);
            int dice=CommandRules.TaskDice(State,a,t);string error=CommandRules.TaskError(State,a.id,t);
            Label(p,a.name+" · "+CommandRules.Skills[t.skill]+"当前"+CommandRules.Dice(State,a,t.skill)+"骰 / 方案+"+(t.plan==0?0:1)+" / 支援+"+(t.intelSupport?1:0)+" / 盘查−"+CommandRules.DistrictPenalty(State,t)+"\n最终"+dice+"骰，需要"+t.required+"个4—6：成功率 "+(DispatchRules.Probability(dice,t.required)*100).ToString("0.#")+"%",28,269,844,68,21,Teal);
            Label(p,"执行消耗："+CommandRules.PlanCost(t)+"银两、"+(t.intelSupport?1:0)+"情报、14体力、6精神\n暴露+"+CommandRules.TaskExposure(State,a,t)+(t.plan==2?"；压力+2（成败都生效）":"")+"\n成功："+CommandRules.TaskReward(t)+"\n失败：额外暴露+"+t.failExposure+"、压力+2；已用资源不返还，期限内可再授权",28,349,844,135,19);
            string follow=string.IsNullOrEmpty(t.followup)?"此任务没有固定后续。":"成功后解锁："+t.followup+"（同一地区，另行授权）。";
            Label(p,follow+"\n"+(string.IsNullOrEmpty(t.lastResult)?"每骰独立判定；夜间状态变化会影响后续任务骰池。":t.lastResult),28,496,844,75,17,Muted);
            Label(p,t.actor>=0?"已授权：先撤回授权才能更改方案。":error!=""?error:"可用："+(State.money-CommandRules.ReservedMoney(State,t.id))+"银两 / "+(State.intelligence-CommandRules.ReservedIntel(State,t.id))+"情报。授权预留，撤回释放；成员自行执行。",28,583,844,53,18,error==""?Teal:Red);
            Button("MissionBack",p,"返回指挥席",28,652,300,48,()=>{modal="";Draw();});
            Button("MissionAuthorize",p,"按此方案授权 "+a.name,348,652,524,48,()=>{toast=CommandRules.Assign(State,a.id,t.id);if(toast==""){modal="";Save(false);}Draw();},error==""&&t.actor<0,true);
        }
    }
}
