using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class OrganizationVerification
    {
        static int passed;
        static void Check(bool condition,string title){if(!condition)throw new Exception("ORG_FAIL: "+title);passed++;Debug.Log("ORG_PASS: "+title);}
        static RunState Fresh(uint seed=911){var s=RunLifecycle.Create(ConfigStore.Load(),seed,1,0,false);s.storyEnabled=false;return s;}
        static bool Reject(Action action){try{action();return false;}catch{return true;}}
        public static void Verify()
        {
            passed=0;var s=Fresh();
            Check(s.money==36&&s.candidates.Count==3&&s.agents.All(a=>a.spirit==100&&a.suspicion==0&&a.maintainCover),"Organization initialization");
            Check(JsonUtility.ToJson(s.candidates)==JsonUtility.ToJson(Fresh().candidates),"Recruit candidates seeded deterministically");
            Check(OrganizationRules.Assign(s,0,OrganizationDuty.Recruit,0,0)==""&&PrototypeRules.UsedAP(s)==1&&OrganizationRules.ReservedMoney(s)==12,"Recruit reserves AP and money");
            string before=JsonUtility.ToJson(s);
            Check(OrganizationRules.Assign(s,1,OrganizationDuty.Recruit,0,0)!=""&&before==JsonUtility.ToJson(s),"Duplicate recruit rejected atomically");
            OrganizationRules.Assign(s,1,OrganizationDuty.Recruit,0,1);
            Check(OrganizationRules.Assign(s,2,OrganizationDuty.Recruit,0,2)!="","Pending recruitment respects capacity");
            PrototypeRules.Assign(s,0,Duty.Rest);
            Check(OrganizationRules.ReservedMoney(s)==12,"Normal cancellation releases recruitment budget");
            PrototypeRules.Resolve(s);
            Check(s.agents.Count==5&&s.config.agents.Count==5&&s.agents.Last().handler==1&&s.candidates[1].hired,"Recruited actor and portrait data added with handler");
            Check(s.money==22&&s.agents.Last().spirit==100&&s.agents.Last().suspicion==0,"New recruit skips arrival-night upkeep and actions");
            Check(OrganizationRules.Assign(s,0,OrganizationDuty.Recruit,0,1)!="","Hired candidate cannot be recruited twice");
            s=Fresh();s.money=0;before=JsonUtility.ToJson(s);
            Check(OrganizationRules.Assign(s,0,OrganizationDuty.Recruit,0,0)!=""&&before==JsonUtility.ToJson(s),"Insufficient funds rollback");
            PrototypeRules.Resolve(s);
            Check(s.agents.All(a=>a.suspicion==12)&&s.money==6,"Unaffordable maintenance increases suspicion and income settles last");
            s=Fresh();OrganizationRules.Maintain(s,0);PrototypeRules.Resolve(s);
            Check(s.agents[0].suspicion==12&&s.agents[1].suspicion==0,"Per-agent cover choice affects suspicion");
            s=Fresh();s.agents[0].suspicion=95;s.agents[0].maintainCover=false;PrototypeRules.Resolve(s);
            Check(s.agents[0].condition==Condition.Captured,"Collapsed cover leads to capture");
            s=Fresh();s.agents[0].spirit=10;
            Check(PrototypeRules.Assign(s,0,Duty.Recon)!=""&&OrganizationRules.Assign(s,0,OrganizationDuty.Recruit,0,0)!="","Very low spirit prevents field assignments");
            Check(PrototypeRules.Assign(s,0,Duty.Rest)=="","Low spirit always permits rest");PrototypeRules.Resolve(s);
            Check(s.agents[0].spirit==28&&PrototypeRules.Dice(s.agents[0],Duty.Recon)==2,"Rest restores spirit and remaining penalty affects dice");
            s=Fresh();s.agents[0].spirit=90;PrototypeRules.Assign(s,0,Duty.Recon);PrototypeRules.Resolve(s);
            Check(s.agents[0].spirit==82,"Ordinary field action consumes spirit");
            s=Fresh();Check(OrganizationRules.Assign(s,0,OrganizationDuty.Infiltrate,3)=="","Spy assignment accepted");PrototypeRules.Resolve(s);
            Check(s.agents[0].condition==Condition.Undercover&&s.agents[0].spyNights==0&&s.packets.Count==0,"Entering infiltration does not also produce intel");
            Check(PrototypeRules.Assign(s,0,Duty.Recon)!="","Undercover member cannot double-act");
            PrototypeRules.Resolve(s);Check(s.packets.Count==0,"No early spy output");PrototypeRules.Resolve(s);
            Check(s.packets.Count==1&&s.packets[0].kind=="军事"&&s.intelligence==1,"Spy output is cached rather than spendable");
            Check(OrganizationRules.Assign(s,1,OrganizationDuty.Deliver,0)!="","Courier must visit correct district");
            Check(OrganizationRules.Assign(s,1,OrganizationDuty.Deliver,3)=="","Courier assigned to cached intel");
            Check(OrganizationRules.Assign(s,2,OrganizationDuty.Deliver,3)!="","Duplicate couriers rejected");
            PrototypeRules.Resolve(s);
            Check(s.packets.Count==0&&s.intelligence==4&&s.agents[0].condition==Condition.Active,"Delivery converts intel and completed spy returns");
            s=Fresh();OrganizationRules.Assign(s,0,OrganizationDuty.Infiltrate,3);PrototypeRules.Resolve(s);
            Check(OrganizationRules.Recall(s,0)==""&&PrototypeRules.UsedAP(s)==1,"Recall reserves one AP");
            PrototypeRules.Resolve(s);Check(s.agents[0].condition==Condition.Active&&s.packets.Count==0,"Early extraction produces no free intelligence");
            s=Fresh();OrganizationRules.Assign(s,0,OrganizationDuty.Infiltrate,3);PrototypeRules.Resolve(s);PrototypeRules.Resolve(s);PrototypeRules.Resolve(s);
            s.agents[0].exposure=69;PrototypeRules.Resolve(s);
            Check(s.agents[0].condition==Condition.Captured&&s.packets.Count==0&&s.agents[0].interrogation==0,"Spy compromise destroys undelivered packets without immediate interrogation");
            s=Fresh();s.packets.Add(new IntelPacket{id=s.nextPacketId++,source=0,district=0,expires=1,value=3,kind="治安"});
            PrototypeRules.Resolve(s);Check(s.packets.Count==0,"Uncollected packet expires at end of deadline night");
            s=Fresh();s.packets.Add(new IntelPacket{id=s.nextPacketId++,source=0,district=0,expires=1,value=3,kind="治安"});OrganizationRules.Assign(s,1,OrganizationDuty.Deliver);PrototypeRules.Resolve(s);
            Check(s.intelligence==4,"Deadline-night delivery precedes expiry");
            s=Fresh();s.turn=7;Check(OrganizationRules.Assign(s,0,OrganizationDuty.Infiltrate)!="","Late infiltration cannot waste entire remaining run");
            s=Fresh();s.pendingEvent="curfew";before=JsonUtility.ToJson(s);
            Check(OrganizationRules.Maintain(s,0)!=""&&OrganizationRules.Assign(s,0,OrganizationDuty.Recruit,0,0)!=""&&before==JsonUtility.ToJson(s),"Story choice blocks organization mutations");
            string file=Path.Combine(Path.GetTempPath(),"minglu-org-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                s=Fresh();OrganizationRules.Assign(s,0,OrganizationDuty.Recruit,0,0);PrototypeRules.Resolve(s);OrganizationRules.Assign(s,1,OrganizationDuty.Infiltrate,3);PrototypeRules.Resolve(s);PrototypeRules.Resolve(s);PrototypeRules.Resolve(s);
                PrototypeSave.Write(s,file);var loaded=PrototypeSave.Read(file);
                Check(JsonUtility.ToJson(s)==JsonUtility.ToJson(loaded),"Expanded roster spirit spy and packet snapshot roundtrip");
                PrototypeRules.Resolve(s);PrototypeRules.Resolve(loaded);Check(JsonUtility.ToJson(s)==JsonUtility.ToJson(loaded),"Expanded save exact continuation");
                loaded.money=-1;PrototypeSave.Write(loaded,file);Check(Reject(()=>PrototypeSave.Read(file)),"Invalid organization save rejected");
            }
            finally{foreach(var p in new[]{file,file+".bak",file+".tmp"})if(File.Exists(p))File.Delete(p);}
            int wins=0;
            for(uint seed=1;seed<=200;seed++)
            {
                s=Fresh(seed);s.storyEnabled=true;
                for(int turn=0;turn<8&&!s.ended;turn++)
                {
                    if(!string.IsNullOrEmpty(s.pendingEvent))PrototypeRules.ChooseEvent(s,false);
                    if(s.ended)break;
                    foreach(var a in s.agents.Where(a=>a.condition==Condition.Active).ToList())
                    {
                        if(a.stamina<50||a.exposure>=40||a.spirit<40)continue;
                        var packet=s.packets.FirstOrDefault();
                        if(packet!=null&&OrganizationRules.Assign(s,a.id,OrganizationDuty.Deliver,packet.district)=="")continue;
                        if(s.turn==1&&a.id==0&&OrganizationRules.Assign(s,a.id,OrganizationDuty.Recruit,0,0)=="")continue;
                        if(s.turn==1&&a.id==1&&OrganizationRules.Assign(s,a.id,OrganizationDuty.Infiltrate,3)=="")continue;
                        PrototypeRules.Assign(s,a.id,s.intelligence>PrototypeRules.ReservedIntel(s)?Duty.Sabotage:Duty.Recon);
                    }
                    PrototypeRules.Resolve(s);OrganizationRules.ValidateSave(s);ConfigStore.Validate(s.config);
                    if(s.money<0||s.intelligence<0||s.agents.Count>8)throw new Exception("Organization simulation invariant "+seed);
                }
                if(!s.ended)throw new Exception("Organization simulation failed to end");if(s.won)wins++;
            }
            Check(wins>0,"Integrated policy can still win");
            Debug.Log("MINGLU_ORGANIZATION_SUCCESS: "+passed+" checks; 200 runs; "+wins+" policy wins (not player balance evidence).");
        }
        public static void BatchDeliver()
        {
            try{CityVerification.Verify();DispatchVerification.Verify();LifecycleVerification.Verify();Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
