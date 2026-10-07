using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {

        int LookIndex(int n){return n==13?(lookDraft.featureEditing?lookDraft.ears:lookDraft.parts[0]%4):lookDraft.style==1&&n>=2&&n<=5&&!lookDraft.featureEditing?lookDraft.parts[0]%4:lookDraft.parts[n];}
        void SetLookOption(int n,int option)
        {
            lookDraft=AppearanceRules.AsRealistic(lookDraft);
            if(n==13)lookDraft.ears=option;
            else if(n==0)AppearanceRules.SelectFace(lookDraft,option,lookLocks[12]);
            else lookDraft.parts[n]=option;
        }
        string LookCategoryName(int n){return n==13?"耳朵":lookDraft.style==1&&n==0?"基础面容":AppearanceRules.Labels[n];}
        string[] LookOptions(int n)
        {
            if(lookDraft.style==1&&n==0)return lookDraft.sex==0?new[]{"清俊青年","端正壮年","坚毅中年","沉稳长者","书生","匠人","旅者","长者"}:new[]{"清秀青年","温婉壮年","干练中年","沉静长者","亲和","干练","坚韧","慈和"};
            if((n>=2&&n<=5)||n==13)return AppearancePartCatalog.Names(n);
            if(lookDraft.style==1&&n==1)return lookDraft.sex==0?AppearanceRules.MaleHairNames:AppearanceRules.FemaleHairNames;
            return AppearanceRules.Names[n];
        }
        void DrawRealisticLook(Transform parent,Appearance look,float x,float y,float w,float h,Color tint,bool headOnly=false)
        {
            // Keep the established editor and save entry point, but use only the
            // registered three-quarter painted assets for every portrait size.
            DrawPaintedLook(parent,look,x,y,w,h,tint,headOnly);
        }
    }
}



