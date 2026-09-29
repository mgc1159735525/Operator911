using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        bool newCampaign=true;
        bool atTitle=true, settingsFromGame, newCrisis, replaceConfirmed;
        string homePage="main", seedText="", homeNotice="";
        int newDifficulty=1, newDoctrine;
        PlayerProfile profile=new PlayerProfile();
        string ProfilePath { get { return smokeSave!=null?smokeSave+".profile":Path.Combine(Application.persistentDataPath,"minglu-profile-v1.json"); } }
        void InitializeProfile()
        {
            try { profile=RunLifecycle.ReadProfile(ProfilePath); }
            catch(Exception e){homeNotice="档案读取失败，已使用默认设置；原文件保留备份："+e.Message;profile=new PlayerProfile();}
            sound=profile.sound;
            if(smokeSave==null)Screen.fullScreen=profile.fullscreen;
        }
        void PersistProfile()
        {
            try{profile.sound=sound;RunLifecycle.WriteProfile(profile,ProfilePath);}
            catch(Exception e){homeNotice="设置未能保存："+e.Message;}
        }
        void RecordEnding()
        {
            if(State==null||!State.ended||!State.roguelikeRun)return;
            try
            {
                var candidate=JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
                if(RunLifecycle.Record(candidate,State)){RunLifecycle.WriteProfile(candidate,ProfilePath);profile=candidate;}
            }
            catch(Exception e){toast="战绩保存失败："+e.Message;}
        }
        void OpenHome(string page="main") { atTitle=true;homePage=page;settingsFromGame=false;modal="";replaceConfirmed=false;scrollPositions.Clear();Draw(); }
        bool SaveForExit()
        {
            try{PrototypeSave.Write(State,SavePath);RecordEnding();return true;}
            catch(Exception e){toast="存档失败，已留在当前对局："+e.Message;Draw();return false;}
        }
        void ReturnHome(){if(SaveForExit())OpenHome();}
        void QuitGame(){if(!atTitle&&!SaveForExit())return;PersistProfile();Application.Quit();}
        void OnApplicationQuit()
        {
            if(smokeSave!=null||State==null||!State.roguelikeRun)return;
            try{PrototypeSave.Write(State,SavePath);RecordEnding();PersistProfile();}catch(Exception e){Debug.LogWarning("退出存档失败："+e.Message);}
        }
        void OpenSettings(){settingsFromGame=!atTitle;atTitle=true;homePage="settings";Draw();}
        void BackHome()
        {
            if(settingsFromGame){settingsFromGame=false;atTitle=false;modal="menu";Draw();}
            else OpenHome();
        }
        void BeginConfiguredRun()
        {
            uint seed;
            if(string.IsNullOrWhiteSpace(seedText)){seed=BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(),0);if(seed==0)seed=911;}
            else if(!uint.TryParse(seedText,out seed)||seed==0){homeNotice="种子请输入1—4294967295之间的整数，或留空随机。";Draw();return;}
            if(File.Exists(SavePath)&&!replaceConfirmed){homePage="replace";Draw();return;}
            try
            {
                var content=newCampaign?CampaignRules.Load():null;
                var config=newCampaign?CampaignRules.BuildConfig(ConfigStore.Load(),content):ConfigStore.Load();
                var candidate=RunLifecycle.Create(config,seed,newDifficulty,newDoctrine,newCrisis);
                if(newCampaign){CampaignRules.Initialize(candidate,content);CityContentRules.Initialize(candidate);CourierRules.Initialize(candidate);}
                CommandRules.Initialize(candidate,newCrisis);
                candidate.chiefLook=profile.chiefLook==null?new Appearance{style=1}:AppearanceRules.Copy(profile.chiefLook);
                PrototypeSave.Write(candidate,SavePath);
                State=candidate;atTitle=false;homeNotice="";replaceConfirmed=false;ResetRunView();Draw();
            }
            catch(Exception e){homeNotice="无法开始新局："+e.Message;Draw();}
        }
        void ResetRunView()
        {
            LoadArt();selectedAgent=State.agents.First(x=>x.condition==Condition.Active).id;
            selectedSite="";selectedDistrict=0;rescueTarget=-1;selectedMission="";selectedDuty=Duty.Recon;modal="";tab=State.command!=null?0:State.courierEnabled?6:3;reviewingEnding=false;scrollPositions.Clear();
            commandTab=0;commandTask=-1;taskFilter=0;
            toast="本局种子 "+State.initialSeed+(State.command!=null?"。先设计链条，授权任务，再推进一夜。":State.courierEnabled?"。先选成员接回首份密件，再安排外送。":"。先查看限时任务，再选择成员。");
        }
        void DrawHome()
        {
            if(homePage=="appearance"){var studio=Box("HomeAppearance",root,350,145,900,620,Panel);DrawAppearance(studio);return;}
            var art=Rect("HomeArt",root,0,0,1600,900).gameObject.AddComponent<RawImage>();art.texture=map;art.color=new Color(.28f,.32f,.32f,1);art.raycastTarget=false;
            Box("HomeVeil",root,0,0,1600,900,new Color(.03f,.07f,.08f,.5f));
            Label(root,"明路 · 地下",100,76,800,96,64,Gold);Label(root,"新京二十夜 / 单人回合制策略",106,184,900,34,23,TextColor);
            var p=Box("HomePanel",root,860,70,640,750,new Color(.05f,.10f,.12f,.96f));Border(p,0,0,640,750,Line);
            Label(root,"城中每一盏灯，都可能是新的起点。",106,680,720,45,27,Gold);
            Label(root,"本局资源与人员状态不跨局继承。\n设置与战绩独立保存；同种子、同配置、同选择可复现。",106,746,710,88,20,TextColor);
            if(homePage=="main")
            {
                Label(p,"行动总部",38,30,560,48,32,Gold);
                RunState saved=null;string description="尚无本版存档。开始一局新的行动。";
                if(File.Exists(SavePath))try{saved=PrototypeSave.Read(SavePath);description=(saved.ended?"已结束":"可继续")+" · 第"+saved.turn+"夜 · 成果"+saved.progress+"\n种子 "+saved.initialSeed;}catch(Exception e){description="存档不可读取。可保留旧文件并开始新局。\n"+e.Message;}
                Label(p,description,38,91,560,80,19,Muted);
                Button("HomeContinue",p,saved!=null&&saved.ended?"查看上局结局":"继续行动",38,190,564,58,()=>{Load();},saved!=null,true);
                Button("HomeNew",p,"开始新局",38,265,564,58,()=>{homePage="new";homeNotice="";replaceConfirmed=false;Draw();});
                Button("HomeArchive",p,"行动档案 · "+profile.records.Count+" 局",38,340,564,58,()=>{homePage="archive";Draw();});
                Button("HomeSettings",p,"设置",38,415,272,53,OpenSettings);
                Button("HomeHelp",p,"玩法与操作",326,415,276,53,()=>{homePage="help";Draw();});
                Button("HomeQuit",p,"退出游戏",38,493,564,53,QuitGame);
                Button("HomeAppearance",p,"负责人外观 / 组合头像",38,566,564,53,()=>OpenAppearance(-1));
                Label(p,"组织首脑 · 联盟经营与多阶段行动\n负责人及队员可编辑部件，预设跨局保存。\n自动存档：新局、结算、剧情选择、应用外观。",38,641,564,96,16,Muted);
            }
            else if(homePage=="new"||homePage=="replace")
            {
                Label(p,"新的一夜",38,28,550,46,32,Gold);
                Label(p,"难度",38,92,560,25,18,Muted);
                for(int i=0;i<3;i++){int n=i;Button("Difficulty_"+i,p,RunLifecycle.DifficultyName(i),38+i*192,126,180,43,()=>{newDifficulty=n;Draw();},true,newDifficulty==i);}
                Label(p,newCampaign?"组织考验：建立链条 → 培养身份 → 完成随机门槛":"8夜组织挑战：相同规则，更紧迫的截止时间",38,182,564,30,17,Muted);
                Label(p,"开局方案",38,224,560,26,18,Muted);
                for(int i=0;i<3;i++){int n=i;Button("Doctrine_"+i,p,RunLifecycle.DoctrineName(i),38+i*192,264,180,43,()=>{newDoctrine=n;Draw();},true,newDoctrine==i);}
                Label(p,newDoctrine==0?"均衡：使用默认资源与人员状态。":newDoctrine==1?"情报先行：情报+2，全体可行动成员初始暴露10。":"隐蔽入城：初始压力-8，全体可行动成员体力85。",38,321,564,63,18,Muted);
                Label(p,"随机种子（留空随机）",38,396,564,26,18,Muted);
                var field=Box("Seed",p,38,435,360,46,Ink);var input=field.gameObject.AddComponent<InputField>();
                var text=Label(field,seedText,12,8,336,32,21);input.textComponent=text;input.text=seedText;input.characterLimit=10;input.contentType=InputField.ContentType.IntegerNumber;input.onValueChanged.AddListener(v=>seedText=v);
                Button("RandomSeed",p,"随机",416,435,186,46,()=>{seedText="";Draw();});
                Button("CrisisToggle",p,newCrisis?"危机开局：开（一名成员临时离岗）":"危机开局：关（全员归队）",38,500,564,43,()=>{newCrisis=!newCrisis;Draw();});
                Button("CampaignToggle",p,newCampaign?"模式：组织首脑 · 20夜":"模式：快速试玩 · 8夜",38,548,564,32,()=>{newCampaign=!newCampaign;Draw();});
                Button("BeginRun",p,"出发 · 初始化本局",38,591,564,48,BeginConfiguredRun,true,true);
                Button("HomeBack",p,"返回",38,652,160,42,BackHome);
                if(homePage=="replace")
                {
                    var shade=Box("ReplaceShade",root,0,0,1600,900,new Color(0,0,0,.78f));
                    var cover=Box("Replace",shade,500,270,600,340,Panel);Border(cover,0,0,600,340,Gold);
                    Label(cover,"开始新局将替换当前存档",30,30,540,50,29,Gold);
                    Label(cover,"已有存档会保留为备份。未结束的行动不会计入结算战绩。",30,112,540,90,22);
                    Button("ConfirmNewRun",cover,"确认开始",30,250,258,50,()=>{replaceConfirmed=true;homePage="new";BeginConfiguredRun();},true,true);
                    Button("CancelNewRun",cover,"返回配置",310,250,258,50,()=>{homePage="new";Draw();});
                }
            }
            else if(homePage=="settings")
            {
                Label(p,"设置",38,30,560,48,32,Gold);
                Button("SettingSound",p,sound?"界面音效：开":"界面音效：关",38,126,564,56,()=>{sound=!sound;PersistProfile();Draw();});
                Label(p,"音量 "+Mathf.RoundToInt(profile.volume*100)+"%",38,219,564,35,23);
                Button("VolumeDown",p,"降低",38,269,272,50,()=>{profile.volume=Mathf.Max(0,profile.volume-.1f);PersistProfile();Draw();});
                Button("VolumeUp",p,"提高",330,269,272,50,()=>{profile.volume=Mathf.Min(1,profile.volume+.1f);PersistProfile();Draw();});
                Button("SettingFullscreen",p,profile.fullscreen?"显示：全屏":"显示：窗口",38,360,564,56,()=>{profile.fullscreen=!profile.fullscreen;if(smokeSave==null)Screen.fullScreen=profile.fullscreen;PersistProfile();Draw();});
                Label(p,"设置立即生效并独立保存。\n本版只有界面音效，暂未制作背景音乐。",38,460,564,90,20,Muted);
                Button("HomeBack",p,"返回",38,652,160,42,BackHome);
            }
            else if(homePage=="archive")
            {
                Label(p,"历次行动",38,30,560,48,32,Gold);Label(p,"完成 "+profile.records.Count+" 局  /  成功 "+profile.records.Count(r=>r.won)+" 局",38,94,564,33,22);
                var list=Scroll(p,"RunArchive",38,157,564,458,Math.Max(1,profile.records.Count)*105);
                if(profile.records.Count==0)Label(list,"完成首局后，这里会记下种子、结果与开局方案。",0,16,540,90,22,Muted);
                int i=0;foreach(var r in profile.records.AsEnumerable().Reverse())Label(list,(r.won?"行动成功":"行动失败")+" · "+r.date+"\n种子 "+r.seed+" / "+r.doctrine+"\n第"+r.turn+"夜 · 成果"+r.progress,0,i++*105,540,97,19,Muted);
                Button("HomeBack",p,"返回",38,652,160,42,BackHome);
            }
            else
            {
                Label(p,"行动指南",38,30,560,48,32,Gold);
                Label(p,"你是地区负责人\n只做人事和任务授权，不亲自执行接头。\n\n开局三步\n在传递链条选内线、接头人、交通员并启用。\n在任务页看需求，推荐负责人后授权。\n推进一夜，成员自动执行并提交骰面报告。\n\n人员与生活\n体力、精神、暴露和生活积压会影响骰子。\n空闲成员处理日常，提升表身份；合理轮替岗位。\n\n关卡与重开\n按顶部目标完成送达、任务与身份门槛。\n不同种子生成不同队伍、任务与事件。\n\n鼠标操作、滚轮翻页；F5保存、F9读取。",38,113,564,509,20);
                Button("HomeBack",p,"返回",38,652,160,42,BackHome);
            }
            if(!string.IsNullOrEmpty(homeNotice))Label(root,homeNotice,100,845,1400,48,18,Red);
        }
    }
}




