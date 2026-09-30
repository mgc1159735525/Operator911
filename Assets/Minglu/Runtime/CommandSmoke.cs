using System;
using System.Collections;
using System.IO;
using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        bool noticeVerified;
        IEnumerator DrainCommandNotices(string path)
        {
            if(State.command.notices==null||State.command.notices.Count==0)yield break;
            if(!noticeVerified)
            {
                Capture(Path.ChangeExtension(path,"notice.png"));
                int count=State.command.notices.Count;Save(false);Load();
                if(State.command.notices.Count!=count)throw new Exception("Unread notice lost on reload");
                int target=State.command.notices[0].tab;yield return Click("CommandNoticeOpen");
                if(commandTab!=target)throw new Exception("Notice navigation failed");
                noticeVerified=true;
            }
            while(State.command.notices.Count>0)yield return Click("CommandNoticeDismiss");
            Load();if(State.command.notices.Count!=0)throw new Exception("Read notices repeated after reload");
        }
        IEnumerator CommandSmoke(string path)
        {
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(State.command==null||State.agents.Count!=6||State.agents.Any(a=>a.appearance==null||!a.appearance.enabled))throw new Exception("Random team not initialized");
            Capture(Path.ChangeExtension(path,"team.png"));
            yield return Click("CommandTab_1");
            for(int i=0;i<3;i++){yield return Click("CommandAgent_"+i);yield return Click("RouteSlot_0_"+i);yield return Click("RouteCandidate_"+i);}
            yield return Click("RouteEnable_0");Capture(Path.ChangeExtension(path,"chain.png"));
            yield return Click("CommandTab_0");var task=State.command.tasks.First(t=>t.status==0&&t.level==1);
            yield return Click("CommandRecommend_"+task.id);Capture(Path.ChangeExtension(path,"task.png"));
            yield return Click("MissionPlan_1");
            if(State.intelligence>0)yield return Click("MissionIntel");
            Capture(Path.ChangeExtension(path,"mission-plan.png"));yield return Click("CommandAuthorize");
            if(CommandRules.ReservedMoney(State)!=4)throw new Exception("Plan budget not reserved");
            yield return Click("Save");yield return Click("Load");
            if(!State.command.routes[0].enabled||!State.command.tasks.Any(t=>t.actor>=0))throw new Exception("Command save failed");
            for(int i=0;i<12&&(i<6||State.command.delivered==0)&&!State.ended;i++)
            {
                if(State.command.eventKind!=0)yield return Click(State.money>=(State.command.eventKind==1?6:State.command.eventKind==2?10:0)?"CommandEventSafe":"CommandEventRisk");
                yield return DrainCommandNotices(path);
                yield return Click("CommandEnd");if(i==0)Capture(Path.ChangeExtension(path,"confirm.png"));yield return Click("CommandExecute");
            }
            if(State.command.delivered==0)throw new Exception("Automated chain never delivered");
            if(State.command.eventKind!=0)yield return Click(State.money>=(State.command.eventKind==1?6:State.command.eventKind==2?10:0)?"CommandEventSafe":"CommandEventRisk");
            yield return DrainCommandNotices(path);
            yield return Click("CommandTab_1");Capture(Path.ChangeExtension(path,"running-chain.png"));
            yield return Click("CommandTab_2");Capture(Path.ChangeExtension(path,"life.png"));
            yield return Click("CommandPeopleDetails");yield return Click("LifePolicy_1");yield return Click("CommandEditLook");yield return Click("LookCategory_1");yield return Click("LookOption_2");yield return Click("LookApply");yield return Click("MemberDetailsClose");
            yield return Click("Save");yield return Click("Load");if(State.command==null)throw new Exception("Appearance commit lost command state");
            State.command.eventKind=2;State.command.eventActor=0;State.command.eventText="城中大清查预警，需要决定是否分散休整。";Draw();yield return null;Capture(Path.ChangeExtension(path,"event.png"));
            yield return Click(State.money>=10?"CommandEventSafe":"CommandEventRisk");Capture(Path.ChangeExtension(path,"decision-result.png"));yield return DrainCommandNotices(path);yield return Click("CommandTab_3");Capture(Path.ChangeExtension(path,"map.png"));
            yield return Click("CommandTab_4");Capture(Path.ChangeExtension(path,"report.png"));
            foreach(string f in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(f))File.Delete(f);
            if(!noticeVerified)throw new Exception("No event notice tested");
            UnityEngine.Debug.Log("MINGLU_COMMAND_UI_SUCCESS: notice persistence/ack/navigation, random crew, route assignment, automated handoff and delivery, task delegation, policies, appearance, events, save/load, map and report");
        }
    }
}
