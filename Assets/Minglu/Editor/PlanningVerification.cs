using System;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class PlanningVerification
    {
        static int checks;
        static void Check(bool ok,string message){if(!ok)throw new Exception("Planning: "+message);checks++;}
        static RunState Fresh()
        {
            var campaign=CampaignRules.Load();
            var state=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),campaign),911,1,0,false);
            CommandRules.Initialize(state);state.command.tasks.Clear();state.turn=2;return state;
        }
        public static void Verify()
        {
            checks=0;var s=Fresh();
            var task=new CommandTask{id=990,title="限期核验",skill=0,required=1,level=1,deadline=3,stepsRequired=2};s.command.tasks.Add(task);
            Check(CommandRules.TaskNightsLeft(s,task)==2&&CommandRules.TaskAttemptsNeeded(task)==2,"Two stages consume two remaining nights");
            Check(CommandRules.TaskUrgent(s,task)&&CommandRules.NightWarnings(s).Contains("今夜必须开始"),"Night confirmation warns on the last starting night");
            task.reconnaissance=true;
            Check(CommandRules.TaskAttemptsNeeded(task)==3&&!CommandRules.TaskCanFinish(s,task),"Investigation adds an extra night before execution");
            Check(CommandRules.NightWarnings(s).Contains("来不及"),"Impossible plans retain an explicit timing warning");
            task.deadline=4;task.actor=0;
            Check(CommandRules.NightWarnings(s).Contains("已安排调查")&&!CommandRules.TaskTimingText(s,task).Contains("必须开始"),"Authorized investigation is distinct from an unassigned order");
            task.actor=-1;task.reconnaissance=false;task.stepsRequired=1;
            string before=JsonUtility.ToJson(s);
            Check(CommandRules.CanStaffTask(s,task)&&before==JsonUtility.ToJson(s),"Staff query is read-only and finds available candidates");
            foreach(var person in s.command.people)person.policy=1;
            Check(!CommandRules.CanStaffTask(s,task),"Living-priority members are not recommended for tasks");
            s.turn=s.config.Value("turns");task.deadline=s.turn+5;task.stepsRequired=2;
            Check(CommandRules.TaskNightsLeft(s,task)==1&&!CommandRules.TaskCanFinish(s,task),"Campaign end bounds the apparent task deadline");

            s=Fresh();var route=s.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;
            CommandRules.Person(s,0).institution=1;
            var courierOrder=new CommandTask{id=991,title="机动委托",actor=2,deadline=8};s.command.tasks.Add(courierOrder);
            Check(!CommandRules.RouteSlotHasWork(s,route,2),"An empty relay gives its courier no work tonight");
            Check(!CommandRules.NightWarnings(s).Contains(s.agents[2].name+"优先执行任务"),"Idle courier does not produce a false chain conflict");
            CommandRules.Person(s,2).policy=1;
            Check(!CommandRules.NightWarnings(s).Contains(s.agents[2].name+"岗位等待"),"Idle courier leave is not reported as delayed transmission");
            CommandRules.Person(s,2).policy=0;route.waiting=1;courierOrder.actor=1;
            Check(CommandRules.RouteSlotHasWork(s,route,1),"Pending source intelligence requires the liaison tonight");
            Check(CommandRules.NightWarnings(s).Contains(s.agents[1].name+"优先执行任务"),"Busy liaison delay is shown in the night confirmation");
            courierOrder.actor=-1;CommandRules.Person(s,1).policy=1;
            Check(CommandRules.NightWarnings(s).Contains(s.agents[1].name+"岗位等待"),"Unavailable working liaison retains a warning");
            s.command.pauseUntil=s.turn;
            Check(!CommandRules.RouteSlotHasWork(s,route,1)&&!CommandRules.NightWarnings(s).Contains(s.agents[1].name+"岗位等待"),"Whole-line pause does not invent additional worker conflicts");
            s.command.pauseUntil=0;route.enabled=false;route.age=5;
            Check(!CommandRules.RouteSlotHasWork(s,route,1)&&CommandRules.NightWarnings(s).Contains("积压即将失效"),"Paused packets retain their expiry warning");
            route.enabled=true;s.agents[0].condition=Condition.Lost;
            Check(!CommandRules.RouteSlotHasWork(s,route,1)&&CommandRules.NightWarnings(s).Contains("链1暂停"),"Invalid enabled routes remain visible as errors");
            Debug.Log("MINGLU_PLANNING_VERIFY_SUCCESS: "+checks+" checks / stage timing, read-only staffing, active versus idle chain slots, pause and invalid route warnings");
        }
    }
}
