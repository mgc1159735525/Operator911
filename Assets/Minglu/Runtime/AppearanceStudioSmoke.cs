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
            if(lookOptionPage>0)yield return Click("LookOptionPrevious");
            string initial=JsonUtility.ToJson(lookDraft);yield return Click("LookOptionNext");
            if(lookOptionPage!=1||JsonUtility.ToJson(lookDraft)!=initial)throw new Exception("Browsing portrait choices changed the preview");
            int shown=0;foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.name.StartsWith("LookOption_"))shown++;
            if(shown!=4)throw new Exception("Portrait selector did not limit visible alternatives to four");
            yield return Click("LookOption_7");if(lookDraft.parts[0]!=7)throw new Exception("Second face page is not selectable");
            yield return Click("LookOptionPrevious");yield return Click("LookOption_0");
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
            yield return Click("LookCategory_1");yield return Click("LookOption_2");
            int hair=lookDraft.parts[1];yield return Click("LookLock");string before=JsonUtility.ToJson(lookDraft);yield return Click("LookRandom");
            if(lookDraft.parts[1]!=hair||lookDraft.sex!=0)throw new Exception("Locked random changed hair or sex");string after=JsonUtility.ToJson(lookDraft);
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=before)throw new Exception("Undo not exact");yield return Click("LookRedo");if(JsonUtility.ToJson(lookDraft)!=after)throw new Exception("Redo not exact");
            yield return Click("LookGroup_1");yield return Click("LookCategory_6");yield return Click("LookOption_3");CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"male-full.png"));
            yield return Click("LookZoom");Capture(Path.ChangeExtension(path,"male-detail.png"));
            yield return Click("LookFemale");if(lookDraft.parts[6]!=0)throw new Exception("Female retains hidden beard");yield return Click("LookCategory_1");yield return Click("LookOption_1");Capture(Path.ChangeExtension(path,"female-detail.png"));
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
            Debug.Log("MINGLU_STUDIO_UI_SUCCESS: home/new-run/full-portrait captures, panel bounds and text, popup raycast isolation, four-choice pagination, random settings, locked random, undo/redo, gender/beard, zoom, comparison, preset confirmation/persistence, chief apply, keep/discard and save/load");
        }
    }
}
