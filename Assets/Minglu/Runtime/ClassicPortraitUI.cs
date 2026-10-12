using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        readonly Dictionary<string,Texture2D> classicTextures=new Dictionary<string,Texture2D>();
        readonly Dictionary<int,Material> classicMaterials=new Dictionary<int,Material>();
        Texture2D ClassicTexture(int index)
        {
            string path=ClassicPortraitCatalog.ResourcePaths[index];Texture2D tex;
            if(!classicTextures.TryGetValue(path,out tex)){tex=Resources.Load<Texture2D>(path);classicTextures[path]=tex;}
            return tex;
        }
        Material ClassicMaterial(Appearance a,int mode)
        {
            bool cap=a.sex==0&&(a.parts[0]==2||a.parts[0]==6||a.parts[0]==11),white=a.parts[0]>=9;
            int key=mode+16*a.parts[10]+64*a.parts[11]+384*a.parts[12]+1536*(cap?1:0)+3072*(white?1:0);
            int facialPart=mode==1?2:mode==5?4:mode==6?5:-1;
            if(facialPart>=0)key+=6144*(1+a.sex+2*a.parts[0]+24*a.parts[facialPart]);
            else if(mode==0||mode==8)key+=6144*(1+a.sex+2*a.parts[0]);
            Material mat;if(classicMaterials.TryGetValue(key,out mat))return mat;
            var shader=Resources.Load<Shader>("Minglu/Art/Classic/Portrait");if(shader==null)return null;
            mat=new Material(shader){name="ClassicPortraitPalette",hideFlags=HideFlags.HideAndDontSave};
            mat.SetFloat("_Mode",mode);mat.SetFloat("_HairIndex",a.parts[12]);mat.SetFloat("_SourceWhite",white?1:0);mat.SetFloat("_Cap",cap?1:0);
            mat.SetColor("_Skin",Hex(new[]{"FFF4E8","F5DCC0","D8B591","AE896D"}[a.parts[10]]));
            mat.SetColor("_Cloth",Hex(new[]{"738B9D","7A8C76","AE7869","B2A082","666B6C","8F7E9E"}[a.parts[11]]));
            if(mode==0||mode==8)
            {
                var cell=ClassicPortraitCatalog.Cell(a.parts[0],4,3);
                mat.SetVector("_BaseUV",new Vector4(cell.x,cell.y,cell.width,cell.height));
                mat.SetFloat("_HeadTopTrim",ClassicPortraitCatalog.HeadTopTrim(a));
                mat.SetFloat("_HeadLeftTrim",ClassicPortraitCatalog.HeadLeftTrim(a));
            }
            if(facialPart>=0)
            {
                mat.SetTexture("_BaseTex",ClassicTexture(a.sex));
                var baseCell=ClassicPortraitCatalog.Cell(a.parts[0],4,3);mat.SetVector("_BaseUV",new Vector4(baseCell.x,baseCell.y,baseCell.width,baseCell.height));
                var source=ClassicPortraitCatalog.FeatureCell(a,facialPart);mat.SetVector("_FeatureUV",new Vector4(source.x,source.y,source.width,source.height));
                mat.SetColor("_FeatureSkin",ClassicPortraitCatalog.FeatureSkin(a,facialPart));
                var target=ClassicPortraitCatalog.FeatureRect(a,facialPart);var head=ClassicPortraitCatalog.HeadRect;
                mat.SetVector("_FeatureTarget",new Vector4((target.x-head.x)/head.width,(target.y-head.y)/head.height,target.width/head.width,target.height/head.height));
            }
            classicMaterials[key]=mat;return mat;
        }
        void DrawClassicLook(Transform parent,Appearance value,float x,float y,float w,float h,Color tint,bool headOnly=false)
        {
            var look=AppearanceRules.AsRealistic(value);
            var frame=Rect("ClassicPortrait",parent,x,y,w,h);
            var back=frame.gameObject.AddComponent<Image>();back.color=Hex(new[]{"DBD7BF","CDD7D5","DDCEB8","ECE5D1"}[look.parts[9]])*tint;back.raycastTarget=false;
            frame.gameObject.AddComponent<RectMask2D>();
            var landscape=Rect("ClassicBackdrop",frame,0,0,w,h).gameObject.AddComponent<RawImage>();
            landscape.texture=ClassicTexture(5);landscape.uvRect=ClassicPortraitCatalog.Cell(look.parts[9],2,2);landscape.color=new Color(tint.r,tint.g,tint.b,.55f*tint.a);landscape.raycastTarget=false;
            float scale=Mathf.Min(w/400,h/600),ox=(w-400*scale)/2,oy=(h-600*scale)/2;
            if(headOnly){scale=Mathf.Min(w/330,h/380);ox=w*.5f-200*scale;oy=(h-380*scale)*.5f+8*scale;}
            else if(w<115){scale=Mathf.Max(w/265,h/340);ox=w*.5f-200*scale;oy=8*scale;}
            var art=Rect("ClassicCanvas",frame,ox,oy,400*scale,600*scale);
            ClassicLayer(art,"Neck",look,look.sex,ClassicPortraitCatalog.NeckUV(look),ClassicPortraitCatalog.NeckRect,scale,8,tint);
            ClassicLayer(art,"Head",look,look.sex,ClassicPortraitCatalog.Cell(look.parts[0],4,3),ClassicPortraitCatalog.HeadRect,scale,0,tint);
            ClassicLayer(art,"Outfit",look,3,ClassicPortraitCatalog.OutfitUV(look),ClassicPortraitCatalog.GarmentRect(look),scale,3,tint);
            string[] names={"Eyes","Brows","Nose","Mouth"};
            for(int p=2;p<=5;p++)ClassicLayer(art,names[p-2],look,p==3?6:2,ClassicPortraitCatalog.FeatureCell(look,p),ClassicPortraitCatalog.FeatureRect(look,p),scale,p==2?1:p==3?2:p==4?5:6,tint);
            if(look.sex==0&&look.parts[6]>0)ClassicLayer(art,"Beard",look,4,ClassicPortraitCatalog.DetailUV(look.parts[6]-1),ClassicPortraitCatalog.BeardRect(look),scale,7,tint);
            if(look.parts[8]==2)
            {
                ClassicLayer(art,"AdornmentFar",look,4,ClassicPortraitCatalog.SourcePixels(76,812,77,174,1448,1086),ClassicPortraitCatalog.FromHead(new Rect(95,211,11,27)),scale,4,tint);
                ClassicLayer(art,"Adornment",look,4,ClassicPortraitCatalog.SourcePixels(215,812,78,174,1448,1086),ClassicPortraitCatalog.FromHead(new Rect(262,229,15,33)),scale,4,tint);
            }
            else if(look.parts[8]>0)ClassicLayer(art,"Adornment",look,4,ClassicPortraitCatalog.DetailUV(look.parts[8]+6),ClassicPortraitCatalog.AdornmentRect(look),scale,4,tint);
        }
        void ClassicLayer(Transform parent,string name,Appearance look,int texture,Rect uv,Rect position,float scale,int mode,Color tint)
        {
            var layer=Rect("Classic"+name,parent,position.x*scale,position.y*scale,position.width*scale,position.height*scale).gameObject.AddComponent<RawImage>();
            layer.texture=ClassicTexture(texture);layer.uvRect=uv;layer.color=tint;layer.raycastTarget=false;layer.material=ClassicMaterial(look,mode);
        }
        void DisposeClassicPortraits(){foreach(var material in classicMaterials.Values)if(material!=null)Destroy(material);classicMaterials.Clear();}
    }
}
