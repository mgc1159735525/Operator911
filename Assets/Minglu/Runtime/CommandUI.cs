using System;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        int commandTab=3,commandTask=-1;
        string CommandRole(int id)
        {
            var route=State.command.routes.FirstOrDefault(r=>r.enabled&&new[]{r.source,r.liaison,r.courier}.Contains(id));
            return route==null?"机动成员":route.source==id?"潜伏内线":route.liaison==id?"接头联络":"交通外送";
        }
        string CommandName(int id){return id<0?"未指定":CommandRules.Agent(State,id).name;}
        sealed class CommandStep
        {
            public string message,label,dialog="";
            public int page=3,actor=-1,task=-1;
            public bool ready,enabled=true;
        }
        CommandStep TaskStep(CommandTask task,string reason,string label)
        {
            return new CommandStep{message="「"+task.title+"」"+reason,label=label,task=task.id};
        }
        // The visible advice and its destination must come from the same decision.
        CommandStep NextCommandStep()
        {
            if(State.ended)return new CommandStep{message="本局已结束，可在结局回看每夜报告。",label="本局已结束",enabled=false};
            if(State.command.eventKind!=0)return new CommandStep{message="先处理眼前的突发事件，组织正等待你的决定。",label="等待事件决定",enabled=false};
            var pending=State.command.tasks.Where(t=>t.status==0&&t.actor<0).OrderBy(t=>t.deadline).ToList();
            var feasible=pending.Where(t=>CommandRules.CanStaffTask(State,t)).ToList();
            var rescue=feasible.FirstOrDefault(t=>t.operation==2);
            if(rescue!=null)return TaskStep(rescue,"：救援尚待授权，先比较人选。","安排救援  ›");
            var watched=State.agents.Where(a=>NeedsAttention(a)&&a.exposure>=State.command.strategy.config.warning).OrderByDescending(a=>a.exposure).FirstOrDefault();
            if(watched!=null)return new CommandStep{message=watched.name+"被重点追查：安排生活优先，并检查原岗位替补。",label="安排成员休养  ›",actor=watched.id,page=2,dialog="memberDetails"};
            if(State.turn==1)
            {
                if(State.command.tasks.Any(t=>t.status==0&&t.actor>=0))return new CommandStep{message="第一项指示已安排。结束今夜，观察成员执行与骰子结果；第二夜再建立联络链。",label="核对今夜安排  ›",dialog="commandConfirm",ready=true};
                var first=feasible.Where(t=>t.operation==0).OrderBy(t=>t.stepsRequired).ThenBy(t=>t.required).FirstOrDefault();
                if(first!=null)return TaskStep(first,"：先比较负责人并安排一项任务，再结束今夜看结果。","安排第一项任务  ›");
                return new CommandStep{message="今夜没有可派人的普通任务。先让成员生活与休养，核对安排后推进；查看不会消耗资源。",label="核对今夜安排  ›",dialog="commandConfirm",ready=true};
            }
            var lastChance=feasible.FirstOrDefault(t=>CommandRules.TaskUrgent(State,t));
            if(lastChance!=null)return TaskStep(lastChance,"："+CommandRules.TaskTimingText(State,lastChance)+"。先决定是否安排。","处理限期任务  ›");
            bool insider=HasActiveInsider();
            var appointment=State.command.tasks.FirstOrDefault(t=>t.operation==4&&t.status==0&&t.actor>=0);
            if(!insider&&appointment==null)
            {
                var job=feasible.FirstOrDefault(t=>t.operation==4);
                if(job!=null)return TaskStep(job,"：任职仍待授权，完成后才能接入情报链。","安排机构任职  ›");
                var member=State.agents.FirstOrDefault(a=>CommandRules.EmploymentError(State,a.id,1)==""&&!State.command.tasks.Any(t=>t.status==0&&t.actor==a.id));
                if(member!=null)return new CommandStep{message="缺少机构内线：为成员建立敌方机构任职，再配置传递链。",label="建立机构任职  ›",page=2,actor=member.id,dialog="memberDetails"};
            }
            if(insider)
            {
                var freeMembers=State.agents.Where(a=>CommandRules.Availability(State,a.id)==""&&!State.command.tasks.Any(t=>t.status==0&&t.actor==a.id)).ToList();
                bool canForm=freeMembers.Count>=3&&freeMembers.Any(a=>CommandRules.HasInstitution(CommandRules.Person(State,a.id)));
                if(canForm&&State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)!=""))return new CommandStep{message="联络链有失效岗位：替换成员，检查接头与外送是否能继续。",label="修复联络链  ›",page=1};
                if(!State.command.routes.Any(r=>r.enabled)&&canForm)return new CommandStep{message="第二步：配置机构内线 → 接头人 → 交通员。启用后自动跨夜传递，至少需要三夜。",label="配置联络网  ›",page=1};
            }
            var neglected=State.command.people.FirstOrDefault(p=>p.neglect>=3&&NeedsAttention(CommandRules.Agent(State,p.id)));
            if(neglected!=null)return new CommandStep{message=CommandName(neglected.id)+"的家事积压：安排轮休，再决定是否替换链条岗位。",label="处理成员家事  ›",page=2,actor=neglected.id,dialog="memberDetails"};
            int assigned=State.command.tasks.Count(t=>t.status==0&&t.actor>=0);
            if(assigned>0&&feasible.Count>0)return new CommandStep{message="今夜已安排 "+assigned+" 项；另有 "+feasible.Count+" 项可选待办。可以结束今夜，不必派满所有成员。",label="查看可选待办  ›",page=0,ready=true};
            if(feasible.Count>0)return TaskStep(feasible[0],"待授权：比较人选与方案，决定今夜是否执行。","安排待办任务  ›");
            string message=!insider&&appointment!=null?"机构任职已签发：推进一夜等待执行结果，无需重复安排任职。":State.command.tasks.Any(t=>t.status==0&&t.actor>=0)?"今夜任务已签发；核对岗位冲突后，由成员自行执行。":"暂无可推进的待授权任务；成员继续链条工作、日常生活与休养。";
            if(!State.agents.Any(a=>a.condition==Condition.Active))message="当前没有在岗成员；核对组织状态后推进结算。";
            if(pending.Any(t=>!CommandRules.TaskCanFinish(State,t)))message+=" 部分事项已来不及。";
            else if(pending.Count>feasible.Count)message+=" 其余事项当前无人满足条件，可休养或调整方案。";
            return new CommandStep{message=message,label="核对今夜安排  ›",dialog="commandConfirm",ready=true};
        }
        string CommandNext(){return NextCommandStep().message;}
        void DrawCommandGame()
        {
            CommandRules.EnsureStrategy(State);
            DrawDossierDesk();
            if(State.command.eventKind!=0)DrawCommandEvent();
            else if(State.ended){if(modal=="endingReview")DrawEndingReview();else DrawCommandEnding();}
            else if(State.command.notices!=null&&State.command.notices.Count>0)DrawCommandNotice();
            else if(modal=="memberDetails")DrawMemberDetails();
            else if(modal=="districtDetails")DrawDistrictDetails();
            else if(modal=="routeDetails")DrawRouteDetails();
            else if(modal=="strategyDetails")DrawStrategyDetails();
            else if(modal=="commandOverview")DrawCommandOverview();
            else if(modal=="menu")DrawCommandMenu();
            else if(modal=="commandSettings")DrawCommandPreferences();
            else if(modal=="commandHelp")DrawCommandGuide();
            else if(modal=="appearance")DrawModal();
            else if(modal=="compareCandidates")DrawCandidateComparison();
            else if(modal=="institutionPicker")DrawInstitutionPicker();
            else if(modal=="recruitment")DrawRecruitment();
            else if(modal=="routePicker")DrawRoutePicker();
            else if(modal=="routeReplace")DrawRouteReplacement();
            else if(modal=="missionPlan")DrawMissionPlan();
            else if(modal=="commandConfirm")DrawCommandConfirm();
            else if(modal!=""){modal="";}
            else if(commandTab!=3)DrawCommandBusiness();
        }
        void DrawCommandTasks(Transform panel)
        {
            Label(panel,"任务簿",28,20,560,42,32,Gold);
            Label(panel,State.command.tasks.Count(t=>t.status==0)+" 项待办",640,30,170,24,15,Muted);
            int urgent=State.command.tasks.Count(t=>t.status==0&&t.actor<0&&CommandRules.TaskUrgent(State,t));
            int late=State.command.tasks.Count(t=>t.status==0&&!CommandRules.TaskCanFinish(State,t));
            Label(panel,"待安排 "+State.command.tasks.Count(t=>t.status==0&&t.actor<0)+" 项 · "+(urgent>0?urgent+" 项今夜必须开始":"本夜暂无最后开工事项")+(late>0?" · "+late+" 项来不及":""),28,68,782,24,16,urgent>0?Red:Muted);
            string[] filters={"全部","待安排","已安排","时间紧迫"};
            for(int f=0;f<4;f++){int value=f;Button("TaskFilter_"+f,panel,filters[f],28+f*198,104,188,32,()=>{taskFilter=value;resetCommandTaskScroll=true;Draw();},true,taskFilter==f);}
            var tasks=State.command.tasks.Where(t=>t.status==0&&(taskFilter==0||taskFilter==1&&t.actor<0||taskFilter==2&&t.actor>=0||taskFilter==3&&(CommandRules.TaskUrgent(State,t)||!CommandRules.TaskCanFinish(State,t)))).OrderBy(t=>t.id==commandTask?0:t.operation==2?1:2).ThenBy(t=>t.deadline).ToList();
            var content=Scroll(panel,"CommandTasks",28,148,782,426,Mathf.Max(100,tasks.Count*142));int i=0;
            if(tasks.Count==0)Label(content,"当前筛选没有任务。可切换「全部」，或到地区经营发起委托；链条与日常仍会运行。",12,15,750,70,20,Teal);
            foreach(var t in tasks)
            {
                int index=i++;int y=index*142;var card=Rect("CommandTaskCard",content,0,y,764,134);bool focus=t.id==commandTask||commandTask<0&&index==0;
                Rule(card,0,133,764);Label(card,(index+1).ToString("00"),0,10,28,28,15,Muted);
                if(focus){var marker=Box("TaskSelection",card,29,8,2,27,Accent);marker.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;}
                var title=Label(card,t.title,40,3,720,34,focus?23:21,Gold);title.font=titleFont??font;
                Label(card,State.config.District(t.district).name+" · "+TaskOrderText(t)+(t.reconComplete?" · 已调查 +1骰":"")+(t.stepsRequired>1?" · 阶段 "+t.stepsDone+"/"+t.stepsRequired:""),40,41,720,24,16,t.actor<0?Muted:Teal);
                Label(card,CommandRules.TaskTimingText(State,t),40,70,520,23,16,CommandRules.TaskUrgent(State,t)&&t.actor<0?Red:Muted);
                Button("CommandTask_"+t.id,card,t.actor<0?"打开任务  ›":"查看安排  ›",28,96,224,32,()=>OpenCommandTask(t.id));
                if(t.actor<0)Button("CommandRecommend_"+t.id,card,CommandRules.CanStaffTask(State,t)?"推荐人选  ›":"查看阻碍  ›",582,96,178,32,()=>OpenCommandTask(t.id,true));
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
            Label(panel,"内线产出 → 次夜接头 → 再次夜外送；每环独立判定。展开查看今晚安排。",28,76,782,26,16,Muted);
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
            Label(panel,"正常交接保留积压；失陷损失见交接预告，暂停仍继续计时。",28,550,782,24,15,Muted);
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
            Label(panel,"从上方名册切换成员，在个人档案调整生活与任职安排。",28,382,782,28,16,Muted);
            Rule(panel,28,452,782);
            Label(panel,State.agents.Count>=State.command.config.capacity?"已达到本局人数上限。":State.money-CommandRules.ReservedMoney(State)<12?"招募需12可用银两，当前经费不足。":"新成员的身份、能力与外观随机。",28,472,782,26,15,Muted);
            ActionTicket("CommandRecruit",panel,"查看接纳人选","12 银两",28,516,374,42,OpenRecruitment,State.money-CommandRules.ReservedMoney(State)>=12&&State.agents.Count<State.command.config.capacity);
            Button("CommandPeopleAppearance",panel,"编辑外观  ›",428,516,382,42,()=>OpenAppearance(a.id));
        }
        void DrawCommandMap(Transform panel)
        {
            if(!State.config.districts.Any(d=>d.id==selectedDistrict))selectedDistrict=State.config.districts[0].id;
            var bounds=(RectTransform)panel;float width=bounds.rect.width,height=bounds.rect.height;
            float aspect=map!=null?(float)map.width/map.height:1.5f;
            float mapWidth=Mathf.Min(width,height*aspect),mapHeight=mapWidth/aspect,x=(width-mapWidth)/2,y=(height-mapHeight)/2;
            Box("MapMat",panel,0,0,width,height,Ink);
            var raw=Rect("CommandMap",panel,x,y,mapWidth,mapHeight).gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.texture=map;raw.color=new Color(.86f,.84f,.79f);raw.raycastTarget=false;
            Border(panel,x-1,y-1,mapWidth+2,mapHeight+2,Line);
            foreach(var d in State.config.districts)
            {
                var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==d.id).ToList();
                bool urgent=tasks.Any(t=>t.actor<0&&CommandRules.TaskUrgent(State,t));
                int pending=tasks.Count(t=>t.actor<0&&CommandRules.TaskCanFinish(State,t));
                int assigned=tasks.Count(t=>t.actor>=0),late=tasks.Count(t=>!CommandRules.TaskCanFinish(State,t));
                string status=urgent?"今夜必须开始":pending>0?pending+" 项待安排":assigned>0?(tasks.All(t=>t.reconnaissance)?"已安排调查":"已安排 "+assigned+" 项"):late>0?late+" 项来不及":"";
                var pin=Button("CommandDistrict_"+d.id,panel,d.name,x+d.x*(mapWidth-188)+10,y+d.y*(mapHeight-133)+25,168,38,()=>{selectedDistrict=d.id;modal="districtDetails";Draw();},true,d.id==selectedDistrict);
                if(status!="")
                {
                    var tag=Box("MapTaskTag",pin.transform,9,39,150,28,urgent?Accent:Ink);tag.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                    Label(tag,status,2,1,146,26,15,urgent?Paper:Gold,UnityEngine.TextAnchor.MiddleCenter);
                }
                if(urgent){var colors=pin.colors;colors.normalColor=Accent;colors.highlightedColor=Hex("AD5141");colors.pressedColor=Hex("753329");colors.selectedColor=Accent;pin.colors=colors;pin.image.CrossFadeColor(Accent,0,true,true);pin.GetComponentInChildren<UnityEngine.UI.Text>().color=Paper;}
                if(d.id==selectedDistrict)Border(pin.transform,-2,-2,172,42,Gold);
            }
            Box("MapNote",panel,x+16,y+mapHeight-44,574,28,Ink);Label(panel,"点选地名查看安排  ·  红色表示今夜必须开始",x+28,y+mapHeight-42,550,26,15,Muted);
        }
        void DrawCommandReport(Transform panel){DrawNightReport(panel);}
        Transform CommandModal(string title,float height=600)
        {
            var shade=Box("CommandShade",root,0,0,1600,900,new Color(.04f,.06f,.07f,.78f));Box("DialogShadow",shade,359,(900-height)/2+10,900,height,new Color(0,0,0,.24f));var p=Box("CommandModal",shade,350,(900-height)/2,900,height,Panel);DossierHeading(p,title);return p;
        }
        void DrawCommandConfirm()
        {
            var p=CommandModal("推进一夜前，确认组织安排",750);
            string orders=string.Join("\n",State.command.tasks.Where(t=>t.status==0&&t.actor>=0).Select(t=>CommandName(t.actor)+" → "+(t.reconnaissance?"调查":"执行")+"「"+t.title+"」"));
            int jobs=State.command.tasks.Count(t=>t.status==0&&t.actor>=0),routes=State.command.routes.Count(r=>r.enabled);
            var orderList=Scroll(p,"NightOrders",28,97,844,150,Mathf.Max(150,(jobs+3)*29));
            Label(orderList,"授权任务 "+jobs+" 项 / 运行链条 "+routes+" 条\n预留："+CommandRules.ReservedMoney(State)+"银两、"+CommandRules.ReservedIntel(State)+"份情报\n"+(orders==""?"没有额外任务：成员按链条和日常政策工作。":orders),0,0,805,Mathf.Max(150,(jobs+3)*29),19);
            Label(p,"任务优先于长期岗位；空闲成员自动工作与休养。\n本夜维护预计 "+(State.agents.Count(a=>a.condition==Condition.Active)*2)+"银两，夜末收入 "+(State.command.config.income+(State.command.strategy.alliances[0]>=State.command.strategy.config.allianceThreshold?4:0))+"银两（含联盟）。\n"+CommandNext(),28,258,840,125,18,Teal);
            var warnings=CommandRules.NightWarnings(State);var warningList=Scroll(p,"NightWarnings",28,397,844,235,Mathf.Max(235,warnings.Split('\n').Length*55));Label(warningList,warnings,0,0,805,Mathf.Max(235,warnings.Split('\n').Length*55),19,Gold);
            Button("CommandBack",p,"返回调整",28,665,300,50,()=>{modal="";Draw();});
            Button("CommandExecute",p,"下达指示 · 成员执行",348,665,524,50,()=>{CommandRules.Resolve(State);reportNight=-1;reportAll=false;commandTab=3;modal="";toast="执行完毕：要报汇总本夜变化；详细过程可打开值夜报告。";Save(false);Draw();},true,true);
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
            string explanation=c.eventKind==1?"两项都会准假一夜；援助额外恢复信任。链条会等待此人，不会自动越级传递。":c.eventKind==2?"分散休整：链条暂停今夜、压力−12。\n维持工作：全员暴露+18、压力+8，产生应急任务。":"谈话：该成员今夜离岗，信任+15。\n继续信任：当前叛离概率 "+CommandRules.BetrayalChance(CommandRules.Person(State,c.eventActor))+"%；只冲击本人所在的联络链。";
            Label(p,explanation,28,290,844,125,18,Muted);
            Button("CommandEventSafe",p,protect[c.eventKind],28,492,408,52,()=>{toast=CommandRules.Choose(State,true);Save(false);Draw();},State.money-CommandRules.ReservedMoney(State)>=(c.eventKind==1?6:c.eventKind==2?10:0));
            Button("CommandEventRisk",p,accept[c.eventKind],456,492,416,52,()=>{toast=CommandRules.Choose(State,false);Save(false);Draw();});
        }
        void DrawCommandNotice()
        {
            var notices=State.command.notices.OrderBy(n=>NoticeNeedsRescue(n)?0:1).ToList();
            bool urgent=notices.Any(NoticeNeedsRescue);var p=CommandModal(urgent?"急报与夜间要报":"夜间要报",750);
            Label(p,notices.Count+" 条消息已合并 · "+(urgent?"救援事项置顶，请先查看。":"一次阅读后回到指挥。"),28,94,844,27,17,urgent?Red:Muted);
            var content=Scroll(p,"CommandNoticeBody",28,137,844,495,495);float y=0;
            for(int i=0;i<notices.Count;i++)
            {
                var notice=notices[i];bool rescue=NoticeNeedsRescue(notice);
                Label(content,(rescue?"急  /  ":"")+notice.title,0,y,800,30,22,rescue?Red:Gold);
                var body=Label(content,notice.body,0,y+42,800,70,18,TextColor);
                float bodyHeight=Mathf.Max(70,body.preferredHeight+10);body.rectTransform.sizeDelta=new Vector2(800,bodyHeight);
                Button("CommandNoticeTarget_"+i,content,NoticeDestination(notice)+"  ›",0,y+48+bodyHeight,800,38,()=>ReadCommandNotices(notice,true));
                y+=bodyHeight+108;Rule(content,0,y-9,800);
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(495,y));
            Button("CommandNoticeDismiss",p,"全部已阅 · 继续安排",28,670,350,50,()=>AcknowledgeNotice(false));
            Button("CommandNoticeOpen",p,urgent?"查看救援任务  ›":NoticeDestination(notices[0])+"  ›",398,670,474,50,()=>AcknowledgeNotice(true),true,true);
        }
        bool NoticeNeedsRescue(CommandNotice notice)
        {return State.command.tasks.Any(t=>t.status==0&&t.operation==2&&(t.id==notice.task||notice.actor>=0&&t.target==notice.actor));}
        string NoticeDestination(CommandNotice notice)
        {return NoticeNeedsRescue(notice)?"查看救援任务":notice.tab==0?"查看相关任务":notice.tab==1?"检查联络网":notice.tab==2?"查看成员情况":notice.tab==5?"查看地方事务":"查看执行报告";}
        void AcknowledgeNotice(bool navigate)
        {
            var notice=State.command.notices.OrderBy(n=>NoticeNeedsRescue(n)?0:1).FirstOrDefault();
            if(notice!=null)ReadCommandNotices(notice,navigate);
        }
        void ReadCommandNotices(CommandNotice n,bool navigate)
        {
            State.command.notices.Clear();
            commandTab=3;modal="";
            if(navigate)
            {
                if(n.actor>=0)selectedAgent=n.actor;
                var rescue=State.command.tasks.FirstOrDefault(t=>t.status==0&&t.operation==2&&(t.id==n.task||n.actor>=0&&t.target==n.actor));
                if(rescue!=null)FocusCommandTask(rescue.id);
                else if(n.tab==0)
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
            Label(p,State.ending+"\n\n完成任务 "+State.command.completed+" / 送达情报 "+State.command.delivered+"\n成果 "+State.progress+" / 最高表身份 "+State.command.people.Max(x=>x.level)+"级\n\n下次开局会生成不同队员与任务顺序；新局目标按所选难度确定。",28,109,840,280,24);
            Label(p,GateProgress(),28,389,844,69,20,Teal);
            Button("CommandEndReview",p,"查看每夜报告",28,489,322,58,()=>{modal="endingReview";reportNight=-1;Draw();});
            Button("CommandEndHome",p,"保存战绩，返回主菜单",370,489,502,58,ReturnHome,true,true);
        }
    }
}
