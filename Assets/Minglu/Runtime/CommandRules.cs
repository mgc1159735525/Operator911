using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Minglu
{
    public static partial class CommandRules
    {
        public static readonly string[] Skills={"情报","潜行","说服","组织"};
        public static readonly string[] Professions={"书吏","行商","教书先生","药师","工匠","账房"};
        public static void Log(RunState s,string text){s.journal.Add("["+s.turn+"] "+text);}
        static int Next(RunState s,int n){uint x=s.random;x^=x<<13;x^=x>>17;x^=x<<5;s.random=x;return (int)(x%(uint)n);}
        public static CommandPerson Person(RunState s,int id){return s.command.people.First(p=>p.id==id);}
        public static Agent Agent(RunState s,int id){return s.agents.First(a=>a.id==id);}
        public static void Initialize(RunState s,bool crisis=false)
        {
            var asset=Resources.Load<TextAsset>("Minglu/Config/command_config");
            s.command=new CommandState{missionRevision=2,institutionRevision=1,config=JsonUtility.FromJson<CommandConfig>(asset.text),variant=(int)(s.initialSeed%3)};
            InitializeExperience(s);
            ValidateConfig(s.command.config);
            s.courierEnabled=false;s.organizationEnabled=false;s.cityEnabled=false;s.dispatchEnabled=false;s.storyEnabled=false;s.campaignEnabled=false;
            s.pendingEvent="";s.agents.Clear();s.config.agents.Clear();s.config.missions.Clear();s.config.events.Clear();s.journal.Clear();s.money=60+(1-s.difficulty)*12;s.progress=0;s.intelligence=0;s.pressure=10+s.difficulty*5;s.turn=1;s.ended=false;
            for(int i=0;i<s.command.config.startingCrew;i++)AddPerson(s);
            // The command game starts without delivered intelligence. The old
            // +2 intelligence perk was discarded by this initialization, leaving
            // only its exposure penalty; fund the early liaison work instead.
            if(s.doctrine==1){s.money+=12;foreach(var a in s.agents)a.exposure=10;}
            if(s.doctrine==2){s.pressure=Math.Max(0,s.pressure-8);foreach(var a in s.agents)a.stamina=85;}
            if(crisis){Person(s,s.agents[0].id).leaveUntil=2;s.pressure+=10;}
            EnsureStrategy(s);
            s.command.routes.Add(new CommandRoute{id=0});s.command.routes.Add(new CommandRoute{id=1});
            AddTask(s,"上级指示",-1);AddTask(s,"上级指示",-1);AddTask(s,"成员发现",s.agents[Next(s,s.agents.Count)].id);
            InitializeCommandCampaign(s);
            Log(s,"你是地区组织首脑。先签发一项任务了解执行，再将机构内线接入情报链；成员自动接头、工作、生活。三个阶段门槛全部完成才能赢得本局。");
        }
        public static void AddPerson(RunState s)
        {
            if(s.agents.Count>=s.command.config.capacity)throw new InvalidOperationException("组织容量已满");
            int id=s.agents.Count;int sex=Next(s,2);if(id==1)sex=1-s.agents[0].appearance.sex;
            string[] surnames={"周","沈","陆","许","程","叶","林","陈","方","苏","顾","唐"};
            string[] male={"怀远","砚生","知行","绍安","明川","云舟","伯言","景平","庭松","清和","慎之","望舒"};
            string[] female={"映雪","素秋","南枝","晚晴","静宜","玉宁","知微","青禾","兰因","月白","含章","锦书"};
            string name;do{name=surnames[Next(s,surnames.Length)]+(sex==0?male:female)[Next(s,12)];}while(s.agents.Any(a=>a.name==name));
            var a=new Agent{id=id,name=name,intel=2+Next(s,4),stealth=2+Next(s,4),persuade=2+Next(s,4),organize=2+Next(s,4),appearance=AppearanceRules.Randomize(Next(s,int.MaxValue),sex),handler=-1};
            // Keep the existing single simulation-RNG draw so this cosmetic
            // preference does not shift professions, stats or later events.
            a.appearance.parts[12]=AppearanceRules.IsElderFace(a.appearance.parts[0])?(Next(s,100)<85?3:2):Next(s,2);
            var p=new CommandPerson{id=id,profession=Professions[Next(s,Professions.Length)],code="灯影"+(Next(s,800)+100),age=new[]{22,32,44,58}[a.appearance.parts[0]%4]+Next(s,10),loyalty=60+Next(s,31),level=1+Next(s,2),life=new[]{"照顾家人","经营铺面","进修学业","偿还旧债","照顾病亲","维系同乡"}[Next(s,6)],temper=new[]{"谨慎","热心","果断"}[Next(s,3)],originalLook=AppearanceRules.Copy(a.appearance)};
            p.institution=id==0?1+Next(s,4):(Next(s,100)<25?1+Next(s,4):0);
            a.coverRole=p.profession;s.agents.Add(a);s.command.people.Add(p);
            s.config.agents.Add(new AgentRow{id=id,name=name,role=p.profession,bio=p.age+"岁，平日"+p.life+"。性情"+p.temper+"，地下代号「"+p.code+"」。",intel=a.intel,stealth=a.stealth,handler=-1,portrait=id%4});
        }
        public static void AddTask(RunState s,string origin,int source)
        {
            if(s.command.tasks.Count(t=>t.status==0)>=9)return;
            var reporter=s.command.people.FirstOrDefault(p=>p.id==source);
            if(source>=0&&reporter==null)return;
            var candidates=origin=="突发事件"||source<0?s.command.people:s.command.people.Where(p=>p.id==source).ToList();
            var pool=s.command.config.templates.Where(t=>!t.followupOnly&&!s.command.tasks.Any(active=>active.status==0&&active.title==t.title)&&
                (origin=="内线提议"?reporter!=null&&Agent(s,reporter.id).condition==Condition.Active&&t.sourceInstitution==reporter.institution&&HasInstitution(reporter)&&reporter.level>=t.level:
                 t.sourceInstitution==0&&candidates.Any(p=>Agent(s,p.id).condition==Condition.Active&&p.level>=t.level&&(t.profession==""||t.profession==p.profession)))).ToList();
            if(s.command.nextTask<2)pool=pool.Where(t=>t.level==1).ToList();
            if(pool.Count==0)return;
            var template=pool[Next(s,pool.Count)];
            CreateTask(s,template,origin,source,Next(s,s.config.districts.Count));
        }
        public static int Dice(RunState s,Agent a,int skill)
        {
            return Math.Max(1,new[]{a.intel,a.stealth,a.persuade,a.organize}[skill]-(a.stamina<40?2:0)-(a.spirit<35?1:0)-(Person(s,a.id).neglect>=3?1:0)-(a.exposure>=60?1:0)-(s.pressure>=60?1:0));
        }
        public static string Availability(RunState s,int id)
        {
            var a=s.agents.FirstOrDefault(x=>x.id==id);if(a==null)return "未指定成员";
            if(s.ended)return "本局已结束";
            if(s.command.eventKind!=0)return "先处理待决事件";
            if(a.condition!=Condition.Active)return a.condition==Condition.Captured?"成员被捕：前往任务页安排营救":"成员已脱离组织";
            var p=Person(s,id);if(p.leaveUntil>=s.turn)return "正在处理生活事务，预计第"+(p.leaveUntil+1)+"夜恢复";
            if(p.policy==1)return "生活优先：先在成员页切回平衡或主动政策";
            if(a.stamina<20||a.spirit<20)return "需要休养：体力或精神不足20";
            return "";
        }
        public static string TaskError(RunState s,int actor,CommandTask t)
        {
            string error=Availability(s,actor);if(error!="")return error;
            if(t==null||t.status!=0||t.deadline<s.turn)return "任务已结束";
            if(t.reconnaissance&&t.reconComplete)return "目标已调查，改为执行任务；调查收益不能重复叠加";
            if(t.stepsRequired-t.stepsDone+(t.reconnaissance?1:0)>Math.Min(t.deadline,s.config.Value("turns"))-s.turn+1)return "剩余夜数不足以完成"+(t.reconnaissance?"调查和后续阶段":"后续阶段")+"；请安排其他任务";
            if(!t.reconnaissance&&t.operation==4&&(t.target!=actor||HasInstitution(Person(s,actor))))return "任职任务须由尚无敌方任职身份的指定成员本人执行";
            if(t.actor>=0&&t.actor!=actor)return "任务已有负责人";
            if(s.command.tasks.Any(x=>x.status==0&&x.actor==actor&&x.id!=t.id))return "成员已有授权任务";
            var p=Person(s,actor);if(!t.reconnaissance&&p.level<t.level)return "表身份需达到"+t.level+"级；日常工作可以提升等级";
            if(!t.reconnaissance&&!string.IsNullOrEmpty(t.profession)&&p.profession!=t.profession)return "需要表身份："+t.profession;
            string resource=TaskResourceError(s,t);if(resource!="")return resource;
            if(TaskDice(s,Agent(s,actor),t)<ActionRequired(t))return "骰子不足，成功率为零；换人、筹备或启用情报支援";
            return "";
        }
        public static string Assign(RunState s,int actor,int task)
        {
            var t=s.command.tasks.Find(x=>x.id==task);string e=TaskError(s,actor,t);if(e!="")return e;t.actor=actor;return "";
        }
        public static int WithdrawAssignments(RunState s,int actor,string reason)
        {
            var tasks=s.command.tasks.Where(t=>t.status==0&&t.actor==actor).ToList();
            foreach(var t in tasks){t.actor=-1;Log(s,"撤回「"+t.title+"」授权："+reason+"；预留银两与情报已释放，可另派成员。");}
            return tasks.Count;
        }
        public static void SetPolicy(RunState s,int actor,int policy)
        {
            if(policy<0||policy>2)throw new ArgumentOutOfRangeException(nameof(policy));
            Person(s,actor).policy=policy;
            if(policy==1)WithdrawAssignments(s,actor,Agent(s,actor).name+"改为生活优先");
        }
        static void ReleaseUnavailableAssignments(RunState s)
        {
            // Availability also blocks on a pending event; an event alone must not revoke unrelated orders.
            foreach(var p in s.command.people)
            {
                var a=Agent(s,p.id);
                string reason=a.condition!=Condition.Active?"成员已无法出勤":p.leaveUntil>=s.turn?"成员休假":p.policy==1?"成员生活优先":a.stamina<20||a.spirit<20?"成员需要休养":"";
                if(reason!="")WithdrawAssignments(s,p.id,reason);
            }
        }
        public static string RouteError(RunState s,CommandRoute r)
        {
            if(new[]{r.source,r.liaison,r.courier}.Any(id=>id<0))return "尚缺成员：内线、接头人、交通员均须指定";
            string sourceError=SourceError(s,r.source);if(sourceError!="")return sourceError;
            if(new[]{r.source,r.liaison,r.courier}.Distinct().Count()!=3)return "三个岗位需要不同成员";
            foreach(int id in new[]{r.source,r.liaison,r.courier})
            {
                var a=s.agents.FirstOrDefault(x=>x.id==id);if(a==null||a.condition!=Condition.Active)return "链条有失效成员，请替换";
                if(s.command.routes.Any(x=>x!=r&&x.enabled&&new[]{x.source,x.liaison,x.courier}.Contains(id)))return "同一成员不能同时属于两条启用链";
            }
            return "";
        }
        static bool Roll(RunState s,Agent a,int skill,int required,string job,CommandTask task=null)
        {
            int dice=task==null?Dice(s,a,skill):TaskDice(s,a,task),success=0;var faces=new List<int>();
            for(int i=0;i<dice;i++){int f=Next(s,6)+1;faces.Add(f);if(f>=4)success++;}
            a.stamina=Math.Max(0,a.stamina-ActionStaminaCost(s,a));a.spirit=Math.Max(0,a.spirit-6);a.exposure=Math.Min(100,a.exposure+(task==null?(Person(s,a.id).temper=="谨慎"?3:5):TaskExposure(s,a,task)));
            if(task!=null)task.lastResult="第"+s.turn+"夜 · "+a.name+" · 「"+task.title+"」 · "+Plans[task.plan]+" · "+dice+"骰 ["+string.Join(",",faces)+"] → "+success+"/"+required+"成功";
            Log(s,a.name+" · "+job+"："+Skills[skill]+" "+dice+"骰 ["+string.Join(",",faces)+"]，"+success+"/"+required+"成功，"+(success>=required?"完成":"未成，保留机会至期限"));return success>=required;
        }
        static void AddRouteNote(CommandRoute r,string note){r.last=(r.last=="本夜待报"?"":r.last+"；")+note;}
        static void AgeRoute(CommandRoute r){if(r.waiting+r.atRelay>0){r.age++;if(r.age>=7){r.waiting=0;r.atRelay=0;r.age=0;AddRouteNote(r,"连续6夜未送达，积压失效；请检查接头岗位");}}else r.age=0;}
        public static int FinalDeliveries(RunState s){EnsureExperience(s);return s.command.goalDeliveries;}
        public static int FinalScore(RunState s){EnsureExperience(s);return s.command.goalScore;}
        public static int FinalLevel(RunState s){EnsureExperience(s);return s.command.goalLevel;}
        public static string GateText(RunState s)
        {
            return s.command.gate==0?"立足：送达2份情报 + 完成2项任务":s.command.gate==1?"扎根：送达"+SecondGateDeliveries(s)+"份 + 至少2人身份达到2级":"决胜：成果"+FinalScore(s)+" + 送达"+FinalDeliveries(s)+"份 + 至少1人身份达到"+FinalLevel(s)+"级";
        }
        public static void CheckGate(RunState s)
        {
            var c=s.command;
            if(c.gate==0&&c.delivered>=2&&c.completed>=2){c.gate=1;Log(s,"阶段突破：组织立足。上级要求扎根社会，培养身份并扩大送达。");}
            if(c.gate==1&&c.delivered>=SecondGateDeliveries(s)&&c.people.Count(p=>p.level>=2&&Agent(s,p.id).condition==Condition.Active)>=2){c.gate=2;Log(s,"阶段突破：进入决胜考验。");}
            if(c.gate==2&&s.progress>=FinalScore(s)&&c.delivered>=FinalDeliveries(s)&&c.people.Any(p=>p.level>=FinalLevel(s)&&Agent(s,p.id).condition==Condition.Active)&&CampaignReadyForVictory(s)){s.ended=true;s.won=true;s.ending=HasCommandCampaign(s)?CampaignEnding(s,true):"灯火成网：组织完成本局考验。";}
        }
        public static void Resolve(RunState s)
        {
            if(s.ended||s.command.eventKind!=0)return;
            EnsureStrategy(s);
            ReconcileCampaignPackets(s);
            ReleaseUnavailableAssignments(s);
            var c=s.command;int oldMoney=s.money,oldIntel=s.intelligence,maintenancePaid=0,unpaidMembers=0;int firstTask=c.nextTask,oldDelivered=c.delivered,oldCompleted=c.completed,oldGate=c.gate;var oldLevels=c.people.ToDictionary(p=>p.id,p=>p.level);var priorStatus=c.tasks.ToDictionary(t=>t.id,t=>t.status);var taskActorsBefore=c.tasks.ToDictionary(t=>t.id,t=>t.actor);var busy=new HashSet<int>();Log(s,"—— 第"+s.turn+"夜 / 组织执行报告 ——");
            foreach(var t in c.tasks.Where(t=>t.status==0&&t.actor>=0).ToList())
            {
                int id=t.actor;string e=TaskError(s,id,t);if(e!=""){Log(s,t.title+"暂停："+e);t.actor=-1;continue;}
                busy.Add(id);var a=Agent(s,id);
                Log(s,"「"+t.title+"」采用"+Plans[t.plan]+"：扣除"+PlanCost(t)+"银两、"+(t.intelSupport?1:0)+"份情报。" );
                s.money-=PlanCost(t);if(t.intelSupport)s.intelligence--;
                bool taskSuccess=Roll(s,a,ActionSkill(t),ActionRequired(t),(t.reconnaissance?"目标调查 · ":"")+t.title,t);if(t.plan==2)s.pressure=Math.Min(100,s.pressure+2);
                if(t.reconnaissance)
                {
                    if(taskSuccess){t.reconComplete=true;t.lastResult+="；目标调查完成：后续执行+1骰、暴露−3；未完成任务阶段，不产出库存情报。";}
                    else{a.exposure=Math.Min(100,a.exposure+2);s.pressure=Math.Min(100,s.pressure+1);t.lastResult+="；调查未成：额外暴露+2、压力+1；可改为执行或再次调查。";}
                    Log(s,t.lastResult);Notice(s,new CommandNotice{title=taskSuccess?"目标调查有了结果":"目标调查未能完成",body=a.name+"提交「"+t.title+"」调查报告。\n"+(taskSuccess?"执行该目标时额外+1骰、暴露−3，效果保留至任务结束，可交给另一名成员执行。":"未获得调查优势，本夜投入已消耗；是否再次调查取决于剩余期限。")+"\n任务仍需授权执行，第"+t.deadline+"夜截止。",tab=0,task=t.id});
                    t.actor=-1;t.reconnaissance=false;continue;
                }
                if(taskSuccess){t.stepsDone++;if(t.stepsDone<t.stepsRequired){t.lastResult+="；阶段完成 "+t.stepsDone+"/"+t.stepsRequired+"，下夜另行授权";Log(s,t.lastResult);}else{CompleteOperation(s,t);t.status=1;CampaignOnTaskCompleted(s,t);c.completed++;s.progress+=t.reward;s.money+=t.money;s.pressure=Math.Max(0,s.pressure-t.pressureRelief);c.strategy.trace=Math.Max(0,c.strategy.trace-t.traceRelief);c.strategy.hearts=Math.Min(100,c.strategy.hearts+t.heartsReward);GainSkillExperience(s,a,t.skill);Log(s,"完成「"+t.title+"」："+TaskReward(t));t.lastResult+="；完成 · "+TaskReward(t);}}
                else{a.exposure=Math.Min(100,a.exposure+t.failExposure);s.pressure=Math.Min(100,s.pressure+2);t.lastResult+="；未成 · 额外暴露+"+t.failExposure+"、压力+2，期限内可重新授权";Log(s,t.lastResult);}
                t.actor=-1;
            }
            foreach(var r in c.routes.Where(r=>!r.enabled)){int pending=r.waiting+r.atRelay;AgeRoute(r);ReconcileCampaignPackets(s);if(pending>0&&r.waiting+r.atRelay==0)Log(s,"情报链"+(r.id+1)+"：暂停期间积压失效，恢复前请重新安排。");}
            foreach(var r in c.routes.Where(r=>r.enabled))
            {
                string e=RouteError(s,r);if(e!=""){r.last=e;AgeRoute(r);ReconcileCampaignPackets(s);Log(s,"情报链"+(r.id+1)+"暂停："+r.last);continue;}
                if(c.pauseUntil>=s.turn){r.last="隐蔽政策生效，本夜暂停；存量密件仍计时。";AgeRoute(r);ReconcileCampaignPackets(s);Log(s,"情报链"+(r.id+1)+"："+r.last);continue;}
                r.last="本夜待报";int oldWaiting=r.waiting,oldRelay=r.atRelay;
                // Advance downstream first: a packet cannot traverse two links in one night.
                int[] actors={r.courier,r.liaison,r.source};
                for(int stage=0;stage<3;stage++)
                {
                    int id=actors[stage];bool work=stage==0?oldRelay>0:stage==1?oldWaiting>0:SourceWorksTonight(s,r);
                    if(!work)continue;
                    e=Availability(s,id);if(e!=""||busy.Contains(id)){AddRouteNote(r,Agent(s,id).name+"岗位暂停："+(e!=""?e:"优先执行已授权任务"));continue;}
                    var a=Agent(s,id);busy.Add(id);int skill=stage==0?1:stage==1?2:0;
                    if(!Roll(s,a,skill,1,stage==0?"链条外送":stage==1?"链条接头":"内线观察")){AddRouteNote(r,a.name+"未通过判定，下夜自动重试");continue;}
                    GainSkillExperience(s,a,skill);
                    if(stage==0){r.atRelay--;r.age=0;r.delivered++;c.delivered++;s.intelligence++;CampaignOnDelivery(s,r.source,r.id);s.progress+=2;s.money+=3;AddRouteNote(r,"送达1份：成果+2 / 银两+3");}
                    else if(stage==1){r.waiting--;r.atRelay++;CampaignOnRelay(s,r.id);AddRouteNote(r,"接头成功：进入中继，下夜可外送");}
                    else{r.waiting++;CampaignOnSourceProduced(s,r.source,r.id);AddRouteNote(r,Employment(Person(s,id))+"："+InstitutionIntel[Person(s,id).institution]+"，产出1份待接情报");if(Next(s,100)<40&&s.turn<s.config.Value("turns")){int before=c.nextTask;AddTask(s,"内线提议",id);if(c.nextTask>before)Log(s,a.name+"从岗位发现新机会，已提交任务待授权。");}}
                }
                if(r.last=="本夜待报")r.last="按观察周期等待：内线次夜观察，本夜没有上游情报可接头或外送。";
                AgeRoute(r);ReconcileCampaignPackets(s);
                Log(s,"情报链"+(r.id+1)+"："+r.last);
            }
            foreach(var p in c.people.Where(p=>Agent(s,p.id).condition==Condition.Active))
            {
                var a=Agent(s,p.id);bool paid=s.money>=2;if(paid){s.money-=2;maintenancePaid+=2;}
                if(busy.Contains(p.id)){p.neglect++;p.last="地下工作 / 日常事务顺延";if(p.neglect>=3){p.loyalty=Math.Max(0,p.loyalty-7);a.suspicion=Math.Min(100,a.suspicion+8);Log(s,a.name+"连续忙于地下工作，家事积压；建议生活优先休养。");}}
                else{p.neglect=0;p.xp++;a.stamina=Math.Min(100,a.stamina+20);a.spirit=Math.Min(100,a.spirit+15);a.exposure=Math.Max(0,a.exposure-10);a.suspicion=Math.Max(0,a.suspicion-6);p.loyalty=Math.Min(100,p.loyalty+5);p.last=Employment(p)+" · "+p.profession+"日常 / "+p.life;if(p.xp>=c.config.coverXP*p.level&&p.level<4){p.xp=0;p.level++;Log(s,a.name+"的表身份升至"+p.level+"级，获得更高层级任务准入。");}if(p.policy==2&&Next(s,100)<(p.temper=="热心"?35:22)&&s.turn<s.config.Value("turns")){int before=c.nextTask;AddTask(s,"成员发现",p.id);if(c.nextTask>before)p.last+=" / 主动上报机会";}}
                if(!paid){unpaidMembers++;p.loyalty=Math.Max(0,p.loyalty-6);a.suspicion=Math.Min(100,a.suspicion+6);}
            }
            s.money+=c.config.income;
            foreach(var t in c.tasks.Where(t=>t.status==0&&t.deadline<=s.turn)){t.status=2;t.actor=-1;s.pressure=Math.Min(100,s.pressure+3);Log(s,"任务到期："+t.title+"，压力+3；不会直接锁死全部主线。");}
            StrategyNight(s,busy);
            s.pressure=Math.Min(100,s.pressure+1);CheckGate(s);
            Log(s,"夜末账目：银两 "+oldMoney+" → "+s.money+"（含维护支出"+maintenancePaid+"）；情报 "+oldIntel+" → "+s.intelligence+"。当前目标："+GateText(s));
            if(unpaidMembers>0)Log(s,"维护经费不足：部分成员未领到生活补助，信任−6、可疑度+6；请预留生活经费。");
            if(!s.agents.Any(a=>a.condition==Condition.Active)||(s.agents.Count(a=>a.condition==Condition.Active)<3&&!s.agents.Any(a=>a.condition==Condition.Captured)&&s.agents.Count>=c.config.capacity)){s.ended=true;s.won=false;s.ending="组织无法维持最小联络链。";}
            if(!s.ended&&s.turn>=s.config.Value("turns")){s.ended=true;s.won=false;s.ending="行动窗口结束，未达成："+GateText(s);}
            if(s.ended){if(HasCommandCampaign(s))s.ending=CampaignEnding(s,s.won);return;}
            s.turn++;
            SpawnFollowups(s);
            if(s.turn%c.config.taskInterval==1){int before=c.nextTask;AddTask(s,"上级指示",-1);if(c.nextTask>before)Log(s,"上级发来新指示，请查看任务来源与人员需求。");}
            TryContextEvent(s);
            ArmRaid(s);
            var messages=new List<string>();
            var fresh=c.tasks.Where(t=>t.id>=firstTask).ToList();
            foreach(var t in fresh)messages.Add(t.origin+"：「"+t.title+"」 · 第"+t.deadline+"夜截止");
            foreach(var p in c.people.Where(p=>p.level>oldLevels[p.id]))messages.Add(Agent(s,p.id).name+"的表身份升至 Lv."+p.level+"，可承担更高门槛任务。");
            if(c.delivered>oldDelivered)messages.Add("情报送达 "+(c.delivered-oldDelivered)+"份，上级已收到。成果与银两已结算。");
            if(c.completed>oldCompleted)foreach(var t in c.tasks.Where(t=>t.status==1&&priorStatus.ContainsKey(t.id)&&priorStatus[t.id]==0))
                messages.Add(Agent(s,taskActorsBefore[t.id]).name+"完成「"+t.title+"」："+TaskReward(t)+"。可重新安排该成员。");
            if(c.gate>oldGate)messages.Add("阶段目标达成，新的上级要求已更新。");
            if(messages.Count>0)Notice(s,new CommandNotice{title="夜间要报 · 第"+(s.turn-1)+"夜",night=s.turn-1,body=string.Join("\n\n",messages),tab=fresh.Count>0?0:4,task=fresh.Count>0?fresh[0].id:-1});
        }
        public static void Notice(RunState s,CommandNotice notice,bool first=false)
        {
            if(s.command.notices==null)s.command.notices=new List<CommandNotice>();
            EnsureNoticeArchive(s);
            if(notice.night<=0)notice.night=s.turn;
            ArchiveNotice(s,notice);
            if(first)s.command.notices.Insert(0,notice);else s.command.notices.Add(notice);
        }
        public static string Choose(RunState s,bool protect)
        {
            var c=s.command;if(c.eventKind==4)return ResolveRaid(s,protect);if(c.eventKind==0)return "没有待决事件";var p=Person(s,c.eventActor);var a=Agent(s,p.id);
            int cost=c.eventKind==1?6:c.eventKind==2?10:0;if(protect&&s.money-ReservedMoney(s)<cost)return "未预留银两不足，请选择另一方案";
            int logStart=s.journal.Count;
            if(protect)s.money-=cost;
            if(c.eventKind==1){p.leaveUntil=s.turn;p.loyalty=Math.Min(100,p.loyalty+(protect?15:4));Log(s,a.name+"获准处理家事，今夜不出勤。" );}
            if(c.eventKind==2){if(protect){s.pressure=Math.Max(0,s.pressure-12);c.pauseUntil=s.turn;Log(s,"已安排分散休整：今夜链条暂停，压力−12。");}else{foreach(var who in s.agents.Where(x=>x.condition==Condition.Active))who.exposure=Math.Min(100,who.exposure+18);s.pressure=Math.Min(100,s.pressure+8);Log(s,"维持工作：清查增加全员暴露18、压力8。发布应急安置任务。");AddTask(s,"突发事件",p.id);}}
            if(c.eventKind==3){if(protect){p.leaveUntil=s.turn;p.loyalty=Math.Min(100,p.loyalty+15);Log(s,a.name+"暂离联络岗位接受谈话，今夜休整。" );}else if(Next(s,100)<BetrayalChance(p)){a.condition=Condition.Lost;DisruptRoutes(s,a.id,"成员叛离，此线暂停");foreach(var r in c.routes.Where(r=>new[]{r.source,r.liaison,r.courier}.Contains(a.id)))foreach(int id in new[]{r.source,r.liaison,r.courier}.Where(id=>id>=0&&id!=a.id))Agent(s,id).exposure=Math.Min(100,Agent(s,id).exposure+25);Log(s,a.name+"叛离组织；只损伤其所接触的环节，其他情报与联络链保留。");AddTask(s,"突发事件",p.id);}else{p.loyalty=Math.Min(100,p.loyalty+8);Log(s,a.name+"接受信任，继续留任。");}}
            ReleaseUnavailableAssignments(s);
            Notice(s,new CommandNotice{title="决定已执行",body=string.Join("\n\n",s.journal.Skip(logStart))+"\n\n下一步：检查受影响的成员和链条，再安排今夜工作。",tab=c.eventKind==2||a.condition==Condition.Lost?1:2,actor=p.id},true);
            c.eventKind=0;c.eventText="";ArmRaid(s);return "";
        }
        public static void Validate(RunState s)
        {
            var c=s.command;if(c==null||c.version!=1||c.config==null||c.people==null||c.tasks==null||c.routes==null||c.config.templates==null||c.config.templates.Count<1||c.config.coverXP<1||c.config.taskInterval<1||s.turn<1||s.turn>s.config.Value("turns")||s.random==0||s.money<0||s.agents.Count!=c.people.Count||c.people.Select(p=>p.id).Distinct().Count()!=c.people.Count)throw new Exception("组织存档结构无效");
            AppearanceRules.Validate(s.chiefLook);
            UpgradeMissions(s);UpgradeInstitutions(s);EnsureExperience(s);ValidateStrategy(s);ValidateCommandCampaign(s);EnsureNoticeArchive(s);
            if(c.experienceRevision!=1||c.goalDeliveries<1||c.goalScore<1||c.goalLevel<1||c.goalLevel>4||c.secondGateDeliveries<1||c.routes.Any(r=>r.compromisedSlots<0||r.compromisedSlots>7))throw new Exception("本局目标或链条交接数据无效");
            if(c.notices==null)c.notices=new List<CommandNotice>();
            foreach(var p in c.people){var a=Agent(s,p.id);if(p.institution<0||p.institution>=Institutions.Length||p.level<1||p.level>4||p.loyalty<0||p.loyalty>100||a.stamina<0||a.stamina>100||a.spirit<0||a.spirit>100)throw new Exception("成员数据无效");AppearanceRules.Validate(a.appearance);}
            foreach(var t in c.tasks)if(t.skill<0||t.skill>3||t.required<1||t.status<0||t.status>2||t.plan<0||t.plan>2||t.intelReward<0||t.pressureRelief<0||t.failExposure<0||t.traceRelief<0||t.traceRelief>20||t.heartsReward<0||t.heartsReward>20||t.actor>=0&&!s.agents.Any(a=>a.id==t.actor))throw new Exception("授权任务数据无效");
            foreach(var r in c.routes)if(new[]{r.source,r.liaison,r.courier}.Any(id=>id!=-1&&!s.agents.Any(a=>a.id==id)))throw new Exception("联络链成员无效");
        }
        public static void ValidateConfig(CommandConfig c)
        {
            if(c==null||c.startingCrew<3||c.startingCrew>8||c.capacity<c.startingCrew||c.capacity>8||c.income<0||c.income>100||c.taskInterval<1||c.taskInterval>10||c.coverXP<1||c.coverXP>20||c.eventChance<0||c.eventChance>100||c.templates==null||!c.templates.Any(t=>t.level==1&&t.profession==""))throw new Exception("组织配置范围无效，需至少一项无职业限制的1级任务");
            ValidateMissionTemplates(c);
            foreach(var t in c.templates)if(t==null||string.IsNullOrWhiteSpace(t.title)||string.IsNullOrWhiteSpace(t.body)||t.skill<0||t.skill>3||t.required<1||t.required>6||t.level<1||t.level>4||t.reward<0||t.reward>30||t.money<0||t.money>50||t.profession!=""&&!Professions.Contains(t.profession))throw new Exception("任务模板字段无效");
        }
    }
}

