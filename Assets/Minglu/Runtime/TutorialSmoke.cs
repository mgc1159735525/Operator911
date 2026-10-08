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
        [Serializable] sealed class TutorialSmokeProfile
        {
            public List<RunRecord> records;
            public Appearance chiefLook;
            public Appearance[] lookSlots;
        }

        int tutorialSmokeEventChoices;
        Button TutorialSmokeButton(string name)
        {return root.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);}
        bool TutorialSmokeCanClick(string name)
        {var button=TutorialSmokeButton(name);return button!=null&&button.IsInteractable();}
        static string TutorialSmokeFile(string file)
        {return File.Exists(file)?Convert.ToBase64String(File.ReadAllBytes(file)):null;}
        string TutorialSmokeProfileState(PlayerProfile source=null)
        {var value=source??profile;return JsonUtility.ToJson(new TutorialSmokeProfile{records=value.records,chiefLook=value.chiefLook,lookSlots=value.lookSlots});}

        void TutorialSmokeLayout()
        {
            Canvas.ForceUpdateCanvases();
            Transform scope=null;
            foreach(Transform child in root)
                if(child.name=="TutorialShade"||child.name=="CommandShade"||child.name=="PersistenceFailureShade")scope=child;
            if(scope==null)scope=root.Find("HomePanel")??root;
            foreach(var label in scope.GetComponentsInChildren<Text>())
            {
                if(!label.gameObject.activeInHierarchy||string.IsNullOrWhiteSpace(label.text))continue;
                if(label.verticalOverflow==VerticalWrapMode.Truncate&&label.preferredHeight>label.rectTransform.rect.height+4)
                    throw new Exception("Tutorial text clipped: "+label.text+" needs "+label.preferredHeight+" / "+label.rectTransform.rect.height);
            }
            var rootRect=(RectTransform)root;
            foreach(var button in scope.GetComponentsInChildren<Button>())
            {
                if(!button.IsInteractable()||button.GetComponentInParent<ScrollRect>()!=null)continue;
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect,button.transform);
                if(bounds.min.x<rootRect.rect.xMin-2||bounds.max.x>rootRect.rect.xMax+2||bounds.min.y<rootRect.rect.yMin-2||bounds.max.y>rootRect.rect.yMax+2)
                    throw new Exception("Tutorial button outside the viewport: "+button.name);
            }
        }

        void TutorialSmokeNavigation()
        {
            var original=EventSystem.current.currentSelectedGameObject;
            var controls=root.GetComponentsInChildren<Selectable>().Where(s=>s.IsInteractable()).ToArray();
            foreach(var control in controls)foreach(var direction in KeyboardDirections)
            {
                EventSystem.current.SetSelectedGameObject(control.gameObject);KeyboardMove(direction);
                var selected=EventSystem.current.currentSelectedGameObject;
                var selectable=selected==null?null:selected.GetComponent<Selectable>();
                if(selectable==null||!selectable.IsInteractable()||!controls.Contains(selectable))
                    throw new Exception("Tutorial keyboard navigation reached a blocked control from "+control.name+" toward "+direction);
            }
            EventSystem.current.SetSelectedGameObject(original!=null&&original.GetComponent<Selectable>()!=null&&original.GetComponent<Selectable>().IsInteractable()?original:controls.FirstOrDefault()?.gameObject);
        }

        IEnumerator TutorialSmokeBlocked(string name)
        {
            var button=TutorialSmokeButton(name);
            if(button==null)throw new Exception("Tutorial blocked-control fixture missing: "+name);
            if(button.IsInteractable())throw new Exception("Strong tutorial allows an unrelated action: "+name);
            string before=JsonUtility.ToJson(State),beforeModal=modal;int beforeTab=commandTab;
            // Send a real pointer event only to the first raycast hit, then a
            // Submit directly to the disabled selectable. Neither may execute.
            var rt=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(viewCamera,rt.TransformPoint(rt.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count>0)ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            yield return null;yield return new WaitForEndOfFrame();
            if(before!=JsonUtility.ToJson(State)||modal!=beforeModal||commandTab!=beforeTab)
                throw new Exception("Blocked tutorial control changed state through mouse or Submit: "+name);
        }

        IEnumerator TutorialSmokePrompts()
        {
            for(int guard=0;guard<20;guard++)
            {
                if(TutorialSmokeCanClick("CommandEventSafe"))
                {
                    TutorialSmokeLayout();RequireKeyboardScope("CommandShade");
                    yield return Click("CommandEventSafe");tutorialSmokeEventChoices++;continue;
                }
                if(TutorialSmokeCanClick("CommandEventRisk"))
                {yield return Click("CommandEventRisk");tutorialSmokeEventChoices++;continue;}
                if(TutorialSmokeCanClick("CommandNoticeDismiss"))
                {TutorialSmokeLayout();yield return Click("CommandNoticeDismiss");continue;}
                if(TutorialSmokeCanClick("TutorialAck"))
                {
                    TutorialSmokeLayout();RequireKeyboardScope("TutorialShade");
                    yield return KeyboardSubmit("TutorialAck");continue;
                }
                yield break;
            }
            throw new Exception("Tutorial prompts failed to settle");
        }

        IEnumerator TutorialSmokeCapture(string path,string suffix)
        {
            yield return null;yield return new WaitForEndOfFrame();TutorialSmokeLayout();
            Capture(Path.ChangeExtension(path,"tutorial-"+suffix+".png"));
        }

        IEnumerator TutorialSmokeMap()
        {
            for(int guard=0;guard<8;guard++)
            {
                yield return TutorialSmokePrompts();
                if(commandTab==3&&modal=="")yield break;
                string close=new[]{"MemberDetailsClose","MissionBack","RouteDetailsClose","CompareBack","CommandBack","CommandPageClose","CommandResume"}.FirstOrDefault(TutorialSmokeCanClick);
                if(close==null)throw new Exception("Cannot return to tutorial map from "+modal+" / "+commandTab);
                yield return Click(close);
            }
            throw new Exception("Tutorial return loop failed");
        }

        IEnumerator TutorialSmokePauseResume()
        {
            yield return TutorialSmokeMap();
            string before=JsonUtility.ToJson(State);
            yield return Click("TutorialPause");TutorialSmokeLayout();RequireKeyboardScope("TutorialShade");
            yield return KeyboardSubmit("TutorialContinue");
            if(JsonUtility.ToJson(State)!=before)throw new Exception("Pausing and continuing changed tutorial progress");
            yield return Click("TutorialPause");yield return Click("TutorialExit");
            if(!atTitle)throw new Exception("Tutorial exit did not return to the title");
            yield return Click("HomeTutorial");
            string saved=TutorialSmokeFile(TutorialSavePath);
            yield return Click("TutorialReplay");TutorialSmokeLayout();
            RequireKeyboardScope("TutorialShade");
            if(TutorialSmokeFile(TutorialSavePath)!=saved)throw new Exception("Opening replay confirmation replaced the tutorial before consent");
            yield return KeyboardSubmit("TutorialRestartCancel");
            if(TutorialSmokeFile(TutorialSavePath)!=saved)throw new Exception("Cancelling replay changed the tutorial save");
            yield return Click("TutorialResume");
            if(atTitle||!TutorialRules.IsTutorial(State)||JsonUtility.ToJson(State)!=before)
                throw new Exception("Tutorial resume did not restore the exact saved stage, assignments and random stream");
            if(SavePath!=smokeSave+".tutorial")throw new Exception("Resuming tutorial selected the regular save slot");
            yield return TutorialSmokePrompts();
        }

        IEnumerator TutorialSmokeRestart()
        {
            // The full eight-night result is already captured. Exercise replay
            // on a fresh unfinished lesson without touching the ordinary slot.
            yield return Click("HomeTutorial");yield return Click("TutorialBegin");
            yield return TutorialSmokePrompts();
            string previous=State.runId;
            yield return Click("TutorialPause");yield return Click("TutorialExit");
            string saved=TutorialSmokeFile(TutorialSavePath);
            yield return Click("HomeTutorial");yield return Click("TutorialReplay");
            RequireKeyboardScope("TutorialShade");
            if(TutorialSmokeFile(TutorialSavePath)!=saved)throw new Exception("Replay changed disk before confirmation");
            yield return Click("TutorialRestartConfirm");
            if(atTitle||!TutorialRules.IsTutorial(State)||State.runId==previous||State.tutorial.stage!=0||State.turn!=1)
                throw new Exception("Confirmed replay did not create a fresh first-night lesson");
            if(TutorialSmokeFile(TutorialSavePath+".bak")!=saved)throw new Exception("Confirmed replay did not preserve the previous tutorial backup");
            yield return TutorialSmokePrompts();
            yield return Click("TutorialPause");yield return Click("TutorialExit");
        }

        IEnumerator TutorialSmokeLessonReview(string path)
        {
            string before=JsonUtility.ToJson(State),saved=TutorialSmokeFile(TutorialSavePath),backup=TutorialSmokeFile(TutorialSavePath+".bak");
            int learned=State.tutorial.stage;
            yield return Click("TutorialHelp");TutorialSmokeLayout();RequireKeyboardScope("TutorialShade");
            yield return TutorialSmokeBlocked("TutorialNextLesson");
            for(int lesson=learned;lesson>0;lesson--)
            {
                yield return Click("TutorialPreviousLesson");TutorialSmokeLayout();
                if(tutorialHelpStage!=lesson-1)throw new Exception("Lesson review did not move to the previous learned lesson");
                if(tutorialHelpStage==4)yield return TutorialSmokeCapture(path,"instruction-popup");
            }
            yield return TutorialSmokeBlocked("TutorialPreviousLesson");
            for(int lesson=0;lesson<learned;lesson++)
            {
                yield return Click("TutorialNextLesson");TutorialSmokeLayout();
                if(tutorialHelpStage!=lesson+1)throw new Exception("Lesson review did not move to the next learned lesson");
            }
            TutorialSmokeNavigation();yield return KeyboardSubmit("TutorialAck");
            if(TutorialDialogVisible||JsonUtility.ToJson(State)!=before||TutorialSmokeFile(TutorialSavePath)!=saved||TutorialSmokeFile(TutorialSavePath+".bak")!=backup)
                throw new Exception("Reviewing learned instructions changed tutorial progress, assignments, RNG or saved files");
        }

        IEnumerator TutorialSmokeStaleBackup()
        {
            if(string.IsNullOrEmpty(smokeSave)||!TutorialRules.IsTutorial(State)||State.tutorial.stage!=2)
                throw new Exception("Tutorial recovery fixture requires an isolated, authorized first task");
            string before=JsonUtility.ToJson(State);
            var backup=JsonUtility.FromJson<RunState>(before);
            // Authorize saves its real assignment before the UI observation
            // advances the lesson. Such a valid backup can contain a stage-1
            // lesson together with the already assigned first task.
            backup.tutorial.stage=1;backup.tutorial.introShownStage=1;
            var task=backup.command.tasks.First(t=>t.id==backup.tutorial.firstTaskId);
            if(task.actor<0||!backup.tutorial.comparedMember)throw new Exception("Tutorial recovery fixture has no real authorized task");
            string fixture=smokeSave+".tutorial-recovery";
            try
            {
                PrototypeSave.Write(backup,fixture+".bak");
                File.WriteAllText(fixture,"{broken tutorial primary");
                string damaged=TutorialSmokeFile(fixture),valid=TutorialSmokeFile(fixture+".bak");
                // Fault injection is the only non-click load in this smoke:
                // strong guidance deliberately keeps the menu Load disabled.
                LoadRun(fixture);yield return null;yield return new WaitForEndOfFrame();
                var expected=JsonUtility.FromJson<RunState>(before);expected.tutorial.introShownStage=1;
                if(atTitle||!TutorialRules.IsTutorial(State)||State.tutorial.stage!=2||JsonUtility.ToJson(State)!=JsonUtility.ToJson(expected))
                    throw new Exception("Backup recovery failed to refresh the stale lesson, or changed assignment, resources or RNG");
                if(TutorialSmokeFile(fixture)!=damaged||TutorialSmokeFile(fixture+".bak")!=valid||!toast.Contains("备份"))
                    throw new Exception("Backup recovery silently rewrote evidence or failed to explain recovery");
                if(!TutorialSmokeCanClick("TutorialAck"))throw new Exception("Recovered authorized task did not offer the execution lesson");
                RequireKeyboardScope("TutorialShade");yield return TutorialSmokePrompts();
                if(JsonUtility.ToJson(State)!=before||!TutorialSmokeCanClick("CommandEnd"))
                    throw new Exception("Recovered tutorial is blocked or differs after acknowledging the execution lesson");
                Debug.Log("MINGLU_TUTORIAL_BACKUP_UI_SUCCESS: damaged isolated primary + valid stage-1/assigned-task backup loaded through LoadRun; stage 2 restored without changing actor, resources or RNG; lesson acknowledged and advance control available");
            }
            finally
            {
                foreach(string file in new[]{fixture,fixture+".bak",fixture+".tmp",fixture+".bak.bak",fixture+".bak.tmp"})
                    if(File.Exists(file))File.Delete(file);
            }
        }

        IEnumerator TutorialSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Tutorial smoke requires --minglu-capture isolated data");
            tutorialSmokeEventChoices=0;
            // Establish an actual ordinary run first. The lesson must never
            // silently replace it, including its .bak and personal records.
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            yield return Click("CommandMenu");yield return Click("CommandHome");
            string regularId=State.runId;
            profile.records.Add(new RunRecord{id="tutorial-isolated-protected-record",date="2000-01-01",doctrine="test",seed=123,difficulty=1,progress=7,turn=8,won=false});
            RunLifecycle.WriteProfile(profile,ProfilePath);
            string profileBefore=TutorialSmokeProfileState();
            var regularBefore=new Dictionary<string,string>();
            foreach(string file in new[]{smokeSave,smokeSave+".bak",smokeSave+".tmp"})regularBefore[file]=TutorialSmokeFile(file);
            Draw();yield return null;
            yield return Click("HomeTutorial");yield return TutorialSmokeCapture(path,"intro");
            yield return Click("TutorialBegin");
            if(!TutorialRules.IsTutorial(State)||State.config.Value("turns")!=8||State.initialSeed!=43||SavePath!=smokeSave+".tutorial")
                throw new Exception("Dedicated tutorial did not create its eight-night seed43 isolated run");
            yield return TutorialSmokePrompts();
            if(State.tutorial.stage!=0)throw new Exception("Tutorial must start at member inspection");
            yield return TutorialSmokeBlocked("CommandEnd");yield return TutorialSmokeBlocked("CommandTab_5");TutorialSmokeNavigation();

            yield return Click("CommandTab_2");yield return Click("CommandPeopleDetails");
            yield return TutorialSmokeCapture(path,"member");
            if(State.tutorial.stage!=0)throw new Exception("Opening the member immediately hid the information behind the next lesson");
            yield return Click("MemberDetailsClose");yield return TutorialSmokePrompts();
            if(State.tutorial.stage!=1||!State.tutorial.inspectedMember)throw new Exception("Reading and closing the member did not complete the first lesson");
            yield return TutorialSmokeMap();yield return TutorialSmokePauseResume();

            int first=State.tutorial.firstTaskId;
            yield return Click("CommandNextAction");yield return TutorialSmokeBlocked("CommandAuthorize");yield return Click("CommandCompare_"+first);
            int firstActor=State.tutorial.freeActorId;
            yield return AssignmentScrollTo("CandidateList","CompareSelect_"+firstActor);yield return Click("CompareSelect_"+firstActor);
            yield return TutorialSmokeCapture(path,"task");TutorialSmokeNavigation();
            yield return Click("CommandAuthorize");yield return TutorialSmokePrompts();
            if(State.tutorial.stage!=2||!State.tutorial.comparedMember||State.command.tasks.First(t=>t.id==first).actor!=firstActor)
                throw new Exception("Tutorial comparison and authorization were not performed by the selected member");
            yield return TutorialSmokeStaleBackup();
            yield return TutorialSmokeMap();yield return Click("CommandEnd");
            RequireWorkOverviewLayout();yield return Click("CommandExecute");yield return TutorialSmokePrompts();
            if(State.turn!=2||State.tutorial.stage!=3)throw new Exception("First real execution did not advance to the report lesson");
            yield return Click("CommandTab_4");yield return TutorialSmokeBlocked("CommandPageClose");yield return Click("ReportAll");
            if(State.tutorial.stage!=3)throw new Exception("Report lesson advanced before the player finished reading");
            yield return Click("CommandPageClose");yield return TutorialSmokePrompts();
            if(State.tutorial.stage!=4||!State.tutorial.reportRead)throw new Exception("Reading full report and returning did not complete report lesson");

            yield return Click("CommandTab_1");
            int[] roleIds={State.tutorial.sourceId,State.tutorial.liaisonId,State.tutorial.courierId};
            for(int slot=0;slot<3;slot++)
            {
                yield return Click("RouteSlot_0_"+slot);
                yield return AssignmentScrollTo("RouteCandidates","RouteCandidate_"+roleIds[slot]);
                yield return Click("RouteCandidate_"+roleIds[slot]);
            }
            yield return TutorialSmokeCapture(path,"chain");yield return Click("RouteEnable_0");yield return TutorialSmokePrompts();
            var route=State.command.routes.First(r=>r.id==0);
            if(State.tutorial.stage!=5||!route.enabled||CommandRules.RouteError(State,route)!="")throw new Exception("Tutorial route was not configured and enabled through actual controls");
            yield return TutorialSmokeMap();yield return Click("CommandTab_2");
            yield return Click("CommandAgent_"+State.tutorial.restActorId);yield return Click("CommandPeopleDetails");
            yield return Click("LifePolicy_1");yield return TutorialSmokePrompts();
            if(State.tutorial.stage!=6||!State.tutorial.lifeArranged)throw new Exception("Tutorial life choice did not enter free practice");
            yield return TutorialSmokeMap();yield return TutorialSmokeCapture(path,"weak");yield return TutorialSmokeLessonReview(path);yield return TutorialSmokePauseResume();

            // Play the rest of the lesson with normal task, route and event
            // rules. Never inject success, intelligence, turns or RNG results.
            for(int guard=0;guard<9&&!State.ended;guard++)
            {
                yield return TutorialSmokePrompts();yield return TutorialSmokeMap();
                var eligible=State.command.tasks.Where(t=>t.status==0&&t.actor<0&&(t.id==State.tutorial.firstTaskId||t.id==State.tutorial.secondTaskId)).OrderBy(t=>t.id).ToList();
                foreach(var task in eligible)
                {
                    var person=State.agents.Where(a=>!roleIds.Contains(a.id)&&CommandRules.TaskError(State,a.id,task)=="")
                        .OrderByDescending(a=>CommandRules.TaskDice(State,a,task)).FirstOrDefault();
                    if(person==null)continue;
                    yield return Click("CommandTab_0");
                    yield return AssignmentScrollTo("CommandTasks","CommandTask_"+task.id);yield return Click("CommandTask_"+task.id);
                    yield return Click("CommandCompare_"+task.id);
                    yield return AssignmentScrollTo("CandidateList","CompareSelect_"+person.id);yield return Click("CompareSelect_"+person.id);
                    yield return Click("CommandAuthorize");yield return TutorialSmokeMap();
                }
                int night=State.turn;yield return Click("CommandEnd");yield return Click("CommandExecute");
                if(!State.ended&&State.turn!=night+1)throw new Exception("Tutorial night did not resolve exactly once");
                yield return TutorialSmokePrompts();
            }
            if(!State.ended||!State.won||State.tutorial.resolvedNights!=8||State.command.completed<2||State.command.delivered<1||!State.tutorial.eventHandled||tutorialSmokeEventChoices<1)
                throw new Exception("Tutorial did not finish a real eight-night game with tasks, delivery and an event: "+JsonUtility.ToJson(State.tutorial));
            yield return TutorialSmokeCapture(path,"ending");
            string endingBefore=JsonUtility.ToJson(State);
            yield return Click("CommandEndReview");yield return Click("EndingReviewBack");
            if(JsonUtility.ToJson(State)!=endingBefore)throw new Exception("Tutorial ending review changed the finished run");
            yield return Click("CommandEndHome");
            if(!atTitle)throw new Exception("Tutorial ending did not return to the title");
            yield return TutorialSmokeRestart();
            if(TutorialSmokeProfileState()!=profileBefore)throw new Exception("Tutorial changed ordinary records or portrait presets");
            if(TutorialSmokeProfileState(RunLifecycle.ReadProfile(ProfilePath))!=profileBefore)throw new Exception("Tutorial changed ordinary records or portrait presets on disk");
            foreach(var entry in regularBefore)if(TutorialSmokeFile(entry.Key)!=entry.Value)throw new Exception("Tutorial overwrote the protected regular slot: "+entry.Key);
            yield return Click("HomeContinue");
            if(TutorialRules.IsTutorial(State)||State.runId!=regularId)throw new Exception("Ordinary Continue resumed the tutorial instead of the protected game");

            foreach(string file in new[]{smokeSave,smokeSave+".bak",smokeSave+".tmp",smokeSave+".tutorial",smokeSave+".tutorial.bak",smokeSave+".tutorial.tmp",ProfilePath,ProfilePath+".bak",ProfilePath+".tmp"})
                if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_TUTORIAL_UI_SUCCESS: "+Screen.width+"x"+Screen.height+" actual pointer/Move/Submit, intro/member/task/report/chain/life/free practice, read-only review of all seven learned lessons, pause/resume, replay cancel/confirm with backup, stale-stage backup load recovery, real eight-night tasks/delivery/event/ending review; protected regular saves and ordinary records unchanged in memory and on disk; screenshots require visual review");
        }
    }
}
