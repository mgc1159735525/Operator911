using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        int LookIndex(int n){return lookDraft.parts[n];}
        void SetLookOption(int n,int option)
        {
            lookDraft=AppearanceRules.AsRealistic(lookDraft);
            if(n==0)AppearanceRules.SelectFace(lookDraft,option,lookLocks[12]);
            else lookDraft.parts[n]=option;
        }
        string LookCategoryName(int n){return AppearanceRules.Labels[n];}
        string[] LookOptions(int n)
        {
            if(n==0)return AppearanceRules.HeadNames(lookDraft.sex);
            if(n>=2&&n<=5)return AppearancePartCatalog.Names(n);
            return AppearanceRules.Names[n];
        }
        void DrawRealisticLook(Transform parent,Appearance look,float x,float y,float w,float h,Color tint,bool headOnly=false)
        { DrawClassicLook(parent,look,x,y,w,h,tint,headOnly); }
    }
}
