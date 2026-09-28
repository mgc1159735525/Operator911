using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    [Serializable] public class CitySite { public string id,name,description,duty,evidence,requires;public int district;public float x,y; }
    [Serializable] public class CityCase { public CampaignMission mission;public string site,group,requires,grants,outcome;public int heat,support; }
    [Serializable] public class CityContent { public List<CitySite> sites=new List<CitySite>();public List<CityCase> cases=new List<CityCase>(); }
    [Serializable] public class CityQuarter { public int district,heat,support; }
    public static class CityContentRules
    {
        public static CityContent Load(){return JsonUtility.FromJson<CityContent>(Resources.Load<TextAsset>("Minglu/Config/city_content").text);}
        public static void Initialize(RunState s)
        {
            if(!s.campaignEnabled||s.cityEnabled)throw new InvalidOperationException("城市内容只能初始化一次，且必须是战役。");
            var c=Load();Validate(c,s.config);s.cityEnabled=true;s.city=c;
            foreach(var d in s.config.districts)s.quarters.Add(new CityQuarter{district=d.id,heat=d.risk,support=10});
            foreach(var item in c.cases){s.campaign.missions.Add(item.mission);s.config.missions.Add(item.mission.task);}
            ConfigStore.Validate(s.config);CampaignRules.Validate(s.campaign);
            Log(s,"城市线报开放：巡访地点可找到永久线索。同一案件只能完成一种处理方式；区域热度和支持会改变外出风险。");
        }
        public static CityCase Case(RunState s,string id){return s.cityEnabled?s.city.cases.Find(c=>c.mission.task.id==id):null;}
        public static bool Available(RunState s,string id)
        {
            var c=Case(s,id);if(c==null)return true;
            return (string.IsNullOrEmpty(c.requires)||s.cityEvidence.Contains(c.requires))&&
                (string.IsNullOrEmpty(c.group)||!s.city.cases.Any(x=>x.group==c.group&&x.mission.task.id!=id&&s.completedMissions.Contains(x.mission.task.id)));
        }
        public static bool Unlocked(RunState s,CitySite site){return string.IsNullOrEmpty(site.requires)||s.cityEvidence.Contains(site.requires);}
        public static string AssignmentError(RunState s,int agent,string id)
        {
            var c=Case(s,id);if(c==null||string.IsNullOrEmpty(c.group))return "";
            return s.agents.Any(a=>a.id!=agent&&!string.IsNullOrEmpty(a.mission)&&Case(s,a.mission)!=null&&Case(s,a.mission).group==c.group)?"同一案件已有处理方案；请先撤回原指令。":"";
        }
        public static string AssignSite(RunState s,int id,string siteId)
        {
            var site=s.cityEnabled?s.city.sites.Find(x=>x.id==siteId):null;
            if(site==null||!Unlocked(s,site))return "地点尚未解锁，需要相关线索。";
            var error=PrototypeRules.Assign(s,id,(Duty)Enum.Parse(typeof(Duty),site.duty),-1,site.district);
            if(error=="")s.agents.First(a=>a.id==id).site=siteId;
            return error;
        }
        public static int Exposure(RunState s,int district)
        {
            if(!s.cityEnabled)return 0;var q=s.quarters.First(x=>x.district==district);
            return Math.Max(0,q.heat/25*3-(q.support>=40?2:0));
        }
        public static void ActionResult(RunState s,Agent a,bool success)
        {
            if(!s.cityEnabled)return;
            var q=s.quarters.First(x=>x.district==a.district);
            q.heat=Math.Max(0,Math.Min(100,q.heat+(a.order==Duty.Sabotage?12:a.order==Duty.Contact?-6:4)+(success?0:4)));
            if(success&&(a.order==Duty.Contact||a.order==Duty.Propaganda))q.support=Math.Min(100,q.support+4);
            if(success&&!string.IsNullOrEmpty(a.site))
            {
                var site=s.city.sites.First(x=>x.id==a.site);
                if(!s.visitedSites.Contains(site.id)){s.visitedSites.Add(site.id);Log(s,a.name+"完成首次巡访："+site.name+"。");}
                Evidence(s,site.evidence);
            }
        }
        public static void Complete(RunState s,string id)
        {
            var c=Case(s,id);if(c==null)return;
            Evidence(s,c.grants);var q=s.quarters.First(x=>x.district==c.mission.task.district);
            q.heat=Math.Max(0,Math.Min(100,q.heat+c.heat));q.support=Math.Max(0,Math.Min(100,q.support+c.support));
            Log(s,"案件余波："+c.outcome+" 区域热度"+c.heat.ToString("+0;-0;0")+"，支持"+c.support.ToString("+0;-0;0")+"。"+(string.IsNullOrEmpty(c.group)?"":"其他处理方式已关闭。"));
        }
        public static void EndNight(RunState s){if(s.cityEnabled)foreach(var q in s.quarters)q.heat=Math.Max(0,q.heat-2);}
        static void Evidence(RunState s,string clue){if(!string.IsNullOrEmpty(clue)&&!s.cityEvidence.Contains(clue)){s.cityEvidence.Add(clue);Log(s,"线索入档：「"+clue+"」。可解锁对应地点和案件，线索不消耗。 ");}}
        public static string Summary(CityCase c){return "热度"+c.heat.ToString("+0;-0;0")+" / 支持"+c.support.ToString("+0;-0;0")+(string.IsNullOrEmpty(c.group)?"":" / 互斥方案")+(string.IsNullOrEmpty(c.requires)?"":" / 需「"+c.requires+"」");}
        public static void Validate(CityContent c,GameConfig config)
        {
            if(c==null||c.sites==null||c.cases==null||c.sites.Count!=12||c.sites.Any(x=>x==null||string.IsNullOrWhiteSpace(x.id)||string.IsNullOrWhiteSpace(x.name)||!config.districts.Any(d=>d.id==x.district)||!new[]{"Recon","Contact","Propaganda"}.Contains(x.duty)||!config.actions.Any(a=>a.id==x.duty)||float.IsNaN(x.x)||float.IsNaN(x.y)||x.x<.08f||x.x>.92f||x.y<.1f||x.y>.88f)||c.sites.Select(x=>x.id).Distinct().Count()!=12)throw new InvalidOperationException("城市地点配置无效。");
            if(c.cases.Any(x=>x==null||x.mission==null||x.mission.task==null||!c.sites.Any(p=>p.id==x.site&&p.district==x.mission.task.district)||Math.Abs(x.heat)>30||Math.Abs(x.support)>30||string.IsNullOrWhiteSpace(x.outcome))||c.cases.Select(x=>x.mission.task.id).Distinct().Count()!=c.cases.Count)throw new InvalidOperationException("城市案件配置无效。");
            var clues=c.sites.Select(x=>x.evidence).Concat(c.cases.Select(x=>x.grants)).Where(x=>!string.IsNullOrEmpty(x)).ToList();
            if(c.sites.Select(x=>x.requires).Concat(c.cases.Select(x=>x.requires)).Any(x=>!string.IsNullOrEmpty(x)&&!clues.Contains(x)))throw new InvalidOperationException("城市线索引用不存在。");
        }
        public static void ValidateSave(RunState s)
        {
            if(!s.cityEnabled)return;Validate(s.city,s.config);
            if(!s.campaignEnabled||s.quarters==null||s.quarters.Count!=s.config.districts.Count||s.quarters.Select(q=>q.district).Distinct().Count()!=s.quarters.Count||s.quarters.Any(q=>!s.config.districts.Any(d=>d.id==q.district)||q.heat<0||q.heat>100||q.support<0||q.support>100)||s.cityEvidence==null||s.visitedSites==null||s.visitedSites.Any(id=>!s.city.sites.Any(x=>x.id==id))||s.cityEvidence.Distinct().Count()!=s.cityEvidence.Count||s.agents.Any(a=>!string.IsNullOrEmpty(a.site)&&(a.condition!=Condition.Active||a.organizationDuty!=OrganizationDuty.None||!s.city.sites.Any(p=>p.id==a.site&&p.district==a.district&&p.duty==a.order.ToString()))))throw new InvalidOperationException("城市存档状态无效。");
            if(s.agents.Any(a=>!string.IsNullOrEmpty(a.mission)&&AssignmentError(s,a.id,a.mission)!=""))throw new InvalidOperationException("案件互斥指令冲突。");
        }
        static void Log(RunState s,string line){s.journal.Add("["+s.turn+"] "+line);}
    }
}
