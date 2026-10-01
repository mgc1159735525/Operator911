using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator InstitutionSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Isolated save required");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            State.command.config.eventChance=0;
            // Controlled civilian fixture also covers seeds with unusually many insiders.
            State.command.people[3].institution=0;
            yield return SmokeOpenBusiness(1);
            yield return Click("RouteSlot_0_0");
            if(ProducerButton("RouteCandidate_3").interactable||!ProducerButton("RouteCandidate_0").interactable)throw new Exception("Source picker did not distinguish institution employment");
            Capture(Path.ChangeExtension(path,"sources.png"));
            yield return Click("RouteCandidate_0");
            yield return Click("RouteSlot_0_1");yield return Click("RouteCandidate_1");
            yield return Click("RouteSlot_0_2");yield return Click("RouteCandidate_2");yield return Click("RouteEnable_0");
            Capture(Path.ChangeExtension(path,"chain.png"));
            yield return SmokeOpenBusiness(2);yield return Click("CommandAgent_3");
            Capture(Path.ChangeExtension(path,"personnel.png"));
            yield return Click("CommandPeopleDetails");
            yield return Click("CommandEmployment");Capture(Path.ChangeExtension(path,"employers.png"));
            int count=State.command.tasks.Count;
            yield return Click("InstitutionCancel");if(State.command.tasks.Count!=count)throw new Exception("Cancel created a task");
            yield return Click("CommandEmployment");yield return Click("InstitutionSelect_3");
            var task=State.command.tasks.Last();if(task.operation!=4||task.target!=3||commandTask!=task.id||task.actor!=-1)throw new Exception("Employment did not open the unassigned applicant task");
            Capture(Path.ChangeExtension(path,"assignment.png"));
            yield return Click("CommandAuthorize");if(task.actor!=3)throw new Exception("Applicant cannot authorize employment");
            yield return SmokeSaveLoad();
            task=State.command.tasks.Last();if(task.actor!=3||task.institution!=3)throw new Exception("Employment lost on load");
            State.agents[3].persuade=30;yield return Click("CommandEnd");yield return Click("CommandExecute");
            yield return DrainCommandNotices(path);
            if(State.command.people[3].institution!=3||State.intelligence!=0)throw new Exception("Employment did not grant access exclusively");
            yield return SmokeOpenBusiness(1);yield return Click("RouteSlot_1_0");
            if(!ProducerButton("RouteCandidate_3").interactable)throw new Exception("New employee cannot source a second line");
            Capture(Path.ChangeExtension(path,"qualified.png"));
            Debug.Log("MINGLU_INSTITUTION_UI_SUCCESS");
        }
    }
}
