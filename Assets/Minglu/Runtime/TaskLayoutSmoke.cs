using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        RectTransform TaskLayoutRect(string name)
        {
            var matches=root.GetComponentsInChildren<RectTransform>().Where(r=>r.name==name&&r.gameObject.activeInHierarchy).ToArray();
            if(matches.Length!=1)throw new Exception("Task layout needs exactly one visible "+name+", found "+matches.Length);
            return matches[0];
        }
        Rect TaskLayoutBounds(RectTransform child,RectTransform parent)
        {
            var corners=new Vector3[4];child.GetWorldCorners(corners);
            var points=corners.Select(c=>parent.InverseTransformPoint(c)).ToArray();
            return UnityEngine.Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
        }
        void RequireTaskLayoutGeometry()
        {
            Canvas.ForceUpdateCanvases();
            var panel=TaskLayoutRect("CommandModal");
            if(panel.rect.height>760||Mathf.Abs(panel.rect.width-1120)>1)throw new Exception("Task dossier must use the compact 1120-wide layout: "+panel.rect.size);
            var left=TaskLayoutRect("MissionMainColumn");var right=TaskLayoutRect("MissionDecisionColumn");
            Rect leftBounds=TaskLayoutBounds(left,panel),rightBounds=TaskLayoutBounds(right,panel);
            if(leftBounds.xMax>rightBounds.xMin-8||leftBounds.yMin<panel.rect.yMin||rightBounds.yMin<panel.rect.yMin)
                throw new Exception("Task reading and decision columns overlap or overflow the modal");
            var authorize=(RectTransform)ProducerButton("CommandAuthorize").transform;
            Rect footer=TaskLayoutBounds(authorize,panel);
            if(authorize.GetComponentInParent<ScrollRect>()!=null||footer.yMin<panel.rect.yMin||footer.yMax>panel.rect.yMin+112)
                throw new Exception("Task sign-off must remain fixed inside the bottom action area");
            RequireAssignmentTextFits("MissionBackgroundPreview","AssignmentStage","AssignmentRoll","AssignmentOutcome","AssignmentTiming","AssignmentChainImpact","AssignmentCost","AssignmentReconValue","AssignmentRisk","AssignmentLifeAlert","AssignmentBlockReason");
            foreach(var label in left.GetComponentsInChildren<Text>().Concat(right.GetComponentsInChildren<Text>()))
            {
                if(string.IsNullOrWhiteSpace(label.text))continue;
                if(label.preferredHeight>label.rectTransform.rect.height+2)throw new Exception("Task column text clipped: "+label.name+" needs "+label.preferredHeight+" / has "+label.rectTransform.rect.height+" / "+label.text);
            }
            if(root.GetComponentsInChildren<Text>().Any(t=>t.name=="AssignmentWorkload"))throw new Exception("Detailed living consequences still crowd the main task dossier");
        }
        void RequireTaskLayoutBlocked(CommandTask task)
        {
            string error=CommandRules.TaskError(State,selectedAgent,task);
            if(string.IsNullOrEmpty(error)||ProducerButton("CommandAuthorize").interactable)throw new Exception("Blocked task remained signable");
            if(!AssignmentText("AssignmentBlockReason").text.Contains(error))throw new Exception("Blocked task omitted its actionable reason: "+error);
            RequireAssignmentTextFits("AssignmentBlockReason","AssignmentTiming");
        }
        IEnumerator TaskLayoutOpenDistrictTask(CommandTask task)
        {
            yield return AssignmentScrollTo("DistrictTasks_"+task.district,"DistrictTask_"+task.id);
            yield return Click("DistrictTask_"+task.id);
            if(modal!="missionPlan")throw new Exception("District control did not open the task dossier");
        }
        IEnumerator TaskLayoutSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Task layout smoke requires an isolated save");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            State.command.config.eventChance=0;State.command.notices.Clear();
            foreach(var member in State.command.people){member.policy=0;member.leaveUntil=0;member.neglect=0;member.level=2;}
            foreach(var agent in State.agents){agent.intel=agent.stealth=agent.persuade=agent.organize=6;agent.stamina=agent.spirit=100;agent.exposure=0;}
            foreach(var item in State.command.tasks)item.actor=-1;
            var task=State.command.tasks[0];task.level=1;task.profession=State.command.people[0].profession;task.required=2;task.stepsRequired=2;task.stepsDone=0;task.deadline=State.config.Value("turns");task.plan=0;task.intelSupport=false;task.reconnaissance=false;task.reconComplete=false;task.followup="";
            task.body=string.Concat(Enumerable.Repeat("这份长背景用于验证纸质档案中的正文阅读。任务要求、投入与签发按钮应始终留在固定区域，背景可以单独滚动。\n",16))+"正文末尾仍然可读。";
            State.intelligence=3;int initialTurn=State.turn,initialMoney=State.money,initialIntel=State.intelligence;uint initialRandom=State.random;
            Draw();yield return null;
            yield return Click("CommandDistrict_"+task.district);yield return TaskLayoutOpenDistrictTask(task);yield return RequireMapDeskBlocked();
            RequireTaskLayoutGeometry();
            var currentMember=CommandRules.Person(State,selectedAgent);int originalNeglect=currentMember.neglect;
            currentMember.neglect=2;Draw();yield return null;RequireTaskLayoutGeometry();
            string lifeAlert=AssignmentText("AssignmentLifeAlert").text;
            if(!lifeAlert.Contains("信任−7")||!lifeAlert.Contains("可疑+8"))throw new Exception("Third-night work hid its trust and suspicion penalties from the main decision panel");
            Capture(Path.ChangeExtension(path,"task-layout-life-risk.png"));
            currentMember.neglect=originalNeglect;Draw();yield return null;
            if(!AssignmentText("AssignmentStage").text.Replace(" ","").Contains("已完成0/2阶段")||!AssignmentText("AssignmentRoll").text.Replace(" ","").Contains("阶段1/2"))
                throw new Exception("Multi-stage progress or the current roll stage is hidden on the main task dossier");
            var preview=AssignmentText("MissionBackgroundPreview");
            if(preview.GetComponentInParent<ScrollRect>()!=null||root.GetComponentsInChildren<ScrollRect>().Any(s=>s.name=="TaskDescription_"+task.id)||preview.text.Length>=task.body.Length)
                throw new Exception("Main task background must be a fixed short preview, with the complete body in a separate dialog");
            int backgroundActor=selectedAgent,backgroundPlan=task.plan;bool backgroundRecon=task.reconnaissance,backgroundIntel=task.intelSupport;
            Vector2 mainSize=TaskLayoutRect("CommandModal").rect.size;
            Capture(Path.ChangeExtension(path,"task-layout-long.png"));
            yield return Click("MissionBackgroundDetails");
            if(modal!="missionBackground")throw new Exception("Full task background did not open in its own reading dialog");
            var description=root.GetComponentsInChildren<ScrollRect>().First(s=>s.name=="TaskDescription_"+task.id);
            var body=description.content.GetComponentsInChildren<Text>().First();
            if(body.preferredHeight>body.rectTransform.rect.height+2||description.content.rect.height<=description.viewport.rect.height+100)
                throw new Exception("Long task background is clipped or has no independent scroll range");
            Capture(Path.ChangeExtension(path,"task-layout-background.png"));
            description.verticalNormalizedPosition=0;yield return null;yield return new WaitForEndOfFrame();
            if(description.verticalNormalizedPosition>.01f||!body.text.Contains("正文末尾仍然可读。"))throw new Exception("Long background cannot reach its final paragraph");
            Capture(Path.ChangeExtension(path,"task-layout-scrolled.png"));
            yield return Click("MissionBackgroundBack");
            if(modal!="missionPlan"||TaskLayoutRect("CommandModal").rect.size!=mainSize||selectedAgent!=backgroundActor||task.plan!=backgroundPlan||task.reconnaissance!=backgroundRecon||task.intelSupport!=backgroundIntel)
                throw new Exception("Closing the full background changed the main layout or current task choices");
            RequireTaskLayoutGeometry();
            yield return Click("MissionBackgroundDetails");
            if(!HandleCommandModalEscape())throw new Exception("Full task background did not handle Escape");
            yield return null;
            if(modal!="missionPlan"||selectedAgent!=backgroundActor||task.plan!=backgroundPlan||task.reconnaissance!=backgroundRecon||task.intelSupport!=backgroundIntel)
                throw new Exception("Escape from the full background lost the current task choices");
            RequireTaskLayoutGeometry();

            yield return Click("MissionPlan_1");yield return Click("MissionModeRecon");yield return Click("MissionIntel");
            int chosenActor=selectedAgent,chosenPlan=task.plan;bool chosenRecon=task.reconnaissance,chosenIntel=task.intelSupport;
            yield return Click("MissionImpactDetails");
            if(modal!="missionImpact")throw new Exception("Detailed impact did not open in its own dialog");
            RequireAssignmentTextFits("AssignmentWorkload");Capture(Path.ChangeExtension(path,"task-layout-impact.png"));
            // The same handler is called by CityGameUI.Update when Escape is pressed.
            if(!HandleCommandModalEscape())throw new Exception("Impact dialog did not handle Escape");
            yield return null;
            if(modal!="missionPlan"||selectedAgent!=chosenActor||task.plan!=chosenPlan||task.reconnaissance!=chosenRecon||task.intelSupport!=chosenIntel)
                throw new Exception("Returning from task impact lost the selected person, mode or plan");
            yield return Click("MissionImpactDetails");yield return Click("MissionImpactBack");
            if(modal!="missionPlan")throw new Exception("Impact return button did not match Escape behavior");
            if(State.turn!=initialTurn||State.money!=initialMoney||State.intelligence!=initialIntel||State.random!=initialRandom)
                throw new Exception("Planning controls spent resources, time or random rolls before signing");

            State.money=0;Draw();yield return null;RequireTaskLayoutBlocked(task);Capture(Path.ChangeExtension(path,"task-layout-no-money.png"));
            State.money=initialMoney;yield return Click("MissionModeExecute");task.deadline=State.turn;task.stepsRequired=2;Draw();yield return null;
            RequireTaskLayoutBlocked(task);Capture(Path.ChangeExtension(path,"task-layout-no-time.png"));
            task.deadline=State.config.Value("turns");task.stepsRequired=1;task.reconComplete=true;Draw();yield return null;
            string requirements=AssignmentText("AssignmentReconValue").text;
            if(!requirements.Contains(CommandRules.Skills[task.skill])||!requirements.Contains(task.required+"成功")||!requirements.Contains("身份"+task.level+"级")||!requirements.Contains(task.profession))
                throw new Exception("Completed reconnaissance hid the target's original skill and identity requirements");
            RequireTaskLayoutGeometry();
            yield return Click("CommandAuthorize");
            if(task.actor!=chosenActor||modal!="districtDetails"||commandTab!=3||selectedDistrict!=task.district||CommandRules.ReservedMoney(State)!=4||CommandRules.ReservedIntel(State)!=1)
                throw new Exception("Signing did not reserve the chosen plan and return to the source district");
            yield return TaskLayoutOpenDistrictTask(task);yield return Click("CommandCancel_"+task.id);yield return Click("MissionBack");
            if(task.actor>=0||modal!="districtDetails"||CommandRules.ReservedMoney(State)!=0||CommandRules.ReservedIntel(State)!=0)
                throw new Exception("Revocation lost source context or retained resource reservations");
            if(State.turn!=initialTurn||State.money!=initialMoney||State.intelligence!=initialIntel||State.random!=initialRandom)
                throw new Exception("Signing or revoking the plan changed resources or advanced execution");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_TASK_LAYOUT_UI_SUCCESS: compact columns, fixed sign-off, short background preview/full reading dialog, visible stage progress, readable decision text, resource/time reasons, background/impact Escape, unchanged planning state, district sign/revoke return");
        }
    }
}
