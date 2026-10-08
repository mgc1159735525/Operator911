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
        void RequireWorkOverviewLayout()
        {
            Canvas.ForceUpdateCanvases();
            var panel=TaskLayoutRect("CommandModal");
            foreach(var label in panel.GetComponentsInChildren<Text>())
                if(!string.IsNullOrWhiteSpace(label.text)&&label.preferredHeight>label.rectTransform.rect.height+3)
                    throw new Exception("Work overview text clipped: "+label.text);
            var execute=ProducerButton("CommandExecute");
            if(execute==null||execute.GetComponentInParent<ScrollRect>()!=null||TaskLayoutBounds((RectTransform)execute.transform,panel).yMin<panel.rect.yMin)
                throw new Exception("Night execution action moved outside its fixed footer");
        }

        IEnumerator WorkOverviewSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Work overview test requires isolated save");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            string opening=JsonUtility.ToJson(State);
            State.turn=7;State.command.config.eventChance=0;State.command.notices.Clear();
            State.command.campaignEnabled=false;State.command.tasks.Clear();
            foreach(var person in State.command.people){person.policy=0;person.leaveUntil=0;person.neglect=0;}
            var route=State.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;route.waiting=1;route.atRelay=1;
            CommandRules.Person(State,0).institution=1;
            State.agents[4].exposure=State.command.strategy.config.warning;
            var continued=new CommandTask{id=State.command.nextTask++,title="核对第二份名册",body="阶段已推进，今夜需要继续授权。",origin="上级指示",deadline=12,stepsRequired=2,stepsDone=1,lastResult="第一阶段完成",skill=0};
            State.command.tasks.Add(continued);
            var signed=new CommandTask{id=State.command.nextTask++,title="本夜已签发的核证",body="用于核对人员与接头岗位冲突。",origin="成员发现",deadline=12,skill=0,actor=1};
            State.command.tasks.Add(signed);
            Draw();yield return null;
            string before=JsonUtility.ToJson(State);
            yield return Click("CommandWorkOverview");
            if(modal!="commandConfirm"||workOverviewPage!=2)throw new Exception("Existing work conflicts were not surfaced on opening review");
            RequireWorkOverviewLayout();yield return RequireMapDeskBlocked();
            if(!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(State.agents[1].name)&&t.text.Contains("接头")))throw new Exception("Review omitted the member's live handoff conflict");
            Capture(Path.ChangeExtension(path,"work-conflict.png"));
            int routeRow=CommandRules.BuildWorkOverview(State).blocked.FindIndex(x=>x.routeId==route.id&&x.taskId<0);
            yield return AssignmentScrollTo("WorkOverviewList_2","WorkOverviewOpen_"+routeRow);yield return Click("WorkOverviewOpen_"+routeRow);
            if(commandTab!=1||modal!="")throw new Exception("Conflict action did not open the route editor");
            yield return Click("CommandPageClose");
            if(modal!="commandConfirm"||workOverviewPage!=2)throw new Exception("Route return lost the review's risk group");
            int memberRow=CommandRules.BuildWorkOverview(State).blocked.FindIndex(x=>x.actorId==State.agents[4].id&&x.routeId<0&&x.taskId<0);
            yield return AssignmentScrollTo("WorkOverviewList_2","WorkOverviewOpen_"+memberRow);yield return Click("WorkOverviewOpen_"+memberRow);
            if(modal!="memberDetails")throw new Exception("Risk action did not open the affected member");
            if(!HandleCommandModalEscape()||modal!="commandConfirm")throw new Exception("Member Escape lost its originating review");
            yield return null;
            yield return Click("WorkOverviewTab_0");RequireWorkOverviewLayout();
            if(!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(signed.title)))throw new Exception("Signed work vanished from tonight's overview");
            yield return Click("WorkOverviewTab_1");RequireWorkOverviewLayout();
            if(!root.GetComponentsInChildren<Text>().Any(t=>t.text.Replace(" ","").Contains("1/2")))throw new Exception("Continued work lacks stage progress");
            Capture(Path.ChangeExtension(path,"work-continuation.png"));
            if(before!=JsonUtility.ToJson(State))throw new Exception("Reading review consumed resources, advanced time, RNG, or changed assignments");
            yield return Click("WorkOverviewOpen_0");
            if(modal!="missionPlan"||commandTask!=continued.id)throw new Exception("Continuation shortcut opened another task");
            yield return Click("MissionBack");
            if(modal!="commandConfirm"||workOverviewPage!=1)throw new Exception("Task return lost the work review and selected group");
            yield return Click("WorkOverviewOpen_0");
            yield return Click("CommandAuthorize");
            if(modal!="commandConfirm"||continued.actor<0||CommandRules.BuildWorkOverview(State).continuation.Any(x=>x.taskId==continued.id))throw new Exception("Authorization did not clear the continuation item");
            yield return Click("WorkOverviewTab_0");
            int row=CommandRules.BuildWorkOverview(State).assigned.FindIndex(x=>x.taskId==continued.id);
            if(row<0)throw new Exception("Newly signed continuation missing from tonight's work");
            yield return AssignmentScrollTo("WorkOverviewList_0","WorkOverviewOpen_"+row);
            yield return Click("WorkOverviewOpen_"+row);yield return Click("CommandCancel_"+continued.id);yield return Click("MissionBack");
            if(modal!="commandConfirm"||continued.actor>=0||!CommandRules.BuildWorkOverview(State).continuation.Any(x=>x.taskId==continued.id))throw new Exception("Revocation failed to restore the continuation item");
            yield return Click("CommandBack");
            if(commandTab!=3||modal!="")throw new Exception("Work review did not return to the map");
            if(State.turn!=7)throw new Exception("Planning advanced the night before explicit confirmation");
            yield return Click("CommandWorkOverview");yield return Click("WorkOverviewTab_2");
            routeRow=CommandRules.BuildWorkOverview(State).blocked.FindIndex(x=>x.routeId==route.id&&x.taskId<0);
            yield return AssignmentScrollTo("WorkOverviewList_2","WorkOverviewOpen_"+routeRow);yield return Click("WorkOverviewOpen_"+routeRow);
            if(!Save(false))throw new Exception("Review fixture could not save");
            Load();yield return null;
            if(returnToWorkOverview||commandTab!=3||modal!="")throw new Exception("Loading retained an old review return destination");
            yield return Click("CommandTab_2");yield return Click("CommandPageClose");
            if(modal!=""||commandTab!=3)throw new Exception("Business page close after loading returned to an unrelated review");

            // Observe a real first-night failure selected from reproducible seeds;
            // the preview copies run the same rules, without altering the player's RNG.
            State=JsonUtility.FromJson<RunState>(opening);State.command.config.eventChance=0;
            var first=State.command.tasks.First(t=>t.operation==0&&t.stepsRequired==1&&CommandRules.CanStaffTask(State,t));
            var actor=State.agents.First(a=>CommandRules.TaskError(State,a.id,first)=="");
            if(CommandRules.Assign(State,actor.id,first.id)!="")throw new Exception("Opening task assignment failed");
            bool found=false;
            for(uint seed=1;seed<=1000;seed++)
            {
                var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));copy.random=seed;
                CommandRules.Resolve(copy);
                var failed=copy.command.tasks.First(t=>t.id==first.id);
                if(failed.status==0&&failed.lastResult.Contains("；未成")){State.random=seed;found=true;break;}
            }
            if(!found)throw new Exception("Could not locate a deterministic failed opening roll");
            commandTab=3;modal="";Draw();yield return null;
            yield return Click("CommandEnd");yield return Click("CommandExecute");
            if(State.turn!=2||State.command.notices.Count==0)throw new Exception("First-night failed order still returns to map without its result");
            if(!OpeningChecklist().StartsWith("✓"))throw new Exception("Failed first task reset the completed signing lesson");
            var report=root.GetComponentsInChildren<Text>();
            if(!report.Any(t=>t.text.Contains(first.title))||!report.Any(t=>t.text.Contains("失败")))throw new Exception("Opening failure bulletin lacks task name or result");
            Capture(Path.ChangeExtension(path,"opening-failure.png"));
            yield return Click("CommandNoticeOpen");
            if(modal!="missionPlan"||commandTask!=first.id)throw new Exception("First-night failure did not link to the task for another decision");
            yield return Click("MissionBack");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_WORK_OVERVIEW_UI_SUCCESS: continued stages, conflict navigation, readonly review, authorize/revoke/return, fixed footer and first-night failure feedback");
        }
    }
}
