using System;
using System.Linq;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class RecruitmentVerification
    {
        static void Check(bool ok,string reason){if(!ok)throw new Exception("Recruitment: "+reason);}
        public static void Verify()
        {
            var config=CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load());
            var s=RunLifecycle.Create(config,911,1,0,false);CommandRules.Initialize(s);
            string before=JsonUtility.ToJson(s);
            var first=CommandRules.PreviewRecruit(s);var again=CommandRules.PreviewRecruit(s);
            Check(before==JsonUtility.ToJson(s),"Preview must not change live state or RNG");
            Check(JsonUtility.ToJson(first.agent)==JsonUtility.ToJson(again.agent),"Cancel/reopen cannot reroll");
            int money=s.money,count=s.agents.Count;
            Check(CommandRules.Recruit(s)=="","Recruit succeeds");
            Check(s.money==money-12&&s.agents.Count==count+1,"Exactly one recruit and payment");
            Check(JsonUtility.ToJson(s.agents.Last())==JsonUtility.ToJson(first.agent),"Recruit matches preview including appearance");
            Check(JsonUtility.ToJson(s.command.people.Last())==JsonUtility.ToJson(first.person),"Recruit identity matches preview");
            s.money=0;before=JsonUtility.ToJson(s);Check(CommandRules.Recruit(s)!=""&&before==JsonUtility.ToJson(s),"Insufficient money is non-mutating");
            s.money=100;s.command.eventKind=1;before=JsonUtility.ToJson(s);Check(CommandRules.Recruit(s)!=""&&before==JsonUtility.ToJson(s),"Events block recruitment");
            s.command.eventKind=0;Check(CommandRules.Recruit(s)==""&&CommandRules.PreviewRecruit(s)==null,"Capacity is enforced");
            Debug.Log("MINGLU_RECRUITMENT_VERIFY_SUCCESS: deterministic preview, costs, capacity and event guards");
        }
    }
}
