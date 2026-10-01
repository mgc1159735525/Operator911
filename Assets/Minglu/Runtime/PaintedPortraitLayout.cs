using System;
using UnityEngine;

namespace Minglu
{
    // Coordinates are top-left normalized coordinates in the continuous 2:3
    // body painting. Registration is independent of the UI rectangle and save.
    public static class PaintedPortraitLayout
    {
        public const float Width=400, Height=600;
        public static readonly Rect DetailCrop=new Rect(.055f,-.035f,.88f,.84f);
        // Show the authored shoulder width and more chest. The previous 1.07
        // crop cut the 1.27-wide garments at both shoulders, exaggerating heads.
        public static readonly Rect FullCrop=new Rect(-.16f,-.080f,1.32f,1.33f);
        public struct Features
        {
            public Vector4 eyeFar,eyeNear,browFar,browNear,nose,mouth,earFar,earNear;
            public Vector2 chin,skull,hairline,lipFar,lipNear;
        }
        // Measured separately for all sixteen paintings. The second row of
        // each sex is 6-11 source pixels higher; one age offset was insufficient.
        static readonly Vector2[,] NearEyes={
            {new Vector2(.456f,.333f),new Vector2(.445f,.334f),new Vector2(.417f,.335f),new Vector2(.409f,.335f),new Vector2(.456f,.316f),new Vector2(.446f,.311f),new Vector2(.421f,.312f),new Vector2(.414f,.315f)},
            {new Vector2(.454f,.309f),new Vector2(.446f,.309f),new Vector2(.424f,.312f),new Vector2(.416f,.311f),new Vector2(.450f,.294f),new Vector2(.437f,.293f),new Vector2(.424f,.293f),new Vector2(.421f,.302f)}};
        static readonly Vector2[,] FarEyes={
            {new Vector2(.291f,.345f),new Vector2(.274f,.345f),new Vector2(.256f,.348f),new Vector2(.239f,.347f),new Vector2(.287f,.330f),new Vector2(.274f,.327f),new Vector2(.255f,.327f),new Vector2(.246f,.327f)},
            {new Vector2(.288f,.322f),new Vector2(.272f,.322f),new Vector2(.253f,.325f),new Vector2(.243f,.322f),new Vector2(.286f,.312f),new Vector2(.271f,.311f),new Vector2(.255f,.307f),new Vector2(.251f,.314f)}};
        static readonly Vector2[,] Noses={
            {new Vector2(.312f,.424f),new Vector2(.298f,.423f),new Vector2(.276f,.426f),new Vector2(.262f,.427f),new Vector2(.303f,.407f),new Vector2(.296f,.402f),new Vector2(.275f,.404f),new Vector2(.260f,.410f)},
            {new Vector2(.314f,.399f),new Vector2(.305f,.402f),new Vector2(.279f,.401f),new Vector2(.268f,.403f),new Vector2(.305f,.387f),new Vector2(.301f,.387f),new Vector2(.283f,.383f),new Vector2(.274f,.388f)}};
        static readonly Vector2[,] Mouths={
            {new Vector2(.375f,.489f),new Vector2(.365f,.489f),new Vector2(.345f,.489f),new Vector2(.333f,.492f),new Vector2(.376f,.471f),new Vector2(.360f,.466f),new Vector2(.344f,.471f),new Vector2(.342f,.473f)},
            {new Vector2(.382f,.468f),new Vector2(.363f,.468f),new Vector2(.351f,.471f),new Vector2(.341f,.471f),new Vector2(.380f,.453f),new Vector2(.363f,.453f),new Vector2(.350f,.453f),new Vector2(.350f,.463f)}};
        static readonly Vector2[,] Chins={
            {new Vector2(.420f,.564f),new Vector2(.411f,.566f),new Vector2(.394f,.566f),new Vector2(.380f,.566f),new Vector2(.425f,.548f),new Vector2(.415f,.546f),new Vector2(.395f,.548f),new Vector2(.386f,.551f)},
            {new Vector2(.422f,.553f),new Vector2(.411f,.553f),new Vector2(.395f,.556f),new Vector2(.382f,.555f),new Vector2(.427f,.540f),new Vector2(.412f,.540f),new Vector2(.397f,.541f),new Vector2(.389f,.550f)}};
        static readonly Vector2[,] Ears={
            {new Vector2(.746f,.382f),new Vector2(.734f,.383f),new Vector2(.716f,.386f),new Vector2(.701f,.384f),new Vector2(.746f,.363f),new Vector2(.734f,.365f),new Vector2(.716f,.367f),new Vector2(.699f,.365f)},
            {new Vector2(.743f,.359f),new Vector2(.732f,.360f),new Vector2(.717f,.365f),new Vector2(.700f,.366f),new Vector2(.740f,.343f),new Vector2(.724f,.345f),new Vector2(.710f,.352f),new Vector2(.697f,.353f)}};
        static readonly Vector2[,] Skulls={
            {new Vector2(.545f,.107f),new Vector2(.537f,.106f),new Vector2(.513f,.108f),new Vector2(.504f,.111f),new Vector2(.541f,.088f),new Vector2(.529f,.086f),new Vector2(.511f,.087f),new Vector2(.498f,.089f)},
            {new Vector2(.541f,.084f),new Vector2(.536f,.084f),new Vector2(.508f,.086f),new Vector2(.506f,.086f),new Vector2(.536f,.073f),new Vector2(.524f,.073f),new Vector2(.508f,.075f),new Vector2(.508f,.078f)}};
        public static Features Landmarks(Appearance look)
        {
            int s=look.sex,f=look.parts[0];var near=NearEyes[s,f];var far=FarEyes[s,f];var mouth=Mouths[s,f];var ear=Ears[s,f];
            return new Features{
                eyeFar=new Vector4(far.x,far.y,.050f,.027f),eyeNear=new Vector4(near.x,near.y,.070f,.031f),
                browFar=new Vector4(far.x-.006f,far.y-.034f,.057f,.024f),browNear=new Vector4(near.x-.004f,near.y-.038f,.079f,.025f),
                nose=new Vector4(Noses[s,f].x,Noses[s,f].y,.070f,.068f),mouth=new Vector4(mouth.x,mouth.y,.089f,.033f),
                earFar=new Vector4(far.x-.040f,ear.y,.018f,.032f),earNear=new Vector4(ear.x,ear.y,.061f,.065f),
                chin=Chins[s,f],skull=Skulls[s,f],hairline=new Vector2(near.x+.018f,near.y-.106f),
                lipFar=mouth+new Vector2(-.054f,-.002f),lipNear=mouth+new Vector2(.073f,.002f)};
        }
        public static Rect Cell(int column,int row,int columns,int rows)
        {
            if(columns<1||rows<1||column<0||column>=columns||row<0||row>=rows)throw new ArgumentOutOfRangeException("Portrait atlas cell");
            return new Rect((float)column/columns,1f-(float)(row+1)/rows,1f/columns,1f/rows);
        }
        public static Rect Base(Appearance look){return Cell(look.parts[0]%4,look.sex*2+look.parts[0]/4,4,4);}
        public static Rect HairCanvas(Appearance look)
        {
            if(look.sex==0&&look.parts[1]==0)return new Rect(0,0,1,.92f);
            if(look.sex==0&&look.parts[1]==1)return new Rect(0,0,.94f,.92f);
            if(look.sex==1&&look.parts[1]==0)return new Rect(0,-.042f,1,1.042f);
            return new Rect(0,0,1,1);
        }
        public static Rect Hair(Appearance look)
        {
            var cell=Cell(look.parts[1],look.sex,4,2);var bounds=HairCanvas(look);
            return new Rect(cell.x+bounds.x*cell.width,cell.y+(1-bounds.yMax)*cell.height,cell.width*bounds.width,cell.height*bounds.height);
        }
        public static Rect Outfit(Appearance look){return Cell(look.parts[7]%4,look.sex*2+look.parts[7]/4,4,4);}
        // Column 1 contains a 2-3 pixel sliver of column 0, separated from its
        // own shoulder by transparent pixels 316-318. Fade only that gap;
        // the actual garment begins at pixel 319 and keeps its full width.
        public static float OutfitEdgeAlpha(Appearance look,Vector2 source)
        {return look.parts[7]%4==1?Smooth(.0128f,.0160f,source.x):1;}
        public static Rect Beard(Appearance look){return Cell(Mathf.Clamp(look.parts[6]-1,0,2),0,3,1);}
        public static Rect HairRect(Appearance look)
        {
            float[] ms={.780f,.780f,.780f,.804f},fs={.800f,.815f,.795f,.800f};
            float[] mx={.165f,.165f,.165f,.162f},fx={.142f,.142f,.151f,.146f};
            float[] my={-.052f,-.052f,-.052f,-.060f},fy={-.020f,-.031f,-.017f,-.016f};
            int h=look.parts[1];float scale=(look.sex==0?ms:fs)[h];var b=HairCanvas(look);
            return new Rect((look.sex==0?mx:fx)[h]+b.x*scale,(look.sex==0?my:fy)[h]+b.y*scale,b.width*scale,b.height*scale);
        }
        public static Rect BeardRect(Appearance look){return new Rect(0,0,1,1);}
        public static Rect OutfitRect(Appearance look){return new Rect(-.112f,look.sex==0?.424f:.416f,1.27f,1.27f*2f/3);}
        public static Rect Register(Rect frame,Rect registration)
        {return new Rect(frame.x+frame.width*registration.x,frame.y+frame.height*registration.y,frame.width*registration.width,frame.height*registration.height);}
        public static Rect DrawRect(Appearance look,PaintedPortraitLayer layer)
        {return layer==PaintedPortraitLayer.Hair?new Rect(-.16f,-.17f,1.34f,1.40f):layer==PaintedPortraitLayer.Beard?new Rect(.10f,.32f,.80f,.75f):layer==PaintedPortraitLayer.Outfit?OutfitRect(look):new Rect(0,0,1,1);}
        public static Rect Placement(float width,float height,bool detail)
        {
            var crop=detail?DetailCrop:FullCrop;float scale=Mathf.Min(width/(Width*crop.width),height/(Height*crop.height));
            return new Rect((width-Width*crop.width*scale)/2-Width*crop.x*scale,(height-Height*crop.height*scale)/2-Height*crop.y*scale,Width*scale,Height*scale);
        }
        // Crown, frontal hairline, exposed near ear and far temple. Source
        // coordinates precede the crop which removes neighbouring-cell debris.
        static readonly Vector2[,] HairSources={
            {new Vector2(.480f,.198f),new Vector2(.400f,.347f),new Vector2(.755f,.550f),new Vector2(.160f,.500f)},
            {new Vector2(.480f,.198f),new Vector2(.400f,.346f),new Vector2(.720f,.551f),new Vector2(.120f,.500f)},
            {new Vector2(.480f,.198f),new Vector2(.400f,.349f),new Vector2(.720f,.556f),new Vector2(.117f,.500f)},
            {new Vector2(.494f,.199f),new Vector2(.400f,.371f),new Vector2(.668f,.545f),new Vector2(.065f,.500f)},
            {new Vector2(.500f,.116f),new Vector2(.400f,.270f),new Vector2(.753f,.468f),new Vector2(.170f,.425f)},
            {new Vector2(.480f,.111f),new Vector2(.400f,.310f),new Vector2(.720f,.470f),new Vector2(.130f,.425f)},
            {new Vector2(.480f,.095f),new Vector2(.400f,.312f),new Vector2(.686f,.475f),new Vector2(.120f,.425f)},
            {new Vector2(.480f,.095f),new Vector2(.400f,.328f),new Vector2(.650f,.468f),new Vector2(.112f,.425f)}};
        public static Vector2 HairSource(Appearance look,int anchor){return HairSources[look.sex*4+look.parts[1],anchor];}
        public static Vector2 HairTarget(Appearance look,int anchor)
        {
            var f=Landmarks(look);
            if(anchor==0)return f.skull+new Vector2(0,-.012f);
            if(anchor==1)return f.hairline;
            if(anchor==2)return new Vector2(f.earNear.x,f.earNear.y);
            return new Vector2(f.eyeFar.x-.017f,f.eyeFar.y);
        }
        public static Vector4 HairBinding(Appearance look,int anchor)
        {
            var source=HairSource(look,anchor);var canvas=HairCanvas(look);var rect=HairRect(look);var target=HairTarget(look,anchor);
            var projected=new Vector2(rect.x+(source.x-canvas.x)/canvas.width*rect.width,rect.y+(source.y-canvas.y)/canvas.height*rect.height);
            return new Vector4(target.x,target.y,target.x-projected.x,target.y-projected.y);
        }
        public static Vector4 CrownAdjustment(Appearance look)
        {
            var p=HairTarget(look,0);float amount=look.parts[1]==0?(look.sex==0?.89f:.84f):look.sex==0&&look.parts[1]==3?.90f:1;
            return new Vector4(p.x,p.y,amount,0);
        }
        static float Smooth(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3-2*t);}
        public static Vector2 HairSamplingPoint(Appearance look,Vector2 p)
        {
            var crown=CrownAdjustment(look);var center=new Vector2(crown.x,crown.y);
            float influence=1-Smooth(crown.y-.02f,crown.y+.03f,p.y);
            p=Vector2.Lerp(p,center+(p-center)/crown.z,influence);
            Vector2 delta=Vector2.zero;float total=0;
            for(int i=0;i<4;i++)
            {
                var b=HairBinding(look,i);var d=p-new Vector2(b.x,b.y);d.y*=1.5f;
                float weight=1/(d.sqrMagnitude+.0000001f);delta+=new Vector2(b.z,b.w)*weight;total+=weight;
            }
            var rect=HairRect(look);var sample=p-delta/total;
            return new Vector2((sample.x-rect.x)/rect.width,(sample.y-rect.y)/rect.height);
        }
        public static Vector4 BeardMouth(Appearance look){var f=Landmarks(look);return new Vector4(f.mouth.x,f.mouth.y,.400f,.382f);}
        public static Vector4 BeardChin(Appearance look){var f=Landmarks(look);return new Vector4(f.chin.x,f.chin.y,.394f,.510f);}
        public static Vector4 BeardScales(Appearance look)
        {
            var f=Landmarks(look);float style=look.parts[6]==3?.86f:1;
            return new Vector4((f.mouth.x-f.lipFar.x)/.120f*style,(f.lipNear.x-f.mouth.x)/.110f*style,.49f,look.parts[6]==2?.49f:.46f);
        }
        public static Vector2 BeardSamplingPoint(Appearance look,Vector2 p)
        {
            var m=BeardMouth(look);var c=BeardChin(look);var scale=BeardScales(look);
            float t=Mathf.Clamp01((p.y-m.y)/(c.y-m.y));float center=Mathf.Lerp(m.x,c.x,t),sourceCenter=Mathf.Lerp(m.z,c.z,t);
            float sy=p.y<m.y?m.w+(p.y-m.y)/scale.z:p.y>c.y?c.w+(p.y-c.y)/scale.w:Mathf.Lerp(m.w,c.w,t);
            float sxScale=Mathf.Lerp(p.x<center?scale.x:scale.y,.57f,t);
            return new Vector2(sourceCenter+(p.x-center)/sxScale,sy);
        }
    }
}
