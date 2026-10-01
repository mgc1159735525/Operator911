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
            bool changed=JsonUtility.ToJson(before)!=JsonUtility.ToJson(lookDraft);
            if(changed){lookUndo.Push(before);lookRedo.Clear();}
            lookCompare=false;lookOptionPage=LookIndex(lookCategory)/4;lookNotice=changed?"预览已更新，应用后生效。":"外观未改变。保留的部件不会参与随机。";Draw();
        }
        void UndoLook(bool redo)
        {
            var from=redo?lookRedo:lookUndo;var to=redo?lookUndo:lookRedo;if(from.Count==0)return;
            to.Push(AppearanceRules.Copy(lookDraft));lookDraft=from.Pop();if(lookDraft.sex==1&&lookCategory==6)lookCategory=2;lookOptionPage=LookIndex(lookCategory)/4;lookCompare=false;lookNotice=redo?"已重做，应用后生效。":"已撤销，应用后生效。";Draw();
        }
        void RandomizeLook()
        {
            if(LookEditableCategories().All(category=>lookLocks[category])){lookNotice="已保留全部部件，外观未改变。";Draw();return;}
            ChangeLook(()=>{var old=lookDraft;var next=AppearanceRules.Randomize(Guid.NewGuid().GetHashCode(),old.sex);for(int n=0;n<13;n++)if(n==8||lookLocks[n])next.parts[n]=old.parts[n];if(lookLocks[13])next.ears=old.ears;lookDraft=next;});
        }
        void RequestCloseAppearance()
        {
            if(lookPopup!=""){lookPopup="";Draw();return;}
            if(LookDirty()){lookPopup="discard";Draw();}else CloseAppearance();
        }
        void DrawAppearanceOverlay()
        {
            var shade=Box("AppearanceShade",root,0,0,1600,900,new Color(0,0,0,.85f));
            var p=Box("AppearanceStudio",shade,240,30,1120,840,Paper);SurfaceTheme(p,true);DrawAppearance(p);
        }
        void SelectLookCategory(int category){lookCategory=category;lookOptionPage=LookIndex(category)/4;Draw();}
        void DrawAppearance(Transform p)
        {
            string who=lookTarget<0?"地区负责人":State.agents.First(x=>x.id==lookTarget).name;
            Label(p,"人物绘卷",32,24,360,38,27,Gold);
            Label(p,who+"  /  外观不影响能力",32,68,440,26,15,Muted);
            Button("LookMale",p,"男性",836,30,82,34,()=>ChangeLook(()=>{lookDraft.sex=0;lookCategory=0;lookGroup=0;}),true,lookDraft.sex==0);
            Button("LookFemale",p,"女性",926,30,82,34,()=>ChangeLook(()=>{lookDraft.sex=1;lookCategory=0;lookGroup=0;}),true,lookDraft.sex==1);
            Button("LookClose",p,"关闭",1016,30,72,34,RequestCloseAppearance);
            var preview=Box("StudioPreview",p,32,112,344,516,PaperInset);
            DrawRealisticLook(preview,lookCompare?lookOriginal:lookDraft,0,0,344,516,Color.white,lookZoom);
            Label(p,lookCompare?"原貌对照":"当前形象",32,638,344,24,15,Muted,TextAnchor.MiddleCenter);
            Button("LookZoom",p,lookZoom?"查看半身":"查看面容",32,674,164,36,()=>{lookZoom=!lookZoom;Draw();});
            Button("LookCompare",p,lookCompare?"返回预览":"对照原貌",212,674,164,36,()=>{lookCompare=!lookCompare;Draw();});
            Button("LookUndo",p,"撤销",32,718,104,32,()=>UndoLook(false),lookUndo.Count>0);
            Button("LookRedo",p,"重做",152,718,104,32,()=>UndoLook(true),lookRedo.Count>0);
            Button("LookReset",p,"还原",272,718,104,32,()=>ChangeLook(()=>lookDraft=AppearanceRules.Copy(lookOriginal)),LookDirty());
            string[] groups={"形貌","五官","设色"};
            for(int i=0;i<3;i++){int g=i;Button("LookGroup_"+i,p,groups[i],424+i*226,112,212,40,()=>{lookGroup=g;SelectLookCategory(g==0?0:g==1?2:10);},true,lookGroup==i);}
            int[] categories=lookGroup==0?new[]{0,1,7}:lookGroup==1?(lookDraft.sex==0?new[]{2,3,4,5,13,6}:new[]{2,3,4,5,13}):new[]{10,12,11,9};
            for(int i=0;i<categories.Length;i++){int category=categories[i];Button("LookCategory_"+category,p,LookCategoryName(category)+(lookLocks[category]?" · 锁":""),424+i%3*226,176+i/3*44,212,34,()=>SelectLookCategory(category),true,lookCategory==category);}
            var options=LookOptions(lookCategory);
            Label(p,LookCategoryName(lookCategory)+" / "+options[LookIndex(lookCategory)],424,278,466,32,21,Gold);
            Button("LookLock",p,lookLocks[lookCategory]?"已锁定":"随机时保留",920,276,168,36,()=>{lookLocks[lookCategory]=!lookLocks[lookCategory];Draw();},true,lookLocks[lookCategory]);
            int pages=(options.Length+3)/4;lookOptionPage=Mathf.Clamp(lookOptionPage,0,pages-1);
            for(int cell=0;cell<4;cell++)
            {
                int option=lookOptionPage*4+cell;if(option>=options.Length)break;
                var b=Button("LookOption_"+option,p,"",424+cell%2*342,332+cell/2*136,322,120,()=>ChangeLook(()=>SetLookOption(lookCategory,option)),true,LookIndex(lookCategory)==option);
                var sample=AppearanceRules.Copy(lookDraft);if(lookCategory==13)sample.ears=option;else sample.parts[lookCategory]=option;
                bool feature=(lookCategory>=2&&lookCategory<=6)||lookCategory==13;
                DrawRealisticLook(b.transform,sample,12,12,64,96,Color.white,feature);
                Label(b.transform,options[option],96,35,210,32,18,LookIndex(lookCategory)==option?Red:TextColor);
                if(LookIndex(lookCategory)==option)Label(b.transform,"当前选择",96,76,210,23,13,Muted);
            }
            if(pages>1)
            {
                Button("LookOptionPrevious",p,"上一页",424,606,110,32,()=>{lookOptionPage--;Draw();},lookOptionPage>0);
                Label(p,(lookOptionPage+1)+" / "+pages+"  ·  "+options.Length+" 款",546,608,410,28,14,Muted,TextAnchor.MiddleCenter);
                Button("LookOptionNext",p,"下一页",978,606,110,32,()=>{lookOptionPage++;Draw();},lookOptionPage<pages-1);
            }
            Rule(p,424,658,664);
            Button("LookRandom",p,"随机形象",424,674,208,36,RandomizeLook);
            Button("LookPresets",p,"收藏预设",648,674,208,36,()=>{lookPopup="presets";Draw();});
            int locked=LookEditableCategories().Count(n=>lookLocks[n]);
            Button("LookRandomSettings",p,"随机设定"+(locked>0?" · 保留 "+locked:""),872,674,216,36,()=>{lookPopup="random";Draw();});
            Label(p,string.IsNullOrEmpty(lookNotice)?"点选右侧部件，在左侧查看完整搭配。":lookNotice,424,722,664,24,14,Muted);
            Rule(p,32,768,1056);
            Button("LookCancel",p,"取消",704,780,112,44,RequestCloseAppearance);
            Button("LookApply",p,lookCompare?"先返回预览":atTitle?"保存为开局外观":"应用并保存",840,780,248,44,CommitAppearance,!lookCompare,true);
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
