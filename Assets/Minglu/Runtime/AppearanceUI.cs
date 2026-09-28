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
            lookTarget=target;lookCategory=0;lookNotice="";
            lookDraft=AppearanceRules.AsRealistic(target<0?(atTitle?profile.chiefLook:State.chiefLook):State.agents.First(x=>x.id==target).appearance);
            if(!lookDraft.enabled){lookDraft.style=1;lookDraft.sex=target==1?1:0;}
            if(atTitle)homePage="appearance";else modal="appearance";Draw();
        }
        void CloseAppearance(){if(atTitle)homePage="main";else modal=State.command!=null?"":lookTarget<0?"":"agent";Draw();}
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
        void DrawAppearance(Transform p)
        {
            string who=lookTarget<0?"地区负责人":State.agents.First(x=>x.id==lookTarget).name;
            Label(p,"人物外观 / "+who,26,20,690,42,28,Gold);
            Label(p,"写实外观",28,70,160,25,19,Gold);
            Button("LookMale",p,"男性",206,66,108,32,()=>{lookDraft.sex=0;lookDraft.enabled=true;lookDraft.style=1;lookCategory=0;Draw();},true,lookDraft.sex==0);
            Button("LookFemale",p,"女性",326,66,108,32,()=>{lookDraft.sex=1;lookDraft.enabled=true;lookDraft.style=1;lookCategory=0;Draw();},true,lookDraft.sex==1);
            Label(p,"性别不影响能力；预设与存档保留选择",454,70,410,25,15,Muted);
            var frame=Box("PortraitPreview",p,26,110,245,337,Ink);
            if(lookDraft.enabled||lookTarget<0)DrawLook(frame,lookDraft,0,0,245,337);
            else {var original=UnityEngine.JsonUtility.FromJson<Agent>(UnityEngine.JsonUtility.ToJson(State.agents.First(x=>x.id==lookTarget)));original.appearance=null;Portrait(frame,original,0,0,245,337);}
            Label(p,lookDraft.enabled?(lookDraft.style==1?"写实立绘 · 局部塑形":"经典组合头像"):lookTarget<0?"默认负责人头像":"原始角色立绘",26,454,245,29,18,Gold,TextAnchor.MiddleCenter);
            var categories=RealisticCategories.Where(n=>n!=6||lookDraft.sex==0).ToArray();
            for(int i=0;i<categories.Length;i++)
            {
                int category=categories[i];
                Button("LookCategory_"+category,p,LookCategoryName(category),293+(i%4)*141,110+(i/4)*42,132,34,()=>{lookCategory=category;Draw();},true,lookCategory==category);
            }
            Label(p,"选择"+LookCategoryName(lookCategory)+" / "+LookOptions(lookCategory)[LookIndex(lookCategory)],295,278,560,30,21,Gold);
            for(int i=0;i<LookOptions(lookCategory).Length;i++)
            {
                int option=i;
                Button("LookOption_"+i,p,LookOptions(lookCategory)[i],293+(i%3)*188,316+(i/3)*40,178,35,()=>{SetLookOption(lookCategory,option);lookDraft.enabled=true;lookNotice="";Draw();},true,lookDraft.enabled&&LookIndex(lookCategory)==i);
            }
            Button("LookRandom",p,"随机组合",26,494,117,37,()=>{lookDraft=AppearanceRules.Randomize(Guid.NewGuid().GetHashCode(),lookDraft.sex);lookCategory=0;Draw();});
            Button("LookReset",p,"恢复原貌",152,494,119,37,()=>{lookDraft=State.command!=null&&lookTarget>=0?AppearanceRules.Copy(CommandRules.Person(State,lookTarget).originalLook):new Appearance{style=1,sex=lookDraft.sex};lookCategory=0;Draw();});
            Button("LookSlot",p,"预设 "+(lookSlot+1)+" / 3",293,443,174,37,()=>{lookSlot=(lookSlot+1)%3;lookNotice="";Draw();});
            Button("LookSaveSlot",p,"保存到预设",481,443,174,37,SaveLookSlot);
            Button("LookLoadSlot",p,"读取预设",669,443,178,37,()=>{lookDraft=AppearanceRules.AsRealistic(profile.lookSlots[lookSlot]);lookCategory=0;lookNotice="已载入预览，点击应用才会更改角色。";Draw();},profile.lookSlots!=null&&profile.lookSlots.Length==3&&profile.lookSlots[lookSlot]!=null);
            Label(p,lookNotice==""?(atTitle?"主菜单设置用于下一局负责人；队员可在人物档案内编辑。":"保存预设可跨局复用；应用只修改当前人物，并保存当前对局。点击取消放弃预览。 "):lookNotice,293,496,555,52,16,Muted);
            Button("LookCancel",p,"取消",293,560,174,40,CloseAppearance);
            Button("LookApply",p,atTitle?"保存为开局外观":"应用并保存",481,560,366,40,CommitAppearance,true,true);
        }
    }
}

