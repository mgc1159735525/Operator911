using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        Button StudioButton(string name)
        {
            foreach(var button in root.GetComponentsInChildren<Button>())if(button.name==name)return button;
            throw new Exception("Portrait control missing: "+name);
        }
        // Exercise the visible selector instead of setting draft fields directly.
        // Other portrait smoke journeys share this helper, so catalog growth
        // does not require hard-coded option buttons or page numbers.
        IEnumerator StudioSetOption(int category,int option)
        {
            int group=category==0||category==1||category==7?0:category==2||category==3||category==4||category==5||category==6||category==13?1:2;
            if(lookGroup!=group)yield return Click("LookGroup_"+group);
            if(option<0||option>=LookOptions(category).Length)throw new Exception("Portrait option out of range: "+category+" / "+option);
            yield return Click("LookCategory_"+category);
            int remaining=LookOptions(category).Length;
            while(LookIndex(category)!=option&&remaining-->0)yield return Click("LookNext_"+category);
            if(LookIndex(category)!=option)throw new Exception("Portrait cycle did not reach option: "+category+" / "+option);
        }
        IEnumerator CheckStudioCycle(int category)
        {
            // Face selection intentionally defaults elders to silver. Lock the
            // colour through its visible control for this independence test;
            // GroomingSmoke separately tests the age default and override.
            bool restoreColorLock=category==0&&!lookLocks[12];
            if(restoreColorLock){yield return StudioSetOption(12,LookIndex(12));yield return Click("LookLock_12");}
            yield return StudioSetOption(category,0);
            int count=LookOptions(category).Length;
            var before=AppearanceRules.Copy(lookDraft);
            yield return Click("LookPrevious_"+category);
            if(LookIndex(category)!=count-1)throw new Exception("Portrait previous did not wrap: "+category);
            yield return Click("LookCurrent_"+category);
            if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(before))throw new Exception("Portrait current-name click did not wrap exactly: "+category);
            if(EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="LookCurrent_"+category)throw new Exception("Portrait cycle lost keyboard focus after redraw: "+category);
            for(int option=1;option<=count;option++)
            {
                yield return Click("LookNext_"+category);
                var expected=AppearanceRules.Copy(before);
                if(category==13)expected.ears=option%count;else expected.parts[category]=option%count;
                if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expected))throw new Exception("Portrait cycle changed another part or skipped an option: "+category+" / "+option);
            }
            var current=StudioButton("LookCurrent_"+category);EventSystem.current.SetSelectedGameObject(current.gameObject);
            ExecuteEvents.Execute(current.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            yield return null;yield return new WaitForEndOfFrame();
            if(LookIndex(category)!=1||EventSystem.current.currentSelectedGameObject==null||EventSystem.current.currentSelectedGameObject.name!="LookCurrent_"+category)throw new Exception("Portrait keyboard Submit failed or lost focus: "+category);
            yield return Click("LookPrevious_"+category);
            if(restoreColorLock){yield return StudioSetOption(12,LookIndex(12));yield return Click("LookLock_12");yield return StudioSetOption(category,0);}
        }
        void CheckStudioPanel(string name)
        {
            Canvas.ForceUpdateCanvases();var panel=GameObject.Find(name).GetComponent<RectTransform>();
            var corners=new Vector3[4];
            foreach(var button in panel.GetComponentsInChildren<Button>())
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(var world in corners)
                {
                    var local=panel.InverseTransformPoint(world);
                    if(local.x<panel.rect.xMin-1||local.x>panel.rect.xMax+1||local.y<panel.rect.yMin-1||local.y>panel.rect.yMax+1)throw new Exception("Portrait control extends beyond its panel: "+button.name);
                }
            }
            foreach(var label in panel.GetComponentsInChildren<Text>())
            {
                if(label.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(label.text)&&label.verticalOverflow==VerticalWrapMode.Truncate&&label.preferredHeight>label.rectTransform.rect.height+2)
                    throw new Exception("Portrait text is clipped: "+label.text);
            }
        }
        IEnumerator RequireStudioPopupBlocksEditor()
        {
            // Draw replaces the canvas hierarchy in the click handler. Wait for
            // a rendered frame, as Click does, before inspecting graphic depth.
            yield return null;yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
            var button=StudioButton("LookRandom");var rt=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(viewCamera,rt.TransformPoint(rt.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()==button)throw new Exception("Portrait popup does not block editor: hits="+hits.Count+", first="+(hits.Count==0?"none":hits[0].gameObject.name));
        }
        IEnumerator AppearanceStudioSmoke(string path)
        {
            Capture(Path.ChangeExtension(path,"home.png"));yield return Click("HomeNew");CheckStudioPanel("HomePanel");Capture(Path.ChangeExtension(path,"new-run.png"));
            yield return Click("HomeBack");yield return Click("HomeAppearance");CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"editor-initial.png"));
            string untouched=JsonUtility.ToJson(lookDraft);int untouchedUndo=lookUndo.Count;
            yield return Click("LookRandomSettings");yield return Click("LookLockAll");yield return Click("LookRandomFromSettings");
            if(JsonUtility.ToJson(lookDraft)!=untouched||lookUndo.Count!=untouchedUndo||LookDirty())throw new Exception("First-use locked random marked an untouched appearance dirty");
            yield return Click("LookRandomSettings");yield return Click("LookUnlockAll");yield return Click("LookPopupBack");
            yield return Click("LookMale");
            var studio=GameObject.Find("AppearanceStudio");if(studio==null||!PaperSurface(studio.transform))throw new Exception("Portrait editor is not a paper surface");
            yield return CheckStudioCycle(0);yield return CheckStudioCycle(1);yield return CheckStudioCycle(7);
            yield return Click("LookGroup_1");
            foreach(int category in new[]{2,3,4,5,13,6})
            {
                StudioButton("LookCurrent_"+category);
                yield return CheckStudioCycle(category);
            }
            int shown=0;foreach(var button in root.GetComponentsInChildren<Button>())if(button.name.StartsWith("LookCurrent_"))shown++;
            if(shown!=6)throw new Exception("Portrait facial parts are not immediately accessible in six cycle rows");
            CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"facial-cycles.png"));
            yield return StudioSetOption(0,LookOptions(0).Length-1);if(LookIndex(0)!=LookOptions(0).Length-1)throw new Exception("Last face is not selectable");
            yield return StudioSetOption(0,0);
            string beforeSettings=JsonUtility.ToJson(lookDraft);yield return Click("LookRandomSettings");yield return Click("LookRandomLock_0");
            if(lookPopup!="random"||!lookLocks[0]||JsonUtility.ToJson(lookDraft)!=beforeSettings)throw new Exception("Random settings changed character instead of only preferences");
            CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();Capture(Path.ChangeExtension(path,"random-settings.png"));
            yield return Click("LookLockAll");foreach(int category in LookEditableCategories())if(!lookLocks[category])throw new Exception("Lock all omitted an editable component");
            string fullyLocked=JsonUtility.ToJson(lookDraft);int lockedUndoCount=lookUndo.Count;
            yield return Click("LookRandomFromSettings");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=fullyLocked||lookUndo.Count!=lockedUndoCount)throw new Exception("Fully locked random changed appearance or added an undo entry");
            yield return Click("LookRandomSettings");
            yield return Click("LookUnlockAll");foreach(int category in LookEditableCategories())if(lookLocks[category])throw new Exception("Unlock all retained an editable component");
            yield return Click("LookPopupBack");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=beforeSettings)throw new Exception("Returning from random settings mutated preview");
            yield return Click("LookRandomSettings");yield return Click("LookRandomFromSettings");if(lookPopup!="")throw new Exception("Random and view did not return to editor");
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=beforeSettings)throw new Exception("Random settings could not be undone exactly");
            yield return StudioSetOption(1,2);
            int hair=lookDraft.parts[1];yield return Click("LookLock_1");string before=JsonUtility.ToJson(lookDraft);yield return Click("LookRandom");
            if(lookDraft.parts[1]!=hair||lookDraft.sex!=0)throw new Exception("Locked random changed hair or sex");string after=JsonUtility.ToJson(lookDraft);
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=before)throw new Exception("Undo not exact");yield return Click("LookRedo");if(JsonUtility.ToJson(lookDraft)!=after)throw new Exception("Redo not exact");
            yield return StudioSetOption(1,1);if(lookDraft.parts[1]!=1||!lookLocks[1])throw new Exception("Random lock incorrectly prevented explicit hair change");
            yield return StudioSetOption(6,3);CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"male-full.png"));
            yield return Click("LookZoom");Capture(Path.ChangeExtension(path,"male-detail.png"));
            yield return Click("LookFemale");if(lookDraft.parts[6]!=0)throw new Exception("Female retains hidden beard");yield return StudioSetOption(1,1);
            yield return Click("LookGroup_1");
            foreach(int category in new[]{2,3,4,5,13})yield return CheckStudioCycle(category);
            foreach(var button in root.GetComponentsInChildren<Button>())if(button.name=="LookCurrent_6")throw new Exception("Female editor exposes a beard row");
            Capture(Path.ChangeExtension(path,"female-detail.png"));
            yield return Click("LookZoom");CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"female-full.png"));
            yield return Click("LookCompare");if(!lookCompare||StudioButton("LookApply").interactable)throw new Exception("Original comparison allows applying the wrong preview");yield return Click("LookCompare");
            yield return Click("LookPresets");CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();
            yield return Click("LookSaveSlot");if(profile.lookSlots[0]==null)throw new Exception("Preset not saved");Capture(Path.ChangeExtension(path,"presets.png"));
            string savedPreset=JsonUtility.ToJson(profile.lookSlots[0]);yield return Click("LookSaveSlot");if(lookPopup!="overwrite")throw new Exception("Overwrite has no confirmation");
            CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();Capture(Path.ChangeExtension(path,"preset-overwrite.png"));yield return Click("LookPopupKeep");
            if(JsonUtility.ToJson(profile.lookSlots[0])!=savedPreset)throw new Exception("Cancelling preset replacement modified its slot");
            yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(profile.lookSlots[0]))throw new Exception("Preset roundtrip changed look");
            yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");if(State.chiefLook.sex!=1)throw new Exception("Chief appearance not used");
            Capture(Path.ChangeExtension(path,"compact.png"));yield return Click("CommandOverview");Capture(Path.ChangeExtension(path,"overview.png"));yield return Click("OverviewClose");
            yield return SmokeOpenBusiness(2);yield return Click("CommandPeopleDetails");Capture(Path.ChangeExtension(path,"member.png"));yield return Click("CommandEditLook");string original=JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance);yield return Click("LookRandom");yield return Click("LookCancel");
            if(lookPopup!="discard")throw new Exception("Dirty preview closed without warning");CheckStudioPanel("LookPopup");yield return RequireStudioPopupBlocksEditor();Capture(Path.ChangeExtension(path,"discard.png"));
            string unsaved=JsonUtility.ToJson(lookDraft);yield return Click("LookPopupKeep");
            if(lookPopup!=""||JsonUtility.ToJson(lookDraft)!=unsaved||JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance)!=original)throw new Exception("Keep editing lost draft or changed the actor");
            yield return Click("LookCancel");yield return Click("LookPopupConfirm");
            if(modal!="memberDetails"||JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance)!=original)throw new Exception("Discard mutated actor or lost return location");
            yield return Click("MemberDetailsClose");yield return SmokeSaveLoad();
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_STUDIO_UI_SUCCESS: home/new-run/full-portrait captures, panel bounds and text, popup raycast isolation, all-option click cycles, bidirectional wraparound, independent parts, six visible facial rows, random settings, locked random/manual edits, undo/redo, gender/beard, zoom, comparison, preset confirmation/persistence, chief apply, keep/discard and save/load");
        }
    }
}
