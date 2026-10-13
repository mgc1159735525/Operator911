using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    // The game and the authoring preview use this exact compositor.
    public sealed class ClassicPortraitRenderer : IDisposable
    {
        readonly Dictionary<int,Texture2D> textures=new Dictionary<int,Texture2D>();
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static readonly Vector2 TopLeft=new Vector2(0,1);
        static Color Hex(string s){Color c;ColorUtility.TryParseHtmlString("#"+s,out c);return c;}
        Texture2D Texture(int i){Texture2D t;if(!textures.TryGetValue(i,out t)){t=Resources.Load<Texture2D>(ClassicPortraitCatalog.ResourcePaths[i]);textures[i]=t;}return t;}
        public static RectTransform RectNode(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=TopLeft;r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public static Rect BaseRect(Appearance a,PortraitPart part)
        {
            switch(part)
            {
                case PortraitPart.Head:return ClassicPortraitCatalog.HeadRect;
                case PortraitPart.Neck:return ClassicPortraitCatalog.NeckRect;
                case PortraitPart.Outfit:return ClassicPortraitCatalog.GarmentRect(a);
                case PortraitPart.Eyes:return ClassicPortraitCatalog.FeatureRect(a,2);
                case PortraitPart.Brows:return ClassicPortraitCatalog.FeatureRect(a,3);
                case PortraitPart.Nose:return ClassicPortraitCatalog.FeatureRect(a,4);
                case PortraitPart.Mouth:return ClassicPortraitCatalog.FeatureRect(a,5);
                case PortraitPart.Beard:return a.sex==0&&a.parts[6]>0?ClassicPortraitCatalog.BeardRect(a):new Rect();
                case PortraitPart.AdornmentFar:return a.parts[8]==2?ClassicPortraitCatalog.FromHead(new Rect(95,211,11,27)):new Rect();
                case PortraitPart.Adornment:return a.parts[8]==2?ClassicPortraitCatalog.FromHead(new Rect(262,229,15,33)):a.parts[8]>0?ClassicPortraitCatalog.AdornmentRect(a):new Rect();
                default:return new Rect();
            }
        }
        public static Vector2 Rotate(Vector2 point,float degrees)
        {float r=degrees*Mathf.Deg2Rad,c=Mathf.Cos(r),s=Mathf.Sin(r);return new Vector2(c*point.x-s*point.y,s*point.x+c*point.y);}
        public static Vector2 HeadPoint(Appearance a,Vector2 p)
        {
            var t=PortraitCalibration.Resolve(a,PortraitPart.Head);var pivot=ClassicPortraitCatalog.HeadRect.center;
            return pivot+new Vector2(t.dx,t.dy)+Rotate(Vector2.Scale(p-pivot,new Vector2(t.scaleX,t.scaleY)),t.rotation);
        }
        public static Vector2 CanvasDeltaToPartDelta(Appearance a,PortraitPart part,Vector2 delta)
        {
            if(part==PortraitPart.Head||part==PortraitPart.Outfit)return delta;
            var t=PortraitCalibration.Resolve(a,PortraitPart.Head);var v=Rotate(delta,-t.rotation);return new Vector2(v.x/t.scaleX,v.y/t.scaleY);
        }
        public static Rect Bounds(Appearance a,PortraitPart part)
        {
            var rect=BaseRect(a,part);if(rect.width<=0)return rect;
            var t=PortraitCalibration.Resolve(a,part);var pivot=rect.center;
            if(part!=PortraitPart.Head)
            {
                rect=t.Apply(rect);pivot=rect.center;
                if(part==PortraitPart.Eyes||part==PortraitPart.Brows)
                {
                    float unionWidth=rect.width*.5f+Mathf.Abs(rect.width*.5f+t.spacing);
                    rect.x=pivot.x-unionWidth*.5f;rect.width=unionWidth;
                }
            }
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=-min;
            for(int i=0;i<4;i++)
            {
                var p=new Vector2((i&1)==0?rect.xMin:rect.xMax,(i&2)==0?rect.yMin:rect.yMax);
                if(part!=PortraitPart.Head)p=pivot+Rotate(p-pivot,t.rotation);
                if(part!=PortraitPart.Outfit)p=HeadPoint(a,p);
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        RectTransform Rig(Transform parent,string name,Vector2 pivot,float scale,PortraitTransform t,bool stretch)
        {
            var r=RectNode(name,parent,0,0,400*scale,600*scale);
            r.pivot=new Vector2(pivot.x/400,1-pivot.y/600);
            r.anchoredPosition=new Vector2((pivot.x+t.dx)*scale,-(pivot.y+t.dy)*scale);
            r.localRotation=Quaternion.Euler(0,0,-t.rotation);
            if(stretch)r.localScale=new Vector3(t.scaleX,t.scaleY,1);
            return r;
        }
        public RectTransform Draw(Transform parent,Appearance value,float x,float y,float w,float h,Color tint,bool headOnly=false)
        {
            var a=AppearanceRules.AsRealistic(value);var frame=RectNode("ClassicPortrait",parent,x,y,w,h);
            var back=frame.gameObject.AddComponent<Image>();back.color=Hex(new[]{"DBD7BF","CDD7D5","DDCEB8","ECE5D1"}[a.parts[9]])*tint;back.raycastTarget=false;
            frame.gameObject.AddComponent<RectMask2D>();
            var landscape=RectNode("ClassicBackdrop",frame,0,0,w,h).gameObject.AddComponent<RawImage>();landscape.texture=Texture(5);landscape.uvRect=ClassicPortraitCatalog.Cell(a.parts[9],2,2);landscape.color=new Color(tint.r,tint.g,tint.b,.55f*tint.a);landscape.raycastTarget=false;
            float scale=Mathf.Min(w/400,h/600),ox=(w-400*scale)*.5f,oy=(h-600*scale)*.5f;
            if(headOnly){scale=Mathf.Min(w/330,h/380);ox=w*.5f-200*scale;oy=(h-380*scale)*.5f+8*scale;}
            else if(w<115){scale=Mathf.Max(w/265,h/340);ox=w*.5f-200*scale;oy=8*scale;}
            var art=RectNode("ClassicCanvas",frame,ox,oy,400*scale,600*scale);
            Layer(art,a,PortraitPart.Neck,a.sex,ClassicPortraitCatalog.NeckUV(a),scale,8,tint);
            Layer(art,a,PortraitPart.Head,a.sex,ClassicPortraitCatalog.Cell(a.parts[0],4,3),scale,0,tint);
            Layer(art,a,PortraitPart.Outfit,3,ClassicPortraitCatalog.OutfitUV(a),scale,3,tint);
            for(int p=2;p<=5;p++)Layer(art,a,(PortraitPart)((int)PortraitPart.Eyes+p-2),p==3?6:2,ClassicPortraitCatalog.FeatureCell(a,p),scale,p==2?1:p==3?2:p==4?5:6,tint);
            if(a.sex==0&&a.parts[6]>0)Layer(art,a,PortraitPart.Beard,4,ClassicPortraitCatalog.DetailUV(a.parts[6]-1),scale,7,tint);
            if(a.parts[8]==2)
            {
                Layer(art,a,PortraitPart.AdornmentFar,4,ClassicPortraitCatalog.SourcePixels(76,812,77,174,1448,1086),scale,4,tint);
                Layer(art,a,PortraitPart.Adornment,4,ClassicPortraitCatalog.SourcePixels(215,812,78,174,1448,1086),scale,4,tint);
            }
            else if(a.parts[8]>0)Layer(art,a,PortraitPart.Adornment,4,ClassicPortraitCatalog.DetailUV(a.parts[8]+6),scale,4,tint);
            return frame;
        }
        void Layer(Transform parent,Appearance a,PortraitPart part,int tex,Rect uv,float scale,int mode,Color tint)
        {
            var position=BaseRect(a,part);var t=PortraitCalibration.Resolve(a,part);
            if(part!=PortraitPart.Outfit)parent=Rig(parent,"HeadRig"+part,ClassicPortraitCatalog.HeadRect.center,scale,PortraitCalibration.Resolve(a,PortraitPart.Head),true);
            if(part!=PortraitPart.Head)
            {
                position=t.Apply(position);
                var rotationOnly=PortraitTransform.Identity;rotationOnly.rotation=t.rotation;
                parent=Rig(parent,"PartRig"+part,position.center,scale,rotationOnly,false);
            }
            var pivot=position.center;
            if(part==PortraitPart.Eyes||part==PortraitPart.Brows)
            {
                var left=position;left.width*=.5f;left.x-=t.spacing*.5f;
                var right=position;right.width*=.5f;right.x+=right.width+t.spacing*.5f;
                var lu=uv;lu.width*=.5f;var ru=lu;ru.x+=lu.width;
                Raw(parent,part.ToString(),a,tex,lu,left,pivot,t.rotation,scale,mode,tint);
                Raw(parent,part+"Right",a,tex,ru,right,pivot,t.rotation,scale,mode,tint);
            }
            else Raw(parent,part.ToString(),a,tex,uv,position,pivot,part==PortraitPart.Head?0:t.rotation,scale,mode,tint);
        }
        void Raw(Transform parent,string name,Appearance a,int tex,Rect uv,Rect position,Vector2 pivot,float angle,float scale,int mode,Color tint)
        {
            var layer=RectNode("Classic"+name,parent,position.x*scale,position.y*scale,position.width*scale,position.height*scale).gameObject.AddComponent<RawImage>();
            layer.texture=Texture(tex);layer.uvRect=uv;layer.color=tint;layer.raycastTarget=false;layer.material=Material(a,mode,uv,position,pivot,angle);
        }
        static string Number(float n){return n.ToString("R",CultureInfo.InvariantCulture);}
        Material Material(Appearance a,int mode,Rect source,Rect target,Vector2 pivot,float angle)
        {
            string key=a.sex+":"+string.Join(",",a.parts)+":"+mode+":"+Number(source.x)+":"+Number(source.y)+":"+Number(source.width)+":"+Number(source.height)+":"+Number(target.x)+":"+Number(target.y)+":"+Number(target.width)+":"+Number(target.height)+":"+Number(pivot.x)+":"+Number(pivot.y)+":"+Number(angle);
            Material mat;if(materials.TryGetValue(key,out mat))return mat;
            var shader=Resources.Load<Shader>("Minglu/Art/Classic/Portrait");if(shader==null)return null;
            bool cap=a.sex==0&&(a.parts[0]==2||a.parts[0]==6||a.parts[0]==11),white=a.parts[0]>=9;
            mat=new Material(shader){name="ClassicPortraitPalette",hideFlags=HideFlags.HideAndDontSave};
            mat.SetFloat("_Mode",mode);mat.SetFloat("_HairIndex",a.parts[12]);mat.SetFloat("_SourceWhite",white?1:0);mat.SetFloat("_Cap",cap?1:0);
            mat.SetColor("_Skin",Hex(new[]{"FFF4E8","F5DCC0","D8B591","AE896D"}[a.parts[10]]));
            mat.SetColor("_Cloth",Hex(new[]{"738B9D","7A8C76","AE7869","B2A082","666B6C","8F7E9E"}[a.parts[11]]));
            var cell=ClassicPortraitCatalog.Cell(a.parts[0],4,3);mat.SetVector("_BaseUV",new Vector4(cell.x,cell.y,cell.width,cell.height));
            if(mode==0||mode==8){mat.SetFloat("_HeadTopTrim",ClassicPortraitCatalog.HeadTopTrim(a));mat.SetFloat("_HeadLeftTrim",ClassicPortraitCatalog.HeadLeftTrim(a));}
            int facialPart=mode==1?2:mode==5?4:mode==6?5:-1;
            if(facialPart>=0)
            {
                mat.SetTexture("_BaseTex",Texture(a.sex));mat.SetVector("_FeatureUV",new Vector4(source.x,source.y,source.width,source.height));
                mat.SetColor("_FeatureSkin",ClassicPortraitCatalog.FeatureSkin(a,facialPart));
                var head=ClassicPortraitCatalog.HeadRect;
                mat.SetVector("_FeatureTarget",new Vector4((target.x-head.x)/head.width,(target.y-head.y)/head.height,target.width/head.width,target.height/head.height));
                mat.SetVector("_FeatureRotation",new Vector4((pivot.x-head.x)/head.width,(pivot.y-head.y)/head.height,Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad)));
            }
            materials[key]=mat;return mat;
        }
        public void Dispose(){foreach(var m in materials.Values)if(m!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(m);else UnityEngine.Object.DestroyImmediate(m);}materials.Clear();}
    }
}
