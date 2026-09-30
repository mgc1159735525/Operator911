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
                yield return Click("CommandTab_"+page);
                if(commandTab!=page)throw new Exception("Sidebar failed to open page "+page);
                Capture(Path.ChangeExtension(path,pages[page]+".png"));
            }
            yield return Click("CommandTab_0");
            var firstTask=State.command.tasks.Where(t=>t.status==0).OrderBy(t=>t.deadline).First();
            yield return Click("CommandRecommend_"+firstTask.id);
            if(commandTask!=firstTask.id||CommandRules.TaskError(State,selectedAgent,firstTask)!="")throw new Exception("Recommendation failed to select an eligible task lead");
            Capture(Path.ChangeExtension(path,"dispatch.png"));
            yield return Click("CommandPlan");
            if(modal!="missionPlan")throw new Exception("Action plan did not open");
            Capture(Path.ChangeExtension(path,"plan.png"));yield return Click("MissionBack");
            yield return Click("CommandMemberOpen");
            if(modal!="memberDetails")throw new Exception("Member dossier did not open");
            Capture(Path.ChangeExtension(path,"member.png"));yield return Click("MemberDetailsClose");

            // Overview is in the contextual advice area when no task is selected on the current page.
            yield return Click("CommandTab_1");yield return Click("CommandOverview");
            if(modal!="commandOverview")throw new Exception("Organization overview did not open");
            Capture(Path.ChangeExtension(path,"overview.png"));yield return Click("OverviewClose");
            yield return Click("RouteSlot_0_0");
            if(modal!="routePicker")throw new Exception("Role candidate picker did not open");
            Capture(Path.ChangeExtension(path,"candidates.png"));yield return Click("RoutePickerCancel");
            if(State.command.routes[0].source!=-1)throw new Exception("Cancelling role selection assigned a member");
            yield return Click("CommandEnd");
            if(modal!="commandConfirm")throw new Exception("Night review did not open");
            Capture(Path.ChangeExtension(path,"night-review.png"));yield return Click("CommandBack");

            int turn=State.turn,recordCount=profile.records.Count;
            var journal=State.journal.ToArray();
            yield return Click("Save");yield return Click("Load");
            if(State.turn!=turn||!State.journal.SequenceEqual(journal)||profile.records.Count!=recordCount)throw new Exception("Visual navigation or save/load changed the turn or recorded history");
            if(State.command.tasks.Any(t=>t.actor>=0)||State.command.routes.Any(r=>r.enabled))throw new Exception("Preview-only navigation unexpectedly authorized work");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})
                if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_DESIGN_UI_SUCCESS: title/settings/help/archive/new-run, six sidebar pages, dispatch/plan/dossier/overview/candidates/night-review, real button hits and unchanged turn/history after save/load");
        }
    }
}
