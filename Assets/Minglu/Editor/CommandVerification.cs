using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class CommandVerification
    {
        static int checks;
        static void Check(bool ok,string label){if(!ok)throw new Exception("Command: "+label);checks++;}
        static RunState Fresh(uint seed=911,int doctrine=0){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),seed,1,doctrine,false);CommandRules.Initialize(s);return s;}
        static void Configure(RunState s){var r=s.command.routes[0];r.source=0;r.liaison=1;r.courier=2;r.enabled=true;}
        [MenuItem("Minglu/验证组织首脑模式")]
        public static void Verify()
        {
            checks=0;var s=Fresh();Check(s.agents.Count==6&&s.agents.All(a=>a.appearance.enabled),"All crew generated");
            var same=Fresh();Check(JsonUtility.ToJson(s.command)==JsonUtility.ToJson(same.command),"Same seed reproduces life, tasks, looks");
            var early=Fresh(911,1);
            Check(early.money==s.money+12&&early.agents.All(a=>a.exposure==10),"Early liaison doctrine grants its displayed funding and exposure tradeoff");
            Check(early.intelligence==0&&early.command.delivered==0&&early.command.routes.All(r=>r.waiting==0&&r.atRelay==0),"Doctrine cannot bypass institution intelligence delivery");
            Check(Fresh(912).agents[0].name!=s.agents[0].name||Fresh(912).agents[0].intel!=s.agents[0].intel,"Seeds vary");
            Check(s.agents.Select(a=>a.name).Distinct().Count()==6&&s.agents.Select(a=>a.appearance.sex).Distinct().Count()==2,"Unique names and both sexes");
            Check(DispatchRules.Probability(5,2)>DispatchRules.Probability(3,2),"More ability dice improves chance");
            Configure(s);Check(CommandRules.RouteError(s,s.command.routes[0])=="","Valid route");
            s.command.routes[1].source=0;s.command.routes[1].liaison=3;s.command.routes[1].courier=4;Check(CommandRules.RouteError(s,s.command.routes[1])!="","Cross-line double role rejected");
            var t=s.command.tasks.First(x=>x.level==1);Check(CommandRules.Assign(s,0,t.id)=="","Delegation");
            Check(CommandRules.Assign(s,1,t.id)!="","No double owner");
            t.actor=-1;int oldLevel=t.level;t.level=4;Check(CommandRules.Assign(s,0,t.id)!="","Identity level gate");t.level=oldLevel;
            int need=t.required;t.required=7;Check(CommandRules.Assign(s,0,t.id)!="","Impossible action rejected");t.required=need;CommandRules.Assign(s,0,t.id);
            s.command.config.eventChance=0;CommandRules.Resolve(s);Check(s.command.routes[0].delivered==0,"No teleport through chain");
            s=Fresh();Configure(s);s.command.config.eventChance=0;s.command.people[1].policy=1;s.command.routes[0].waiting=1;
            for(int i=0;i<5;i++)CommandRules.Resolve(s);
            Check(s.command.routes[0].waiting>0&&s.command.routes[0].atRelay==0&&s.command.delivered==0,"Unavailable liaison blocks chain");
            s.command.people[1].policy=0;for(int i=0;i<6;i++)CommandRules.Resolve(s);Check(s.command.delivered>0,"Chain resumes without per-packet orders");
            Check(s.command.people.Any(p=>p.level>=3),"Civilian life levels identity");
            s=Fresh();s.command.eventKind=1;s.command.eventActor=0;int money=s.money;CommandRules.Choose(s,true);Check(s.money==money-6&&CommandRules.Availability(s,0)!="","Family leave affects availability");
            s.command.eventKind=2;CommandRules.Choose(s,true);Check(s.command.pauseUntil==s.turn,"Purge choice pauses routes");
            s=Fresh();Configure(s);s.command.people[0].loyalty=0;s.command.eventActor=0;bool betrayed=false;
            for(int i=0;i<30&&!betrayed;i++){s.command.eventKind=3;CommandRules.Choose(s,false);betrayed=s.agents[0].condition==Condition.Lost;}
            Check(betrayed&&!s.command.routes[0].enabled,"Betrayal disables known chain");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-command-check.json");s=Fresh();Configure(s);CommandRules.Assign(s,3,s.command.tasks[0].id);PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);
            Check(JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command),"Whole command state restored");CommandRules.Resolve(s);CommandRules.Resolve(loaded);Check(s.random==loaded.random&&JsonUtility.ToJson(s.command)==JsonUtility.ToJson(loaded.command),"Save preserves random future");
            int delivered=0,wins=0;
            for(uint seed=1;seed<=60;seed++)
            {
                s=Fresh(seed);Configure(s);
                while(!s.ended)
                {
                    if(s.command.eventKind!=0)CommandRules.Choose(s,s.money>=10);
                    foreach(var task in s.command.tasks.Where(x=>x.status==0))
                    {var a=s.agents.Where(x=>x.id>=3&&CommandRules.TaskError(s,x.id,task)=="").OrderByDescending(x=>CommandRules.Dice(s,x,task.skill)).FirstOrDefault();if(a!=null)CommandRules.Assign(s,a.id,task.id);}
                    CommandRules.Resolve(s);CommandRules.Validate(s);PrototypeSave.Write(s,path);s=PrototypeSave.Read(path);
                }
                delivered+=s.command.delivered;if(s.won)wins++;
            }
            Check(delivered>100,"Routes produce across many seeds");
            s=Fresh();s.command.completed=2;s.command.delivered=20;s.progress=100;foreach(var p in s.command.people)p.level=3;CommandRules.CheckGate(s);Check(s.ended&&s.won,"Gate victory reachable");
            File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");Debug.Log("MINGLU_COMMAND_VERIFY_SUCCESS: "+checks+" checks / 60 complete runs / deliveries "+delivered+" / baseline wins "+wins);
        }
        public static void BatchDeliver(){try{Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
