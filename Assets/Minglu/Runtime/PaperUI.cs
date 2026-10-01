using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        static readonly Color Paper=Hex("ECE5D5"),PaperInk=Hex("303536"),PaperMuted=Hex("626760"),Accent=Hex("934F3C"),PaperLine=Hex("C8BDA5"),PaperInset=Hex("E0D7C4");
        Font titleFont;
        sealed class PrintButtonArt
        {
            public Sprite normal,hover,pressed,selected,disabled;
            public bool Complete { get { return normal!=null&&hover!=null&&pressed!=null&&selected!=null&&disabled!=null; } }
        }
        readonly Dictionary<string,PrintButtonArt> printButtonArt=new Dictionary<string,PrintButtonArt>();
        PrintButtonArt ButtonArt(string family)
        {
            PrintButtonArt art;
            if(printButtonArt.TryGetValue(family,out art))return art;
            string path="Minglu/UI/PrintButtons/button_"+family+"_";
            art=new PrintButtonArt
            {
                normal=Resources.Load<Sprite>(path+"normal"),hover=Resources.Load<Sprite>(path+"hover"),
                pressed=Resources.Load<Sprite>(path+"pressed"),selected=Resources.Load<Sprite>(path+"selected"),
                disabled=Resources.Load<Sprite>(path+"disabled")
            };
            // A partially installed set must fall back as a whole; never leave an invisible state.
            printButtonArt[family]=art.Complete?art:null;
            return printButtonArt[family];
        }
        bool UseButtonArt(Button button,string family,bool selected)
        {
            var art=ButtonArt(family);if(art==null)return false;
            var image=button.image;image.color=Color.white;image.type=Image.Type.Sliced;image.fillCenter=true;
            image.sprite=selected?art.selected:art.normal;
            var states=button.spriteState;states.highlightedSprite=art.hover;states.pressedSprite=art.pressed;states.selectedSprite=art.selected;states.disabledSprite=art.disabled;
            button.spriteState=states;button.transition=Selectable.Transition.SpriteSwap;
            image.overrideSprite=button.interactable?null:art.disabled;
            // Clear the old tint immediately; SpriteSwap changes textures without a colour tween.
            image.CrossFadeColor(Color.white,0,true,true);
            return true;
        }
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
            if(name.EndsWith("Back")||name.EndsWith("Close")||name.EndsWith("Cancel"))return true;
            if(name.StartsWith("RouteDetails_")||name.StartsWith("LookRandomLock_")||name=="LookRandom"||name=="LookPresets"||name=="LookRandomSettings"||name=="LookLock"||name=="LookLockAll"||name=="LookUnlockAll"||name=="LookZoom"||name=="LookCompare"||name=="LookOptionPrevious"||name=="LookOptionNext")return true;
            if(name=="CommandChiefAppearance"||name=="CommandPeopleDetails"||name=="CommandPeopleAppearance"||name=="CommandEditLook"||name=="CommandEmployment"||name=="DistrictOpen"||name=="StrategyDetails"||name=="LookPopupKeep"||name=="LookSaveSlot")return true;
            if(name.StartsWith("DistrictTask_")||name=="DistrictAffairs"||name=="DistrictRoutes")return true;
            return name=="Save"||name=="Load"||name=="CommandHome"||name=="CommandMenu"||name=="CommandHelp"||name=="CommandPortrait"||name=="CommandNextAction"||name=="CommandMemberOpen"||name=="CommandOverview"||name=="CommandHelpRoute"||name=="CommandPlan"||name=="LookClose"||name=="LookUndo"||name=="LookRedo"||name=="LookReset"||name.StartsWith("CommandRecommend_")||name.StartsWith("CommandCompare_")||name.StartsWith("CommandCancel_")||name.StartsWith("CommandTask_")||name.StartsWith("BriefTask_")||name.StartsWith("RosterVacancy_");
        }
        bool ChoiceControl(string name)
        {
            return name.StartsWith("CommandTab_")||name.StartsWith("TaskFilter_")||name.StartsWith("LookGroup_")||name.StartsWith("LookCategory_")||name.StartsWith("MissionPlan_")||name.StartsWith("MissionMode")||name.StartsWith("LifePolicy_")||name.StartsWith("Difficulty_")||name.StartsWith("Doctrine_")||name=="LookMale"||name=="LookFemale"||name=="ReportSummary"||name=="ReportAll"||name=="MissionIntel";
        }
        Button PrintButton(string name,Transform parent,string text,float x,float y,float w,float h,Action action,bool enabled,bool selected)
        {
            bool paper=PaperSurface(parent),nav=name.StartsWith("CommandTab_"),choice=ChoiceControl(name),quiet=QuietControl(name),photo=name.StartsWith("LookOption_")||name.StartsWith("LookSlot_")||name.StartsWith("CommandAgent_");
            bool submit=RedAction(name);
            if(submit)quiet=false;
            Color ground=paper?Paper:Ink,foreground=paper?PaperInk:TextColor;
            Color normal=submit?Accent:selected&&!choice?paper?PaperInset:Hex("3B3B34"):quiet||choice?ground:paper?PaperInset:Panel;
            if(quiet&&!selected)foreground=paper?PaperMuted:Muted;
            if(submit)foreground=Paper;
            if(!enabled)foreground=paper?Hex("89867D"):Hex("85867D");
            var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=Color.white;
            SurfaceTheme(r,paper);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;b.interactable=enabled;b.transition=Selectable.Transition.ColorTint;
            var colors=b.colors;colors.normalColor=normal;colors.highlightedColor=submit?Hex("AD5141"):paper?Hex("D1C9B6"):Hex("3D403A");colors.pressedColor=submit?Hex("753329"):paper?Hex("C6BEAB"):Hex("45483F");colors.selectedColor=normal;colors.disabledColor=paper?Hex("DFD9CD"):Hex("242622");colors.fadeDuration=.1f;b.colors=colors;
            // Directory links, portrait choices and map pins keep their existing understated styles.
            // Map pins also receive a dynamic urgent tint from DrawCommandMap.
            // Flat dossier controls: state is conveyed by fill, a fine border
            // and typography, rather than embossed raster button textures.
            bool artwork=false;
            if(artwork&&submit&&enabled)foreground=Hex("FFF6E8");
            if(artwork&&!enabled)foreground=submit?Hex("DED6C5"):paper?Hex("625F57"):Hex("A3A294");
            bool left=nav||name.StartsWith("DistrictTask_")||name=="DistrictAffairs"||name=="DistrictRoutes"||name=="CommandMemberOpen"||name=="CommandOverview"||name=="CommandHelpRoute"||name=="CommandPlan"||name.StartsWith("CommandTask_")||name.StartsWith("CommandRecommend_")||name.StartsWith("CommandCompare_")||name.StartsWith("CommandCancel_");
            var label=Label(r,text,nav?42:left?14:8,0,w-(nav?48:left?26:16),h,nav?17:w<60?14:17,foreground,left?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);label.color=foreground;
            if(choice)
            {
                if(nav&&selected){var mark=Box("SelectionMark",r,0,7,3,h-14,Gold);mark.GetComponent<Image>().raycastTarget=false;}
                if(!nav&&selected)Rule(r,9,h-1,w-18,paper?Accent:Gold);
                if(selected&&enabled)label.color=paper?Accent:Gold;
            }
            else if(photo)
            {
                if(selected){Border(r,0,0,w,h,paper?Accent:Gold);}
            }
            else if(!quiet&&!artwork)
            {
                Border(r,0,0,w,h,submit?Hex("B87658"):paper?PaperLine:Line);
            }
            var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=paper?Accent:Gold;outline.effectDistance=new Vector2(1,-1);outline.enabled=false;r.gameObject.AddComponent<UIFocusOutline>();
            // Set the first visible frame directly: a full redraw must not fade every surface from white.
            im.CrossFadeColor(artwork?Color.white:enabled?normal:colors.disabledColor,0,true,true);
            b.onClick.AddListener(()=>{Sound();action();});return b;
        }
    }
}
