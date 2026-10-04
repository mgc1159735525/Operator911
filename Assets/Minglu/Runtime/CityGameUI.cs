using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI : MonoBehaviour
    {
        public Font font;
        public RunState State { get; private set; }
        RectTransform root;
        Canvas canvas;
        Camera viewCamera;
        Texture2D map, portraits;
        int selectedAgent = 1, selectedDistrict, rescueTarget = -1, tab = 3;
        Duty selectedDuty = Duty.Recon;
        string toast = "先选择成员，再选择区域与行动。未派遣的成员将自动休整。", modal = "";
        bool sound = true, reviewingEnding;
        AudioSource audioSource;
        AudioClip clickSound;
        uint nextSeed = 911;
        string smokeSave;
        string SavePath { get { return smokeSave ?? Path.Combine(Application.persistentDataPath,"minglu-run-v14.json"); } }
        static readonly Color Ink = Hex("20282D"), Panel = Hex("2D373C"), Line = Hex("4C5558"), Gold = Hex("D2B57F"), TextColor = Hex("EEE8DA"), Muted = Hex("B2B6B2"), Red = Hex("E39A86"), Teal = Hex("A8C0B1");

        void Start()
        {
            try
            {
                font = font != null ? font : Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18);
                titleFont=Font.CreateDynamicFontFromOSFont(new[]{"SimSun","Microsoft YaHei"},28);
                if (Environment.GetCommandLineArgs().Contains("--minglu-capture")) smokeSave = Path.Combine(Path.GetTempPath(), "minglu-ui-" + Guid.NewGuid().ToString("N") + ".json");
                InitializeProfile();
                State = PrototypeRules.NewRun(true, nextSeed, ConfigStore.Load(), true, true);
                selectedAgent = State.agents.First(x => x.condition == Condition.Active).id;
                LoadArt();
                viewCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
                if (viewCamera == null) viewCamera = new GameObject("CityCamera").AddComponent<Camera>();
                // A flat UI uses orthographic projection to keep small control hit areas numerically stable.
                viewCamera.orthographic=true;viewCamera.orthographicSize=450;
                viewCamera.backgroundColor = Ink; viewCamera.clearFlags = CameraClearFlags.SolidColor;
                var go = new GameObject("CityCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = viewCamera; canvas.planeDistance = 2;
                var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                if (FindObjectOfType<EventSystem>() == null) new GameObject("CityEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                audioSource = gameObject.AddComponent<AudioSource>();
                clickSound = AudioClip.Create("Paper tap", 2205, 1, 44100, false);
                var samples = new float[2205];
                for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * .12f) * .07f * Mathf.Exp(-i / 360f);
                clickSound.SetData(samples, 0);
                Draw(); StartCoroutine(CaptureIfRequested());
            }
            catch (Exception e) { Debug.LogException(e); enabled = false; }
        }
        void LoadArt() { map = Resources.Load<Texture2D>(State.config.Asset("city").resource); portraits = Resources.Load<Texture2D>(State.config.Asset("portraits").resource); }
        void Update()
        {
            if (State == null) return;
            if((atTitle&&homePage=="appearance"||modal=="appearance")&&Input.GetKeyDown(KeyCode.Escape)){RequestCloseAppearance();return;}
            if(modal=="appearance"&&Input.GetKeyDown(KeyCode.F9)){lookNotice="请先应用或取消外观修改，再读取存档。";Draw();return;}
            if(State.command!=null&&State.ended&&modal=="endingReview"&&Input.GetKeyDown(KeyCode.Escape)){modal="";Draw();return;}
            if(atTitle){if(Input.GetKeyDown(KeyCode.Escape)&&homePage!="main")BackHome();return;}
            if(!atTitle&&Input.GetKeyDown(KeyCode.Escape)&&HandleCommandModalEscape())return;
            if (Input.GetKeyDown(KeyCode.Escape)) { if(State.command!=null&&(State.command.eventKind!=0||State.ended||State.command.notices.Count>0))return; if (modal != "" && modal != "event" && modal != "ending") { modal = ""; Draw(); } else { modal = "menu"; Draw(); } }
            if (Input.GetKeyDown(KeyCode.F5)) Save();
            if (Input.GetKeyDown(KeyCode.F9)) Load();
        }
        void OnDestroy() { if (clickSound != null) Destroy(clickSound); }
        void Sound() { if (sound && audioSource != null) audioSource.PlayOneShot(clickSound,profile.volume); }
        static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#" + value, out c); return c; }
        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        RectTransform Box(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var r = Rect(name, parent, x, y, w, h); var im = r.gameObject.AddComponent<Image>(); im.color = color; SkinSurface(im,name,w,h,color); return r;
        }
        Text Label(Transform parent, string text, float x, float y, float w, float h, int size = 18, Color? color = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var r = Rect("Text", parent, x, y, w, h); var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = text; t.fontSize = size; t.color = SurfaceText(parent,color ?? TextColor); t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.raycastTarget = false; t.supportRichText = false; t.lineSpacing = 1.12f;if(size>=23){t.font=titleFont??font;t.fontStyle=FontStyle.Normal;} return t;
        }
        Button Button(string name, Transform parent, string text, float x, float y, float w, float h, Action action, bool enabled = true, bool primary = false)
        {
            return PrintButton(name,parent,text,x,y,w,h,action,enabled,primary);
        }
        void Border(Transform parent, float x, float y, float w, float h, Color color)
        {
            Box("Rule",parent,x,y,w,1,color); Box("Rule",parent,x,y+h-1,w,1,color); Box("Rule",parent,x,y,1,h,color); Box("Rule",parent,x+w-1,y,1,h,color);
        }
        void Portrait(Transform parent, Agent a, float x, float y, float w, float h)
        {
            if(a.appearance!=null&&a.appearance.enabled){DrawLook(parent,a.appearance,x,y,w,h,a.condition==Condition.Lost?new Color(.3f,.35f,.35f):a.condition==Condition.Captured?new Color(.65f,.65f,.65f):Color.white);return;}
            var raw = Rect("Portrait",parent,x,y,w,h).gameObject.AddComponent<RawImage>(); raw.texture = portraits;
            int index = State.config.agents.First(d => d.id == a.id).portrait;
            raw.uvRect = new UnityEngine.Rect(index * .25f, 0, .25f, 1);
            raw.color = a.condition == Condition.Lost ? new Color(.3f,.35f,.35f) : a.condition == Condition.Captured ? new Color(.65f,.65f,.65f) : Color.white; raw.raycastTarget = false;
        }
        void Bar(Transform p, float x, float y, float w, int val, int max, Color color)
        {
            Box("Track",p,x,y,w,4,Line); Box("Fill",p,x,y,Mathf.Clamp01((float)val / Math.Max(1,max))*w,4,color);
        }
        RectTransform Scroll(Transform parent, string name, float x, float y, float w, float h, float contentHeight)
        {
            var viewport=Box(name,parent,x,y,w,h,new Color(0,0,0,0)); viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Content",viewport,0,0,w-12,Mathf.Max(h,contentHeight));
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=35;
            // Dynamic text may grow after construction; keep an auto-hiding scrollbar wired up.
            {
                var track=Box("ScrollTrack",viewport,w-10,2,10,h-4,new Color(0,0,0,.025f));
                var handle=Box("ScrollHandle",track,2,0,6,h-4,PaperSurface(parent)?PaperMuted:Muted);
                handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=new Vector2(2,0);handle.offsetMax=new Vector2(-2,0);
                var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<Image>();
                scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            }
            return content;
        }
        void Draw()
        {
            RememberScroll();
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            root=Rect("CityRoot",canvas.transform,0,0,1600,900); root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            Box("Background",root,0,0,1600,900,Ink);
            if(atTitle){DrawHome();RestoreScroll();return;}
            RecordEnding();
            if(State.command!=null){DrawCommandGame();RestoreScroll();return;}
            Label(root,"明路 · 地下",24,23,255,46,32,Gold); Label(root,"新京密报局   /   长夜未尽",26,74,255,25,15,Muted);
            if(State.chiefLook!=null)DrawLook(root,State.chiefLook,880,24,48,65);
            Stat(304,"行动成果",State.progress+" / "+State.config.Value("goal"),State.progress,State.config.Value("goal"),Gold);
            Stat(505,"敌方压力",State.pressure.ToString(),State.pressure,100,Red);
            Stat(706,"情报储备",State.intelligence+" 份 · 已预留 "+PrototypeRules.ReservedIntel(State),State.intelligence,10,Teal);
            Label(root,"第 "+State.turn.ToString("00")+" 夜",940,30,190,36,27); Label(root,"共 "+State.config.Value("turns")+" 回合  /  "+(State.campaignEnabled?CampaignRules.Stage(State).name:State.turn<=2?"蛰伏":State.turn<=5?"暗流":"风暴"),942,72,200,28,16,Muted);
            Button("Save",root,"存档",1180,30,88,38,Save); Button("Load",root,"读档",1276,30,88,38,Load,File.Exists(SavePath));
            Button("Help",root,"指南",1372,30,88,38,()=>{modal="help";Draw();}); Button("Menu",root,"菜单",1468,30,108,38,()=>{modal="menu";Draw();});
            Box("TopRule",root,24,114,1552,1,Line);
            DrawRoster(); DrawCenter(); DrawOrders(); DrawBottom();
            Button("ChiefAppearance",root,"负责人外观",150,137,121,32,()=>OpenAppearance(-1));
            if(State.ended&&modal==""&&!reviewingEnding)modal="ending";
            if(!string.IsNullOrEmpty(State.pendingEvent)&&modal!="report"&&modal!="menu"&&modal!="help")modal="event";
            if(modal!="")DrawModal();
            RestoreScroll();
        }
        void Stat(float x,string title,string value,int amount,int max,Color color)
        {
            Label(root,title,x,27,195,22,15,Muted);Label(root,value,x,53,195,31,23,color);Bar(root,x,93,165,amount,max,color);
        }
        static string Status(Agent a) { return a.condition==Condition.Active?"可行动":a.condition==Condition.Captured?"被捕":a.condition==Condition.Undercover?"潜伏中":"失联"; }
        void DrawRoster()
        {
            Label(root,"同志名录",24,137,190,32,23,Gold);Label(root,"选择成员查看档案与派遣",24,176,248,25,14,Muted);
            var content=Scroll(root,"Roster",24,214,248,528,State.agents.Count*132);
            for(int i=0;i<State.agents.Count;i++)
            {
                var a=State.agents[i];var b=Button("Agent_"+a.id,content,"",0,i*132,232,120,()=>SelectAgent(a));
                b.image.color=a.id==selectedAgent?Hex("304244"):Panel; Portrait(b.transform,a,6,6,54,108);
                Label(b.transform,a.name,70,11,155,28,22,a.condition==Condition.Captured?Red:TextColor);
                var focused=DispatchRules.Open(State).FirstOrDefault(m=>m.id==SelectedMission());
                string comparison=focused!=null&&a.condition==Condition.Active?"成功率 "+(DispatchRules.Probability(DispatchRules.Dice(State,a,(Duty)Enum.Parse(typeof(Duty),focused.duty),focused.id),State.config.Action((Duty)Enum.Parse(typeof(Duty),focused.duty)).required+State.config.District(focused.district).difficulty)*100).ToString("0.#")+"%":State.config.agents.First(x=>x.id==a.id).role+" / "+Status(a);
                Label(b.transform,comparison,70,42,153,22,14,Muted);
                Label(b.transform,a.condition==Condition.Captured?"审讯 "+a.interrogation+" / "+State.config.Value("lostTurn"):a.condition==Condition.Lost?"联络中断":OrganizationRules.OrderName(State,a),70,69,153,24,15,a.condition==Condition.Captured?Red:Gold);
                Bar(b.transform,70,104,66,a.stamina,100,Teal);Bar(b.transform,145,104,66,a.exposure,100,Red);
                if(a.id==selectedAgent)Border(b.transform,0,0,232,120,Gold);
            }
            Label(root,"青线：体力    朱线：暴露",24,748,248,23,13,Muted);
        }
        void DrawCenter()
        {
            string[] tabs={"城坊地图","组织联系","行动日志","限时任务","组织事务","战役进度","今夜指引"};
            for(int i=0;i<(State.courierEnabled?7:6);i++){int index=i;Button("Tab_"+i,root,tabs[i],288+i*(State.courierEnabled?124:119),134,State.courierEnabled?116:111,38,()=>{tab=index;Draw();},true,tab==i);}
            if(!State.courierEnabled)Label(root,"任务完成 " + State.completedMissions.Count,996,143,156,22,14,Muted,TextAnchor.MiddleRight);
            if(tab==0)DrawMap();else if(tab==1)DrawNetwork();else if(tab==3)DrawMissions();else if(tab==4)DrawOrganization();else if(tab==5)DrawCampaign();else if(tab==6)DrawCourier();else DrawJournal();
        }
        void DrawMap()
        {
            if(State.cityEnabled){DrawLivingCity();return;}
            var panel=Box("MapPanel",root,288,186,868,570,Panel);
            var raw=Rect("CityArt",panel,0,0,868,570).gameObject.AddComponent<RawImage>();raw.texture=map;raw.raycastTarget=false;
            foreach(var d in State.config.districts)
            {
                float x=d.x*868,y=d.y*570;Box("PinStem",panel,x-1,y-24,2,30,Gold);
                var pin=Button("District_"+d.id,panel,d.name,x-66,y,132,42,()=>{selectedDistrict=d.id;selectedMission="";Draw();},true,d.id==selectedDistrict);
                pin.image.color=d.id==selectedDistrict?Gold:new Color(.05f,.11f,.13f,.94f);
                int assigned=State.agents.Count(a=>a.condition==Condition.Active&&a.order!=Duty.Rest&&a.order!=Duty.CutContact&&a.district==d.id);
                int captive=State.agents.Count(a=>a.condition==Condition.Captured&&a.district==d.id);
                if(assigned+captive>0)Label(panel,(assigned>0?assigned+"人执行 ":"")+(captive>0?captive+"人被捕":""),x-80,y+45,160,24,14,captive>0?Red:Gold,TextAnchor.MiddleCenter);
            }
            DrawMapMissions(panel);
            Border(panel,0,0,868,570,Line);var dsel=State.config.District(selectedDistrict);
            Box("MapLegend",panel,16,517,836,37,new Color(.04f,.08f,.09f,.94f));
            Label(panel,"选中 "+dsel.name+"    额外暴露 +"+dsel.risk+"    成功需求 +"+dsel.difficulty+"    侦察情报 +"+dsel.reconBonus+"    破坏成果 +"+dsel.sabotageBonus,27,525,815,22,15,Gold);
        }
        void DrawNetwork()
        {
            var p=Box("Network",root,288,186,868,570,Panel);
            Label(p,"灯塔 / 领导者",320,28,250,35,25,Gold,TextAnchor.MiddleCenter);Label(p,"只沿直接下线传播。断线转移让旧地址失效。",140,76,588,25,16,Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<State.agents.Count;i++)
            {
                var a=State.agents[i];var b=Button("NetworkAgent_"+a.id,p,"",38+(i%4)*205,155+(i/4)*172,186,155,()=>{selectedAgent=a.id;modal="agent";Draw();});
                b.image.color=Ink;Portrait(b.transform,a,8,10,53,105);Label(b.transform,a.name,70,12,108,27,20,Gold);Label(b.transform,Status(a),70,47,108,23,15,a.condition==Condition.Captured?Red:Teal);
                var handler=State.agents.Find(x=>x.id==a.handler);Label(b.transform,"上线："+(handler==null?"灯塔":handler.name),8,124,174,24,14,Muted);Bar(b.transform,70,97,105,a.exposure,100,Red);
            }
            Label(p,string.Join("    /    ",State.agents.Select(a=>a.name+" → "+(a.handler<0?"灯塔":State.agents.Find(x=>x.id==a.handler).name))),30,508,808,50,16,Muted);
        }
        void DrawJournal()
        {
            var p=Box("Journal",root,288,186,868,570,Panel);Label(p,"每个决定，都留下回声",22,16,780,35,23,Gold);
            var content=Scroll(p,"LogScroll",22,63,822,484,State.journal.Count*70);
            for(int i=0;i<State.journal.Count;i++){Label(content,State.journal[State.journal.Count-1-i],0,i*70,790,60,17);Box("Divider",content,0,i*70+65,790,1,Line);}
        }
        void DrawOrders()
        {
            if(State.courierEnabled&&tab==6){DrawGuidedOrders();return;}
            var p=Box("OrderPanel",root,1172,134,404,622,Panel);var d=State.config.District(selectedDistrict);var a=State.agents.First(x=>x.id==selectedAgent);
            var mission=DispatchRules.Open(State).FirstOrDefault(m=>m.id==SelectedMission());
            var place=SelectedVisit();
            Label(p,mission==null?(place==null?"行动档案 / "+d.name:place.name+" / 巡访"):mission.title,20,16,364,35,25,Gold);
            Label(p,mission==null?d.subtitle:d.name+" / 第"+mission.deadline+"夜截止",20,59,364,23,15,Teal);
            Label(p,mission==null?(place==null?d.description:place.description+"\n线索："+place.evidence):mission.body.Replace("，","，\n")+"\n额外成果+"+mission.progressReward+"；逾期压力+"+mission.failurePressure,20,91,364,75,17,Muted);Box("Divider",p,20,175,364,1,Line);
            Portrait(p,a,20,192,44,88);Label(p,a.name+" · "+Status(a),78,193,296,30,23);Label(p,"体力"+a.stamina+"  暴露"+a.exposure+"  连勤"+a.consecutiveShifts,78,232,296,24,16,a.consecutiveShifts>=2?Red:Muted);
            Button("AgentDetails",p,"查看人物档案",78,264,155,30,()=>{modal="agent";Draw();});
            if(CityContentRules.Case(State,SelectedMission())!=null)Button("CaseDetails",p,"案情与后果",244,264,140,30,()=>{modal="case";Draw();});
            var duties=new[]{Duty.Recon,Duty.Sabotage,Duty.Rescue,Duty.Rest,Duty.CutContact,Duty.Contact,Duty.Propaganda}.Where(x=>State.config.actions.Any(r=>r.id==x.ToString())).ToArray();
            for(int i=0;i<duties.Length;i++)
            {
                var duty=duties[i];var row=State.config.Action(duty);
                Button("Action_"+duty,p,row.name+"  "+row.ap+"AP",20+(i%2)*186,304+(i/2)*36,178,31,()=>{selectedDuty=duty;selectedMission="";selectedSite="";if(duty==Duty.Rescue)modal="rescue";Draw();},a.condition==Condition.Active&&!State.ended,selectedDuty==duty);
            }
            var action=State.config.Action(selectedDuty);string preview=action.description;
            if(selectedDuty!=Duty.Rest&&selectedDuty!=Duty.CutContact)
            {
                int dice=DispatchRules.Dice(State,a,selectedDuty,SelectedMission()),required=action.required+d.difficulty;
                preview="掷 "+dice+" 骰 / 需 "+required+" 成功   ·   情报消耗 "+action.intelCost+"\n"+Chance(dice,required)+"；体力 -"+action.staminaCost+"，暴露 +"+(action.exposure+d.risk+CityContentRules.Exposure(State,d.id))+"，失败另 +"+action.failExposure;
                if(selectedDuty==Duty.Rescue){var target=State.agents.Find(x=>x.id==rescueTarget);preview+="\n营救："+(target==null?"尚未选择目标":target.name);}
            }
            if(a.condition!=Condition.Active)preview=a.condition==Condition.Undercover?"正在长期潜伏，不能接普通任务。请到组织事务查看进度或安排接应撤回。":a.condition==Condition.Captured?"此人正在被审讯。请选择另一名成员进行营救，或让下线断线转移。":"此人已永久失联，无法派遣或营救。";
            string blocked=AssignmentError();
            Label(p,blocked==""?preview:blocked+"\n"+preview,20,452,364,98,15,blocked==""?Muted:Red);
            Button("Assign",p,blocked!=""?"暂不可签发":a.order!=Duty.Rest?"更新"+a.name+"的指令":mission==null?"签发指令 · "+action.name:"派遣"+a.name,20,557,240,43,Assign,blocked=="",true);
            Button("CancelOrder",p,"撤回",270,557,114,43,()=>{toast=PrototypeRules.Assign(State,a.id,Duty.Rest);if(toast=="")toast=a.name+"将留在安全屋休整。";Draw();},!State.ended&&a.condition==Condition.Active);
        }
        static string Chance(int dice,int required)
        {
            if(required>dice)return "无法完成，请换人或换区域";
            double p=0;for(int k=required;k<=dice;k++){double combinations=1;for(int i=1;i<=k;i++)combinations=combinations*(dice-i+1)/i;p+=combinations/Math.Pow(2,dice);}
            return "成功率 "+(p*100).ToString("0.#")+"%";
        }
        void Assign()
        {
            if(selectedDuty==Duty.Rescue&&rescueTarget<0){modal="rescue";Draw();return;}
            string mission=SelectedMission();
            string error=string.IsNullOrEmpty(mission)?(SelectedVisit()!=null?CityContentRules.AssignSite(State,selectedAgent,selectedSite):PrototypeRules.Assign(State,selectedAgent,selectedDuty,rescueTarget,selectedDistrict)):DispatchRules.Assign(State,selectedAgent,mission);toast=error==""?State.agents.First(x=>x.id==selectedAgent).name+"的指令已签发。可在结算前修改。":error;Draw();
        }
        void DrawBottom()
        {
            Box("Footer",root,24,780,1552,96,Panel);Label(root,"今夜指令",43,795,140,26,20,Gold);Label(root,"行动点 "+PrototypeRules.UsedAP(State)+" / "+State.config.Value("ap"),43,832,160,25,17);
            Label(root,string.Join("  /  ",State.agents.Where(a=>a.condition==Condition.Active||a.condition==Condition.Undercover).Select(a=>a.name+"："+OrganizationRules.OrderName(State,a))),216,793,1060,42,15);Label(root,toast,216,839,1060,28,15,Muted);
            Button("EndTurn",root,State.ended?"查看结局":"封缄 · 结算今夜",1300,805,250,47,()=>{modal=State.ended?"ending":"confirm";Draw();},string.IsNullOrEmpty(State.pendingEvent),true);
        }
        void DrawModal()
        {
            if(modal=="appearance"){DrawAppearanceOverlay();return;}
            var shade=Box("ModalShade",root,0,0,1600,900,new Color(0,0,0,.78f));var p=Box("Modal",shade,350,145,900,620,Panel);Border(p,0,0,900,620,Gold);
            if(modal!="event"&&modal!="ending")Button("CloseModal",p,"关闭",784,20,94,35,()=>{modal="";Draw();});
            if(modal=="confirm")
            {
                Label(p,"封缄之前",36,30,690,50,32,Gold);Label(p,"断线转移 → 行动执行 → 囚犯审讯 → 敌方搜捕",36,99,828,30,21);
                var content=Scroll(p,"OrderSummary",36,153,828,275,State.agents.Count*50);
                int i=0;foreach(var a in State.agents.Where(a=>a.condition==Condition.Active||a.condition==Condition.Undercover)){Label(content,a.name+"   "+OrganizationRules.OrderName(State,a)+"   "+((a.order==Duty.Rest||a.order==Duty.CutContact)&&a.organizationDuty==OrganizationDuty.None&&a.condition!=Condition.Undercover?"":State.config.District(a.district).name),0,i++*50,790,44,21);}
                Label(p,DispatchWarnings(),36,428,828,107,16,Muted);
                Button("ExecuteTurn",p,PrototypeRules.UsedAP(State)==0?"全员休整 · 推进一夜":"执行全部指令",564,541,300,48,()=>{PrototypeRules.Resolve(State);scrollPositions.Remove("ReportScroll");modal="report";toast="今夜行动已结算。";Save(false);Draw();},true,true);
            }
            else if(modal=="report")
            {
                Label(p,"夜报 / 行动后的回声",36,30,800,50,31,Gold);int last=State.journal.FindLastIndex(x=>x.Contains("回合行动"));var logs=State.journal.Skip(Math.Max(0,last)).ToList();var content=Scroll(p,"ReportScroll",36,105,828,405,logs.Count*65);
                for(int i=0;i<logs.Count;i++)Label(content,logs[i],0,i*65,795,59,18);
                Button("ContinueReport",p,"收起密报",564,541,300,48,()=>{modal="";Draw();},true,true);
            }
            else if(modal=="event")
            {
                var e=State.config.events.First(x=>x.id==State.pendingEvent);Label(p,"密报  /  第"+State.turn+"夜",36,28,800,30,18,Muted);Label(p,e.title,36,79,820,55,35,Gold);Label(p,e.body,36,164,828,153,24);EventChoice(p,e,true,341);EventChoice(p,e,false,454);
            }
            else if(modal=="rescue")
            {
                Label(p,"选择营救对象",36,32,760,50,32,Gold);Label(p,"营救将自动定位到囚犯所在区域。",36,98,800,35,19,Muted);var prisoners=State.agents.Where(x=>x.condition==Condition.Captured).ToList();
                if(prisoners.Count==0)Label(p,"目前没有被捕成员。",36,180,800,50,24);
                var content=Scroll(p,"Prisoners",36,159,828,365,prisoners.Count*83);
                for(int i=0;i<prisoners.Count;i++){var prisoner=prisoners[i];Button("RescueTarget_"+prisoner.id,content,prisoner.name+"  /  "+State.config.District(prisoner.district).name+"  /  审讯 "+prisoner.interrogation,0,i*83,790,66,()=>{rescueTarget=prisoner.id;selectedDistrict=prisoner.district;modal="";Draw();},true,true);}
            }
            else if(modal=="dispatchPacket")DrawDispatchPacket(p);
            else if(modal=="evidence")DrawEvidence(p);
            else if(modal=="case")DrawCaseDetails(p);
            else if(modal=="appearance")DrawAppearance(p);
            else if(modal=="agent")
            {
                var a=State.agents.First(x=>x.id==selectedAgent);var data=State.config.agents.First(x=>x.id==a.id);Portrait(p,a,36,36,236,472);Label(p,a.name,307,48,500,55,38,Gold);Label(p,data.role+" / "+Status(a),307,121,500,33,21,Teal);Label(p,data.bio,307,190,535,155,23);
                Label(p,"情报能力 "+a.intel+"   潜行能力 "+a.stealth+"\n说服 "+a.persuade+"   组织 "+a.organize+"   "+a.coverRole+"\n体力 "+a.stamina+"   暴露 "+a.exposure+"\n\n上线："+(a.handler<0?"灯塔":State.agents.First(x=>x.id==a.handler).name),307,366,535,178,20,Muted);Button("AgentAppearance",p,"自定义外观",36,553,236,40,()=>OpenAppearance(a.id));Label(p,"外观不改变能力、身份与行动风险。",307,556,525,30,16,Muted);
            }
            else if(modal=="help")
            {
                Label(p,"密报局行动手册",36,30,760,55,33,Gold);Label(p,"一  分流任务\n限时任务按截止夜排序。先看失守代价，再决定是否承接。\n\n二  核实与派遣\n任务板可花1份未预留情报永久加1骰，选择任务后在右侧签发。\n\n三  轮休\n连续出勤两夜后少1骰，完整休整一夜清除。推荐只选择未派遣成员。\n\n四  守住联系\n审讯达到阈值会供出直接下线。营救先于审讯，断线让旧地址失效。\n\nF5 存档 / F9 读档 / Esc 关闭面板。名录和日志可滚轮滚动。",36,112,828,465,21);
            }
            else if(modal=="menu")
            {
                Label(p,"长夜中的片刻",36,30,760,55,33,Gold);Label(p,"开始新局会放弃尚未保存的进度。磁盘存档在下次保存时替换。",36,110,828,70,21,Muted);
                Button("RestartCrisis",p,"新局 · 危机教学",36,218,390,66,()=>Restart(true),true,true);Button("RestartNormal",p,"新局 · 常规行动",450,218,414,66,()=>Restart(false));
                Button("SoundToggle",p,"设置",36,319,390,60,OpenSettings);Button("OpenGuide",p,"行动指南",450,319,414,60,()=>{modal="help";Draw();});
                Button("ReturnHome",p,"保存并返回主菜单",36,414,390,60,ReturnHome);Button("QuitGame",p,"保存并退出游戏",450,414,414,60,QuitGame);
                Label(p,"暂停期间不推进夜晚。新局按钮会打开初始化配置。",36,520,828,50,18,Muted);
            }
            else if(modal=="ending")
            {
                Label(p,State.won?"火种未熄":"长夜未明",36,37,828,75,45,State.won?Gold:Red);Label(p,State.ending.Replace("。","。\n"),36,155,828,113,26);Label(p,"行动成果 "+State.progress+" / "+State.config.Value("goal")+"\n\n归队 "+State.agents.Count(x=>x.condition==Condition.Active)+"人    被捕 "+State.agents.Count(x=>x.condition==Condition.Captured)+"人    失联 "+State.agents.Count(x=>x.condition==Condition.Lost)+"人",36,309,828,134,24,Muted);
                Button("EndingJournal",p,"复盘日志",36,541,260,48,()=>{reviewingEnding=true;modal="";tab=2;Draw();});Button("EndingHome",p,"返回主菜单",310,541,260,48,ReturnHome);Button("EndingRestart",p,"再起一局",584,541,280,48,()=>Restart(false),true,true);
            }
        }
        void EventChoice(Transform p,EventRow e,bool a,float y)
        {
            int intel=a?e.intelA:e.intelB,pressure=a?e.pressureA:e.pressureB,progress=a?e.progressA:e.progressB;
            Button(a?"EventA":"EventB",p,a?e.optionA:e.optionB,36,y,828,49,()=>{string error=PrototypeRules.ChooseEvent(State,a);if(error==""){modal="";Save(false);}else toast=error;Draw();},State.intelligence+intel>=0&&State.progress+progress>=0,true);Label(p,"情报 "+Signed(intel)+"    压力 "+Signed(pressure)+"    成果 "+Signed(progress),48,y+58,804,30,18,Muted);
        }
        static string Signed(int n) { return n.ToString("+0;-0;0"); }
        void Restart(bool crisis)
        {
            if(!SaveForExit())return;
            newCrisis=crisis;seedText="";OpenHome("new");
        }
        void Save() { Save(true); }
        void Save(bool redraw) {try{PrototypeSave.Write(State,SavePath);if(redraw)toast="已保存当前进度和指令。";}catch(Exception e){toast="保存失败："+e.Message;}if(redraw)Draw();}
        void Load()
        {
            try{State=PrototypeSave.Read(SavePath);selectedSite="";atTitle=false;LoadArt();selectedAgent=(State.agents.FirstOrDefault(a=>a.condition==Condition.Active)??State.agents[0]).id;commandTab=3;commandTask=-1;taskFilter=0;reportNight=-1;reportAll=false;resetCommandTaskScroll=true;scrollPositions.Clear();selectedDistrict=0;selectedMission="";rescueTarget=-1;modal="";reviewingEnding=false;toast="已恢复存档及当局规则。";}catch(Exception e){toast=homeNotice="读取失败："+e.Message;}Draw();
        }
        IEnumerator CaptureIfRequested()
        {
            string[] args=Environment.GetCommandLineArgs();int capture=Array.IndexOf(args,"--minglu-capture");if(capture<0||capture+1>=args.Length)yield break;
            Application.runInBackground=true;UnityEngine.Rendering.SplashScreen.Stop(UnityEngine.Rendering.SplashScreen.StopBehavior.StopImmediate);yield return null;yield return null;
            string path=args[capture+1];Capture(path);
            if(Array.IndexOf(args,"--task-layout-only")>=0){yield return TaskLayoutSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--recruitment-only")>=0){yield return RecruitmentSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--assignment-only")>=0){yield return AssignmentSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--portrait-combinations-only")>=0){yield return PortraitCombinationsSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--recon-only")>=0){yield return ReconDeskSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--map-desk-only")>=0){yield return MapDeskSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--minglu-modal-smoke")>=0){yield return ModalCommandSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--portrait-catalog-only")>=0){yield return PortraitCatalogSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--painted-only")>=0){yield return PaintedPortraitSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--institution-only")>=0){yield return InstitutionSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--button-art-only")>=0){yield return ButtonArtSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--design-only")>=0){yield return DesignSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--producer-only")>=0){yield return ProducerSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--studio-only")>=0){yield return AppearanceStudioSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--strategy-only")>=0){yield return StrategySmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--portrait-repair")>=0){yield return PortraitRepairSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--command-only")>=0){yield return CommandSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--guidance-only")>=0){yield return GuidanceSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--features-only")>=0){yield return FeatureAppearanceSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--realistic-only")>=0){yield return RealisticAppearanceSmoke(path);Application.Quit();yield break;}
            if(Array.IndexOf(args,"--appearance-only")>=0){yield return AppearanceSmoke(path);Application.Quit();yield break;}
            yield return Click("HomeSettings");yield return Click("VolumeDown");yield return Click("HomeBack");
            if(Mathf.Abs(RunLifecycle.ReadProfile(ProfilePath).volume-.6f)>.01f)throw new Exception("Settings did not persist");
            yield return Click("HomeHelp");yield return Click("HomeBack");
            yield return Click("HomeNew");
            yield return Click("CampaignToggle");
            var seedInput=root.GetComponentsInChildren<InputField>().First();seedInput.text="911";
            yield return Click("CrisisToggle");yield return null;Capture(Path.ChangeExtension(path,"newrun.png"));
            yield return Click("BeginRun");
            if(atTitle||!State.roguelikeRun||State.initialSeed!=911||State.agents[0].condition!=Condition.Captured)throw new Exception("New-run initialization failed");
            yield return Click("Menu");yield return Click("ReturnHome");yield return Click("HomeContinue");
            if(atTitle||State.initialSeed!=911)throw new Exception("Main menu continuation failed");
            yield return Click("Agent_1");
            yield return Click("Tab_4");yield return null;Capture(Path.ChangeExtension(path,"organization.png"));
            yield return Click("Recruit_0");yield return Click("Agent_2");yield return Click("StartInfiltration");
            if(OrganizationRules.ReservedMoney(State)!=12||PrototypeRules.UsedAP(State)!=2)throw new Exception("Organization UI reservation failed");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(State.agents.Count!=5||State.agents[2].condition!=Condition.Undercover)throw new Exception("Organization UI recruitment or infiltration failed");
            var organizationScroll=root.GetComponentsInChildren<ScrollRect>().First(s=>s.name=="OrganizationScroll");organizationScroll.verticalNormalizedPosition=.5f;yield return null;
            yield return Click("Maintain_3");if(State.agents[3].maintainCover)throw new Exception("Identity maintenance toggle failed");yield return Click("Maintain_3");
            yield return Click("Recall_2");if(!State.agents[2].recall)throw new Exception("Recall planning failed");yield return Click("Recall_2");
            for(int organizationNight=0;organizationNight<2;organizationNight++)
            {
                yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            }
            if(State.packets.Count!=1)throw new Exception("Spy packet not produced through UI turns");
            yield return Click("Agent_1");yield return Click("DeliverIntel");yield return Click("Save");yield return Click("Load");
            if(State.agents[1].organizationDuty!=OrganizationDuty.Deliver||State.packets.Count!=1)throw new Exception("Expanded UI save/load failed");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return null;Capture(Path.ChangeExtension(path,"intelreport.png"));yield return Click("ContinueReport");
            if(State.packets.Count!=0||State.agents[2].condition!=Condition.Active||State.intelligence<3)throw new Exception("UI courier conversion or spy return failed");
            yield return Click("Menu");yield return Click("RestartCrisis");yield return Click("BeginRun");yield return Click("ConfirmNewRun");
            yield return Click("MissionRecommend_ledger");
            if(selectedAgent!=1)throw new Exception("Mission recommendation did not select specialist");
            yield return Click("MissionLocate_ledger");
            if(tab!=0||selectedDistrict!=1||SelectedMission()!="ledger")throw new Exception("Task map location lost focus");
            yield return Click("MapMission_cargo");yield return Click("Agent_3");
            if(SelectedMission()!="cargo"||selectedDuty!=Duty.Sabotage)throw new Exception("Changing member lost sabotage task");
            yield return null;Capture(Path.ChangeExtension(path,"mapfocus.png"));
            yield return Click("Tab_3");yield return Click("MissionRecommend_ledger");
            yield return Click("MissionVerify_ledger");yield return Click("Assign");
            if(State.agents[1].mission!="ledger"||!State.verifiedMissions.Contains("ledger")||State.intelligence!=0)throw new Exception("Mission UI did not verify and assign");
            yield return Click("Agent_2");
            if(AssignmentError()==""||root.GetComponentsInChildren<Button>().First(b=>b.name=="Assign").interactable)throw new Exception("Conflicting dispatch was not blocked before click");
            yield return Click("MissionRecommend_ledger");
            if(State.agents[1].mission!=""||PrototypeRules.UsedAP(State)!=0)throw new Exception("Card withdrawal did not release assignment");
            yield return Click("MissionRecommend_ledger");yield return Click("Assign");
            yield return null;Capture(Path.ChangeExtension(path,"dispatch.png"));
            yield return Click("Save");yield return Click("Load");
            if(State.agents[1].mission!="ledger")throw new Exception("Mission UI save/load failed");
            yield return Click("Agent_1");yield return Click("CancelOrder");
            if(State.agents[1].mission!="")throw new Exception("Mission UI cancellation failed");
            yield return Click("Menu");yield return Click("RestartCrisis");yield return Click("BeginRun");yield return Click("ConfirmNewRun");
            yield return Click("Tab_0");
            yield return Click("District_5");yield return Click("Action_Sabotage");yield return Click("Assign");if(State.agents.First(x=>x.id==selectedAgent).district!=5)throw new Exception("UI assignment did not select district");
            yield return Click("AgentDetails");yield return null;Capture(Path.ChangeExtension(path,"character.png"));yield return Click("CloseModal");
            yield return Click("EndTurn");yield return null;Capture(Path.ChangeExtension(path,"orders.png"));yield return Click("ExecuteTurn");if(State.turn!=2)throw new Exception("UI turn did not advance");
            yield return null;Capture(Path.ChangeExtension(path,"report.png"));yield return Click("ContinueReport");
            yield return null;Capture(Path.ChangeExtension(path,"event.png"));if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");if(!string.IsNullOrEmpty(State.pendingEvent))throw new Exception("UI event choice not applied");
            yield return Click("Tab_1");yield return null;Capture(Path.ChangeExtension(path,"network.png"));
            yield return Click("Tab_2");yield return null;Capture(Path.ChangeExtension(path,"journal.png"));
            yield return Click("Help");yield return null;Capture(Path.ChangeExtension(path,"help.png"));yield return Click("CloseModal");
            yield return Click("Menu");yield return Click("RestartCrisis");yield return Click("BeginRun");yield return Click("ConfirmNewRun");yield return Click("Agent_3");yield return Click("Action_Rescue");
            yield return null;Capture(Path.ChangeExtension(path,"rescue.png"));yield return Click("RescueTarget_0");yield return Click("Assign");
            if(State.agents.First(x=>x.id==3).order!=Duty.Rescue||State.agents.First(x=>x.id==3).target!=0)throw new Exception("Rescue UI target failed");
            yield return Click("CancelOrder");if(PrototypeRules.ReservedIntel(State)!=0)throw new Exception("Cancel UI did not release intel");
            yield return Click("Agent_1");yield return Click("Action_CutContact");yield return Click("Assign");yield return Click("Save");yield return Click("Load");
            if(State.agents.First(x=>x.id==1).order!=Duty.CutContact)throw new Exception("UI save/load dropped order");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(State.agents.First(x=>x.id==1).handler!=-1)throw new Exception("Cut contact UI failed");
            while(!State.ended)
            {
                yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");
                if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
                yield return null;
            }
            yield return null;Capture(Path.ChangeExtension(path,"ending.png"));
            if(profile.records.Count!=1)throw new Exception("Ending archive not recorded once");
            yield return Click("EndingHome");yield return Click("HomeArchive");yield return null;Capture(Path.ChangeExtension(path,"archive.png"));yield return Click("HomeBack");yield return Click("HomeContinue");
            if(profile.records.Count!=1)throw new Exception("Continued ending duplicated archive");
            yield return Click("EndingHome");yield return Click("HomeNew");yield return Click("CampaignToggle");
            seedText="911";
            if(newCrisis)yield return Click("CrisisToggle");
            yield return null;Capture(Path.ChangeExtension(path,"campaign-new.png"));
            yield return Click("BeginRun");yield return Click("ConfirmNewRun");
            if(!State.campaignEnabled||State.config.Value("turns")!=20)throw new Exception("Campaign menu initialization failed");
            if(!State.cityEnabled)throw new Exception("City content not initialized");
            yield return Click("Tab_0");yield return null;Capture(Path.ChangeExtension(path,"city-map.png"));
            foreach(var site in State.city.sites)yield return Click("Site_"+site.id);
            yield return Click("CityLayer_1");yield return null;Capture(Path.ChangeExtension(path,"city-heat.png"));
            yield return Click("CityLayer_2");yield return Click("CityLayer_0");
            yield return Click("Site_warehouse");
            if(root.GetComponentsInChildren<Button>().First(x=>x.name=="VisitSite").interactable)throw new Exception("Locked site visit is enabled");
            yield return Click("Site_tea");yield return Click("Agent_2");yield return Click("VisitSite");
            yield return Click("Save");yield return Click("Load");
            if(State.agents[2].site!="tea")throw new Exception("Site order lost after load");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(!State.cityEvidence.Contains("证人口供"))throw new Exception("Seed 911 tea visit failed");
            yield return Click("Site_warehouse");yield return null;Capture(Path.ChangeExtension(path,"city-unlock.png"));
            yield return Click("CityEvidence");yield return null;Capture(Path.ChangeExtension(path,"city-evidence.png"));yield return Click("CloseModal");
            yield return Click("District_0");yield return Click("CityMission_tea_protect");yield return Click("Assign");
            yield return Click("CaseDetails");yield return null;Capture(Path.ChangeExtension(path,"case-details.png"));yield return Click("CloseModal");
            yield return Click("CityMission_tea_bait");
            if(root.GetComponentsInChildren<Button>().First(x=>x.name=="Assign").interactable)throw new Exception("Conflicting case approach UI not blocked");
            yield return null;Capture(Path.ChangeExtension(path,"city-choice.png"));
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return null;Capture(Path.ChangeExtension(path,"city-outcome.png"));yield return Click("ContinueReport");
            if(!State.completedMissions.Contains("tea_protect")||DispatchRules.Open(State).Any(x=>x.id=="tea_bait"))throw new Exception("Case outcome UI failed");
            Debug.Log("MINGLU_CITY_UI_SUCCESS: 12 site hit targets, 3 map layers, locked site, persistent visit, clue discovery, hidden location, evidence dossier, exclusive case and consequence");
            yield return Click("Tab_5");yield return null;Capture(Path.ChangeExtension(path,"campaign.png"));
            yield return Click("Tab_0");yield return Click("Agent_2");yield return Click("Action_Contact");yield return Click("Assign");
            yield return Click("Agent_1");yield return Click("Action_Propaganda");yield return Click("Assign");
            yield return Click("AgentDetails");yield return null;Capture(Path.ChangeExtension(path,"campaign-character.png"));yield return Click("CloseModal");
            yield return Click("Save");yield return Click("Load");
            if(State.agents[2].order!=Duty.Contact||State.agents[1].order!=Duty.Propaganda)throw new Exception("Campaign orders lost on load");
            while(!State.ended)
            {
                yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");
                if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
                if(!State.ended&&(State.turn==5||State.turn==11||State.turn==17))
                {
                    yield return Click("Tab_5");yield return null;Capture(Path.ChangeExtension(path,"campaign-phase-"+State.turn+".png"));
                    yield return Click("Save");yield return Click("Load");
                }
            }
            yield return null;Capture(Path.ChangeExtension(path,"campaign-ending.png"));
            if(State.turn!=20||profile.records.Count!=2)throw new Exception("Campaign full-length UI cycle failed");
            Debug.Log("MINGLU_CAMPAIGN_UI_SUCCESS: new run, four abilities, contact/propaganda assignment, save/load, all four phases, 20 nights and ending archive");
            yield return Click("EndingHome");yield return Click("HomeNew");
            seedText="911";yield return Click("BeginRun");yield return Click("ConfirmNewRun");
            yield return Click("CourierInbox");yield return null;Capture(Path.ChangeExtension(path,"courier-inbox.png"));
            yield return Click("CourierDetails_0");yield return null;Capture(Path.ChangeExtension(path,"courier-sealed.png"));yield return Click("CloseModal");
            yield return Click("Agent_0");yield return Click("CourierAssign_0");yield return Click("Save");yield return Click("Load");
            if(State.agents[0].organizationDuty!=OrganizationDuty.ReceivePacket)throw new Exception("Courier receive order not restored");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(State.dispatchPackets[0].status!=DispatchStatus.Received)throw new Exception("Courier did not receive");
            yield return Click("CourierDetails_0");yield return null;Capture(Path.ChangeExtension(path,"courier-read.png"));yield return Click("CloseModal");
            yield return Click("Agent_1");yield return Click("CourierAssign_0");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            yield return Click("CourierReceipts");yield return null;Capture(Path.ChangeExtension(path,"courier-receipt.png"));
            if(CourierRules.Delivered(State)!=1)throw new Exception("Courier delivery failed");
            while(State.turn<7)
            {
                yield return Click("CourierInbox");
                var packet=State.dispatchPackets.FirstOrDefault(x=>x.status==DispatchStatus.Waiting||x.status==DispatchStatus.Received);
                if(packet!=null){yield return Click("Agent_2");yield return Click("CourierAssign_"+packet.id);}
                yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            }
            yield return Click("Agent_0");yield return Click("CourierSources");yield return Click("CourierExpand_1");
            yield return Click("Save");yield return Click("Load");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(State.courier.sources.Count(x=>x.active)!=2)throw new Exception("Courier network expansion failed");
            yield return null;Capture(Path.ChangeExtension(path,"courier-network.png"));
            Debug.Log("MINGLU_COURIER_UI_SUCCESS: sealed and read dossier, receive, different sender, receipts, expansion, pending save/load");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp"})if(File.Exists(file))File.Delete(file);
            foreach (string file in new[] { smokeSave, smokeSave + ".bak", smokeSave + ".tmp" }) if (File.Exists(file)) File.Delete(file);
            Debug.Log("MINGLU_UI_SMOKE_SUCCESS: raycast buttons, map, assignment, dossier, confirmation, turn, event, network, journal, guide, rescue, cancel, save/load, cut contact, ending");Application.Quit();
        }
        IEnumerator Click(string name)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            var button=root.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);if(button==null||!button.interactable)throw new Exception("UI button unavailable: "+name);
            Canvas.ForceUpdateCanvases();
            var rt=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(viewCamera,rt.TransformPoint(rt.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Vector2 debugLocal;RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,pointer.position,viewCamera,out debugLocal);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new Exception("UI button obstructed: "+name+" rect="+rt.rect+" local="+debugLocal+" aspect="+viewCamera.aspect+" contains="+RectTransformUtility.RectangleContainsScreenPoint(rt,pointer.position,viewCamera)+" filter="+button.image.Raycast(pointer.position,viewCamera)+" enabled="+button.image.enabled+" forward="+button.transform.forward+" depth="+button.image.depth+" cull="+button.image.canvasRenderer.cull+" raycast="+button.image.raycastTarget+" at "+pointer.position+" hits "+string.Join(",",hits.Select(h=>h.gameObject.name+"/"+h.gameObject.transform.parent.name)));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        void Capture(string path)
        {
            Canvas.ForceUpdateCanvases();int w=Screen.width,h=Screen.height;var target=new RenderTexture(w,h,24);target.Create();var previous=viewCamera.targetTexture;viewCamera.targetTexture=target;viewCamera.Render();var active=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(w,h,TextureFormat.RGB24,false);texture.ReadPixels(new UnityEngine.Rect(0,0,w,h),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=active;viewCamera.targetTexture=previous;viewCamera.ResetAspect();viewCamera.ResetProjectionMatrix();target.Release();Destroy(target);Destroy(texture);
        }
    }
}


