using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        void RequireCompactControl(string name,bool icon=true)
        {
            Canvas.ForceUpdateCanvases();
            var button=ProducerButton(name);
            if(button==null||!button.IsInteractable())throw new Exception("Compact control is unavailable: "+name);
            if(icon&&!button.GetComponentsInChildren<Image>().Any(i=>i.name.StartsWith("Icon_")&&i.sprite!=null))
                throw new Exception("Compact control has no loaded pictogram: "+name);
            var rect=(RectTransform)button.transform;
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var screen=RectTransformUtility.WorldToScreenPoint(viewCamera,corner);
                if(screen.x<-.5f||screen.x>Screen.width+.5f||screen.y<-.5f||screen.y>Screen.height+.5f)
                    throw new Exception("Compact control leaves the screen: "+name+" at "+screen);
            }
            if(rect.rect.height<30)throw new Exception("Compact control is too short to target: "+name);
            foreach(var label in button.GetComponentsInChildren<Text>())
                if(!string.IsNullOrWhiteSpace(label.text)&&label.preferredHeight>label.rectTransform.rect.height+2)
                    throw new Exception("Compact control label is clipped: "+name+" / "+label.text);
        }
        string CompactBriefingText()
        {
            var scroll=root.GetComponentsInChildren<ScrollRect>().Single(s=>s.name=="BriefingBody");
            return string.Join("\n",scroll.content.GetComponentsInChildren<Text>().Select(t=>t.text));
        }
        IEnumerator CompactBriefingRoundTrip(string control,string path,string capture,params string[] required)
        {
            RequireCompactControl(control);
            string before=JsonUtility.ToJson(State),priorModal=modal;
            int priorTab=commandTab,priorActor=selectedAgent,priorDistrict=selectedDistrict,priorTask=commandTask,priorFilter=taskFilter,priorReportNight=reportNight;
            bool priorReportAll=reportAll;
            string beforeFile=File.Exists(SavePath)?File.ReadAllText(SavePath):null;
            yield return KeyboardSubmit(control);
            if(modal!="commandBriefing")throw new Exception("Compact help did not open: "+control);
            if(EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="BriefingClose")
                throw new Exception("Read-only explanation should initially focus its safe return control: "+control);
            RequireCompactControl("BriefingClose");
            foreach(string text in required)
                if(!CompactBriefingText().Contains(text))throw new Exception("Explanation omits essential rule: "+control+" / "+text);
            if(!HandleBriefingKeyboard())throw new Exception("Explanation does not consume global keyboard shortcuts");
            if(InTutorial&&root.GetComponentsInChildren<Transform>().Any(t=>t.name=="TutorialCoach"))
                throw new Exception("Tutorial coach crowds the explanation reading layer");
            RequireKeyboardScope("CommandShade");
            yield return RequireMapDeskBlocked();
            // Execute Unity's Submit handler on a disabled background control; a
            // pointer shield alone does not establish keyboard isolation.
            var hidden=ProducerButton("CommandEnd");
            if(hidden==null||hidden.IsInteractable())throw new Exception("Explanation leaves end-night available");
            ExecuteEvents.Execute(hidden.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            yield return null;yield return new WaitForEndOfFrame();
            if(modal!="commandBriefing"||JsonUtility.ToJson(State)!=before)throw new Exception("Background Submit acted through an explanation");
            if(!string.IsNullOrEmpty(capture))Capture(Path.ChangeExtension(path,capture+".png"));
            yield return KeyboardSubmit("BriefingClose");
            if(modal!=priorModal||commandTab!=priorTab||selectedAgent!=priorActor||selectedDistrict!=priorDistrict||commandTask!=priorTask||taskFilter!=priorFilter||reportNight!=priorReportNight||reportAll!=priorReportAll)
                throw new Exception("Explanation return lost decision context: "+control);
            if(EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!=control)
                throw new Exception("Explanation return lost the opening keyboard focus: "+control);
            if(JsonUtility.ToJson(State)!=before||(File.Exists(SavePath)?File.ReadAllText(SavePath):null)!=beforeFile)
                throw new Exception("Reading an explanation mutated the run or save: "+control);
            if(HandleBriefingKeyboard())throw new Exception("Explanation shortcut guard remained active after returning");
        }
        IEnumerator CompactUiSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Compact UI smoke requires --minglu-capture isolated saves");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            State.command.config.eventChance=0;State.command.notices.Clear();Draw();
            yield return null;yield return new WaitForEndOfFrame();
            RequireMapDesk();
            for(int metric=0;metric<5;metric++)RequireCompactControl("DeskMetric_"+metric);
            RequireCompactControl("DeskAdviceInfo");
            Capture(Path.ChangeExtension(path,"compact-map.png"));
            for(int metric=0;metric<5;metric++)yield return CompactBriefingRoundTrip("DeskMetric_"+metric,path,metric==1?"compact-intelligence-help":"");
            yield return CompactBriefingRoundTrip("DeskAdviceInfo",path,"compact-advice-help");

            yield return SmokeOpenBusiness(0);
            yield return CompactBriefingRoundTrip("TasksHelp",path,"compact-tasks-help","阶段");
            Capture(Path.ChangeExtension(path,"compact-tasks.png"));
            yield return SmokeOpenBusiness(1);
            var route=State.command.routes[0];
            string routeError=CommandRules.RouteError(State,route);
            if(routeError!=""&&(ProducerButton("RouteEnable_"+route.id).interactable||!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(routeError))))
                throw new Exception("Shortened chain view hid its blocking reason");
            yield return CompactBriefingRoundTrip("RoutesHelp",path,"compact-routes-help","机构","内线","暂停","任务");
            Capture(Path.ChangeExtension(path,"compact-routes.png"));
            yield return SmokeOpenBusiness(2);
            yield return CompactBriefingRoundTrip("PeopleHelp",path,"","12","被捕");
            Capture(Path.ChangeExtension(path,"compact-people.png"));
            yield return Click("CommandPeopleDetails");
            RequireAssignmentTextFits("MemberSkillGrowth","MemberGrowthChoice");
            yield return CompactBriefingRoundTrip("MemberGrowthHelp",path,"compact-growth-help","4","6","身份");
            yield return CompactBriefingRoundTrip("MemberHistoryHelp",path,"compact-history-help");
            Capture(Path.ChangeExtension(path,"compact-member.png"));

            // Stress the same detail component with a long explanation. The test
            // content remains in this isolated process and is never exported.
            string longBefore=JsonUtility.ToJson(State);int longActor=selectedAgent;
            EventSystem.current.SetSelectedGameObject(ProducerButton("MemberGrowthHelp").gameObject);
            OpenBriefing("长说明回归",string.Concat(Enumerable.Repeat("规则说明使用独立滚动区；查看不会消耗资源、改派成员或推进夜晚。\n",48))+"最后一行：可以返回原人物。");
            yield return null;yield return new WaitForEndOfFrame();
            var body=root.GetComponentsInChildren<ScrollRect>().Single(s=>s.name=="BriefingBody");
            if(body.content.rect.height<body.viewport.rect.height+200)throw new Exception("Long compact explanation has no scroll range");
            foreach(var label in body.content.GetComponentsInChildren<Text>())
                if(label.preferredHeight>label.rectTransform.rect.height+2)throw new Exception("Long explanation truncates its final lines");
            body.verticalNormalizedPosition=0;yield return null;yield return new WaitForEndOfFrame();
            if(body.verticalNormalizedPosition>.01f||!CompactBriefingText().Contains("最后一行"))throw new Exception("Explanation cannot reach its last line");
            Capture(Path.ChangeExtension(path,"compact-help-scrolled.png"));
            if(!HandleCommandModalEscape())throw new Exception("Explanation does not share the Escape return path");
            yield return null;
            if(modal!="memberDetails"||selectedAgent!=longActor||JsonUtility.ToJson(State)!=longBefore)
                throw new Exception("Escape from a long explanation changed the viewed member or run");
            yield return Click("MemberDetailsClose");
            yield return SmokeOpenBusiness(4);
            yield return CompactBriefingRoundTrip("ReportHelp",path,"compact-report-help","夜");

            // The compact presentation must preserve decisions and the reasons a
            // decision is unavailable; readable decoration is insufficient.
            foreach(var person in State.command.people){person.policy=0;person.leaveUntil=0;person.neglect=0;person.level=2;}
            foreach(var agent in State.agents){agent.intel=agent.persuade=agent.stealth=agent.organize=6;agent.stamina=agent.spirit=100;agent.exposure=0;}
            foreach(var item in State.command.tasks)item.actor=-1;
            var task=State.command.tasks[0];task.required=1;task.level=1;task.profession="";task.deadline=State.config.Value("turns");task.plan=0;task.intelSupport=false;task.reconnaissance=false;task.stepsRequired=2;
            State.intelligence=2;Draw();yield return null;
            yield return SmokeOpenBusiness(0);yield return AssignmentScrollTo("CommandTasks","CommandTask_"+task.id);yield return Click("CommandTask_"+task.id);
            yield return Click("MissionPlan_1");yield return Click("MissionIntel");
            var member=CommandRules.Person(State,selectedAgent);member.neglect=2;Draw();yield return null;
            RequireTaskLayoutGeometry();
            if(!AssignmentText("AssignmentCost").text.Contains("4")||!AssignmentText("AssignmentCost").text.Contains("1")||!AssignmentText("AssignmentRisk").text.Contains("失败")||!AssignmentText("AssignmentLifeAlert").text.Contains("信任−7")||!AssignmentText("AssignmentLifeAlert").text.Contains("可疑+8"))
                throw new Exception("Compact task view hid selected cost, failure or third-night penalties");
            string planned=JsonUtility.ToJson(State);int actor=selectedAgent;
            yield return Click("MissionImpactDetails");RequireKeyboardScope("CommandShade");yield return RequireMapDeskBlocked();yield return KeyboardSubmit("MissionImpactBack");
            if(modal!="missionPlan"||selectedAgent!=actor||JsonUtility.ToJson(State)!=planned)throw new Exception("Task risk return changed a pending plan");
            Capture(Path.ChangeExtension(path,"compact-mission.png"));
            int money=State.money;State.money=0;Draw();yield return null;RequireTaskLayoutBlocked(task);
            Capture(Path.ChangeExtension(path,"compact-mission-blocked.png"));
            State.money=money;member.policy=1;Draw();yield return null;RequireTaskLayoutBlocked(task);
            member.policy=0;Draw();yield return null;
            yield return Click("MissionBack");
            if(task.actor>=0||CommandRules.ReservedMoney(State)!=0||CommandRules.ReservedIntel(State)!=0)throw new Exception("Canceling a compact task preview committed a plan");

            State.journal.Clear();
            State.journal.Add("[1] —— 第1夜 / 组织执行报告 ——");State.journal.Add("[1] 首夜安排已经执行。");
            State.journal.Add("[2] —— 第2夜 / 组织执行报告 ——");State.journal.Add("[2] 次夜安排已经执行。");
            State.ended=true;State.won=false;State.ending="界面回归用结束记录";modal="";Draw();yield return null;
            yield return Click("CommandEndReview");yield return Click("ReportAll");yield return Click("ReportPrevious");
            if(modal!="endingReview"||reportNight!=1||!reportAll)throw new Exception("Ending report fixture did not select its prior night and full detail");
            yield return CompactBriefingRoundTrip("ReportHelp",path,"compact-ending-help","夜");
            Capture(Path.ChangeExtension(path,"compact-ending-review.png"));
            yield return Click("EndingReviewBack");
            if(modal!=""||!State.ended)throw new Exception("Ending report return reopened play");

            OpenHome();yield return null;yield return Click("HomeTutorial");yield return Click("TutorialBegin");yield return TutorialSmokePrompts();
            if(!InTutorial||State.tutorial.stage!=0)throw new Exception("Compact help tutorial did not begin with strong guidance");
            yield return Click("CommandTab_2");yield return Click("CommandPeopleDetails");
            if(!root.GetComponentsInChildren<Transform>().Any(t=>t.name=="TutorialCoach"))throw new Exception("Strong guide missing before reading member help");
            yield return CompactBriefingRoundTrip("MemberGrowthHelp",path,"compact-tutorial-help","4","6","身份");
            if(State.tutorial.stage!=0||!root.GetComponentsInChildren<Transform>().Any(t=>t.name=="TutorialCoach")||ProducerButton("LifePolicy_1").IsInteractable())
                throw new Exception("Returning from member help lost or bypassed strong tutorial guidance");
            Capture(Path.ChangeExtension(path,"compact-tutorial-return.png"));
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp",TutorialSavePath,TutorialSavePath+".bak",TutorialSavePath+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_COMPACT_UI_SUCCESS: "+Screen.width+"x"+Screen.height+"; five icon metrics, contextual help, exact return/focus, read-only state/save, Move/Submit and mouse isolation, shortcut guard, long-text scroll/Escape, costs/penalties/blockers, unchanged task choices/cancel, ending report context, strong tutorial help/return");
        }
    }
}
