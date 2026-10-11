using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class AppearanceCatalogVerification
    {
        static int checks;
        static void Check(bool value,string label){checks++;if(!value)throw new Exception("Appearance catalog: "+label);}
        public static void Verify()
        {
            checks=0;int[] categories={2,3,4,5};
            foreach(int category in categories)
            {
                var names=AppearancePartCatalog.Names(category);
                Check(names.Length==8&&new HashSet<string>(names).Count==8,"Feature sprite options are incomplete or duplicated: "+category);
                Check(ReferenceEquals(names,AppearanceRules.Names[category]),"UI and persisted sprite catalogs differ");
            }
            bool rejected=false;try{AppearancePartCatalog.Names(13);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"Retired ear catalog is still exposed");
            Check(AppearanceRules.Names[1].Length==1,"Independent hair options remain in the new catalog");
            foreach(int sex in new[]{0,1})Check(AppearanceRules.HeadNames(sex).Length==12&&new HashSet<string>(AppearanceRules.HeadNames(sex)).Count==12,"Full head catalog is incomplete");
            var seen=new HashSet<int>[4];for(int i=0;i<seen.Length;i++)seen[i]=new HashSet<int>();
            var heads=new[]{new HashSet<int>(),new HashSet<int>()};
            for(int sex=0;sex<2;sex++)for(int seed=0;seed<400;seed++)
            {
                var a=AppearanceRules.Randomize(seed,sex);AppearanceRules.Validate(a);heads[sex].Add(a.parts[0]);
                for(int i=0;i<categories.Length;i++)seen[i].Add(a.parts[categories[i]]);
                Check(a.portraitVersion==20&&a.parts[1]==0&&a.ears==0,"Random portrait retained independent hair or ears");
                Check(sex==0||a.parts[6]==0,"Female randomizer selected beard");
                Check(JsonUtility.ToJson(a)==JsonUtility.ToJson(AppearanceRules.Randomize(seed,sex)),"Unstable appearance seed");
            }
            foreach(var options in seen)Check(options.Count==8,"Feature sprites are absent from random characters");
            Check(heads[0].Count==12&&heads[1].Count==12,"A complete head is absent from random characters");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-catalog-v20-roundtrip.json");
            try
            {
                foreach(bool legacy in new[]{true,false})
                {
                    var look=legacy?new Appearance{enabled=true,style=1,sex=0,portraitVersion=15,featureEditing=true,groomingEditing=true,ears=7}:AppearanceRules.Randomize(24,0,11);
                    look.parts[0]=legacy?7:11;look.parts[1]=legacy?7:0;
                    for(int i=2;i<=7;i++)look.parts[i]=7;look.parts[8]=3;look.parts[12]=0;
                    string original=JsonUtility.ToJson(look);
                    var profile=new PlayerProfile{chiefLook=AppearanceRules.Copy(look)};profile.lookSlots[2]=AppearanceRules.Copy(look);
                    RunLifecycle.WriteProfile(profile,path);string disk=File.ReadAllText(path);var loaded=RunLifecycle.ReadProfile(path);
                    Check(JsonUtility.ToJson(loaded.lookSlots[2])==original&&JsonUtility.ToJson(loaded.chiefLook)==original,"Profile read remapped saved selections");
                    var display=AppearanceRules.AsRealistic(loaded.chiefLook);Check(display.portraitVersion==20&&display.parts[12]==0,"Display lost the manual elder palette");
                    Check(File.ReadAllText(path)==disk&&JsonUtility.ToJson(loaded.chiefLook)==original,"Displaying or reading a preset wrote back its migration");
                    var state=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);CommandRules.Initialize(state);state.chiefLook=AppearanceRules.Copy(look);state.agents[0].appearance=AppearanceRules.Copy(look);
                    PrototypeSave.Write(state,path);disk=File.ReadAllText(path);var saved=PrototypeSave.Read(path);uint random=saved.random;
                    Check(JsonUtility.ToJson(saved.chiefLook)==original&&JsonUtility.ToJson(saved.agents[0].appearance)==original,"Game read remapped saved appearance data");
                    AppearanceRules.AsRealistic(saved.agents[0].appearance);
                    Check(saved.random==random&&File.ReadAllText(path)==disk&&JsonUtility.ToJson(saved.agents[0].appearance)==original,"Rendering migration changed simulation or disk data");
                }
                File.WriteAllText(path,"{\"version\":1,\"sound\":true,\"volume\":0.7,\"records\":[]}");var oldProfile=RunLifecycle.ReadProfile(path);
                Check(oldProfile.lookSlots.Length==3,"Profile without appearance slots no longer loads");AppearanceRules.Validate(oldProfile.chiefLook);
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
            Debug.Log("MINGLU_APPEARANCE_CATALOG_VERIFY_SUCCESS: "+checks+" checks; 32 sprite choices; 24 complete heads; 800 seeded portraits; untouched old/new game and profile files after read/display. Asset rendering is verified separately.");
        }
    }
}
