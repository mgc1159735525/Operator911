using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        int campaignPage;

        void OpenCampaignDossier(int page=0)
        {
            campaignPage=Mathf.Clamp(page,0,2);commandTab=3;modal="campaignDossier";Draw();
        }

        string CampaignDeskObjective()
        {
            if(!CommandRules.HasCommandCampaign(State))return "阶段目标  /  "+GateProgress().Replace(" / 本阶段","").Replace("\n"," · ");
            var campaign=State.command.campaign;var chapter=CommandRules.CampaignCurrentChapter(State);
            return campaign.scenarioData.title+"  /  "+(chapter==null?"三章委任完成 · 继续达成组织目标":"第 "+(campaign.chapter+1)+" 章 · "+chapter.title)+"  ·  已完成 "+campaign.chapter+" / 3 章";
        }

        CommandStep CampaignNextStep()
        {
            if(!CommandRules.HasCommandCampaign(State))return null;
            var campaign=State.command.campaign;
            var task=State.command.tasks.FirstOrDefault(t=>t.id==campaign.activeTask&&t.status==0);
            if(task!=null)
            {
                if(task.actor>=0)return null;
                if(!CommandRules.CanStaffTask(State,task))return null;
                return TaskStep(task,"：主线仍待签发。已完成阶段会保留，可更换负责人继续。","推进本章主线  ›");
            }
            if(campaign.chapter>=3)return null;
            if(!State.agents.Any(a=>CommandRules.Availability(State,a.id)==""&&!State.command.tasks.Any(t=>t.status==0&&t.actor==a.id)))return null;
            bool available=Enumerable.Range(0,2).Any(i=>CommandRules.CampaignApproachError(State,i)=="");
            bool arranged=State.command.tasks.Any(t=>t.status==0&&t.actor>=0);
            return new CommandStep{message=CommandRules.CampaignObjective(State)+(available?"。选择处理路线；已有安排时也可以直接结束今夜。":"。查看本章缺少的条件与可用路线。"),label="查看主线委任  ›",dialog="campaignDossier",ready=arranged};
        }

        void CampaignBodyText(Transform parent,string key,string text,float x,float y,float width,float height,int size=19)
        {
            var scroll=Scroll(parent,key,x,y,width,height,height);
            var label=Label(scroll,text,0,0,width-28,height,size,TextColor);
            float actual=Mathf.Max(height,label.preferredHeight+10);
            label.rectTransform.sizeDelta=new Vector2(width-28,actual);
            scroll.sizeDelta=new Vector2(scroll.sizeDelta.x,actual);
        }

        void DrawCampaignDossier()
        {
            if(!CommandRules.HasCommandCampaign(State)){modal="commandOverview";DrawCommandOverview();return;}
            var campaign=State.command.campaign;
            var p=CommandModal("地区委任 / "+campaign.scenarioData.title,760,1120);
            Label(p,"第 "+State.turn+" / "+State.config.Value("turns")+" 夜  ·  "+CommandRules.CampaignProgressText(State),32,95,1056,28,18,Muted);
            string[] pages={"当前主线","情报档案","行动进度"};
            for(int i=0;i<3;i++)
            {
                int next=i;
                Button("CampaignPage_"+i,p,pages[i],32+i*263,135,247,40,()=>{campaignPage=next;Draw();},true,campaignPage==i);
            }
            Button("CampaignOrganization",p,"组织概况  ›",821,135,267,40,()=>{modal="commandOverview";Draw();});
            Rule(p,32,190,1056);
            if(campaignPage==0)DrawCampaignObjective(p);
            else if(campaignPage==1)
            {
                Label(p,"来自敌方机构的已送达档案",32,209,1056,31,24,Gold);
                Label(p,"机构内线产出，经联络链送达后永久留档；核证不消耗库存情报。",32,251,1056,27,18,Muted);
                CampaignBodyText(p,"CampaignEvidence",CommandRules.CampaignEvidenceSummary(State),32,299,1056,337);
                Button("CampaignRoutes",p,"安排机构内线与联络链  ›",32,651,502,39,()=>OpenCommandPage(1));
                Button("CampaignPeople",p,"查看成员任职  ›",552,651,536,39,()=>OpenCommandPage(2));
            }
            else
            {
                Label(p,"本局已发生的改变",32,209,1056,31,24,Gold);
                string progress=campaign.scenarioData.premise+"\n\n完整胜利还需维持组织：\n"+GateProgress()+"\n\n"+string.Join("\n\n",campaign.history.ToArray());
                if(campaign.history.Count==0)progress+="\n行动刚开始。完成章节后，这里会记录采用的方法与城区变化。";
                CampaignBodyText(p,"CampaignHistory",progress,32,261,1056,327);
                Label(p,OpeningChecklist(),32,605,1056,81,18,Muted);
            }
            Rule(p,32,704,1056);
            Button("CampaignClose",p,"返回城区地图",32,714,276,38,()=>{modal="";commandTab=3;Draw();});
            Label(p,"完整胜利：三章委任 + 组织门槛。查看不耗时，结束今夜才执行。",330,720,758,27,16,Muted,TextAnchor.MiddleRight);
        }

        void DrawCampaignObjective(Transform panel)
        {
            var campaign=State.command.campaign;
            var chapter=CommandRules.CampaignCurrentChapter(State);
            if(chapter==null)
            {
                Label(panel,"委任已完成",32,215,1056,40,28,Gold);
                CampaignBodyText(panel,"CampaignComplete",campaign.scenarioData.endingSuccess,32,274,1056,374);
                return;
            }
            Label(panel,"第 "+(campaign.chapter+1)+" 章  /  "+chapter.title,32,211,1056,36,26,Gold);
            CampaignBodyText(panel,"CampaignBrief",chapter.briefing,32,257,1056,68);
            var active=State.command.tasks.FirstOrDefault(t=>t.id==campaign.activeTask&&t.status==0);
            if(active!=null)
            {
                var card=Box("CampaignActiveTask",panel,32,365,1056,218,PaperInset);
                Label(card,"当前方法  /  "+active.title,24,22,1008,32,24,Gold);
                Label(card,TaskOrderText(active)+"  ·  已完成 "+active.stepsDone+" / "+active.stepsRequired+" 阶段",24,69,1008,28,19,TextColor);
                Label(card,CommandRules.TaskTimingText(State,active)+"\n阶段成功保留进度；每次执行仍需签发。",24,109,1008,58,18,Muted);
                string changeError=CommandRules.CancelCampaignApproachError(State);
                bool canChange=changeError=="";
                Label(card,canChange?"尚未执行，可重新选择本章方法。":changeError,24,177,1008,29,17,Muted);
                Button("CampaignChangeApproach",panel,"重新选择方法",32,605,330,58,()=>
                {
                    string error=CommandRules.CancelCampaignApproach(State);
                    toast=error==""?"未执行的方法已撤回，可重新选择；时间与资源未消耗。":error;
                    if(error=="")Save(false);Draw();
                },canChange);
                Button("CampaignTask",panel,active.actor<0?"比较人选并签发本章任务  ›":"查看已签发的主线任务  ›",382,605,706,58,()=>OpenCommandTask(active.id,true),true,true);
                return;
            }
            for(int i=0;i<2;i++)
            {
                int approach=i;float x=32+i*540;
                var method=chapter.approaches[i];
                var card=Box("CampaignApproachCard_"+i,panel,x,337,516,296,PaperInset);
                Label(card,CommandRules.CampaignApproachName(State,i),20,17,476,32,23,Gold);
                string error=CommandRules.CampaignApproachError(State,i);
                Label(card,method.body,20,63,476,77,18,TextColor);
                Label(card,CommandRules.Skills[method.skill]+" · 每阶段需"+method.required+"成功 · 最少"+method.steps+"夜",20,150,476,27,18,Gold);
                string reward="完成：成果+"+method.reward+(method.money>0?" / 银两+"+method.money:"")+(method.pressureRelief>0?" / 压力−"+method.pressureRelief:"")+(method.traceRelief>0?" / 追查−"+method.traceRelief:"")+(method.heartsReward>0?" / 民心+"+method.heartsReward:"");
                Label(card,reward,20,190,476,44,17,TextColor);
                string access=error;
                if(error.StartsWith("尚未收到"))access="缺少"+CommandRules.Institutions[method.evidenceInstitution]+"档案。可选另一方法，或先安排该机构联络。";
                if(error=="")access=method.evidenceInstitution>0?"档案已齐 · 不消耗库存情报；下一步指派成员。":"无需特定任职或档案 · 下一步指派成员。";
                Label(card,access,20,238,476,51,17,error==""?Muted:Red);
                Button("CampaignApproach_"+i,panel,"选择此方法  ›",x,643,516,46,()=>
                {
                    string result=CommandRules.StartCampaignApproach(State,approach);
                    if(result!=""){toast=result;Draw();return;}
                    Save(false);OpenCommandTask(State.command.campaign.activeTask,true);
                },error=="",error==""&&Enumerable.Range(0,2).Count(n=>CommandRules.CampaignApproachError(State,n)=="")==1);
            }
        }
    }
}
