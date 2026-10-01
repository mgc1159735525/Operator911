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
            if(!HasActiveInsider())return "缺少机构内线：在人事档案建立任职身份，再配置传递链。";
            if(!State.command.routes.Any(r=>r.enabled))return "第一步：选择敌方机构内线，配齐接头人与交通员，再启用链条。";
            if(State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)!=""))return "联络链有失效岗位，请替换成员并恢复联系。";
            if(State.command.people.Any(p=>p.neglect>=3&&NeedsAttention(CommandRules.Agent(State,p.id))))return "有人生活事务积压：在人事页安排生活优先，或替换链条岗位。";
            if(State.command.tasks.Any(t=>t.status==0&&t.actor<0))return "还有待授权事项：比较人员与方案；也可留待后续，链条会自动执行。";
            return "任务已授权。检查岗位冲突后推进一夜，成员会自行执行并提交报告。";
        }
        void DrawCommandGame()
        {
            CommandRules.EnsureStrategy(State);
            Label(root,"明路 · 地下",32,28,228,35,25,TextColor);
            Label(root,"新京地区  /  机密",32,76,228,22,14,Muted);
            Label(root,"第 "+State.turn.ToString("00")+" 夜",286,28,234,35,25,TextColor);
            Label(root,"行动期限  "+State.config.Value("turns")+" 夜",286,76,234,22,14,Muted);
            Label(root,"行动成果",548,29,155,22,14,Muted);Label(root,State.progress.ToString("00"),548,58,155,32,22,TextColor);
            Label(root,"可用情报",720,29,155,22,14,Muted);Label(root,(State.intelligence-CommandRules.ReservedIntel(State))+" 份",720,58,155,32,22,TextColor);
            Label(root,"可用经费",893,29,230,22,14,Muted);Label(root,(State.money-CommandRules.ReservedMoney(State))+" 银两",893,58,230,32,22,TextColor);
            Button("Save",root,"保存",1150,29,88,32,Save);Button("Load",root,"读档",1240,29,88,32,Load,System.IO.File.Exists(SavePath));
            Button("CommandHome",root,"返回",1330,29,78,32,ReturnHome);
            Button("CommandHelp",root,"指南",1410,29,70,32,()=>{modal="commandHelp";Draw();});Button("CommandMenu",root,"菜单",1490,29,78,32,()=>{modal="menu";Draw();});
            Label(root,"追查 "+State.command.strategy.trace+" / "+State.command.strategy.config.raid+"     民心 "+State.command.strategy.hearts,1150,76,418,22,14,State.command.strategy.trace>=State.command.strategy.config.raid-15?Red:Muted);
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
            Button("CommandEnd",root,"结束今夜",1260,855,308,37,()=>{modal="commandConfirm";Draw();},!State.ended&&State.command.eventKind==0);
            if(State.command.eventKind!=0)DrawCommandEvent();
            else if(State.ended){if(modal=="endingReview")DrawEndingReview();else DrawCommandEnding();}
            else if(State.command.notices!=null&&State.command.notices.Count>0)DrawCommandNotice();
            else if(modal=="memberDetails")DrawMemberDetails();
            else if(modal=="districtDetails")DrawDistrictDetails();
            else if(modal=="routeDetails")DrawRouteDetails();
            else if(modal=="strategyDetails")DrawStrategyDetails();
            else if(modal=="commandOverview")DrawCommandOverview();
            else if(modal=="menu")DrawCommandMenu();
            else if(modal=="commandHelp")DrawCommandGuide();
            else if(modal=="appearance")DrawModal();
            else if(modal=="compareCandidates")DrawCandidateComparison();
            else if(modal=="institutionPicker")DrawInstitutionPicker();
            else if(modal=="routePicker")DrawRoutePicker();
            else if(modal=="routeReplace")DrawRouteReplacement();
            else if(modal=="missionPlan")DrawMissionPlan();
            else if(modal=="commandConfirm")DrawCommandConfirm();
            else if(modal!=""){modal="";}
        }
        void DrawCommandTasks(Transform panel)
        {
            Label(panel,"任务簿",28,20,560,42,32,Gold);
            Label(panel,State.command.tasks.Count(t=>t.status==0)+" 项待办",640,30,170,24,15,Muted);
            int urgent=State.command.tasks.Count(t=>t.status==0&&t.actor<0&&t.deadline<=State.turn);
            int due=State.command.tasks.Count(t=>t.status==0&&t.deadline<=State.turn);
            Label(panel,"待签发 "+State.command.tasks.Count(t=>t.status==0&&t.actor<0)+" 项 · "+(urgent>0?urgent+" 项今夜截止待安排":due>0?due+" 项今夜截止，已安排":"今夜暂无到期任务"),28,68,782,24,16,urgent>0?Red:Muted);
            string[] filters={"全部","待授权","已授权","两夜内截止"};
            for(int f=0;f<4;f++){int value=f;Button("TaskFilter_"+f,panel,filters[f],28+f*198,104,188,32,()=>{taskFilter=value;resetCommandTaskScroll=true;Draw();},true,taskFilter==f);}
            var tasks=State.command.tasks.Where(t=>t.status==0&&(taskFilter==0||taskFilter==1&&t.actor<0||taskFilter==2&&t.actor>=0||taskFilter==3&&t.deadline<=State.turn+1)).OrderBy(t=>t.id==commandTask?0:t.operation==2?1:2).ThenBy(t=>t.deadline).ToList();
            var content=Scroll(panel,"CommandTasks",28,156,782,394,Mathf.Max(100,tasks.Count*130));int i=0;
            if(tasks.Count==0)Label(content,"当前筛选没有任务。可切换「全部」，或到地区经营发起委托；链条与日常仍会运行。",12,15,750,70,20,Teal);
            foreach(var t in tasks)
            {
                int index=i++;int y=index*130;var card=Rect("CommandTaskCard",content,0,y,764,122);bool focus=t.id==commandTask||commandTask<0&&index==0;
                Rule(card,0,121,764);Label(card,(index+1).ToString("00"),0,10,28,28,15,Muted);
                if(focus){var marker=Box("TaskSelection",card,29,8,2,27,Accent);marker.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;}
                var title=Label(card,t.title,40,3,526,36,focus?25:21,Gold);title.font=titleFont??font;Label(card,(t.deadline<=State.turn?"今夜截止":"剩 "+(t.deadline-State.turn+1)+" 夜"),606,9,154,25,16,t.deadline<=State.turn?Red:Muted);
                Label(card,State.config.District(t.district).name+" · "+(t.actor<0?"待签发":CommandName(t.actor)+"执行")+(t.stepsRequired>1?" · 阶段 "+t.stepsDone+"/"+t.stepsRequired:""),40,47,720,25,16,t.actor<0?Muted:Teal);
                Button("CommandTask_"+t.id,card,t.actor<0?"打开任务  ›":"查看已签发方案  ›",28,82,224,30,()=>OpenCommandTask(t.id));
                if(t.actor<0)Button("CommandRecommend_"+t.id,card,"推荐人选  ›",582,82,178,30,()=>OpenCommandTask(t.id,true));
            }
        }
        void DrawCommandAdvice()
        {
            if(commandTab==3){DrawDistrictContext();return;}
            var p=Box("CommandAdvice",root,1146,122,422,376,Ink);var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,a.id);
            Label(p,"当前成员",24,8,374,22,14,Muted);
            Portrait(p,a,24,48,52,76);Label(p,a.name,96,48,302,32,22,TextColor);
            Label(p,member.profession+" · "+member.level+"级",96,91,302,25,16,Muted);
            Label(p,"体力 "+a.stamina+"    精神 "+a.spirit+"    暴露 "+a.exposure,24,144,374,24,15,a.exposure>=State.command.strategy.config.warning?Red:Muted);
            Button("CommandMemberOpen",p,"人物档案  ›",12,180,386,32,()=>{modal="memberDetails";Draw();});
            var t=State.command.tasks.FirstOrDefault(tk=>tk.id==commandTask&&tk.status==0);
            if(commandTab==0&&t!=null)
            {
                Rule(p,24,232,374);Label(p,t.title,24,252,374,29,19,TextColor);
                Label(p,t.actor<0?"待签发":"已签发给 "+CommandName(t.actor),24,290,374,23,15,t.actor<0?Muted:Teal);
                Button("CommandPlan",p,"任务与方案  ›",12,328,386,32,()=>OpenCommandTask(t.id));
            }
            else
            {
                Rule(p,24,232,374);
                Button("CommandOverview",p,"组织概况  ›",12,252,386,32,()=>{modal="commandOverview";Draw();});
                Button("CommandHelpRoute",p,"检查联络网  ›",12,300,386,32,()=>{commandTab=1;Draw();});
            }
        }
        void DrawCommandRoutes(Transform panel)
        {
            Label(panel,"联络网",28,20,782,42,32,Gold);
            Label(panel,"情报来源须有敌方机构任职身份；接头与外送可由普通成员承担。",28,76,782,26,16,Muted);
            int i=0;
            foreach(var r in State.command.routes)
            {
                int y=120+i++*208;Rule(panel,28,y,782);
                Label(panel,(r.id+1).ToString("00")+"   "+(r.source<0?"未指定机构来源":CommandRules.Employment(CommandRules.Person(State,r.source))),28,y+16,620,29,21,Gold);
                Label(panel,r.enabled?"运行中":"未启用",680,y+18,130,24,14,r.enabled?Teal:Muted);
                for(int slot=0;slot<3;slot++)
                {
                    int n=slot,id=slot==0?r.source:slot==1?r.liaison:r.courier;
                    var role=Button("RouteSlot_"+r.id+"_"+slot,panel,"",28+slot*264,y+57,254,64,()=>OpenRoutePicker(r,n));
                    Label(role.transform,new[]{"内线 · 情报","接头人 · 说服","交通员 · 潜行"}[slot],14,7,217,21,13,Muted);
                    Label(role.transform,id<0?"选择成员  ＋":CommandName(id)+"  ›",14,31,217,29,20,Gold);
                    if(slot<2)Label(panel,"›",282+slot*264,y+77,10,25,17,Muted);
                }
                string error=CommandRules.RouteError(State,r);
                Label(panel,CompactRouteStatus(r),28,y+170,580,35,14,error==""?Muted:Red);
                Button("RouteDetails_"+r.id,panel,"待接 "+r.waiting+"  →  中继 "+r.atRelay+"  →  已送达 "+r.delivered+"    详情 ›",16,y+132,596,30,()=>{detailRouteId=r.id;modal="routeDetails";Draw();});
                Button("RouteEnable_"+r.id,panel,r.enabled?"暂停":"启用此线",650,y+138,160,38,()=>{r.enabled=!r.enabled;r.last=r.enabled?"已启用：内线隔夜尝试产出，接头与外送自动推进。":"已暂停，成员按其他任务或生活安排工作。";Save(false);toast=r.enabled?"链条已启用：后续夜晚自动判定，任务优先占用人手。":"链条已暂停，成员可以生活与休养。";Draw();},r.enabled||error=="");
            }
            Label(panel,"传递至少跨三夜 · 更换岗位清空积压 · 暂停保留积压，6夜未送达失效",28,550,782,22,13,Muted);
        }
        void DrawCommandPeople(Transform panel)
        {
            var a=CommandRules.Agent(State,selectedAgent);var p=CommandRules.Person(State,a.id);
            DrawLook(panel,a.appearance,28,28,208,312);
            Label(panel,a.name,264,28,546,42,32,Gold);
            Label(panel,"代号「"+p.code+"」 · "+p.profession+" "+p.level+"级",264,84,546,26,18,Muted);
            Label(panel,CommandRules.Employment(p),264,122,546,27,19,TextColor);
            Label(panel,MemberStatus(a),264,173,546,29,20,a.exposure>=State.command.strategy.config.warning?Red:Teal);
            Rule(panel,264,220,546);
            Label(panel,"情报 "+a.intel+"     潜行 "+a.stealth+"     说服 "+a.persuade+"     组织 "+a.organize,264,244,546,28,18,TextColor);
            Button("CommandPeopleDetails",panel,"个人档案 / 生活与任职  ›",252,300,558,40,()=>{modal="memberDetails";Draw();});
            Label(panel,"从下方名册切换成员，在个人档案调整生活与任职安排。",28,382,782,28,16,Muted);
            Rule(panel,28,452,782);
            Label(panel,State.agents.Count>=State.command.config.capacity?"已达到本局人数上限。":State.money-CommandRules.ReservedMoney(State)<12?"招募需12可用银两，当前经费不足。":"新成员的身份、能力与外观随机。",28,472,782,26,15,Muted);
            ActionTicket("CommandRecruit",panel,"接纳新成员","12 银两",28,516,374,42,()=>{State.money-=12;CommandRules.AddPerson(State);Save(false);toast="新人已到场，请检查身份能力并安排岗位。";Draw();},State.money-CommandRules.ReservedMoney(State)>=12&&State.agents.Count<State.command.config.capacity);
            Button("CommandPeopleAppearance",panel,"编辑外观  ›",428,516,382,42,()=>OpenAppearance(a.id));
        }
        void DrawCommandMap(Transform panel)
        {
            if(!State.config.districts.Any(d=>d.id==selectedDistrict))selectedDistrict=State.config.districts[0].id;
            var raw=Rect("CommandMap",panel,0,0,838,578).gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.texture=map;raw.color=new Color(.65f,.65f,.65f);raw.raycastTarget=false;
            foreach(var d in State.config.districts)
            {
                var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==d.id).ToList();
                bool urgent=tasks.Any(t=>t.deadline<=State.turn&&t.actor<0);
                string status="待派 "+tasks.Count(t=>t.actor<0)+" · 已派 "+tasks.Count(t=>t.actor>=0);
                var pin=Button("CommandDistrict_"+d.id,panel,(urgent?"急 · ":"")+d.name+"\n"+status,d.x*650+10,d.y*445+25,168,56,()=>{selectedDistrict=d.id;modal="districtDetails";Draw();},true,d.id==selectedDistrict);
                if(urgent){var colors=pin.colors;colors.normalColor=Accent;colors.highlightedColor=Hex("AD5141");colors.pressedColor=Hex("753329");colors.selectedColor=Accent;pin.colors=colors;pin.image.CrossFadeColor(Accent,0,true,true);pin.GetComponentInChildren<UnityEngine.UI.Text>().color=Paper;}
                if(d.id==selectedDistrict){Border(pin.transform,-3,-3,174,62,Paper);Border(pin.transform,-4,-4,176,64,PaperInk);}
            }
            Box("MapNote",panel,16,518,806,48,Ink);Label(panel,"点选城区打开当地档案。朱红标记：有任务今夜截止。",28,530,775,28,17,Gold);
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
            Label(p,affected.name+" / "+CommandRules.Person(State,affected.id).profession,140,104,732,30,20,Gold);
            Label(p,c.eventText,140,154,732,105,20);
            string[] protect={"","援助并准假 · 6银","分散休整 · 10银","暂停岗位，安排谈话"};
            string[] accept={"","准假，暂不拨款","维持工作，承担风险","继续信任，允许留任"};
            string explanation=c.eventKind==1?"两项都会准假一夜；援助额外恢复信任。链条会等待此人，不会自动越级传递。":c.eventKind==2?"分散休整：链条暂停今夜、压力−12。\n维持工作：全员暴露+18、压力+8，产生应急任务。":"谈话：该成员今夜离岗，信任+15。\n继续信任：有叛离风险，信任不足45时更高；只冲击本人所在的联络链。";
            Label(p,explanation,28,290,844,125,18,Muted);
            Button("CommandEventSafe",p,protect[c.eventKind],28,492,408,52,()=>{toast=CommandRules.Choose(State,true);Save(false);Draw();},State.money-CommandRules.ReservedMoney(State)>=(c.eventKind==1?6:c.eventKind==2?10:0));
            Button("CommandEventRisk",p,accept[c.eventKind],456,492,416,52,()=>{toast=CommandRules.Choose(State,false);Save(false);Draw();});
        }
        void DrawCommandNotice()
        {
            var n=State.command.notices[0];
            int lines=n.body.Split('\n').Sum(line=>Mathf.Max(1,Mathf.CeilToInt(line.Length/36f)));
            float contentHeight=lines*33+16,bodyHeight=Mathf.Clamp(contentHeight,120,335),height=bodyHeight+235;
            var p=CommandModal(n.title,height);
            Label(p,"事项已发生 · 查看不消耗行动，也不推进夜晚",28,88,844,24,15,Muted);
            var content=Scroll(p,"CommandNoticeBody",28,125,844,bodyHeight,Mathf.Max(bodyHeight,contentHeight));
            var noticeText=Label(content,n.body,0,0,806,Mathf.Max(bodyHeight,contentHeight),21);
            float measured=Mathf.Max(bodyHeight,noticeText.preferredHeight+12);noticeText.rectTransform.sizeDelta=new Vector2(806,measured);content.sizeDelta=new Vector2(content.sizeDelta.x,measured);
            Button("CommandNoticeDismiss",p,"已阅 · 继续安排",28,height-80,350,50,()=>AcknowledgeNotice(false));
            Button("CommandNoticeOpen",p,n.tab==0?"查看任务簿":n.tab==1?"检查联络网":n.tab==2?"查看成员情况":n.tab==5?"查看地方事务":"查看执行报告",398,height-80,474,50,()=>AcknowledgeNotice(true),true,true);
        }
        void AcknowledgeNotice(bool navigate)
        {
            var n=State.command.notices[0];State.command.notices.RemoveAt(0);
            if(navigate)
            {
                if(n.actor>=0)selectedAgent=n.actor;
                if(n.tab==0)
                {
                    if(State.command.tasks.Any(t=>t.id==n.task&&t.status==0))FocusCommandTask(n.task);
                    else{modal="";commandTab=0;}
                }
                else{modal="";commandTab=n.tab;}
            }
            Save(false);Draw();
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
