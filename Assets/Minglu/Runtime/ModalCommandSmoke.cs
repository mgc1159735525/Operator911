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
        IEnumerator ModalCommandSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Modal smoke requires an isolated save");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");State.command.config.eventChance=0;
            int turn=State.turn,money=State.money,intel=State.intelligence;
            yield return SmokeOpenBusiness(0);Capture(Path.ChangeExtension(path,"task-list.png"));
            if(ProducerButton("CommandAuthorize")!=null)throw new Exception("Task authorization still clutters the permanent screen");
            var task=State.command.tasks.First(t=>t.status==0&&State.agents.Any(a=>CommandRules.TaskError(State,a.id,t)==""));
            yield return Click("CommandRecommend_"+task.id);
            if(modal!="missionPlan"||CommandRules.TaskError(State,selectedAgent,task)!="")throw new Exception("Recommendation did not open a valid task dossier");
            if(root.GetComponentsInChildren<Button>().Count(b=>b.name=="CommandAuthorize")!=1)throw new Exception("Duplicate authorize controls");
            Capture(Path.ChangeExtension(path,"task-dossier.png"));
            string originalBody=task.body;
            // The full reading dialog fits twelve lines; use a genuinely overflowing document.
            task.body=string.Join("\n",Enumerable.Repeat("较长的任务背景应完整保留在可滚动正文内，不能覆盖下方人员、方案、费用与签发按钮。",24));
            Draw();yield return null;
            yield return Click("MissionBackgroundDetails");
            if(modal!="missionBackground")throw new Exception("Long task body did not open its separate reading dialog");
            var longBody=root.GetComponentsInChildren<ScrollRect>().First(s=>s.name=="TaskDescription_"+task.id);
            var bodyText=longBody.content.GetComponentInChildren<Text>();
            if(bodyText.preferredHeight>bodyText.rectTransform.rect.height||longBody.content.rect.height<=longBody.viewport.rect.height)throw new Exception("Long task body needs "+bodyText.preferredHeight+" / has "+bodyText.rectTransform.rect.height+"; content "+longBody.content.rect.height+" / viewport "+longBody.viewport.rect.height);
            longBody.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Capture(Path.ChangeExtension(path,"long-task.png"));
            yield return Click("MissionBackgroundBack");
            if(modal!="missionPlan")throw new Exception("Reading dialog did not return to the current task plan");
            task.body=originalBody;Draw();yield return null;
            yield return Click("CommandCompare_"+task.id);Capture(Path.ChangeExtension(path,"candidates.png"));yield return Click("CompareBack");
            if(modal!="missionPlan")throw new Exception("Candidate cancellation lost the task context");
            yield return Click("MissionPlan_1");
            if(task.actor>=0||CommandRules.ReservedMoney(State)!=0)throw new Exception("Plan preview reserved resources or authorized work");
            yield return Click("CommandAuthorize");
            if(task.actor<0||modal!=""||CommandRules.ReservedMoney(State)!=4)throw new Exception("Dossier authorization failed");
            yield return Click("CommandTask_"+task.id);yield return Click("CommandCancel_"+task.id);
            if(task.actor>=0||CommandRules.ReservedMoney(State)!=0)throw new Exception("Revocation failed to release reservation");
            yield return Click("MissionBack");
            yield return SmokeReturnToMap();yield return Click("CommandDistrict_"+task.district);
            if(modal!="districtDetails"||commandTab!=3)throw new Exception("District pin did not open an overlay on the map");
            Capture(Path.ChangeExtension(path,"district.png"));
            yield return Click("DistrictTask_"+task.id);yield return Click("MissionBack");
            if(commandTab!=3||modal!="districtDetails")throw new Exception("Task close did not return to its source district dossier");
            yield return Click("DistrictTask_"+task.id);
            if(!HandleCommandModalEscape()||commandTab!=3||modal!="districtDetails")throw new Exception("Task Escape did not preserve the source district dossier");
            yield return Click("DistrictClose");
            yield return SmokeOpenBusiness(2);Capture(Path.ChangeExtension(path,"people.png"));
            if(ProducerButton("LifePolicy_1")!=null)throw new Exception("Life settings still occupy the permanent person page");
            yield return Click("CommandPeopleDetails");Capture(Path.ChangeExtension(path,"person-dossier.png"));
            if(root.GetComponentsInChildren<Button>().Count(b=>b.name=="CommandEditLook")!=1)throw new Exception("Duplicate appearance controls");
            int policy=CommandRules.Person(State,selectedAgent).policy;
            yield return Click("LifePolicy_1");yield return Click("LifePolicy_"+policy);yield return Click("MemberDetailsClose");
            State.command.people[3].institution=0;yield return Click("CommandAgent_3");
            yield return Click("CommandPeopleDetails");yield return Click("CommandEmployment");
            int beforeEmployment=State.command.tasks.Count;
            if(!HandleCommandModalEscape()||modal!="memberDetails"||State.command.tasks.Count!=beforeEmployment)throw new Exception("Employer Escape lost the dossier or created a task");
            yield return Click("CommandEmployment");yield return Click("InstitutionSelect_1");
            if(modal!="missionPlan"||selectedAgent!=3)throw new Exception("Employer did not focus the applicant task");
            yield return Click("MissionBack");
            if(modal!="memberDetails"||selectedAgent!=3)throw new Exception("Employment task back lost the applicant dossier");
            yield return Click("CommandEmployment");
            if(!HandleCommandModalEscape()||modal!="memberDetails"||selectedAgent!=3)throw new Exception("Employment task Escape lost the applicant dossier");
            yield return Click("MemberDetailsClose");
            yield return SmokeOpenBusiness(4);Capture(Path.ChangeExtension(path,"report.png"));
            CommandRules.Notice(State,new CommandNotice{title="查看任务检查",body="打开任务后仍有另一条通知。",tab=0,task=task.id});
            CommandRules.Notice(State,new CommandNotice{title="后续通知检查",body="确认此条不能丢弃已经选择的任务。",tab=4});Draw();yield return null;
            yield return Click("CommandNoticeOpen");
            if(modal!="missionPlan"||commandTask!=task.id||State.command.notices.Count!=0)throw new Exception("Grouped notices did not open the requested task and consume the group");
            yield return Click("MissionBack");
            if(commandTab!=3||modal!="")throw new Exception("Task opened from the nightly bulletin did not return to the command map");
            yield return SmokeOpenBusiness(1);yield return Click("RouteDetails_0");Capture(Path.ChangeExtension(path,"chain-details.png"));yield return Click("RouteDetailsClose");
            yield return SmokeOpenBusiness(5);yield return Click("StrategyDetails");Capture(Path.ChangeExtension(path,"safety-rules.png"));yield return Click("StrategyDetailsClose");
            if(State.turn!=turn||State.money!=money||State.intelligence!=intel||State.command.tasks.Any(t=>t.actor>=0))throw new Exception("Browsing or revocation changed resources/time/assignments");
            yield return SmokeReturnToMap();yield return Click("CommandEnd");yield return Click("CommandBack");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_MODAL_COMMAND_UI_SUCCESS: single dossier/compare/authorize/revoke, long body scroll, map/notice return context, employer cancel/Escape/dossier, person policies, unchanged resources/time");
        }
    }
}
