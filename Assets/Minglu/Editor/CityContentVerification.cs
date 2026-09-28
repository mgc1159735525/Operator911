using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class CityContentVerification
    {
        static int passed;
        static void Check(bool ok,string name){if(!ok)throw new Exception("City content: "+name);passed++;}
        static RunState Fresh(uint seed=911){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),seed,1,0,false);CampaignRules.Initialize(s,c);CityContentRules.Initialize(s);return s;}
        [MenuItem("Minglu/验证城市地点与案件")]
        public static void Verify()
        {
            passed=0;var s=Fresh();Check(s.city.sites.Count==12&&s.config.missions.Count==34,"Content merged");
            Check(!DispatchRules.Open(s).Any(x=>x.id=="tea_protect"),"Cases require evidence");
            Check(CityContentRules.AssignSite(s,0,"warehouse")!="","Hidden site requires clue");
            Check(CityContentRules.AssignSite(s,2,"tea")=="","Visit assigned");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-city-v07-test.json");PrototypeSave.Write(s,path);s=PrototypeSave.Read(path);
            Check(s.agents[2].site=="tea","Visit order restored");
            s.config.Action(Duty.Contact).required=0;PrototypeRules.Resolve(s);PrototypeRules.ChooseEvent(s,false);
            Check(s.cityEvidence.Contains("证人口供")&&s.visitedSites.Contains("tea"),"Successful visit discovers permanent clue");
            Check(s.agents.All(a=>string.IsNullOrEmpty(a.site)),"Orders clear at end of night");
            Check(DispatchRules.Open(s).Count(x=>x.id.StartsWith("tea_"))==2,"Both approaches unlocked");
            Check(CityContentRules.AssignSite(s,0,"warehouse")=="","Clue unlocks hidden site");PrototypeRules.Assign(s,0,Duty.Rest);
            Check(DispatchRules.Assign(s,2,"tea_protect")=="","Approach assigned");
            int ap=PrototypeRules.UsedAP(s);Check(DispatchRules.Assign(s,1,"tea_bait")!=""&&PrototypeRules.UsedAP(s)==ap&&s.agents[1].order==Duty.Rest,"Competing approach cannot be double-booked");
            Check(DispatchRules.Assign(s,2,"tea_bait")=="","Same agent can replace approach");
            Check(DispatchRules.Assign(s,2,"tea_protect")=="","Change decision before resolution");
            PrototypeRules.Resolve(s);Check(s.completedMissions.Contains("tea_protect")&&!DispatchRules.Open(s).Any(x=>x.id=="tea_bait"),"Success permanently closes rival approach");
            Check(s.quarters.First(x=>x.district==0).support>=26&&s.cityEvidence.Contains("证人获救"),"Choice affects district and story flags");
            s=Fresh();s.cityEvidence.Add("证人口供");s.turn=4;int before=s.pressure;DispatchRules.EndNight(s);
            int expected=s.config.missions.Where(x=>x.deadline==4&&!x.id.StartsWith("tea_")).Sum(x=>x.failurePressure)+2;
            Check(s.pressure-before==expected,"Mutually exclusive case penalized only once");
            s=Fresh();s.turn=4;before=s.pressure;DispatchRules.EndNight(s);Check(s.pressure-before==expected-2,"Undiscovered cases do not punish player");
            var q=s.quarters[0];q.heat=75;q.support=10;Check(CityContentRules.Exposure(s,q.district)==9,"Heat changes actual exposure");q.support=40;Check(CityContentRules.Exposure(s,q.district)==7,"Support offsets exposure");CityContentRules.EndNight(s);Check(q.heat==73,"Heat naturally decays");
            s=Fresh();CityContentRules.AssignSite(s,2,"tea");PrototypeRules.Assign(s,2,Duty.Rest);Check(s.agents[2].site=="","Cancel clears site");
            CityContentRules.AssignSite(s,2,"tea");OrganizationRules.Assign(s,2,OrganizationDuty.Recruit,0,0);Check(s.agents[2].site=="","Organization reassignment clears visit");
            s=Fresh();s.config.Action(Duty.Contact).required=8;CityContentRules.AssignSite(s,2,"tea");PrototypeRules.Resolve(s);Check(!s.cityEvidence.Contains("证人口供"),"Failed visit does not grant evidence");
            s=Fresh();s.cityEvidence.Add("证人口供");s.quarters[0].heat=67;s.quarters[0].support=44;PrototypeSave.Write(s,path);var copy=PrototypeSave.Read(path);Check(copy.cityEvidence.Contains("证人口供")&&copy.quarters[0].heat==67&&copy.quarters[0].support==44,"City state snapshot roundtrip");
            int wins=0,visits=0;
            for(uint seed=1;seed<=100;seed++)
            {
                s=Fresh(seed);
                while(!s.ended)
                {
                    if(!string.IsNullOrEmpty(s.pendingEvent))PrototypeRules.ChooseEvent(s,false);
                    foreach(var m in DispatchRules.Open(s).OrderByDescending(x=>new[]{"shelter","network","garrison","uprising"}.Contains(x.id)).ThenBy(x=>x.deadline).ToList())
                    {
                        var a=DispatchRules.Recommend(s,m).FirstOrDefault(x=>x.stamina>=60&&x.exposure<35&&x.spirit>=35);if(a!=null)DispatchRules.Assign(s,a.id,m.id);
                    }
                    foreach(var a in s.agents.Where(x=>x.condition==Condition.Active&&x.order==Duty.Rest&&x.organizationDuty==OrganizationDuty.None&&x.stamina>=60&&x.exposure<35))
                    {
                        var site=s.city.sites.FirstOrDefault(x=>CityContentRules.Unlocked(s,x)&&!s.visitedSites.Contains(x.id)&&!s.agents.Any(b=>b.site==x.id));
                        if(site!=null)CityContentRules.AssignSite(s,a.id,site.id);else PrototypeRules.Assign(s,a.id,s.pressure>35?Duty.Contact:Duty.Propaganda);
                    }
                    PrototypeRules.Resolve(s);CityContentRules.ValidateSave(s);CampaignRules.ValidateSave(s);
                    if(s.turn%5==0||s.ended){PrototypeSave.Write(s,path);s=PrototypeSave.Read(path);}
                    if(s.turn>20)throw new Exception("City campaign timeout");
                }
                if(s.won)wins++;visits+=s.visitedSites.Count;
                foreach(var group in s.city.cases.Where(x=>!string.IsNullOrEmpty(x.group)).GroupBy(x=>x.group))if(group.Count(x=>s.completedMissions.Contains(x.mission.task.id))>1)throw new Exception("Both mutually exclusive outcomes completed");
            }
            Check(wins>0&&visits>0,"Integrated city policy wins and explores");
            foreach(var file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_CITY_CONTENT_SUCCESS: "+passed+" checks; 100 campaigns; "+wins+" policy wins; "+visits+" visited sites. Not player balance evidence.");
        }
        public static void BatchDeliver(){try{CityVerification.Verify();DispatchVerification.Verify();LifecycleVerification.Verify();OrganizationVerification.Verify();CampaignVerification.Verify();Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
