using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        int reportNight=-1,routePickerId,routePickerSlot;
        bool reportAll;
        bool NeedsAttention(Agent a){return a.condition==Condition.Active&&CommandRules.Person(State,a.id).policy!=1&&CommandRules.Person(State,a.id).leaveUntil<State.turn;}
        string MemberStatus(Agent a)
        {
            var p=CommandRules.Person(State,a.id);
            if(a.condition==Condition.Captured)return "被捕待救";
            if(a.condition!=Condition.Active)return "已离队";
            if(p.leaveUntil>=State.turn)return "休假至第"+p.leaveUntil+"夜";
            if(p.policy==1)return "生活优先 / 休养";
            if(a.stamina<20||a.spirit<20)return "状态不足 / 需休养";
            if(p.neglect>=3)return "家事积压 / 待轮休";
            if(State.command.tasks.Any(t=>t.status==0&&t.actor==a.id))return "执行授权任务";
            return CommandRole(a.id);
        }
        string GateProgress()
        {
            var c=State.command;int level2=c.people.Count(p=>p.level>=2&&CommandRules.Agent(State,p.id).condition==Condition.Active);
            int level=CommandRules.FinalLevel(State);
            if(c.gate==0)return "立足 / 本阶段\n送达 "+c.delivered+"/2 · 任务 "+c.completed+"/2";
            if(c.gate==1)return "扎根 / 本阶段\n送达 "+c.delivered+"/"+CommandRules.SecondGateDeliveries(State)+"\n2级成员 "+level2+"/2";
            return "决胜 / 成果 "+State.progress+"/"+CommandRules.FinalScore(State)+"\n送达 "+c.delivered+"/"+CommandRules.FinalDeliveries(State)+" · "+level+"级成员 "+c.people.Count(p=>p.level>=level&&CommandRules.Agent(State,p.id).condition==Condition.Active)+"/1";
        }
        string OpeningChecklist()
        {
            bool chain=State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)=="");
            bool task=State.command.completed>0||State.command.tasks.Any(t=>t.actor>=0&&t.status==0||!string.IsNullOrEmpty(t.lastResult));
            return (task?"✓":"①")+" 比较人选，签发首项任务\n"+(State.turn>1?"✓":"②")+" 推进一夜，查看执行报告\n"+(chain?"✓":"③")+" 第二夜配置机构情报链";
        }
        string RouteForecast(CommandRoute r)
        {
            string error=CommandRules.RouteError(State,r);if(error!="")return error;
            if(!r.enabled)return "本夜暂停；积压仍计时。";
            if(State.command.pauseUntil>=State.turn)return "撤离/隐蔽休整：本夜整线暂停。";
            int[] ids={r.source,r.liaison,r.courier},skills={0,2,1};string[] roles={"内线","接头","外送"};
            string[] rows=new string[3];
            for(int i=0;i<3;i++)
            {
                string availability=CommandRules.Availability(State,ids[i]);
                bool task=State.command.tasks.Any(t=>t.status==0&&t.actor==ids[i]);
                bool work=i==0?CommandRules.SourceWorksTonight(State,r):i==1?r.waiting>0:r.atRelay>0;
                string status=!work?(i==0?"次夜观察":"等待上游"):availability!=""?"休养/离岗":task?"任务占用":(CommandRules.Dice(State,CommandRules.Agent(State,ids[i]),skills[i])+"骰执行");
                rows[i]=roles[i]+"："+status;
            }
            return string.Join(" / ",rows);
        }
        string RouteCandidateError(CommandRoute route,int slot,int id)
        {
            string error=CommandRules.Availability(State,id);if(error!="")return error;
            if(slot==0){error=CommandRules.SourceError(State,id);if(error!="")return error;}
            int[] ids={route.source,route.liaison,route.courier};
            if(ids.Where((n,i)=>i!=slot).Contains(id))return "已在此链其他岗位；需要三名不同成员";
            if(State.command.routes.Any(r=>r.id!=route.id&&r.enabled&&new[]{r.source,r.liaison,r.courier}.Contains(id)))return "已在另一条运行中的链上";
            return "";
        }
        void OpenRoutePicker(CommandRoute route,int slot){routePickerId=route.id;routePickerSlot=slot;modal="routePicker";Draw();}
        void DrawRoutePicker()
        {
            var route=State.command.routes.First(r=>r.id==routePickerId);int skill=new[]{0,2,1}[routePickerSlot];
            var p=CommandModal("选择"+new[]{"内线","接头人","交通员"}[routePickerSlot]+" / 联络链 "+(route.id+1),730);
            Label(p,routePickerSlot==0?"内线须在敌方机构任职，名单标明来源；无任职身份者可在人事档案建立。每次产出需1个成功。":"按当前"+CommandRules.Skills[skill]+"骰数比较；一骰4—6算成功，每环需1次成功。民间身份可承担传递。",28,83,844,53,18,Muted);
            var list=Scroll(p,"RouteCandidates",28,150,844,465,State.agents.Count*93);
            var people=State.agents.OrderBy(a=>RouteCandidateError(route,routePickerSlot,a.id)==""?0:1).ThenBy(a=>State.command.tasks.Any(t=>t.status==0&&t.actor==a.id)?1:0).ThenByDescending(a=>CommandRules.Dice(State,a,skill)).ToList();
            for(int i=0;i<people.Count;i++)
            {
                var a=people[i];string error=RouteCandidateError(route,routePickerSlot,a.id);int dice=CommandRules.Dice(State,a,skill);
                Label(list,a.name+" / "+(routePickerSlot==0?CommandRules.Employment(CommandRules.Person(State,a.id))+" / ":"")+dice+"骰 / "+(DispatchRules.Probability(dice,1)*100).ToString("0.#")+"%",8,i*93,594,31,19,Gold);
                bool work=routePickerSlot==0?CommandRules.SourceWorksTonight(State,route):routePickerSlot==1?route.waiting>0:route.atRelay>0;
                string duty=work?"本夜有待办，签发任务会让此岗位等待":"该岗位本夜无待办，可兼顾其他安排";
                Label(list,error!=""?error:State.command.tasks.Any(t=>t.status==0&&t.actor==a.id)?"已有任务；"+(work?"本夜岗位将等待":"本夜暂无传递冲突"):"体力 "+a.stamina+" / 暴露 "+a.exposure+" · "+duty,8,i*93+36,594,48,17,error!=""?Red:Muted);
                Button("RouteCandidate_"+a.id,list,"安排此人",625,i*93+15,180,43,()=>{selectedAgent=a.id;modal="";RequestRouteReplacement(route,routePickerSlot);if(modal=="")Draw();},error=="");
            }
            Button("RoutePickerCancel",p,"取消 · 不更换岗位",28,650,844,48,()=>{modal="";Draw();});
        }
        int JournalNight(string row){int end=row.IndexOf(']');int night;return end>1&&int.TryParse(row.Substring(1,end-1),out night)?night:0;}
        List<string> ReportRows(int night,bool all)
        {
            var rows=State.journal.Where(r=>JournalNight(r)==night).ToList();
            if(all)return rows;
            return rows.Where(r=>!r.Contains("组织执行报告")&&!r.Contains("采用")&&(!r.Contains("骰 [")||r.Contains("；未成")||r.Contains("；阶段完成")))
                .Select(r=>Regex.Replace(r,@"(\d+骰) \[[^\]]*\]","$1")).ToList();
        }
        void DrawNightReport(Transform panel)
        {
            int latest=State.journal.Where(r=>r.Contains("组织执行报告")).Select(JournalNight).DefaultIfEmpty(0).Max();
            int night=reportNight<0?latest:Mathf.Clamp(reportNight,1,Mathf.Max(1,latest));
            Label(panel,latest==0?"值夜报告":"值夜报告 / 第 "+night+" 夜",28,20,552,42,32,Gold);
            Button("ReportPrevious",panel,"上一夜",600,24,99,34,()=>{reportNight=Mathf.Max(1,night-1);scrollPositions.Remove("CommandReport");Draw();},night>1);
            Button("ReportNext",panel,"下一夜",711,24,99,34,()=>{reportNight=Mathf.Min(latest,night+1);Draw();},night<latest);
            var rows=ReportRows(night,true);
            int finished=rows.Count(r=>r.Contains("完成「")),expired=rows.Count(r=>r.Contains("任务到期：")),delivered=rows.Count(r=>r.Contains("情报链")&&r.Contains("送达1份"));
            Label(panel,"任务完成 "+finished+" / 情报送达 "+delivered+" / 任务到期 "+expired,28,76,530,24,16,Muted);
            Button("ReportNotices",panel,"要报留档  ›",600,70,210,34,()=>{modal="noticeArchive";Draw();});
            Button("ReportSummary",panel,"结果与原因",28,116,384,34,()=>{reportAll=false;Draw();},true,!reportAll);
            Button("ReportAll",panel,"展开全部判定",426,116,384,34,()=>{reportAll=true;Draw();},true,reportAll);
            if(!reportAll)rows=ReportRows(night,false);
            if(rows.Count==0)rows.Add("还没有执行报告。先比较人选、签发一项任务，再结束今夜查看结果。\n第二夜再配置机构情报链；产出、接头与外送至少跨三次结算。");
            var content=Scroll(panel,"CommandReport_"+night+"_"+reportAll,28,174,782,376,Mathf.Max(378,rows.Count*65));float y=0;
            foreach(var row in rows){var text=Label(content,row,0,y,756,50,18,row.Contains("未成")||row.Contains("暂停")||row.Contains("到期")?Red:TextColor);float h=Mathf.Max(44,text.preferredHeight+10);text.rectTransform.sizeDelta=new Vector2(756,h);y+=h+8;}
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(376,y));
        }
        void DrawEndingReview()
        {
            var p=CommandModal("行动复盘 / 回看每夜的决定",780);var content=Box("EndingReviewBody",p,30,83,838,580,Panel);DrawNightReport(content);
            Button("EndingReviewBack",p,"返回结局",28,709,844,45,()=>{modal="";Draw();});
        }
    }
}
