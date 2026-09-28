using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class RealisticAppearanceVerification
    {
        static int count;
        static void Check(bool ok,string name){if(!ok)throw new Exception("Realistic appearance: "+name);count++;}
        public static void Verify()
        {
            count=0;for(uint seed=1;seed<=100;seed++)
            {
                var s=RunLifecycle.Create(ConfigStore.Load(),seed,1,0,false);uint rng=s.random;
                if(!s.candidates.Any(c=>c.appearance.sex==0)||!s.candidates.Any(c=>c.appearance.sex==1))throw new Exception("Unbalanced recruit sex pool");
                foreach(var c in s.candidates){AppearanceRules.Validate(c.appearance);if(JsonUtility.ToJson(c.appearance)!=JsonUtility.ToJson(AppearanceRules.ForRecruit(seed,c.id)))throw new Exception("Unstable appearance");}
                if(s.random!=rng)throw new Exception("Appearance changes world RNG");
            }
            Check(true,"100 seeded pools have both sexes and stable appearances");
            var run=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);string expected=JsonUtility.ToJson(run.candidates[1].appearance);
            Check(OrganizationRules.Assign(run,0,OrganizationDuty.Recruit,0,1)=="","Recruit assigned");PrototypeRules.Resolve(run);if(!string.IsNullOrEmpty(run.pendingEvent))PrototypeRules.ChooseEvent(run,false);
            Check(JsonUtility.ToJson(run.agents.Last().appearance)==expected&&run.agents.Last().appearance.sex==1,"Female recruit preserves preview");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-realistic-test.json");PrototypeSave.Write(run,path);run=PrototypeSave.Read(path);
            Check(JsonUtility.ToJson(run.agents.Last().appearance)==expected,"Recruit survives save/load");
            var bad=AppearanceRules.Randomize(7);bad.sex=2;bool rejected=false;try{AppearanceRules.Validate(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"Invalid sex rejected");
            var legacy=JsonUtility.FromJson<Appearance>("{\"enabled\":true,\"parts\":[0,0,0,0,0,0,0,0,0,0,0,0,0]}");Check(legacy.style==0,"Legacy custom portrait remains classic");
            Check(Resources.Load<Texture2D>("Minglu/Art/realistic-atlas-v10")!=null,"Atlas available");
            Debug.Log("MINGLU_REALISTIC_VERIFY_SUCCESS: "+count+" checks, 100 seeded recruitment pools");
        }
        public static void BatchDeliver(){try{Verify();AppearanceVerification.Verify();OrganizationVerification.Verify();LifecycleVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
