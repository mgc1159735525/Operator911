using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public class FeaturePortrait:RawImage
    {
        Material owned;
        public void Configure(Appearance look)
        {
            if(!look.featureEditing)return;
            var shader=Resources.Load<Shader>("Minglu/Art/PortraitFeatures");
            if(shader==null)throw new System.InvalidOperationException("缺少写实五官着色器。");
            owned=new Material(shader);owned.SetVector("_HeadRect",new Vector4(uvRect.x,uvRect.y,uvRect.width,uvRect.height));
            owned.SetVector("_FeatureCols",new Vector4(look.parts[2],look.parts[3],look.parts[4],look.parts[5]));
            owned.SetVector("_Landmarks",PortraitLayout.Landmarks(look.sex,look.parts[0]));
            var hair=Resources.Load<Texture2D>("Minglu/Art/hair-atlas-v153");
            var src=PortraitLayout.HairSource[look.sex,look.parts[1]];
            var dst=PortraitLayout.Grooming(look.sex,look.parts[1],look);
            var head=PortraitLayout.Head(look);
            owned.SetTexture("_HairMask",hair);
            owned.SetVector("_HairMap",new Vector4((src.x+(head.x-dst.x)*src.width/dst.width)/hair.width,1-(src.y+(head.y-dst.y)*src.height/dst.height)/hair.height,head.width*src.width/dst.width/hair.width,-head.height*src.height/dst.height/hair.height));
            owned.SetVector("_HairBounds",new Vector4(src.x/hair.width,1-(src.y+src.height)/hair.height,(src.x+src.width)/hair.width,1-src.y/hair.height));
            SetCrown(look.sex,look.parts[1],hair);
            var neckFit=PortraitLayout.NeckFit(look);owned.SetVector("_NeckFit",new Vector4(neckFit.x,neckFit.y*.5f,0,0));
            owned.SetFloat("_EarCol",look.ears);owned.SetFloat("_Sex",look.sex);material=owned;
        }
        protected override void OnDestroy(){base.OnDestroy();if(owned!=null)Destroy(owned);}
        public void ConfigureNeck()
        {
            owned=new Material(Resources.Load<Shader>("Minglu/Art/PortraitFeatures"));
            owned.SetFloat("_NeckMode",1);
            owned.SetVector("_HeadRect",new Vector4(uvRect.x,uvRect.y,uvRect.width,uvRect.height));
            material=owned;
        }
        public void ConfigureHair(int tone)
        {
            var shader=Resources.Load<Shader>("Minglu/Art/PortraitFeatures");owned=new Material(shader);owned.SetFloat("_HairMode",1);
            owned.SetColor("_HairShadow",new[]{new Color(.04f,.035f,.03f),new Color(.09f,.045f,.025f),new Color(.19f,.19f,.18f),new Color(.34f,.34f,.32f)}[tone]);
            owned.SetColor("_HairLight",new[]{new Color(.35f,.32f,.28f),new Color(.50f,.33f,.19f),new Color(.72f,.72f,.69f),new Color(.95f,.94f,.89f)}[tone]);material=owned;
        }
        void SetCrown(int sex,int style,Texture atlas)
        {
            var src=PortraitLayout.HairSource[sex,style];
            owned.SetVector("_Crown",new Vector4(1-PortraitLayout.HairAnchor[sex,style].y/atlas.height,PortraitLayout.CrownHeight[sex,style],7f/atlas.height,65f/atlas.height));
            owned.SetVector("_HairBounds",new Vector4(src.x/atlas.width,1-(src.y+src.height)/atlas.height,(src.x+src.width)/atlas.width,1-src.y/atlas.height));
        }
        public void ConfigureCrown(int sex,int style){SetCrown(sex,style,texture);}
        public void ExcludeAtlasRegion(int index,float x,float y,float width,float height)
        {owned.SetVector("_Exclude"+index,new Vector4(x/texture.width,1-(y+height)/texture.height,(x+width)/texture.width,1-y/texture.height));}
        public void HairFade(float bottom,float top,bool back){owned.SetVector("_FrontFade",new Vector4(bottom,top,back?1:0,0));}
    }
}

