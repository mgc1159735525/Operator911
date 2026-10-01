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
        IEnumerator DesignSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Design smoke requires an isolated save");
            float initialVolume=profile.volume;
            yield return Click("HomeSettings");
            if(homePage!="settings")throw new Exception("Settings navigation failed");
            Capture(Path.ChangeExtension(path,"settings.png"));
            yield return Click("VolumeDown");
            float lowered=Mathf.Max(0,initialVolume-.1f);
            if(Mathf.Abs(profile.volume-lowered)>.001f)throw new Exception("Volume down control did not update the profile");
            yield return Click("VolumeUp");
            if(Mathf.Abs(profile.volume-Mathf.Min(1,lowered+.1f))>.001f)throw new Exception("Volume up control did not update the profile");
            if(Mathf.Abs(RunLifecycle.ReadProfile(ProfilePath).volume-profile.volume)>.001f)throw new Exception("Settings were not persisted");
            yield return Click("HomeBack");

            yield return Click("HomeHelp");
            if(homePage!="help")throw new Exception("Help navigation failed");
            Capture(Path.ChangeExtension(path,"guide.png"));yield return Click("HomeBack");
            yield return Click("HomeArchive");
            if(homePage!="archive")throw new Exception("Archive navigation failed");
            Capture(Path.ChangeExtension(path,"archive.png"));yield return Click("HomeBack");
            yield return Click("HomeNew");
            if(homePage!="new")throw new Exception("New-run configuration did not open");
            var seedInput=root.GetComponentsInChildren<InputField>().Single();seedInput.text="911";
            if(seedText!="911")throw new Exception("Seed field did not update the run configuration");
            Capture(Path.ChangeExtension(path,"new-run.png"));yield return Click("BeginRun");
            if(atTitle||State.command==null||State.initialSeed!=911||State.turn!=1)throw new Exception("New-run submission did not initialize the expected campaign");

            string[] pages={"tasks","chains","personnel","map","reports","alliances"};
            for(int page=0;page<pages.Length;page++)
            {
                yield return SmokeOpenBusiness(page);
                if(commandTab!=page)throw new Exception("Desk failed to open business page "+page);
                Capture(Path.ChangeExtension(path,pages[page]+".png"));
                if(page==3)
                {
                    int beforeTurn=State.turn,beforeMoney=State.money;
                    var beforeActors=State.command.tasks.Select(t=>t.actor).ToArray();
                    foreach(var district in State.config.districts)
                    {
                        yield return Click("CommandDistrict_"+district.id);
                        if(commandTab!=3||selectedDistrict!=district.id)throw new Exception("District selection unexpectedly left the map");
                        yield return Click("DistrictClose");
                    }
                    var mapTask=State.command.tasks.Where(t=>t.status==0).OrderBy(t=>t.deadline).First();
                    yield return Click("CommandDistrict_"+mapTask.district);
                    Capture(Path.ChangeExtension(path,"district-context.png"));
                    yield return Click("DistrictTask_"+mapTask.id);
                    if(modal!="missionPlan"||commandTask!=mapTask.id)throw new Exception("District task link did not focus the requested task");
                    yield return Click("MissionBack");
                    yield return SmokeReturnToMap();
                    var emptyDistrict=State.config.districts.FirstOrDefault(d=>!State.command.tasks.Any(t=>t.status==0&&t.district==d.id));
                    if(emptyDistrict!=null)
                    {
                        yield return Click("CommandDistrict_"+emptyDistrict.id);
                        Capture(Path.ChangeExtension(path,"empty-district.png"));
                    }
                    else yield return Click("CommandDistrict_"+mapTask.district);
                    yield return Click("DistrictAffairs");
                    if(commandTab!=5)throw new Exception("District affairs link failed");
                    yield return SmokeOpenBusiness(1);
                    if(commandTab!=1)throw new Exception("Desk route entry failed");
                    if(State.turn!=beforeTurn||State.money!=beforeMoney||!State.command.tasks.Select(t=>t.actor).SequenceEqual(beforeActors))
                        throw new Exception("Map browsing changed resources, time or task assignments");
                }
            }
            yield return SmokeOpenBusiness(0);
            var firstTask=State.command.tasks.Where(t=>t.status==0&&State.agents.Any(a=>CommandRules.TaskError(State,a.id,t)=="")).OrderBy(t=>t.deadline).First();
            yield return Click("CommandRecommend_"+firstTask.id);
            if(commandTask!=firstTask.id||CommandRules.TaskError(State,selectedAgent,firstTask)!="")throw new Exception("Recommendation failed to select an eligible task lead");
            Capture(Path.ChangeExtension(path,"dispatch.png"));
            if(modal!="missionPlan")throw new Exception("Action plan did not open");
            Capture(Path.ChangeExtension(path,"plan.png"));yield return Click("MissionBack");
            yield return SmokeOpenBusiness(2);yield return Click("CommandPeopleDetails");
            if(modal!="memberDetails")throw new Exception("Member dossier did not open");
            Capture(Path.ChangeExtension(path,"member.png"));yield return Click("MemberDetailsClose");

            // Overview is reached from the map, members from the personnel window.
            yield return SmokeReturnToMap();yield return Click("CommandOverview");
            if(modal!="commandOverview")throw new Exception("Organization overview did not open");
            Capture(Path.ChangeExtension(path,"overview.png"));yield return Click("OverviewClose");
            yield return SmokeOpenBusiness(1);yield return Click("RouteSlot_0_0");
            if(modal!="routePicker")throw new Exception("Role candidate picker did not open");
            Capture(Path.ChangeExtension(path,"candidates.png"));yield return Click("RoutePickerCancel");
            if(State.command.routes[0].source!=-1)throw new Exception("Cancelling role selection assigned a member");
            yield return SmokeReturnToMap();yield return Click("CommandEnd");
            if(modal!="commandConfirm")throw new Exception("Night review did not open");
            Capture(Path.ChangeExtension(path,"night-review.png"));yield return Click("CommandBack");

            int turn=State.turn,recordCount=profile.records.Count;
            var journal=State.journal.ToArray();
            yield return SmokeSaveLoad();
            if(State.turn!=turn||!State.journal.SequenceEqual(journal)||profile.records.Count!=recordCount)throw new Exception("Visual navigation or save/load changed the turn or recorded history");
            if(State.command.tasks.Any(t=>t.actor>=0)||State.command.routes.Any(r=>r.enabled))throw new Exception("Preview-only navigation unexpectedly authorized work");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})
                if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_DESIGN_UI_SUCCESS: title/settings/help/archive/new-run, persistent map and five business dialogs, district/context/empty district navigation, task/person/overview/candidate/night dialogs, real button hits and unchanged turn/history after settings save/load");
        }
    }
}
