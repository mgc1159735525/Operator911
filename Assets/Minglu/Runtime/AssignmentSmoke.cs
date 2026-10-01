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
        Text AssignmentText(string name){return root.GetComponentsInChildren<Text>().First(t=>t.name==name);}
        void RequireAssignmentTextFits(params string[] names)
        {
            Canvas.ForceUpdateCanvases();
            foreach(string name in names)
            {
                var text=AssignmentText(name);
                if(text.preferredHeight>text.rectTransform.rect.height+2)throw new Exception("Assignment text clipped: "+name+"; needs "+text.preferredHeight+" / has "+text.rectTransform.rect.height+"; "+text.text);
            }
        }
        IEnumerator AssignmentScrollTo(string scrollName,string buttonName)
        {
            var scroll=root.GetComponentsInChildren<ScrollRect>().First(v=>v.name==scrollName);
            var choice=(RectTransform)ProducerButton(buttonName).transform;
            float overflow=scroll.content.rect.height-scroll.viewport.rect.height;
            Vector3 center=scroll.content.InverseTransformPoint(choice.TransformPoint(choice.rect.center));
            if(overflow>0)scroll.verticalNormalizedPosition=1-Mathf.Clamp01((-center.y-scroll.viewport.rect.height*.5f)/overflow);
            yield return null;
        }
        IEnumerator AssignmentSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Assignment smoke requires an isolated save");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            State.command.config.eventChance=0;State.command.notices.Clear();
            foreach(var person in State.command.people){person.policy=0;person.leaveUntil=0;person.neglect=0;person.level=2;}
            foreach(var agent in State.agents){agent.persuade=2;agent.stamina=agent.spirit=100;agent.exposure=0;}
            var route=State.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;route.waiting=1;route.atRelay=0;
            State.command.people[0].institution=1;
            State.agents[0].persuade=State.agents[1].persuade=6;State.agents[2].persuade=4;State.agents[3].persuade=5;
            var task=State.command.tasks[0];task.skill=2;task.required=1;task.level=1;task.profession="";task.deadline=State.config.Value("turns");task.plan=0;task.intelSupport=false;task.reconnaissance=false;
            foreach(var item in State.command.tasks)item.actor=-1;
            int turn=State.turn,money=State.money;Draw();yield return null;
            yield return SmokeOpenBusiness(0);yield return AssignmentScrollTo("CommandTasks","CommandRecommend_"+task.id);yield return Click("CommandRecommend_"+task.id);
            if(selectedAgent!=3)throw new Exception("Recommendation preferred a busy chain member over the capable free member");
            RequireAssignmentTextFits("AssignmentOutcome","AssignmentTiming","AssignmentChainImpact","AssignmentCost","AssignmentWorkload","AssignmentReconValue");
            Capture(Path.ChangeExtension(path,"assignment-plan.png"));
            yield return Click("CommandCompare_"+task.id);
            if(!AssignmentText("CandidateReason_2").text.Contains("今晚无待办"))throw new Exception("Idle courier was reported as delaying a packet");
            if(!AssignmentText("CandidateReason_1").text.Contains("接头等待"))throw new Exception("Busy liaison did not explain the actual delay");
            Capture(Path.ChangeExtension(path,"assignment-candidates.png"));
            yield return AssignmentScrollTo("CandidateList","CompareSelect_2");yield return Click("CompareSelect_2");
            if(!AssignmentText("AssignmentChainImpact").text.Contains("不推迟"))throw new Exception("Task plan invented a chain conflict for the idle courier");
            task.deadline=State.turn;Draw();yield return null;yield return Click("MissionModeRecon");
            if(ProducerButton("CommandAuthorize").interactable||CommandRules.TaskCanFinish(State,task))throw new Exception("Final-night investigation promised an unreachable execution");
            Capture(Path.ChangeExtension(path,"assignment-too-late.png"));
            yield return Click("MissionModeExecute");task.deadline=State.config.Value("turns");Draw();yield return null;
            yield return Click("CommandCompare_"+task.id);yield return AssignmentScrollTo("CandidateList","CompareSelect_3");yield return Click("CompareSelect_3");
            yield return Click("MissionPlan_1");yield return Click("CommandAuthorize");
            if(task.actor!=3||CommandRules.ReservedMoney(State)!=4)throw new Exception("Task sign-off did not reserve its per-stage cost");
            yield return SmokeOpenBusiness(2);yield return Click("CommandPeopleDetails");
            RequireAssignmentTextFits("MemberSkillGrowth","MemberGrowthChoice");Capture(Path.ChangeExtension(path,"assignment-member-growth.png"));
            yield return Click("LifePolicy_1");
            if(task.actor!=-1||CommandRules.ReservedMoney(State)!=0)throw new Exception("Living-first policy retained an incompatible task or its budget");
            yield return Click("MemberDetailsClose");
            route.waiting=2;route.atRelay=1;route.age=3;Draw();yield return null;
            yield return SmokeOpenBusiness(1);yield return Click("RouteDetails_0");
            RequireAssignmentTextFits("RouteStepForecast_0","RouteStepForecast_1","RouteStepForecast_2");Capture(Path.ChangeExtension(path,"assignment-route-forecast.png"));
            yield return Click("RouteDetailsClose");yield return Click("RouteSlot_0_1");yield return AssignmentScrollTo("RouteCandidates","RouteCandidate_4");yield return Click("RouteCandidate_4");
            if(!AssignmentText("RouteReplacementConsequences").text.Contains("保留"))throw new Exception("Normal handover still warns of destroying all intelligence");
            Capture(Path.ChangeExtension(path,"assignment-handover.png"));yield return Click("RouteReplaceConfirm");
            if(route.liaison!=4||route.waiting!=2||route.atRelay!=1||route.age!=3||route.enabled)throw new Exception("Normal handover lost packets or reset their age");
            if(State.turn!=turn||State.money!=money)throw new Exception("Assignment planning, policy and handover advanced time or spent money");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_ASSIGNMENT_UI_SUCCESS: role-aware comparison, idle courier, deadline/recon feasibility, per-stage budget, living-first withdrawal, readable growth, nightly chain forecast and preserving handover");
        }
    }
}
