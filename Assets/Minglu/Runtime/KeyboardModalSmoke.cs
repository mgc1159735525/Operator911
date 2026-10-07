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
        static readonly MoveDirection[] KeyboardDirections={MoveDirection.Up,MoveDirection.Down,MoveDirection.Left,MoveDirection.Right};
        Selectable KeyboardNeighbour(Selectable item,MoveDirection direction)
        {
            return direction==MoveDirection.Up?item.FindSelectableOnUp():direction==MoveDirection.Down?item.FindSelectableOnDown():direction==MoveDirection.Left?item.FindSelectableOnLeft():item.FindSelectableOnRight();
        }
        void KeyboardMove(MoveDirection direction)
        {
            var current=EventSystem.current.currentSelectedGameObject;
            if(current==null)throw new Exception("Keyboard move lost focus");
            var axis=new AxisEventData(EventSystem.current){moveDir=direction,moveVector=direction==MoveDirection.Up?Vector2.up:direction==MoveDirection.Down?Vector2.down:direction==MoveDirection.Left?Vector2.left:Vector2.right};
            ExecuteEvents.Execute(current,axis,ExecuteEvents.moveHandler);
        }
        IEnumerator KeyboardSubmit(string name)
        {
            var target=root.GetComponentsInChildren<Selectable>().FirstOrDefault(s=>s.name==name&&s.IsInteractable());
            if(target==null)throw new Exception("Keyboard submit target unavailable: "+name);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
            ExecuteEvents.Execute(target.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            yield return null;yield return new WaitForEndOfFrame();
        }
        void RequireKeyboardScope(string scopeName)
        {
            var scope=root.GetComponentsInChildren<Transform>().Last(t=>t.name==scopeName);
            var all=root.GetComponentsInChildren<Selectable>();
            foreach(var item in all)
            {
                if(!item.transform.IsChildOf(scope))
                {
                    if(item.IsInteractable())throw new Exception("Keyboard modal leaves background active: "+scopeName+" / "+item.name);
                    continue;
                }
                if(!item.IsInteractable())continue;
                foreach(var direction in KeyboardDirections)
                {
                    EventSystem.current.SetSelectedGameObject(item.gameObject);KeyboardMove(direction);
                    var selected=EventSystem.current.currentSelectedGameObject;
                    if(selected==null||!selected.transform.IsChildOf(scope))throw new Exception("Keyboard focus escaped "+scopeName+" from "+item.name+" toward "+direction);
                }
            }
        }
        // Reproduces the former bug through Unity's actual Move/Submit handlers,
        // rather than invoking a hidden button's onClick directly.
        IEnumerator ProbeKeyboardEscape(string path)
        {
            var start=root.GetComponentsInChildren<Selectable>().First(s=>s.name=="LookCurrent_0");
            var destination=root.GetComponentsInChildren<Selectable>().First(s=>s.name=="CommandMenu");
            var queue=new Queue<Selectable>();var paths=new Dictionary<Selectable,List<MoveDirection>>();
            queue.Enqueue(start);paths[start]=new List<MoveDirection>();
            while(queue.Count>0&&!paths.ContainsKey(destination))
            {
                var item=queue.Dequeue();
                foreach(var direction in KeyboardDirections)
                {
                    var next=KeyboardNeighbour(item,direction);
                    if(next==null||paths.ContainsKey(next))continue;
                    paths[next]=new List<MoveDirection>(paths[item]){direction};queue.Enqueue(next);
                }
            }
            if(!paths.ContainsKey(destination))throw new Exception("Historical keyboard escape no longer reachable; run --keyboard-modal-only for regression coverage");
            EventSystem.current.SetSelectedGameObject(start.gameObject);
            foreach(var direction in paths[destination])KeyboardMove(direction);
            if(EventSystem.current.currentSelectedGameObject!=destination.gameObject)throw new Exception("Probe route did not reach background settings");
            if(!LookDirty())throw new Exception("Probe must begin with an unapplied appearance");
            Capture(Path.ChangeExtension(path,"keyboard-background-focus.png"));
            ExecuteEvents.Execute(destination.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            yield return null;yield return new WaitForEndOfFrame();
            if(modal!="menu"||lookPopup=="discard")throw new Exception("Historical escape did not bypass the draft warning");
            Capture(Path.ChangeExtension(path,"keyboard-draft-bypassed.png"));
            Debug.Log("MINGLU_KEYBOARD_MODAL_REPRODUCED: direction path "+string.Join(",",paths[destination])+" reached background settings; Submit closed dirty appearance without discard confirmation");
        }
        IEnumerator KeyboardModalSmoke(string path,bool probe)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Keyboard smoke requires isolated saves");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            yield return Click("CommandOverview");
            if(modal=="campaignDossier")yield return Click("CampaignOrganization");
            yield return Click("CommandChiefAppearance");yield return Click("LookCurrent_0");
            if(probe){yield return ProbeKeyboardEscape(path);yield break;}
            string before=JsonUtility.ToJson(State);int first=LookIndex(0);
            if(TryPrepareQuit()||lookPopup!="discard"||quitApproved)throw new Exception("Window close discarded an unapplied in-game portrait draft");
            RequireKeyboardScope("LookPopupShade");yield return KeyboardSubmit("LookPopupKeep");
            RequireKeyboardScope("AppearanceShade");
            yield return KeyboardSubmit("LookCurrent_0");
            if(LookIndex(0)==first||EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="LookCurrent_0")throw new Exception("Keyboard component cycling or focus restoration stopped working");
            yield return KeyboardSubmit("LookPresets");RequireKeyboardScope("LookPopupShade");
            yield return KeyboardSubmit("LookSaveSlot");RequireKeyboardScope("LookPopupShade");
            yield return KeyboardSubmit("LookSaveSlot");
            if(lookPopup!="overwrite")throw new Exception("Preset overwrite confirmation did not open");
            RequireKeyboardScope("LookPopupShade");yield return KeyboardSubmit("LookPopupKeep");
            yield return KeyboardSubmit("LookClose");
            if(lookPopup!="discard")throw new Exception("Dirty appearance did not request confirmation");
            RequireKeyboardScope("LookPopupShade");yield return KeyboardSubmit("LookPopupKeep");
            if(modal!="appearance"||!LookDirty())throw new Exception("Continuing editing lost the draft");
            RequireKeyboardScope("AppearanceShade");Capture(Path.ChangeExtension(path,"keyboard-appearance.png"));
            yield return KeyboardSubmit("LookClose");yield return KeyboardSubmit("LookPopupConfirm");
            if(modal!="commandOverview"||JsonUtility.ToJson(State)!=before)throw new Exception("Discard did not restore overview without changing state");
            RequireKeyboardScope("CommandShade");yield return Click("OverviewClose");
            foreach(int page in new[]{0,1,2,4,5})
            {
                yield return Click("CommandTab_"+page);RequireKeyboardScope("CommandShade");
                yield return KeyboardSubmit("CommandPageClose");
            }
            yield return Click("CommandMenu");yield return Click("CommandGuide");
            RequireKeyboardScope("CommandShade");
            if(!root.GetComponentsInChildren<Scrollbar>().Any(s=>s.IsInteractable()))throw new Exception("Guide scrollbar became unavailable inside its modal");
            yield return KeyboardSubmit("CommandGuideBack");yield return KeyboardSubmit("CommandResume");
            CommandRules.Notice(State,new CommandNotice{title="键盘隔离检查",body="未读要报覆盖地图时，焦点仍须留在要报内。",tab=4});Draw();yield return null;
            RequireKeyboardScope("CommandShade");yield return KeyboardSubmit("CommandNoticeDismiss");
            State.command.eventKind=1;State.command.eventActor=State.agents[0].id;State.command.eventText="键盘事件选择检查";Draw();yield return null;
            RequireKeyboardScope("CommandShade");yield return KeyboardSubmit("CommandEventSafe");
            if(State.command.eventKind!=0)throw new Exception("Keyboard event decision did not execute");
            if(State.command.notices.Count>0)yield return KeyboardSubmit("CommandNoticeDismiss");
            yield return Click("CommandMenu");yield return Click("CommandHome");yield return Click("HomeNew");
            yield return Click("BeginRun");
            if(homePage!="replace")throw new Exception("New-run replacement confirmation missing");
            RequireKeyboardScope("ReplaceShade");
            if(root.GetComponentsInChildren<InputField>().Any(i=>i.IsInteractable()))throw new Exception("Seed input accepts keyboard input behind replace confirmation");
            yield return KeyboardSubmit("CancelNewRun");
            if(!root.GetComponentsInChildren<InputField>().Any(i=>i.IsInteractable()))throw new Exception("Seed input did not recover after closing the confirmation");
            yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"keyboard-new-run.png"));
            OpenHome();yield return null;yield return Click("HomeAppearance");yield return Click("LookCurrent_0");
            if(TryPrepareQuit()||lookPopup!="discard"||quitApproved)throw new Exception("Window close discarded an unapplied main-menu portrait draft");
            RequireKeyboardScope("LookPopupShade");yield return KeyboardSubmit("LookPopupKeep");
            if(!LookDirty()||homePage!="appearance")throw new Exception("Cancelling close lost the main-menu portrait draft");
            yield return KeyboardSubmit("LookClose");yield return KeyboardSubmit("LookPopupConfirm");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_KEYBOARD_MODAL_UI_SUCCESS: Move/Submit stay inside top dialog; appearance cycle, preset/discard confirmations, five business windows, report scrollbar, notices/events and replacement input isolation");
        }
    }
}
