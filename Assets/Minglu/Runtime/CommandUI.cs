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
            if(State.agents.Any(a=>NeedsAttention(a)&&a.exposure>=State.command.strategy.config.warning))return "有人被重点追查：前往人事页安排生活优先，并检查链条替补。";
            if(State.command.tasks.Any(t=>t.status==0&&t.operation==2&&t.actor<0))return "有人被捕：优先比较救援人选，按任务阶段逐夜推进，留意截止夜。";
            if(!State.command.routes.Any(r=>r.enabled))return "第一步：打开传递链条，选择三名成员并启用。";
            if(State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)!=""))return "联络链有失效岗位，请替换成员并恢复联系。";
            if(State.command.people.Any(p=>p.neglect>=3&&NeedsAttention(CommandRules.Agent(State,p.id))))return "有人生活事务积压：在人事页安排生活优先，或替换链条岗位。";
            if(State.command.tasks.Any(t=>t.status==0&&t.actor<0))return "还有待授权事项：比较人员与方案；也可留待后续，链条会自动执行。";
            return "任务已授权。检查岗位冲突后推进一夜，成员会自行执行并提交报告。";
        }
        void DrawCommandGame()
        {
            CommandRules.EnsureStrategy(State);
            Label(root,"明路 · 地下",32,24,238,48,32,Gold);
            Label(root,"新京地区  /  机密",33,78,228,24,14,Muted);
            Label(root,"第 "+State.turn.ToString("00")+" 夜",286,26,234,48,37,Gold);
            Label(root,"行动期限  "+State.config.Value("turns")+" 夜",288,80,225,24,14,Muted);
            Label(root,"行动成果",548,28,155,22,14,Muted);Label(root,State.progress.ToString("00"),548,54,155,43,28,Gold);
            Label(root,"可用情报",720,28,155,22,14,Muted);Label(root,(State.intelligence-CommandRules.ReservedIntel(State))+" 份",720,54,155,43,28,Gold);
            Label(root,"可用经费",893,28,230,22,14,Muted);Label(root,(State.money-CommandRules.ReservedMoney(State))+" 银两",893,54,230,43,28,Gold);
            Button("Save",root,"保存",1150,29,88,32,Save);Button("Load",root,"读档",1240,29,88,32,Load,System.IO.File.Exists(SavePath));
            Button("CommandHome",root,"返回",1330,29,78,32,ReturnHome);
            Button("CommandHelp",root,"指南",1410,29,70,32,()=>{modal="commandHelp";Draw();});Button("CommandMenu",root,"菜单",1490,29,78,32,()=>{modal="menu";Draw();});
            Label(root,"追查 "+State.command.strategy.trace+" / "+State.command.strategy.config.raid+"     民心 "+State.command.strategy.hearts,1155,81,410,24,15,Muted);
            Rule(root,32,108,1536);
            string[] tabs={"任务簿","联络网","人事档案","城区地图","值夜报告","地方事务"};
            for(int i=0;i<tabs.Length;i++){int n=i;Button("CommandTab_"+i,root,tabs[i],32,130+i*47,228,39,()=>{commandTab=n;Draw();},true,commandTab==i);}
            DrawNoirBrief();DrawNoirRoster();
            Box("PageShadow",root,290,127,838,578,new Color(0,0,0,.2f));
            var panel=Box("CommandCenter",root,286,122,838,578,Panel);
            if(commandTab==0)DrawCommandTasks(panel);else if(commandTab==1)DrawCommandRoutes(panel);else if(commandTab==2)DrawCommandPeople(panel);else if(commandTab==3)DrawCommandMap(panel);else if(commandTab==4){if(!(State.ended&&modal=="endingReview"))DrawCommandReport(panel);}else DrawStrategy(panel);
            DrawCommandAdvice();
            Rule(root,32,847,1536);
            Label(root,toast,32,862,958,30,15,Muted);
            Button("CommandNextAction",root,"处理下一项  ›",1010,855,230,37,GoNextCommand);
            Button("CommandEnd",root,"签发指令  /  结束今夜",1260,855,308,37,()=>{modal="commandConfirm";Draw();},!State.ended&&State.command.eventKind==0,true);
            if(State.command.eventKind!=0)DrawCommandEvent();
            else if(State.ended){if(modal=="endingReview")DrawEndingReview();else DrawCommandEnding();}
            else if(State.command.notices!=null&&State.command.notices.Count>0)DrawCommandNotice();
            else if(modal=="memberDetails")DrawMemberDetails();
            else if(modal=="commandOverview")DrawCommandOverview();
            else if(modal=="menu")DrawCommandMenu();
            else if(modal=="commandHelp")DrawCommandGuide();
            else if(modal=="appearance")DrawModal();
            else if(modal=="compareCandidates")DrawCandidateComparison();
            else if(modal=="routePicker")DrawRoutePicker();
            else if(modal=="routeReplace")DrawRouteReplacement();
            else if(modal=="missionPlan")DrawMissionPlan();
            else if(modal=="commandConfirm")DrawCommandConfirm();
            else if(modal!=""){modal="";}
        }
        void DrawCommandTasks(Transform panel)
        {
            Label(panel,"任务簿",26,16,560,40,29,Gold);
            Label(panel,State.command.tasks.Count(t=>t.status==0)+" 项待办",645,25,155,26,16,Muted);
            Label(panel,"选择任务，比较成员，再签发行动方案。",26,61,772,28,16,Muted);
            string[] filters={"全部","待授权","已授权","两夜内截止"};
            for(int f=0;f<4;f++){int value=f;Button("TaskFilter_"+f,panel,filters[f],20+f*198,92,187,32,()=>{taskFilter=value;resetCommandTaskScroll=true;Draw();},true,taskFilter==f);}
            var tasks=State.command.tasks.Where(t=>t.status==0&&(taskFilter==0||taskFilter==1&&t.actor<0||taskFilter==2&&t.actor>=0||taskFilter==3&&t.deadline<=State.turn+1)).OrderBy(t=>t.id==commandTask?0:t.operation==2?1:2).ThenBy(t=>t.deadline).ToList();
            var content=Scroll(panel,"CommandTasks",18,137,802,431,Mathf.Max(100,tasks.Count*164));int i=0;
            if(tasks.Count==0)Label(content,"当前筛选没有任务。可切换「全部」，或到地区经营发起委托；链条与日常仍会运行。",12,15,750,70,20,Teal);
            foreach(var t in tasks)
            {
                int index=i++;int y=index*164;var card=Rect("CommandTaskCard",content,0,y,780,156);
                Rule(card,0,155,780);Label(card,(index+1).ToString("00"),8,13,41,30,18,Muted);
                if(t.id==commandTask){var marker=Box("TaskSelection",card,0,10,3,132,Accent);marker.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;}
                Label(card,t.title,55,7,520,33,24,Gold);Label(card,(t.deadline<=State.turn?"今夜截止":"剩"+(t.deadline-State.turn+1)+"夜"),592,12,173,26,16,t.deadline==State.turn?Red:Muted);
                Label(card,State.config.District(t.district).name+" · "+t.origin.Split('：')[0]+"    "+CommandRules.Skills[t.skill]+" / "+t.required+"成功 / 身份"+t.level+"级"+(t.profession==""?"":" / "+t.profession),55,49,710,25,16,Muted);
                Label(card,(t.stepsRequired>1?"阶段 "+t.stepsDone+"/"+t.stepsRequired+" · ":"")+CommandRules.TaskReward(t)+" · "+(t.actor<0?"待授权":CommandName(t.actor)+" / "+CommandRules.Plans[t.plan]),55,80,710,27,15,Teal);
                Button("CommandTask_"+t.id,card,"查看任务  ›",42,116,158,32,()=>{commandTask=t.id;resetCommandTaskScroll=true;Draw();},true,t.id==commandTask);
                Button("CommandRecommend_"+t.id,card,"推荐人选  ›",205,116,158,32,()=>{commandTask=t.id;resetCommandTaskScroll=true;var candidate=State.agents.Where(a=>CommandRules.TaskError(State,a.id,t)=="").OrderByDescending(a=>CommandRules.TaskDice(State,a,t)).ThenBy(a=>CommandRole(a.id)=="机动成员"?0:1).FirstOrDefault();if(candidate!=null){selectedAgent=candidate.id;toast="已推荐"+candidate.name+"。请检查右侧成功率和岗位冲突，再授权。";}else toast="暂无满足要求的人选：可先培养身份、安排休养，或选择其他任务。";Draw();});
                if(t.actor>=0)Button("CommandCancel_"+t.id,card,"撤回授权",391,116,135,32,()=>{t.actor=-1;toast="授权已撤回，释放成员及预留资源。";Save(false);Draw();},t.actor>=0);
                Button("CommandCompare_"+t.id,card,"比较人选  ›",620,116,148,32,()=>{commandTask=t.id;resetCommandTaskScroll=true;modal="compareCandidates";Draw();});
            }
        }
        void DrawCommandAdvice()
        {
            var taskSelected=commandTab==0&&State.command.tasks.Any(tk=>tk.id==commandTask&&tk.status==0);
            var p=Box("CommandAdvice",root,1146,122,422,taskSelected?578:350,Ink);var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,a.id);
            Label(p,"指挥签发",20,0,374,26,15,Muted);
            Portrait(p,a,20,40,61,90);Label(p,a.name,102,43,295,39,28,Gold);
            Label(p,member.profession+" · "+member.level+"级",103,88,289,27,18,TextColor);
            Label(p,"体力 "+a.stamina+"    精神 "+a.spirit+"    暴露 "+a.exposure,20,148,379,26,17,Muted);
            Button("CommandMemberOpen",p,"查看人物档案  ›",20,185,379,34,()=>{modal="memberDetails";Draw();});
            var t=State.command.tasks.FirstOrDefault(tk=>tk.id==commandTask&&tk.status==0);
            if(commandTab==0&&t!=null)
            {
                string error=CommandRules.TaskError(State,a.id,t);int dice=CommandRules.TaskDice(State,a,t);double chance=DispatchRules.Probability(dice,t.required);
                Rule(p,20,232,379);Label(p,t.title,20,247,379,35,24,Gold);Label(p,t.body,20,292,379,57,17);
                Label(p,(chance*100).ToString("0.#")+"%",20,354,172,54,38,Gold);
                Label(p,"成功概率\n"+dice+"骰 / 需"+t.required+"个成功",203,357,196,52,16,Muted);
                Label(p,CommandRules.Plans[t.plan]+"  ·  体力−14  /  暴露+"+CommandRules.TaskExposure(State,a,t),20,415,379,28,16,Muted);
                Button("CommandPlan",p,"调整方案与投入  ›",20,452,379,34,()=>{modal="missionPlan";Draw();});
                Label(p,error!=""?error:CommandRole(a.id)!="机动成员"?"本夜优先执行任务，联络岗位将等待。":"授权后预留资源，结束今夜时执行。",20,495,379,40,15,error==""?Muted:Red);
                Button("CommandAuthorize",p,"签发给 "+a.name,20,540,379,38,()=>{toast=CommandRules.Assign(State,a.id,t.id);if(toast==""){toast="已授权并预留资源；推进一夜后执行。";Save(false);}Draw();},error=="",true);
            }
            else
            {
                Rule(p,20,232,379);
                Button("CommandOverview",p,"组织概况  ›",20,252,379,38,()=>{modal="commandOverview";Draw();});
                Button("CommandHelpRoute",p,"检查联络网  ›",20,302,379,38,()=>{commandTab=1;Draw();});
            }
        }
        void DrawCommandRoutes(Transform panel)
        {
            Label(panel,"联络网",26,16,570,40,29,Gold);Label(panel,"观察  →  接头  →  外送",507,27,296,28,16,Muted);
            Label(panel,"点选岗位安排成员。启用后，成员会按各自的周期行动。",26,63,772,29,16,Muted);
            int i=0;
            foreach(var r in State.command.routes)
            {
                int y=112+i++*211;Rule(panel,26,y,772);
                Label(panel,(r.id+1).ToString("00")+"   联络链",26,y+14,500,30,21,Gold);
                Label(panel,r.enabled?"运行中":"未启用",677,y+17,125,25,15,r.enabled?Teal:Muted);
                for(int slot=0;slot<3;slot++)
                {
                    int n=slot,id=slot==0?r.source:slot==1?r.liaison:r.courier;
                    var role=Button("RouteSlot_"+r.id+"_"+slot,panel,"",26+slot*260,y+53,245,64,()=>OpenRoutePicker(r,n));
                    Label(role.transform,new[]{"内线 · 情报","接头人 · 说服","交通员 · 潜行"}[slot],14,7,217,21,13,Muted);
                    Label(role.transform,id<0?"选择成员  ＋":CommandName(id)+"  ›",14,31,217,29,20,Gold);
                    if(slot<2)Label(panel,"›",272+slot*260,y+73,14,25,19,Muted);
                }
                string error=CommandRules.RouteError(State,r);
                Label(panel,"待接 "+r.waiting+"   →   中继 "+r.atRelay+"   →   已送达 "+r.delivered,26,y+128,551,27,17,Teal);
                Label(panel,RouteForecast(r),26,y+163,551,45,15,error==""?Muted:Red);
                Button("RouteEnable_"+r.id,panel,r.enabled?"暂停":"启用此线",642,y+139,156,38,()=>{r.enabled=!r.enabled;r.last=r.enabled?"已启用：内线隔夜尝试产出，接头与外送自动推进。":"已暂停，成员按其他任务或生活安排工作。";Save(false);toast=r.enabled?"链条已启用：后续夜晚自动判定，任务优先占用人手。":"链条已暂停，成员可以生活与休养。";Draw();},r.enabled||error=="",true);
            }
            Label(panel,"传递至少跨三夜。换人须重新启用；暂停保留积压，6夜未送达会失效。",26,548,772,25,14,Muted);
        }
        void DrawCommandPeople(Transform panel)
        {
            var a=CommandRules.Agent(State,selectedAgent);var p=CommandRules.Person(State,a.id);
            DrawLook(panel,a.appearance,20,22,230,360);
            Label(panel,a.name+" / 代号「"+p.code+"」",270,23,540,38,25,Gold);
            var details=Scroll(panel,"PersonDetails_"+a.id,270,83,540,247,248);
            var personText=Label(details,p.age+"岁 · 性情"+p.temper+"\n表身份："+p.profession+" Lv."+p.level+"\n能力：情报"+a.intel+" 潜行"+a.stealth+" 说服"+a.persuade+" 组织"+a.organize+"\n地下岗位："+CommandRole(a.id)+"\n生活牵挂："+p.life+"\n身份经验："+p.xp+" / "+(State.command.config.coverXP*p.level)+"\n组织信任："+p.loyalty+" / 100\n近期情况："+p.last,0,0,514,247,20);
            float detailsHeight=Mathf.Max(247,personText.preferredHeight+12);personText.rectTransform.sizeDelta=new Vector2(514,detailsHeight);details.sizeDelta=new Vector2(details.sizeDelta.x,detailsHeight);
            string[] policies={"平衡安排","生活优先","主动发现"};
            for(int i=0;i<3;i++){int n=i;Button("LifePolicy_"+i,panel,policies[i],270+i*181,343,170,41,()=>{CommandRules.SetPolicy(State,p.id,n);Save(false);toast="政策已修改："+policies[n]+"。生活优先会暂停该成员的任务和链条岗位。";Draw();},true,p.policy==i);}
            Label(panel,"空闲时自动工作、处理家事并恢复体力精神，累计经验提升表身份。连续地下工作3夜后，生活积压会影响状态与信任。主动发现有机会提交新任务。",20,411,790,85,19,Muted);
            Label(panel,State.agents.Count>=State.command.config.capacity?"招募已满员：达到本局人数上限。":State.money-CommandRules.ReservedMoney(State)<12?"招募需12可用银两；预留预算不能用于招募。":"可招募：新人身份与能力随机，先检查再安排。",20,490,790,26,16,Muted);
            Button("CommandRecruit",panel,"接纳随机新成员 · 12银两",20,520,385,43,()=>{State.money-=12;CommandRules.AddPerson(State);Save(false);toast="新人已到场，请检查身份能力并安排岗位。";Draw();},State.money-CommandRules.ReservedMoney(State)>=12&&State.agents.Count<State.command.config.capacity,true);
            Button("CommandEditLook",panel,"编辑外观 / 保留角色能力",423,520,375,43,()=>OpenAppearance(a.id));
        }
        void DrawCommandMap(Transform panel)
        {
            var raw=Rect("CommandMap",panel,0,0,838,588).gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.texture=map;raw.color=new Color(.65f,.65f,.65f);raw.raycastTarget=false;
            foreach(var d in State.config.districts)
            {
                var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==d.id).ToList();
                bool urgent=tasks.Any(t=>t.deadline<=State.turn&&t.actor<0);
                string status=urgent?"! 今夜截止":tasks.Count==0?"暂无待办":"待派 "+tasks.Count(t=>t.actor<0)+" · 已派 "+tasks.Count(t=>t.actor>=0);
                var pin=Button("CommandDistrict_"+d.id,panel,d.name+" / 风险+"+Mathf.Clamp(d.risk,0,6)+"\n"+status,d.x*650+10,d.y*445+25,168,56,()=>{var t=tasks.OrderBy(tk=>tk.actor>=0?1:0).ThenBy(tk=>tk.deadline).FirstOrDefault();if(t!=null){FocusCommandTask(t.id);toast="已定位该区最紧急待办；比较人员后授权。";}else toast=d.name+"当前没有待办，可查看其他城区或地区经营。";Draw();});
                if(urgent){var colors=pin.colors;colors.normalColor=Accent;colors.highlightedColor=Hex("AD5141");colors.pressedColor=Hex("753329");colors.selectedColor=Accent;pin.colors=colors;pin.image.CrossFadeColor(Accent,0,true,true);pin.GetComponentInChildren<UnityEngine.UI.Text>().color=Paper;}
            }
            Box("MapNote",panel,16,508,806,64,Ink);Label(panel,"点选城区查看任务。风险越高，成员行动时的暴露越大。",28,522,775,44,18,Gold);
        }
        void DrawCommandReport(Transform panel){DrawNightReport(panel);}
        Transform CommandModal(string title,float height=600)
        {
            var shade=Box("CommandShade",root,0,0,1600,900,new Color(0,0,0,.76f));Box("DialogShadow",shade,359,(900-height)/2+10,900,height,new Color(0,0,0,.24f));var p=Box("CommandModal",shade,350,(900-height)/2,900,height,Panel);Box("CaseRibbon",p,28,0,64,5,Accent);Label(p,title,28,24,840,45,30,Gold);Rule(p,28,73,844);return p;
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
            Button("CommandExecute",p,"下达指示 · 成员执行",348,665,524,50,()=>{CommandRules.Resolve(State);reportNight=-1;reportAll=false;commandTab=4;modal="";toast="执行完毕：查看报告，处理待决事件，再安排下一夜。";Save(false);Draw();},true,true);
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
            Button("CommandNoticeOpen",p,n.tab==0?"查看任务簿":n.tab==1?"检查联络网":n.tab==2?"查看成员情况":n.tab==5?"查看地方事务":"查看执行报告",398,height-80,474,50,()=>AcknowledgeNotice(true),true,true);
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
            Label(p,GateProgress(),28,389,844,69,20,Teal);
            Button("CommandEndReview",p,"查看每夜报告",28,489,322,58,()=>{modal="endingReview";reportNight=-1;Draw();});
            Button("CommandEndHome",p,"保存战绩，返回主菜单",370,489,502,58,ReturnHome,true,true);
        }
    }
}
