using System;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        int commandTab,commandTask=-1;
        string CommandRole(int id)
        {
            var route=State.command.routes.FirstOrDefault(r=>r.enabled&&new[]{r.source,r.liaison,r.courier}.Contains(id));
            return route==null?"机动成员":route.source==id?"潜伏内线":route.liaison==id?"接头联络":"交通外送";
        }
        string CommandName(int id){return id<0?"未指定":CommandRules.Agent(State,id).name;}
        string CommandNext()
        {
            if(State.command.eventKind!=0)return "先处理突发事件，组织正等待你的决定。";
            if(State.agents.Any(a=>a.condition==Condition.Active&&a.exposure>=State.command.strategy.config.warning))return "有人被重点追查：前往人事页安排生活优先，并检查链条替补。";
            if(State.command.tasks.Any(t=>t.status==0&&t.operation==2&&t.actor<0))return "有人被捕：优先比较救援人选，营救需要两次成功，留意截止夜。";
            if(!State.command.routes.Any(r=>r.enabled))return "第一步：打开传递链条，选择三名成员并启用。";
            if(State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)!=""))return "联络链有失效岗位，请替换成员并恢复联系。";
            if(State.command.people.Any(p=>p.neglect>=3&&CommandRules.Agent(State,p.id).condition==Condition.Active))return "有人生活事务积压：在人事页安排生活优先，或替换链条岗位。";
            if(State.command.tasks.Any(t=>t.status==0&&t.actor<0))return "还有待授权事项：比较人员与方案；也可留待后续，链条会自动执行。";
            return "任务已授权。检查岗位冲突后推进一夜，成员会自行执行并提交报告。";
        }
        void DrawCommandGame()
        {
            CommandRules.EnsureStrategy(State);
            Label(root,"明路 · 地下",24,20,250,44,30,Gold);
            Label(root,"追查 "+State.command.strategy.trace+" / "+State.command.strategy.config.raid+"  ·  民心 "+State.command.strategy.hearts,24,83,265,24,16,Muted);
            Label(root,"第 "+State.turn+" / "+State.config.Value("turns")+" 夜",310,24,220,35,27,Gold);
            Label(root,"成果 "+State.progress+"  ·  送达 "+State.command.delivered+"  ·  可用情报 "+(State.intelligence-CommandRules.ReservedIntel(State))+"  ·  可用银两 "+(State.money-CommandRules.ReservedMoney(State))+"  ·  压力 "+State.pressure,545,28,640,34,19,Teal);

            Button("Save",root,"保存",1192,28,88,38,Save);Button("Load",root,"读取",1290,28,88,38,Load,System.IO.File.Exists(SavePath));
            Button("CommandHome",root,"返回",1390,28,82,38,ReturnHome);
            Button("CommandHelp",root,"?",1482,28,42,38,()=>{modal="commandHelp";Draw();});Button("CommandMenu",root,"菜单",1530,28,46,38,()=>{modal="menu";Draw();});
            Box("TopRule",root,24,113,1552,1,Line);
            DrawNoirBrief();DrawNoirRoster();
            string[] tabs={"任务指挥","传递链条","人事生活","城区态势","执行报告","地区经营"};
            for(int i=0;i<tabs.Length;i++){int n=i;Button("CommandTab_"+i,root,tabs[i],310+i*140,79,132,36,()=>{commandTab=n;Draw();},true,commandTab==i);}
            var panel=Box("CommandCenter",root,310,126,838,578,Panel);
            if(commandTab==0)DrawCommandTasks(panel);else if(commandTab==1)DrawCommandRoutes(panel);else if(commandTab==2)DrawCommandPeople(panel);else if(commandTab==3)DrawCommandMap(panel);else if(commandTab==4)DrawCommandReport(panel);else DrawStrategy(panel);
            DrawCommandAdvice();
            Box("CommandFooter",root,24,850,1552,42,Panel);
            Label(root,toast,38,860,985,26,16,Muted);
            Button("CommandNextAction",root,"前往处理",1040,854,214,34,GoNextCommand);
            Button("CommandEnd",root,"推进一夜 · 执行授权",1285,854,268,34,()=>{modal="commandConfirm";Draw();},!State.ended&&State.command.eventKind==0,true);
            if(State.command.eventKind!=0)DrawCommandEvent();
            else if(State.ended)DrawCommandEnding();
            else if(State.command.notices!=null&&State.command.notices.Count>0)DrawCommandNotice();
            else if(modal=="menu")DrawCommandMenu();
            else if(modal=="commandHelp")DrawCommandGuide();
            else if(modal=="appearance")DrawModal();
            else if(modal=="compareCandidates")DrawCandidateComparison();
            else if(modal=="routeReplace")DrawRouteReplacement();
            else if(modal=="missionPlan")DrawMissionPlan();
            else if(modal=="commandConfirm")DrawCommandConfirm();
            else if(modal!=""){modal="";}
        }
        void DrawCommandTasks(Transform panel)
        {
            Label(panel,"你决定目标与负责人，成员决定具体执行",20,15,800,32,23,Gold);
            Label(panel,"选目标 → 比较人员 → 配置方案 → 授权；失败可在期限内重试。",20,56,798,30,17,Muted);
            string[] filters={"全部","待授权","已授权","两夜内截止"};
            for(int f=0;f<4;f++){int value=f;Button("TaskFilter_"+f,panel,filters[f],20+f*198,92,187,32,()=>{taskFilter=value;resetCommandTaskScroll=true;Draw();},true,taskFilter==f);}
            var tasks=State.command.tasks.Where(t=>t.status==0&&(taskFilter==0||taskFilter==1&&t.actor<0||taskFilter==2&&t.actor>=0||taskFilter==3&&t.deadline<=State.turn+1)).OrderBy(t=>t.id==commandTask?0:t.operation==2?1:2).ThenBy(t=>t.deadline).ToList();
            var content=Scroll(panel,"CommandTasks",18,137,802,431,Mathf.Max(100,tasks.Count*174));int i=0;
            if(tasks.Count==0)Label(content,"当前筛选没有任务。可切换「全部」，或到地区经营发起委托；链条与日常仍会运行。",12,15,750,70,20,Teal);
            foreach(var t in tasks)
            {
                int y=i++*174;var card=Box("CommandTaskCard",content,0,y,780,162,Ink);
                Label(card,t.title+" / "+(string.IsNullOrEmpty(t.category)?"委托":t.category)+" · "+t.origin.Split('：')[0],12,9,575,29,21,Gold);Label(card,(t.deadline<=State.turn?"今夜截止":"剩"+(t.deadline-State.turn+1)+"夜"),592,12,173,26,16,t.deadline==State.turn?Red:Muted);
                Label(card,State.config.District(t.district).name+" · "+CommandRules.Skills[t.skill]+"骰 / "+t.required+"个成功 / 身份Lv."+t.level+(t.profession==""?"":" / "+t.profession),12,48,750,27,17);
                Label(card,(t.stepsRequired>1?"阶段 "+t.stepsDone+"/"+t.stepsRequired+" · ":"")+CommandRules.TaskReward(t)+" · "+(t.actor<0?"待授权":CommandName(t.actor)+" / "+CommandRules.Plans[t.plan]),12,81,750,28,16,Teal);
                Button("CommandTask_"+t.id,card,"查看与分配",12,118,200,32,()=>{commandTask=t.id;resetCommandTaskScroll=true;Draw();},true,t.id==commandTask);
                Button("CommandRecommend_"+t.id,card,"推荐可用人选",226,118,212,32,()=>{commandTask=t.id;resetCommandTaskScroll=true;var candidate=State.agents.Where(a=>CommandRules.TaskError(State,a.id,t)=="").OrderByDescending(a=>CommandRules.TaskDice(State,a,t)).ThenBy(a=>CommandRole(a.id)=="机动成员"?0:1).FirstOrDefault();if(candidate!=null){selectedAgent=candidate.id;toast="已推荐"+candidate.name+"。请检查右侧成功率和岗位冲突，再授权。";}else toast="暂无满足要求的人选：可先培养身份、安排休养，或选择其他任务。";Draw();});
                Button("CommandCancel_"+t.id,card,"撤回授权",452,118,165,32,()=>{t.actor=-1;toast="授权已撤回，释放成员及预留资源。";Save(false);Draw();},t.actor>=0);
                Button("CommandCompare_"+t.id,card,"对比人选",631,118,137,32,()=>{commandTask=t.id;resetCommandTaskScroll=true;modal="compareCandidates";Draw();});
            }
        }
        void DrawCommandAdvice()
        {
            var p=Box("CommandAdvice",root,1164,79,412,625,Panel);var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,a.id);
            Label(p,"指挥席 / "+a.name,18,16,375,33,25,Gold);
            Label(p,member.profession+" Lv."+member.level+" / "+CommandRole(a.id),18,62,375,28,18,Teal);
            Label(p,"情报 "+a.intel+" · 潜行 "+a.stealth+" · 说服 "+a.persuade+" · 组织 "+a.organize,18,103,375,52,18);
            Label(p,"体力 "+a.stamina+" / 精神 "+a.spirit+" / 暴露 "+a.exposure+"\n日常："+member.life+" · 积压"+member.neglect+"夜",18,161,375,65,17,Muted);
            var t=State.command.tasks.FirstOrDefault(tk=>tk.id==commandTask&&tk.status==0);
            if(commandTab==0&&t!=null)
            {
                string error=CommandRules.TaskError(State,a.id,t);int dice=CommandRules.TaskDice(State,a,t);double chance=DispatchRules.Probability(dice,t.required);
                Label(p,t.title,18,235,375,34,23,Gold);Label(p,t.body,18,277,375,72,17);
                Label(p,CommandRules.Plans[t.plan]+(t.intelSupport?" + 情报支援":"")+" / "+State.config.District(t.district).name+"\n"+dice+"骰 / 需"+t.required+"成功 / "+(chance*100).ToString("0.#")+"%\n执行：14体力、6精神、暴露+"+CommandRules.TaskExposure(State,a,t),18,356,375,88,18,Teal);
                Button("CommandPlan",p,"方案 / 投入 / 完整后果",18,452,375,40,()=>{modal="missionPlan";Draw();});
                Label(p,error!=""?error:CommandRole(a.id)!="机动成员"?"此人优先执行任务，对应链条岗位本夜等待。":"条件满足，授权时预留资源，执行时扣除。",18,501,375,68,17,error==""?Muted:Red);
                Button("CommandAuthorize",p,"授权 "+a.name,18,578,375,32,()=>{toast=CommandRules.Assign(State,a.id,t.id);if(toast==""){toast="已授权并预留资源；推进一夜后执行。";Save(false);}Draw();},error=="",true);
            }
            else
            {
                Label(p,"当前关卡门槛",18,250,375,32,23,Gold);
                Label(p,CommandRules.GateText(State)+"\n\n已完成任务 "+State.command.completed+"\n已送达情报 "+State.command.delivered+"\n身份2级以上 "+State.command.people.Count(x=>x.level>=2&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人 / 3级以上 "+State.command.people.Count(x=>x.level>=3&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人",18,298,375,180,19);
                Label(p,"预算已预留："+CommandRules.ReservedMoney(State)+"银两 / "+CommandRules.ReservedIntel(State)+"情报。\n每夜：任务 → 链条 → 日常；一人只做一项地下工作。",18,490,375,90,17,Muted);
                Button("CommandHelpRoute",p,"查看 / 设计传递链条",18,576,375,33,()=>{commandTab=1;Draw();});
            }
        }
        void DrawCommandRoutes(Transform panel)
        {
            Label(panel,"设计链条，接头由成员自动完成",20,14,798,33,24,Gold);
            Label(panel,"先在底部选人，再点岗位填入。启用后持续运行；同夜不能越过两环。",20,55,798,45,17,Muted);
            int i=0;
            foreach(var r in State.command.routes)
            {
                int y=110+i++*226;Label(panel,"联络链 "+(r.id+1)+"  /  "+(r.enabled?"自动运行":"暂停或未启用"),20,y,790,29,21,Gold);
                for(int slot=0;slot<3;slot++)
                {
                    int n=slot,id=slot==0?r.source:slot==1?r.liaison:r.courier;string label=new[]{"内线 · 情报","接头人 · 说服","交通员 · 潜行"}[slot];
                    Button("RouteSlot_"+r.id+"_"+slot,panel,label+"\n"+CommandName(id),20+slot*266,y+40,252,55,()=>RequestRouteReplacement(r,n));
                }
                string error=CommandRules.RouteError(State,r);
                Label(panel,"待接 "+r.waiting+" → 中继 "+r.atRelay+" → 上级累计收到 "+r.delivered+"\n"+(error!=""?error:r.last),20,y+105,555,66,16,error==""?Teal:Red);
                Button("RouteEnable_"+r.id,panel,r.enabled?"暂停此线":"确认启用此线",588,y+112,210,42,()=>{r.enabled=!r.enabled;r.last=r.enabled?"已启用：内线隔夜尝试产出，接头与外送自动推进。":"已暂停，成员按其他任务或生活安排工作。";Save(false);toast=r.enabled?"链条已启用：后续夜晚自动判定，任务优先占用人手。":"链条已暂停，成员可以生活与休养。";Draw();},r.enabled||error=="",true);
            }
            Label(panel,"修改岗位会清空该线积压；暂停保留积压，但消息仍会逐夜失效。先做好人事再启用。",20,546,798,28,14,Muted);
        }
        void DrawCommandPeople(Transform panel)
        {
            var a=CommandRules.Agent(State,selectedAgent);var p=CommandRules.Person(State,a.id);
            DrawLook(panel,a.appearance,20,22,230,360);
            Label(panel,a.name+" / 代号「"+p.code+"」",270,23,540,38,25,Gold);
            var details=Scroll(panel,"PersonDetails_"+a.id,270,83,540,247,248);
            var personText=Label(details,p.age+"岁 · 性情"+p.temper+"\n表身份："+p.profession+" Lv."+p.level+"\n地下岗位："+CommandRole(a.id)+"\n生活牵挂："+p.life+"\n身份经验："+p.xp+" / "+(State.command.config.coverXP*p.level)+"\n组织信任："+p.loyalty+" / 100\n近期情况："+p.last,0,0,514,247,20);
            float detailsHeight=Mathf.Max(247,personText.preferredHeight+12);personText.rectTransform.sizeDelta=new Vector2(514,detailsHeight);details.sizeDelta=new Vector2(details.sizeDelta.x,detailsHeight);
            string[] policies={"平衡安排","生活优先","主动发现"};
            for(int i=0;i<3;i++){int n=i;Button("LifePolicy_"+i,panel,policies[i],270+i*181,343,170,41,()=>{p.policy=n;Save(false);toast="政策已修改："+policies[n]+"。生活优先会暂停该成员的任务和链条岗位。";Draw();},true,p.policy==i);}
            Label(panel,"空闲时自动工作、处理家事并恢复体力精神，累计经验提升表身份。连续地下工作3夜后，生活积压会影响状态与信任。主动发现有机会提交新任务。",20,411,790,85,19,Muted);
            Label(panel,State.agents.Count>=State.command.config.capacity?"招募已满员：达到本局人数上限。":State.money-CommandRules.ReservedMoney(State)<12?"招募需12可用银两；预留预算不能用于招募。":"可招募：新人身份与能力随机，先检查再安排。",20,490,790,26,16,Muted);
            Button("CommandRecruit",panel,"接纳随机新成员 · 12银两",20,520,385,43,()=>{State.money-=12;CommandRules.AddPerson(State);Save(false);toast="新人已到场，请检查身份能力并安排岗位。";Draw();},State.money-CommandRules.ReservedMoney(State)>=12&&State.agents.Count<State.command.config.capacity,true);
            Button("CommandEditLook",panel,"编辑外观 / 保留角色能力",423,520,375,43,()=>OpenAppearance(a.id));
        }
        void DrawCommandMap(Transform panel)
        {
            var raw=Rect("CommandMap",panel,0,0,838,588).gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.texture=map;raw.color=new Color(.65f,.65f,.65f);raw.raycastTarget=false;
            foreach(var d in State.config.districts){int count=State.command.tasks.Count(t=>t.status==0&&t.district==d.id);Button("CommandDistrict_"+d.id,panel,d.name+" / 待办"+count+"\n风险+"+Mathf.Clamp(d.risk,0,6),d.x*650+10,d.y*445+25,168,56,()=>{var t=State.command.tasks.Where(tk=>tk.status==0&&tk.district==d.id).OrderBy(tk=>tk.deadline).FirstOrDefault();if(t!=null){FocusCommandTask(t.id);toast="已定位该区最紧急事项；比较人员后授权。";}else toast=d.name+"当前没有待办，可查看其他城区或地区经营。";Draw();});}
            Box("MapNote",panel,16,508,806,64,Ink);Label(panel,"地区风险会增加暴露；进阶任务受盘查影响。点击城区选择任务，再比较人员与方案。",28,522,775,44,18,Gold);
        }
        void DrawCommandReport(Transform panel)
        {
            Label(panel,"组织执行报告 · 每次判定都有记录",20,16,795,36,24,Gold);
            var rows=State.journal.AsEnumerable().Reverse().Take(80).ToList();var content=Scroll(panel,"CommandReport",20,72,797,495,rows.Count*77);
            float reportY=0;
            foreach(var row in rows){var text=Label(content,row,0,reportY,770,70,18);float height=Mathf.Max(70,text.preferredHeight+8);text.rectTransform.sizeDelta=new Vector2(770,height);reportY+=height+10;}
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(495,reportY));
        }
        Transform CommandModal(string title,float height=600)
        {
            var shade=Box("CommandShade",root,0,0,1600,900,new Color(0,0,0,.8f));var p=Box("CommandModal",shade,350,(900-height)/2,900,height,Panel);Border(p,0,0,900,height,Line);Box("CaseRibbon",p,0,0,8,height,Hex("AB493C"));Label(p,title,28,24,840,45,30,Gold);return p;
        }
        void DrawCommandConfirm()
        {
            var p=CommandModal("推进一夜前，确认组织安排",750);
            string orders=string.Join("\n",State.command.tasks.Where(t=>t.status==0&&t.actor>=0).Select(t=>CommandName(t.actor)+" → "+t.title));
            int jobs=State.command.tasks.Count(t=>t.status==0&&t.actor>=0),routes=State.command.routes.Count(r=>r.enabled);
            var orderList=Scroll(p,"NightOrders",28,97,844,150,Mathf.Max(150,(jobs+3)*29));
            Label(orderList,"授权任务 "+jobs+" 项 / 运行链条 "+routes+" 条\n预留："+CommandRules.ReservedMoney(State)+"银两、"+CommandRules.ReservedIntel(State)+"份情报\n"+(orders==""?"没有额外任务：成员按链条和日常政策工作。":orders),0,0,805,Mathf.Max(150,(jobs+3)*29),19);
            Label(p,"任务优先于长期岗位；空闲成员自动工作与休养。\n本夜维护预计 "+(State.agents.Count(a=>a.condition==Condition.Active)*2)+"银两，夜末收入 "+(State.command.config.income+(State.command.strategy.alliances[0]>=State.command.strategy.config.allianceThreshold?4:0))+"银两（含联盟）。\n"+CommandNext(),28,258,840,125,18,Teal);
            var warnings=CommandRules.NightWarnings(State);var warningList=Scroll(p,"NightWarnings",28,397,844,235,Mathf.Max(235,warnings.Split('\n').Length*55));Label(warningList,warnings,0,0,805,Mathf.Max(235,warnings.Split('\n').Length*55),19,Gold);
            Button("CommandBack",p,"返回调整",28,665,300,50,()=>{modal="";Draw();});
            Button("CommandExecute",p,"下达指示 · 成员执行",348,665,524,50,()=>{CommandRules.Resolve(State);commandTab=4;modal="";toast="执行完毕：查看报告，处理待决事件，再安排下一夜。";Save(false);Draw();},true,true);
        }
        void DrawCommandEvent()
        {
            if(State.command.eventKind==4){DrawRaidEvent();return;}
            var c=State.command;var p=CommandModal(new[]{"","生活与组织","大清查预警","信任危机"}[c.eventKind]);
            var affected=CommandRules.Agent(State,c.eventActor);Portrait(p,affected,28,102,90,132);
            Label(p,affected.name+" / "+CommandRules.Person(State,affected.id).profession,140,99,728,32,21,Gold);
            Label(p,c.eventText,140,141,728,105,24);
            string[] protect={"","援助并准假 · 6银","分散休整 · 10银","暂停岗位，安排谈话"};
            string[] accept={"","准假，暂不拨款","维持工作，承担风险","继续信任，允许留任"};
            string explanation=c.eventKind==1?"两项都会准假一夜；援助额外恢复信任。链条会等待此人，不会自动越级传递。":c.eventKind==2?"分散休整：链条暂停今夜、压力−12。\n维持工作：全员暴露+18、压力+8，产生应急任务。":"谈话：该成员今夜离岗，信任+15。\n继续信任：有叛离风险，信任不足45时更高；只冲击本人所在的联络链。";
            Label(p,explanation,28,265,840,135,21,Muted);
            Button("CommandEventSafe",p,protect[c.eventKind],28,464,408,68,()=>{toast=CommandRules.Choose(State,true);Save(false);Draw();},State.money-CommandRules.ReservedMoney(State)>=(c.eventKind==1?6:c.eventKind==2?10:0),true);
            Button("CommandEventRisk",p,accept[c.eventKind],456,464,416,68,()=>{toast=CommandRules.Choose(State,false);Save(false);Draw();});
        }
        void DrawCommandNotice()
        {
            var n=State.command.notices[0];
            int lines=n.body.Split('\n').Sum(line=>Mathf.Max(1,Mathf.CeilToInt(line.Length/36f)));
            float contentHeight=lines*33+16,bodyHeight=Mathf.Clamp(contentHeight,120,335),height=bodyHeight+235;
            var p=CommandModal(n.title,height);
            Label(p,"已发生的事项 · 不消耗行动，不推进夜晚",28,77,840,30,18,Teal);
            var content=Scroll(p,"CommandNoticeBody",28,125,844,bodyHeight,Mathf.Max(bodyHeight,contentHeight));
            var noticeText=Label(content,n.body,0,0,806,Mathf.Max(bodyHeight,contentHeight),21);
            float measured=Mathf.Max(bodyHeight,noticeText.preferredHeight+12);noticeText.rectTransform.sizeDelta=new Vector2(806,measured);content.sizeDelta=new Vector2(content.sizeDelta.x,measured);
            Button("CommandNoticeDismiss",p,"已阅 · 继续安排",28,height-80,350,50,()=>AcknowledgeNotice(false));
            Button("CommandNoticeOpen",p,n.tab==0?"前往任务指挥":n.tab==1?"检查传递链条":n.tab==2?"查看成员情况":n.tab==5?"查看地区经营":"查看执行报告",398,height-80,474,50,()=>AcknowledgeNotice(true),true,true);
        }
        void AcknowledgeNotice(bool navigate)
        {
            var n=State.command.notices[0];State.command.notices.RemoveAt(0);
            if(navigate){commandTab=n.tab;if(n.tab==0){FocusCommandTask(n.task);}if(n.actor>=0)selectedAgent=n.actor;}
            modal="";Save(false);Draw();
        }
        void DrawCommandEnding()
        {
            var p=CommandModal(State.won?"灯火成网":"本次组织行动结束");
            Label(p,State.ending+"\n\n完成任务 "+State.command.completed+" / 送达情报 "+State.command.delivered+"\n成果 "+State.progress+" / 最高表身份 "+State.command.people.Max(x=>x.level)+"级\n\n下次开局会生成不同队员、任务顺序和关卡指标。",28,109,840,280,24);
            Button("CommandEndHome",p,"保存战绩，返回主菜单",28,489,844,58,ReturnHome,true,true);
        }
    }
}
