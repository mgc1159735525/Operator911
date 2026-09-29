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
