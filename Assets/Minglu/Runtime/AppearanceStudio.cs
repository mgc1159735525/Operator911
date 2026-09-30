using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        Appearance lookOriginal;
        readonly Stack<Appearance> lookUndo=new Stack<Appearance>(),lookRedo=new Stack<Appearance>();
        readonly bool[] lookLocks=new bool[14];
        int lookGroup,lookOptionPage;
        bool lookZoom,lookCompare;
        string lookPopup="",lookReturnModal="";
        bool LookDirty(){return JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(lookOriginal);}
        void ChangeLook(Action change)
        {
            var before=AppearanceRules.Copy(lookDraft);change();lookDraft=AppearanceRules.AsRealistic(lookDraft);lookDraft.enabled=true;if(lookDraft.sex==1)lookDraft.parts[6]=0;
            if(JsonUtility.ToJson(before)!=JsonUtility.ToJson(lookDraft)){lookUndo.Push(before);lookRedo.Clear();}
            lookCompare=false;lookOptionPage=LookIndex(lookCategory)/4;lookNotice="预览已更新，应用后生效。";Draw();
        }
        void UndoLook(bool redo)
        {
            var from=redo?lookRedo:lookUndo;var to=redo?lookUndo:lookRedo;if(from.Count==0)return;
            to.Push(AppearanceRules.Copy(lookDraft));lookDraft=from.Pop();if(lookDraft.sex==1&&lookCategory==6)lookCategory=2;lookOptionPage=LookIndex(lookCategory)/4;lookCompare=false;Draw();
        }
        void RandomizeLook()
        {
            ChangeLook(()=>{var old=lookDraft;var next=AppearanceRules.Randomize(Guid.NewGuid().GetHashCode(),old.sex);for(int n=0;n<13;n++)if(lookLocks[n])next.parts[n]=old.parts[n];if(lookLocks[13])next.ears=old.ears;lookDraft=next;});
        }
        void RequestCloseAppearance()
        {
            if(lookPopup!=""){lookPopup="";Draw();return;}
            if(LookDirty()){lookPopup="discard";Draw();}else CloseAppearance();
        }
        void DrawAppearanceOverlay()
        {
            var shade=Box("AppearanceShade",root,0,0,1600,900,new Color(0,0,0,.85f));
            var p=Box("AppearanceStudio",shade,240,48,1120,804,Paper);SurfaceTheme(p,true);DrawAppearance(p);
        }
        void SelectLookCategory(int category){lookCategory=category;lookOptionPage=LookIndex(category)/4;Draw();}
        void DrawAppearance(Transform p)
        {
            string who=lookTarget<0?"地区负责人":State.agents.First(x=>x.id==lookTarget).name;
            Label(p,"人物绘卷",30,21,280,43,30,Gold);Label(p,who,247,30,245,30,19,Muted);
            Button("LookMale",p,"男性",770,26,91,38,()=>ChangeLook(()=>{lookDraft.sex=0;lookCategory=0;lookGroup=0;}),true,lookDraft.sex==0);
            Button("LookFemale",p,"女性",870,26,91,38,()=>ChangeLook(()=>{lookDraft.sex=1;lookCategory=0;lookGroup=0;}),true,lookDraft.sex==1);
            Button("LookClose",p,"关闭",996,26,94,38,RequestCloseAppearance);Rule(p,30,81,1060);
            var preview=Box("StudioPreview",p,30,107,350,482,PaperInset);
            DrawRealisticLook(preview,lookCompare?lookOriginal:lookDraft,0,0,350,482,Color.white,lookZoom);
            Label(p,lookCompare?"原貌对照":"绘制立绘 · 当前预览",30,601,350,28,18,Gold,TextAnchor.MiddleCenter);
            Button("LookZoom",p,lookZoom?"查看半身":"查看面容",30,645,169,40,()=>{lookZoom=!lookZoom;Draw();});
            Button("LookCompare",p,lookCompare?"返回预览":"对照原貌",211,645,169,40,()=>{lookCompare=!lookCompare;Draw();});
            Button("LookUndo",p,"撤销",30,696,109,36,()=>UndoLook(false),lookUndo.Count>0);
            Button("LookRedo",p,"重做",150,696,109,36,()=>UndoLook(true),lookRedo.Count>0);
            Button("LookReset",p,"还原",270,696,110,36,()=>ChangeLook(()=>lookDraft=AppearanceRules.Copy(lookOriginal)),LookDirty());
            string[] groups={"形貌","五官","设色"};
            for(int i=0;i<3;i++){int g=i;Button("LookGroup_"+i,p,groups[i],420+i*227,107,215,42,()=>{lookGroup=g;SelectLookCategory(g==0?0:g==1?2:10);},true,lookGroup==i);}
            int[] categories=lookGroup==0?new[]{0,1,7}:lookGroup==1?(lookDraft.sex==0?new[]{2,3,4,5,13,6}:new[]{2,3,4,5,13}):new[]{10,12,11,9};
            for(int i=0;i<categories.Length;i++){int category=categories[i];Button("LookCategory_"+category,p,LookCategoryName(category)+(lookLocks[category]?" · 锁":""),420+i%3*227,168+i/3*43,215,35,()=>SelectLookCategory(category),true,lookCategory==category);}
            var options=LookOptions(lookCategory);
            Label(p,LookCategoryName(lookCategory)+" / "+options[LookIndex(lookCategory)],420,270,442,32,21,Gold);
            Button("LookLock",p,lookLocks[lookCategory]?"已锁定此项":"锁定此项",905,265,181,36,()=>{lookLocks[lookCategory]=!lookLocks[lookCategory];Draw();},true,lookLocks[lookCategory]);
            int pages=(options.Length+3)/4;lookOptionPage=Mathf.Clamp(lookOptionPage,0,pages-1);
            for(int cell=0;cell<4;cell++)
            {
                int option=lookOptionPage*4+cell;if(option>=options.Length)break;
                var b=Button("LookOption_"+option,p,"",420+cell%2*341,318+cell/2*137,325,125,()=>ChangeLook(()=>SetLookOption(lookCategory,option)),true,LookIndex(lookCategory)==option);
                var sample=AppearanceRules.Copy(lookDraft);if(lookCategory==13)sample.ears=option;else sample.parts[lookCategory]=option;
                DrawRealisticLook(b.transform,sample,7,7,79,111,Color.white,lookCategory!=7);
                Label(b.transform,options[option],103,42,207,36,20,LookIndex(lookCategory)==option?Red:TextColor);
            }
            if(pages>1)
            {
                Button("LookOptionPrevious",p,"上一页",420,594,110,36,()=>{lookOptionPage--;Draw();},lookOptionPage>0);
                Label(p,(lookOptionPage+1)+" / "+pages+"    共 "+options.Length+" 款",544,597,418,30,17,Muted,TextAnchor.MiddleCenter);
                Button("LookOptionNext",p,"下一页",976,594,110,36,()=>{lookOptionPage++;Draw();},lookOptionPage<pages-1);
            }
            Button("LookRandom",p,"随机形象",420,645,215,40,RandomizeLook);
            Button("LookPresets",p,"收藏预设",647,645,215,40,()=>{lookPopup="presets";Draw();});
            int locked=LookEditableCategories().Count(n=>lookLocks[n]);
            Button("LookRandomSettings",p,"随机设定"+(locked>0?" · 锁定 "+locked:""),874,645,212,40,()=>{lookPopup="random";Draw();});
            if(!string.IsNullOrEmpty(lookNotice))Label(p,lookNotice,420,699,666,28,16,Muted);
            Rule(p,30,743,1060);
            Button("LookCancel",p,"取消",420,756,215,38,RequestCloseAppearance);
            Button("LookApply",p,lookCompare?"先返回预览":atTitle?"保存为开局外观":"应用并保存",647,756,439,38,CommitAppearance,!lookCompare,true);
            if(lookPopup!="")DrawLookPopup();
        }
        void DrawLookPopup()
        {
            if(lookPopup=="random"){DrawLookRandomSettings();return;}
            var shade=Box("LookPopupShade",root,0,0,1600,900,new Color(0,0,0,.82f));var p=Box("LookPopup",shade,430,215,740,470,Paper);SurfaceTheme(p,true);
            Label(p,lookPopup=="presets"?"绘卷收藏":lookPopup=="overwrite"?"覆盖当前预设？":"放弃尚未应用的外观？",26,25,685,44,27,Gold);
            if(lookPopup=="presets")
            {
                for(int i=0;i<3;i++){int slot=i;bool exists=profile.lookSlots!=null&&profile.lookSlots.Length>i&&profile.lookSlots[i]!=null;var b=Button("LookSlot_"+i,p,"",26+i*234,92,220,180,()=>{lookSlot=slot;Draw();},true,lookSlot==i);if(exists)DrawLook(b.transform,profile.lookSlots[i],10,10,86,132);Label(b.transform,"预设 "+(i+1)+"\n"+(exists?"已收藏":"空槽"),109,32,100,80,18,lookSlot==i?Red:TextColor);}
                Button("LookSaveSlot",p,"保存当前预览",26,297,329,46,()=>{if(profile.lookSlots!=null&&profile.lookSlots.Length>lookSlot&&profile.lookSlots[lookSlot]!=null){lookPopup="overwrite";Draw();}else SaveLookSlot();});
                Button("LookLoadSlot",p,"读取到预览",373,297,341,46,()=>{lookPopup="";ChangeLook(()=>{lookDraft=AppearanceRules.AsRealistic(profile.lookSlots[lookSlot]);lookCategory=0;lookGroup=0;});},profile.lookSlots!=null&&profile.lookSlots.Length>lookSlot&&profile.lookSlots[lookSlot]!=null);
                Label(p,lookNotice,26,356,688,35,16,Muted);Button("LookPopupBack",p,"返回编辑",26,405,688,40,()=>{lookPopup="";Draw();});
            }
            else
            {
                Label(p,lookPopup=="overwrite"?"将用当前预览替换预设 "+(lookSlot+1)+"。已应用到角色的外观不受影响。":"已应用的角色外观保持不变。继续编辑可以保留这次预览。",26,113,688,150,23);
                Button("LookPopupKeep",p,"返回编辑",26,364,329,52,()=>{lookPopup="";Draw();});
                Button("LookPopupConfirm",p,lookPopup=="overwrite"?"确认覆盖":"放弃修改",373,364,341,52,()=>{if(lookPopup=="overwrite"){lookPopup="presets";SaveLookSlot();}else{lookPopup="";CloseAppearance();}});
            }
        }
        int[] LookEditableCategories(){return lookDraft.sex==0?new[]{0,1,7,2,3,4,5,13,6,10,12,11,9}:new[]{0,1,7,2,3,4,5,13,10,12,11,9};}
        void DrawLookRandomSettings()
        {
            var shade=Box("LookPopupShade",root,0,0,1600,900,new Color(0,0,0,.82f));var p=Box("LookPopup",shade,400,153,800,594,Paper);SurfaceTheme(p,true);
            Label(p,"随机设定",28,23,744,43,29,Gold);
            Label(p,"锁定的部件会在随机时保留；逐项点选仍可修改。",28,83,744,32,18,Muted);
            var categories=LookEditableCategories();
            for(int i=0;i<categories.Length;i++)
            {
                int category=categories[i];Button("LookRandomLock_"+category,p,(lookLocks[category]?"保留 · ":"随机 · ")+LookCategoryName(category),28+i%3*252,137+i/3*51,238,42,()=>{lookLocks[category]=!lookLocks[category];Draw();},true,lookLocks[category]);
            }
            Button("LookLockAll",p,"全部保留",28,416,238,39,()=>{foreach(int n in categories)lookLocks[n]=true;Draw();});
            Button("LookUnlockAll",p,"全部随机",280,416,238,39,()=>{foreach(int n in categories)lookLocks[n]=false;Draw();});
            Label(p,"随机保留当前性别，外观不会改变角色能力。",28,475,744,28,17,Muted);
            Button("LookPopupBack",p,"返回编辑",28,527,360,43,()=>{lookPopup="";Draw();});
            Button("LookRandomFromSettings",p,"随机并查看",408,527,364,43,()=>{lookPopup="";RandomizeLook();});
        }
    }
}
