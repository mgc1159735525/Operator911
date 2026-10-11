using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class GroomingAgeVerification
    {
        static int checks;
        static void Require(bool condition,string message){checks++;if(!condition)throw new Exception("Grooming age: "+message);}
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
                    int minimum=new[]{22,32,44,58}[AppearanceRules.AgeBand(member.appearance.parts[0])];
                    Require(person.age>=minimum&&person.age<minimum+10,"member age still uses the retired four-face modulo");
                    if(AppearanceRules.IsElderFace(member.appearance.parts[0]))
                    {
                        startingElders++;if(member.appearance.parts[12]==3)startingWhite++;
                        Require(member.appearance.parts[12]>=2&&person.age>=58,"starting elder lacks matching age and grey/white hair");
                    }
                }
                uint random=state.random;var preview=CommandRules.PreviewRecruit(state);
                Require(preview!=null&&state.random==random,"recruit preview consumes simulation RNG or cannot be created");
                Require(CommandRules.Recruit(state)=="","valid recruitment failed");
                var recruit=state.agents[state.agents.Count-1];AppearanceRules.Validate(recruit.appearance);var personAfter=CommandRules.Person(state,recruit.id);
                Require(JsonUtility.ToJson(recruit.appearance)==JsonUtility.ToJson(preview.agent.appearance),"recruitment changed the previewed appearance");
                Require(JsonUtility.ToJson(personAfter.originalLook)==JsonUtility.ToJson(recruit.appearance),"recruit original look diverged from the joined member");
                int ageBase=new[]{22,32,44,58}[AppearanceRules.AgeBand(recruit.appearance.parts[0])];
                Require(personAfter.age>=ageBase&&personAfter.age<ageBase+10,"recruit age does not match the complete head");
                if(AppearanceRules.IsElderFace(recruit.appearance.parts[0]))
                {
                    recruitedElders++;if(recruit.appearance.parts[12]==3)recruitedWhite++;
                    Require(recruit.appearance.parts[12]>=2,"recruited elder has dark hair");
                }
            }
            Require(startingElders>200&&startingWhite/(float)startingElders>.75f,"actual starting crews lost the elder white preference");
            Require(recruitedElders>30&&recruitedWhite/(float)recruitedElders>.75f,"actual recruits lost the elder white preference");
        }
        public static void Verify()
        {
            checks=0;
            Require(AppearanceRules.HeadCount==12&&AppearanceRules.MaleHeadNames.Length==12&&AppearanceRules.FemaleHeadNames.Length==12&&AppearanceRules.BeardNames.Length==8,"new head/beard catalogs are incomplete");
            foreach(var names in new[]{AppearanceRules.MaleHeadNames,AppearanceRules.FemaleHeadNames,AppearanceRules.BeardNames})Require(new HashSet<string>(names).Count==names.Length,"duplicate appearance names");
            for(int head=0;head<12;head++)Require(AppearanceRules.IsElderFace(head)==(head>=9)&&AppearanceRules.AgeBand(head)==head/3,"head age grouping is incorrect");
            var headSeen=new[]{new HashSet<int>(),new HashSet<int>()};var beardSeen=new HashSet<int>();int elders=0,white=0,grey=0;
            for(int sex=0;sex<2;sex++)for(int seed=0;seed<1000;seed++)
            {
                var a=AppearanceRules.Randomize(seed,sex);AppearanceRules.Validate(a);headSeen[sex].Add(a.parts[0]);
                Require(a.parts[1]==0&&a.ears==0,"randomization recreated retired independent grooming");
                if(sex==0)beardSeen.Add(a.parts[6]);else Require(a.parts[6]==0,"female random has a beard");
                if(AppearanceRules.IsElderFace(a.parts[0]))
                {
                    elders++;if(a.parts[12]==3)white++;else if(a.parts[12]==2)grey++;else throw new Exception("Grooming age: elder generated with dark hair");
                }
                Require(JsonUtility.ToJson(a)==JsonUtility.ToJson(AppearanceRules.Randomize(seed,sex)),"randomization is not reproducible");
            }
            Require(headSeen[0].Count==12&&headSeen[1].Count==12&&beardSeen.Count==8,"new options absent from random characters");
            Require(elders>400&&white/(float)elders>.80f&&white/(float)elders<.90f&&grey>0,"elder white preference must remain approximately 85 percent with grey variety");
            foreach(int face in new[]{9,10,11})for(int sex=0;sex<2;sex++)
            {
                var manual=AppearanceRules.Randomize(42,sex,face);manual.parts[12]=0;
                AppearanceRules.SelectFace(manual,face,true);Require(manual.parts[12]==0,"locked color replaced by head selection");
                AppearanceRules.SelectFace(manual,face,false);Require(manual.parts[12]==3,"elder selection did not suggest white");
                manual.parts[12]=1;AppearanceRules.ApplyAgeHairColor(manual);Require(manual.parts[12]==3,"age-color action did not explicitly restore white");
                manual.parts[12]=0;string before=JsonUtility.ToJson(manual);
                Require(JsonUtility.ToJson(AppearanceRules.AsRealistic(manual))==before,"reading an existing manual elder recolored it");
                for(int seed=0;seed<100;seed++)
                {
                    var lockedHead=AppearanceRules.Randomize(seed,sex,face);
                    Require(lockedHead.parts[0]==face&&lockedHead.parts[12]>=2,"random color was based on a discarded random head instead of the locked elder");
                }
            }
            var young=AppearanceRules.Randomize(30,0,0);young.parts[12]=1;AppearanceRules.SelectFace(young,1);Require(young.parts[12]==1,"nonelder head change replaced manual palette");
            AppearanceRules.ApplyAgeHairColor(young);Require(young.parts[12]==0,"young age-color action must use black");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-grooming-v20-test.json");
            try
            {
                var look=AppearanceRules.Randomize(91,0,11);look.parts[6]=7;look.parts[12]=0;
                var profile=new PlayerProfile{chiefLook=AppearanceRules.Copy(look)};profile.lookSlots[1]=AppearanceRules.Copy(look);
                RunLifecycle.WriteProfile(profile,path);var saved=RunLifecycle.ReadProfile(path);
                Require(JsonUtility.ToJson(saved.chiefLook)==JsonUtility.ToJson(look)&&JsonUtility.ToJson(saved.lookSlots[1])==JsonUtility.ToJson(look),"manual elder color lost in profile/preset roundtrip");
                var state=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);CommandRules.Initialize(state);state.chiefLook=AppearanceRules.Copy(look);state.agents[0].appearance=AppearanceRules.Copy(look);
                PrototypeSave.Write(state,path);var loaded=PrototypeSave.Read(path);
                Require(JsonUtility.ToJson(loaded.chiefLook)==JsonUtility.ToJson(look)&&JsonUtility.ToJson(loaded.agents[0].appearance)==JsonUtility.ToJson(look),"new head or manual elder color lost in game roundtrip");
                foreach(int category in new[]{0,1,6})
                {
                    var invalid=AppearanceRules.Copy(look);invalid.parts[category]=AppearanceRules.Names[category].Length;string validBytes=File.ReadAllText(path);state.chiefLook=invalid;bool rejected=false;
                    try{PrototypeSave.Write(state,path);}catch(ArgumentException){rejected=true;}
                    Require(rejected&&File.ReadAllText(path)==validBytes,"invalid head/grooming overwrote the last valid save: "+category);
                }
                state.chiefLook=null;PrototypeSave.Write(state,path);var legacy=PrototypeSave.Read(path);AppearanceRules.Validate(legacy.chiefLook);
                Require(legacy.chiefLook==null||!legacy.chiefLook.enabled,"null legacy appearance was unexpectedly enabled");
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
            VerifyMemberGeneration();
            Debug.Log("MINGLU_GROOMING_AGE_VERIFY_SUCCESS: "+checks+" checks; 24 complete heads; 7 beards plus none; 2,000 seeded portraits; 256 actual starting crews and recruit previews/joins; head age and elder white preference; protected manual/locked palettes and save boundaries");
        }
    }
}
