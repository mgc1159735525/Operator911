using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class CourierVerification
    {
        static int passed;
        static void Check(bool ok,string name){if(!ok)throw new Exception("Courier: "+name);passed++;}
        static RunState Fresh(uint seed=911){var c=CampaignRules.Load();var s=RunLifecycle.Create(CampaignRules.BuildConfig(ConfigStore.Load(),c),seed,1,0,false);CampaignRules.Initialize(s,c);CityContentRules.Initialize(s);CourierRules.Initialize(s);return s;}
        static void Night(RunState s){PrototypeRules.Resolve(s);if(!string.IsNullOrEmpty(s.pendingEvent))PrototypeRules.ChooseEvent(s,false);}
        [MenuItem("Minglu/验证情报交通")]
        public static void Verify()
        {
            passed=0;var s=Fresh();Check(s.dispatchPackets.Count==1&&s.courier.sources.Count(x=>x.active)==1,"Initial source and packet");
            Check(CourierRules.Assign(s,0,OrganizationDuty.SendPacket,0)!="","Cannot send sealed packet");
            Check(CourierRules.Assign(s,0,OrganizationDuty.ExpandNetwork,1)!="","Expansion requires deliveries");
            Check(CourierRules.Assign(s,0,OrganizationDuty.ReceivePacket,0)=="","Receive assigned");
            Check(CourierRules.Assign(s,1,OrganizationDuty.ReceivePacket,0)!=""&&PrototypeRules.UsedAP(s)==1,"Duplicate rejected without AP mutation");
            string path=Path.Combine(Application.temporaryCachePath,"minglu-courier-test.json");PrototypeSave.Write(s,path);s=PrototypeSave.Read(path);
            Check(s.agents[0].courierTarget==0&&s.agents[0].organizationDuty==OrganizationDuty.ReceivePacket,"Pending order restored");
            int intel=s.intelligence;Night(s);Check(s.dispatchPackets[0].status==DispatchStatus.Received&&s.intelligence==intel,"Receipt gives no premature intel");
            Check(CourierRules.Assign(s,1,OrganizationDuty.SendPacket,0)=="","Different courier can send");Night(s);
            Check(CourierRules.Delivered(s)==1&&s.intelligence==intel+s.dispatchPackets[0].value,"Send credits once");
            Check(CourierRules.Assign(s,1,OrganizationDuty.SendPacket,0)!="","Cannot repeat reward");
            s=Fresh();CourierRules.Assign(s,0,OrganizationDuty.ReceivePacket,0);PrototypeRules.Assign(s,0,Duty.Rest);
            Check(s.agents[0].courierTarget==-1&&PrototypeRules.UsedAP(s)==0,"Cancel clears reservation");
            s.turn=s.dispatchPackets[0].expires;CourierRules.EndNight(s);Check(s.dispatchPackets[0].status==DispatchStatus.Expired,"Unsent packet expires");
            s=Fresh();s.turn=s.dispatchPackets[0].expires;Check(CourierRules.Assign(s,0,OrganizationDuty.ReceivePacket,0)!="","Useless last-night receipt rejected");Night(s);Check(s.dispatchPackets[0].status==DispatchStatus.Expired,"Last-night receive cannot bypass deadline");
            s=Fresh();CourierRules.FromSpy(s,s.agents[0]);Check(s.dispatchPackets.Count==2&&s.dispatchPackets[1].source==-1,"Temporary spy uses same pipeline");
            var spy=s.agents[0];CourierRules.FromSpy(s,spy);s.dispatchPackets[2].status=DispatchStatus.Received;s.dispatchPackets[2].received=1;
            spy.condition=Condition.Undercover;spy.spyStarted=0;spy.exposure=100;
            OrganizationRules.EndNight(s,new System.Collections.Generic.List<Agent>());
            Check(s.dispatchPackets[1].status==DispatchStatus.Expired&&s.dispatchPackets[2].status==DispatchStatus.Received,"Compromised source loses only uncollected packets");
            int total=0,expanded=0;
            for(uint seed=1;seed<=50;seed++)
            {
                s=Fresh(seed);
                while(!s.ended)
                {
                    foreach(var packet in s.dispatchPackets.Where(x=>x.status==DispatchStatus.Waiting||x.status==DispatchStatus.Received).OrderBy(x=>x.expires).ToList())
                    {
                        var a=s.agents.FirstOrDefault(x=>x.condition==Condition.Active&&x.organizationDuty==OrganizationDuty.None&&x.stamina>=35&&x.spirit>=30&&x.exposure<45);
                        if(a!=null)CourierRules.Assign(s,a.id,packet.status==DispatchStatus.Waiting?OrganizationDuty.ReceivePacket:OrganizationDuty.SendPacket,packet.id);
                    }
                    var source=s.courier.sources.FirstOrDefault(x=>!x.active);var spare=s.agents.FirstOrDefault(x=>x.condition==Condition.Active&&x.organizationDuty==OrganizationDuty.None&&x.stamina>=35&&x.spirit>=30&&x.exposure<45);
                    if(source!=null&&spare!=null)CourierRules.Assign(s,spare.id,OrganizationDuty.ExpandNetwork,source.id);
                    PrototypeSave.Write(s,path);s=PrototypeSave.Read(path);Night(s);CourierRules.ValidateSave(s);
                }
                total+=CourierRules.Delivered(s);expanded+=s.courier.sources.Count(x=>x.active)>1?1:0;
            }
            Check(total>=100&&expanded>0,"Multi-night delivery and expansion reachable");
            Debug.Log("Courier verification passed: "+passed+" checks / 50 runs / "+total+" deliveries / "+expanded+" expanded networks");
        }
        public static void BatchDeliver(){try{Verify();CityVerification.Verify();DispatchVerification.Verify();LifecycleVerification.Verify();OrganizationVerification.Verify();CampaignVerification.Verify();CityContentVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
