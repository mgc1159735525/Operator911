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
        void DrawLook(Transform parent,Appearance look,float x,float y,float w,float h,Color? tint=null)
        {
            DrawRealisticLook(parent,AppearanceRules.AsRealistic(look),x,y,w,h,tint??Color.white);
        }
        void OpenAppearance(int target)
        {
            lookReturnModal=modal;lookTarget=target;lookCategory=0;lookNotice="";lookGroup=0;lookPopup="";lookZoom=false;lookCompare=false;lookUndo.Clear();lookRedo.Clear();Array.Clear(lookLocks,0,lookLocks.Length);
            lookDraft=AppearanceRules.AsRealistic(target<0?(atTitle?profile.chiefLook:State.chiefLook):State.agents.First(x=>x.id==target).appearance);
            if(!lookDraft.enabled){lookDraft.style=1;lookDraft.sex=target==1?1:0;}
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
                    var next=UnityEngine.JsonUtility.FromJson<PlayerProfile>(UnityEngine.JsonUtility.ToJson(profile));next.chiefLook=AppearanceRules.Copy(lookDraft);RunLifecycle.WriteProfile(next,ProfilePath);profile=next;
                }
                else
                {
                    var next=UnityEngine.JsonUtility.FromJson<RunState>(UnityEngine.JsonUtility.ToJson(State));
                    if(lookTarget<0)next.chiefLook=AppearanceRules.Copy(lookDraft);else next.agents.First(x=>x.id==lookTarget).appearance=AppearanceRules.Copy(lookDraft);
                    PrototypeSave.Write(next,SavePath);State=next;
                }
                toast="外观已应用并保存。";CloseAppearance();
            }
            catch(Exception e){lookNotice="未保存："+e.Message;Draw();}
        }
        void SaveLookSlot()
        {
            try
            {
                var next=UnityEngine.JsonUtility.FromJson<PlayerProfile>(UnityEngine.JsonUtility.ToJson(profile));
                if(next.lookSlots==null||next.lookSlots.Length!=3)next.lookSlots=new Appearance[3];
                next.lookSlots[lookSlot]=AppearanceRules.Copy(lookDraft);RunLifecycle.WriteProfile(next,ProfilePath);profile=next;lookNotice="已保存到预设 "+(lookSlot+1)+"，可跨局复用。";
            }
            catch(Exception e){lookNotice="预设未保存："+e.Message;}Draw();
        }
    }
}
