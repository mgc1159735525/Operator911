using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class RecruitmentVerification
    {
        static int checks;
        static void Check(bool ok,string reason){if(!ok)throw new Exception("Recruitment: "+reason);checks++;}
        static RunState Fresh()
        {
            var config=CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load());
            var s=RunLifecycle.Create(config,911,1,0,false);CommandRules.Initialize(s);
            s.command.config.eventChance=0;return s;
        }
        static void FillRoster(RunState s)
        {s.money=100;while(CommandRules.OccupiedRosterCount(s)<s.command.config.capacity)Check(CommandRules.Recruit(s)=="","Fill roster");}
        public static void Verify()
        {
            checks=0;var s=Fresh();
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
            VerifyReplacementAndHistory();VerifyRescueCapacity();VerifyRecoveryWindow();
            Debug.Log("MINGLU_RECRUITMENT_VERIFY_SUCCESS: "+checks+" checks; deterministic preview, costs, active capacity, repeated replacement, rescue and historical save preservation");
        }
        static void VerifyReplacementAndHistory()
        {
            var s=Fresh();FillRoster(s);s.turn=4;
            var departed=s.agents[0];int oldId=departed.id;string oldName=departed.name;
            CommandRules.CaptureForCommand(s,oldId);CommandRules.Person(s,oldId).loyalty=100;
            string before=JsonUtility.ToJson(s);
            Check(CommandRules.OccupiedRosterCount(s)==8&&CommandRules.RecruitError(s).Contains("待救")&&CommandRules.PreviewRecruit(s)==null,"Prisoners reserve a roster place");
            Check(CommandRules.Recruit(s)!=""&&before==JsonUtility.ToJson(s),"Recruiting at occupied capacity does not mutate state");
            for(int n=0;n<5&&!s.ended;n++)CommandRules.Resolve(s);
            Check(departed.condition==Condition.Lost&&CommandRules.OccupiedRosterCount(s)==7,"Actual rescue expiry releases one roster place");
            int historicalCount=s.agents.Count,money=s.money;before=JsonUtility.ToJson(s);
            var expected=CommandRules.PreviewRecruit(s);
            Check(expected!=null&&before==JsonUtility.ToJson(s),"Replacement preview is available and read-only after permanent departure");
            Check(CommandRules.Recruit(s)==""&&s.money==money-12&&s.agents.Count==historicalCount+1&&CommandRules.OccupiedRosterCount(s)==8,"A replacement costs once and restores occupied capacity");
            Check(JsonUtility.ToJson(expected.agent)==JsonUtility.ToJson(s.agents.Last()),"Replacement matches its preview");
            Check(CommandRules.Agent(s,oldId)==departed&&departed.name==oldName&&s.command.tasks.Any(t=>t.operation==2&&t.target==oldId),"Departed member and linked rescue history retain their original ID");
            for(int n=0;n<3;n++)
            {
                var previous=s.agents.Last();previous.condition=Condition.Lost;
                int nextId=s.agents.Max(a=>a.id)+1;historicalCount=s.agents.Count;s.money=100;
                Check(CommandRules.Recruit(s)==""&&CommandRules.OccupiedRosterCount(s)==8&&s.agents.Count==historicalCount+1&&s.agents.Last().id==nextId,"Repeated losses replace occupancy without recycling historical IDs");
                Check(CommandRules.Agent(s,previous.id)==previous&&s.agents.Select(a=>a.id).Distinct().Count()==s.agents.Count,"Replacement preserves every historical record");
            }
            CommandRules.Validate(s);
            bool rejected=false;
            try{ConfigStore.Validate(s.config);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"Non-command configurations still enforce their original eight-member limit");
            var invalid=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));
            invalid.agents.First(a=>a.condition==Condition.Lost).condition=Condition.Active;rejected=false;
            try{CommandRules.Validate(invalid);}catch(Exception){rejected=true;}
            Check(rejected,"Historical roster support cannot admit nine occupied places");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-recruitment-history-check.json");
            try
            {
                PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);
                Check(loaded.agents.Count==s.agents.Count&&CommandRules.OccupiedRosterCount(loaded)==8&&JsonUtility.ToJson(loaded.command)==JsonUtility.ToJson(s.command),"Expanded historical roster and links survive save/load");
            }
            finally{foreach(var file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);}
        }
        static void VerifyRescueCapacity()
        {
            var s=Fresh();FillRoster(s);s.turn=4;CommandRules.CaptureForCommand(s,0);
            var rescue=s.command.tasks.Last();s.agents[3].stealth=6;
            for(int n=0;n<2;n++)
            {
                s.random=1;
                Check(CommandRules.Assign(s,3,rescue.id)=="","Rescue remains assignable at occupied capacity");CommandRules.Resolve(s);
            }
            Check(rescue.status==1&&s.agents[0].condition==Condition.Active&&CommandRules.OccupiedRosterCount(s)==8,"Rescued prisoner returns without exceeding roster capacity");
            Check(CommandRules.RecruitError(s)!=""&&CommandRules.PreviewRecruit(s)==null,"Full rescued roster cannot recruit a ninth serving member");
        }
        static void VerifyRecoveryWindow()
        {
            var s=Fresh();FillRoster(s);
            foreach(var a in s.agents.Skip(2))a.condition=Condition.Lost;
            CommandRules.Resolve(s);
            Check(!s.ended&&CommandRules.OccupiedRosterCount(s)==2,"Two survivors are not defeated merely because historical roster once reached eight");
            Check(CommandRules.Recruit(s)==""&&CommandRules.OccupiedRosterCount(s)==3,"Surviving organization can restore its minimum chain crew");
            foreach(var a in s.agents)a.condition=Condition.Lost;
            CommandRules.Resolve(s);Check(s.ended&&!s.won,"No surviving member still ends the run");
        }
    }
}
