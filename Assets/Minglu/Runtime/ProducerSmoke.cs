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
        Button ProducerButton(string name)
        {
            return root.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);
        }

        void RequireProducerNight(int night)
        {
            var titles=root.GetComponentsInChildren<Text>().Where(t=>t.text.StartsWith("值夜报告")).Select(t=>t.text).ToArray();
            var reports=root.GetComponentsInChildren<ScrollRect>().Where(s=>s.name.StartsWith("CommandReport_")).Select(s=>s.name).ToArray();
            if(!titles.Contains("值夜报告 / 第 "+night+" 夜")||!reports.Contains("CommandReport_"+night+"_"+reportAll))
                throw new Exception("Producer report did not show requested night "+night+"; titles="+string.Join(" | ",titles)+"; content="+string.Join(" | ",reports)+"; selectedNight="+reportNight+"; modal="+modal+"; tab="+commandTab);
        }

        IEnumerator ProducerSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Producer smoke requires an isolated smoke save");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(State.command==null||commandTab!=3)throw new Exception("First run did not open the city map");
            yield return SmokeOpenBusiness(1);
            State.command.config.eventChance=0;
            var route=State.command.routes[0];

            // Opening a position is a choice, not an implicit assignment of the selected roster member.
            yield return Click("RouteSlot_0_0");
            if(route.source!=-1||modal!="routePicker")throw new Exception("Opening a chain position silently assigned a member");
            yield return Click("RoutePickerCancel");
            if(route.source!=-1||modal!="")throw new Exception("Cancelling the chain picker changed the position");
            int source=-1;
            for(int slot=0;slot<3;slot++)
            {
                yield return Click("RouteSlot_0_"+slot);
                if(slot>0)
                {
                    var duplicate=ProducerButton("RouteCandidate_"+source);
                    if(duplicate==null||duplicate.interactable)throw new Exception("Same-chain duplicate candidate was not disabled");
                }
                if(slot==1)Capture(Path.ChangeExtension(path,"chain-picker.png"));
                var choice=root.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("RouteCandidate_")&&b.interactable);
                int id=int.Parse(choice.name.Substring("RouteCandidate_".Length));
                yield return Click(choice.name);
                int actual=slot==0?route.source:slot==1?route.liaison:route.courier;
                if(actual!=id||modal!="")throw new Exception("Chain picker assigned a different member or remained open");
                if(slot==0)source=id;
            }
            if(new[]{route.source,route.liaison,route.courier}.Distinct().Count()!=3)throw new Exception("Picker produced duplicate chain roles");
            yield return Click("RouteEnable_0");
            if(!route.enabled)throw new Exception("Configured chain did not enable");
            yield return SmokeReturnToMap();yield return Click("CommandEnd");yield return Click("CommandExecute");
            if(State.turn!=2)throw new Exception("First-night confirmation did not advance exactly one night");
            yield return DrainCommandNotices(path);

            // Controlled history exercises actual report controls without depending on random roll outcomes.
            State.turn=4;State.command.eventKind=0;State.command.notices.Clear();
            State.journal.Clear();
            State.journal.Add("[1] —— 第1夜 / 组织执行报告 ——");
            State.journal.Add("[1] 情报链1：内线产出1份，等待接头人");
            State.journal.Add("[2] —— 第2夜 / 组织执行报告 ——");
            State.journal.Add("[2] 完成「第二夜核查」：成果+4 / 银两+2");
            State.journal.Add("[3] —— 第3夜 / 组织执行报告 ——");
            State.journal.Add("[3] 「路线调查」采用筹备：扣除4银两、0份情报。");
            State.journal.Add("[3] 第一联络员 · 路线调查：情报 4骰 [4,4,4,1]，3/1成功，完成");
            State.journal.Add("[3] 完成「路线调查」：成果+4 / 银两+2");
            const string failure="第三夜营救 · 3骰 [1,2,3] → 0/2成功；未成 · 额外暴露+4、压力+2，期限内可重新授权";
            State.journal.Add("[3] "+failure);
            State.journal.Add("[3] 情报链1：交通员岗位暂停：优先执行已授权任务");
            State.journal.Add("[3] 任务到期：街坊急事，压力+3；不会直接锁死全部主线。");
            reportNight=-1;reportAll=false;Draw();yield return null;
            yield return SmokeOpenBusiness(4);RequireProducerNight(3);
            yield return Click("ReportSummary");
            var summary=ReportRows(3,false);
            if(!summary.Any(r=>r.Contains("第三夜营救")&&r.Contains("未成")&&r.Contains("额外暴露+4")))throw new Exception("Night summary hid a failed roll and its consequences");
            if(!summary.Any(r=>r.Contains("岗位暂停"))||!summary.Any(r=>r.Contains("任务到期")))throw new Exception("Night summary hid a blocked action or expiry");
            if(summary.Any(r=>r.StartsWith("[1]")||r.StartsWith("[2]")))throw new Exception("Night summary mixed other nights into the selected report");
            yield return Click("ReportAll");
            if(!reportAll||ReportRows(3,true).Count<=summary.Count)throw new Exception("Full report did not restore detailed roll and cost rows");
            yield return Click("ReportSummary");
            if(reportAll)throw new Exception("Summary button did not return to concise report");
            yield return Click("ReportPrevious");RequireProducerNight(2);
            yield return Click("ReportPrevious");RequireProducerNight(1);
            if(ProducerButton("ReportPrevious").interactable)throw new Exception("First report can navigate before the first night");
            yield return Click("ReportNext");RequireProducerNight(2);
            yield return Click("ReportNext");RequireProducerNight(3);
            if(ProducerButton("ReportNext").interactable)throw new Exception("Latest report can navigate into an unresolved night");
            Capture(Path.ChangeExtension(path,"report.png"));

            // The chief editor must be reachable in the live game and preserve the caller when applying.
            yield return SmokeReturnToMap();yield return Click("CommandOverview");yield return Click("CommandChiefAppearance");
            if(lookTarget!=-1||modal!="appearance")throw new Exception("Chief appearance control opened a teammate instead");
            yield return Click("LookGroup_1");yield return Click("LookCategory_4");
            string original=JsonUtility.ToJson(lookDraft);
            int option=(LookIndex(4)+1)%LookOptions(4).Length;
            yield return StudioSetOption(4,option);
            string edited=JsonUtility.ToJson(lookDraft);
            if(edited==original)throw new Exception("Nose option did not change the chief preview");
            yield return Click("LookUndo");
            if(lookGroup!=1||lookCategory!=4||JsonUtility.ToJson(lookDraft)!=original)throw new Exception("Undo lost appearance category or exact original draft");
            yield return Click("LookRedo");
            if(lookGroup!=1||lookCategory!=4||JsonUtility.ToJson(lookDraft)!=edited)throw new Exception("Redo lost appearance category or exact edited draft");
            yield return Click("LookApply");
            if(modal!="commandOverview"||JsonUtility.ToJson(State.chiefLook)!=edited)throw new Exception("Chief apply lost the overview return location or edited appearance");
            if(JsonUtility.ToJson(PrototypeSave.Read(SavePath).chiefLook)!=edited)throw new Exception("Chief appearance was not persisted by apply");
            Capture(Path.ChangeExtension(path,"chief-overview.png"));
            yield return Click("OverviewClose");yield return SmokeSaveLoad();
            if(JsonUtility.ToJson(State.chiefLook)!=edited)throw new Exception("Saved chief appearance changed after reload");

            // An ended campaign remains inspectable without changing its outcome or advancing time.
            State.ended=true;State.won=false;State.ending="行动窗口结束，未达成送达目标。";
            State.command.eventKind=0;State.command.notices.Clear();modal="";reportNight=-1;Draw();yield return null;
            int endedTurn=State.turn;
            yield return Click("CommandEndReview");RequireProducerNight(3);
            yield return Click("ReportPrevious");RequireProducerNight(2);
            yield return Click("ReportNext");RequireProducerNight(3);
            Capture(Path.ChangeExtension(path,"ending-review.png"));
            yield return Click("EndingReviewBack");
            if(ProducerButton("CommandEndReview")==null||!State.ended||State.won||State.turn!=endedTurn)throw new Exception("Ending review could not return safely to the same result");

            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})
                if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_PRODUCER_UI_SUCCESS: chain-first onboarding, explicit role picker/cancel/duplicates, first-night execution, per-night report/failed-roll summary, chief appearance persistence, undo category retention and ending review");
        }
    }
}
