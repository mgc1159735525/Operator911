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
        IEnumerator NoticeArchiveSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Archive smoke requires isolated saves");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            var task=State.command.tasks[0];
            CommandRules.Notice(State,new CommandNotice{title="成员来信",body="待办仍待授权，关闭要报后也应能找到这封信。",task=task.id,tab=0});
            CommandRules.Notice(State,new CommandNotice{title="岗位提醒",body="今晚请检查联络链。",tab=1});
            Draw();yield return null;
            yield return Click("CommandNoticeDismiss");
            if(State.command.notices.Count!=0||State.command.noticeArchive.Count!=2)throw new Exception("Acknowledgement discarded archived notices");
            yield return SmokeSaveLoad();yield return SmokeOpenBusiness(4);yield return Click("ReportNotices");
            if(modal!="noticeArchive"||!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("成员来信")))throw new Exception("Saved archive cannot be reopened");
            yield return RequireMapDeskBlocked();yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"notice-archive.png"));
            int turn=State.turn,money=State.money;uint rng=State.random;
            yield return Click("ArchivedNotice_1");
            if(commandTab!=1||modal!=""||State.turn!=turn||State.money!=money||State.random!=rng)throw new Exception("Archived notice navigation mutated simulation");
            yield return SmokeReturnToMap();yield return SmokeOpenBusiness(4);yield return Click("ReportNotices");yield return Click("NoticeArchiveClose");
            if(commandTab!=4||modal!="")throw new Exception("Archive return did not restore report");
            CommandRules.Notice(State,new CommandNotice{title="主线来信",body="下一章等待指示。",tab=6});Draw();yield return null;
            yield return Click("CommandNoticeOpen");
            if(modal!="campaignDossier"||commandTab!=3)throw new Exception("Chapter notice opened an empty business page instead of the campaign");
            yield return Click("CampaignClose");yield return SmokeOpenBusiness(4);yield return Click("ReportNotices");yield return Click("ArchivedNotice_2");
            if(modal!="campaignDossier"||State.command.noticeArchive.Count!=3)throw new Exception("Archived chapter notice lost its destination");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",smokeSave,smokeSave+".bak"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_NOTICE_ARCHIVE_UI_SUCCESS: physical clicks, retained messages, save/load, source navigation and modal shielding");
        }
    }
}
