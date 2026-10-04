using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator RealisticAppearanceSmoke(string path)
        {
            yield return Click("HomeAppearance");
            for(int sex=0;sex<2;sex++)
            {
                yield return Click(sex==0?"LookMale":"LookFemale");
                for(int head=0;head<LookOptions(0).Length;head++)
                {
                    yield return StudioSetOption(0,head);
                    for(int outfit=0;outfit<LookOptions(7).Length;outfit++)yield return StudioSetOption(7,outfit);
                    yield return null;Capture(Path.ChangeExtension(path,"sex-"+sex+"-face-"+head+".png"));
                }
            }
            yield return StudioSetOption(0,1);yield return StudioSetOption(7,1);
            yield return Click("LookPresets");yield return Click("LookSaveSlot");yield return Click("LookPopupBack");yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(State.chiefLook.sex!=1||State.chiefLook.style!=1)throw new Exception("Chief sex/style not persisted");
            if(!State.agents.Any(a=>a.appearance.sex==0)||!State.agents.Any(a=>a.appearance.sex==1))throw new Exception("Opening team lacks both sexes");
            yield return SmokeOpenBusiness(2);int initialCount=State.agents.Count;var original=new string[2];
            for(int i=0;i<2;i++)
            {
                yield return Click("CommandRecruit");original[i]=JsonUtility.ToJson(recruitPreview.agent.appearance);
                Capture(Path.ChangeExtension(path,"candidate-"+i+".png"));yield return Click("RecruitConfirm");
                if(State.agents.Count!=initialCount+i+1||JsonUtility.ToJson(State.agents.Last().appearance)!=original[i])throw new Exception("Recruited look differs from its preview");
            }
            yield return SmokeSaveLoad();
            if(JsonUtility.ToJson(State.agents[initialCount].appearance)!=original[0]||JsonUtility.ToJson(State.agents[initialCount+1].appearance)!=original[1])throw new Exception("Recruited looks changed on reload");
            int recruitId=State.agents.Last().id,recruitSex=State.agents.Last().appearance.sex;
            yield return SmokeOpenBusiness(2);yield return Click("CommandAgent_"+recruitId);yield return Click("CommandPeopleDetails");Capture(Path.ChangeExtension(path,"recruited-member.png"));
            yield return Click("CommandEditLook");yield return Click("LookRandom");
            if(lookDraft.sex!=recruitSex)throw new Exception("Randomize changed selected sex");
            yield return Click("LookCancel");if(lookPopup=="discard")yield return Click("LookPopupConfirm");yield return Click("MemberDetailsClose");
            if(JsonUtility.ToJson(State.agents.Last().appearance)!=original[1])throw new Exception("Canceled editing changed recruit");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_REALISTIC_UI_SUCCESS: all gender/head/outfit selections through cycle controls, female chief, random team sexes, preview-to-hire consistency, save/load and cancel");
        }
    }
}
