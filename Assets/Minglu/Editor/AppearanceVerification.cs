using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class AppearanceVerification
    {
        static int passed;
        static void Check(bool ok,string label){if(!ok)throw new Exception("Appearance: "+label);passed++;}
        [MenuItem("Minglu/验证组合外观")]
        public static void Verify()
        {
            passed=0;var s=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);uint random=s.random;
            for(int i=0;i<300;i++)AppearanceRules.Validate(AppearanceRules.Randomize(i));
            Check(s.random==random,"Appearance generation does not consume simulation RNG");
            var look=AppearanceRules.Randomize(12);var clone=AppearanceRules.Copy(look);clone.parts[0]=(clone.parts[0]+1)%4;
            Check(look.parts[0]!=clone.parts[0],"Draft is independent");
            Check(JsonUtility.ToJson(AppearanceRules.Randomize(12))==JsonUtility.ToJson(look),"Seeded appearance is reproducible");
            var invalid=AppearanceRules.Copy(look);invalid.parts[2]=999;bool rejected=false;try{AppearanceRules.Validate(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"Out of range rejected");
            invalid.parts=new int[1];rejected=false;try{AppearanceRules.Validate(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"Wrong part count rejected");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-appearance-test.json");
            s.chiefLook=AppearanceRules.Copy(look);s.agents[1].appearance=AppearanceRules.Copy(look);PrototypeSave.Write(s,path);var saved=PrototypeSave.Read(path);
            Check(JsonUtility.ToJson(saved.agents[1].appearance)==JsonUtility.ToJson(look),"Member appearance roundtrip");
            Check(JsonUtility.ToJson(saved.chiefLook)==JsonUtility.ToJson(look),"Chief appearance roundtrip");
            Check(saved.agents[0].appearance==null||!saved.agents[0].appearance.enabled,"Original member portrait preserved");
            Check(saved.random==random&&saved.agents[1].intel==s.agents[1].intel,"Stats and simulation unchanged");
            var profile=new PlayerProfile{chiefLook=AppearanceRules.Copy(look)};profile.lookSlots[1]=AppearanceRules.Copy(look);RunLifecycle.WriteProfile(profile,path);profile=RunLifecycle.ReadProfile(path);
            Check(JsonUtility.ToJson(profile.lookSlots[1])==JsonUtility.ToJson(look),"Cross-run preset roundtrip");
            File.WriteAllText(path,"{\"version\":1,\"sound\":true,\"volume\":0.7,\"records\":[]}");profile=RunLifecycle.ReadProfile(path);Check(profile.lookSlots.Length==3,"Legacy profile loads");
            s.agents[1].appearance=new Appearance();PrototypeSave.Write(s,path);saved=PrototypeSave.Read(path);Check(!saved.agents[1].appearance.enabled,"Original portrait reset persisted");
            Debug.Log("MINGLU_APPEARANCE_VERIFY_SUCCESS: "+passed+" checks, 300 valid combinations");
        }
        public static void BatchDeliver(){try{Verify();LifecycleVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
