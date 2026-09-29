using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        Texture2D realisticAtlas,baldAtlas,hairAtlas,beardAtlas,neckAtlas,expansionAtlas;
        static readonly int[] RealisticCategories={0,1,2,3,4,5,13,6,7,9,10,11,12};
        int LookIndex(int n){return n==13?(lookDraft.featureEditing?lookDraft.ears:lookDraft.parts[0]%4):lookDraft.style==1&&n>=2&&n<=5&&!lookDraft.featureEditing?lookDraft.parts[0]%4:lookDraft.parts[n];}
        void SetLookOption(int n,int option)
        {
            lookDraft=AppearanceRules.AsRealistic(lookDraft);
            if(n==13)lookDraft.ears=option;else lookDraft.parts[n]=option;
        }
        string LookCategoryName(int n){return n==13?"耳朵":lookDraft.style==1&&n==0?"基础面容":AppearanceRules.Labels[n];}
        string[] LookOptions(int n)
        {
            if(lookDraft.style==1&&n==0)return lookDraft.sex==0?new[]{"清俊青年","端正壮年","坚毅中年","沉稳长者","书生","匠人","旅者","长者"}:new[]{"清秀青年","温婉壮年","干练中年","沉静长者","亲和","干练","坚韧","慈和"};
            if(lookDraft.style==1&&((n>=2&&n<=5)||n==13))return n==2?new[]{"原生眼形","细长","圆润","舒展"}:n==3?new[]{"原生眉形","平缓","上扬","修长"}:n==4?new[]{"原生鼻形","窄鼻","宽鼻","修长"}:n==5?new[]{"原生嘴形","收窄","舒展","丰唇"}:new[]{"原生耳形","贴耳","宽耳","修长"};
            if(lookDraft.style==1&&n==1)return lookDraft.sex==0?new[]{"束髻","短束","披发","巾帽"}:new[]{"高髻","低髻","侧髻","半束"};
            return AppearanceRules.Names[n];
        }
        void DrawRealisticLook(Transform parent,Appearance look,float x,float y,float w,float h,Color tint)
        {
            if(realisticAtlas==null)realisticAtlas=Resources.Load<Texture2D>("Minglu/Art/realistic-atlas-v10");
            if(baldAtlas==null)baldAtlas=Resources.Load<Texture2D>("Minglu/Art/bald-atlas-v11");
            if(hairAtlas==null)hairAtlas=Resources.Load<Texture2D>("Minglu/Art/hair-atlas-v153");
            if(beardAtlas==null)beardAtlas=Resources.Load<Texture2D>("Minglu/Art/hair-atlas-v11");
            if(neckAtlas==null)neckAtlas=Resources.Load<Texture2D>("Minglu/Art/neck-shoulders-v153");
            if(expansionAtlas==null)expansionAtlas=Resources.Load<Texture2D>("Minglu/Art/expansion-atlas-v12");
            var bg=Hex(new[]{"243D3B","2D4053","584437","D4C4A6"}[look.parts[9]])*tint;
            var frame=Box("RealisticPortrait",parent,x,y,w,h,bg);frame.GetComponent<Image>().raycastTarget=false;frame.gameObject.AddComponent<RectMask2D>();
            if(realisticAtlas==null){Label(frame,"立绘资源未加载",2,2,w,h,12,Red);return;}
            bool closePortrait=w<115;
            float scale=closePortrait?Mathf.Min(w/285,h/340):Mathf.Min(w/400,h/690),dx=(w-400*scale)/2,dy=closePortrait?25*scale:(h-690*scale)/2+25*scale;
            var skin=new[]{Color.white,new Color(1,.94f,.87f),new Color(.88f,.76f,.64f),new Color(.72f,.59f,.49f)}[look.parts[10]];
            var head=PortraitLayout.Head(look);
            // Long hair is behind the neck/body. The cap and fringe are drawn again above the face only.
            GroomingLayer(frame,look.parts[1],look.sex,dx,dy,scale,look,tint,true);
            var neckRect=PortraitLayout.Neck(look);
            var neck=Rect("AnatomicalNeck",frame,dx+neckRect.x*scale,dy+neckRect.y*scale,neckRect.width*scale,neckRect.height*scale).gameObject.AddComponent<FeaturePortrait>();
            neck.texture=neckAtlas;neck.uvRect=new UnityEngine.Rect((look.sex==0?259f:1134f)/1774f,1-720f/887f,400f/1774f,510f/887f);neck.color=skin*tint;neck.raycastTarget=false;neck.ConfigureNeck();
            AtlasPart(frame,look.parts[0],look.sex,dx+head.x*scale,dy+head.y*scale,head.width*scale,head.height*scale,skin*tint,look);
            AtlasPart(frame,look.parts[7],look.sex+2,dx,dy+PortraitLayout.GarmentTop(look.sex,look.parts[7])*scale,400*scale,TorsoHeight(look)*scale,Hex(new[]{"8CA5C6","88AB94","CD9582","DBC59A","A2A4A6","B4A0C1"}[look.parts[11]])*tint);
            GroomingLayer(frame,look.parts[1],look.sex,dx,dy,scale,look,tint,false);
            if(look.sex==0&&look.parts[6]>0)GroomingLayer(frame,new[]{0,0,2,3}[look.parts[6]],2,dx,dy,scale,look,tint,false);
        }
        float TorsoHeight(Appearance look){return 400f*(look.sex==0?282f:look.parts[7]>=4?313f:314f)/(1254f*.248f);}
        void AtlasPart(Transform parent,int column,int row,float x,float y,float w,float h,Color tint,Appearance look=null)
        {
            var go=Rect("PaintedLayer",parent,x,y,w,h).gameObject;
            RawImage raw=look==null?go.AddComponent<RawImage>():go.AddComponent<FeaturePortrait>();raw.texture=column>=4?expansionAtlas:look!=null&&look.groomingEditing&&baldAtlas!=null?baldAtlas:realisticAtlas;
            bool expanded=column>=4;column%=4;
            float top=new[]{0f,315f,630f,921f}[row]/1254f,bottom=new[]{312f,628f,912f,1235f}[row]/1254f;
            if(expanded&&row==3){top=916f/1254f;bottom=1229f/1254f;}
            raw.uvRect=new UnityEngine.Rect(column*.25f+.001f,1-bottom,.248f,bottom-top);raw.color=tint;raw.raycastTarget=false;
            if(look!=null)((FeaturePortrait)raw).Configure(look);
        }
        void GroomingLayer(Transform parent,int column,int row,float dx,float dy,float scale,Appearance look,Color tint,bool back)
        {
            var atlas=row==2?beardAtlas:hairAtlas;if(atlas==null)return;
            var src=PortraitLayout.HairSource[row,column];var dst=PortraitLayout.Grooming(row,column,look);
            // Upper/front hair ends below the ears. Lower locks stay behind the shoulders.

            var raw=Rect(back?"BackHair":"FrontHairBeard",parent,dx+dst.x*scale,dy+dst.y*scale,dst.width*scale,dst.height*scale).gameObject.AddComponent<FeaturePortrait>();raw.texture=atlas;
            raw.uvRect=new UnityEngine.Rect(src.x/atlas.width,1-(src.y+src.height)/atlas.height,src.width/atlas.width,src.height/atlas.height);raw.color=tint;raw.raycastTarget=false;raw.ConfigureHair(look.parts[12]);if(row<2)raw.ConfigureCrown(row,column);
            if(row<2){float factor=src.height/dst.height;var head=PortraitLayout.Head(look);raw.HairFade(1-(src.y+(head.y+head.height*.936f-dst.y)*factor)/atlas.height,1-(src.y+(head.y+head.height*.793f-dst.y)*factor)/atlas.height,back);}
            else raw.ConfigureBeard(look,dst);
        }
    }
}



