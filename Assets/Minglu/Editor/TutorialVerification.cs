using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class TutorialVerification
    {
        static int checks;
        static void Require(bool condition,string message){checks++;if(!condition)throw new Exception("Tutorial: "+message);}
        static RunState Copy(RunState s){return JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));}
        static void Connect(RunState s)
        {
            var t=s.tutorial;var route=s.command.routes[0];
            Require(CommandRules.ReplaceRouteMember(s,route,0,t.sourceId)=="","source assignment");
            Require(CommandRules.ReplaceRouteMember(s,route,1,t.liaisonId)=="","liaison assignment");
            Require(CommandRules.ReplaceRouteMember(s,route,2,t.courierId)=="","courier assignment");
            Require(CommandRules.RouteError(s,route)=="","complete route is valid");route.enabled=true;
        }
        static RunState Play(string path,bool fundLeave,bool roundtrip)
        {
            var s=TutorialRules.Create(ConfigStore.Load());
            s.tutorial.inspectedMember=true;s.tutorial.comparedMember=true;TutorialRules.Refresh(s);
            for(int night=1;night<=TutorialRules.Nights;night++)
            {
                Require(!s.ended&&s.turn==night,"lesson ended early or skipped a night");
                if(s.command.eventKind!=0)
                {
                    Require(s.command.eventKind==1&&s.tutorial.eventOffered,"unexpected tutorial incident");
                    Require(CommandRules.Choose(s,fundLeave)=="","family choice must always have a usable option");
                    Require(s.tutorial.eventHandled,"real choice did not register event fact");
                }
                if(night==2)
                {
                    Connect(s);CommandRules.SetPolicy(s,s.tutorial.restActorId,1);s.tutorial.lifeArranged=true;
                }
                if(night==3)CommandRules.SetPolicy(s,s.tutorial.restActorId,0);
                var route=s.command.routes[0];
                int[] pipeline={s.tutorial.sourceId,s.tutorial.liaisonId,s.tutorial.courierId};
                foreach(int id in new[]{s.tutorial.firstTaskId,s.tutorial.secondTaskId})
                {
                    if(night==1&&id!=s.tutorial.firstTaskId)continue;
                    var task=s.command.tasks.First(t=>t.id==id);if(task.status!=0)continue;
                    var actor=s.agents.Where(a=>!pipeline.Contains(a.id)&&CommandRules.TaskError(s,a.id,task)=="")
                        .OrderByDescending(a=>CommandRules.TaskDice(s,a,task)).FirstOrDefault();
                    Require(actor!=null,"core task has no available learner-compatible actor");
                    Require(CommandRules.Assign(s,actor.id,task.id)=="","real task authorization failed");
                }
                TutorialRules.Refresh(s);int oldRelay=route.atRelay,oldDelivered=s.command.delivered;uint rng=s.random;
                TutorialRules.Refresh(s);TutorialRules.Refresh(s);Require(s.random==rng,"reading tutorial progress consumed RNG");
                CommandRules.Resolve(s);
                Require(oldRelay>0||s.command.delivered==oldDelivered,"intelligence teleported through two links");
                Require(s.intelligence==s.command.delivered,"lesson granted internal intelligence outside real deliveries");
                if(!string.IsNullOrEmpty(s.command.tasks.First(t=>t.id==s.tutorial.firstTaskId).lastResult))s.tutorial.reportRead=true;
                TutorialRules.Refresh(s);CommandRules.Validate(s);TutorialRules.Validate(s);
                Require(s.tutorial.resolvedNights==night,"resolution count does not match executed nights");
                if(night<8)Require(!s.ended,"normal victory quota bypassed the eight-night lesson");
                if(roundtrip)
                {
                    PrototypeSave.Write(s,path);var read=PrototypeSave.Read(path);
                    Require(JsonUtility.ToJson(s)==JsonUtility.ToJson(read),"nightly save/load changed tutorial state or RNG");s=read;
                }
            }
            Require(s.ended&&s.won&&s.tutorial.completed&&s.tutorial.stage==6,"recommended real eight-night play did not satisfy the lesson goals");
            Require(s.command.completed>=2&&s.command.delivered>=1&&s.tutorial.eventHandled,"completion missing actual core results");
            return s;
        }
        static uint FailingRollSeed(int dice)
        {
            // Fault fixture only: select a valid deterministic RNG state whose
            // first real roll fails. Production tutorial never rewrites dice.
            for(uint seed=1;seed<100000;seed++)
            {
                uint x=seed;bool failed=true;
                for(int i=0;i<dice;i++){x^=x<<13;x^=x>>17;x^=x<<5;if(x%6>=3)failed=false;}
                if(failed)return seed;
            }
            throw new Exception("could not find a deterministic failed-roll fixture");
        }
        static void VerifyFailure(string path)
        {
            var s=TutorialRules.Create(ConfigStore.Load());var t=s.tutorial;var task=s.command.tasks.First(x=>x.id==t.firstTaskId);
            t.inspectedMember=true;t.comparedMember=true;
            Require(CommandRules.Assign(s,t.freeActorId,task.id)=="","failure fixture assignment");TutorialRules.Refresh(s);
            Require(t.stage==2,"signing must enter review stage before resolving");
            s.random=FailingRollSeed(CommandRules.TaskDice(s,CommandRules.Agent(s,t.freeActorId),task));
            PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);
            CommandRules.Resolve(s);CommandRules.Resolve(loaded);
            Require(JsonUtility.ToJson(s)==JsonUtility.ToJson(loaded),"loading before a failed roll changed the outcome");
            Require(task.status==0&&task.stepsDone==0&&task.actor==-1&&task.deadline==8,"failure lost the real retry window or silently succeeded");
            Require(t.stage==3&&!string.IsNullOrEmpty(task.lastResult)&&s.command.completed==0&&s.intelligence==0,"failed result did not advance to report reading");
            t.reportRead=true;TutorialRules.Refresh(s);Require(t.stage==4,"failed action should still teach report/chain flow");
            Require(CommandRules.Assign(s,t.freeActorId,task.id)=="","failed task cannot be reauthorized");
            TutorialRules.Refresh(s);Require(t.stage==4,"retry regressed lesson stage");
            uint random=s.random;var serialized=JsonUtility.ToJson(s);TutorialRules.AfterResolve(s);
            Require(s.random==random&&JsonUtility.ToJson(s)==serialized,"duplicate post-resolve hook changed a saved night");
        }
        static void VerifyInsufficient()
        {
            var s=TutorialRules.Create(ConfigStore.Load());
            while(!s.ended)
            {
                if(s.command.eventKind!=0)Require(CommandRules.Choose(s,false)=="","unfunded family leave must not deadlock");
                CommandRules.Resolve(s);
            }
            Require(s.turn==8&&!s.won&&!s.tutorial.completed&&s.command.completed==0&&s.command.delivered==0,"empty practice falsely passed or never ended");
            Require(s.ending.Contains("完成2项任务")&&s.ending.Contains("机构联络链")&&s.ending.Contains("重新练习"),"failure review lacks concrete recovery guidance");
            TutorialRules.Validate(s);
        }
        static void InvalidWriteDoesNotReplace(RunState baseline,string path,Action<RunState> mutate,string label)
        {
            PrototypeSave.Write(baseline,path);string before=File.ReadAllText(path);var invalid=Copy(baseline);mutate(invalid);
            bool rejected=false;try{PrototypeSave.Write(invalid,path);}catch(Exception){rejected=true;}
            Require(rejected&&File.ReadAllText(path)==before,"invalid tutorial save replaced good data: "+label);
        }
        public static void Verify()
        {
            checks=0;var config=ConfigStore.Load();string source=JsonUtility.ToJson(config);var start=TutorialRules.Create(config);
            Require(JsonUtility.ToJson(config)==source,"tutorial mutated global source config");
            Require(TutorialRules.IsTutorial(start)&&start.roguelikeRun&&start.initialSeed==43&&start.config.Value("turns")==8,"isolated lesson identity/contract");
            Require(start.agents.Count==6&&start.agents.Select(a=>a.id).Distinct().Count()==6,"actual seeded crew was not retained");
            Require(CommandRules.Agent(start,0).name=="叶含章"&&CommandRules.Agent(start,0).intel==5&&CommandRules.Person(start,0).institution==1&&CommandRules.Agent(start,1).persuade==5&&CommandRules.Agent(start,2).stealth==5,"seed 43 teaching roles changed; re-evaluate the lesson recommendations");
            var first=start.command.tasks.First(x=>x.id==start.tutorial.firstTaskId);
            Require(first.required==1&&first.stepsRequired==1&&first.deadline==8&&first.intelReward==0,"first task no longer provides a real low-risk retryable lesson");
            Require(DispatchRules.Probability(CommandRules.TaskDice(start,CommandRules.Agent(start,3),first),1)>DispatchRules.Probability(CommandRules.TaskDice(start,CommandRules.Agent(start,4),first),1),"recommended competence comparison has no probability difference");
            var early=Copy(start);early.command.completed=8;early.command.delivered=8;early.progress=100;
            foreach(var p in early.command.people)p.level=4;
            CommandRules.CheckGate(early);Require(!early.ended,"ordinary quota ended tutorial before night eight");
            Require(CommandRules.GateText(start).Contains("八夜教学")&&CommandRules.GateText(start).Contains("1份机构情报"),"night report advertises the ordinary victory contract");
            var flexible=Copy(start);flexible.tutorial.stage=4;
            var flexibleRoute=flexible.command.routes[0];flexibleRoute.source=0;flexibleRoute.liaison=3;flexibleRoute.courier=2;flexibleRoute.enabled=true;
            TutorialRules.Refresh(flexible);
            Require(flexible.tutorial.stage==5&&flexible.tutorial.restActorId!=3&&!new[]{0,3,2}.Contains(flexible.tutorial.restActorId),"valid alternative chain forced one of its members to rest");
            var occupied=Copy(start);occupied.tutorial.stage=4;CommandRules.Person(occupied,3).institution=2;
            var routeA=occupied.command.routes[0];routeA.source=0;routeA.liaison=1;routeA.courier=2;routeA.enabled=true;
            var routeB=occupied.command.routes[1];routeB.source=3;routeB.liaison=4;routeB.courier=5;routeB.enabled=true;
            TutorialRules.Refresh(occupied);
            Require(occupied.tutorial.stage==4&&routeA.enabled&&routeB.enabled&&occupied.command.people.All(p=>p.policy==0),"no-reserve fallback silently disrupts an occupied role");
            string folder=Path.Combine(Application.temporaryCachePath,"minglu-tutorial-verification-"+Guid.NewGuid().ToString("N"));
            string formal=Path.Combine(folder,"formal.json"),lesson=Path.Combine(folder,"tutorial.json"),profilePath=Path.Combine(folder,"profile.json"),legacy=Path.Combine(folder,"legacy.json");
            try
            {
                var ordinary=RunLifecycle.Create(ConfigStore.Load(),911,0,0,false);CommandRules.Initialize(ordinary);
                PrototypeSave.Write(ordinary,formal);var profile=new PlayerProfile();RunLifecycle.WriteProfile(profile,profilePath);
                string formalBefore=File.ReadAllText(formal),profileBefore=File.ReadAllText(profilePath);
                var result=Play(lesson,false,true);var funded=Play(lesson,true,false);
                Require(!RunLifecycle.Record(profile,result)&&!RunLifecycle.Record(profile,funded)&&profile.records.Count==0,"tutorial contaminated ordinary achievements");
                Require(File.ReadAllText(formal)==formalBefore&&File.ReadAllText(profilePath)==profileBefore,"lesson wrote ordinary save/profile bytes");
                var replay=TutorialRules.Create(ConfigStore.Load());Require(replay.runId!=result.runId&&replay.tutorial.stage==0&&replay.command.completed==0,"replay retained a previous lesson's progress");
                Require(JsonUtility.ToJson(replay.command)==JsonUtility.ToJson(start.command),"replay did not reproduce the fixed task/crew contract");
                VerifyFailure(lesson);VerifyInsufficient();
                string old=Regex.Replace(formalBefore,"\\\"tutorial\\\"\\s*:\\s*(null|\\{[^{}]*\\})\\s*,?","");
                Require(!old.Contains("\"tutorial\""),"legacy fixture still contains the new field");File.WriteAllText(legacy,old);
                var oldRead=PrototypeSave.Read(legacy);Require(!TutorialRules.IsTutorial(oldRead)&&oldRead.random==ordinary.random,"old missing-field save became a tutorial or changed RNG");
                ordinary.ended=true;ordinary.won=true;Require(RunLifecycle.Record(profile,ordinary)&&profile.records.Count==1,"ordinary completed runs stopped archiving");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.stage=7,"stage");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.sourceId=999,"source");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.resolvedNights=2,"night count");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.revision=2,"future revision");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.enabled=false,"disabled active lesson");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.eventHandled=true,"unoffered event");
                InvalidWriteDoesNotReplace(start,lesson,s=>s.tutorial.completed=true,"unearned completion");
                var pending=Copy(start);pending.turn=3;pending.tutorial.resolvedNights=pending.tutorial.lastResolvedNight=2;
                pending.tutorial.resolvingNight=3;pending.turn=4;pending.command.eventKind=2;pending.command.eventActor=0;pending.command.eventText="existing incident";
                TutorialRules.AfterResolve(pending);
                Require(pending.command.eventKind==2&&pending.command.eventText=="existing incident"&&!pending.tutorial.eventOffered,"lesson overwrote an existing incident");
            }
            finally
            {
                foreach(string path in new[]{formal,lesson,profilePath,legacy})foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);
                if(Directory.Exists(folder))Directory.Delete(folder,false);
            }
            Debug.Log("MINGLU_TUTORIAL_VERIFY_SUCCESS: "+checks+" checks; two real eight-night practices, zero-cost and funded event choices, nightly exact save/RNG roundtrips, deterministic failed roll/retry, incomplete practice, original save/profile isolation, old null/omitted field, strict bad-data rejection. UI interaction facts are simulated here; physical UI smoke is separate.");
        }
    }
}
