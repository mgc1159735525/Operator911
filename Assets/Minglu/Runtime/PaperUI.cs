using System;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        static readonly Color Paper=Hex("E6DFCF"),PaperInk=Hex("282A27"),PaperMuted=Hex("5D6157"),Accent=Hex("943F32"),PaperLine=Hex("C4BEAF"),PaperInset=Hex("DCD5C5");
        Font titleFont;
        bool PaperSurface(Transform parent)
        {
            for(var p=parent;p!=null;p=p.parent){var theme=p.GetComponent<UISurfaceTheme>();if(theme!=null)return theme.paper;}
            return false;
        }
        void SurfaceTheme(Transform target,bool paper)
        {
            var theme=target.GetComponent<UISurfaceTheme>()??target.gameObject.AddComponent<UISurfaceTheme>();theme.paper=paper;
        }
        Color SurfaceText(Transform parent,Color color)
        {
            if(!PaperSurface(parent))return color;
            if(color==Gold||color==TextColor)return PaperInk;
            if(color==Muted)return PaperMuted;
            if(color==Teal)return Hex("465C4B");
            if(color==Red)return Accent;
            if(color==Line)return PaperLine;
            return color;
        }
        void Rule(Transform parent,float x,float y,float w,Color? color=null)
        {
            var line=Box("PrintRule",parent,x,y,w,1,color??(PaperSurface(parent)?PaperLine:Line));line.GetComponent<Image>().raycastTarget=false;
        }
        bool QuietControl(string name)
        {
            return name=="Save"||name=="Load"||name=="CommandHome"||name=="CommandMenu"||name=="CommandHelp"||name=="CommandPortrait"||name=="CommandNextAction"||name=="CommandMemberOpen"||name=="CommandOverview"||name=="CommandHelpRoute"||name=="CommandPlan"||name=="LookClose"||name=="LookUndo"||name=="LookRedo"||name=="LookReset"||name.StartsWith("CommandRecommend_")||name.StartsWith("CommandCompare_")||name.StartsWith("CommandCancel_")||name.StartsWith("CommandTask_")||name.StartsWith("BriefTask_")||name.StartsWith("RosterVacancy_");
        }
        bool ChoiceControl(string name)
        {
            return name.StartsWith("CommandTab_")||name.StartsWith("TaskFilter_")||name.StartsWith("LookGroup_")||name.StartsWith("LookCategory_")||name.StartsWith("MissionPlan_")||name.StartsWith("LifePolicy_")||name.StartsWith("Difficulty_")||name.StartsWith("Doctrine_")||name=="LookMale"||name=="LookFemale"||name=="ReportSummary"||name=="ReportAll";
        }
        Button PrintButton(string name,Transform parent,string text,float x,float y,float w,float h,Action action,bool enabled,bool selected)
        {
            bool paper=PaperSurface(parent),nav=name.StartsWith("CommandTab_"),choice=ChoiceControl(name),quiet=QuietControl(name),photo=name.StartsWith("LookOption_")||name.StartsWith("LookSlot_")||name.StartsWith("CommandAgent_");
            bool submit=RedAction(name);
            Color ground=paper?Paper:Ink,foreground=paper?PaperInk:TextColor;
            Color normal=submit?Accent:selected&&!choice?paper?PaperInset:Hex("3B3B34"):quiet||choice?ground:paper?PaperInset:Panel;
            if(submit)foreground=Paper;
            if(!enabled)foreground=paper?Hex("89867D"):Hex("85867D");
            var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=Color.white;
            SurfaceTheme(r,paper);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;b.interactable=enabled;b.transition=Selectable.Transition.ColorTint;
            var colors=b.colors;colors.normalColor=normal;colors.highlightedColor=submit?Hex("AD5141"):paper?Hex("D1C9B6"):Hex("3D403A");colors.pressedColor=submit?Hex("753329"):paper?Hex("C6BEAB"):Hex("45483F");colors.selectedColor=normal;colors.disabledColor=paper?Hex("DFD9CD"):Hex("242622");colors.fadeDuration=.1f;b.colors=colors;
            bool left=nav||name=="CommandMemberOpen"||name=="CommandOverview"||name=="CommandHelpRoute"||name=="CommandPlan"||name.StartsWith("CommandTask_")||name.StartsWith("CommandRecommend_")||name.StartsWith("CommandCompare_")||name.StartsWith("CommandCancel_");
            var label=Label(r,text,left?14:8,0,w-(left?26:16),h,nav?21:w<60?14:17,foreground,left?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);label.color=foreground;
            if(choice)
            {
                if(nav&&selected){var mark=Box("SelectionMark",r,0,7,3,h-14,Red);mark.GetComponent<Image>().raycastTarget=false;}
                if(!nav)Rule(r,9,h-1,w-18,selected?(paper?Accent:Gold):(paper?PaperLine:Line));
                if(selected&&enabled)label.color=paper?Accent:Gold;
            }
            else if(photo)
            {
                if(selected){Border(r,0,0,w,h,paper?Accent:Gold);}
            }
            else if(!quiet&&!submit)
            {
                Rule(r,0,h-1,w,paper?Hex("B7AF9D"):Hex("64665A"));
            }
            var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=paper?Accent:Gold;outline.effectDistance=new Vector2(1,-1);outline.enabled=false;r.gameObject.AddComponent<UIFocusOutline>();
            // Set the first visible frame directly: a full redraw must not fade every surface from white.
            im.CrossFadeColor(enabled?normal:colors.disabledColor,0,true,true);
            b.onClick.AddListener(()=>{Sound();action();});return b;
        }
    }
}
