using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class GroomingAgeVerification
    {
        static void Require(bool condition,string message){if(!condition)throw new Exception("Grooming age: "+message);}
        static void VerifyMemberGeneration()
        {
            var config=ConfigStore.Load();int startingElders=0,startingWhite=0,recruitedElders=0,recruitedWhite=0;
            for(uint seed=1;seed<=256;seed++)
            {
                var state=RunLifecycle.Create(config,seed,1,0,false);CommandRules.Initialize(state);
                foreach(var member in state.agents)
                {
                    AppearanceRules.Validate(member.appearance);var person=CommandRules.Person(state,member.id);
                    Require(JsonUtility.ToJson(person.originalLook)==JsonUtility.ToJson(member.appearance),"member original look diverged from generated appearance");
                    if(AppearanceRules.IsElderFace(member.appearance.parts[0]))
                    {
                        startingElders++;if(member.appearance.parts[12]==3)startingWhite++;
                        Require(member.appearance.parts[12]>=2&&person.age>=58,"starting elder does not have age-matched grey or white hair");
                    }
                }
                uint random=state.random;var preview=CommandRules.PreviewRecruit(state);
                Require(preview!=null&&state.random==random,"recruit preview consumes simulation RNG or cannot be created");
                Require(CommandRules.Recruit(state)=="","valid recruitment failed");
                var recruit=state.agents[state.agents.Count-1];AppearanceRules.Validate(recruit.appearance);
                Require(JsonUtility.ToJson(recruit.appearance)==JsonUtility.ToJson(preview.agent.appearance),"recruitment changed the previewed grooming");
                Require(JsonUtility.ToJson(CommandRules.Person(state,recruit.id).originalLook)==JsonUtility.ToJson(recruit.appearance),"recruit original look diverged from generated appearance");
                if(AppearanceRules.IsElderFace(recruit.appearance.parts[0]))
                {
                    recruitedElders++;if(recruit.appearance.parts[12]==3)recruitedWhite++;
                    Require(recruit.appearance.parts[12]>=2,"recruited elder has dark hair");
                }
            }
            Require(startingElders>200&&startingWhite/(float)startingElders>.75f,"actual starting members lost the elder white preference");
            Require(recruitedElders>30&&recruitedWhite/(float)recruitedElders>.75f,"actual recruited members lost the elder white preference");
        }
        public static void Verify()
        {
            Require(AppearanceRules.MaleHairNames.Length==8&&AppearanceRules.FemaleHairNames.Length==8&&AppearanceRules.BeardNames.Length==8,"expanded catalogs are incomplete");
            foreach(var names in new[]{AppearanceRules.MaleHairNames,AppearanceRules.FemaleHairNames,AppearanceRules.BeardNames})
                Require(new HashSet<string>(names).Count==names.Length,"duplicate grooming names");
            Require(AppearanceRules.MaleHairNames[3]=="巾帽"&&AppearanceRules.FemaleHairNames[3]=="半束"&&AppearanceRules.BeardNames[3]=="八字须","existing choice IDs changed meaning");

            var hairSeen=new HashSet<int>[2]{new HashSet<int>(),new HashSet<int>()};var beardSeen=new HashSet<int>();
            int elders=0,white=0,grey=0;
            for(int sex=0;sex<2;sex++)for(int seed=0;seed<1000;seed++)
            {
                var a=AppearanceRules.Randomize(seed,sex);AppearanceRules.Validate(a);hairSeen[sex].Add(a.parts[1]);
                if(sex==0)beardSeen.Add(a.parts[6]);else Require(a.parts[6]==0,"female random has a beard");
                if(AppearanceRules.IsElderFace(a.parts[0]))
                {
                    elders++;if(a.parts[12]==3)white++;else if(a.parts[12]==2)grey++;else throw new Exception("Grooming age: elder generated with dark hair");
                }
                Require(JsonUtility.ToJson(a)==JsonUtility.ToJson(AppearanceRules.Randomize(seed,sex)),"randomization is not reproducible");
            }
            Require(hairSeen[0].Count==8&&hairSeen[1].Count==8&&beardSeen.Count==8,"new parts absent from random characters");
            Require(elders>400&&white/(float)elders>.80f&&white/(float)elders<.90f&&grey>0,"elder white preference must remain approximately 85 percent with grey variety");
            foreach(int face in new[]{3,7})for(int sex=0;sex<2;sex++)
            {
                var manual=AppearanceRules.Randomize(42,sex,face);manual.parts[12]=0;
                AppearanceRules.SelectFace(manual,face,true);Require(manual.parts[12]==0,"locked color replaced by face selection");
                AppearanceRules.SelectFace(manual,face,false);Require(manual.parts[12]==3,"elder face selection did not suggest white");
                manual.parts[12]=1;AppearanceRules.ApplyAgeHairColor(manual);Require(manual.parts[12]==3,"age-color action did not explicitly restore white");
                manual.parts[12]=0;string before=JsonUtility.ToJson(manual);
                Require(JsonUtility.ToJson(AppearanceRules.AsRealistic(manual))==before,"reading an existing manual elder recolored it");
                for(int seed=0;seed<100;seed++)
                {
                    var lockedFace=AppearanceRules.Randomize(seed,sex,face);
                    Require(lockedFace.parts[0]==face&&lockedFace.parts[12]>=2,"locked face random color was based on discarded random face");
                }
            }
            var young=AppearanceRules.Randomize(30,0,0);young.parts[12]=1;AppearanceRules.SelectFace(young,1);Require(young.parts[12]==1,"nonelder face change replaced manual palette");
            AppearanceRules.ApplyAgeHairColor(young);Require(young.parts[12]==0,"young age-color action must use black");

            string path=Path.Combine(Application.temporaryCachePath,"minglu-grooming-age-test.json");
            try
            {
                var look=AppearanceRules.Randomize(91,0,7);look.parts[1]=7;look.parts[6]=7;look.parts[12]=0;
                var profile=new PlayerProfile{chiefLook=AppearanceRules.Copy(look)};profile.lookSlots[1]=AppearanceRules.Copy(look);
                RunLifecycle.WriteProfile(profile,path);var saved=RunLifecycle.ReadProfile(path);
                Require(JsonUtility.ToJson(saved.chiefLook)==JsonUtility.ToJson(look)&&JsonUtility.ToJson(saved.lookSlots[1])==JsonUtility.ToJson(look),"new grooming indexes or manual elder color lost in profile/preset roundtrip");
                var state=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);CommandRules.Initialize(state);state.chiefLook=AppearanceRules.Copy(look);state.agents[0].appearance=AppearanceRules.Copy(look);
                PrototypeSave.Write(state,path);var loaded=PrototypeSave.Read(path);
                Require(JsonUtility.ToJson(loaded.chiefLook)==JsonUtility.ToJson(look)&&JsonUtility.ToJson(loaded.agents[0].appearance)==JsonUtility.ToJson(look),"new grooming indexes or manual elder color lost in game roundtrip");
                var old=AppearanceRules.Copy(look);old.parts[1]=3;old.parts[6]=3;AppearanceRules.Validate(old);
                Require(JsonUtility.ToJson(AppearanceRules.AsRealistic(old))==JsonUtility.ToJson(old),"old grooming options changed during migration");
                foreach(int category in new[]{1,6})
                {
                    var invalid=AppearanceRules.Copy(look);invalid.parts[category]=8;bool rejected=false;try{AppearanceRules.Validate(invalid);}catch(ArgumentException){rejected=true;}
                    Require(rejected,"out-of-range grooming index accepted");
                    state.chiefLook=invalid;PrototypeSave.Write(state,path);rejected=false;
                    try{PrototypeSave.Read(path);}catch(ArgumentException){rejected=true;}
                    Require(rejected,"command save accepted an out-of-range chief grooming index: "+category);
                }
                state.chiefLook=null;PrototypeSave.Write(state,path);var legacy=PrototypeSave.Read(path);
                // JsonUtility may materialize an inline serializable null class
                // as its default object. Compatibility means safe loading and a
                // valid disabled appearance, not preservation of reference null.
                AppearanceRules.Validate(legacy.chiefLook);
                Require(legacy.chiefLook==null||!legacy.chiefLook.enabled,"legacy command save unexpectedly enabled a custom chief appearance");
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
            VerifyMemberGeneration();
            Debug.Log("MINGLU_GROOMING_AGE_VERIFY_SUCCESS: 16 hairstyles, 7 beards plus none; 2,000 seeded random portraits; 256 actual starting crews and recruit previews/joins; elder white preference; manual/locked palettes and old/new save roundtrips");
        }
    }
}
