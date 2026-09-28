using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class CampaignVerification
    {
        static int passed;
        static void Check(bool ok,string name){if(!ok)throw new Exception("Campaign: "+name);passed++;}
        static RunState Fresh(uint seed=911)
        {
            var content=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),content),seed,1,0,false);CampaignRules.Initialize(s,content);return s;
        }
        [MenuItem("Minglu/验证20夜战役")]
        public static void Verify()
        {
            passed=0;var s=Fresh();
            Check(s.config.Value("turns")==20&&s.config.Value("ap")==4&&s.campaign.missions.Count==18&&s.config.events.Count==9,"Authored content loads");
            Check(ConfigStore.Load().Value("turns")==8,"Base demo remains isolated");
            foreach(int turn in new[]{1,4,5,10,11,16,17,20}){s.turn=turn;CampaignRules.BeginTurn(s);Check(s.config.Value("pressurePerTurn")== (turn<5?1:turn<11?2:turn<17?3:4),"Phase boundary "+turn);}
            s=Fresh();Check(PrototypeRules.Dice(s.agents[2],Duty.Contact)==5&&PrototypeRules.Dice(s.agents[1],Duty.Propaganda)==5,"Identity and four abilities");
            s.progress=100;PrototypeRules.Resolve(s);Check(!s.ended,"Progress cannot bypass uprising");
            s=Fresh();s.turn=17;s.progress=100;PrototypeRules.Resolve(s);Check(!s.ended,"Final phase still requires uprising");
            s=Fresh();s.turn=17;s.progress=100;s.completedMissions.Add("uprising");s.campaignFlags.Add("shelter_people");PrototypeRules.Resolve(s);Check(s.won&&s.ending.StartsWith("明路"),"Protected people ending");
            s=Fresh();s.turn=17;s.progress=100;s.completedMissions.Add("uprising");PrototypeRules.Resolve(s);Check(s.won&&s.ending.StartsWith("解放"),"Liberation ending");
            s=Fresh();s.turn=20;PrototypeRules.Resolve(s);Check(s.ended&&!s.won&&s.ending.StartsWith("长夜"),"Deadline failure");
            s=Fresh();s.turn=20;s.progress=40;PrototypeRules.Resolve(s);Check(s.ending.StartsWith("妥协"),"Compromise ending");
            s=Fresh();s.turn=20;s.agents[0].condition=Condition.Lost;s.agents[1].condition=Condition.Lost;PrototypeRules.Resolve(s);Check(s.ending.StartsWith("悲剧"),"Tragedy ending");
            s=Fresh();CampaignRules.CompleteMission(s,"supply");int pressure=s.pressure;CampaignRules.SettleConsequences(s);Check(s.pressure==pressure&&s.consequences.Count==1,"Delayed effect not immediate");s.turn=3;CampaignRules.SettleConsequences(s);Check(s.pressure==pressure+4&&s.consequences.Count==0,"Delayed effect settles at due night");CampaignRules.SettleConsequences(s);Check(s.pressure==pressure+4,"Delayed effect cannot repeat");
            s=Fresh();var a=s.agents[2];a.order=Duty.Contact;for(int i=0;i<4;i++)CampaignRules.SuccessfulAction(s,a);Check(a.persuade==5&&a.skillExperience[2]==0&&s.pressure==0,"Action growth and contact pressure reduction");
            for(int i=0;i<4;i++)CampaignRules.SuccessfulAction(s,a);Check(a.persuade==5,"Growth capped");
            s=Fresh();s.turn=5;Check(!DispatchRules.Open(s).Any(m=>m.id=="network"),"Missing prerequisite blocks chain");s.completedMissions.Add("shelter");Check(DispatchRules.Open(s).Any(m=>m.id=="network"),"Completed prerequisite unlocks chain");
            s=Fresh();s.turn=13;s.pendingEvent="shelter_choice";s.intelligence=0;Check(PrototypeRules.ChooseEvent(s,true)!=""&&!s.campaignFlags.Contains("shelter_people"),"Unaffordable choice has no flag");s.intelligence=2;PrototypeRules.ChooseEvent(s,true);Check(s.campaignFlags.Contains("shelter_people"),"Choice persists protection flag");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-campaign-test.json");
            s=Fresh();CampaignRules.CompleteMission(s,"supply");s.agents[1].skillExperience[3]=2;PrototypeSave.Write(s,path);var restored=PrototypeSave.Read(path);Check(restored.consequences.Count==1&&restored.agents[1].skillExperience[3]==2&&restored.campaign.missions.Count==18,"Campaign snapshot roundtrip");
            int wins=0;
            for(uint seed=1;seed<=200;seed++)
            {
                s=Fresh(seed);
                while(!s.ended)
                {
                    if(!string.IsNullOrEmpty(s.pendingEvent))PrototypeRules.ChooseEvent(s,s.pendingEvent=="shelter_choice"&&s.intelligence>=2);
                    foreach(var m in DispatchRules.Open(s).OrderByDescending(m=>new[]{"shelter","network","garrison","uprising"}.Contains(m.id)).ThenBy(m=>m.deadline).ToList())
                    {
                        var candidate=DispatchRules.Recommend(s,m).FirstOrDefault(x=>x.stamina>=60&&x.exposure<35&&x.spirit>=35);
                        if(candidate!=null)DispatchRules.Assign(s,candidate.id,m.id);
                    }
                    foreach(var member in s.agents.Where(x=>x.condition==Condition.Active&&x.order==Duty.Rest&&x.stamina>=60&&x.exposure<35&&x.spirit>=35))PrototypeRules.Assign(s,member.id,s.pressure>35?Duty.Contact:Duty.Propaganda);
                    PrototypeRules.Resolve(s);CampaignRules.ValidateSave(s);OrganizationRules.ValidateSave(s);
                    if(s.intelligence<0||s.money<0||s.pressure<0||s.pressure>100)throw new Exception("Campaign resources invariant "+seed);
                    // Every phase is saved and restored, including pending events and expired mission chains.
                    PrototypeSave.Write(s,path);s=PrototypeSave.Read(path);
                }
                if(s.won)wins++;
                if(s.won&&(s.turn<17||!s.completedMissions.Contains("uprising")))throw new Exception("Illegal campaign victory");
            }
            Check(wins>0&&wins<200,"Policy exercises both successful and failed runs");
            foreach(var file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_CAMPAIGN_SUCCESS: "+passed+" checks; 200 saved campaign runs; "+wins+" policy wins (not player balance evidence).");
        }
        public static void BatchDeliver()
        {
            try{CityVerification.Verify();DispatchVerification.Verify();LifecycleVerification.Verify();OrganizationVerification.Verify();Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
