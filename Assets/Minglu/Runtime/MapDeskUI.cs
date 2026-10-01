using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        void OpenCommandPage(int page)
        {
            commandTab=Mathf.Clamp(page,0,5);modal="";Draw();
        }
        void DrawCommandBusiness()
        {
            bool people=commandTab==2;
            float height=people?844:706;
            var shade=Box("CommandShade",root,0,0,1600,900,new Color(0,0,0,.76f));
            Box("DialogShadow",shade,359,(900-height)/2+10,900,height,new Color(0,0,0,.24f));
            var panel=Box("CommandModal",shade,350,(900-height)/2,900,height,Panel);
            Box("CaseRibbon",panel,59,0,64,5,Accent);
            float top=16;
            if(people)
            {
                Label(panel,"成员名册  ·  "+State.agents.Count+" / "+State.command.config.capacity,59,23,782,24,15,Muted);
                var roster=Scroll(panel,"CommandRoster",59,60,782,86,Mathf.CeilToInt(State.agents.Count/4f)*46);
                for(int i=0;i<State.agents.Count;i++)
                {
                    var a=State.agents[i];
                    Button("CommandAgent_"+a.id,roster,a.name+(a.condition==Condition.Active?"":" · 离岗"),(i%4)*193,i/4*46,183,38,()=>{selectedAgent=a.id;Draw();},true,a.id==selectedAgent);
                }
                top=154;
            }
            var body=Rect("CommandBusinessBody",panel,31,top,838,578);
            if(commandTab==0)DrawCommandTasks(body);
            else if(commandTab==1)DrawCommandRoutes(body);
            else if(commandTab==2)DrawCommandPeople(body);
            else if(commandTab==4)DrawCommandReport(body);
            else if(commandTab==5)DrawStrategy(body);
            Button("CommandPageClose",panel,"返回城区地图",59,height-76,276,48,()=>OpenCommandPage(3));
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
