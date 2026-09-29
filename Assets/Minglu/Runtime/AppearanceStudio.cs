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
        int lookGroup;
        bool lookZoom,lookCompare;
        string lookPopup="",lookReturnModal="";
        bool LookDirty(){return JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(lookOriginal);}
        void ChangeLook(Action change)
        {
            var before=AppearanceRules.Copy(lookDraft);change();lookDraft=AppearanceRules.AsRealistic(lookDraft);lookDraft.enabled=true;if(lookDraft.sex==1)lookDraft.parts[6]=0;
            if(JsonUtility.ToJson(before)!=JsonUtility.ToJson(lookDraft)){lookUndo.Push(before);lookRedo.Clear();}
            lookCompare=false;lookNotice="预览已更新，应用后才修改角色。";Draw();
        }
        void UndoLook(bool redo)
        {
            var from=redo?lookRedo:lookUndo;var to=redo?lookUndo:lookRedo;if(from.Count==0)return;
            to.Push(AppearanceRules.Copy(lookDraft));lookDraft=from.Pop();if(lookDraft.sex==1&&lookCategory==6)lookCategory=2;lookCompare=false;Draw();
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
            var p=Box("AppearanceStudio",shade,260,60,1080,780,Panel);DrawAppearance(p);
        }
        void DrawAppearance(Transform p)
        {
            string who=lookTarget<0?"地区负责人":State.agents.First(x=>x.id==lookTarget).name;
            Label(p,"外观工作室 / "+who,26,22,840,45,29,Gold);
            Button("LookClose",p,"关闭",958,25,96,38,RequestCloseAppearance);
            var preview=Box("StudioPreview",p,26,91,328,470,Ink);
            DrawRealisticLook(preview,lookCompare?lookOriginal:lookDraft,0,0,328,470,Color.white,lookZoom);
            Label(p,lookCompare?"打开时的外观":"当前预览 · 尚未应用",26,567,328,25,17,Gold,TextAnchor.MiddleCenter);
            Button("LookZoom",p,lookZoom?"查看半身":"放大头部",26,606,155,38,()=>{lookZoom=!lookZoom;Draw();});
            Button("LookCompare",p,lookCompare?"返回预览":"对比原貌",193,606,161,38,()=>{lookCompare=!lookCompare;Draw();});
            Button("LookUndo",p,"撤销",26,655,100,36,()=>UndoLook(false),lookUndo.Count>0);
            Button("LookRedo",p,"重做",138,655,100,36,()=>UndoLook(true),lookRedo.Count>0);
            Button("LookReset",p,"还原",250,655,104,36,()=>ChangeLook(()=>lookDraft=AppearanceRules.Copy(lookOriginal)),LookDirty());
            string[] groups={"形象","五官","配色"};
            for(int i=0;i<3;i++){int g=i;Button("LookGroup_"+i,p,groups[i],384+i*223,92,211,40,()=>{lookGroup=g;lookCategory=g==0?0:g==1?2:10;Draw();},true,lookGroup==i);}
            int[] categories=lookGroup==0?new[]{0,1,7}:lookGroup==1?(lookDraft.sex==0?new[]{2,3,4,5,13,6}:new[]{2,3,4,5,13}):new[]{10,12,11,9};
            for(int i=0;i<categories.Length;i++){int category=categories[i];Button("LookCategory_"+category,p,LookCategoryName(category)+(lookLocks[category]?" · 锁":""),384+i%4*168,148+i/4*39,157,32,()=>{lookCategory=category;Draw();},true,lookCategory==category);}
            Button("LookMale",p,"男性",384,236,100,34,()=>ChangeLook(()=>{lookDraft.sex=0;lookCategory=0;lookGroup=0;}),true,lookDraft.sex==0);
            Button("LookFemale",p,"女性",496,236,100,34,()=>ChangeLook(()=>{lookDraft.sex=1;lookCategory=0;lookGroup=0;}),true,lookDraft.sex==1);
            Label(p,LookCategoryName(lookCategory)+" / "+LookOptions(lookCategory)[LookIndex(lookCategory)],612,238,435,30,19,Gold);
            var options=LookOptions(lookCategory);
            for(int i=0;i<options.Length;i++)
            {
                int option=i;var b=Button("LookOption_"+i,p,"",384+i%4*168,286+i/4*120,157,110,()=>ChangeLook(()=>SetLookOption(lookCategory,option)),true,LookIndex(lookCategory)==i);
                var sample=AppearanceRules.Copy(lookDraft);if(lookCategory==13)sample.ears=i;else sample.parts[lookCategory]=i;
                DrawRealisticLook(b.transform,sample,6,7,64,91,Color.white,lookCategory!=7);
                Label(b.transform,options[i],78,19,72,60,15,LookIndex(lookCategory)==i?Ink:TextColor);
            }
            Button("LookLock",p,lookLocks[lookCategory]?"已锁定此项 · 随机时保留":"锁定此项 · 随机时保留",384,541,325,40,()=>{lookLocks[lookCategory]=!lookLocks[lookCategory];Draw();},true,lookLocks[lookCategory]);
            Button("LookRandom",p,"随机未锁定部件",723,541,322,40,RandomizeLook);
            Button("LookPresets",p,"预设管理 / 保存与读取",384,596,325,40,()=>{lookPopup="presets";Draw();});
            Label(p,"性别不影响能力；随机保留当前性别。",724,599,324,44,16,Muted);
            Label(p,string.IsNullOrEmpty(lookNotice)?"先选分类，再点缩略图。锁定仅约束随机组合。":lookNotice,384,650,660,48,16,Muted);
            Button("LookCancel",p,"取消",384,716,211,44,RequestCloseAppearance);
            Button("LookApply",p,lookCompare?"先返回预览再应用":atTitle?"保存为开局外观":"应用并保存",607,716,438,44,CommitAppearance,!lookCompare,true);
            if(lookPopup!="")DrawLookPopup();
        }
        void DrawLookPopup()
        {
            var shade=Box("LookPopupShade",root,0,0,1600,900,new Color(0,0,0,.82f));var p=Box("LookPopup",shade,430,215,740,470,Panel);
            Label(p,lookPopup=="presets"?"外观预设":lookPopup=="overwrite"?"覆盖当前预设？":"放弃尚未应用的外观？",26,25,685,44,27,Gold);
            if(lookPopup=="presets")
            {
                for(int i=0;i<3;i++){int slot=i;bool exists=profile.lookSlots!=null&&profile.lookSlots.Length>i&&profile.lookSlots[i]!=null;var b=Button("LookSlot_"+i,p,"",26+i*234,92,220,180,()=>{lookSlot=slot;Draw();},true,lookSlot==i);if(exists)DrawLook(b.transform,profile.lookSlots[i],10,10,86,132);Label(b.transform,"预设 "+(i+1)+"\n"+(exists?"已保存":"空槽"),109,32,100,80,18,lookSlot==i?Ink:TextColor);}
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
    }
}
