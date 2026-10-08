using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        int workOverviewPage;
        bool returnToWorkOverview;

        void OpenWorkOverview()
        {
            var overview=CommandRules.BuildWorkOverview(State);
            returnToWorkOverview=false;
            workOverviewPage=overview.blocked.Any(item=>item.warning)?2:overview.continuation.Count>0?1:0;
            modal="commandConfirm";Draw();
        }

        void OpenWorkOverviewItem(CommandWorkOverviewItem item)
        {
            if(item.taskId>=0){OpenCommandTask(item.taskId);return;}
            if(item.routeId>=0){returnToWorkOverview=true;commandTab=1;modal="";Draw();return;}
            if(item.actorId>=0){returnToWorkOverview=true;selectedAgent=item.actorId;commandTab=2;modal="memberDetails";Draw();}
        }

        void ReturnToWorkOverview()
        {returnToWorkOverview=false;commandTab=3;modal="commandConfirm";Draw();}

        void DrawCommandConfirm()
        {
            var overview=CommandRules.BuildWorkOverview(State);
            foreach(var route in State.command.routes.Where(r=>r.enabled))
                overview.assigned.Add(new CommandWorkOverviewItem{title="联络链 "+(route.id+1)+" · 自动按岗位运行",routeId=route.id,
                    detail=RouteForecast(route)+"\n待接 "+route.waiting+" 份 / 中继 "+route.atRelay+" 份；各环独立判定，同一份情报本夜只前进一环。"});
            var p=CommandModal("今夜安排 / 核对后下达指示",780,1040);
            int orders=State.command.tasks.Count(t=>t.status==0&&t.actor>=0);
            Label(p,"第 "+State.turn+" 夜  ·  已签发 "+orders+" 项  ·  启用链条 "+overview.activeRouteCount+" 条  ·  待续办 "+overview.continuation.Count+" 项",28,96,984,31,23,Gold).name="WorkOverviewSummary";
            Label(p,"预留 "+CommandRules.ReservedMoney(State)+" 银两 / "+CommandRules.ReservedIntel(State)+" 份情报；查看与返回均不推进时间。",28,141,984,27,18,Muted);
            string[] pages={"本夜安排  "+overview.assigned.Count,"待续办  "+overview.continuation.Count,"冲突与风险  "+overview.blocked.Count};
            for(int i=0;i<3;i++){int page=i;MissionChoice("WorkOverviewTab_"+i,p,pages[i],"",28+i*332,189,320,43,()=>{workOverviewPage=page;Draw();},true,workOverviewPage==i);}
            var rows=workOverviewPage==0?overview.assigned:workOverviewPage==1?overview.continuation:overview.blocked;
            var content=Scroll(p,"WorkOverviewList_"+workOverviewPage,28,253,984,363,363);
            if(rows.Count==0)
            {
                string empty=workOverviewPage==0?"今夜没有已签发任务。启用的联络链按岗位运行；空闲成员自动处理日常与休养。":workOverviewPage==1?"没有等待继续的任务。新任务可在任务簿查看，不必为了派满成员而额外安排。":"当前没有列出的冲突与风险。任务仍需掷骰判定，不保证成功。";
                Label(content,empty,12,19,922,87,21,Muted).name="WorkOverviewEmpty";
            }
            float y=0;
            for(int i=0;i<rows.Count;i++)
            {
                var item=rows[i];
                var title=Label(content,item.title,4,y+8,746,30,21,Gold);
                float titleHeight=Mathf.Max(30,title.preferredHeight);title.rectTransform.sizeDelta=new Vector2(746,titleHeight);
                var detail=Label(content,item.detail,4,y+titleHeight+17,746,55,18,Muted);
                float detailHeight=Mathf.Max(48,detail.preferredHeight);detail.rectTransform.sizeDelta=new Vector2(746,detailHeight);
                string action=item.taskId>=0?"查看任务  ›":item.routeId>=0?"调整联络网  ›":"查看成员  ›";
                if(item.taskId>=0||item.routeId>=0||item.actorId>=0)
                    Button("WorkOverviewOpen_"+i,content,action,774,y+19,176,43,()=>OpenWorkOverviewItem(item));
                y+=titleHeight+detailHeight+38;Rule(content,4,y-1,946);
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(363,y+10));
            string pause=string.IsNullOrEmpty(overview.pauseReason)?"任务优先于岗位，尚未签发的阶段不会自动执行。":overview.pauseReason;
            Label(p,pause+"\n本夜维护预计 "+(State.agents.Count(a=>a.condition==Condition.Active)*2)+" 银两；夜末收入 "+(State.command.config.income+(State.command.strategy.alliances[0]>=State.command.strategy.config.allianceThreshold?4:0))+" 银两。",28,635,984,55,17,Muted).name="WorkOverviewCosts";
            Button("CommandBack",p,"返回调整",28,704,316,50,()=>{modal="";Draw();});
            Button("CommandExecute",p,"确认安排 · 推进一夜",364,704,648,50,()=>{CommandRules.Resolve(State);reportNight=-1;reportAll=false;commandTab=3;modal="";toast="执行完毕：要报汇总本夜变化；详细过程可打开值夜报告。";Save(false);Draw();},!State.ended&&State.command.eventKind==0,true);
        }
    }
}
