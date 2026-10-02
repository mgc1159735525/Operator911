using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        // Choices have visible boundaries and persistent selection, unlike directory links.
        void MissionChoice(string name,Transform parent,string title,string detail,float x,float y,float w,float h,Action action,bool enabled,bool selected)
        {
            var button=Button(name,parent,"",x,y,w,h,action,enabled,selected);
            Color fill=selected?PaperInset:Paper;
            var colors=button.colors;colors.normalColor=fill;colors.selectedColor=fill;button.colors=colors;
            button.image.CrossFadeColor(enabled?fill:colors.disabledColor,0,true,true);
            Border(button.transform,0,0,w,h,selected?Accent:PaperLine);
            Color text=enabled?PaperInk:PaperMuted;
            string mark=name=="MissionIntel"?(selected?"☑  ":"□  "):(selected?"●  ":"○  ");
            Label(button.transform,mark+title,12,detail==""?0:8,w-24,detail==""?h:25,19,text,TextAnchor.MiddleLeft);
            if(detail!="")Label(button.transform,detail,12,34,w-24,h-36,17,PaperMuted);
        }
        void MissionButtonText(Button button,int size=19)
        {button.GetComponentInChildren<Text>().fontSize=size;}
        string MissionRiskText(CommandTask task,Agent actor)
        {
            return "暴露 +"+CommandRules.TaskExposure(State,actor,task)+(task.plan==2?" · 压力 +2":"")+
                "\n失败另加：暴露 +"+(task.reconnaissance?2:task.failExposure)+" / 压力 +"+(task.reconnaissance?1:2);
        }
        string MissionRequirements(CommandTask task)
        {return "执行要求："+CommandRules.Skills[task.skill]+" / "+task.required+"成功 / 身份"+task.level+"级"+(string.IsNullOrEmpty(task.profession)?"":" / "+task.profession);}
        string MissionSynopsis(CommandTask task)
        {
            string first=(task.body??"").Split('\n').FirstOrDefault(line=>!string.IsNullOrWhiteSpace(line))??"查看任务背景，了解本次指示。";
            var text=new System.Globalization.StringInfo(first.Trim());
            return text.LengthInTextElements>44?text.SubstringByTextElements(0,44)+"…":text.String;
        }
        void DrawMissionPlan()
        {
            var t=State.command.tasks.FirstOrDefault(x=>x.id==commandTask&&x.status==0);
            if(t==null){CloseTaskDialog();return;}
            var a=CommandRules.Agent(State,t.actor>=0?t.actor:selectedAgent);var person=CommandRules.Person(State,a.id);
            var p=CommandModal(t.title,736,1120);
            var header=p.Find("DossierHeading");header.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(924,43);
            MissionButtonText(Button("MissionClose",header,"关闭 ×",996,22,96,38,()=>{CloseTaskDialog();Draw();}));
            Label(p,State.config.District(t.district).name+"  /  "+t.origin+"  /  第 "+t.deadline+" 夜截止"+(t.stepsRequired>1?"  /  已完成"+t.stepsDone+"/"+t.stepsRequired+"阶段":""),32,94,1056,26,18,Muted).name="AssignmentStage";

            var main=Rect("MissionMainColumn",p,32,128,640,524);
            Label(main,MissionSynopsis(t),0,0,456,60,19).name="MissionBackgroundPreview";
            MissionButtonText(Button("MissionBackgroundDetails",main,"任务全文  ›",480,6,160,40,()=>{modal="missionBackground";Draw();}),18);
            Label(main,(t.stepsRequired>1?"全部阶段完成后  ":"目标完成后  ")+CommandRules.TaskReward(t),0,74,640,48,18,Teal).name="AssignmentOutcome";
            Rule(main,0,125,640);

            Label(main,"负责人",0,138,640,24,17,Muted);
            Portrait(main,a,0,170,50,73);
            Label(main,a.name,70,168,405,33,25,Gold);
            Label(main,person.profession+" "+person.level+"级 · "+CommandRules.Skills[CommandRules.ActionSkill(t)]+" "+new[]{a.intel,a.stealth,a.persuade,a.organize}[CommandRules.ActionSkill(t)],70,208,405,26,19,Muted);
            var compare=Button("CommandCompare_"+t.id,main,"比较 / 更换 ›",482,183,158,44,()=>{modal="compareCandidates";Draw();},t.actor<0);MissionButtonText(compare);
            Border(compare.transform,0,0,158,44,PaperLine);

            MissionChoice("MissionModeExecute",main,t.reconComplete?"执行 · 已获调查优势":"直接执行目标","",0,254,312,46,()=>{t.reconnaissance=false;Draw();},t.actor<0,!t.reconnaissance);
            MissionChoice("MissionModeRecon",main,t.reconComplete?"调查已完成":"先调查 · 占用一夜","",328,254,312,46,()=>{t.reconnaissance=true;Draw();},t.actor<0&&!t.reconComplete,t.reconnaissance);
            string actionHint=t.reconnaissance?"本夜用情报能力调查：需1成功，不推进任务阶段。":t.reconComplete?"调查优势：本阶段 +1骰、暴露−3。":"调查成功后，目标执行 +1骰、暴露−3。";
            Label(main,MissionRequirements(t)+"\n"+actionHint,0,312,640,48,18,Muted).name="AssignmentReconValue";

            Label(main,"行动方案",0,371,640,24,17,Muted);
            string[] titles={"常规","充分筹备","紧急推进"};
            string[] details={"不追加资源","4银两 · +1骰\n暴露−2","+1骰 · 暴露+8\n压力+2"};
            for(int i=0;i<3;i++){int plan=i;MissionChoice("MissionPlan_"+i,main,titles[i],details[i],i*218,400,204,80,()=>{t.plan=plan;Draw();},t.actor<0,t.plan==i);}
            MissionChoice("MissionIntel",main,"情报支援 · 1份换 +1骰","",0,488,640,36,()=>{t.intelSupport=!t.intelSupport;Draw();},t.actor<0,t.intelSupport);

            var summary=Box("MissionDecisionColumn",p,704,128,384,524,PaperInset);
            Label(summary,"本夜执行预览",24,18,336,26,18,Muted);
            int dice=CommandRules.TaskDice(State,a,t);string error=CommandRules.TaskError(State,a.id,t);
            Label(summary,(DispatchRules.Probability(dice,CommandRules.ActionRequired(t))*100).ToString("0.#")+"%",24,50,336,48,36,Gold).name="AssignmentChance";
            Label(summary,(t.reconnaissance?"调查":t.stepsRequired>1?"阶段"+(t.stepsDone+1)+"/"+t.stepsRequired:"当前阶段")+"成功率 · "+dice+"骰 / 需"+CommandRules.ActionRequired(t)+"成功",24,104,336,27,18,Muted).name="AssignmentRoll";
            Label(summary,CommandRules.TaskTimingText(State,t),24,146,336,48,19,CommandRules.TaskCanFinish(State,t)?TextColor:Red).name="AssignmentTiming";
            Rule(summary,24,201,336);
            Label(summary,"本夜投入 · 执行后扣除",24,216,336,24,17,Muted);
            Label(summary,CommandRules.PlanCost(t)+" 银两 · "+(t.intelSupport?1:0)+" 情报\n体力−"+CommandRules.ActionStaminaCost(State,a)+" · 精神−6",24,250,336,51,20).name="AssignmentCost";
            Label(summary,MissionRiskText(t,a),24,316,336,48,18,Muted).name="AssignmentRisk";
            Label(summary,AssignmentChainText(a.id),24,384,336,48,18,AssignmentChainImpact(a.id)>0?Red:Muted).name="AssignmentChainImpact";
            Label(summary,person.neglect>=2?"连勤"+(person.neglect+1)+"夜：信任−7 / 可疑+8":"连勤 "+(person.neglect+1)+" 夜 · 未触发家事惩罚",24,439,336,25,17,person.neglect>=2?Red:Muted).name="AssignmentLifeAlert";
            MissionButtonText(Button("MissionImpactDetails",summary,"后果与规则详情  ›",12,478,360,36,()=>{modal="missionImpact";Draw();}),18);

            Rule(p,32,668,1056);
            MissionButtonText(Button("MissionBack",p,taskReturnModal=="memberDetails"?"返回个人档案":taskReturnModal=="districtDetails"?"返回城区档案":"返回"+CommandPageName(taskReturnTab),24,676,194,48,()=>{CloseTaskDialog();Draw();}));
            Label(p,t.actor>=0?"已签发给 "+CommandName(t.actor)+"\n撤回后可调整安排。":error!=""?error:"可签发 · 可用 "+(State.money-CommandRules.ReservedMoney(State,t.id))+" 银两 / "+(State.intelligence-CommandRules.ReservedIntel(State,t.id))+" 情报\n结束今夜后，由成员执行。",238,679,438,49,17,error==""?Muted:Red).name="AssignmentBlockReason";
            if(t.actor>=0)MissionButtonText(Button("CommandCancel_"+t.id,p,"撤回授权 · 释放预留",704,676,384,48,()=>{t.actor=-1;toast="授权已撤回，释放成员及预留资源。";Save(false);Draw();}));
            else MissionButtonText(Button("CommandAuthorize",p,(t.reconnaissance?"授权调查 · ":"签发给 ")+a.name,704,676,384,48,()=>{toast=CommandRules.Assign(State,a.id,t.id);if(toast==""){toast="已签发"+(t.reconnaissance?"调查":"执行")+"给 "+a.name+"；结束今夜后执行。";CloseTaskDialog();Save(false);}Draw();},error=="",true),20);
        }
        void DrawMissionBackground()
        {
            var t=State.command.tasks.FirstOrDefault(x=>x.id==commandTask&&x.status==0);
            if(t==null){CloseTaskDialog();return;}
            var p=CommandModal("任务背景",620);
            Label(p,t.title,28,100,844,34,24,Gold);
            var description=Scroll(p,"TaskDescription_"+t.id,28,154,844,352,352);
            string follow=string.IsNullOrEmpty(t.followup)?"":"\n\n完成后解锁："+t.followup+"。";
            var body=Label(description,t.body+follow,0,0,810,352,20);
            float height=Mathf.Max(352,body.preferredHeight+12);body.rectTransform.sizeDelta=new Vector2(810,height);description.sizeDelta=new Vector2(description.sizeDelta.x,height);
            MissionButtonText(Button("MissionBackgroundBack",p,"返回任务安排",28,544,844,48,()=>{modal="missionPlan";Draw();}));
        }
        void DrawMissionImpact()
        {
            var t=State.command.tasks.FirstOrDefault(x=>x.id==commandTask&&x.status==0);
            if(t==null){CloseTaskDialog();return;}
            var a=CommandRules.Agent(State,t.actor>=0?t.actor:selectedAgent);var p=CommandModal("本夜后果 / "+a.name,628);
            Label(p,t.title+" · "+(t.reconnaissance?"调查目标":"执行目标"),28,102,844,30,21,Gold);
            Label(p,"投入与风险",28,153,844,27,20,Gold);
            Label(p,MissionRiskText(t,a)+"。\n行动一经执行，投入不会因失败而返还；签发后、执行前可以撤回。",28,191,844,79,19,Muted);
            Rule(p,28,287,844);
            Label(p,"联络安排",28,306,200,27,20,Gold);
            Label(p,AssignmentChainText(a.id)+"。\n任务优先于岗位工作；同一成员一夜只执行一项地下工作。",248,306,624,68,19,AssignmentChainImpact(a.id)>0?Red:Muted);
            Label(p,"生活与成长",28,405,200,27,20,Gold);
            Label(p,AssignmentLifeText(a)+(t.reconnaissance?"\n调查不积累行动经验，也不增加库存情报。":"\n最终完成任务可积累对应能力经验，中间阶段不计。"),248,405,624,89,19,CommandRules.Person(State,a.id).neglect>=2?Red:Muted).name="AssignmentWorkload";
            MissionButtonText(Button("MissionImpactBack",p,"返回任务安排",28,552,844,48,()=>{modal="missionPlan";Draw();}));
        }
    }
}
