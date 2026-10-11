using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        string ClassicMemberIdentity(string parentName)
        {
            var parent=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==parentName);
            if(parent==null)throw new Exception("Missing actual member portrait surface: "+parentName);
            var frame=parent.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="ClassicPortrait");
            if(frame==null)throw new Exception("Member surface has no classic portrait: "+parentName);
            return string.Join("|",frame.GetComponentsInChildren<RawImage>().Select(image=>image.name+":"+(image.texture==null?"missing":image.texture.name)+":"+image.uvRect.ToString("F6")+":"+image.material.GetInstanceID()).ToArray());
        }
        IEnumerator CheckLegacyMemberEntries(string path)
        {
            var original=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
            var fixture=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
            fixture.agents[0].appearance=new Appearance{enabled=false,sex=1};
            fixture.agents[1].appearance=null;CommandRules.Person(fixture,fixture.agents[1].id).originalLook=new Appearance{sex=1};
            fixture.agents[2].appearance=null;CommandRules.Person(fixture,fixture.agents[2].id).originalLook=null;
            PrototypeSave.Write(fixture,SavePath);yield return Click("CommandMenu");yield return SmokeObservedMenuClick("Load");
            string before=JsonUtility.ToJson(State),disk=File.ReadAllText(SavePath);uint rng=State.random;
            for(int i=0;i<3;i++)
            {
                var member=State.agents[i];var expected=EffectiveMemberLook(member);
                if(i<2&&expected.sex!=1)throw new Exception("An old member's known female sex changed in the fallback");
                yield return SmokeOpenBusiness(2);yield return Click("CommandAgent_"+member.id);
                string roster=ClassicMemberIdentity("CommandBusinessBody");
                yield return Click("CommandPeopleDetails");
                if(ClassicMemberIdentity("CommandModal")!=roster)throw new Exception("Legacy member differs between the personnel page and dossier");
                yield return Click("CommandEditLook");
                if(ClassicMemberIdentity("StudioPreview")!=roster||JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expected)||LookDirty())throw new Exception("Legacy member changed on entering the appearance editor");
                Capture(Path.ChangeExtension(path,"legacy-member-"+i+".png"));
                yield return Click("LookClose");yield return Click("MemberDetailsClose");yield return SmokeReturnToMap();
                if(JsonUtility.ToJson(State)!=before||State.random!=rng||File.ReadAllText(SavePath)!=disk)throw new Exception("Read-only legacy member navigation changed the run, RNG or saved file");
            }
            PrototypeSave.Write(original,SavePath);yield return Click("CommandMenu");yield return SmokeObservedMenuClick("Load");
            Debug.Log("MINGLU_CLASSIC_LEGACY_MEMBER_UI_SUCCESS: disabled female, null appearance with known sex, and unknown-sex null appearance keep identical authored layers across personnel/dossier/editor; no live, RNG or file mutation");
        }
        IEnumerator GroomingSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave)||ProfilePath!=smokeSave+".profile")throw new Exception("Classic grooming smoke requires an isolated profile");
            // Exercise a real legacy profile/preset read and the actual title
            // editor, rather than only converting a standalone object.
            var legacy=new Appearance{enabled=true,style=1,sex=1,portraitVersion=15,ears=7,featureEditing=true,groomingEditing=true};
            legacy.parts=new[]{7,6,7,6,5,4,0,7,3,2,2,5,1};
            string legacyBefore=JsonUtility.ToJson(legacy);var expected=AppearanceRules.AsRealistic(legacy);
            profile.chiefLook=AppearanceRules.Copy(legacy);profile.lookSlots[0]=AppearanceRules.Copy(legacy);
            RunLifecycle.WriteProfile(profile,ProfilePath);profile=RunLifecycle.ReadProfile(ProfilePath);Draw();yield return null;
            yield return Click("HomeAppearance");
            if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expected)||JsonUtility.ToJson(legacy)!=legacyBefore)throw new Exception("Legacy chief did not migrate into the classic editor without mutating its source");
            if(lookDraft.parts[1]!=0||lookDraft.ears!=0||lookDraft.parts[12]!=1)throw new Exception("Legacy head bridge retained removed parts or changed the saved hair palette");
            RequireVisibleClassicPortraits();CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"legacy-head-migrated.png"));
            yield return Click("LookPresets");RequireVisibleClassicPortraits();yield return Click("LookLoadSlot");
            if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expected))throw new Exception("Legacy preset did not use the same complete-head migration");
            for(int sex=0;sex<2;sex++)
            {
                yield return StudioSetSex(sex);
                if(LookOptions(0).Length!=12||LookOptions(6).Length!=8)throw new Exception("Classic head/beard catalog is not exposed in the editor");
                yield return CheckStudioCycle(0);
                foreach(int category in new[]{2,3,4,5})yield return CheckStudioCycle(category);
                if(sex==0)yield return CheckStudioCycle(6);
                yield return StudioSetOption(0,sex==0?2:11);RequireVisibleClassicPortraits();CheckStudioPanel("AppearanceStudio");
                Capture(Path.ChangeExtension(path,"complete-head-"+(sex==0?"male":"female")+".png"));
            }
            yield return StudioSetSex(0);yield return StudioSetOption(0,0);yield return StudioSetOption(12,0);yield return StudioSetOption(0,9);
            if(lookDraft.parts[12]!=3)throw new Exception("Elder complete-head selection did not default to silver");
            yield return StudioSetOption(12,1);yield return StudioSetLock(12,true);yield return StudioSetOption(0,11);
            if(lookDraft.parts[12]!=1)throw new Exception("Complete-head change overwrote the locked manual palette");
            yield return StudioSetLock(12,false);yield return StudioSetOption(12,1);string beforeAge=JsonUtility.ToJson(lookDraft);
            yield return Click("LookAgeColor");if(lookDraft.parts[12]!=3)throw new Exception("Explicit age palette did not restore silver");
            yield return Click("LookUndo");if(JsonUtility.ToJson(lookDraft)!=beforeAge)throw new Exception("Age palette undo is not exact");
            yield return Click("LookRedo");if(lookDraft.parts[12]!=3)throw new Exception("Age palette redo lost silver");
            yield return StudioSetOption(6,7);yield return StudioSetOption(7,7);yield return StudioSetOption(8,3);
            RequireVisibleClassicPortraits();CheckStudioPanel("AppearanceStudio");Capture(Path.ChangeExtension(path,"elder-editor.png"));
            string savedLook=JsonUtility.ToJson(lookDraft);
            yield return Click("LookPresets");yield return Click("LookSaveSlot");
            if(lookPopup=="overwrite")yield return Click("LookPopupConfirm");
            yield return Click("LookPopupBack");yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");
            if(JsonUtility.ToJson(lookDraft)!=savedLook)throw new Exception("Classic preset roundtrip changed complete head/features/palettes");
            yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(JsonUtility.ToJson(State.chiefLook)!=savedLook)throw new Exception("New run changed the classic chief appearance");
            foreach(var a in State.agents)
            {
                AppearanceRules.Validate(a.appearance);
                if(a.appearance==null||a.appearance.portraitVersion!=AppearanceRules.CurrentPortraitVersion||a.appearance.parts[1]!=0||a.appearance.ears!=0)throw new Exception("A randomized member retained the removed portrait system");
            }
            RequireVisibleClassicPortraits();Capture(Path.ChangeExtension(path,"classic-roster.png"));
            yield return SmokeOpenBusiness(2);yield return Click("CommandPeopleDetails");RequireVisibleClassicPortraits();Capture(Path.ChangeExtension(path,"classic-dossier.png"));
            yield return Click("MemberDetailsClose");if(modal=="commandPage")yield return Click("CommandPageClose");
            yield return SmokeSaveLoad();if(JsonUtility.ToJson(State.chiefLook)!=savedLook)throw new Exception("Run save/load changed complete-head indices");
            RequireVisibleClassicPortraits();
            // A saved campaign can still hold the legacy schema until the
            // player explicitly applies a migrated portrait. Its actual Load
            // button must render that data through the replacement renderer.
            var currentRun=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));
            var legacyRun=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));legacyRun.chiefLook=AppearanceRules.Copy(legacy);
            var oldMember=AppearanceRules.Copy(legacy);oldMember.sex=legacyRun.agents[0].appearance.sex;
            legacyRun.agents[0].appearance=oldMember;
            CommandRules.Person(legacyRun,legacyRun.agents[0].id).originalLook=AppearanceRules.Copy(oldMember);
            uint savedRandom=State.random;int savedTurn=State.turn;PrototypeSave.Write(legacyRun,SavePath);
            yield return Click("CommandMenu");yield return SmokeObservedMenuClick("Load");
            if(State.random!=savedRandom||State.turn!=savedTurn)throw new Exception("Legacy appearance load advanced campaign state");
            RequireVisibleClassicPortraits();yield return Click("CommandOverview");
            if(modal=="campaignDossier")yield return Click("CampaignOrganization");
            if(modal!="commandOverview")throw new Exception("The campaign overview did not expose its real organization/appearance entry");
            yield return Click("CommandChiefAppearance");
            if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expected))throw new Exception("A legacy run chief did not migrate through the actual editor entry");
            RequireVisibleClassicPortraits();Capture(Path.ChangeExtension(path,"legacy-run-editor.png"));
            yield return Click("LookClose");yield return Click("OverviewClose");
            PrototypeSave.Write(currentRun,SavePath);yield return Click("CommandMenu");yield return SmokeObservedMenuClick("Load");
            if(JsonUtility.ToJson(State.chiefLook)!=savedLook||State.random!=savedRandom)throw new Exception("Classic migration fixture did not restore the original isolated run");
            yield return CheckLegacyMemberEntries(path);
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_GROOMING_UI_SUCCESS: ClassicPortrait only; real legacy profile/preset/run migration, both sexes/12 complete heads, actual preview-pixel variation for facial/beard cycles, elder silver/manual lock/undo, preset/apply/new-run/save-load, randomized members and dossiers; isolated files cleaned. Visual quality requires review.");
        }
    }
}
