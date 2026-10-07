using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        string smokeNavigationCapture;
        Exception MapDeskFailure(string message)
        {
            string capture=Path.ChangeExtension(smokeNavigationCapture??smokeSave,"navigation-failure.png");
            try{Capture(capture);}catch(Exception error){Debug.LogWarning("Smoke failure screenshot: "+error.Message);}
            return new Exception(message+"; atTitle="+atTitle+"; modal="+modal+"; tab="+commandTab+"; toast="+toast+"; homeNotice="+homeNotice+"; save="+SavePath+"; screenshot="+capture);
        }
        IEnumerator SmokeObservedMenuClick(string name)
        {
            var buttons=root.GetComponentsInChildren<Button>().Where(b=>b.name==name&&b.gameObject.activeInHierarchy).ToList();
            if(buttons.Count!=1)throw MapDeskFailure("Expected exactly one visible menu control "+name+", found "+buttons.Count);
            int calls=0;var button=buttons[0];
            button.onClick.AddListener(()=>{calls++;Debug.Log("MINGLU_MENU_CALLBACK: "+name+" / toast="+toast+" / homeNotice="+homeNotice+" / atTitle="+atTitle+" / modal="+modal);});
            yield return Click(name);
            yield return null;yield return new WaitForEndOfFrame();
            if(calls!=1)throw MapDeskFailure("Real pointer click did not invoke "+name+" exactly once: "+calls);
            if(name=="Save"||name=="CommandHome")
            {
                if(toast.StartsWith("保存失败")||toast.StartsWith("存档失败"))throw MapDeskFailure("Menu persistence reported failure after "+name);
                if(!File.Exists(SavePath)||File.ReadAllText(SavePath)!=JsonUtility.ToJson(State,true))throw MapDeskFailure("Menu persistence did not write the exact current state after "+name);
            }
            if(name=="Load"&&(toast.StartsWith("读取失败")||modal!=""||atTitle||commandTab!=3))throw MapDeskFailure("Menu load failed");
        }
        // Navigation helpers use the same visible return controls as a player.
        // They never assign modal/commandTab or invoke hidden business actions.
        IEnumerator SmokeReturnToMap()
        {
            for(int attempts=0;attempts<12;attempts++)
            {
                if(atTitle&&settingsFromGame){yield return Click("HomeBack");continue;}
                string back=modal=="campaignDossier"?"CampaignClose":modal=="noticeArchive"?"NoticeArchiveClose":modal=="missionPlan"?"MissionBack":modal=="memberDetails"?"MemberDetailsClose":modal=="commandOverview"?"OverviewClose":modal=="routePicker"?"RoutePickerCancel":modal=="routeDetails"?"RouteDetailsClose":modal=="strategyDetails"?"StrategyDetailsClose":modal=="districtDetails"?"DistrictClose":modal=="institutionPicker"?"InstitutionCancel":modal=="compareCandidates"?"CompareBack":modal=="commandHelp"?"CommandGuideBack":modal=="commandSettings"?"CommandSettingsBack":modal=="menu"?"CommandResume":modal=="routeReplace"?"RouteReplaceCancel":modal=="commandConfirm"?"CommandBack":"";
                if(back!=""){yield return Click(back);continue;}
                if(modal!="")throw new Exception("Smoke cannot dismiss this screen through a supported return control: "+modal);
                if(commandTab!=3){yield return Click("CommandPageClose");continue;}
                RequireMapDesk();yield break;
            }
            throw new Exception("Returning from business screens did not reach the map");
        }
        IEnumerator SmokeOpenBusiness(int page)
        {
            yield return SmokeReturnToMap();
            if(page!=3)yield return Click("CommandTab_"+page);
            if(commandTab!=page||modal!="")throw new Exception("Business button opened the wrong screen: "+page);
            RequireMapDesk();
        }
        IEnumerator SmokeSaveLoad()
        {
            yield return SmokeReturnToMap();yield return Click("CommandMenu");yield return SmokeObservedMenuClick("Save");yield return SmokeObservedMenuClick("Load");
            if(atTitle||modal!=""||commandTab!=3)throw MapDeskFailure("Reading through settings did not return to the map");
            RequireMapDesk();
        }
        void RequireMapDesk()
        {
            if(root.GetComponentsInChildren<RawImage>().Count(x=>x.name=="CommandMap"&&x.gameObject.activeInHierarchy)!=1)throw new Exception("Command map is missing or duplicated below the current screen");
        }
        IEnumerator RequireMapDeskBlocked()
        {
            yield return null;yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();RequireMapDesk();
            var district=root.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("CommandDistrict_"));
            var rect=(RectTransform)district.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(viewCamera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()==district)throw new Exception("Business modal allows clicks through to the district map");
        }
        IEnumerator MapDeskSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Map desk smoke requires an isolated save");
            smokeNavigationCapture=path;
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");State.command.config.eventChance=0;
            if(commandTab!=3||modal!="")throw new Exception("New run did not start on the city map");
            RequireMapDesk();
            foreach(string name in new[]{"Save","Load","CommandHome","CommandGuide","CommandSettings","CommandAuthorize","LifePolicy_1"})if(ProducerButton(name)!=null)throw new Exception("Map desk still exposes secondary control: "+name);
            int turn=State.turn,money=State.money,intel=State.intelligence;Capture(Path.ChangeExtension(path,"map-desk.png"));
            yield return DeskGuidanceSmoke(path);
            foreach(int page in new[]{0,1,2,4,5})
            {
                yield return SmokeOpenBusiness(page);yield return RequireMapDeskBlocked();
                Capture(Path.ChangeExtension(path,"business-"+page+".png"));yield return Click("CommandPageClose");
                if(commandTab!=3||modal!="")throw new Exception("Business window did not close to the map: "+page);
            }
            yield return SmokeOpenBusiness(0);
            var task=State.command.tasks.First(t=>t.status==0&&State.agents.Any(a=>CommandRules.TaskError(State,a.id,t)==""));
            yield return Click("CommandRecommend_"+task.id);yield return RequireMapDeskBlocked();
            if(modal!="missionPlan")throw new Exception("Task dossier did not open");
            yield return Click("CommandCompare_"+task.id);yield return RequireMapDeskBlocked();yield return Click("CompareBack");
            if(modal!="missionPlan")throw new Exception("Member comparison lost its task");
            yield return Click("MissionPlan_1");yield return Click("CommandAuthorize");
            if(task.actor<0||commandTab!=0||modal!=""||CommandRules.ReservedMoney(State)!=4)throw new Exception("Task authorization lost the task window or budget");
            yield return Click("CommandTask_"+task.id);yield return Click("CommandCancel_"+task.id);yield return Click("MissionBack");
            if(task.actor>=0||CommandRules.ReservedMoney(State)!=0||commandTab!=0||modal!="")throw new Exception("Task revocation did not release budget or return to the task window");
            yield return SmokeReturnToMap();yield return Click("CommandDistrict_"+task.district);yield return RequireMapDeskBlocked();
            yield return Click("DistrictTask_"+task.id);yield return Click("MissionBack");
            if(commandTab!=3||modal!="districtDetails"||selectedDistrict!=task.district)throw new Exception("District task did not preserve its district dossier and selection");
            yield return Click("DistrictTask_"+task.id);yield return Click("CommandAuthorize");
            if(task.actor<0||commandTab!=3||modal!="districtDetails"||selectedDistrict!=task.district)throw new Exception("District task sign-off did not return to its source dossier");
            yield return Click("DistrictTask_"+task.id);yield return Click("CommandCancel_"+task.id);yield return Click("MissionBack");
            if(task.actor>=0||CommandRules.ReservedMoney(State)!=0||commandTab!=3||modal!="districtDetails")throw new Exception("District task withdrawal lost its return context or retained budget");
            yield return SmokeOpenBusiness(2);yield return Click("CommandAgent_3");yield return Click("CommandPeopleDetails");
            int policy=CommandRules.Person(State,3).policy;yield return Click("LifePolicy_1");yield return Click("LifePolicy_"+policy);yield return Click("MemberDetailsClose");
            if(commandTab!=2||modal!=""||selectedAgent!=3)throw new Exception("Person dossier lost its roster selection");
            yield return SmokeReturnToMap();yield return Click("CommandMenu");yield return RequireMapDeskBlocked();Capture(Path.ChangeExtension(path,"desk-settings.png"));
            foreach(string name in new[]{"Save","Load","CommandHome","CommandGuide","CommandSettings","CommandMenuQuit","CommandResume"})if(ProducerButton(name)==null)throw new Exception("Settings menu omits "+name);
            yield return Click("CommandGuide");yield return RequireMapDeskBlocked();Capture(Path.ChangeExtension(path,"desk-guide.png"));yield return Click("CommandGuideBack");
            if(modal!="menu")throw new Exception("Guide did not return to settings");
            yield return Click("CommandSettings");if(atTitle||modal!="commandSettings")throw new Exception("Audio/display settings did not open above the map");
            yield return RequireMapDeskBlocked();Capture(Path.ChangeExtension(path,"desk-audio-display.png"));
            float volume=profile.volume;yield return Click("VolumeDown");if(Math.Abs(profile.volume-Mathf.Max(0,volume-.1f))>.001f)throw new Exception("In-game volume control did not change the preference");
            yield return Click("CommandSettingsBack");if(atTitle||modal!="menu")throw new Exception("Audio/display settings lost the game menu");
            yield return SmokeObservedMenuClick("Save");yield return SmokeObservedMenuClick("Load");if(commandTab!=3||modal!="")throw MapDeskFailure("Menu load did not return to the map");
            RequireMapDesk();yield return Click("CommandMenu");yield return SmokeObservedMenuClick("CommandHome");if(!atTitle)throw MapDeskFailure("Menu return to title failed");
            yield return Click("HomeContinue");if(atTitle||commandTab!=3||modal!="")throw MapDeskFailure("Continue did not return to the saved map");
            RequireMapDesk();if(State.turn!=turn||State.money!=money||State.intelligence!=intel||State.command.tasks.Any(t=>t.actor>=0))throw new Exception("Navigation or revocation changed time, resources or assignments");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_MAP_DESK_UI_SUCCESS: persistent map, five business dialogs, raycast isolation, task authorize/revoke, detail return context, settings/save/load/guide/title/continue, unchanged time/resources");
        }
        IEnumerator DeskGuidanceSmoke(string path)
        {
            string original=JsonUtility.ToJson(State);State.command.campaignEnabled=false;
            string saved=JsonUtility.ToJson(State);int originalAgent=selectedAgent,originalTask=commandTask;
            try
            {
                var opening=State.command.tasks.First(t=>t.operation==0&&CommandRules.CanStaffTask(State,t));
                if(NextCommandStep().task<0||!NextCommandStep().label.Contains("第一项"))throw MapDeskFailure("Opening guidance did not offer an executable first task");
                opening.actor=State.agents.First(a=>CommandRules.TaskError(State,a.id,opening)=="").id;
                if(!NextCommandStep().ready||!CommandNext().Contains("第二夜"))throw MapDeskFailure("First authorization did not guide the player to their first result");
                State=JsonUtility.FromJson<RunState>(saved);State.turn=2;
                var optionalRoute=State.command.routes[0];optionalRoute.source=0;optionalRoute.liaison=1;optionalRoute.courier=2;optionalRoute.enabled=true;
                foreach(var task in State.command.tasks){task.deadline=8;task.level=1;task.required=1;task.profession="";}
                State.command.tasks[0].actor=3;Draw();yield return null;yield return new WaitForEndOfFrame();
                if(!NextCommandStep().ready||!CommandNext().Contains("可选待办")||ProducerButton("CommandEnd").colors.normalColor!=Accent)throw MapDeskFailure("Optional work still forces every member to take a task");
                Capture(Path.ChangeExtension(path,"optional-orders.png"));
                State=JsonUtility.FromJson<RunState>(saved);State.turn=2;
                // A two-stage task on its final possible start night must beat routine setup.
                foreach(var t in State.command.tasks){t.deadline=8;t.stepsRequired=1;}
                var deadline=State.command.tasks.First(t=>CommandRules.CanStaffTask(State,t));deadline.deadline=State.turn+1;deadline.stepsRequired=2;
                if(!CommandRules.TaskUrgent(State,deadline)||!CommandRules.TaskTimingText(State,deadline).Contains("今夜必须"))throw MapDeskFailure("Shared timing did not account for remaining stages");
                Draw();yield return null;yield return new WaitForEndOfFrame();
                if(!ProducerButton("CommandNextAction").GetComponentInChildren<Text>().text.Contains("限期"))throw MapDeskFailure("Deadline guidance did not identify the last viable starting night");
                if(ProducerButton("CommandNextAction").colors.normalColor!=Accent||ProducerButton("CommandEnd").colors.normalColor==Accent)throw MapDeskFailure("Deadline action is not the single primary map control");
                Capture(Path.ChangeExtension(path,"deadline-guidance.png"));yield return Click("CommandNextAction");
                if(modal!="missionPlan"||commandTask!=deadline.id)throw MapDeskFailure("Visible deadline advice opened another destination");
                yield return SmokeReturnToMap();

                deadline.operation=2;Draw();yield return Click("CommandNextAction");
                if(modal!="missionPlan"||commandTask!=deadline.id)throw MapDeskFailure("Rescue advice did not open its task");
                yield return SmokeReturnToMap();deadline.operation=0;deadline.deadline=8;
                var watched=State.agents[2];watched.exposure=State.command.strategy.config.warning;
                Draw();yield return Click("CommandNextAction");
                if(modal!="memberDetails"||selectedAgent!=watched.id)throw MapDeskFailure("Exposure advice did not open the affected person's policy controls");
                yield return SmokeReturnToMap();

                State=JsonUtility.FromJson<RunState>(saved);State.turn=2;
                foreach(var person in State.command.people)person.institution=0;
                State.command.tasks.Clear();
                State.command.tasks.Add(new CommandTask{id=50,operation=4,target=0,actor=0,title="已安排的机构任职",deadline=5});
                Draw();yield return null;yield return new WaitForEndOfFrame();
                if(!NextCommandStep().ready||!CommandNext().Contains("无需重复"))throw MapDeskFailure("Already authorized employment trapped the next-action guidance");
                if(ProducerButton("CommandEnd").colors.normalColor!=Accent||ProducerButton("CommandNextAction").colors.normalColor==Accent)throw MapDeskFailure("Ready-to-execute map did not move emphasis to ending the night");
                Capture(Path.ChangeExtension(path,"appointment-guidance.png"));yield return Click("CommandNextAction");
                if(modal!="commandConfirm")throw MapDeskFailure("Authorized employment did not lead to review before execution");
                yield return SmokeReturnToMap();

                State=JsonUtility.FromJson<RunState>(saved);State.turn=2;var route=State.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;
                CommandRules.Person(State,0).institution=1;
                foreach(var t in State.command.tasks){t.stepsRequired=2;t.deadline=State.turn;}
                Draw();yield return Click("CommandNextAction");
                if(modal!="commandConfirm")throw MapDeskFailure("Impossible deadlines caused an endless assignment recommendation");
                yield return SmokeReturnToMap();
                var investigation=State.command.tasks[0];investigation.deadline=State.turn+2;investigation.reconnaissance=true;investigation.actor=0;
                if(CommandRules.TaskAttemptsNeeded(investigation)!=3||!TaskOrderText(investigation).Contains("已安排调查"))throw MapDeskFailure("Investigation timing/order state lost its extra night");
                string before=JsonUtility.ToJson(State);CommandRules.CanStaffTask(State,investigation);
                if(before!=JsonUtility.ToJson(State))throw MapDeskFailure("Staff availability query mutated state");
                foreach(var task in State.command.tasks){task.actor=-1;task.reconnaissance=false;task.deadline=8;}
                foreach(var person in State.command.people)person.policy=1;
                if(State.command.tasks.Any(t=>CommandRules.CanStaffTask(State,t))||!NextCommandStep().ready)throw MapDeskFailure("Unavailable crew caused an endless task or chain recommendation");
                Draw();yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"unavailable-guidance.png"));
                foreach(var person in State.command.people)person.institution=0;
                foreach(var agent in State.agents)agent.condition=Condition.Lost;
                State.command.tasks.Clear();Draw();yield return Click("CommandNextAction");
                if(modal!="commandConfirm")throw MapDeskFailure("No active members crashed the next-action guidance");
                if(State.turn!=2||State.money!=JsonUtility.FromJson<RunState>(saved).money)throw MapDeskFailure("Guidance navigation advanced time or spent resources");
                State.command.notices.Add(new CommandNotice{title="普通报告",body="第一条消息",tab=4});
                State.command.notices.Add(new CommandNotice{title="成员生活",body="第二条消息",tab=2});Draw();
                yield return Click("CommandNoticeDismiss");
                if(State.command.notices.Count!=0||commandTab!=3||modal!="")throw MapDeskFailure("Reading the bulletin did not dismiss all notices directly to the map");
                State=JsonUtility.FromJson<RunState>(saved);commandTab=3;modal="";
                var rescueNoticeTask=State.command.tasks[0];rescueNoticeTask.operation=2;rescueNoticeTask.target=1;
                State.command.notices.Add(new CommandNotice{title="日常报告",body="普通情况",tab=4});
                State.command.notices.Add(new CommandNotice{title="救援急报",body="成员需要营救",tab=0,task=rescueNoticeTask.id,actor=1});Draw();
                yield return Click("CommandNoticeOpen");
                if(State.command.notices.Count!=0||modal!="missionPlan"||commandTask!=rescueNoticeTask.id)throw MapDeskFailure("Grouped bulletin lost the urgent rescue destination");
                Debug.Log("MINGLU_GUIDANCE_UI_SUCCESS: first task/result pacing, shared stage/investigation timing, rescue, policy, appointment, unavailable crew, grouped notices, navigation only");
            }
            finally{State=JsonUtility.FromJson<RunState>(original);selectedAgent=originalAgent;commandTask=originalTask;commandTab=3;modal="";Draw();}
        }
        IEnumerator ReconDeskSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Recon smoke requires isolated saves");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");State.command.config.eventChance=0;
            var t=State.command.tasks[0];t.deadline=8;selectedAgent=0;State.agents[0].intel=6;State.random=1;
            yield return SmokeOpenBusiness(0);yield return Click("CommandTask_"+t.id);yield return Click("MissionModeRecon");
            if(!t.reconnaissance)throw new Exception("Recon mode button did not switch action");
            yield return RequireMapDeskBlocked();Capture(Path.ChangeExtension(path,"recon-plan.png"));
            yield return Click("CommandCompare_"+t.id);Capture(Path.ChangeExtension(path,"recon-candidates.png"));
            yield return Click("CompareSelect_0");yield return Click("CommandAuthorize");
            if(t.actor!=0||!t.reconnaissance)throw new Exception("Recon was not authorized");
            yield return SmokeSaveLoad();t=State.command.tasks.First(x=>x.id==t.id);
            if(!t.reconnaissance||t.actor!=0)throw new Exception("Saved reconnaissance order lost");
            yield return Click("CommandEnd");yield return Click("CommandExecute");
            if(commandTab!=3||modal!="")throw new Exception("Night execution did not retain the map beneath the bulletin");
            if(!t.reconComplete||t.actor!=-1||t.status!=0)throw new Exception("Investigation did not yield a delegatable target");
            Capture(Path.ChangeExtension(path,"recon-result.png"));
            while(State.command.notices.Count>0)yield return Click("CommandNoticeDismiss");
            yield return SmokeOpenBusiness(0);yield return Click("CommandTask_"+t.id);
            if(ProducerButton("MissionModeRecon").interactable)throw new Exception("Completed recon can be repeated");
            Capture(Path.ChangeExtension(path,"prepared-target.png"));
            Debug.Log("MINGLU_RECON_UI_SUCCESS: mode, comparison, authorization, save/load, nightly result, persistent target advantage, no repeat investigation");
        }
    }
}
