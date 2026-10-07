using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class CampaignCommandVerification
    {
        static int checks;
        static void Check(bool condition,string label){if(!condition)throw new Exception("Command campaign: "+label);checks++;}
        static RunState Fresh(uint seed=3,bool shortRun=false)
        {
            var config=shortRun?ConfigStore.Load():CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load());
            var s=RunLifecycle.Create(config,seed,1,0,false);CommandRules.Initialize(s);s.command.config.eventChance=0;return s;
        }
        static void SupplyEvidence(RunState s,int institution)
        {
            var route=s.command.routes[0];route.source=0;s.command.people[0].institution=institution;
            route.waiting++;CommandRules.CampaignOnSourceProduced(s,0,route.id);
            route.waiting--;route.atRelay++;CommandRules.CampaignOnRelay(s,route.id);
            route.atRelay--;route.delivered++;CommandRules.CampaignOnDelivery(s,0,route.id);
        }
        static string RemoveJsonProperty(string json,string name)
        {
            int start=json.IndexOf("\""+name+"\":",StringComparison.Ordinal);if(start<0)return json;
            int value=start+name.Length+3,end=value,depth=0;bool quoted=false,escaped=false;
            for(;end<json.Length;end++)
            {
                char c=json[end];
                if(quoted){if(escaped)escaped=false;else if(c=='\\')escaped=true;else if(c=='"')quoted=false;continue;}
                if(c=='"'){quoted=true;continue;}
                if(c=='{'||c=='['){depth++;continue;}
                if(c=='}'||c==']'){if(depth==0)break;depth--;continue;}
                if(c==','&&depth==0)break;
            }
            if(end<json.Length&&json[end]==',')end++;else if(start>0&&json[start-1]==',')start--;
            return json.Remove(start,end-start);
        }
        static void VerifyOldContractAndSnapshot()
        {
            var shortRun=Fresh(7,true);Check(!CommandRules.HasCommandCampaign(shortRun)&&CommandRules.CampaignReadyForVictory(shortRun),"Eight-night mode keeps its existing contract");
            var s=Fresh();uint random=s.random;string baseline=JsonUtility.ToJson(s);
            for(int i=0;i<10;i++)
            {
                CommandRules.CampaignObjective(s);CommandRules.CampaignProgressText(s);CommandRules.CampaignApproachDescription(s,0);CommandRules.CampaignApproachError(s,0);CommandRules.CampaignApproachError(s,1);CommandRules.CampaignEvidenceSummary(s);
            }
            Check(s.random==random&&baseline==JsonUtility.ToJson(s),"Repeated previews neither mutate the campaign nor consume simulation RNG");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-command-campaign-check.json");
            try
            {
                s.command.campaign.scenarioData.title="存档中的委任标题";
                PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);
                Check(loaded.command.campaign.scenarioData.title=="存档中的委任标题"&&loaded.random==s.random,"Loaded campaign uses its saved authored content and exact random future");
                string legacy=RemoveJsonProperty(RemoveJsonProperty(JsonUtility.ToJson(s),"campaignEnabled"),"campaign");
                // RunState also has an old campaignEnabled/campaign pair. Remove
                // the remaining occurrence to reproduce a pre-command-story save.
                legacy=RemoveJsonProperty(RemoveJsonProperty(legacy,"campaignEnabled"),"campaign");
                File.WriteAllText(path,legacy);loaded=PrototypeSave.Read(path);
                Check(!CommandRules.HasCommandCampaign(loaded)&&CommandRules.CampaignReadyForVictory(loaded),"Missing fields do not silently opt an old save into new story gates");
                Check(loaded.random==s.random,"Legacy validation consumes no randomness");
                loaded.command.completed=2;loaded.command.delivered=20;loaded.progress=100;foreach(var p in loaded.command.people)p.level=3;
                CommandRules.CheckGate(loaded);Check(loaded.ended&&loaded.won,"An old save still wins using its original quotas");
                var malformed=Fresh();malformed.command.campaign=null;bool rejected=false;
                try{CommandRules.ValidateCommandCampaign(malformed);}catch(Exception){rejected=true;}
                Check(rejected,"Enabled story with missing data is rejected, not silently downgraded");
            }
            finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
        }
        static void VerifyChoicesAndCapacity()
        {
            var s=Fresh();uint random=s.random;
            Check(CommandRules.CampaignApproachError(s,0).Contains("尚未收到"),"Fast method requires a actually delivered institutional report");
            Check(CommandRules.CampaignApproachError(s,1)=="","Alternative method is available with no institutional evidence");
            int money=s.money,intel=s.intelligence;
            Check(CommandRules.StartCampaignApproach(s,0)!=""&&s.command.campaign.activeTask==-1&&s.random==random,"Rejected evidence method changes nothing");
            while(s.command.tasks.Count(t=>t.status==0)<9)
                CommandRules.CreateTask(s,new CommandTemplate{title="支线容量测试 "+s.command.nextTask,body="支线",skill=0},"测试",-1,0);
            random=s.random;
            Check(CommandRules.StartCampaignApproach(s,1)==""&&s.command.tasks.Count(t=>t.status==0)==10,"Nine optional tasks cannot block an essential main objective");
            Check(s.random==random&&s.money==money&&s.intelligence==intel,"Choosing a fixed story method consumes neither RNG nor resources");
            int taskId=s.command.campaign.activeTask;var task=s.command.tasks.First(t=>t.id==taskId);
            Check(task.level==1&&task.profession==""&&task.stepsRequired==2,"Fallback is independent of profession and promotion luck");
            Check(CommandRules.CancelCampaignApproach(s)==""&&task.status==2&&s.command.campaign.activeTask==-1&&s.random==random,"Unused method can be changed without rerolling or deadline penalty");
            SupplyEvidence(s,CommandRules.CampaignCurrentChapter(s).approaches[0].evidenceInstitution);random=s.random;
            Check(CommandRules.StartCampaignApproach(s,0)==""&&s.random==random,"Archived report opens the one-night route without consuming action randomness");
            task=s.command.tasks.First(t=>t.id==s.command.campaign.activeTask);
            Check(task.stepsRequired==1&&s.intelligence==intel&&!s.command.campaign.evidence[0].verified,"Choosing a method does not consume or prematurely verify an archive");
            task.actor=3;Check(CommandRules.CancelCampaignApproach(s).Contains("撤回"),"Cannot silently cancel a signed authorization");task.actor=-1;
            task.lastResult="本章已有一次尝试";Check(CommandRules.CancelCampaignApproach(s).Contains("已开始"),"An attempted chapter keeps its committed method and progress");
            s=Fresh();s.turn=s.config.Value("turns");
            Check(CommandRules.CampaignApproachError(s,1).Contains("至少需要2夜"),"Two-stage alternative explains an insufficient time window");
        }
        static void VerifyProvenance()
        {
            var s=Fresh();var route=s.command.routes[0];route.source=0;s.command.people[0].institution=1;
            route.waiting++;CommandRules.CampaignOnSourceProduced(s,0,0);string original=s.agents[0].name;
            s.command.people[3].institution=2;route.source=3;
            route.waiting++;CommandRules.CampaignOnSourceProduced(s,3,0);
            route.waiting--;route.atRelay++;CommandRules.CampaignOnRelay(s,0);
            route.atRelay--;CommandRules.CampaignOnDelivery(s,3,0);
            Check(s.command.campaign.evidence.Count==1&&s.command.campaign.evidence[0].institution==1&&s.command.campaign.evidence[0].sourceName==original,"Normal source replacement cannot relabel an older report's institution or author");
            route.waiting--;route.atRelay++;CommandRules.CampaignOnRelay(s,0);
            route.atRelay--;CommandRules.CampaignOnDelivery(s,3,0);
            Check(s.command.campaign.evidence.Last().institution==2,"The new source's own report remains second in FIFO order");
            route.atRelay=1;CommandRules.ReconcileCampaignPackets(s);route.atRelay--;CommandRules.CampaignOnDelivery(s,3,0);
            Check(s.command.campaign.evidence.Count==2,"Unknown synthetic inventory grants no institutional campaign permission");
            route.waiting++;CommandRules.CampaignOnSourceProduced(s,3,0);route.waiting=0;CommandRules.ReconcileCampaignPackets(s);
            Check(s.command.campaign.packets.Count==0,"Expired or compromised inventory loses its pending provenance too");
            s.command.people[3].institution=0;route.waiting++;CommandRules.CampaignOnSourceProduced(s,3,0);route.waiting--;route.atRelay++;CommandRules.CampaignOnRelay(s,0);route.atRelay--;CommandRules.CampaignOnDelivery(s,3,0);
            Check(s.command.campaign.evidence.Count==2,"Civilian work cannot manufacture an institution report");
            CommandRules.Validate(s);
            Check(CommandRules.CampaignEvidenceSummary(s).Contains("第1夜")&&CommandRules.CampaignEvidenceSummary(s).Contains(original)&&CommandRules.CampaignEvidenceSummary(s).Contains("未核证"),"Archive explains arrival date, original author and evidence confidence");
            // Exercise actual source/relay/courier resolution rather than only
            // synthetic hook fixtures, including a save while a packet travels.
            s=Fresh();route=s.command.routes[0];route.source=0;route.liaison=1;route.courier=2;route.enabled=true;s.command.tasks.Clear();
            foreach(var member in s.agents)member.intel=member.persuade=member.stealth=40;
            CommandRules.Resolve(s);Check(route.waiting==1&&s.command.campaign.packets.Count==1&&s.command.campaign.evidence.Count==0,"Real source action creates an in-transit report, not an archive");
            s=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));CommandRules.Validate(s);
            CommandRules.Resolve(s);CommandRules.Resolve(s);
            Check(s.command.delivered==1&&s.command.campaign.evidence.Count==1,"Real three-night transmission archives exactly one delivered report after a save/load");
        }
        static void VerifyAllAuthoredPaths()
        {
            var config=JsonUtility.FromJson<CommandCampaignConfig>(Resources.Load<TextAsset>("Minglu/Config/command_campaign").text);CommandRules.ValidateCommandCampaignConfig(config);
            int methods=0;
            for(uint scenario=0;scenario<3;scenario++)for(int approach=0;approach<2;approach++)
            {
                var s=Fresh(scenario+3);s.command.tasks.Clear();
                foreach(var member in s.agents)member.intel=member.persuade=member.stealth=member.organize=40;
                foreach(var person in s.command.people){person.level=3;person.institution=0;}
                s.command.completed=2;s.command.delivered=20;s.progress=100;CommandRules.CheckGate(s);
                Check(!s.ended,"Meeting old quotas alone does not skip new narrative campaign "+scenario);
                for(int chapter=0;chapter<3;chapter++)
                {
                    var current=CommandRules.CampaignCurrentChapter(s);
                    if(approach==0)SupplyEvidence(s,current.approaches[0].evidenceInstitution);
                    Check(CommandRules.StartCampaignApproach(s,approach)=="","Start authored path "+scenario+"/"+chapter+"/"+approach);methods++;
                    var task=s.command.tasks.First(t=>t.id==s.command.campaign.activeTask);
                    for(int stage=0;stage<task.stepsRequired;stage++)
                    {
                        int actor=3+(s.turn%3);Check(CommandRules.Assign(s,actor,task.id)=="","Real actor meets chapter requirements");
                        CommandRules.Resolve(s);CommandRules.Validate(s);
                        if(stage+1<task.stepsRequired)Check(s.command.campaign.chapter==chapter&&task.stepsDone==stage+1&&task.actor<0,"Intermediate success preserves progress and requires a fresh managerial authorization");
                    }
                    Check(s.command.campaign.chapter==chapter+1&&s.command.campaign.history.Count==chapter+1,"Only actual final success advances this chapter");
                    if(approach==0)Check(s.command.campaign.evidence.Any(e=>e.verified&&e.verifiedChapter==chapter),"Evidence becomes verified only after the chapter action succeeds");
                    var same=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));CommandRules.Validate(same);
                    Check(JsonUtility.ToJson(same.command)==JsonUtility.ToJson(s.command)&&same.random==s.random,"Chapter progress, archive and committed methods round-trip exactly");
                }
                Check(s.ended&&s.won&&CommandRules.CampaignReadyForVictory(s),"All three chapters plus original organization goals reach a real victory");
                Check(s.ending.Contains(s.command.campaign.scenarioData.title)&&s.ending.Contains("本局选择"),"Ending attributes outcomes to the chosen campaign and methods");
            }
            Check(methods==18,"All eighteen authored task methods have an executable completion path");
        }
        [MenuItem("Minglu/验证首脑主线委任")]
        public static void Verify()
        {
            checks=0;VerifyOldContractAndSnapshot();VerifyChoicesAndCapacity();VerifyProvenance();VerifyAllAuthoredPaths();
            Debug.Log("MINGLU_COMMAND_CAMPAIGN_VERIFY_SUCCESS: "+checks+" checks / 3 scenarios / 18 authored methods / legacy save contract and institutional provenance preserved. Strong-actor fixtures validate reachability, not player win rate.");
        }
    }
}
