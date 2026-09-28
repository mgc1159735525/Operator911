using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    [Serializable] public class CampaignStage
    {
        public string id,name,description;
        public int start,end,pressure;
    }
    [Serializable] public class CampaignMission
    {
        public MissionRow task;
        public int pressureDelta,money,retaliationDelay,retaliationPressure;
        public string flag;
    }
    [Serializable] public class CampaignCharacter
    {
        public int id,persuade,organize;
        public string role,cover,bio;
    }
    [Serializable] public class CampaignContent
    {
        public string id,title,description,finalMission;
        public int turns=20,goal=70,income=12,startingMoney=48;
        public List<CampaignStage> stages=new List<CampaignStage>();
        public List<CampaignMission> missions=new List<CampaignMission>();
        public List<EventRow> events=new List<EventRow>();
        public List<CampaignCharacter> characters=new List<CampaignCharacter>();
    }
    [Serializable] public class PendingConsequence
    {
        public string cause;
        public int due,pressure;
    }
    public static class CampaignRules
    {
        public static CampaignContent Load()
        {
            var asset=Resources.Load<TextAsset>("Minglu/Config/campaign_xinjing");
            if(asset==null)throw new InvalidOperationException("未找到新京战役内容。");
            return JsonUtility.FromJson<CampaignContent>(asset.text);
        }
        public static GameConfig BuildConfig(GameConfig source,CampaignContent content)
        {
            Validate(content);
            var c=JsonUtility.FromJson<GameConfig>(JsonUtility.ToJson(source));
            c.revision="minglu-campaign-0.6";
            c.settings.First(x=>x.id=="turns").value=content.turns;
            c.settings.First(x=>x.id=="goal").value=content.goal;
            c.settings.First(x=>x.id=="pressurePerTurn").value=content.stages[0].pressure;
            c.settings.First(x=>x.id=="ap").value=4;
            c.Action(Duty.Sabotage).progressReward=5;c.Action(Duty.Sabotage).pressure=4;
            c.Action(Duty.Recon).progressReward=1;
            c.actions.Add(new ActionRow{id="Contact",name="秘密接头",description="运用说服能力联络城中支持者，成功降低敌方压力。",ap=1,required=1,staminaCost=15,exposure=8,failExposure=10,intelReward=1,progressReward=1});
            c.actions.Add(new ActionRow{id="Propaganda",name="散布思想",description="运用组织能力联络街坊、传抄文告，推进城市解放。",ap=1,required=1,staminaCost=18,exposure=12,failExposure=12,progressReward=4});
            c.missions=content.missions.Select(m=>JsonUtility.FromJson<MissionRow>(JsonUtility.ToJson(m.task))).ToList();
            c.events=content.events.Select(e=>JsonUtility.FromJson<EventRow>(JsonUtility.ToJson(e))).ToList();
            foreach(var row in content.characters)
            {
                var agent=c.agents.Find(a=>a.id==row.id);
                if(agent==null)throw new InvalidOperationException("战役角色在基础Agents表中不存在："+row.id);
                agent.role=row.role;agent.bio=row.bio;
            }
            ConfigStore.Validate(c);return c;
        }
        public static void Initialize(RunState s,CampaignContent content)
        {
            s.campaignEnabled=true;s.campaign=content;
            s.organization.capacity=8;s.organization.income=content.income;s.money=content.startingMoney;
            s.organization.spyDuration=5;
            foreach(var row in content.characters)
            {
                var a=s.agents.First(x=>x.id==row.id);a.persuade=row.persuade;a.organize=row.organize;a.coverRole=row.cover;
            }
            BeginTurn(s);
            Log(s,"战役目标：积累"+s.config.Value("goal")+"成果，并在决战期完成「最终起义」。仅达到成果不提前结束战役。");
        }
        public static CampaignStage Stage(RunState s){return s.campaign.stages.First(x=>x.start<=s.turn&&x.end>=s.turn);}
        public static void BeginTurn(RunState s)
        {
            if(!s.campaignEnabled)return;
            var phase=Stage(s);s.config.settings.First(x=>x.id=="pressurePerTurn").value=phase.pressure;
            if(s.turn==phase.start)Log(s,"【"+phase.name+"】"+phase.description+" 敌方自然压力每夜+"+phase.pressure+"。");
        }
        public static void SettleConsequences(RunState s)
        {
            if(!s.campaignEnabled)return;
            foreach(var effect in s.consequences.Where(e=>e.due<=s.turn).ToList())
            {
                s.pressure=Math.Max(0,Math.Min(100,s.pressure+effect.pressure));
                Log(s,"延迟后果：「"+effect.cause+"」引发搜查，敌方压力+"+effect.pressure+"。");s.consequences.Remove(effect);
            }
        }
        public static void CompleteMission(RunState s,string id)
        {
            if(!s.campaignEnabled)return;
            var m=s.campaign.missions.First(x=>x.task.id==id);
            s.pressure=Math.Max(0,Math.Min(100,s.pressure+m.pressureDelta));s.money+=m.money;
            if(!string.IsNullOrEmpty(m.flag)&&!s.campaignFlags.Contains(m.flag))s.campaignFlags.Add(m.flag);
            if(m.retaliationDelay>0)s.consequences.Add(new PendingConsequence{cause=m.task.title,due=s.turn+m.retaliationDelay,pressure=m.retaliationPressure});
            Log(s,m.task.title+"后续：压力"+m.pressureDelta.ToString("+0;-0;0")+"，银两+"+m.money+(m.retaliationDelay>0?"；"+m.retaliationDelay+"夜后敌方报复，压力+"+m.retaliationPressure:"。"));
        }
        public static void SuccessfulAction(RunState s,Agent a)
        {
            if(!s.campaignEnabled)return;
            if(a.order==Duty.Contact){s.pressure=Math.Max(0,s.pressure-5);Log(s,a.name+"疏通联络，敌方压力-5。");}
            int index=a.order==Duty.Recon?0:a.order==Duty.Contact?2:a.order==Duty.Propaganda?3:1;
            if(a.skillExperience==null||a.skillExperience.Length!=4)a.skillExperience=new int[4];
            a.skillExperience[index]++;
            if(a.skillExperience[index]>=4)
            {
                a.skillExperience[index]=0;
                if(index==0)a.intel=Math.Min(5,a.intel+1);else if(index==1)a.stealth=Math.Min(5,a.stealth+1);else if(index==2)a.persuade=Math.Min(5,a.persuade+1);else a.organize=Math.Min(5,a.organize+1);
                Log(s,a.name+"积累了实战经验，"+new[]{"情报","潜行","说服","组织"}[index]+"能力得到锻炼（上限5）。");
            }
        }
        public static int IdentityBonus(Agent a,Duty duty)
        {
            return (a.coverRole=="军官"&&duty==Duty.Sabotage)||(a.coverRole=="记者"&&duty==Duty.Recon)||(a.coverRole=="教师"&&duty==Duty.Propaganda)||(a.coverRole=="商人"&&duty==Duty.Contact)?1:0;
        }
        public static void Recruit(RunState s,Agent a,string role)
        {
            if(!s.campaignEnabled)return;
            a.persuade=3;a.organize=2;a.coverRole=role.Contains("药")?"医生":role.Contains("书")?"教师":"商人";
        }
        public static bool CanWin(RunState s){return !s.campaignEnabled||(s.turn>=17&&s.completedMissions.Contains(s.campaign.finalMission));}
        public static string Ending(RunState s,bool success)
        {
            if(success)return s.agents.All(a=>a.condition!=Condition.Lost)&&s.campaignFlags.Contains("shelter_people")?"明路：城中起义成功，同伴们守住了彼此，也守住了百姓的灯火。":"解放：最终起义完成，二十夜的准备汇成了黎明。仍有一些名字，留在黑暗之中。";
            return s.agents.Count(a=>a.condition==Condition.Lost)>=2?"悲剧：组织遭受重创，未能完成最终起义。留下的记录将交给后来者。":s.progress>=30?"妥协：未能完成最终目标，组织保留了火种，转入更深的潜伏。":"长夜：行动窗口已经关闭，准备与联络尚不足以改变这座城。";
        }
        public static void ChooseEvent(RunState s,string id,bool optionA)
        {
            if(s.campaignEnabled&&id=="shelter_choice"&&optionA&&!s.campaignFlags.Contains("shelter_people"))s.campaignFlags.Add("shelter_people");
        }
        public static void Validate(CampaignContent c)
        {
            if(c==null||c.id!="xinjing"||c.turns!=20||c.goal<40||c.goal>100||string.IsNullOrWhiteSpace(c.title)||c.stages==null||c.stages.Count!=4||c.missions==null||c.events==null||c.characters==null||c.income<0||c.income>40||c.startingMoney<0||c.startingMoney>200)throw new InvalidOperationException("战役基础数据无效。");
            int[] starts={1,5,11,17},ends={4,10,16,20};
            for(int i=0;i<4;i++)if(c.stages[i]==null||c.stages[i].start!=starts[i]||c.stages[i].end!=ends[i]||c.stages[i].pressure<0||c.stages[i].pressure>10||string.IsNullOrWhiteSpace(c.stages[i].name))throw new InvalidOperationException("战役阶段须覆盖1—4、5—10、11—16、17—20夜。");
            if(c.missions.Any(m=>m==null||m.task==null||Math.Abs(m.pressureDelta)>30||m.money<0||m.money>50||m.retaliationDelay<0||m.retaliationDelay>5||m.retaliationPressure<0||m.retaliationPressure>30)||!c.missions.Any(m=>m.task.id==c.finalMission&&m.task.start==17&&m.task.deadline==20))throw new InvalidOperationException("战役任务后果或最终任务无效。");
            if(c.characters.Any(a=>a==null||a.persuade<1||a.persuade>5||a.organize<1||a.organize>5||!new[]{"商人","记者","教师","军官","医生"}.Contains(a.cover))||c.characters.Select(a=>a.id).Distinct().Count()!=c.characters.Count)throw new InvalidOperationException("战役人物配置无效。");
            string[] chain={"shelter","network","garrison","uprising"};
            if(c.finalMission!="uprising"||chain.Any(id=>c.missions.Count(m=>m.task.id==id)!=1)||c.characters.Count!=4||!Enumerable.Range(0,4).All(id=>c.characters.Any(a=>a.id==id)))throw new InvalidOperationException("当前章节须保留四个主线任务与四名初始人物。");
            for(int i=1;i<chain.Length;i++)if(c.missions.First(m=>m.task.id==chain[i]).task.prerequisite!=chain[i-1])throw new InvalidOperationException("主线前置顺序不合法。");
        }
        public static void ValidateSave(RunState s)
        {
            if(!s.campaignEnabled)return;
            Validate(s.campaign);
            if(s.campaignFlags==null||s.consequences==null||s.campaignFlags.Distinct().Count()!=s.campaignFlags.Count||s.consequences.Any(e=>e==null||e.due<1||e.due>25||e.pressure<0||e.pressure>30)||s.config.Value("turns")!=20||!s.config.actions.Any(a=>a.id=="Contact")||!s.config.actions.Any(a=>a.id=="Propaganda")||s.agents.Any(a=>a.persuade<1||a.persuade>5||a.organize<1||a.organize>5||a.skillExperience==null||a.skillExperience.Length!=4||a.skillExperience.Any(x=>x<0||x>3)))throw new InvalidOperationException("战役状态存档无效。");
        }
        static void Log(RunState s,string message){s.journal.Add("["+s.turn+"] "+message);}
    }
}
