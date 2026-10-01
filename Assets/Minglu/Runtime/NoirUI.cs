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
            var image=Rect("Icon_"+name,parent,x,y,size,size).gameObject.AddComponent<Image>();image.sprite=NoirSprite("icon_"+name);image.color=SurfaceText(parent,color);image.raycastTarget=false;
        }
        void SkinSurface(Image image,string name,float w,float h,Color color)
        {
            if(w<=2||h<=2){image.color=SurfaceText(image.transform.parent,color);image.raycastTarget=false;return;}
            if(name=="CommandCenter"||name=="CommandModal"||name=="AppearanceStudio"||name=="LookPopup"||name=="Replace")
            {SurfaceTheme(image.transform,true);image.color=Paper;return;}
            if(name=="RealisticPortrait"){SurfaceTheme(image.transform,false);return;}
            if(PaperSurface(image.transform.parent)&&color.a>.95f)
            {
                if(color==Panel)image.color=Paper;
                else if(color==Ink)image.color=PaperInset;
                else if(color==Line)image.color=PaperLine;
                else if(color==Teal)image.color=PaperMuted;
            }
        }
        bool RedAction(string name)
        {
            if(name=="LookLoadSlot"||name=="LookSaveSlot")
            {
                bool occupied=profile.lookSlots!=null&&profile.lookSlots.Length>lookSlot&&profile.lookSlots[lookSlot]!=null;
                return name=="LookLoadSlot"?occupied:!occupied;
            }
            if(name=="CommandEventSafe"||name=="CommandEventRisk")
            {
                int kind=State.command.eventKind;
                int cost=kind==4?State.command.strategy.config.relocateCost:kind==1?6:kind==2?10:0;
                bool affordable=State.money-CommandRules.ReservedMoney(State)>=cost;
                return name=="CommandEventSafe"?affordable:!affordable;
            }
            if(name=="CommandEnd"||name=="CommandNextAction")
            {
                if(State==null||State.command==null)return false;
                var step=NextCommandStep();
                return step.enabled&&(name=="CommandNextAction"?!step.ready:step.ready);
            }
            return new[]{"CommandEnd","CommandExecute","CommandAuthorize","MissionAuthorize","RecruitConfirm","CommandNoticeOpen","HomeContinue","HomeNew","BeginRun","ConfirmNewRun","RouteReplaceConfirm","LookApply","LookRandomFromSettings","LookPopupConfirm"}.Contains(name);
        }
        void DrawNoirRoster()
        {
            Rule(root,32,710,1536);
            Label(root,"人员名册",32,720,240,24,15,Muted);
            Label(root,State.agents.Count+" / "+State.command.config.capacity+" 人",262,721,160,24,14,Muted);
            Button("CommandPortrait",root,"编辑外观",1450,716,118,30,()=>OpenAppearance(selectedAgent));
            float cell=1536f/Mathf.Max(8,State.agents.Count);
            for(int i=0;i<State.agents.Count;i++)
            {
                var a=State.agents[i];var person=CommandRules.Person(State,a.id);
                var card=Button("CommandAgent_"+a.id,root,"",32+i*cell,748,cell-8,89,()=>{selectedAgent=a.id;Draw();},true,a.id==selectedAgent);
                Portrait(card.transform,a,5,5,50,77);
                Label(card.transform,a.name,65,4,cell-76,25,17,a.id==selectedAgent?Gold:TextColor);
                Label(card.transform,person.profession+" · "+person.level+"级",65,31,cell-76,22,14,Muted);
                Label(card.transform,MemberStatus(a),65,55,cell-76,22,13,a.condition==Condition.Active?Teal:Red);
                Bar(card.transform,65,83,cell-80,a.stamina,100,Teal);
            }
            if(State.agents.Count<8)
            {
                var vacant=Button("RosterVacancy_"+State.agents.Count,root,"＋ 接纳成员",32+State.agents.Count*cell,764,cell-8,48,()=>{commandTab=2;Draw();});
            }
        }
        void DrawNoirBrief()
        {
            var p=Rect("DispatchBrief",root,32,436,228,386);
            Rule(p,0,0,228);
            Label(p,"阶段目标",0,20,220,23,14,Muted);Label(p,GateProgress(),0,52,222,71,17,TextColor);
            var t=State.command.tasks.Where(v=>v.status==0).OrderBy(v=>v.operation==2?0:1).ThenBy(v=>v.actor<0?0:1).ThenBy(v=>v.deadline).FirstOrDefault();
            if(t!=null)
            {
                var card=Button("BriefTask_"+t.id,p,"",0,130,228,57,()=>{FocusCommandTask(t.id);Draw();});
                Label(card.transform,t.deadline<=State.turn?"今夜截止":t.actor>=0?"已签发":"待签发",0,0,222,21,13,t.deadline<=State.turn?Red:Muted);
                Label(card.transform,t.title+"  ›",0,27,222,28,16,TextColor);
            }
            Button("CommandOverview",p,"组织概况  ›",-14,210,242,32,()=>{modal="commandOverview";Draw();});
            Label(p,"下一步",0,264,228,22,14,Muted);
            Label(p,CommandNext(),0,296,228,90,14,Muted);
        }
    }
}
