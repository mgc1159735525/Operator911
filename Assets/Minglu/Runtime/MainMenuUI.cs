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
            commandTab=3;commandTask=-1;taskFilter=0;reportNight=-1;reportAll=false;
            toast="本局种子 "+State.initialSeed+(State.command!=null?"。先打开联络网建立链条，再到任务簿授权。":State.courierEnabled?"。先选成员接回首份密件，再安排外送。":"。先查看限时任务，再选择成员。");
        }
        Button HomeTextButton(string name,Transform parent,string text,float x,float y,float w,float h,Action action,bool enabled=true,Color? tint=null)
        {
            var button=Button(name,parent,text,x,y,w,h,action,enabled);
            button.image.sprite=null;button.image.overrideSprite=null;button.image.type=Image.Type.Simple;button.image.color=Color.white;
            button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=Color.clear;colors.highlightedColor=new Color(1,1,1,.055f);colors.selectedColor=new Color(1,1,1,.055f);colors.pressedColor=new Color(1,1,1,.11f);colors.disabledColor=Color.clear;button.colors=colors;
            foreach(Transform child in button.transform)if(child.name.StartsWith("Icon_")||child.name=="PrintRule")child.gameObject.SetActive(false);
            var label=button.GetComponentInChildren<Text>();label.alignment=TextAnchor.MiddleLeft;label.fontSize=20;label.color=enabled?(tint??TextColor):Muted;
            label.rectTransform.anchoredPosition=new Vector2(0,0);label.rectTransform.sizeDelta=new Vector2(w,h);
            button.image.CrossFadeColor(Color.clear,0,true,true);
            return button;
        }
        void DrawHomeHeading(Transform panel,string title,string note)
        {
            Label(panel,title,0,12,758,58,38,Gold);
            Label(panel,note,0,79,758,38,17,Muted);
            Box("HomeSectionRule",panel,0,129,758,1,Line);
        }
        void DrawHome()
        {
            if(homePage=="appearance"){DrawAppearanceOverlay();return;}
            var art=Rect("HomeArt",root,0,0,1600,900).gameObject.AddComponent<RawImage>();art.texture=Resources.Load<Texture2D>("Minglu/UI/Noir/title_city")??map;art.color=new Color(.66f,.68f,.65f,1);art.raycastTarget=false;
            Box("HomeVeil",root,0,0,1600,900,new Color(.045f,.05f,.05f,.36f));
            Box("HomeReadingShade",root,0,0,630,900,new Color(.055f,.06f,.06f,.83f));
            Label(root,"新京 / 地区工作档案",98,69,452,26,16,Muted);
            Label(root,"明路 · 地下",98,122,540,84,homePage=="main"?56:30,homePage=="main"?Gold:TextColor);
            Label(root,"人员安排 · 情报传递 · 行动授权",98,223,470,32,16,Muted);
            Box("HomeTitleRule",root,98,284,408,1,Line);
            Label(root,"单人回合制策略",98,815,408,25,15,Muted);
            if(homePage=="main")
            {
                RunState saved=null;string description="尚无行动存档。\n从「开始新局」建立本次组织。";
                if(File.Exists(SavePath))try{saved=PrototypeSave.Read(SavePath);description=(saved.ended?"行动已结束":"安排已保存，可继续指挥")+"\n成果 "+saved.progress+" / 种子 "+saved.initialSeed;}catch(Exception e){description="存档暂时无法读取，原文件仍保留。\n"+e.Message;}
                if(saved!=null)
                {
                    Button("HomeContinue",root,saved.ended?"查看上局结局":"继续行动",98,327,408,55,()=>{Load();},true,true);
                    HomeTextButton("HomeNew",root,"开始新局",98,406,408,44,()=>{homePage="new";homeNotice="";replaceConfirmed=false;Draw();});
                }
                else
                {
                    Button("HomeNew",root,"开始新局",98,327,408,55,()=>{homePage="new";homeNotice="";replaceConfirmed=false;Draw();},true,true);
                    HomeTextButton("HomeContinue",root,"继续行动 · 尚无存档",98,406,408,44,()=>{Load();},false);
                }
                HomeTextButton("HomeArchive",root,"历次行动    "+profile.records.Count,98,460,408,44,()=>{homePage="archive";Draw();});
                HomeTextButton("HomeAppearance",root,"负责人外观",98,514,408,44,()=>OpenAppearance(-1));
                Box("HomeMenuRule",root,98,585,408,1,Line);
                HomeTextButton("HomeSettings",root,"设置",98,609,408,42,OpenSettings);
                HomeTextButton("HomeHelp",root,"玩法与操作",98,661,408,42,()=>{homePage="help";Draw();});
                HomeTextButton("HomeQuit",root,"退出游戏",98,713,408,42,QuitGame);
                Label(root,"当前档案",1040,656,424,26,14,Muted);
                Label(root,saved==null?"未立卷":"第 "+saved.turn+" 夜",1040,692,424,42,28,TextColor);
                var status=Label(root,description,1040,750,424,85,16,Muted);
                status.verticalOverflow=VerticalWrapMode.Truncate;
            }
            else
            {
                string section=homePage=="new"||homePage=="replace"?"新局登记":homePage=="settings"?"声音与显示":homePage=="archive"?"历次行动":"指挥手册";
                Label(root,section,98,337,408,36,21,TextColor);
                string detail=homePage=="new"||homePage=="replace"?"选择行动时限与开局条件。\n人员、任务与事件随种子生成。":homePage=="settings"?"设置立即生效。\n返回后继续当前安排。":homePage=="archive"?"已结束的行动保存在此。\n成员与资源不跨局继承。":"你担任地区负责人。\n安排人事与目标，由成员执行。";
                Label(root,detail,98,394,408,113,18,Muted);
                var p=Rect("HomePanel",root,694,66,810,742);
                if(homePage=="new"||homePage=="replace")
                {
                    DrawHomeHeading(p,"新局登记",newCampaign?"组织首脑 / 二十夜行动":"快速试玩 / 八夜行动");
                    Label(p,"难度",0,163,128,32,20,Muted);
                    for(int i=0;i<3;i++){int n=i;Button("Difficulty_"+i,p,RunLifecycle.DifficultyName(i),150+i*205,155,193,44,()=>{newDifficulty=n;Draw();},true,newDifficulty==i);}
                    Label(p,"开局方案",0,242,138,32,20,Muted);
                    for(int i=0;i<3;i++){int n=i;Button("Doctrine_"+i,p,RunLifecycle.DoctrineName(i),150+i*205,234,193,44,()=>{newDoctrine=n;Draw();},true,newDoctrine==i);}
                    Label(p,newDoctrine==0?"按默认资源与人员状态开始。":newDoctrine==1?"追加联络经费 12 银两；全员初始暴露 10。情报仍须经机构内线传递送达。":"初始压力 −8；全体可行动成员体力 85。",150,297,603,64,18,Muted);
                    Label(p,"随机种子",0,377,138,32,20,Muted);
                    var field=Box("Seed",p,150,369,389,44,new Color(.08f,.09f,.09f,1));var input=field.gameObject.AddComponent<InputField>();
                    var text=Label(field,seedText,12,7,365,31,20);input.textComponent=text;input.text=seedText;input.characterLimit=10;input.contentType=InputField.ContentType.IntegerNumber;input.onValueChanged.AddListener(v=>seedText=v);
                    var placeholder=Label(field,"留空随机",12,7,365,31,18,Muted);input.placeholder=placeholder;placeholder.gameObject.SetActive(string.IsNullOrEmpty(seedText));
                    HomeTextButton("RandomSeed",p,"重新随机",564,369,194,44,()=>{seedText="";Draw();});
                    HomeTextButton("CrisisToggle",p,newCrisis?"危机开局    开 / 一名成员临时离岗":"危机开局    关 / 全员归队",0,449,758,42,()=>{newCrisis=!newCrisis;Draw();});
                    HomeTextButton("CampaignToggle",p,newCampaign?"行动时限    二十夜 / 组织首脑":"行动时限    八夜 / 快速试玩",0,499,758,42,()=>{newCampaign=!newCampaign;Draw();});
                    Box("HomeSubmitRule",p,0,574,758,1,Line);
                    HomeTextButton("HomeBack",p,"返回",0,604,170,49,BackHome);
                    Button("BeginRun",p,"开始行动",439,604,319,49,BeginConfiguredRun,true,true);
                    Label(p,"新局、夜间结算与事件选择后自动保存。",0,697,758,28,16,Muted);
                    if(homePage=="replace")
                    {
                        var shade=Box("ReplaceShade",root,0,0,1600,900,new Color(0,0,0,.84f));
                        var cover=Box("Replace",shade,430,248,740,368,Paper);
                        Label(cover,"替换当前行动",34,32,672,49,33,PaperInk);
                        Label(cover,"现有存档将保留为备份。\n未结束的行动不会记入历次战绩。",34,110,672,109,22,PaperInk);
                        Box("ReplaceRule",cover,34,239,672,1,PaperMuted);
                        HomeTextButton("CancelNewRun",cover,"返回配置",34,279,268,48,()=>{homePage="new";Draw();},true,PaperInk);
                        Button("ConfirmNewRun",cover,"确认开始新局",376,279,330,48,()=>{replaceConfirmed=true;homePage="new";BeginConfiguredRun();},true,true);
                    }
                }
                else if(homePage=="settings")
                {
                    DrawHomeHeading(p,"设置","声音与显示偏好独立保存");
                    Label(p,"界面音效",0,174,246,34,22,TextColor);
                    Button("SettingSound",p,sound?"已开启":"已关闭",388,165,218,45,()=>{sound=!sound;PersistProfile();Draw();},true,sound);
                    Label(p,"音量",0,267,246,34,22,TextColor);
                    Label(p,Mathf.RoundToInt(profile.volume*100)+"%",388,266,104,34,22,Gold);
                    Button("VolumeDown",p,"−",522,262,58,43,()=>{profile.volume=Mathf.Max(0,profile.volume-.1f);PersistProfile();Draw();});
                    Button("VolumeUp",p,"+",594,262,58,43,()=>{profile.volume=Mathf.Min(1,profile.volume+.1f);PersistProfile();Draw();});
                    Box("SettingsRule",p,0,349,758,1,Line);
                    Label(p,"显示模式",0,396,246,34,22,TextColor);
                    Button("SettingFullscreen",p,profile.fullscreen?"全屏":"窗口",388,388,218,45,()=>{profile.fullscreen=!profile.fullscreen;if(smokeSave==null)Screen.fullScreen=profile.fullscreen;PersistProfile();Draw();},true,profile.fullscreen);
                    Label(p,"当前提供界面音效，暂无背景音乐。",0,509,758,65,19,Muted);
                    HomeTextButton("HomeBack",p,settingsFromGame?"返回当前行动":"返回",0,644,220,48,BackHome);
                }
                else if(homePage=="archive")
                {
                    DrawHomeHeading(p,"历次行动","完成 "+profile.records.Count+" 局 / 成功 "+profile.records.Count(r=>r.won)+" 局");
                    var list=Scroll(p,"RunArchive",0,154,758,471,Math.Max(1,profile.records.Count)*116);
                    if(profile.records.Count==0)Label(list,"完成首局后，这里会记录结果、种子与开局方案。",0,21,724,94,22,Muted);
                    int i=0;foreach(var r in profile.records.AsEnumerable().Reverse())
                    {
                        int y=i++*116;
                        Label(list,(r.won?"行动成功":"行动失败")+" / "+r.date,0,y+5,715,34,24,TextColor);
                        Label(list,"第 "+r.turn+" 夜 · 成果 "+r.progress+"\n种子 "+r.seed+" / "+r.doctrine,0,y+46,715,52,18,Muted);
                        Box("ArchiveRule",list,0,y+111,715,1,Line);
                    }
                    HomeTextButton("HomeBack",p,"返回",0,660,170,48,BackHome);
                }
                else
                {
                    DrawHomeHeading(p,"行动指南","目标由你确定，具体行动由成员完成");
                    var list=Scroll(p,"HomeGuide",0,154,758,472,860);
                    var guide=Label(list,"建立传递链\n内线必须在敌方机构任职。选择机构内线、接头人、交通员三名不同成员并启用。缺少内线可在人事档案建立任职任务；普通任务与生活不产出情报。\n\n授权任务\n看任务需求与期限，比较负责人，再选择行动方案。提交前检查成功率、资源投入及岗位冲突。\n\n推进与复盘\n确认夜间安排后才推进时间。成员提交执行结果；失败、暂停和到期原因都可在报告中查阅。\n\n生活与成长\n同一成员优先执行授权任务，对应链条岗位会等待。空闲时恢复状态、处理日常并培养表身份；留意连续工作与暴露预警。\n\n阶段目标\n按值班简报中的当前进度，完成送达、任务和身份门槛。不同种子生成不同队伍与事件。\n\n操作\n鼠标选择，滚轮翻页；F5 保存，F9 读取，Esc 返回。",0,0,725,860,20,TextColor);
                    float guideHeight=Mathf.Max(472,guide.preferredHeight+18);guide.rectTransform.sizeDelta=new Vector2(725,guideHeight);list.sizeDelta=new Vector2(list.sizeDelta.x,guideHeight);
                    HomeTextButton("HomeBack",p,"返回",0,660,170,48,BackHome);
                }
            }
            if(!string.IsNullOrEmpty(homeNotice))Label(root,homeNotice,98,853,1406,40,17,Red);
        }
    }
}
