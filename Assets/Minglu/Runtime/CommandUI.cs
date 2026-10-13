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
            if(InTutorial)return TutorialNextStep();
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
            var campaignStep=CampaignNextStep();
            if(campaignStep!=null)return campaignStep;
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
            if(commandTab==3&&modal=="")returnToWorkOverview=false;
            CommandRules.EnsureStrategy(State);
            DrawDossierDesk();
            if(State.command.eventKind!=0)DrawCommandEvent();
            else if(State.ended&&modal!="commandBriefing"){if(modal=="endingReview")DrawEndingReview();else if(modal=="noticeArchive")DrawNoticeArchive();else DrawCommandEnding();}
            else if(!State.ended&&State.command.notices!=null&&State.command.notices.Count>0)DrawCommandNotice();
            else if(modal=="commandBriefing")DrawCommandBriefing();
            else if(modal=="memberDetails")DrawMemberDetails();
            else if(modal=="districtDetails")DrawDistrictDetails();
            else if(modal=="routeDetails")DrawRouteDetails();
            else if(modal=="strategyDetails")DrawStrategyDetails();
            else if(modal=="campaignDossier")DrawCampaignDossier();
            else if(modal=="commandOverview")DrawCommandOverview();
            else if(modal=="noticeArchive")DrawNoticeArchive();
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
            else if(modal=="missionImpact")DrawMissionImpact();
            else if(modal=="missionBackground")DrawMissionBackground();
            else if(modal=="commandConfirm")DrawCommandConfirm();
            else if(modal!=""){modal="";}
            else if(commandTab!=3)DrawCommandBusiness();
        }
        void DrawCommandTasks(Transform panel)
        {
            Label(panel,"任务簿",28,20,560,42,32,Gold);
            IconButton("TasksHelp",panel,"report","任务说明",644,24,166,36,()=>OpenBriefing("如何安排任务","选择任务 → 比较人选 → 签发 → 结束今夜看结果。\n\n查看任务或推荐人选不会消耗资源，也不会推进夜晚；签发才会预留本夜所需资源。撤回会释放预留。\n\n「余几夜」是截止前剩余时间，「最少几夜」是顺利完成仍需的时间。多阶段任务要逐次安排；调查会额外占用一夜，不推进任务阶段。\n\n今夜必须开始的任务优先标红。来不及的任务仍可查看原因；不必派满所有成员，空闲成员会继续日常或链条工作。"));
            int urgent=State.command.tasks.Count(t=>t.status==0&&t.actor<0&&CommandRules.TaskUrgent(State,t));
            int late=State.command.tasks.Count(t=>t.status==0&&!CommandRules.TaskCanFinish(State,t));
            int pending=State.command.tasks.Count(t=>t.status==0&&t.actor<0),assigned=State.command.tasks.Count(t=>t.status==0&&t.actor>=0);
            NoirIcon(panel,urgent>0||late>0?"clock":"tasks",28,71,18,urgent>0||late>0?Red:Muted);
            Label(panel,urgent>0?urgent+" 项今夜必须开始"+(late>0?" · "+late+" 项来不及":""):late>0?late+" 项来不及":"选择任务，再决定是否安排",56,68,754,24,16,urgent>0||late>0?Red:Muted);
            string[] filters={"全部 "+(pending+assigned),"待安排 "+pending,"已安排 "+assigned,"时间紧迫"};
            string[] filterIcons={"tasks","clock","check","risk"};
            for(int f=0;f<4;f++){int value=f;IconButton("TaskFilter_"+f,panel,filterIcons[f],filters[f],28+f*198,104,188,32,()=>{taskFilter=value;resetCommandTaskScroll=true;Draw();},true,taskFilter==f);}
            var tasks=State.command.tasks.Where(t=>t.status==0&&(taskFilter==0||taskFilter==1&&t.actor<0||taskFilter==2&&t.actor>=0||taskFilter==3&&(CommandRules.TaskUrgent(State,t)||!CommandRules.TaskCanFinish(State,t)))).OrderBy(t=>t.id==commandTask?0:t.operation==2?1:2).ThenBy(t=>t.deadline).ToList();
            var content=Scroll(panel,"CommandTasks",28,148,782,426,Mathf.Max(100,tasks.Count*142));int i=0;
            if(tasks.Count==0){NoirIcon(content,"check",12,20,25,Teal);Label(content,"此筛选下暂无任务\n切换「全部」，或继续今夜安排。",51,15,694,70,20,Teal);}
            foreach(var t in tasks)
            {
                int index=i++;int y=index*142;var card=Rect("CommandTaskCard",content,0,y,764,134);bool focus=t.id==commandTask||commandTask<0&&index==0;
                Rule(card,0,133,764);Label(card,(index+1).ToString("00"),0,10,28,28,15,Muted);
                if(focus){var marker=Box("TaskSelection",card,29,8,2,27,Accent);marker.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;}
                var title=Label(card,t.title,40,3,516,34,focus?23:21,Gold);title.font=titleFont??font;
                NoirIcon(card,"map",40,43,17,Muted);
                Label(card,State.config.District(t.district).name+(t.actor>=0?" · "+TaskOrderText(t):""),66,40,490,24,16,t.actor<0?Muted:Teal);
                bool timeWarning=!CommandRules.TaskCanFinish(State,t)||CommandRules.TaskUrgent(State,t)&&t.actor<0;
                NoirIcon(card,"clock",40,75,17,timeWarning?Red:Muted);
                Label(card,CommandRules.TaskTimingText(State,t),66,71,490,25,16,timeWarning?Red:Muted);
                string progress=(t.stepsRequired>1?"阶段 "+t.stepsDone+" / "+t.stepsRequired:"")+(t.reconComplete?(t.stepsRequired>1?" · ":"")+"调查 +1骰":"");
                if(progress!="")Label(card,progress,66,103,490,23,15,Teal);
                IconButton("CommandTask_"+t.id,card,t.actor<0?"tasks":"check",t.actor<0?"安排任务":"查看安排",576,5,188,36,()=>OpenCommandTask(t.id));
                if(t.actor<0)IconButton("CommandRecommend_"+t.id,card,CommandRules.CanStaffTask(State,t)?"people":"risk",CommandRules.CanStaffTask(State,t)?"推荐人选":"查看阻碍",576,52,188,36,()=>OpenCommandTask(t.id,true));
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
            Label(panel,"联络网",28,20,560,42,32,Gold);
            IconButton("RoutesHelp",panel,"report","传递规则",644,24,166,36,()=>OpenBriefing("联络链如何运作","机构内线产出 → 下一夜接头 → 再下一夜外送。\n\n只有在敌方机构任职的内线产出内部情报，普通成员与民间任务不会凭空产出机构情报。送达后才加入可用库存。\n\n每环独立判定，同一份情报一夜只前进一环；失败或离岗会在该处等待。任务优先占用成员，签发前检查链条是否会因此停滞。\n\n暂停保留积压，但情报仍继续老化。正常交接保留积压，失陷后的损失会在换岗预告中显示。点击「详情」查看今晚由谁工作、成功率与具体风险。"));
            NoirIcon(panel,"routes",28,79,19,Muted);
            Label(panel,"机构内线  →  接头  →  外送",57,76,567,26,18,Muted);
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
                    NoirIcon(role.transform,new[]{"intel","people","routes"}[slot],14,9,18,Muted);
                    Label(role.transform,new[]{"内线 · 情报","接头人 · 说服","交通员 · 潜行"}[slot],42,7,194,21,14,Muted);
                    Label(role.transform,id<0?"选择成员  ＋":CommandName(id)+"  ›",14,31,217,29,20,Gold);
                    if(slot<2)Label(panel,"›",282+slot*264,y+77,10,25,17,Muted);
                }
                string error=CommandRules.RouteError(State,r);
                Label(panel,CompactRouteStatus(r),28,y+170,580,35,14,error==""?Muted:Red);
                Label(panel,"待接 "+r.waiting+"  →  中继 "+r.atRelay+"  →  送达 "+r.delivered,28,y+135,405,29,16,Muted);
                IconButton("RouteDetails_"+r.id,panel,"report","详情",442,y+133,174,35,()=>{detailRouteId=r.id;modal="routeDetails";Draw();});
                IconButton("RouteEnable_"+r.id,panel,r.enabled?"clock":"check",r.enabled?"暂停":"启用",650,y+138,160,38,()=>{r.enabled=!r.enabled;r.last=r.enabled?"已启用：内线隔夜尝试产出，接头与外送自动推进。":"已暂停，成员按其他任务或生活安排工作。";toast=r.enabled?"链条已启用：后续夜晚自动判定，任务优先占用人手。":"链条已暂停，成员可以生活与休养。";Save(false);Draw();},r.enabled||error=="");
            }
            if(State.command.routes.Any(r=>r.waiting+r.atRelay>0)){NoirIcon(panel,"clock",28,553,18,Red);Label(panel,"积压情报仍在老化，暂停不会停止计时。",56,550,754,24,16,Red);}
        }
        void DrawCommandPeople(Transform panel)
        {
            var a=CommandRules.Agent(State,selectedAgent);var p=CommandRules.Person(State,a.id);
            DrawLook(panel,EffectiveMemberLook(a),28,28,208,312);
            Label(panel,a.name,264,28,546,42,32,Gold);
            Label(panel,"代号「"+p.code+"」 · "+p.profession+" "+p.level+"级",264,84,546,26,18,Muted);
            Label(panel,CommandRules.Employment(p),264,122,546,27,19,TextColor);
            Label(panel,MemberStatus(a),264,173,546,29,20,a.exposure>=State.command.strategy.config.warning?Red:Teal);
            Rule(panel,264,220,546);
            string[] skills={"情报","潜行","说服","组织"};int[] values={a.intel,a.stealth,a.persuade,a.organize};
            for(int i=0;i<4;i++){Label(panel,skills[i],264+i*138,239,120,22,15,Muted);Label(panel,values[i].ToString(),264+i*138,267,120,32,26,TextColor);}
            IconButton("CommandPeopleDetails",panel,"people","个人档案",264,316,262,42,()=>{modal="memberDetails";Draw();});
            IconButton("PeopleHelp",panel,"report","人事说明",548,316,262,42,()=>OpenBriefing("人事与成长","从上方名册选择成员，进入「个人档案」调整生活与任职。\n\n情报、潜行、说服、组织是不同能力。行动要求哪项能力，就以该能力决定基础骰数；查看任务可比较实际成功率。\n\n成员有表身份与地下岗位。生活优先会暂停地下工作、处理家事并恢复状态；任职与身份等级决定机构资格。任务和链条安排应留出轮休。\n\n招募须有空余编制与12可用银两，查看人选不扣钱，确认接纳才支付。新成员的身份、能力和外观随机。被捕成员仍占编制，永久离队档案不占编制。外观不影响能力。"));
            Rule(panel,28,452,782);
            string recruitError=CommandRules.RecruitError(State);
            if(recruitError!=""){NoirIcon(panel,"risk",28,477,18,Red);Label(panel,recruitError,56,473,754,29,16,Red);}
            ActionTicket("CommandRecruit",panel,"招募成员","12 银两",28,516,374,42,OpenRecruitment,recruitError=="");
            IconButton("CommandPeopleAppearance",panel,"people","编辑外观",428,516,382,42,()=>OpenAppearance(a.id));
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
        Transform CommandModal(string title,float height=600,float width=900)
        {
            float x=(1600-width)/2;
            var shade=Box("CommandShade",root,0,0,1600,900,new Color(.04f,.06f,.07f,.78f));Box("DialogShadow",shade,x+9,(900-height)/2+10,width,height,new Color(0,0,0,.24f));var p=Box("CommandModal",shade,x,(900-height)/2,width,height,Panel);DossierHeading(p,title,width);return p;
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
        {return NoticeNeedsRescue(notice)?"查看救援任务":notice.tab==6?"查看主线进展":notice.tab==0?"查看相关任务":notice.tab==1?"检查联络网":notice.tab==2?"查看成员情况":notice.tab==5?"查看地方事务":"查看执行报告";}
        void AcknowledgeNotice(bool navigate)
        {
            var notice=State.command.notices.OrderBy(n=>NoticeNeedsRescue(n)?0:1).FirstOrDefault();
            if(notice!=null)ReadCommandNotices(notice,navigate);
        }
        void ReadCommandNotices(CommandNotice n,bool navigate)
        {
            CommandRules.EnsureNoticeArchive(State);
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
                else if(n.tab==6&&CommandRules.HasCommandCampaign(State)){commandTab=3;campaignPage=0;modal="campaignDossier";}
                else{modal="";commandTab=UnityEngine.Mathf.Clamp(n.tab,0,5);}
            }
            Save(false);Draw();
        }
        void DrawCommandEnding()
        {
            if(InTutorial){DrawTutorialEnding();return;}
            if(CommandRules.HasCommandCampaign(State))
            {
                var campaign=State.command.campaign;
                var panel=CommandModal(State.won?"委任完成 / "+campaign.scenarioData.title:"行动落幕 / "+campaign.scenarioData.title,750,1040);
                Label(panel,CommandRules.CampaignProgressText(State)+"  ·  第 "+State.turn+" 夜",32,99,976,29,20,Gold);
                CampaignBodyText(panel,"CampaignEndingBody",State.ending,32,146,976,159,21);
                Rule(panel,32,326,976);
                string people="归队 "+State.agents.Count(a=>a.condition==Condition.Active)+" 人 · 待救 "+State.agents.Count(a=>a.condition==Condition.Captured)+" 人 · 离队 "+State.agents.Count(a=>a.condition==Condition.Lost)+" 人";
                Label(panel,people+"\n完成任务 "+State.command.completed+" · 情报送达 "+State.command.delivered+" · 最高表身份 "+State.command.people.Max(x=>x.level)+" 级",32,347,976,66,20,Muted);
                CampaignBodyText(panel,"CampaignEndingHistory",campaign.history.Count>0?string.Join("\n\n",campaign.history.ToArray()):"本局未完成章节。每条主线均提供不依赖机构档案的方法，可更早安排，并为后续阶段留下时间。",32,436,976,190,19);
                Button("CommandEndReview",panel,"回看每夜决定",32,672,318,48,()=>{modal="endingReview";reportNight=-1;Draw();});
                Button("CommandEndHome",panel,"保存战绩，返回主菜单",372,672,636,48,ReturnHome,true,true);
                return;
            }
            var p=CommandModal(State.won?"灯火成网":"本次组织行动结束");
            Label(p,State.ending+"\n\n完成任务 "+State.command.completed+" / 送达情报 "+State.command.delivered+"\n成果 "+State.progress+" / 最高表身份 "+State.command.people.Max(x=>x.level)+"级\n\n下次开局会生成不同队员与任务顺序；新局目标按所选难度确定。",28,109,840,280,24);
            Label(p,GateProgress(),28,389,844,69,20,Teal);
            Button("CommandEndReview",p,"查看每夜报告",28,489,322,58,()=>{modal="endingReview";reportNight=-1;Draw();});
            Button("CommandEndHome",p,"保存战绩，返回主菜单",370,489,502,58,ReturnHome,true,true);
        }
    }
}
