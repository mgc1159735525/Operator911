using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class AppearanceCatalogVerification
    {
        public static void Verify()
        {
            int[] categories={2,3,4,5,13};
            foreach(int category in categories)
            {
                var names=AppearancePartCatalog.Names(category);var shapes=new HashSet<Vector4>();
                if(names.Length!=8||new HashSet<string>(names).Count!=names.Length)throw new Exception("Incomplete appearance catalog: "+category);
                for(int i=0;i<names.Length;i++)
                {
                    var shape=AppearancePartCatalog.Shape(category,i);
                    if(!shapes.Add(shape)||shape.x<.8f||shape.x>1.2f||shape.y<.8f||shape.y>1.2f||Mathf.Abs(shape.z)>.15f)
                        throw new Exception("Duplicate or unsafe feature shape: "+category+" / "+i);
                }
            }
            var seen=new HashSet<int>[5];for(int i=0;i<seen.Length;i++)seen[i]=new HashSet<int>();
            for(int seed=0;seed<400;seed++)
            {
                var a=AppearanceRules.Randomize(seed);AppearanceRules.Validate(a);
                for(int i=0;i<categories.Length;i++)seen[i].Add(categories[i]==13?a.ears:a.parts[categories[i]]);
                if(a.sex==1&&a.parts[6]!=0)throw new Exception("Female randomizer selected beard");
                if(JsonUtility.ToJson(a)!=JsonUtility.ToJson(AppearanceRules.Randomize(seed)))throw new Exception("Unstable appearance seed");
            }
            foreach(var options in seen)if(options.Count!=8)throw new Exception("New parts missing from randomized characters");
            var original=new Appearance{enabled=true,style=1,sex=0,portraitVersion=15,featureEditing=true,groomingEditing=true,ears=3};
            original.parts[2]=3;original.parts[3]=2;original.parts[4]=1;original.parts[5]=3;original.parts[6]=2;
            if(JsonUtility.ToJson(original)!=JsonUtility.ToJson(AppearanceRules.AsRealistic(original)))throw new Exception("Existing saved parts were remapped");
            var expanded=AppearanceRules.Copy(original);for(int i=2;i<=5;i++)expanded.parts[i]=7;expanded.ears=7;
            AppearanceRules.Validate(expanded);
            string path=Path.Combine(Application.temporaryCachePath,"minglu-catalog-roundtrip.json");
            try
            {
                var profile=new PlayerProfile{chiefLook=AppearanceRules.Copy(expanded)};profile.lookSlots[2]=AppearanceRules.Copy(expanded);
                RunLifecycle.WriteProfile(profile,path);var loaded=RunLifecycle.ReadProfile(path);
                if(JsonUtility.ToJson(loaded.lookSlots[2])!=JsonUtility.ToJson(expanded)||JsonUtility.ToJson(loaded.chiefLook)!=JsonUtility.ToJson(expanded))throw new Exception("Expanded preset roundtrip failed");
                var state=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);CommandRules.Initialize(state);state.chiefLook=AppearanceRules.Copy(expanded);state.agents[0].appearance=AppearanceRules.Copy(expanded);
                PrototypeSave.Write(state,path);var saved=PrototypeSave.Read(path);
                if(JsonUtility.ToJson(saved.chiefLook)!=JsonUtility.ToJson(expanded)||JsonUtility.ToJson(saved.agents[0].appearance)!=JsonUtility.ToJson(expanded))throw new Exception("Expanded game save roundtrip failed");
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
            expanded.ears=AppearancePartCatalog.Ears.Length;bool rejected=false;try{AppearanceRules.Validate(expanded);}catch(ArgumentException){rejected=true;}
            if(!rejected)throw new Exception("Out-of-range ear accepted");
            Debug.Log("MINGLU_APPEARANCE_CATALOG_VERIFY_SUCCESS: 40 unique feature shapes, 400 randomized portraits, stable old IDs, expanded presets and save roundtrips");
        }
    }
}
