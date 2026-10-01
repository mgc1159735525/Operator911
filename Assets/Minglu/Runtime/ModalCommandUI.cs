using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        int taskReturnTab,detailRouteId;
        string taskReturnModal="";
        string CommandPageName(int tab){return new[]{"任务簿","联络网","人事档案","城区地图","值夜报告","地方事务"}[UnityEngine.Mathf.Clamp(tab,0,5)];}
        void OpenCommandTask(int id,bool recommend=false)
        {
            FocusCommandTask(id);
            var task=State.command.tasks.FirstOrDefault(t=>t.id==id&&t.status==0);
            if(task!=null&&recommend&&task.actor<0)
            {
                var candidate=State.agents.Where(a=>CommandRules.TaskError(State,a.id,task)=="").OrderByDescending(a=>CommandRules.TaskDice(State,a,task)).ThenBy(a=>CommandRole(a.id)=="机动成员"?0:1).FirstOrDefault();
                if(candidate!=null){selectedAgent=candidate.id;toast="已推荐"+candidate.name+"；查看成功率与岗位冲突后签发。";}
                else toast="暂时没有满足要求的成员。可在任务中比较各人的阻碍原因。";
            }
            Draw();
        }
        void CloseTaskDialog(bool restoreDetails=true){modal=restoreDetails?taskReturnModal:"";commandTab=UnityEngine.Mathf.Clamp(taskReturnTab,0,5);}
        string CompactRouteStatus(CommandRoute route)
        {
            string error=CommandRules.RouteError(State,route);if(error!="")return error;
            if(route.waiting+route.atRelay>0&&route.age>=5)return "积压即将失效，请优先恢复传递。";
            if(!route.enabled)return "尚未启用；暂停中的积压仍继续计时。";
            if(State.command.pauseUntil>=State.turn)return "本夜整线暂停：撤离或隐蔽休整。";
            int[] ids={route.source,route.liaison,route.courier};
            int unavailable=ids.Count(id=>CommandRules.Availability(State,id)!="");
            if(unavailable>0)return unavailable+" 个岗位需休养或离岗，链条将在该处等待。";
            int busy=ids.Count(id=>State.command.tasks.Any(t=>t.status==0&&t.actor==id));
            return busy>0?busy+" 个岗位被任务占用，本夜传递受影响。":"岗位自动执行；本夜情况可展开查看。";
        }
        // Call before the generic Escape handling in CityGameUI.Update.
        bool HandleCommandModalEscape()
        {
            if(State==null||State.command==null||State.command.eventKind!=0||State.ended||State.command.notices.Count>0)return false;
            if(modal=="missionPlan"){CloseTaskDialog();Draw();return true;}
            if(modal=="compareCandidates"){modal="missionPlan";Draw();return true;}
            if(modal=="institutionPicker"){modal="memberDetails";Draw();return true;}
            return false;
        }
        void DrawRouteDetails()
        {
            var route=State.command.routes.FirstOrDefault(r=>r.id==detailRouteId);
            if(route==null){modal="";return;}
            var p=CommandModal("联络链 "+(route.id+1)+" / 运行状况",600);
            Label(p,route.source<0?"尚未指定情报来源":CommandRules.Employment(CommandRules.Person(State,route.source)),28,100,844,29,20,Muted);
            Label(p,"内线："+CommandName(route.source)+" → 接头："+CommandName(route.liaison)+" → 外送："+CommandName(route.courier),28,148,844,28,18);
            string[] labels={"待接","中继","已送达"};int[] values={route.waiting,route.atRelay,route.delivered};
            for(int i=0;i<3;i++){Label(p,labels[i],28+i*284,202,272,22,14,Muted);Label(p,values[i]+" 份",28+i*284,235,272,40,28,i==2?Teal:TextColor);}
            Rule(p,28,298,844);
            Label(p,RouteForecast(route),28,322,844,61,18,CommandRules.RouteError(State,route)==""?TextColor:Red);
            var content=Scroll(p,"RouteHistory_"+route.id,28,401,844,82,82);
            var history=Label(content,"最近情况："+route.last+"\n任务优先占用成员；更换岗位清空积压，暂停保留积压并继续计时。",0,0,810,82,16,Muted);
            float h=UnityEngine.Mathf.Max(82,history.preferredHeight+8);history.rectTransform.sizeDelta=new UnityEngine.Vector2(810,h);content.sizeDelta=new UnityEngine.Vector2(content.sizeDelta.x,h);
            Button("RouteDetailsClose",p,"返回联络网",28,520,276,48,()=>{modal="";Draw();});
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
