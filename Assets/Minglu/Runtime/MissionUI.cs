using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawMissionPlan()
        {
            var t=State.command.tasks.FirstOrDefault(x=>x.id==commandTask&&x.status==0);
            if(t==null){CloseTaskDialog();return;}
            var a=CommandRules.Agent(State,selectedAgent);var p=CommandModal(t.title,820);
            Label(p,State.config.District(t.district).name+" · "+(t.deadline<=State.turn?"今夜截止":"剩余 "+(t.deadline-State.turn+1)+" 夜")+" · "+t.origin,28,81,844,26,17,t.deadline<=State.turn?Red:Muted);
            var description=Scroll(p,"TaskDescription_"+t.id,28,118,844,91,91);
            string follow=t.stepsRequired>1?"阶段 "+t.stepsDone+" / "+t.stepsRequired+"；最后一阶段成功后结算奖励。":string.IsNullOrEmpty(t.followup)?"":"成功后解锁："+t.followup+"。";
            var text=Label(description,t.body+"\n"+(t.reconnaissance?"调查成功：后续执行+1骰、暴露−3；占用一夜，不完成任务、不增加库存情报。":"完成收益："+CommandRules.TaskReward(t)+(follow==""?"":"\n"+follow)),0,0,810,91,18);
            float h=Mathf.Max(91,text.preferredHeight+8);text.rectTransform.sizeDelta=new Vector2(810,h);description.sizeDelta=new Vector2(description.sizeDelta.x,h);
            Button("MissionModeExecute",p,t.reconComplete?"执行目标 · 已调查 +1骰 / 暴露−3":"直接执行目标",28,220,408,42,()=>{t.reconnaissance=false;Draw();},t.actor<0,!t.reconnaissance);
            Button("MissionModeRecon",p,t.reconComplete?"调查已完成":"先调查目标 · 占用一夜",456,220,416,42,()=>{t.reconnaissance=true;Draw();},t.actor<0&&!t.reconComplete,t.reconnaissance);
            Label(p,t.reconnaissance?"调查要求：情报能力 / 1个成功；可由其他成员先行调查，执行仍需符合原身份要求。":"执行要求："+CommandRules.Skills[t.skill]+" / "+t.required+"个成功 / 身份"+t.level+"级"+(string.IsNullOrEmpty(t.profession)?"":" / "+t.profession),28,276,844,38,16,Muted);
            Rule(p,28,313,844);
            Label(p,"负责人  "+a.name+" · "+CommandRules.Person(State,a.id).profession,28,327,557,34,23,Gold);
            Button("CommandCompare_"+t.id,p,"比较 / 更换  ›",640,327,232,34,()=>{modal="compareCandidates";Draw();},t.actor<0);
            int dice=CommandRules.TaskDice(State,a,t);string error=CommandRules.TaskError(State,a.id,t);
            Label(p,(DispatchRules.Probability(dice,CommandRules.ActionRequired(t))*100).ToString("0.#")+"%",28,374,180,44,28,Gold);
            Label(p,(t.reconnaissance?"调查":"本阶段")+"概率 · "+dice+"骰 / 需"+CommandRules.ActionRequired(t)+"成功\n每骰4—6计为成功",224,375,345,45,16,Muted);
            Label(p,"当前岗位\n"+CommandRole(a.id),608,375,264,45,16,Muted);
            Label(p,"行动方案",28,419,844,20,14,Muted);
            string[] descriptions={"常规\n不增加投入","充分筹备\n4 银两 · +1骰 · 暴露−2","紧急推进\n+1骰 · 暴露+8 · 压力+2"};
            for(int i=0;i<3;i++){int plan=i;Button("MissionPlan_"+i,p,descriptions[i],28+i*284,438,272,60,()=>{t.plan=plan;Draw();},t.actor<0,t.plan==i);}
            Button("MissionIntel",p,t.intelSupport?"✓ 情报支援 · 1份情报换 +1骰":"＋ 情报支援 · 可选1份情报换 +1骰",28,514,844,36,()=>{t.intelSupport=!t.intelSupport;Draw();},t.actor<0,t.intelSupport);
            Label(p,"本次投入  "+CommandRules.PlanCost(t)+" 银两 / "+(t.intelSupport?1:0)+" 份情报 / 体力 −14 / 精神 −6",28,571,844,25,17,TextColor);
            Label(p,"暴露 +"+CommandRules.TaskExposure(State,a,t)+(t.plan==2?"、压力 +2":"")+"；失败再增加暴露 +"+(t.reconnaissance?2:t.failExposure)+"、压力 +"+(t.reconnaissance?1:2)+"。已用资源不返还。\n"+(CommandRole(a.id)!="机动成员"?"岗位冲突：本夜优先执行"+(t.reconnaissance?"调查":"任务")+"，"+CommandRole(a.id)+"岗位将等待。":"授权预留资源；结束今夜后成员自行执行。"),28,610,844,65,16,Muted);
            Label(p,t.actor>=0?"已签发给 "+CommandName(t.actor)+"；撤回后可调整方案。":error!=""?error:"可签发 · 可用 "+(State.money-CommandRules.ReservedMoney(State,t.id))+" 银两 / "+(State.intelligence-CommandRules.ReservedIntel(State,t.id))+" 份情报",28,684,844,39,16,error==""?Teal:Red);
            Button("MissionBack",p,taskReturnModal=="memberDetails"?"返回个人档案":"返回"+CommandPageName(taskReturnTab),28,739,276,48,()=>{CloseTaskDialog();Draw();});
            if(t.actor>=0)Button("CommandCancel_"+t.id,p,"撤回授权 / 释放预留",324,739,548,48,()=>{t.actor=-1;toast="授权已撤回，释放成员及预留资源。";Save(false);Draw();});
            else Button("CommandAuthorize",p,(t.reconnaissance?"授权调查 · ":"签发给 ")+a.name,324,739,548,48,()=>{toast=CommandRules.Assign(State,a.id,t.id);if(toast==""){toast="已签发"+(t.reconnaissance?"调查":"执行")+"给 "+a.name+"；结束今夜后执行。";CloseTaskDialog(false);Save(false);}Draw();},error=="",true);
        }
    }
}
