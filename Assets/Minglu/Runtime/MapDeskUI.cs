using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawDossierDesk()
        {
            Box("DeskHeader",root,0,0,1600,78,Panel);
            Label(root,"明路 · 地下",24,20,235,36,26,Gold);
            Label(root,"新京地区",248,26,140,25,16,Muted);
            string[] labels={"行动成果","可用情报","可用经费","追查 / 阈值","民心"};
            string[] values={State.progress.ToString("00"),(State.intelligence-CommandRules.ReservedIntel(State))+" 份",(State.money-CommandRules.ReservedMoney(State))+" 银两",State.command.strategy.trace+" / "+State.command.strategy.config.raid,State.command.strategy.hearts.ToString()};
            for(int i=0;i<5;i++)
            {
                float x=450+i*184;Box("HeaderDivider",root,x-20,18,1,42,Line);
                Label(root,labels[i],x,12,165,21,13,Muted);
                Label(root,values[i],x,36,165,29,21,i==3&&State.command.strategy.trace>=State.command.strategy.config.raid-15?Red:TextColor);
            }
            Button("CommandMenu",root,"设置",1410,21,164,38,()=>{modal="menu";Draw();});
            Rule(root,0,77,1600,Gold);
            Box("DeskNavigation",root,0,78,180,712,Ink);
            DrawLook(root,State.chiefLook,53,100,74,100);
            Label(root,"第 "+State.turn.ToString("00")+" 夜",18,218,144,35,25,Gold,UnityEngine.TextAnchor.MiddleCenter);
            Label(root,"共 "+State.config.Value("turns")+" 夜 · 地区负责人",10,261,160,23,13,Muted,UnityEngine.TextAnchor.MiddleCenter);
            Rule(root,20,305,140);
            int[] order={3,0,1,2,4,5};string[] icons={"tasks","routes","people","map","report","alliance"};
            for(int i=0;i<order.Length;i++)
            {
                int page=order[i];float y=323+i*57;
                var button=Button("CommandTab_"+page,root,CommandPageName(page),10,y,160,46,()=>OpenCommandPage(page),true,commandTab==page);
                NoirIcon(button.transform,icons[page],11,12,21,commandTab==page?Gold:Muted);
            }
            Rule(root,20,685,140);
            Button("CommandOverview",root,CommandRules.HasCommandCampaign(State)?"主线委任  ›":"阶段目标  ›",10,706,160,42,()=>{if(CommandRules.HasCommandCampaign(State))OpenCampaignDossier();else{modal="commandOverview";Draw();}});
            Label(root,CampaignDeskObjective(),206,95,1356,29,18,Gold).name="CampaignDeskObjective";
            var desk=Rect("CommandMapDesk",root,194,136,1380,640);DrawCommandMap(desk);
            Box("DeskFooter",root,0,790,1600,110,Panel);Rule(root,0,790,1600,Gold);
            var step=NextCommandStep();
            Label(root,step.ready?"等待你的指示":"当前重点",24,805,156,26,18,Gold);
            Label(root,step.message,200,803,812,48,16,TextColor);
            Label(root,toast,200,854,812,30,13,Muted);
            Button("CommandNextAction",root,step.label,1040,817,232,48,GoNextCommand,step.enabled,!step.ready);
            Button("CommandEnd",root,"结束今夜  →",1292,817,282,48,()=>{modal="commandConfirm";Draw();},!State.ended&&State.command.eventKind==0,step.ready);
        }
        void DossierHeading(UnityEngine.Transform panel,string title,float width=900)
        {
            var header=Box("DossierHeading",panel,0,0,width,80,Ink);SurfaceTheme(header,false);header.GetComponent<UnityEngine.UI.Image>().color=Ink;
            Label(header,title,28,22,width-56,43,29,TextColor);
            Rule(header,0,79,width,Gold);
        }
        void OpenCommandPage(int page)
        {
            commandTab=Mathf.Clamp(page,0,5);modal="";Draw();
        }
        void DrawCommandBusiness()
        {
            bool people=commandTab==2;
            float height=people?860:738;
            var shade=Box("CommandShade",root,0,0,1600,900,new Color(0,0,0,.76f));
            Box("DialogShadow",shade,359,(900-height)/2+10,900,height,new Color(0,0,0,.24f));
            var panel=Box("CommandModal",shade,350,(900-height)/2,900,height,Panel);
            var folio=Box("FolioHeader",panel,0,0,900,30,Ink);SurfaceTheme(folio,false);folio.GetComponent<UnityEngine.UI.Image>().color=Ink;
            Label(folio,"新京地区  /  "+CommandPageName(commandTab),28,5,580,21,13,Gold);
            Label(folio,"第 "+State.turn.ToString("00")+" 夜 · 机密",676,5,196,21,13,Muted,UnityEngine.TextAnchor.MiddleRight);
            Rule(panel,0,30,900,Gold);
            float top=43;
            if(people)
            {
                Label(panel,"成员名册  ·  在编 "+CommandRules.OccupiedRosterCount(State)+" / "+State.command.config.capacity+"（含待救）  ·  离队档案 "+State.agents.Count(a=>a.condition==Condition.Lost),59,40,782,24,15,Muted).name="CommandRosterCount";
                var roster=Scroll(panel,"CommandRoster",59,74,782,86,Mathf.CeilToInt(State.agents.Count/4f)*46);
                for(int i=0;i<State.agents.Count;i++)
                {
                    var a=State.agents[i];
                    Button("CommandAgent_"+a.id,roster,a.name+(a.condition==Condition.Captured?" · 待救":a.condition==Condition.Lost?" · 已离队":""),(i%4)*193,i/4*46,183,38,()=>{selectedAgent=a.id;Draw();},true,a.id==selectedAgent);
                }
                top=170;
            }
            var body=Rect("CommandBusinessBody",panel,31,top,838,578);
            if(commandTab==0)DrawCommandTasks(body);
            else if(commandTab==1)DrawCommandRoutes(body);
            else if(commandTab==2)DrawCommandPeople(body);
            else if(commandTab==4)DrawCommandReport(body);
            else if(commandTab==5)DrawStrategy(body);
            Rule(panel,59,height-91,782);
            Button("CommandPageClose",panel,"返回城区地图",59,height-76,276,48,()=>OpenCommandPage(3));
            Label(panel,"ESC  关闭档案",620,height-63,220,25,14,Muted,UnityEngine.TextAnchor.MiddleRight);
        }
        void DrawCommandPreferences()
        {
            var panel=CommandModal("声音与显示",610);
            Label(panel,"设置立即生效并独立保存。",28,98,844,27,17,Muted);
            Label(panel,"界面音效",28,159,442,29,20,TextColor);
            Button("SettingSound",panel,sound?"已开启":"已关闭",624,150,248,44,()=>{sound=!sound;PersistProfile();Draw();},true,sound);
            Label(panel,"音量",28,238,442,29,20,TextColor);
            Label(panel,Mathf.RoundToInt(profile.volume*100)+"%",510,238,128,29,20,TextColor);
            Button("VolumeDown",panel,"−",680,230,82,44,()=>{profile.volume=Mathf.Max(0,profile.volume-.1f);PersistProfile();Draw();});
            Button("VolumeUp",panel,"+",790,230,82,44,()=>{profile.volume=Mathf.Min(1,profile.volume+.1f);PersistProfile();Draw();});
            Rule(panel,28,310,844);
            Label(panel,"显示模式",28,350,442,29,20,TextColor);
            Button("SettingFullscreen",panel,profile.fullscreen?"全屏":"窗口",624,342,248,44,()=>{profile.fullscreen=!profile.fullscreen;if(smokeSave==null)Screen.fullScreen=profile.fullscreen;PersistProfile();Draw();},true,profile.fullscreen);
            Label(panel,"当前提供界面音效，暂无背景音乐。",28,429,844,42,17,Muted);
            Button("CommandSettingsBack",panel,"返回设置",28,530,276,48,()=>{modal="menu";Draw();});
        }
    }
}
