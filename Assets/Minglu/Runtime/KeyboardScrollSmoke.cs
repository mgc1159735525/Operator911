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
        Bounds KeyboardControlBounds(RectTransform viewport,RectTransform control)
        {
            var corners=new Vector3[4];control.GetWorldCorners(corners);
            var bounds=new Bounds(viewport.InverseTransformPoint(corners[0]),Vector3.zero);
            for(int i=1;i<4;i++)bounds.Encapsulate(viewport.InverseTransformPoint(corners[i]));
            return bounds;
        }
        void RequireKeyboardVisible(ScrollRect scroll,string expected)
        {
            var focused=EventSystem.current.currentSelectedGameObject;
            if(focused==null||focused.name!=expected)throw new Exception("Keyboard list expected "+expected+", selected "+(focused==null?"none":focused.name));
            var bounds=KeyboardControlBounds(scroll.viewport,(RectTransform)focused.transform);
            var view=scroll.viewport.rect;
            if(bounds.min.y<view.yMin-1||bounds.max.y>view.yMax+1||bounds.min.x<view.xMin-1||bounds.max.x>view.xMax+1)
                throw new Exception("Keyboard focus outside viewport: "+expected+" bounds="+bounds+" view="+view);
            var scope=focused.transform;while(scope.parent!=root&&scope.parent!=null)scope=scope.parent;
            if(scope.name!="CommandShade")throw new Exception("Keyboard scroll escaped the active command dialog");
            if(root.GetComponentsInChildren<Selectable>().Any(control=>!control.transform.IsChildOf(scope)&&control.IsInteractable()))throw new Exception("Background interaction became active while scrolling a modal");
        }
        IEnumerator KeyboardScrollSmoke(string path,bool probe)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Keyboard scroll smoke requires isolated saves");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            yield return Click("CommandTab_0");
            var task=State.command.tasks.First(t=>CommandRules.CanStaffTask(State,t));
            yield return Click("CommandRecommend_"+task.id);yield return Click("CommandCompare_"+task.id);
            string stateBefore=JsonUtility.ToJson(State);
            var scroll=root.GetComponentsInChildren<ScrollRect>().Single(s=>s.name=="CandidateList");
            var candidates=scroll.content.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()).ToArray();
            if(candidates.Length<5)throw new Exception("Keyboard scroll fixture needs at least five eligible candidates");
            EventSystem.current.SetSelectedGameObject(candidates[0].gameObject);
            bool reproduced=false;
            for(int i=1;i<candidates.Length;i++)
            {
                KeyboardMove(MoveDirection.Down);yield return null;yield return new WaitForEndOfFrame();
                var focused=EventSystem.current.currentSelectedGameObject;
                if(focused==null||focused.name!=candidates[i].name)throw new Exception("Candidate list skipped an entry while navigating downward");
                var bounds=KeyboardControlBounds(scroll.viewport,(RectTransform)focused.transform);
                bool invisible=bounds.max.y<scroll.viewport.rect.yMin||bounds.min.y>scroll.viewport.rect.yMax;
                if(probe&&invisible)
                {
                    string target=focused.name;int actor=int.Parse(target.Substring("CompareSelect_".Length));
                    string geometry="viewport="+scroll.viewport.rect+" bounds="+bounds;
                    Capture(Path.ChangeExtension(path,"scroll-invisible-focus.png"));
                    ExecuteEvents.Execute(focused,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
                    yield return null;yield return new WaitForEndOfFrame();
                    if(modal!="missionPlan"||selectedAgent!=actor)throw new Exception("Probe did not submit the invisible candidate");
                    Capture(Path.ChangeExtension(path,"scroll-invisible-selected.png"));
                    Debug.Log("MINGLU_KEYBOARD_SCROLL_REPRODUCED: "+i+" Down moves selected "+target+" fully outside CandidateList; Submit selected that invisible member; "+geometry);
                    reproduced=true;break;
                }
                if(!probe)RequireKeyboardVisible(scroll,candidates[i].name);
            }
            if(probe)
            {
                if(!reproduced)throw new Exception("Historical invisible-focus bug is no longer reproducible; use --keyboard-scroll-only");
            }
            else
            {
                if(scroll.verticalNormalizedPosition>=.99f)throw new Exception("Candidate focus did not scroll the list");
                Capture(Path.ChangeExtension(path,"scroll-candidate-bottom.png"));
                for(int i=candidates.Length-2;i>=0;i--)
                {KeyboardMove(MoveDirection.Up);yield return null;RequireKeyboardVisible(scroll,candidates[i].name);}
                KeyboardMove(MoveDirection.Down);KeyboardMove(MoveDirection.Down);yield return null;
                string selected=EventSystem.current.currentSelectedGameObject.name;
                Draw();yield return null;
                scroll=root.GetComponentsInChildren<ScrollRect>().Single(s=>s.name=="CandidateList");
                RequireKeyboardVisible(scroll,selected);
                int actor=int.Parse(selected.Substring("CompareSelect_".Length));
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
                yield return null;
                if(modal!="missionPlan"||selectedAgent!=actor||JsonUtility.ToJson(State)!=stateBefore)throw new Exception("Visible candidate selection altered the run or chose the wrong member");
                yield return Click("MissionBack");yield return Click("CommandPageClose");
                for(int i=0;i<12;i++)CommandRules.Notice(State,new CommandNotice{title="滚动要报 "+(i+1),body="这是一则独立的键盘回归记录。选择相关档案前，按钮和焦点必须进入当前可见区域。",tab=4});
                Draw();yield return Click("CommandNoticeDismiss");yield return Click("CommandTab_4");yield return Click("ReportNotices");
                scroll=root.GetComponentsInChildren<ScrollRect>().Single(s=>s.name=="NoticeArchiveList");
                var notices=scroll.content.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()).ToArray();
                EventSystem.current.SetSelectedGameObject(notices[0].gameObject);
                for(int i=1;i<notices.Length;i++)
                {KeyboardMove(MoveDirection.Down);yield return null;RequireKeyboardVisible(scroll,notices[i].name);}
                yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"scroll-archive-bottom.png"));
                for(int i=notices.Length-2;i>=0;i--)
                {KeyboardMove(MoveDirection.Up);yield return null;RequireKeyboardVisible(scroll,notices[i].name);}
                for(int i=1;i<notices.Length;i++)
                {KeyboardMove(MoveDirection.Down);yield return null;RequireKeyboardVisible(scroll,notices[i].name);}
                KeyboardMove(MoveDirection.Down);yield return null;
                if(EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="NoticeArchiveClose")throw new Exception("Last archive entry did not allow keyboard navigation to the safe return control");
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);yield return null;
                if(modal!=""||commandTab!=4)throw new Exception("Keyboard archive return did not restore the report");
                yield return Click("CommandPageClose");
                if(!root.GetComponentsInChildren<Button>().Single(b=>b.name=="CommandMenu").IsInteractable())throw new Exception("Background controls did not recover after closing the modal");
                Debug.Log("MINGLU_KEYBOARD_SCROLL_UI_SUCCESS: actual Up/Down and Submit keep candidate and 12-entry archive focus visible, preserve redraw selection, block the background and restore map controls");
            }
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
        }
    }
}
