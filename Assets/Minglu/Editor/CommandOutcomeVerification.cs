using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class CommandOutcomeVerification
    {
        static int checks;
        static void Check(bool value,string reason){if(!value)throw new Exception("Command outcomes: "+reason);checks++;}
        static RunState Fresh()
        {
            var s=RunLifecycle.Create(ConfigStore.Load(),911,0,0,false);CommandRules.Initialize(s);
            s.command.tasks.Clear();s.command.notices.Clear();s.command.noticeArchive.Clear();s.command.config.eventChance=0;
            return s;
        }
        static CommandTask Order(RunState s,int actor=0,int steps=1,bool recon=false)
        {
            var t=new CommandTask{id=s.command.nextTask++,actor=actor,title="结果验收任务"+actor,body="隔离规则验收",origin="上级指示",district=s.config.districts[0].id,deadline=s.config.Value("turns"),stepsRequired=steps,reconnaissance=recon};
            s.command.tasks.Add(t);return t;
        }
        // Select a deterministic die sequence for the branch under test. This
        // is a boundary fixture, not a difficulty or player-win-rate sample.
        static uint RollSeed(int dice,int required,bool success)
        {
            for(uint seed=1;seed<100000;seed++)
            {
                uint value=seed;int successes=0;
                for(int i=0;i<dice;i++){value^=value<<13;value^=value>>17;value^=value<<5;if(value%6>=3)successes++;}
                if((successes>=required)==success)return seed;
            }
            throw new Exception("Unable to construct outcome die fixture");
        }
        static void ArrangeRoll(RunState s,CommandTask t,bool success)
        {s.random=RollSeed(CommandRules.TaskDice(s,CommandRules.Agent(s,t.actor),t),CommandRules.ActionRequired(t),success);}
        static CommandNotice Bulletin(RunState s,int night)
        {return s.command.notices.Single(n=>n.title=="夜间要报 · 第"+night+"夜");}
        static void VerifyTaskResults()
        {
            var s=Fresh();var t=Order(s);t.plan=1;ArrangeRoll(s,t,false);int money=s.money;
            CommandRules.Resolve(s);var n=Bulletin(s,1);
            Check(t.status==0&&t.actor<0&&t.stepsDone==0,"First-night failed action stays retryable");
            Check(n.body.Contains("任务失败")&&n.body.Contains(t.title)&&n.body.Contains("骰 [")&&n.body.Contains("重新比较人选"),"Failure reports actual dice, result and next action");
            Check(n.tab==0&&n.task==t.id&&n.night==1,"Failure opens the failed task and retains its execution night");
            Check(s.money==money-4&&n.body.Contains("投入已消耗"),"Failure does not imply refunded preparation");
            Check(s.command.noticeArchive.Any(a=>a.body==n.body),"Failure is retained after reading");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-outcomes-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                s.command.notices.Clear();PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);
                Check(loaded.command.notices.Count==0&&loaded.command.noticeArchive.Any(a=>a.task==t.id&&a.body==n.body),"Read results do not become unread on reload and preserve their destination");
                Check(loaded.random==s.random&&loaded.turn==s.turn,"Saving an outcome preserves the random future and night");
            }
            finally{foreach(string file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);}

            s=Fresh();t=Order(s,0,2);ArrangeRoll(s,t,true);CommandRules.Resolve(s);n=Bulletin(s,1);
            Check(t.stepsDone==1&&t.status==0&&n.body.Contains("阶段完成 1/2")&&n.body.Contains("尚未取得全部完成奖励"),"Partial progress is shown without pretending the whole task paid out");
            Check(n.task==t.id&&s.command.notices.Count==1,"Partial progress uses one batch notice and a useful next destination");

            foreach(bool success in new[]{false,true})
            {
                s=Fresh();t=Order(s,0,1,true);ArrangeRoll(s,t,success);CommandRules.Resolve(s);n=Bulletin(s,1);
                Check(s.command.notices.Count==1&&n.body.Contains(success?"调查完成":"调查失败"),"Investigation result is merged once into the nightly bulletin");
                Check(t.stepsDone==0&&t.reconComplete==success&&t.actor<0&&s.intelligence==0,"Investigation outcome does not advance execution, retain the actor or mint intelligence");
                Check(n.task==t.id&&n.body.Contains(success?"授权执行目标":"可改为执行"),"Investigation gives an accurate next step");
            }

            s=Fresh();t=Order(s);ArrangeRoll(s,t,true);CommandRules.Resolve(s);n=Bulletin(s,1);
            string complete="完成「"+t.title+"」";
            Check(t.status==1&&n.body.IndexOf(complete,StringComparison.Ordinal)==n.body.LastIndexOf(complete,StringComparison.Ordinal)&&n.body.Contains(complete),"Ordinary completion keeps exactly one success entry");
            s=Fresh();t=Order(s);Order(s,1);ArrangeRoll(s,t,false);CommandRules.Resolve(s);n=Bulletin(s,1);
            Check(s.command.notices.Count==1&&s.command.tasks.All(task=>n.body.Contains(task.title)),"Multiple task outcomes share a single nightly bulletin");

            s=Fresh();t=Order(s);CommandRules.Person(s,0).leaveUntil=1;uint random=s.random;CommandRules.Resolve(s);n=Bulletin(s,1);
            Check(t.actor<0&&t.lastResult==""&&n.body.Contains("未执行")&&n.body.Contains("生活事务")&&n.body.Contains("未扣除任务投入"),"Orders invalidated before rolling explain why nothing happened");
            Check(s.random==random,"Reporting withdrawn orders consumes no dice");
            s=Fresh();t=Order(s);t.plan=1;s.money=0;random=s.random;CommandRules.Resolve(s);n=Bulletin(s,1);
            Check(t.actor<0&&t.lastResult==""&&n.body.Contains("筹备需4银两")&&n.body.Contains("未扣除任务投入")&&s.random==random,"Execution-time resource failures report their reason without rolling");

            s=Fresh();t=Order(s);t.deadline=1;ArrangeRoll(s,t,false);CommandRules.Resolve(s);n=Bulletin(s,1);
            Check(t.status==2&&n.body.Contains("已截止")&&!n.body.Contains("重新比较人选")&&n.task<0,"Expired failed tasks never promise a retry or link to an invalid task");
            Check(n.body.Contains("本夜任务到期：额外压力+3")&&n.body.IndexOf("本夜任务到期",StringComparison.Ordinal)==n.body.LastIndexOf("本夜任务到期",StringComparison.Ordinal),"Failed attempts show the additional expiry penalty once");
            s=Fresh();t=Order(s);s.turn=s.config.Value("turns");ArrangeRoll(s,t,false);CommandRules.Resolve(s);n=Bulletin(s,s.turn);
            Check(s.ended&&n.body.Contains("本局已结束")&&n.tab==4&&n.task<0,"Final-night failures are retained for the read-only ending review");
            Check(s.command.noticeArchive.Any(a=>a.body==n.body),"Final-night outcome is archived even though no next night begins");
        }
        static void VerifyRetrySchedule()
        {
            foreach(bool legacy in new[]{false,true})foreach(int id in new[]{0,1})
            {
                var s=Fresh();s.command.legacySourceSchedule=legacy;s.turn=3;var route=s.command.routes[id];
                if(!CommandRules.SourceWorksTonight(s,route))s.turn++;
                string before=JsonUtility.ToJson(s);string hint=CommandRules.RouteFailureRetryText(s,route,0);
                Check(hint.Contains("第"+(s.turn+2)+"夜")&&!hint.Contains("下夜自动"),"Source retry follows the actual current or legacy observation period");
                Check(before==JsonUtility.ToJson(s),"Retry preview changes neither state nor random sequence");
            }
            var current=Fresh();current.command.tasks.Clear();var r=current.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;
            current.random=RollSeed(CommandRules.Dice(current,current.agents[0],0),1,false);CommandRules.Resolve(current);
            Check(r.waiting==0&&r.last.Contains("第3夜进入下个观察周期"),"Actual failed production reports the correct next observation night");
            current.turn=current.config.Value("turns")-1;
            Check(CommandRules.RouteFailureRetryText(current,r,0).Contains("已无后续观察周期"),"Late source failure does not promise a period outside the run");
            Check(CommandRules.RouteFailureRetryText(current,r,1).Contains("下夜自动重试"),"Relay can still retry on the final night");
            current.turn++;Check(CommandRules.RouteFailureRetryText(current,r,2).Contains("已无后续行动夜"),"Final courier failure does not promise a ninth/twenty-first night");
            current=Fresh();current.command.tasks.Clear();r=current.command.routes[0];r.waiting=1;r.age=6;CommandRules.Resolve(current);
            Check(r.waiting==0&&r.last.Contains("达到7夜"),"Expiration copy agrees with the seven-night counter");
        }
        public static void Verify()
        {
            checks=0;VerifyTaskResults();VerifyRetrySchedule();
            Debug.Log("MINGLU_COMMAND_OUTCOME_VERIFY_SUCCESS: "+checks+" checks; failure/partial/recon/cancel/finale feedback, one batch, save/RNG and source-period retry");
        }
    }
}
