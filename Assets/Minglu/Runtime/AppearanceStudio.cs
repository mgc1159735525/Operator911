using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        Appearance lookOriginal;
        readonly Stack<Appearance> lookUndo=new Stack<Appearance>(),lookRedo=new Stack<Appearance>();
        readonly bool[] lookLocks=new bool[14];
        int lookGroup;
        bool lookZoom,lookCompare;
        string lookPopup="",lookReturnModal="",lookSaveError="";
        static readonly Color ClassicPaper=Hex("E9E2CA"),ClassicLight=Hex("F2EDD9"),ClassicInset=Hex("DCD5BE"),ClassicInk=Hex("2D4035"),ClassicEdge=Hex("637461"),ClassicQuiet=Hex("727761"),ClassicGreen=Hex("3C5A43"),ClassicOchre=Hex("865D3F");

        bool LookDirty(){return JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(lookOriginal);}
        void ChangeLook(Action change)
        {
            var before=AppearanceRules.Copy(lookDraft);change();
            // A female head has no beard channel; clear it before the current
            // save schema validates the edited draft during normalization.
            if(lookDraft.sex==1)lookDraft.parts[6]=0;
            lookDraft=AppearanceRules.AsRealistic(lookDraft);lookDraft.enabled=true;
            bool changed=JsonUtility.ToJson(before)!=JsonUtility.ToJson(lookDraft);
            if(changed){lookUndo.Push(before);lookRedo.Clear();}
            lookCompare=false;lookNotice=changed?"预览已更新，确认后保存。":"外观未改变；已保留的部件不参与随机。";Draw();
        }
        void UndoLook(bool redo)
        {
            var from=redo?lookRedo:lookUndo;var to=redo?lookUndo:lookRedo;if(from.Count==0)return;
            to.Push(AppearanceRules.Copy(lookDraft));lookDraft=from.Pop();if(lookDraft.sex==1&&lookCategory==6)lookCategory=2;
            lookCompare=false;lookNotice=redo?"已重做，确认后保存。":"已撤销，确认后保存。";Draw();
        }
        void RandomizeLook()
        {
            var categories=LookEditableCategories();
            if(categories.All(category=>lookLocks[category])){lookNotice="已保留全部部件，外观未改变。";Draw();return;}
            ChangeLook(()=>
            {
                var old=lookDraft;var next=AppearanceRules.Randomize(Guid.NewGuid().GetHashCode(),old.sex,lookLocks[0]?old.parts[0]:-1);
                // Only current, visible choices can be locked. The removed hair
                // and ear channels stay under the save migration's control.
                foreach(int category in categories)if(lookLocks[category])next.parts[category]=old.parts[category];
                lookDraft=next;
            });
        }
        void RequestCloseAppearance()
        {
            if(lookPopup!=""){lookPopup="";Draw();return;}
            if(LookDirty()){lookPopup="discard";Draw();}else CloseAppearance();
        }
        void DrawAppearanceOverlay()
        {
            var shade=Box("AppearanceShade",root,0,0,1600,900,new Color(.06f,.09f,.07f,.88f));
            var p=Box("AppearanceStudio",shade,240,30,1120,840,ClassicPaper);p.GetComponent<Image>().color=ClassicPaper;SurfaceTheme(p,true);DrawAppearance(p);
        }
        void SelectLookCategory(int category){lookCategory=category;Draw();FocusLookControl("LookCategory_"+category);}
        void FocusLookControl(string control)
        {
            var events=UnityEngine.EventSystems.EventSystem.current;if(events==null)return;
            var target=root.GetComponentsInChildren<Button>().FirstOrDefault(button=>button.name==control&&button.interactable);
            if(target!=null)events.SetSelectedGameObject(target.gameObject);
        }
        int[] LookGroupCategories(){return lookGroup==1?(lookDraft.sex==0?new[]{2,3,4,5,6}:new[]{2,3,4,5}):new[]{0,7,8,10,11,12,9};}
        int[] LookEditableCategories(){return lookDraft.sex==0?new[]{0,7,8,10,11,12,9,2,3,4,5,6}:new[]{0,7,8,10,11,12,9,2,3,4,5};}
        void CycleLookOption(int category,int direction,string control)
        {
            lookCategory=category;int count=LookOptions(category).Length;if(count==0)return;
            int next=(LookIndex(category)+direction+count)%count;
            ChangeLook(()=>SetLookOption(category,next));
            FocusLookControl(control);
        }
        void ClassicLookRule(Transform parent,float x,float y,float w,Color? color=null)
        {
            var r=Rect("ClassicRule",parent,x,y,w,1);var image=r.gameObject.AddComponent<Image>();image.color=color??ClassicEdge;image.raycastTarget=false;
        }
        void ClassicLookFrame(Transform parent,float x,float y,float w,float h,bool doubled=false)
        {
            ClassicLookRule(parent,x,y,w);ClassicLookRule(parent,x,y+h-1,w);
            var left=Rect("ClassicRule",parent,x,y,1,h).gameObject.AddComponent<Image>();left.color=ClassicEdge;left.raycastTarget=false;
            var right=Rect("ClassicRule",parent,x+w-1,y,1,h).gameObject.AddComponent<Image>();right.color=ClassicEdge;right.raycastTarget=false;
            if(doubled)ClassicLookFrame(parent,x+4,y+4,w-8,h-8);
        }
        Text ClassicLookLabel(Transform parent,string value,float x,float y,float w,float h,int size=18,Color? color=null,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var label=Label(parent,value,x,y,w,h,size,color??ClassicInk,align);label.color=color??ClassicInk;return label;
        }
        // The portrait editor has its own restrained palette. Keep the same
        // tutorial observer and keyboard focus behavior as normal game buttons.
        Button ClassicLookButton(string name,Transform parent,string value,float x,float y,float w,float h,Action action,bool enabled=true,bool selected=false,int tone=0,int size=18)
        {
            var r=Rect(name,parent,x,y,w,h);SurfaceTheme(r,true);
            var image=r.gameObject.AddComponent<Image>();image.color=Color.white;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.interactable=enabled;
            Color normal=tone==1?ClassicGreen:tone==2?ClassicOchre:selected?Hex("D5DDBF"):ClassicLight;
            Color foreground=tone>0?ClassicLight:ClassicInk;
            var colors=button.colors;colors.normalColor=normal;colors.highlightedColor=tone==1?Hex("506E50"):tone==2?Hex("9B7150"):Hex("E0E4CB");
            colors.pressedColor=tone==1?Hex("2B4431"):tone==2?Hex("6F4933"):Hex("CBD3B6");colors.selectedColor=normal;colors.disabledColor=Hex("DAD6C5");colors.fadeDuration=.08f;button.colors=colors;
            ClassicLookFrame(r,0,0,w,h);
            if(tone>0){ClassicLookRule(r,3,3,w-6,new Color(.94f,.92f,.79f,.5f));ClassicLookRule(r,3,h-4,w-6,new Color(.15f,.20f,.14f,.5f));}
            ClassicLookLabel(r,value,7,0,w-14,h,size,enabled?foreground:Hex("858775"),TextAnchor.MiddleCenter);
            var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=Hex("AA824B");outline.effectDistance=new Vector2(1,-1);outline.enabled=false;r.gameObject.AddComponent<UIFocusOutline>();
            image.CrossFadeColor(enabled?normal:colors.disabledColor,0,true,true);
            button.onClick.AddListener(()=>{Sound();var before=State;string priorModal=modal;int priorPage=commandTab;action();ObserveTutorialAction(name,before,priorModal,priorPage);});
            return button;
        }
        void DrawLookCycleRow(Transform parent,int category,float y)
        {
            var options=LookOptions(category);int option=Mathf.Clamp(LookIndex(category),0,Mathf.Max(0,options.Length-1));bool active=lookCategory==category;
            ClassicLookButton("LookCategory_"+category,parent,LookCategoryName(category),488,y,104,44,()=>SelectLookCategory(category),true,active);
            ClassicLookButton("LookPrevious_"+category,parent,"◀",600,y,44,44,()=>CycleLookOption(category,-1,"LookPrevious_"+category),options.Length>1,false,0,20);
            var current=ClassicLookButton("LookCurrent_"+category,parent,"",650,y,368,44,()=>CycleLookOption(category,1,"LookCurrent_"+category),options.Length>1,active);
            ClassicLookLabel(current.transform,options.Length==0?"暂无":options[option],14,0,263,44,20);
            ClassicLookLabel(current.transform,(option+1)+" / "+options.Length,280,0,74,44,15,ClassicQuiet,TextAnchor.MiddleRight);
            ClassicLookButton("LookNext_"+category,parent,"▶",1024,y,44,44,()=>CycleLookOption(category,1,"LookNext_"+category),options.Length>1,false,0,20);
        }
        void SelectClassicLookGroup(int group)
        {
            lookGroup=group;lookCategory=group==0?0:2;Draw();FocusLookControl("LookGroup_"+group);
        }
        void SetClassicLookSex(int sex)
        {
            ChangeLook(()=>{lookDraft.sex=sex;lookCategory=0;lookGroup=0;});FocusLookControl(sex==0?"LookMale":"LookFemale");
        }
        void DrawAppearance(Transform p)
        {
            lookGroup=lookGroup==1?1:0;
            var categories=LookGroupCategories();if(!categories.Contains(lookCategory))lookCategory=categories[0];
            string who=lookTarget<0?"地区负责人":State.agents.First(x=>x.id==lookTarget).name;
            ClassicLookFrame(p,8,8,1104,824,true);
            ClassicLookLabel(p,"人物绘卷",32,22,304,42,30);
            ClassicLookLabel(p,who+"  /  外观不影响能力",350,29,588,31,16,ClassicQuiet);
            ClassicLookButton("LookClose",p,"关闭",1004,26,84,34,RequestCloseAppearance,true,false,2,16);
            ClassicLookRule(p,28,78,1064);

            var preview=Box("StudioPreview",p,36,102,404,606,ClassicInset);preview.GetComponent<Image>().color=ClassicInset;preview.GetComponent<Image>().raycastTarget=false;preview.gameObject.AddComponent<RectMask2D>();
            DrawRealisticLook(preview,lookCompare?lookOriginal:lookDraft,0,0,404,606,Color.white,lookZoom);
            ClassicLookFrame(p,28,94,420,622,true);
            ClassicLookLabel(p,lookCompare?"原貌对照":"当前形象",36,710,404,28,17,ClassicQuiet,TextAnchor.MiddleCenter);
            ClassicLookButton("LookZoom",p,lookZoom?"查看半身":"查看面容",32,742,202,32,()=>{lookZoom=!lookZoom;Draw();},true,false,0,16);
            ClassicLookButton("LookCompare",p,lookCompare?"返回预览":"对照原貌",242,742,202,32,()=>{lookCompare=!lookCompare;Draw();},true,lookCompare,0,16);

            ClassicLookButton("LookGroup_0",p,"选择容貌",466,96,308,48,()=>SelectClassicLookGroup(0),true,lookGroup==0,lookGroup==0?1:0,24);
            ClassicLookButton("LookGroup_1",p,"选择部件",778,96,310,48,()=>SelectClassicLookGroup(1),true,lookGroup==1,lookGroup==1?1:0,24);
            ClassicLookFrame(p,466,150,622,524,true);
            if(lookGroup==0)
            {
                ClassicLookLabel(p,"性别",502,168,84,36,20);
                ClassicLookButton("LookMale",p,"男性",600,164,230,42,()=>SetClassicLookSex(0),true,lookDraft.sex==0);
                ClassicLookButton("LookFemale",p,"女性",838,164,230,42,()=>SetClassicLookSex(1),true,lookDraft.sex==1);
                for(int i=0;i<categories.Length;i++)DrawLookCycleRow(p,categories[i],218+i*54);
                ClassicLookButton("LookAgeColor",p,"按年龄设色",488,610,156,34,()=>ChangeLook(()=>AppearanceRules.ApplyAgeHairColor(lookDraft)),true,false,0,16);
                ClassicLookLabel(p,"容貌包含发式、耳廓与颈部。",662,608,406,28,15,ClassicQuiet);
                ClassicLookLabel(p,"点击款名或箭头，可逐款翻选。",662,636,406,26,15,ClassicQuiet);
            }
            else
            {
                for(int i=0;i<categories.Length;i++)DrawLookCycleRow(p,categories[i],176+i*68);
                ClassicLookRule(p,488,536,580);
                var heads=LookOptions(0);int head=Mathf.Clamp(LookIndex(0),0,Mathf.Max(0,heads.Length-1));
                ClassicLookLabel(p,"当前容貌  ·  "+(heads.Length==0?"":heads[head]),488,552,580,31,20);
                ClassicLookLabel(p,"点击款名或箭头替换部件；\n左侧可放大面容，查看五官搭配。",488,590,580,60,17,ClassicQuiet);
            }
            ClassicLookButton("LookRandom",p,"随机形象",466,690,184,38,RandomizeLook,true,false,0,17);
            ClassicLookButton("LookPresets",p,"收藏预设",658,690,184,38,()=>{lookPopup="presets";Draw();},true,false,0,17);
            int locked=LookEditableCategories().Count(n=>lookLocks[n]);
            ClassicLookButton("LookRandomSettings",p,"随机设定"+(locked>0?" · 保留 "+locked:""),850,690,238,38,()=>{lookPopup="random";Draw();},true,false,0,17);
            ClassicLookButton("LookUndo",p,"撤销",466,742,86,32,()=>UndoLook(false),lookUndo.Count>0,false,0,16);
            ClassicLookButton("LookRedo",p,"重做",560,742,86,32,()=>UndoLook(true),lookRedo.Count>0,false,0,16);
            ClassicLookButton("LookReset",p,"还原",654,742,86,32,()=>ChangeLook(()=>lookDraft=AppearanceRules.Copy(lookOriginal)),LookDirty(),false,0,16);
            ClassicLookLabel(p,string.IsNullOrEmpty(lookNotice)?"修改只在确认后保存。":lookNotice,756,738,332,39,14,ClassicQuiet);
            ClassicLookRule(p,28,782,1064);
            ClassicLookLabel(p,"点选款名或箭头翻选  /  Esc 返回",32,792,572,28,14,ClassicQuiet);
            ClassicLookButton("LookCancel",p,"返回",704,792,144,32,RequestCloseAppearance,true,false,2,17);
            ClassicLookButton("LookApply",p,lookCompare?"先返回预览":atTitle?"确定 · 开局外观":"确定 · 保存外观",860,790,228,36,CommitAppearance,!lookCompare,false,1,18);
            if(lookPopup!="")DrawLookPopup();
        }
        RectTransform ClassicLookPopup(float x,float y,float w,float h)
        {
            var shade=Box("LookPopupShade",root,0,0,1600,900,new Color(.06f,.09f,.07f,.82f));var p=Box("LookPopup",shade,x,y,w,h,ClassicPaper);p.GetComponent<Image>().color=ClassicPaper;SurfaceTheme(p,true);ClassicLookFrame(p,7,7,w-14,h-14,true);return p;
        }
        void DrawLookPopup()
        {
            if(lookPopup=="random"){DrawLookRandomSettings();return;}
            if(lookPopup=="applyError"||lookPopup=="presetError"){DrawLookSaveError();return;}
            var p=ClassicLookPopup(430,215,740,470);
            ClassicLookLabel(p,lookPopup=="presets"?"绘卷收藏":lookPopup=="overwrite"?"覆盖当前预设？":"放弃尚未应用的外观？",28,24,684,44,27);
            ClassicLookRule(p,28,79,684);
            if(lookPopup=="presets")
            {
                for(int i=0;i<3;i++)
                {
                    int slot=i;bool exists=HasLookSlot(i);
                    var b=ClassicLookButton("LookSlot_"+i,p,"",28+i*232,98,220,180,()=>{lookSlot=slot;Draw();},true,lookSlot==i);
                    if(exists)DrawLook(b.transform,profile.lookSlots[i],10,10,86,132);
                    ClassicLookLabel(b.transform,"预设 "+(i+1)+"\n"+(exists?"已收藏":"空槽"),108,30,104,84,18);
                }
                ClassicLookButton("LookSaveSlot",p,"保存当前预览",28,297,332,42,()=>{if(HasLookSlot(lookSlot)){lookPopup="overwrite";Draw();}else SaveLookSlot();},true,false,1);
                ClassicLookButton("LookLoadSlot",p,"读取到预览",374,297,338,42,()=>{lookPopup="";ChangeLook(()=>{lookDraft=AppearanceRules.AsRealistic(profile.lookSlots[lookSlot]);lookCategory=0;lookGroup=0;});},HasLookSlot(lookSlot));
                ClassicLookLabel(p,lookNotice,28,352,684,40,16,ClassicQuiet);
                ClassicLookButton("LookPopupBack",p,"返回编辑",28,406,684,38,()=>{lookPopup="";Draw();},true,false,2);
            }
            else
            {
                ClassicLookLabel(p,lookPopup=="overwrite"?"将用当前预览替换预设 "+(lookSlot+1)+"。\n已应用到角色的外观不受影响。":"继续编辑可保留当前预览。\n放弃修改后，角色恢复已保存的外观。",28,110,684,176,23);
                ClassicLookButton("LookPopupKeep",p,"返回编辑",28,365,332,52,()=>{lookPopup="";Draw();},true,false,2);
                ClassicLookButton("LookPopupConfirm",p,lookPopup=="overwrite"?"确认覆盖":"放弃修改",374,365,338,52,()=>{if(lookPopup=="overwrite"){lookPopup="presets";SaveLookSlot();}else{lookPopup="";CloseAppearance();}},true,false,1);
            }
        }
        void DrawLookSaveError()
        {
            bool preset=lookPopup=="presetError";
            var p=ClassicLookPopup(430,215,740,470);
            ClassicLookLabel(p,preset?"预设未保存":"外观未保存",28,24,684,44,27);
            ClassicLookRule(p,28,79,684);
            ClassicLookLabel(p,"当前预览已保留，可以重试或返回编辑。",28,91,684,35,18);
            var content=Scroll(p,"LookSaveFailureText",28,136,684,200,200);
            var message=ClassicLookLabel(content,lookSaveError,0,0,664,200,17,ClassicQuiet,TextAnchor.UpperLeft);
            float height=Mathf.Max(200,message.preferredHeight+8);message.rectTransform.sizeDelta=new Vector2(664,height);content.sizeDelta=new Vector2(672,height);
            ClassicLookButton("LookPopupKeep",p,"返回编辑",28,365,332,52,()=>{lookPopup="";Draw();},true,false,2);
            ClassicLookButton("LookPopupRetry",p,"重试保存",374,365,338,52,()=>{lookPopup=preset?"presets":"";if(preset)SaveLookSlot();else CommitAppearance();},true,false,1);
        }
        void DrawLookRandomSettings()
        {
            var p=ClassicLookPopup(400,153,800,594);
            ClassicLookLabel(p,"随机设定",28,24,744,43,29);
            ClassicLookRule(p,28,80,744);
            ClassicLookLabel(p,"选择要保留的部件；逐项翻选仍可修改。",28,93,744,32,18,ClassicQuiet);
            var categories=LookEditableCategories();
            for(int i=0;i<categories.Length;i++)
            {
                int category=categories[i];ClassicLookButton("LookRandomLock_"+category,p,(lookLocks[category]?"保留 · ":"随机 · ")+LookCategoryName(category),28+i%3*252,143+i/3*56,238,44,()=>{lookLocks[category]=!lookLocks[category];Draw();},true,lookLocks[category]);
            }
            ClassicLookButton("LookLockAll",p,"全部保留",28,395,238,39,()=>{foreach(int n in categories)lookLocks[n]=true;Draw();});
            ClassicLookButton("LookUnlockAll",p,"全部随机",280,395,238,39,()=>{foreach(int n in categories)lookLocks[n]=false;Draw();});
            ClassicLookLabel(p,"随机保留当前性别，外观不会改变角色能力。",28,458,744,32,17,ClassicQuiet);
            ClassicLookButton("LookPopupBack",p,"返回编辑",28,526,360,43,()=>{lookPopup="";Draw();},true,false,2);
            ClassicLookButton("LookRandomFromSettings",p,"随机并查看",408,526,364,43,()=>{lookPopup="";RandomizeLook();},true,false,1);
        }
    }
}
