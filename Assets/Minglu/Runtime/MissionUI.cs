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
            var a=CommandRules.Agent(State,t.actor>=0?t.actor:selectedAgent);var p=CommandModal(t.title,858);
            Label(p,State.config.District(t.district).name+" · "+t.origin+" · 第 "+t.deadline+" 夜截止",28,80,844,27,18,Muted);
            var description=Scroll(p,"TaskDescription_"+t.id,28,117,844,78,78);
            string follow=t.stepsRequired>1?"阶段 "+t.stepsDone+" / "+t.stepsRequired+"；完成最后阶段才结算。":string.IsNullOrEmpty(t.followup)?"":"完成后解锁："+t.followup+"。";
            var text=Label(description,t.body+(follow==""?"":"\n"+follow),0,0,810,78,19);
            float h=Mathf.Max(78,text.preferredHeight+8);text.rectTransform.sizeDelta=new Vector2(810,h);description.sizeDelta=new Vector2(description.sizeDelta.x,h);
            Label(p,"完成目标后  "+CommandRules.TaskReward(t),28,204,844,27,18,Teal).name="AssignmentOutcome";
            Button("MissionModeExecute",p,t.reconComplete?"执行目标 · 已有调查优势":"直接执行目标",28,243,408,42,()=>{t.reconnaissance=false;Draw();},t.actor<0,!t.reconnaissance);
            Button("MissionModeRecon",p,t.reconComplete?"调查已完成":"调查目标 · 额外占用一夜",456,243,416,42,()=>{t.reconnaissance=true;Draw();},t.actor<0&&!t.reconComplete,t.reconnaissance);
            string requirements="执行要求："+CommandRules.Skills[t.skill]+" / "+t.required+"成功 / 身份"+t.level+"级"+(string.IsNullOrEmpty(t.profession)?"":" / "+t.profession);
            Label(p,t.reconnaissance?"调查：情报 / 1成功；本夜不推进阶段，不产出库存情报。\n"+requirements+"；调查成功后执行+1骰、暴露−3。":requirements+"\n"+(t.reconComplete?"调查优势保留：执行+1骰、暴露−3；原有身份门槛不变。":"调查可改善后续判定，但额外占用一夜；也可直接执行。"),28,296,844,44,17,Muted).name="AssignmentReconValue";
            Rule(p,28,345,844);
            Label(p,"负责人  "+a.name+" · "+CommandRules.Person(State,a.id).profession,28,357,557,32,23,Gold);
            Button("CommandCompare_"+t.id,p,"比较 / 更换  ›",640,357,232,34,()=>{modal="compareCandidates";Draw();},t.actor<0);
            int dice=CommandRules.TaskDice(State,a,t);string error=CommandRules.TaskError(State,a.id,t);
            Label(p,(DispatchRules.Probability(dice,CommandRules.ActionRequired(t))*100).ToString("0.#")+"%",28,399,164,43,29,Gold);
            Label(p,(t.reconnaissance?"本夜调查":"本阶段")+"成功率\n"+dice+"骰 / 需"+CommandRules.ActionRequired(t)+"成功",192,397,290,51,18,Muted);
            Label(p,CommandRules.TaskTimingText(State,t)+"\n每次签发执行一夜；失败需要重新安排。",478,397,394,52,17,CommandRules.TaskCanFinish(State,t)?Muted:Red).name="AssignmentTiming";
            Label(p,"行动方案 · 每阶段独立投入",28,459,844,23,17,Muted);
            string[] descriptions={"常规\n不追加资源","充分筹备\n4银两 · +1骰 · 暴露−2","紧急推进\n+1骰 · 暴露+8 · 压力+2"};
            for(int i=0;i<3;i++){int plan=i;Button("MissionPlan_"+i,p,descriptions[i],28+i*284,489,272,58,()=>{t.plan=plan;Draw();},t.actor<0,t.plan==i);}
            Button("MissionIntel",p,t.intelSupport?"✓ 本阶段情报支援 · 消耗1份 / +1骰":"＋ 本阶段情报支援 · 可选1份 / +1骰",28,559,844,37,()=>{t.intelSupport=!t.intelSupport;Draw();},t.actor<0,t.intelSupport);
            Label(p,"本夜投入",28,612,260,22,16,Muted);Label(p,"联络影响",316,612,260,22,16,Muted);Label(p,"生活安排",604,612,268,22,16,Muted);
            Label(p,CommandRules.PlanCost(t)+"银两 / "+(t.intelSupport?1:0)+"情报\n体力−"+CommandRules.ActionStaminaCost(State,a)+" / 精神−6",28,642,260,50,18).name="AssignmentCost";
            Label(p,AssignmentChainText(a.id),316,642,260,50,18,AssignmentChainImpact(a.id)>0?Red:TextColor).name="AssignmentChainImpact";
            Label(p,AssignmentLifeText(a),604,642,268,50,18,CommandRules.Person(State,a.id).neglect>=2?Red:TextColor).name="AssignmentWorkload";
            Label(p,"暴露 +"+CommandRules.TaskExposure(State,a,t)+(t.plan==2?"、压力 +2":"")+"；失败额外暴露 +"+(t.reconnaissance?2:t.failExposure)+"、压力 +"+(t.reconnaissance?1:2)+"。投入不返还。",28,706,844,24,17,Muted);
            Label(p,t.actor>=0?"已签发给 "+CommandName(t.actor)+"；撤回后可调整方案。":error!=""?error:"可签发 · 可用 "+(State.money-CommandRules.ReservedMoney(State,t.id))+" 银两 / "+(State.intelligence-CommandRules.ReservedIntel(State,t.id))+" 份情报",28,739,844,32,17,error==""?Teal:Red);
            Button("MissionBack",p,taskReturnModal=="memberDetails"?"返回个人档案":taskReturnModal=="districtDetails"?"返回城区档案":"返回"+CommandPageName(taskReturnTab),28,785,276,48,()=>{CloseTaskDialog();Draw();});
            if(t.actor>=0)Button("CommandCancel_"+t.id,p,"撤回授权 / 释放预留",324,785,548,48,()=>{t.actor=-1;toast="授权已撤回，释放成员及预留资源。";Save(false);Draw();});
            else Button("CommandAuthorize",p,(t.reconnaissance?"授权调查 · ":"签发给 ")+a.name,324,785,548,48,()=>{toast=CommandRules.Assign(State,a.id,t.id);if(toast==""){toast="已签发"+(t.reconnaissance?"调查":"执行")+"给 "+a.name+"；结束今夜后执行。";CloseTaskDialog();Save(false);}Draw();},error=="",true);
        }
    }
}
