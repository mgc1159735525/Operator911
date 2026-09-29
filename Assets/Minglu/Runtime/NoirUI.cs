using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        readonly Dictionary<string,Sprite> noirSprites=new Dictionary<string,Sprite>();
        Sprite NoirSprite(string name)
        {
            Sprite value;if(noirSprites.TryGetValue(name,out value))return value;
            var texture=Resources.Load<Texture2D>("Minglu/UI/Noir/"+name);if(texture==null)return null;
            bool icon=name.StartsWith("icon_");value=Sprite.Create(texture,new UnityEngine.Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,icon?Vector4.zero:new Vector4(12,12,12,12));noirSprites[name]=value;return value;
        }
        void NoirIcon(Transform parent,string name,float x,float y,float size,Color color)
        {
            var image=Rect("Icon_"+name,parent,x,y,size,size).gameObject.AddComponent<Image>();image.sprite=NoirSprite("icon_"+name);image.color=color;image.raycastTarget=false;
        }
        void SkinSurface(Image image,string name,float w,float h,Color color)
        {
            if(w<30||h<24||color.a<.95f||name=="Background")return;
            image.sprite=NoirSprite(name.Contains("Modal")||name.Contains("HomePanel")?"dialog":name.Contains("Card")?"card":"panel");image.type=Image.Type.Sliced;
        }
        bool RedAction(string name)
        {
            return new[]{"CommandEnd","CommandExecute","CommandAuthorize","MissionAuthorize","HomeContinue","BeginRun","ConfirmNewRun","RouteReplaceConfirm","LookApply","CommandEventRisk"}.Contains(name);
        }
        void DecorateButton(Button button,string name,float w,float h,bool primary)
        {
            string icon=null;
            if(name.StartsWith("CommandTab_"))icon=new[]{"tasks","routes","people","map","report","alliance"}[int.Parse(name.Substring(11))];
            else if(w>=250)
            {
                if(name.Contains("Execute")||name=="CommandEnd")icon="night";
                else if(name.Contains("Authorize"))icon="check";
                else if(name.Contains("Settings"))icon="settings";
                else if(name=="HomeContinue")icon="arrow";
                else if(name=="HomeNew")icon="night";
                else if(name=="HomeArchive")icon="report";
                else if(name=="HomeHelp"||name.Contains("Guide"))icon="tasks";
                else if(name=="HomeQuit")icon="close";
                else if(name.Contains("Home")&&!name.Contains("Appearance"))icon="home";
                else if(name=="MissionIntel")icon="intel";
                else if(name=="CommandRecruit")icon="people";
                else if(name.Contains("Risk")||name.Contains("ReplaceConfirm"))icon="risk";
                else if(name=="LookApply")icon="save";
                else if(name.Contains("Back")||name.Contains("Cancel"))icon="close";
                else if(name=="CommunityTask")icon="alliance";
                else if(name.Contains("Compare"))icon="people";
            }
            if(icon==null)return;
            float size=Mathf.Min(23,h-10);NoirIcon(button.transform,icon,8,(h-size)/2,size,primary&&!RedAction(name)?Ink:Gold);
            var label=button.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(34,0);label.rectTransform.sizeDelta=new Vector2(w-42,h);
        }
        void DrawNoirRoster()
        {
            Box("RosterRail",root,24,742,1552,100,Panel);
            Label(root,"当值人员 / "+State.agents.Count,24,715,400,25,16,Gold);
            Button("CommandPortrait",root,"编辑选中成员外观",1320,715,256,24,()=>OpenAppearance(selectedAgent));
            float cell=1552f/Mathf.Max(8,State.agents.Count);
            for(int i=0;i<State.agents.Count;i++)
            {
                var a=State.agents[i];var person=CommandRules.Person(State,a.id);
                var card=Button("CommandAgent_"+a.id,root,"",24+i*cell,742,cell-6,100,()=>{selectedAgent=a.id;Draw();},true,a.id==selectedAgent);
                card.image.color=a.id==selectedAgent?Hex("655D4E"):Hex("303234");
                Portrait(card.transform,a,5,7,53,80);
                Label(card.transform,a.name,65,6,cell-74,25,18,Gold);
                Label(card.transform,person.profession+" "+person.level+"级",65,33,cell-74,22,14);
                string state=MemberStatus(a);
                Label(card.transform,state,65,57,cell-74,21,14,a.condition==Condition.Active?Teal:Red);Bar(card.transform,65,87,cell-78,a.stamina,100,Teal);
            }
            for(int i=State.agents.Count;i<8;i++)
            {
                var vacant=Button("RosterVacancy_"+i,root,"",24+i*cell,742,cell-6,100,()=>{commandTab=2;Draw();});vacant.image.color=Hex("242627");
                NoirIcon(vacant.transform,"people",12,28,38,Muted);Label(vacant.transform,"备用席位",59,23,cell-68,25,17,Muted);Label(vacant.transform,"前往人事招募",59,55,cell-68,24,14,Muted);
            }
        }
        void DrawNoirBrief()
        {
            var p=Box("DispatchBrief",root,24,126,264,578,Panel);
            NoirIcon(p,"intel",14,17,25,Gold);Label(p,"值班简报",48,16,195,30,23,Gold);
            Label(p,"上级要求",16,65,230,25,15,Muted);Label(p,GateProgress(),16,95,230,90,19);
            Label(p,"优先事项",16,193,230,25,16,Red);
            var urgent=State.command.tasks.Where(t=>t.status==0).OrderBy(t=>t.operation==2?0:1).ThenBy(t=>t.deadline).ThenBy(t=>t.actor<0?0:1).Take(3).ToList();
            for(int i=0;i<urgent.Count;i++)
            {
                var t=urgent[i];var card=Button("BriefTask_"+t.id,p,"",14,226+i*78,236,68,()=>{FocusCommandTask(t.id);Draw();});
                Label(card.transform,t.title,10,7,216,27,18,Gold);Label(card.transform,(t.actor>=0?"已授权":"待指派")+" / "+(t.deadline<=State.turn?"今夜截止":"第"+t.deadline+"夜截止"),10,38,216,22,14,t.deadline<=State.turn?Red:Muted);
            }
            Label(p,State.turn<=3?"初次指挥 / 三步上手":"指挥提示",16,468,230,25,15,Muted);Label(p,State.turn<=3?OpeningChecklist():CommandNext(),16,499,230,72,16,Gold);
        }
    }
}
