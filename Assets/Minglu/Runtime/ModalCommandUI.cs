using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        int taskReturnTab,detailRouteId,taskReturnAgent=-1;
        string taskReturnModal="";
        string CommandPageName(int tab){return new[]{"任务簿","联络网","人事档案","城区地图","值夜报告","地方事务"}[UnityEngine.Mathf.Clamp(tab,0,5)];}
        void OpenCommandTask(int id,bool recommend=false)
        {
            FocusCommandTask(id);
            var task=State.command.tasks.FirstOrDefault(t=>t.id==id&&t.status==0);
            if(task!=null&&recommend&&task.actor<0)
            {
                var candidate=AssignmentCandidates(task).FirstOrDefault(a=>CommandRules.TaskError(State,a.id,task)=="");
                if(candidate!=null){selectedAgent=candidate.id;toast="供参考："+candidate.name+"。"+AssignmentReason(candidate,task)+"；仍需你签发。";}
                else toast="暂时没有满足要求的成员。可在任务中比较各人的阻碍原因。";
            }
            Draw();
        }
        void CloseTaskDialog(bool restoreDetails=true)
        {
            modal=restoreDetails?taskReturnModal:"";commandTab=UnityEngine.Mathf.Clamp(taskReturnTab,0,5);
            if(modal=="memberDetails"&&State.agents.Any(a=>a.id==taskReturnAgent))selectedAgent=taskReturnAgent;
        }
        string CompactRouteStatus(CommandRoute route)
        {
            string error=CommandRules.RouteError(State,route);if(error!="")return error;
            if(route.waiting+route.atRelay>0&&route.age>=5)return "积压即将失效，请优先恢复传递。";
            if(!route.enabled)return "尚未启用；暂停中的积压仍继续计时。";
            if(State.command.pauseUntil>=State.turn)return "本夜整线暂停：撤离或隐蔽休整。";
            int[] ids=Enumerable.Range(0,3).Where(slot=>RouteSlotWorksTonight(route,slot)).Select(slot=>new[]{route.source,route.liaison,route.courier}[slot]).ToArray();
            int unavailable=ids.Count(id=>CommandRules.Availability(State,id)!="");
            if(unavailable>0)return unavailable+" 个岗位需休养或离岗，链条将在该处等待。";
            int busy=ids.Count(id=>State.command.tasks.Any(t=>t.status==0&&t.actor==id));
            return busy>0?busy+" 个今晚待办岗位被任务占用，传递将等待。":RouteForecast(route);
        }
        // Call before the generic Escape handling in CityGameUI.Update.
        bool HandleCommandModalEscape()
        {
            if(modal=="commandBriefing"){CloseBriefing();return true;}
            if(State==null||State.command==null||State.command.eventKind!=0||State.ended||State.command.notices.Count>0)return false;
            if(modal=="missionPlan"){CloseTaskDialog();Draw();return true;}
            if(modal=="missionImpact"||modal=="missionBackground"){modal="missionPlan";Draw();return true;}
            if(modal=="compareCandidates"){modal="missionPlan";Draw();return true;}
            if(modal=="institutionPicker"){modal="memberDetails";Draw();return true;}
            if(modal=="recruitment"){modal="";commandTab=2;Draw();return true;}
            if(modal=="commandHelp"){modal="menu";Draw();return true;}
            if(modal=="commandSettings"){modal="menu";Draw();return true;}
            if(returnToWorkOverview&&(modal=="memberDetails"||modal==""&&commandTab!=3)){ReturnToWorkOverview();return true;}
            if(modal==""&&commandTab!=3){OpenCommandPage(3);return true;}
            return false;
        }
        void DrawRouteDetails()
        {
            var route=State.command.routes.FirstOrDefault(r=>r.id==detailRouteId);
            if(route==null){modal="";return;}
            var p=CommandModal("联络链 "+(route.id+1)+" / 逐夜传递",780);
            Label(p,route.source<0?"尚未指定情报来源":CommandRules.Employment(CommandRules.Person(State,route.source)),28,94,844,28,20,Muted);
            Label(p,"内线产出 → 下一夜接头 → 再下一夜外送",28,139,844,31,24,Gold);
            Label(p,"每环独立判定；同一份情报一夜只前进一环，失败或离岗会等待。",28,180,844,29,18,Muted);
            int[] ids={route.source,route.liaison,route.courier};string[] roles={"内线 / 产出","接头人 / 中继","交通员 / 外送"};
            for(int i=0;i<3;i++)
            {
                int y=226+i*97;Rule(p,28,y,844);
                Label(p,roles[i]+"  "+CommandName(ids[i]),28,y+13,844,27,21,Gold);
                Label(p,"今晚："+RouteStepForecast(route,i),28,y+51,844,36,18,Muted).name="RouteStepForecast_"+i;
            }
            Label(p,"待接 "+route.waiting+" 份     中继 "+route.atRelay+" 份     已送达 "+route.delivered+" 份",28,536,844,29,21,TextColor);
            Label(p,"积压已计时 "+route.age+" 夜；连续7夜未送达将失效。正常交接保留情报，换岗后需重新启用。",28,576,844,44,17,route.age>=5?Red:Muted);
            var content=Scroll(p,"RouteHistory_"+route.id,28,630,844,58,58);
            var history=Label(content,"上夜情况："+route.last,0,0,810,58,18,Muted);
            float h=UnityEngine.Mathf.Max(58,history.preferredHeight+8);history.rectTransform.sizeDelta=new UnityEngine.Vector2(810,h);content.sizeDelta=new UnityEngine.Vector2(content.sizeDelta.x,h);
            Button("RouteDetailsClose",p,"返回联络网",28,707,276,48,()=>{modal="";Draw();});
        }
        void DrawStrategyDetails()
        {
            var x=State.command.strategy;var c=x.config;var p=CommandModal("地区安全 / 追查与潜伏",590);
            Label(p,"当前追查 "+x.trace+" / "+c.raid+"   ·   民心 "+x.hearts,28,100,844,31,21,x.trace>=c.raid-15?Red:TextColor);
            var content=Scroll(p,"StrategyRules",28,159,844,307,307);
            var text=Label(content,"组织安全\n地下行动累积追查；达到阈值后需要决定撤离方式。全员休整每夜减少5追查，民心达到60时每夜再减少2。\n\n成员风险\n暴露达到"+c.warning+"先预警；达到"+c.capture+"，且下一夜仍未脱险，成员才会被捕。可在档案安排生活优先，并替换联络链岗位。\n\n委托规则\n发起委托不花钱、不推进时间。需要到任务弹窗安排负责人；多阶段委托可换人，全部完成后才取得收益。",0,0,810,307,18,Muted);
            float h=UnityEngine.Mathf.Max(307,text.preferredHeight+12);text.rectTransform.sizeDelta=new UnityEngine.Vector2(810,h);content.sizeDelta=new UnityEngine.Vector2(content.sizeDelta.x,h);
            Button("StrategyDetailsClose",p,"返回地方事务",28,510,276,48,()=>{modal="";Draw();});
        }
    }
}
