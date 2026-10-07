using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator RecruitmentSmoke(string path)
        {
            if(smokeSave==null)throw new Exception("Recruitment smoke requires an isolated save");
            yield return Click("HomeNew");seedText="911";Capture(Path.ChangeExtension(path,"new-run.png"));yield return Click("BeginRun");
            yield return SmokeOpenBusiness(2);
            string before=JsonUtility.ToJson(State);int count=State.agents.Count,money=State.money;
            yield return Click("CommandRecruit");yield return RequireMapDeskBlocked();
            if(modal!="recruitment"||before!=JsonUtility.ToJson(State))throw new Exception("Recruit preview spends money or changes game state");
            string candidate=JsonUtility.ToJson(recruitPreview.agent);
            Capture(Path.ChangeExtension(path,"recruit-preview.png"));
            yield return Click("RecruitCancel");yield return Click("CommandRecruit");
            if(candidate!=JsonUtility.ToJson(recruitPreview.agent))throw new Exception("Reopening recruit dossier rerolls candidate");
            yield return Click("RecruitConfirm");
            if(State.agents.Count!=count+1||State.money!=money-12||candidate!=JsonUtility.ToJson(State.agents.Last()))throw new Exception("Recruited member does not match dossier");
            Capture(Path.ChangeExtension(path,"recruited.png"));
            yield return SmokeSaveLoad();
            if(State.agents.Count!=count+1||candidate!=JsonUtility.ToJson(State.agents.Last()))throw new Exception("Recruit changed on load");
            yield return SmokeOpenBusiness(2);
            yield return Click("CommandRecruit");yield return Click("RecruitConfirm");
            if(CommandRules.OccupiedRosterCount(State)!=State.command.config.capacity)throw new Exception("Full roster setup failed");
            State.agents[0].condition=Condition.Captured;Draw();yield return null;
            var recruitButton=root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="CommandRecruit");
            if(recruitButton.interactable)throw new Exception("Captured member must reserve a roster place");
            State.agents[0].condition=Condition.Lost;Draw();yield return null;
            recruitButton=root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="CommandRecruit");
            if(!recruitButton.interactable)throw new Exception("Permanent departure must permit recruitment");
            string rosterText=root.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t=>t.name=="CommandRosterCount").text;
            if(!rosterText.Contains("在编 7 / 8")||!rosterText.Contains("离队档案 1"))throw new Exception("Roster confuses current capacity with historical headcount");
            count=State.agents.Count;money=State.money;
            yield return Click("CommandRecruit");candidate=JsonUtility.ToJson(recruitPreview.agent);yield return Click("RecruitConfirm");
            if(State.agents.Count!=count+1||CommandRules.OccupiedRosterCount(State)!=8||State.money!=money-12||candidate!=JsonUtility.ToJson(State.agents.Last()))throw new Exception("Replacement recruitment failed in UI");
            Capture(Path.ChangeExtension(path,"replacement-roster.png"));
            yield return SmokeSaveLoad();
            if(State.agents.Count!=count+1||State.agents[0].condition!=Condition.Lost||CommandRules.OccupiedRosterCount(State)!=8)throw new Exception("Historical roster was not preserved on reload");
            foreach(string f in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(f))File.Delete(f);
            Debug.Log("MINGLU_RECRUITMENT_UI_SUCCESS: preview, cancel/reopen, confirmation, map blocking, full/captured capacity, departure replacement, historical roster save/load");
        }
    }
}
