using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class SecurityVerification
    {
        static void Check(bool ok,string name){if(!ok)throw new Exception("Security: "+name);}
        static RunState Fresh(){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),911,1,0,false);CampaignRules.Initialize(s,c);CityContentRules.Initialize(s);CourierRules.Initialize(s);return s;}
        public static void Verify()
        {
            var s=Fresh();s.turn=s.dispatchPackets[0].expires;
            Check(CourierRules.Assign(s,0,OrganizationDuty.ReceivePacket,0)!=""&&PrototypeRules.UsedAP(s)==0,"Reject useless receipt without consuming AP");
            s=Fresh();Check(CourierRules.Assign(s,0,OrganizationDuty.ReceivePacket,0)=="","Receive");CourierRules.Execute(s,s.agents[0]);
            Check(s.security.trace==8&&s.agents[0].knownStation==1,"Traffic creates trace and limited knowledge");
            SecurityRules.Disclose(s,s.agents[1]);Check(s.security.trace==8,"Uninformed prisoner reveals nothing");
            SecurityRules.Disclose(s,s.agents[0]);Check(s.security.trace==43,"Informed prisoner reveals current station");
            Check(CourierRules.Assign(s,1,OrganizationDuty.RelocateStation,1)!="","No move with conflicting traffic");
            PrototypeRules.Assign(s,0,Duty.Rest);Check(CourierRules.Assign(s,1,OrganizationDuty.RelocateStation,1)=="","Move assigned");
            int money=s.money;SecurityRules.Move(s,s.agents[1]);Check(s.money==money-8&&s.security.generation==2&&s.dispatchPackets[0].status==DispatchStatus.Received,"Move preserves packets and consumes cost");
            SecurityRules.Disclose(s,s.agents[0]);Check(s.security.trace==0,"Old address no longer leaks");
            PrototypeRules.Assign(s,1,Duty.Rest);s.security.trace=100;s.agents[0].organizationDuty=OrganizationDuty.SendPacket;SecurityRules.EndNight(s);
            Check(s.security.closed&&s.dispatchPackets[0].status==DispatchStatus.Expired,"Raid closes station and destroys stored packet");
            PrototypeRules.Assign(s,0,Duty.Rest);Check(CourierRules.Assign(s,0,OrganizationDuty.RelocateStation,2)=="","Recovery available");SecurityRules.Move(s,s.agents[0]);PrototypeRules.Assign(s,0,Duty.Rest);Check(!s.security.closed&&s.security.trace==55,"Recovery opens station");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-security-check.json");PrototypeSave.Write(s,path);var loaded=PrototypeSave.Read(path);Check(loaded.security.generation==3&&loaded.security.district==2,"Save restores station");
            loaded.security=null;SecurityRules.Validate(loaded);Check(loaded.security.generation==1,"Legacy default");
            File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");
            Debug.Log("MINGLU_SECURITY_VERIFY_SUCCESS");
        }
        public static void BatchDeliver(){try{Verify();CourierVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
