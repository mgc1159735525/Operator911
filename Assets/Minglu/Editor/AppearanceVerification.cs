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
        static void Reject(Action action,string label)
        {bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,label);}
        static void VerifyMigration()
        {
            foreach(int version in new[]{0,1,12,14,15,19})for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)for(int hair=0;hair<8;hair++)
            {
                var old=new Appearance{enabled=true,style=version%2,sex=sex,ears=7,portraitVersion=version};
                old.parts=new[]{face,hair,7,6,5,4,7,7,3,3,3,5,1};string before=JsonUtility.ToJson(old);
                AppearanceRules.Validate(old);Check(JsonUtility.ToJson(old)==before,"Legacy validation changed data");
                var next=AppearanceRules.AsRealistic(old);AppearanceRules.Validate(next);
                Check(next.portraitVersion==20&&next.style==1&&next.parts[1]==0&&next.ears==0,"Legacy format did not normalize its retired slots");
                Check(AppearanceRules.AgeBand(next.parts[0])==face%4,"Migration changed the old face's age band");
                for(int category=2;category<13;category++)Check(next.parts[category]==(category==6&&sex==1?0:old.parts[category]),"Migration discarded a valid palette or sprite successor");
                Check(JsonUtility.ToJson(old)==before&&!ReferenceEquals(old.parts,next.parts),"Drawing migration mutated its input");
                Check(JsonUtility.ToJson(AppearanceRules.AsRealistic(old))==JsonUtility.ToJson(next),"Legacy head mapping is not deterministic");
                Check(JsonUtility.ToJson(AppearanceRules.AsRealistic(next))==JsonUtility.ToJson(next),"Second migration changed an upgraded portrait");
            }
            Check(AppearanceRules.LegacyHead(0,3,3)==11&&AppearanceRules.LegacyHead(0,0,2)==1,"Male hat/loose-hair migration lost its closest full head");
            Check(AppearanceRules.LegacyHead(1,0,2)==2&&AppearanceRules.LegacyHead(1,1,5)==3&&AppearanceRules.LegacyHead(1,7,7)==11,"Female side/braided/elder head migration is incorrect");
            var disabled=AppearanceRules.AsRealistic(null);Check(!disabled.enabled&&disabled.portraitVersion==20,"Null legacy portrait was enabled by a read");
            AppearanceRules.Validate(new Appearance());AppearanceRules.Validate(null);
            var corrupt=new Appearance();corrupt.parts[0]=8;Reject(()=>AppearanceRules.AsRealistic(corrupt),"Invalid legacy face was silently clamped");
        }
        static void VerifyCurrentValidation(Appearance look)
        {
            for(int category=0;category<13;category++)foreach(int invalidIndex in new[]{-1,AppearanceRules.Names[category].Length})
            {
                var invalid=AppearanceRules.Copy(look);invalid.sex=0;invalid.parts[category]=invalidIndex;
                Reject(()=>AppearanceRules.Validate(invalid),"Current invalid part accepted: "+category);
                Reject(()=>AppearanceRules.AsRealistic(invalid),"Current invalid part repaired silently: "+category);
            }
            var invalidLook=AppearanceRules.Copy(look);invalidLook.parts=null;Reject(()=>AppearanceRules.Validate(invalidLook),"Null parts accepted");
            invalidLook.parts=new int[12];Reject(()=>AppearanceRules.Validate(invalidLook),"Wrong part count accepted");
            invalidLook=AppearanceRules.Copy(look);invalidLook.ears=1;Reject(()=>AppearanceRules.Validate(invalidLook),"Retired ear selection accepted in a new portrait");
            invalidLook=AppearanceRules.Copy(look);invalidLook.sex=1;invalidLook.parts[6]=1;Reject(()=>AppearanceRules.Validate(invalidLook),"Invisible female beard persisted");
            foreach(int version in new[]{-1,21}){invalidLook=AppearanceRules.Copy(look);invalidLook.portraitVersion=version;Reject(()=>AppearanceRules.Validate(invalidLook),"Invalid portrait version accepted");}
            foreach(int sex in new[]{-2,2})Reject(()=>AppearanceRules.Randomize(3,sex),"Randomizer accepted invalid sex");
            foreach(int face in new[]{-2,12})Reject(()=>AppearanceRules.Randomize(3,0,face),"Randomizer accepted invalid head");
        }
        static void VerifyMemberFallback()
        {
            var member=new Agent{id=2,appearance=AppearanceRules.Randomize(91,1)};string before=JsonUtility.ToJson(member);
            Check(JsonUtility.ToJson(AppearanceRules.EffectiveMemberLook(member,911,0))==JsonUtility.ToJson(member.appearance),"Enabled member appearance was replaced by a fallback");
            Check(JsonUtility.ToJson(member)==before,"Enabled member rendering mutated its source");
            member.appearance=new Appearance{sex=1,enabled=false};before=JsonUtility.ToJson(member);
            var disabled=AppearanceRules.EffectiveMemberLook(member,911,0);
            Check(disabled.sex==1&&disabled.enabled&&disabled.portraitVersion==20,"Disabled old female lost her explicit sex");
            Check(JsonUtility.ToJson(member)==before,"Disabled member fallback was written into the source");
            Check(JsonUtility.ToJson(disabled)==JsonUtility.ToJson(AppearanceRules.EffectiveMemberLook(member,911,0)),"Disabled member fallback changed between visits");
            member.appearance=null;before=JsonUtility.ToJson(member);
            Check(AppearanceRules.EffectiveMemberLook(member,911,1).sex==1,"Old member sex hint was ignored");
            var unknown=AppearanceRules.EffectiveMemberLook(member,911);
            Check(JsonUtility.ToJson(unknown)==JsonUtility.ToJson(AppearanceRules.EffectiveMemberLook(member,911)),"Null member fallback is not seed-stable");
            Check(JsonUtility.ToJson(member)==before,"Null member fallback changed save data");
            member.appearance=new Appearance();before=JsonUtility.ToJson(member);
            Check(AppearanceRules.EffectiveMemberLook(member,911,1).sex==1,"A serialized null placeholder overrode the original female sex hint");
            Check(JsonUtility.ToJson(AppearanceRules.EffectiveMemberLook(member,911))==JsonUtility.ToJson(unknown),"A serialized null placeholder changed the unknown-sex seeded fallback");
            Check(JsonUtility.ToJson(member)==before,"Resolving a materialized null placeholder changed the stored member");
            member.appearance=new Appearance{portraitVersion=20,sex=0};
            Check(AppearanceRules.EffectiveMemberLook(member,911,1).sex==0,"A deliberately configured current disabled male lost his explicit sex");
            member.appearance=null;
            bool male=false,female=false,notParity=false;
            for(int id=0;id<32;id++){member.id=id;var a=AppearanceRules.EffectiveMemberLook(member,911);male|=a.sex==0;female|=a.sex==1;notParity|=a.sex!=id%2;AppearanceRules.Validate(a);}
            Check(male&&female&&notParity,"Unknown member sex is hard-coded by ID instead of seeded random");
        }
        static void VerifyEmptyPresetSlots()
        {
            string path=Path.Combine(Application.temporaryCachePath,"minglu-classic-empty-slots-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var untouched=new PlayerProfile();var untouchedCopy=RunLifecycle.CopyProfile(untouched);
                Check(untouched.lookSlots[0]==null&&untouched.lookSlots[1]==null&&untouched.lookSlots[2]==null&&untouched.chiefLook==null&&untouchedCopy.lookSlots[0]==null&&untouchedCopy.lookSlots[1]==null&&untouchedCopy.lookSlots[2]==null,"Copying materialized a live null slot or chief");
                RunLifecycle.WriteProfile(untouched,path);
                Check(untouched.lookSlots[0]==null&&untouched.lookSlots[1]==null&&untouched.lookSlots[2]==null&&untouched.chiefLook==null&&untouched.lookSlotRevision==0&&untouched.lookSlotMask==0,"Saving materialized a live null slot or changed occupancy metadata");
                var profile=new PlayerProfile();string before=JsonUtility.ToJson(profile);
                Check(!RunLifecycle.IsEmptyLookSlot(AppearanceRules.AsRealistic(new Appearance())),"A current-version disabled preset was incorrectly treated as an old empty placeholder");
                var clone=RunLifecycle.CopyProfile(profile);
                Check(clone.lookSlots[0]==null&&clone.lookSlots[1]==null&&clone.lookSlots[2]==null,"Cloning an empty profile fabricated presets");
                profile.records.Add(new RunRecord{id="isolated-copy",date="2026-10-11",doctrine="test",seed=91,difficulty=2,progress=7,turn=8,won=true});
                var withRecord=RunLifecycle.CopyProfile(profile);withRecord.records[0].progress=99;
                Check(profile.records[0].progress==7&&withRecord.records[0].id=="isolated-copy"&&withRecord.records[0].seed==91&&withRecord.records[0].difficulty==2&&withRecord.records[0].turn==8&&withRecord.records[0].won,"Explicit profile copy shared or omitted ending records");
                profile.records.Clear();
                RunLifecycle.WriteProfile(profile,path);var loaded=RunLifecycle.ReadProfile(path);
                Check(loaded.lookSlots[0]==null&&loaded.lookSlots[1]==null&&loaded.lookSlots[2]==null,"Saving empty preset slots turned them into default portraits");
                Check(JsonUtility.ToJson(profile)==before,"Adding slot occupancy metadata mutated the live profile");
                profile.lookSlots[1]=AppearanceRules.Randomize(81,1);string saved=JsonUtility.ToJson(profile.lookSlots[1]);
                clone=RunLifecycle.CopyProfile(profile);clone.lookSlots[1].parts[0]=(clone.lookSlots[1].parts[0]+1)%12;
                Check(JsonUtility.ToJson(profile.lookSlots[1])==saved&&clone.lookSlots[0]==null&&clone.lookSlots[2]==null,"Profile copy shared a preset or filled empty neighbours");
                RunLifecycle.WriteProfile(profile,path);loaded=RunLifecycle.ReadProfile(path);
                Check(loaded.lookSlots[0]==null&&loaded.lookSlots[2]==null&&JsonUtility.ToJson(loaded.lookSlots[1])==saved,"Occupied and empty preset slots did not roundtrip independently");
                profile.lookSlots[1]=null;RunLifecycle.WriteProfile(profile,path);loaded=RunLifecycle.ReadProfile(path);
                Check(loaded.lookSlots[1]==null,"An explicitly empty slot inherited stale occupancy metadata");
                var legacy=new PlayerProfile();legacy.lookSlots[0]=new Appearance();legacy.lookSlots[1]=new Appearance{enabled=false,sex=1};legacy.lookSlots[2]=new Appearance{enabled=true};
                File.WriteAllText(path,JsonUtility.ToJson(legacy));loaded=RunLifecycle.ReadProfile(path);
                Check(loaded.lookSlots[0]==null&&loaded.lookSlots[1]!=null&&loaded.lookSlots[1].sex==1&&loaded.lookSlots[2]!=null,"Legacy empty placeholder migration discarded a real disabled/custom preset");
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
        }
        [MenuItem("Minglu/验证组合外观")]
        public static void Verify()
        {
            passed=0;var state=RunLifecycle.Create(ConfigStore.Load(),911,1,0,false);
            CommandRules.Initialize(state);uint random=state.random;
            for(int i=0;i<300;i++)AppearanceRules.Validate(AppearanceRules.Randomize(i));
            Check(state.random==random,"Appearance generation consumed simulation RNG");
            var look=AppearanceRules.Randomize(12);var clone=AppearanceRules.Copy(look);clone.parts[0]=(clone.parts[0]+1)%AppearanceRules.HeadCount;
            Check(look.parts[0]!=clone.parts[0],"Draft shares the live parts array");
            Check(JsonUtility.ToJson(AppearanceRules.Randomize(12))==JsonUtility.ToJson(look),"Seeded appearance is not reproducible");
            VerifyMigration();VerifyCurrentValidation(look);VerifyMemberFallback();VerifyEmptyPresetSlots();
            string path=Path.Combine(Application.temporaryCachePath,"minglu-appearance-v20-test.json");
            try
            {
                state.chiefLook=AppearanceRules.Copy(look);state.agents[1].appearance=AppearanceRules.Copy(look);PrototypeSave.Write(state,path);var saved=PrototypeSave.Read(path);
                Check(JsonUtility.ToJson(saved.agents[1].appearance)==JsonUtility.ToJson(look)&&JsonUtility.ToJson(saved.chiefLook)==JsonUtility.ToJson(look),"Current member/chief appearance roundtrip failed");
                Check(saved.random==random&&saved.agents[1].intel==state.agents[1].intel,"Appearance changed stats or simulation RNG");
                state.agents[1].appearance=new Appearance();PrototypeSave.Write(state,path);saved=PrototypeSave.Read(path);Check(!saved.agents[1].appearance.enabled,"Disabled portrait changed while saving");
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
            Debug.Log("MINGLU_APPEARANCE_VERIFY_SUCCESS: "+passed+" checks; 300 seeded portraits; 768 old sex/face/hair/version migrations; strict current IDs; copy-only upgrade and save compatibility");
        }
        public static void BatchDeliver(){try{Verify();LifecycleVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
