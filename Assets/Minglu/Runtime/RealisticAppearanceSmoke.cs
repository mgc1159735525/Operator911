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
                for(int head=0;head<4;head++)
                {
                    yield return Click("LookCategory_0");yield return Click("LookOption_"+head);
                    for(int outfit=0;outfit<4;outfit++){yield return Click("LookCategory_7");yield return Click("LookOption_"+outfit);}
                    yield return null;Capture(Path.ChangeExtension(path,"sex-"+sex+"-face-"+head+".png"));
                }
            }
            yield return Click("LookCategory_0");yield return Click("LookOption_1");yield return Click("LookCategory_7");yield return Click("LookOption_1");
            yield return Click("LookSaveSlot");yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(State.chiefLook.sex!=1||State.chiefLook.style!=1)throw new Exception("Chief sex/style not persisted");
            var original=State.candidates.Select(c=>JsonUtility.ToJson(c.appearance)).ToArray();
            if(!State.candidates.Any(c=>c.appearance.sex==0)||!State.candidates.Any(c=>c.appearance.sex==1))throw new Exception("Recruit pool lacks both sexes");
            yield return Click("Tab_4");yield return null;Capture(Path.ChangeExtension(path,"candidates.png"));
            yield return Click("Agent_0");yield return Click("Recruit_0");yield return Click("Agent_1");yield return Click("Recruit_1");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(State.agents.Count!=6||JsonUtility.ToJson(State.agents[4].appearance)!=original[0]||JsonUtility.ToJson(State.agents[5].appearance)!=original[1])throw new Exception("Recruited looks changed");
            yield return Click("Save");yield return Click("Load");
            var roster=root.GetComponentsInChildren<ScrollRect>().First(x=>x.name=="Roster");roster.verticalNormalizedPosition=0;yield return null;
            yield return Click("Agent_5");yield return Click("AgentDetails");yield return null;Capture(Path.ChangeExtension(path,"recruited-woman.png"));
            yield return Click("AgentAppearance");yield return Click("LookRandom");
            if(lookDraft.sex!=1)throw new Exception("Randomize changed selected sex");
            yield return Click("LookCancel");yield return Click("CloseModal");
            if(JsonUtility.ToJson(State.agents[5].appearance)!=original[1])throw new Exception("Reload or canceled editing changed recruit");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_REALISTIC_UI_SUCCESS: 32 gender/head/outfit combinations, female chief, both recruit sexes, preview-to-hire consistency, save/load and cancel");
        }
    }
}
