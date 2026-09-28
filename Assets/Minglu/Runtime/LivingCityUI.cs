using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        string selectedSite="";int cityLayer;
        CitySite SelectedVisit(){return State.cityEnabled&&string.IsNullOrEmpty(selectedMission)?State.city.sites.Find(x=>x.id==selectedSite&&x.district==selectedDistrict&&x.duty==selectedDuty.ToString()):null;}
        void SelectSite(CitySite site){selectedSite=site.id;selectedDistrict=site.district;selectedMission="";selectedDuty=(Duty)Enum.Parse(typeof(Duty),site.duty);Draw();}
        void CityRoad(Transform p,Vector2 a,Vector2 b,Color color,float width=2)
        {
            var delta=b-a;var r=Box("Road",p,a.x,a.y,delta.magnitude,width,color);r.pivot=new Vector2(0,.5f);r.localEulerAngles=new Vector3(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);r.GetComponent<Image>().raycastTarget=false;
        }
        Vector2 QuarterPoint(int id){var d=State.config.District(id);return new Vector2(20+d.x*550,80+d.y*390);}
        void DrawLivingCity()
        {
            var p=Box("LivingCity",root,288,186,868,570,Panel);
            Label(p,"新京街巷 / 地点与线报",18,14,375,32,24,Gold);
            for(int i=0;i<3;i++){int n=i;Button("CityLayer_"+i,p,new[]{"线报","搜查","民心"}[i],369+i*78,13,70,32,()=>{cityLayer=n;Draw();},true,cityLayer==i);}
            Button("CityEvidence",p,"线索簿 · "+State.cityEvidence.Count,616,13,232,32,()=>{modal="evidence";Draw();});
            var art=Rect("StreetArt",p,10,56,590,420).gameObject.AddComponent<RawImage>();art.texture=map;art.color=new Color(.68f,.72f,.67f);art.raycastTarget=false;
            Box("StreetTint",p,10,56,590,420,new Color(.02f,.06f,.07f,.25f)).GetComponent<Image>().raycastTarget=false;
            int[,] roads={{0,1},{1,2},{2,3},{0,4},{4,5},{3,5},{2,4}};
            for(int i=0;i<roads.GetLength(0);i++)CityRoad(p,QuarterPoint(roads[i,0]),QuarterPoint(roads[i,1]),new Color(.79f,.68f,.44f,.5f),3);
            // River is an orientation aid, not a travel-time simulation.
            CityRoad(p,new Vector2(568,60),new Vector2(571,465),new Color(.25f,.57f,.63f,.42f),12);
            Label(p,"东\n河",571,361,22,62,16,Teal);
            foreach(var q in State.quarters)
            {
                var d=State.config.District(q.district);var pos=QuarterPoint(q.district);
                int count=DispatchRules.Open(State).Count(m=>m.district==q.district),urgent=DispatchRules.Open(State).Count(m=>m.district==q.district&&m.deadline==State.turn);
                Color tint=cityLayer==1?Color.Lerp(Teal,Red,q.heat/100f):cityLayer==2?Color.Lerp(Muted,Teal,q.support/100f):Gold;
                var halo=Box("QuarterZone",p,pos.x-69,pos.y-26,138,63,new Color(tint.r,tint.g,tint.b,.14f));halo.GetComponent<Image>().raycastTarget=false;
                Button("District_"+d.id,p,d.name,pos.x-58,pos.y-23,116,30,()=>{selectedDistrict=d.id;selectedSite="";selectedMission="";Draw();},true,selectedDistrict==d.id);
                string hint=cityLayer==1?"热度 "+q.heat+" · 暴露+"+CityContentRules.Exposure(State,d.id):cityLayer==2?"支持 "+q.support+" / 100":count+"项线报"+(urgent>0?" · 今夜"+urgent:"");
                Label(p,hint,pos.x-74,pos.y+9,148,25,14,urgent>0?Red:tint,TextAnchor.MiddleCenter);
            }
            foreach(var site in State.city.sites)
            {
                float x=site.x*570+15,y=site.y*400+55;bool unlocked=CityContentRules.Unlocked(State,site);
                string marker=!unlocked?"◇ ":State.visitedSites.Contains(site.id)?"● ":"○ ";
                var b=Button("Site_"+site.id,p,marker+site.name,x-52,y,104,25,()=>SelectSite(site),true,selectedSite==site.id);
                if(!unlocked)b.image.color=new Color(.12f,.17f,.19f,.94f);
                b.GetComponentInChildren<Text>().fontSize=13;
            }
            var area=State.config.District(selectedDistrict);var status=State.quarters.First(x=>x.district==area.id);
            var rail=Box("DistrictDossier",p,610,56,248,420,Ink);
            Label(rail,area.name+" / 区域档案",14,10,224,29,21,Gold);
            Label(rail,"搜查热度 "+status.heat+"   民众支持 "+status.support,14,48,224,25,16,Muted);
            Label(rail,"本区行动额外暴露 +"+CityContentRules.Exposure(State,area.id),14,80,224,27,16,Red);
            var cases=DispatchRules.Open(State).Where(m=>m.district==area.id).ToList();
            Label(rail,"待办线报 · "+cases.Count,14,118,224,28,19,Gold);
            var list=Scroll(rail,"CityIncidents",10,154,230,State.courierEnabled?210:251,Math.Max(1,cases.Count)*69);
            if(cases.Count==0)Label(list,"暂无线报。巡访地点取得线索，留意开放夜与前置条件。",6,10,208,110,18,Muted);
            for(int i=0;i<cases.Count;i++)
            {
                var m=cases[i];Button("CityMission_"+m.id,list,m.title,2,i*69,212,37,()=>SelectMission(m,true),true,selectedMission==m.id);
                Label(list,"第"+m.deadline+"夜截止 / "+m.duty.Replace("Recon","调查").Replace("Contact","交涉").Replace("Sabotage","破坏").Replace("Propaganda","传播"),6,i*69+41,208,23,14,m.deadline==State.turn?Red:Muted);
            }
            if(State.courierEnabled)Button("MapCourier",rail,"待接密件 · "+State.dispatchPackets.Count(x=>x.district==area.id&&x.status==DispatchStatus.Waiting),14,377,220,31,()=>{tab=6;courierPage=0;Draw();});
            var place=State.city.sites.Find(x=>x.id==selectedSite);
            var bottom=Box("SiteDossier",p,10,485,848,75,Ink);
            if(place==null)Label(bottom,"○ 未访  ● 已访  ◇ 待解锁 · 点击地点查看线索与巡访。\n热度每25增加3暴露；支持达到40抵消2。每夜热度自然−2。",14,12,820,57,17,Muted);
            else
            {
                bool unlocked=CityContentRules.Unlocked(State,place);
                Label(bottom,place.name+" · "+(unlocked?"巡访可取得「"+place.evidence+"」":"需要「"+place.requires+"」"),14,8,585,25,18,Gold);
                Label(bottom,place.description,14,36,585,33,14,Muted);
                var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(State));string blocked=CityContentRules.AssignSite(copy,selectedAgent,place.id);
                Button("VisitSite",bottom,unlocked?"派遣巡访 · 1AP":"地点尚未解锁",624,17,210,41,()=>{toast=CityContentRules.AssignSite(State,selectedAgent,place.id);if(toast=="")toast="已安排"+State.agents.First(a=>a.id==selectedAgent).name+"巡访"+place.name+"。";Draw();},blocked=="",true);
            }
        }
        void DrawEvidence(RectTransform p)
        {
            Label(p,"线索簿 / 城市案件",36,27,720,44,30,Gold);
            Label(p,"线索永久保留，不消耗。先巡访取得线索，再选择案件处理方式。",36,89,810,40,19,Muted);
            var list=Scroll(p,"EvidenceScroll",36,148,826,428,Math.Max(1,State.city.cases.Count)*132+100);
            Label(list,State.cityEvidence.Count==0?"尚未收集线索。地图上的空心标记代表未巡访地点。":"已知线索："+string.Join(" / ",State.cityEvidence),0,0,794,87,19,Teal);
            int i=0;foreach(var c in State.city.cases)
            {
                var m=c.mission.task;string state=State.completedMissions.Contains(m.id)?"已完成":State.expiredMissions.Contains(m.id)?"已过期":!CityContentRules.Available(State,m.id)?"线索不足 / 其他方案已选":DispatchRules.Open(State).Any(x=>x.id==m.id)?"可派遣":"等待时机或前置";
                float y=100+i++*132;
                Label(list,m.title+" · "+state,0,y,790,30,21,Gold);
                Label(list,CityContentRules.Summary(c)+"\n"+c.outcome,0,y+37,790,74,17,Muted);
            }
        }
        void DrawCaseDetails(RectTransform p)
        {
            var c=CityContentRules.Case(State,selectedMission);
            if(c==null){Label(p,"案件已不可用。",36,80,800,50,26,Gold);return;}
            var m=c.mission.task;var site=State.city.sites.First(x=>x.id==c.site);
            Label(p,m.title,36,28,720,44,29,Gold);
            Label(p,site.name+" / 第"+m.start+"—"+m.deadline+"夜 / "+PrototypeRules.Name((Duty)Enum.Parse(typeof(Duty),m.duty)),36,90,810,32,20,Teal);
            Label(p,m.body,36,143,810,80,23);
            Label(p,"所需线索："+(string.IsNullOrEmpty(c.requires)?"无":c.requires)+"    前置任务："+(string.IsNullOrEmpty(m.prerequisite)?"无":State.config.missions.First(x=>x.id==m.prerequisite).title),36,239,810,46,18,Muted);
            Label(p,"成功后：额外成果+"+m.progressReward+" / 情报+"+m.intelReward+" / 银两+"+c.mission.money+"\n区域热度"+c.heat.ToString("+0;-0;0")+" / 支持"+c.support.ToString("+0;-0;0")+" / 全城压力"+c.mission.pressureDelta.ToString("+0;-0;0"),36,300,810,72,22,Gold);
            string delayed=c.mission.retaliationDelay>0?c.mission.retaliationDelay+"夜后搜查：全城压力+"+c.mission.retaliationPressure:"没有额外延迟搜查。";
            Label(p,c.outcome+"\n"+delayed+(string.IsNullOrEmpty(c.grants)?"":" 新增线索：「"+c.grants+"」。"),36,392,810,79,20,Muted);
            string alternative=string.Join(" / ",State.city.cases.Where(x=>!string.IsNullOrEmpty(c.group)&&x.group==c.group&&x!=c).Select(x=>x.mission.task.title));
            Label(p,string.IsNullOrEmpty(alternative)?"成功奖励叠加常规行动收益；失败可在期限内重试。":"完成后关闭："+alternative+"\n结算前可撤回改选；行动失败可在期限内重试。",36,505,810,76,20,Red);
        }
    }
}
