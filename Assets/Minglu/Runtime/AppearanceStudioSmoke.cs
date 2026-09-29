using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator AppearanceStudioSmoke(string path)
        {
            yield return Click("HomeAppearance");yield return Click("LookMale");yield return Click("LookCategory_1");yield return Click("LookOption_2");
            int hair=lookDraft.parts[1];yield return Click("LookLock");string before=JsonUtility.ToJson(lookDraft);yield return Click("LookRandom");
            if(lookDraft.parts[1]!=hair||lookDraft.sex!=0)throw new Exception("Locked random changed hair or sex");string after=JsonUtility.ToJson(lookDraft);
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=before)throw new Exception("Undo not exact");yield return Click("LookRedo");if(JsonUtility.ToJson(lookDraft)!=after)throw new Exception("Redo not exact");
            yield return Click("LookGroup_1");yield return Click("LookCategory_6");yield return Click("LookOption_3");yield return Click("LookZoom");Capture(Path.ChangeExtension(path,"male-detail.png"));
            yield return Click("LookFemale");if(lookDraft.parts[6]!=0)throw new Exception("Female retains hidden beard");yield return Click("LookCategory_1");yield return Click("LookOption_1");Capture(Path.ChangeExtension(path,"female-detail.png"));
            yield return Click("LookCompare");if(!lookCompare)throw new Exception("Comparison not enabled");yield return Click("LookCompare");
            yield return Click("LookPresets");yield return Click("LookSaveSlot");if(profile.lookSlots[0]==null)throw new Exception("Preset not saved");Capture(Path.ChangeExtension(path,"presets.png"));
            yield return Click("LookSaveSlot");if(lookPopup!="overwrite")throw new Exception("Overwrite has no confirmation");yield return Click("LookPopupKeep");
            yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(profile.lookSlots[0]))throw new Exception("Preset roundtrip changed look");
            yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");if(State.chiefLook.sex!=1)throw new Exception("Chief appearance not used");
            Capture(Path.ChangeExtension(path,"compact.png"));yield return Click("CommandOverview");Capture(Path.ChangeExtension(path,"overview.png"));yield return Click("OverviewClose");
            yield return Click("CommandMemberOpen");Capture(Path.ChangeExtension(path,"member.png"));yield return Click("CommandEditLook");string original=JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance);yield return Click("LookRandom");yield return Click("LookCancel");
            if(lookPopup!="discard")throw new Exception("Dirty preview closed without warning");yield return Click("LookPopupConfirm");
            if(modal!="memberDetails"||JsonUtility.ToJson(State.agents.Find(a=>a.id==lookTarget).appearance)!=original)throw new Exception("Discard mutated actor or lost return location");
            yield return Click("MemberDetailsClose");yield return Click("Save");yield return Click("Load");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_STUDIO_UI_SUCCESS: locked random, undo/redo, gender/beard, zoom, comparison, preset confirmation/persistence, chief apply, compact dialogs, discard and save/load");
        }
    }
}
