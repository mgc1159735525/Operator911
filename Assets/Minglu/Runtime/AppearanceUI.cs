using System;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        Appearance lookDraft;
        int lookTarget=-1,lookCategory,lookSlot;
        string lookNotice="";
        static bool LookSlotHasContent(Appearance look){return !RunLifecycle.IsEmptyLookSlot(look);}
        bool HasLookSlot(int slot){return profile.lookSlots!=null&&slot>=0&&slot<profile.lookSlots.Length&&LookSlotHasContent(profile.lookSlots[slot]);}
        void DrawLook(Transform parent,Appearance look,float x,float y,float w,float h,Color? tint=null)
        {
            DrawRealisticLook(parent,AppearanceRules.AsRealistic(look),x,y,w,h,tint??Color.white);
        }
        Appearance EffectiveMemberLook(Agent member)
        {
            int knownSex=-1;
            if(State!=null&&State.command!=null&&State.command.people!=null)
            {
                var person=State.command.people.FirstOrDefault(p=>p.id==member.id);
                if(person!=null&&!RunLifecycle.IsEmptyLookSlot(person.originalLook)&&person.originalLook.sex>=0&&person.originalLook.sex<=1)knownSex=person.originalLook.sex;
            }
            return AppearanceRules.EffectiveMemberLook(member,State!=null&&State.initialSeed!=0?State.initialSeed:911u,knownSex);
        }
        void OpenAppearance(int target)
        {
            lookReturnModal=modal;lookTarget=target;lookCategory=0;lookNotice="";lookGroup=0;lookPopup="";lookSaveError="";lookZoom=false;lookCompare=false;lookUndo.Clear();lookRedo.Clear();Array.Clear(lookLocks,0,lookLocks.Length);
            lookDraft=target<0?AppearanceRules.AsRealistic(atTitle?profile.chiefLook:State.chiefLook):EffectiveMemberLook(State.agents.First(x=>x.id==target));
            lookOriginal=AppearanceRules.Copy(lookDraft);
            if(atTitle)homePage="appearance";else modal="appearance";Draw();
        }
        void CloseAppearance(){if(atTitle)homePage="main";else modal=State.command!=null?(lookReturnModal=="memberDetails"||lookReturnModal=="commandOverview"?lookReturnModal:""):lookTarget<0?"":"agent";Draw();}
        void CommitAppearance()
        {
            try
            {
                AppearanceRules.Validate(lookDraft);
                if(atTitle)
                {
                    var next=RunLifecycle.CopyProfile(profile);next.chiefLook=AppearanceRules.Copy(lookDraft);RunLifecycle.WriteProfile(next,ProfilePath);profile=next;
                }
                else
                {
                    var next=UnityEngine.JsonUtility.FromJson<RunState>(UnityEngine.JsonUtility.ToJson(State));
                    if(lookTarget<0)next.chiefLook=AppearanceRules.Copy(lookDraft);else next.agents.First(x=>x.id==lookTarget).appearance=AppearanceRules.Copy(lookDraft);
                    PrototypeSave.Write(next,SavePath);State=next;
                }
                toast="外观已应用并保存。";CloseAppearance();
            }
            catch(Exception e){lookNotice="未保存：当前预览已保留。";lookSaveError=e.Message;lookPopup="applyError";Draw();}
        }
        void SaveLookSlot()
        {
            try
            {
                var next=RunLifecycle.CopyProfile(profile);
                if(next.lookSlots==null||next.lookSlots.Length!=3)next.lookSlots=new Appearance[3];
                next.lookSlots[lookSlot]=AppearanceRules.Copy(lookDraft);RunLifecycle.WriteProfile(next,ProfilePath);profile=next;lookSaveError="";lookNotice="已保存到预设 "+(lookSlot+1)+"，可跨局复用。";
            }
            catch(Exception e){lookNotice="预设未保存：当前预览已保留。";lookSaveError=e.Message;lookPopup="presetError";}Draw();
        }
    }
}
