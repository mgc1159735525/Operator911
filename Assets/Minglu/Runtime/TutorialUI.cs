using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        string tutorialPopup="";
        int tutorialHelpStage;
        bool tutorialRestart;
        string RegularSavePath { get { return smokeSave??Path.Combine(MingluStorage.Directory,"minglu-run-v14.json"); } }
        string TutorialSavePath { get { return smokeSave!=null?smokeSave+".tutorial":Path.Combine(MingluStorage.Directory,"minglu-tutorial-v1.json"); } }
        bool InTutorial { get { return TutorialRules.IsTutorial(State); } }
        bool TutorialUrgent { get { return InTutorial&&(State.command.eventKind!=0||State.command.notices.Count>0); } }
        bool TutorialLessonPending { get { return InTutorial&&!atTitle&&!State.ended&&!TutorialUrgent&&State.tutorial.introShownStage<State.tutorial.stage; } }
        bool TutorialDialogVisible { get { return !atTitle&&InTutorial&&(tutorialPopup!=""||TutorialLessonPending); } }
        readonly string[] tutorialTitles={"认识你的成员","比较人选，签发指示","让成员执行一夜","读懂结果与骰子","亲手建立情报链","给成员留出生活","现在，由你独立指挥"};

        void DrawTutorialHome(Transform p)
        {
            RunState saved=null;string issue="";
            if(File.Exists(TutorialSavePath)||File.Exists(TutorialSavePath+".bak"))
                try{saved=PrototypeSave.ReadWithRecovery(TutorialSavePath,out issue);if(!TutorialRules.IsTutorial(saved))throw new InvalidDataException("此文件不是教学进度。");}
                catch(Exception e){issue="教学记录读取失败，原文件保留："+e.Message;saved=null;}
            DrawHomeHeading(p,"新手演练 · 第一盏灯","可选教学关卡 / 八夜完整实践 / 独立保存进度");
            Label(p,"你是地区负责人。先学会让合适的人做合适的事，再让一份情报穿过完整的联络链。",0,162,758,75,25,TextColor);
            string[] headings={"01  跟着做","02  自己安排","03  复盘一局"};
            string[] bodies={"查看人物，比较能力，签发第一项任务。\n亲眼查看真实骰子结果；关键按钮会高亮。","设置内线、接头人和交通员，再安排生活。\n之后开放全部操作，保留目标和建议。","争取完成两项任务、送达一份情报，\n并处理一次事件。结果真实，失败可以重试。"};
            for(int i=0;i<3;i++){float y=263+i*100;Label(p,headings[i],0,y,180,28,21,Gold);Label(p,bodies[i],202,y,550,81,19,TextColor);}
            Rule(p,0,570,758);
            Label(p,saved==null?"不会覆盖正式行动，也不会计入正式战绩。":saved.ended?"上次演练已结束。可重新练习；正式行动仍保留。":"已保存到第 "+saved.turn+" 夜 / "+tutorialTitles[Mathf.Clamp(saved.tutorial.stage,0,6)]+"。",0,590,758,50,18,Muted);
            HomeTextButton("HomeBack",p,"返回",0,656,140,49,BackHome);
            if(saved!=null&&!saved.ended)
            {
                HomeTextButton("TutorialReplay",p,"重新演练",166,656,180,49,()=>{tutorialRestart=true;Draw();});
                Button("TutorialResume",p,"继续教学",422,656,336,49,ResumeTutorial,true,true);
            }
            else Button("TutorialBegin",p,saved==null?"开始新手演练":"再练一局",422,656,336,49,BeginTutorial,true,true);
            if(issue!="")Label(p,issue,0,712,758,49,15,Red);
            if(tutorialRestart)
            {
                var shade=Box("TutorialShade",root,0,0,1600,900,new Color(0,0,0,.8f));
                var panel=Box("TutorialRestartPanel",shade,430,260,740,350,Paper);SurfaceTheme(panel,true);
                Label(panel,"重新开始教学？",32,30,676,48,31,PaperInk);
                Label(panel,"本次教学进度会重新开始，原教学记录保留为备份。正式行动和人物预设不受影响。",32,109,676,98,21,PaperInk);
                Button("TutorialRestartCancel",panel,"继续原教学",32,259,278,50,()=>{tutorialRestart=false;Draw();});
                Button("TutorialRestartConfirm",panel,"重新演练",332,259,376,50,BeginTutorial,true,true);
            }
        }
        void BeginTutorial()
        {
            try
            {
                var candidate=TutorialRules.Create(ConfigStore.Load());
                candidate.chiefLook=profile.chiefLook==null?new Appearance{style=1}:AppearanceRules.Copy(profile.chiefLook);
                PrototypeSave.Write(candidate,TutorialSavePath);
                State=candidate;atTitle=false;tutorialRestart=false;tutorialPopup="";homeNotice="";ResetRunView();Draw();
            }
            catch(Exception e){homeNotice="无法开始教学，原进度已保留："+e.Message;Draw();}
        }
        void ResumeTutorial()
        {
            try
            {
                string recovery;var candidate=PrototypeSave.ReadWithRecovery(TutorialSavePath,out recovery);
                if(!TutorialRules.IsTutorial(candidate))throw new InvalidDataException("不是教学存档。");
                State=candidate;TutorialRules.Refresh(State);atTitle=false;tutorialPopup="";ResetRunView();
                toast=recovery==""?"已恢复教学进度。继续当前目标，不会重新掷骰。":recovery;Draw();
            }
            catch(Exception e){homeNotice="教学读取失败，原文件已保留："+e.Message;Draw();}
        }
        void ObserveTutorialAction(string name,RunState before,string priorModal,int priorPage)
        {
            if(!InTutorial||atTitle||before!=State||name.StartsWith("Tutorial")||State.ended)return;
            var t=State.tutorial;string prior=JsonUtility.ToJson(t);
            // Reading requires leaving the real dossier/report; opening it must
            // not immediately cover its contents with the next lesson.
            if(name=="MemberDetailsClose"&&priorModal=="memberDetails")t.inspectedMember=true;
            if(name.StartsWith("CompareSelect_")&&modal=="missionPlan")t.comparedMember=true;
            if(name=="CommandPageClose"&&priorPage==4&&t.resolvedNights>0)t.reportRead=true;
            if(name.StartsWith("LifePolicy_")&&priorModal=="memberDetails"&&selectedAgent==t.restActorId&&CommandRules.Person(State,selectedAgent).policy==1)t.lifeArranged=true;
            TutorialRules.Refresh(State);
            if(prior!=JsonUtility.ToJson(t))Save(false);
            Draw();
        }
        bool HandleTutorialKeyboard()
        {
            if(atTitle)
            {
                if(homePage=="tutorial"&&tutorialRestart&&Input.GetKeyDown(KeyCode.Escape)){tutorialRestart=false;Draw();return true;}
                return false;
            }
            if(!InTutorial||State.ended||modal=="appearance")return false;
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(tutorialPopup=="pause")tutorialPopup="";
                else tutorialPopup="pause";
                Draw();return true;
            }
            // F9 must never replace state from behind an instruction or pause.
            return TutorialDialogVisible;
        }
        string TutorialGoalLine()
        {
            return "教学目标  /  任务 "+Math.Min(2,State.command.completed)+" / 2   ·   情报送达 "+Math.Min(1,State.command.delivered)+" / 1   ·   事件决定 "+(State.tutorial.eventHandled?"1":"0")+" / 1";
        }
        string TutorialLessonBody(int stage)
        {
            var t=State.tutorial;
            switch(stage)
            {
                case 0:return "你只安排人员与目标，成员会自行行动。\n\n打开「人事档案」，查看一人的详细档案：情报、潜行、说服、组织决定对应行动的骰子数；表身份等级决定可接任务。\n\n读完后点「返回人事档案」。查看页面不会消耗一夜。";
                case 1:return "打开第一项任务，点击「比较人选」并亲自选择一人。\n\n每颗六面骰掷出 4、5、6 算一个成功。能力越高，骰子越多；成功数达到任务要求才完成。比较界面显示实际成功率。\n\n再点「签发」。这只是安排指令，成员尚未执行；不要把查看、调查和执行混为一谈。";
                case 2:return "第一项指示已签发。先打开「结束今夜」核对表，再确认推进。\n\n只有确认推进才消耗一夜、扣除投入并掷骰。已经执行的成本不会因失败退还；稍后的自主练习中，可在执行前撤回指示。\n\n空闲成员会自己生活与恢复；不需要把每个人都派出去。";
                case 3:return "刚才的结果来自真实掷骰。失败并不表示你点错了。\n\n打开「值夜报告」，查看「完整判定」，看骰子、成功数以及实际资源变化；然后返回地图。\n\n任务未成时，期限内可以重新签发；已经完成的阶段会保留。";
                case 4:return "现在建立第一条联络链，需要三名不同成员。\n\n内线必须在敌方机构任职；接头看说服，外送看潜行。可参考："+CommandName(t.sourceId)+" → "+CommandName(t.liaisonId)+" → "+CommandName(t.courierId)+"。三个岗位填好后，还要点「启用」。\n\n同一份情报每夜只走一环。第一条链在奇数夜观察：第二夜配好，最快第五夜送达；失败、休假或任务占岗都会延后。";
                case 5:return "地下工作与日常生活会争用同一个人的时间。\n\n到「"+CommandName(t.restActorId)+"」的个人档案，选择「生活优先」。他会留出时间恢复体力、精神并积累表身份经验。\n\n忙于任务或联络的人不积累日常经验。生活优先也会暂停其地下工作；以后需要工作时，记得改回「平衡安排」。";
                default:return "现在开放全部操作，不再替你锁定下一步。\n\n在八夜内争取：完成 2 项任务、真实送达 1 份情报、作出一次事件决定。目标始终显示在地图上方，底部建议可以随时定位待办。\n\n链条等待期间，用机动成员做任务。留意期限、连勤与暴露；轮休或任务占岗时可更换成员。第八夜结算后逐项复盘，未达标也能查看原因、重新练习。";
            }
        }
        string TutorialCoachText()
        {
            var t=State.tutorial;
            if(TutorialUrgent)return State.command.eventKind!=0?"成员带来一项需要你决定的事件。先看两项后果，再选择；决定后可在要报查看实际变化。":"先阅读本夜要报。随后可以打开值夜报告，查阅每颗骰子和完整原因。";
            switch(t.stage)
            {
                case 0:return modal=="memberDetails"?"看能力与表身份，再看生活与成长。\n\n读完点下方返回。":"点「人事档案」，选一位成员，再查看人物详情。";
                case 1:return modal=="compareCandidates"?"先比较对应能力、成功率和状态，再点「选此人」。":t.comparedMember?"人选已确定。检查成功率与成本，点击「签发」；失败仍可在期限内重试。":"打开第一项任务，再点「比较人选」。先学比较，之后才开放签发。";
                case 2:return "签发不是执行。核对今夜安排，再确认推进一夜；不必让所有成员出勤。";
                case 3:return "查看「完整判定」，找到骰子结果与资源变化。读完点「返回城区地图」继续。";
                case 4:return "在第一条联络链：\n依次选人，再启用。\n\n内线："+CommandName(t.sourceId)+"\n接头："+CommandName(t.liaisonId)+"\n外送："+CommandName(t.courierId)+"\n\n这是建议人选，\n你可自行比较。";
                case 5:return "在「"+CommandName(t.restActorId)+"」的详细档案，点「生活优先」。\n\n他负责本次休养练习；保持三位链条成员可出勤。";
                default:return "已开放全部操作。目标与建议仍在地图上，可随时回看教学说明。";
            }
        }
        CommandStep TutorialNextStep()
        {
            var t=State.tutorial;
            switch(t.stage)
            {
                case 0:return new CommandStep{message="打开一位成员的详细档案，理解能力与表身份。读完点返回。",label="查看成员  ›",page=2,actor=t.freeActorId};
                case 1:return new CommandStep{message="比较负责人，选择一位成员，签发第一项指示。",label="安排第一项任务  ›",task=t.firstTaskId};
                case 2:return new CommandStep{message="指示已签发；核对后确认推进一夜，让成员执行。",label="核对今夜安排  ›",dialog="commandConfirm",ready=true};
                case 3:return new CommandStep{message="打开值夜报告，展开判定，读完返回地图。",label="阅读执行报告  ›",page=4};
                case 4:return new CommandStep{message="配置第一条链的内线、接头人、交通员，然后启用。",label="建立联络链  ›",page=1};
                case 5:return new CommandStep{message="为 "+CommandName(t.restActorId)+" 设置生活优先，理解恢复与身份成长。",label="安排成员生活  ›",page=2,actor=t.restActorId,dialog="memberDetails"};
            }
            if(State.command.tasks.Any(x=>x.status==0&&x.actor>=0))return new CommandStep{message="指示已签发。核对联络岗位是否被占用，再推进今夜。",label="核对今夜安排  ›",dialog="commandConfirm",ready=true};
            if(!State.command.routes.Any(r=>r.enabled&&CommandRules.RouteError(State,r)==""))return new CommandStep{message="还需要一条能运行的联络链。修复岗位后记得启用。",label="检查联络网  ›",page=1};
            var stalled=State.command.routes.FirstOrDefault(r=>r.enabled&&new[]{r.source,r.liaison,r.courier}.Any(id=>CommandRules.Availability(State,id)!=""));
            if(stalled!=null)return new CommandStep{message="链条中有成员休养或离岗。更换为可出勤成员并重新启用，或在档案恢复平衡安排。",label="调整联络岗位  ›",page=1};
            var strained=State.command.people.FirstOrDefault(p=>p.neglect>=3&&NeedsAttention(CommandRules.Agent(State,p.id)));
            if(strained!=null)return new CommandStep{message=CommandName(strained.id)+"已连续工作：考虑轮休，替换联络岗位后重新启用。",label="安排轮休  ›",actor=strained.id,page=2,dialog="memberDetails"};
            if(State.command.completed<2)
            {
                var task=State.command.tasks.Where(x=>x.status==0&&x.actor<0&&CommandRules.CanStaffTask(State,x)).OrderBy(x=>x.operation==0?0:1).ThenBy(x=>x.stepsRequired-x.stepsDone).ThenBy(x=>x.required).FirstOrDefault();
                if(task!=null)return TaskStep(task,"：交给机动成员，避免占用今夜有待办的链条岗位。", "安排任务  ›");
            }
            return new CommandStep{message=State.command.delivered<1?"联络链会自动逐夜传递。查看岗位预测与积压，核对后推进；在途情报尚不能使用。":"核心目标已接近完成。继续维持人员与经费到第八夜，尝试调查、轮换或地方事务。",label="核对今夜安排  ›",dialog="commandConfirm",ready=true};
        }
        void NavigateTutorial()
        {
            if(TutorialUrgent)return;
            if(State.tutorial.stage==0&&modal=="memberDetails")return;
            if(State.tutorial.stage==3&&commandTab==4)return;
            modal="";commandTab=3;GoNextCommand();
        }
        void OpenTutorialHelp(){tutorialHelpStage=State.tutorial.stage;tutorialPopup="help";Draw();}
        void DrawTutorialLayer()
        {
            if(!InTutorial||atTitle||State.ended||modal=="appearance"||modal=="commandBriefing")return;
            if(TutorialDialogVisible){DrawTutorialDialog();return;}
            Transform scope=root;foreach(Transform child in root)if(child.name=="CommandShade")scope=child;
            bool strong=State.tutorial.stage<6;
            if(!strong&&!TutorialUrgent)
            {
                if(scope==root)
                {
                    Button("TutorialHelp",root,"回看教学",1230,91,164,35,OpenTutorialHelp);
                    Button("TutorialPause",root,"暂停 / 离开",1406,91,166,35,()=>{tutorialPopup="pause";Draw();});
                }
                return;
            }
            bool dialog=scope!=root;float x=dialog?14:220,y=dialog?160:170,w=dialog?208:362;
            var coach=Box("TutorialCoach",scope,x,y,w,498,Ink);SurfaceTheme(coach,false);
            Border(coach,0,0,w,498,Gold);
            Label(coach,TutorialUrgent?"现场决定":("教学 "+(State.tutorial.stage+1)+" / 7"),16,18,w-32,29,20,Gold);
            var body=Label(coach,TutorialCoachText(),16,67,w-32,320,18,TextColor);body.name="TutorialCoachBody";
            if(!TutorialUrgent)Button("TutorialNavigate",coach,dialog?"定位当前步骤  ›":"前往当前目标  ›",12,374,w-24,43,NavigateTutorial);
            Button("TutorialHelp",coach,"回看说明",12,425,(w-28)/2,42,OpenTutorialHelp);
            Button("TutorialPause",coach,"暂停",16+(w-28)/2,425,(w-28)/2,42,()=>{tutorialPopup="pause";Draw();});
        }
        void DrawTutorialDialog()
        {
            var shade=Box("TutorialShade",root,0,0,1600,900,new Color(0,0,0,.8f));
            var p=Box("TutorialPanel",shade,380,150,840,600,Paper);SurfaceTheme(p,true);
            bool pause=tutorialPopup=="pause",review=tutorialPopup=="help";int stage=review?tutorialHelpStage:State.tutorial.stage;
            Label(p,pause?"新手演练 / 随时可以继续":"教学 "+(stage+1)+" / 7  ·  "+tutorialTitles[stage],32,28,776,48,31,PaperInk);
            Label(p,pause?TutorialGoalLine():stage<6?"跟随高亮操作 · 查看不耗时 · Esc 可暂停":"弱引导阶段 · 全部功能开放",32,91,776,31,18,PaperMuted);
            Rule(p,32,137,776,PaperLine);
            Label(p,pause?"进度会保存在独立的教学档案中。\n\n正式行动、人物预设与正式战绩均保留。可以现在返回主菜单，下次从「新手演练」继续。\n\n本局第 "+State.turn+" / 8 夜。当前学习："+tutorialTitles[stage]+"。":TutorialLessonBody(stage),32,166,776,306,21,PaperInk).name="TutorialLessonText";
            Rule(p,32,497,776,PaperLine);
            if(review)
            {
                Button("TutorialPreviousLesson",p,"‹ 上一课",32,468,160,29,()=>{tutorialHelpStage--;Draw();},stage>0);
                Button("TutorialNextLesson",p,"下一课 ›",648,468,160,29,()=>{tutorialHelpStage++;Draw();},stage<State.tutorial.stage);
            }
            if(pause)
            {
                Button("TutorialExit",p,"保存教学，返回主菜单",32,529,374,48,()=>{if(SaveForExit()){tutorialPopup="";OpenHome();}});
                Button("TutorialContinue",p,"继续演练",430,529,378,48,()=>{tutorialPopup="";Draw();},true,true);
            }
            else
            {
                Button("TutorialPause",p,"暂停 / 离开",32,529,218,48,()=>{tutorialPopup="pause";Draw();});
                Button("TutorialAck",p,review?"返回当前练习":stage==6?"我来指挥":"明白了，动手试试",274,529,534,48,()=>{if(!review){State.tutorial.introShownStage=stage;Save(false);}tutorialPopup="";Draw();},true,true);
            }
        }
        bool TutorialAllows(string name)
        {
            if(name.StartsWith("Tutorial"))return true;
            if(name=="MemberGrowthHelp"||name=="MemberHistoryHelp")return true;
            if(State.tutorial.stage==3&&name=="CommandPageClose")return reportAll;
            if(name=="CommandMenu"||name=="CommandResume"||name=="CommandHome"||name=="CommandMenuQuit"||name=="Save")return true;
            if(name.EndsWith("Close")||name.EndsWith("Back")||name.EndsWith("Cancel")||name=="CommandPageClose"||name=="CommandTab_3")return true;
            int stage=State.tutorial.stage;
            if(name=="CommandNextAction")return true;
            switch(stage)
            {
                case 0:return name=="CommandTab_2"||name.StartsWith("CommandAgent_")||name=="CommandPeopleDetails";
                case 1:return name=="CommandCompare_"+State.tutorial.firstTaskId||name.StartsWith("CompareSelect_")||name=="CommandAuthorize"&&State.tutorial.comparedMember||name=="MissionPlan_0"||name=="MissionPlan_1"||name=="MissionModeExecute"||name=="MissionBackgroundDetails"||name=="MissionImpactDetails";
                case 2:return name=="CommandEnd"||name=="CommandExecute"||name=="CommandWorkOverview"||name.StartsWith("WorkOverviewTab_");
                case 3:return name=="CommandTab_4"||name=="ReportAll"||name=="ReportSummary";
                case 4:return name=="CommandTab_1"||name.StartsWith("RouteSlot_0_")||name.StartsWith("RouteCandidate_")||name=="RouteEnable_0"||name=="RouteDetails_0";
                case 5:return name=="CommandTab_2"||name=="CommandAgent_"+State.tutorial.restActorId||name=="CommandPeopleDetails"||name=="LifePolicy_1"&&selectedAgent==State.tutorial.restActorId;
                default:return true;
            }
        }
        void RestrictTutorialControls(Transform scope,Selectable[] controls)
        {
            if(atTitle||!InTutorial||State.ended||State.tutorial.stage>=6||TutorialDialogVisible||TutorialUrgent||PersistenceFailurePending)return;
            foreach(var control in controls)
            {
                if(!control.transform.IsChildOf(scope)||!control.interactable)continue;
                // Scrollbars retain navigation. Never re-enable a rule-disabled button.
                if(control is Button&&!TutorialAllows(control.name))control.interactable=false;
                else if(control is InputField)control.interactable=false;
            }
            foreach(var button in controls.OfType<Button>().Where(b=>b.interactable&&b.transform.IsChildOf(scope)&&TutorialTarget(b.name)))
            {var rect=(RectTransform)button.transform;Border(button.transform,-2,-2,rect.rect.width+4,rect.rect.height+4,PaperSurface(button.transform)?Accent:Gold);}
        }
        bool TutorialTarget(string name)
        {
            switch(State.tutorial.stage)
            {
                case 0:return name=="CommandTab_2"||name=="CommandPeopleDetails"||name=="MemberDetailsClose";
                case 1:return name=="CommandNextAction"||(!State.tutorial.comparedMember?name.StartsWith("CommandCompare_"):name=="CommandAuthorize");
                case 2:return name=="CommandEnd"||name=="CommandExecute";
                case 3:return name=="CommandTab_4"||(reportAll?name=="CommandPageClose":name=="ReportAll");
                case 4:
                    var route=State.command.routes.First(r=>r.id==0);
                    string next=route.source<0?"RouteSlot_0_0":route.liaison<0?"RouteSlot_0_1":route.courier<0?"RouteSlot_0_2":"RouteEnable_0";
                    return name=="CommandTab_1"||name==next;
                case 5:return name=="CommandTab_2"||name=="CommandPeopleDetails"||name=="LifePolicy_1"||name=="CommandAgent_"+State.tutorial.restActorId;
            }
            return false;
        }
        void DrawTutorialEnding()
        {
            var p=CommandModal(State.won?"演练完成 / 你已经独立指挥一局":"八夜演练结束 / 看看下一局怎么改",740,1040);
            Label(p,"真实任务、真实骰子、真实情报传递；本局不计入正式战绩。",32,99,976,30,19,Muted);
            Label(p,TutorialGoalLine(),32,155,976,39,24,Gold);
            Label(p,State.ending,32,219,976,177,21).name="TutorialEndingFeedback";
            Rule(p,32,418,976);
            Label(p,"接下来可以试一局正式行动：\n八夜短局有更严格的配额；二十夜剧情围绕三章委任展开。正式局人员和任务随种子变化，没有操作锁定。",32,441,976,93,20,Muted);
            Button("CommandEndReview",p,"回看每夜报告",32,575,300,48,()=>{modal="endingReview";reportNight=-1;Draw();});
            Button("TutorialEndReplay",p,"再练一局",354,575,300,48,()=>{if(SaveForExit()){OpenHome("tutorial");tutorialRestart=true;Draw();}});
            Button("TutorialStartCampaign",p,"配置正式行动",676,575,332,48,()=>{if(SaveForExit()){tutorialPopup="";newCampaign=true;OpenHome("new");}},true,true);
            Button("CommandEndHome",p,"保存教学，返回主菜单",32,655,976,46,ReturnHome);
        }
    }
}
