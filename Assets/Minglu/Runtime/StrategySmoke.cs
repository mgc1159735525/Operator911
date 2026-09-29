using System;
using System.Collections;
using System.IO;
using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator StrategySmoke(string path)
        {
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");State.command.config.eventChance=0;
            yield return Click("CommandMenu");Capture(Path.ChangeExtension(path,"menu.png"));yield return Click("CommandGuide");Capture(Path.ChangeExtension(path,"guide.png"));yield return Click("CommandGuideBack");
            yield return Click("TaskFilter_2");if(root.Find("CommandCenter").GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name.StartsWith("CommandTask_")))throw new Exception("Authorized filter shows unassigned tasks");yield return Click("TaskFilter_0");Capture(Path.ChangeExtension(path,"tasks.png"));
            var taskScroll=root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().First(s=>s.name=="CommandTasks");taskScroll.verticalNormalizedPosition=0;
            yield return Click("TaskFilter_1");taskScroll=root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().First(s=>s.name=="CommandTasks");if(taskScroll.verticalNormalizedPosition<.99f)throw new Exception("Task filter did not reset scroll position");yield return Click("TaskFilter_0");
            yield return Click("TaskFilter_2");CommandRules.Notice(State,new CommandNotice{title="定位检查",body="从通知定位未授权任务",tab=0,task=State.command.tasks[0].id});Draw();yield return null;yield return Click("CommandNoticeOpen");
            if(taskFilter!=0||commandTask!=State.command.tasks[0].id)throw new Exception("Notice target hidden by filter");
            Save(false);Load();if(commandTask!=-1||taskFilter!=0)throw new Exception("Stale task selection survived load");
            yield return Click("CommandTab_2");var person=CommandRules.Person(State,selectedAgent);string originalHistory=person.last;person.last=string.Concat(Enumerable.Repeat("近期行动记录较长，需要完整阅读而不遮挡政策按钮。",12));Draw();yield return null;
            var details=root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().First(v=>v.name=="PersonDetails_"+selectedAgent);var detailText=details.content.GetComponentInChildren<UnityEngine.UI.Text>();if(detailText.preferredHeight>detailText.rectTransform.rect.height)throw new Exception("Personal history clipped");if(details.verticalNormalizedPosition<.99f)throw new Exception("Growing history unexpectedly jumped to bottom");Capture(Path.ChangeExtension(path,"people-long.png"));person.last=originalHistory;
            yield return Click("CommandTab_0");
            yield return Click("CommandTab_5");Capture(Path.ChangeExtension(path,"strategy.png"));
            yield return Click("AllianceTask_0");var task=State.command.tasks.Last();if(task.operation!=1)throw new Exception("Alliance UI did not create task");
            yield return Click("CommandCompare_"+task.id);Capture(Path.ChangeExtension(path,"compare.png"));
            yield return Click("CompareSelect_3");Capture(Path.ChangeExtension(path,"plan.png"));yield return Click("MissionAuthorize");
            if(task.actor!=3)throw new Exception("Comparison authorized wrong actor");
            yield return Click("CommandTab_1");for(int i=0;i<3;i++){yield return Click("CommandAgent_"+i);yield return Click("RouteSlot_0_"+i);}yield return Click("RouteEnable_0");
            var route=State.command.routes[0];route.waiting=2;route.atRelay=1;Draw();yield return null;
            yield return Click("CommandAgent_4");yield return Click("RouteSlot_0_1");Capture(Path.ChangeExtension(path,"replace-warning.png"));yield return Click("RouteReplaceCancel");
            if(route.liaison!=1||route.waiting!=2||route.atRelay!=1||!route.enabled)throw new Exception("Cancel replacement changed route");
            yield return Click("RouteSlot_0_1");yield return Click("RouteReplaceConfirm");if(route.liaison!=4||route.waiting!=0||route.enabled)throw new Exception("Confirmed route change not applied");
            yield return Click("CommandEnd");Capture(Path.ChangeExtension(path,"night-review.png"));yield return Click("CommandBack");
            State.command.strategy.trace=75;State.command.strategy.raidPending=true;State.command.eventKind=4;Draw();yield return null;Capture(Path.ChangeExtension(path,"raid.png"));
            int money=State.money;yield return Click("CommandEventSafe");if(State.money!=money-10||State.command.eventKind!=0)throw new Exception("Relocation UI failed");yield return DrainCommandNotices(path);
            State.turn=4;CommandRules.CaptureForCommand(State,0);Draw();yield return null;Capture(Path.ChangeExtension(path,"capture-notice.png"));yield return DrainCommandNotices(path);
            var rescue=State.command.tasks.Last();yield return Click("CommandNextAction");yield return Click("CommandCompare_"+rescue.id);Capture(Path.ChangeExtension(path,"rescue-compare.png"));yield return Click("CompareBack");
            yield return Click("Save");yield return Click("Load");if(State.agents[0].condition!=Condition.Captured||!State.command.tasks.Any(t=>t.operation==2))throw new Exception("Prisoner or rescue lost on reload");
            foreach(string f in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(f))File.Delete(f);
            UnityEngine.Debug.Log("MINGLU_STRATEGY_UI_SUCCESS: alliance creation, comparison, authorization, route cancel/confirm, night review, relocation, capture notice, rescue and persistence");
        }
    }
}
