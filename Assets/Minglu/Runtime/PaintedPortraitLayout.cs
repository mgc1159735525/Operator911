using System;
using UnityEngine;

namespace Minglu
{
    // Coordinates are top-left normalized coordinates in the continuous 2:3
    // body painting. Registration is independent of the UI rectangle and save.
    public static class PaintedPortraitLayout
    {
        public const float Width=400, Height=600;
        public const int HairCount=8, BeardCount=8;
        // Hair and facial hair share pigments. The ivory range starts bright
        // enough to read as white, while retaining painted strand shadows.
        public static Color HairShadow(int color)
        {return new[]{new Color(.055f,.048f,.041f),new Color(.105f,.062f,.039f),new Color(.31f,.31f,.29f),new Color(.64f,.64f,.61f)}[Mathf.Clamp(color,0,3)];}
        public static Color HairLight(int color)
        {return new[]{new Color(.35f,.31f,.27f),new Color(.48f,.34f,.23f),new Color(.78f,.78f,.74f),new Color(.99f,.985f,.95f)}[Mathf.Clamp(color,0,3)];}
        public static readonly Rect DetailCrop=new Rect(.055f,-.090f,.88f,.895f);
        // One common full-body frame for all headwear and outfits. Leave room
        // for the shoulder silhouette instead of fitting the frame to the head.
        public static readonly Rect FullCrop=new Rect(-.235f,-.095f,1.52f,1.495f);
        public struct Features
        {
            public Vector4 eyeFar,eyeNear,browFar,browNear,nose,mouth,earFar,earNear;
            public Vector2 chin,skull,hairline,lipFar,lipNear;
        }
        // Measured separately for all sixteen paintings. The second row of
        // each sex is 6-11 source pixels higher; one age offset was insufficient.
        static readonly Vector2[,] NearEyes={
            {new Vector2(0.504f,0.336f),new Vector2(0.496f,0.336f),new Vector2(0.480f,0.339f),new Vector2(0.473f,0.336f),new Vector2(0.508f,0.315f),new Vector2(0.508f,0.310f),new Vector2(0.477f,0.312f),new Vector2(0.469f,0.315f)},
            {new Vector2(0.504f,0.312f),new Vector2(0.504f,0.312f),new Vector2(0.477f,0.312f),new Vector2(0.465f,0.312f),new Vector2(0.500f,0.299f),new Vector2(0.496f,0.299f),new Vector2(0.477f,0.299f),new Vector2(0.465f,0.307f)}};
        static readonly Vector2[,] FarEyes={
            {new Vector2(0.312f,0.346f),new Vector2(0.297f,0.344f),new Vector2(0.293f,0.349f),new Vector2(0.273f,0.346f),new Vector2(0.312f,0.331f),new Vector2(0.309f,0.328f),new Vector2(0.285f,0.328f),new Vector2(0.277f,0.328f)},
            {new Vector2(0.316f,0.320f),new Vector2(0.309f,0.320f),new Vector2(0.285f,0.323f),new Vector2(0.270f,0.323f),new Vector2(0.312f,0.312f),new Vector2(0.301f,0.310f),new Vector2(0.277f,0.307f),new Vector2(0.270f,0.315f)}};
        static readonly Vector2[,] Noses={
            {new Vector2(0.352f,0.427f),new Vector2(0.340f,0.424f),new Vector2(0.332f,0.427f),new Vector2(0.324f,0.427f),new Vector2(0.352f,0.406f),new Vector2(0.352f,0.404f),new Vector2(0.328f,0.406f),new Vector2(0.309f,0.409f)},
            {new Vector2(0.355f,0.401f),new Vector2(0.355f,0.404f),new Vector2(0.320f,0.401f),new Vector2(0.312f,0.406f),new Vector2(0.340f,0.391f),new Vector2(0.348f,0.393f),new Vector2(0.320f,0.388f),new Vector2(0.305f,0.391f)}};
        static readonly Vector2[,] Mouths={
            {new Vector2(0.414f,0.492f),new Vector2(0.402f,0.492f),new Vector2(0.398f,0.492f),new Vector2(0.387f,0.495f),new Vector2(0.418f,0.474f),new Vector2(0.410f,0.469f),new Vector2(0.391f,0.474f),new Vector2(0.387f,0.477f)},
            {new Vector2(0.426f,0.474f),new Vector2(0.410f,0.471f),new Vector2(0.398f,0.474f),new Vector2(0.383f,0.477f),new Vector2(0.418f,0.458f),new Vector2(0.410f,0.458f),new Vector2(0.395f,0.458f),new Vector2(0.383f,0.469f)}};
        static readonly Vector2[,] Chins={
            {new Vector2(0.457f,0.568f),new Vector2(0.441f,0.568f),new Vector2(0.441f,0.568f),new Vector2(0.430f,0.565f),new Vector2(0.461f,0.549f),new Vector2(0.461f,0.549f),new Vector2(0.434f,0.552f),new Vector2(0.426f,0.555f)},
            {new Vector2(0.457f,0.555f),new Vector2(0.465f,0.552f),new Vector2(0.441f,0.557f),new Vector2(0.426f,0.557f),new Vector2(0.465f,0.542f),new Vector2(0.457f,0.544f),new Vector2(0.438f,0.544f),new Vector2(0.422f,0.552f)}};
        static readonly Vector2[,] Ears={
            {new Vector2(0.758f,0.383f),new Vector2(0.742f,0.383f),new Vector2(0.730f,0.383f),new Vector2(0.719f,0.380f),new Vector2(0.762f,0.359f),new Vector2(0.754f,0.365f),new Vector2(0.730f,0.367f),new Vector2(0.719f,0.362f)},
            {new Vector2(0.762f,0.357f),new Vector2(0.754f,0.359f),new Vector2(0.742f,0.362f),new Vector2(0.719f,0.365f),new Vector2(0.754f,0.344f),new Vector2(0.746f,0.344f),new Vector2(0.730f,0.349f),new Vector2(0.711f,0.352f)}};
        static readonly Vector2[,] Skulls={
            {new Vector2(0.531f,0.102f),new Vector2(0.512f,0.102f),new Vector2(0.496f,0.102f),new Vector2(0.496f,0.104f),new Vector2(0.531f,0.083f),new Vector2(0.520f,0.081f),new Vector2(0.504f,0.081f),new Vector2(0.488f,0.083f)},
            {new Vector2(0.535f,0.076f),new Vector2(0.535f,0.076f),new Vector2(0.508f,0.078f),new Vector2(0.508f,0.078f),new Vector2(0.531f,0.065f),new Vector2(0.527f,0.065f),new Vector2(0.504f,0.068f),new Vector2(0.500f,0.070f)}};
        // Neck roots remain in the authored shoulder pose when the face turns.
        // Independently matched at the painted nape and lower throat.
        static readonly Vector4[,] NeckGuides={
            {new Vector4(0.4200f,0.5666f,0.7382f,0.3846f),new Vector4(0.3993f,0.5608f,0.7223f,0.3830f),new Vector4(0.3862f,0.5608f,0.7082f,0.3886f),new Vector4(0.3761f,0.5712f,0.6893f,0.3866f),new Vector4(0.4250f,0.5532f,0.7421f,0.3630f),new Vector4(0.4189f,0.5512f,0.7145f,0.3754f),new Vector4(0.3833f,0.5428f,0.6965f,0.3696f),new Vector4(0.3743f,0.5484f,0.6951f,0.3624f)},
            {new Vector4(0.4259f,0.5504f,0.7430f,0.3564f),new Vector4(0.4188f,0.5582f,0.7359f,0.3574f),new Vector4(0.3950f,0.5586f,0.7131f,0.3650f),new Vector4(0.3781f,0.5550f,0.6961f,0.3660f),new Vector4(0.4309f,0.5452f,0.7400f,0.3430f),new Vector4(0.4159f,0.5426f,0.7162f,0.3476f),new Vector4(0.3970f,0.5410f,0.7100f,0.3520f),new Vector4(0.3773f,0.5448f,0.6931f,0.3504f)}};
        public static Features Landmarks(Appearance look)
        {
            int s=look.sex,f=look.parts[0];var near=NearEyes[s,f];var far=FarEyes[s,f];var mouth=Mouths[s,f];var ear=Ears[s,f];
            // Keep the measured feature centres, but fit each edit's footprint
            // to this face's eye span and lower-face height. A shared footprint
            // pulled an older/narrower face's neighbouring features into edits.
            float width=Mathf.Clamp((near.x-far.x)/.192f,.90f,1.08f);
            float lower=Mathf.Clamp((Chins[s,f].y-mouth.y)/.076f,.90f,1.10f);
            float earHeight=Mathf.Clamp((ear.y-Skulls[s,f].y)/.281f,.93f,1.06f);
            return new Features{
                eyeFar=new Vector4(far.x,far.y,.050f*width,.027f),eyeNear=new Vector4(near.x,near.y,.070f*width,.031f),
                browFar=new Vector4(far.x-.006f,far.y-.034f,.057f*width,.024f),browNear=new Vector4(near.x-.004f,near.y-.038f,.079f*width,.025f),
                nose=new Vector4(Noses[s,f].x,Noses[s,f].y,.070f*width,.068f*lower),mouth=new Vector4(mouth.x,mouth.y,.089f*width,.033f*lower),
                earFar=new Vector4(far.x-.040f,ear.y,.018f*width,.032f*earHeight),earNear=new Vector4(ear.x,ear.y,.061f*width,.065f*earHeight),
                chin=Chins[s,f],skull=Skulls[s,f],hairline=new Vector2(near.x+.018f,near.y-.106f),
                lipFar=mouth+new Vector2(-.054f*width,-.002f),lipNear=mouth+new Vector2(.073f*width,.002f)};
        }
        static Vector2 Morph(Vector2 p,Vector4 feature,Vector4 shape,float tiltMultiplier=1)
        {
            var d=p-new Vector2(feature.x,feature.y);
            float weight=1-Smooth(.12f,1,new Vector2(d.x/feature.z,d.y/feature.w).magnitude);
            var transformed=new Vector2(d.x/shape.x,d.y/shape.y+d.x*shape.z*tiltMultiplier);
            return (transformed-d)*weight;
        }
        // This inverse mapping is shared by the face and facial hair. In
        // particular a nose edit must also move the moustache's upper edge.
        public static Vector2 FeatureSamplingPoint(Appearance look,Vector2 p)
        {
            var f=Landmarks(look);var eye=AppearancePartCatalog.Shape(2,look.parts[2]);var brow=AppearancePartCatalog.Shape(3,look.parts[3]);
            return p+Morph(p,f.eyeFar,eye,.70f)+Morph(p,f.eyeNear,eye,-1)
                +Morph(p,f.browFar,brow,.70f)+Morph(p,f.browNear,brow,-1)
                +Morph(p,f.nose,AppearancePartCatalog.Shape(4,look.parts[4]))
                +Morph(p,f.mouth,AppearancePartCatalog.Shape(5,look.parts[5]))
                +EarOffset(look,p,f);
        }
        static Vector2 EarOffset(Appearance look,Vector2 p,Features f)
        {
            var shape=AppearancePartCatalog.Shape(13,look.ears);
            return Morph(p,f.earFar,shape)+Morph(p,f.earNear,shape);
        }
        public static Vector2 EarSamplingPoint(Appearance look,Vector2 p){return p+EarOffset(look,p,Landmarks(look));}
        public static float HairEarAlpha(Appearance look,Vector2 p)
        {
            var f=Landmarks(look);var sample=EarSamplingPoint(look,p)-new Vector2(f.earNear.x,f.earNear.y);
            return Smooth(.45f,1,new Vector2(sample.x/(f.earNear.z*.58f),sample.y/(f.earNear.w*.65f)).magnitude);
        }
        public static float BeardLipAlpha(Appearance look,Vector2 p)
        {
            var f=Landmarks(look);var sample=FeatureSamplingPoint(look,p)-new Vector2(f.mouth.x,f.mouth.y);
            return Smooth(.65f,1,new Vector2(sample.x/(f.mouth.z*(.058f/.089f)),sample.y/(f.mouth.w*(.010f/.033f))).magnitude);
        }
        public static Rect Cell(int column,int row,int columns,int rows)
        {
            if(columns<1||rows<1||column<0||column>=columns||row<0||row>=rows)throw new ArgumentOutOfRangeException("Portrait atlas cell");
            return new Rect((float)column/columns,1f-(float)(row+1)/rows,1f/columns,1f/rows);
        }
        public static Rect Base(Appearance look){return Cell(look.parts[0]%4,look.sex*2+look.parts[0]/4,4,4);}
        public static Rect HairCanvas(Appearance look)
        {
            if(look.sex==0&&look.parts[1]==2)return new Rect(0,0,1.04f,1.07f);
            if(look.sex==0&&look.parts[1]==3)return new Rect(.05f,0,.95f,1.07f);
            if(look.sex==0&&look.parts[1]==6)return new Rect(0,.09f,1.04f,.97f);
            if(look.sex==0&&look.parts[1]==7)return new Rect(.05f,.09f,.95f,.91f);
            if(look.sex==1&&look.parts[1]==2)return new Rect(0,.08f,1,.92f);
            return new Rect(0,0,1,1);
        }
        public static Rect Hair(Appearance look)
        {
            var cell=Cell(look.parts[1]%4,look.sex*2+look.parts[1]/4,4,4);var bounds=HairCanvas(look);
            return new Rect(cell.x+bounds.x*cell.width,cell.y+(1-bounds.yMax)*cell.height,cell.width*bounds.width,cell.height*bounds.height);
        }
        public static Rect Outfit(Appearance look){return Cell(look.parts[7]%4,look.sex*2+look.parts[7]/4,4,4);}
        // Column 1 contains a 2-3 pixel sliver of column 0, separated from its
        // own shoulder by transparent pixels 316-318. Fade only that gap;
        // the actual garment begins at pixel 319 and keeps its full width.
        public static float OutfitEdgeAlpha(Appearance look,Vector2 source)
        {return look.parts[7]%4==1?Smooth(.0128f,.0160f,source.x):1;}
        // Source canvases select the complete silhouette with transparent
        // margins. They no longer need per-pixel patches from the old atlas.
        public static float HairSourceAlpha(Appearance look,Vector2 source)
        {return 1;}
        // The long beard has a deliberately taller transparent source canvas.
        // The following row begins below its tip; crop that row's empty margin
        // so the neighbouring long tip cannot enter the three-strand beard.
        public static Rect BeardCanvas(Appearance look)
        {return look.parts[6]==2?new Rect(0,0,1,1.16f):look.parts[6]==6?new Rect(0,.17f,1,.83f):new Rect(0,0,1,1);}
        public static Rect Beard(Appearance look)
        {
            int index=look.parts[6]==0?7:Mathf.Clamp(look.parts[6]-1,0,6);
            var cell=Cell(index%4,index/4,4,2);var bounds=BeardCanvas(look);
            return new Rect(cell.x+bounds.x*cell.width,cell.y+(1-bounds.yMax)*cell.height,cell.width*bounds.width,cell.height*bounds.height);
        }
        public static Rect HairRect(Appearance look)
        {
            const float scale=.82f;var b=HairCanvas(look);var source=HairSource(look,0);var target=HairTarget(look,0);
            return new Rect(target.x+(b.x-source.x)*scale,target.y+(b.y-source.y)*scale,b.width*scale,b.height*scale);
        }
        public static Rect BeardRect(Appearance look){return new Rect(0,0,1,1);}
        public static Rect OutfitRect(Appearance look){return new Rect(-.185f,look.sex==0?.424f:.416f,1.42f,1.42f*2f/3);}
        // Every generated garment has its own collar height. Bind the far tip,
        // nape tip and opening bottom; a shared rectangle alone cannot do this.
        static readonly Vector2[,] CollarSources={
            {new Vector2(.410f,.156f),new Vector2(.697f,.140f),new Vector2(.454f,.408f)},
            {new Vector2(.420f,.162f),new Vector2(.703f,.137f),new Vector2(.462f,.357f)},
            {new Vector2(.415f,.162f),new Vector2(.690f,.134f),new Vector2(.447f,.389f)},
            {new Vector2(.412f,.159f),new Vector2(.680f,.134f),new Vector2(.447f,.382f)},
            {new Vector2(.410f,.077f),new Vector2(.691f,.042f),new Vector2(.460f,.291f)},
            {new Vector2(.414f,.077f),new Vector2(.684f,.045f),new Vector2(.481f,.272f)},
            {new Vector2(.411f,.083f),new Vector2(.687f,.048f),new Vector2(.454f,.297f)},
            {new Vector2(.412f,.083f),new Vector2(.680f,.048f),new Vector2(.454f,.307f)},
            {new Vector2(.433f,.083f),new Vector2(.680f,.058f),new Vector2(.475f,.304f)},
            {new Vector2(.429f,.067f),new Vector2(.671f,.048f),new Vector2(.481f,.220f)},
            {new Vector2(.421f,.083f),new Vector2(.677f,.058f),new Vector2(.458f,.294f)},
            {new Vector2(.433f,.077f),new Vector2(.696f,.048f),new Vector2(.463f,.304f)},
            {new Vector2(.420f,.025f),new Vector2(.672f,.006f),new Vector2(.478f,.239f)},
            {new Vector2(.411f,.029f),new Vector2(.675f,.016f),new Vector2(.484f,.213f)},
            {new Vector2(.419f,.029f),new Vector2(.687f,.010f),new Vector2(.473f,.274f)},
            {new Vector2(.413f,.029f),new Vector2(.673f,.013f),new Vector2(.473f,.271f)}};
        public static Vector2 CollarSource(Appearance look,int anchor){return CollarSources[look.sex*8+look.parts[7],anchor];}
        public static Vector2 CollarTarget(Appearance look,int anchor)
        {
            var neck=NeckGuides[look.sex,look.parts[0]];var front=new Vector2(neck.x,neck.y);
            if(anchor==0)return front+new Vector2(-.015f,.025f);
            if(anchor==1)return new Vector2(neck.z+.010f,neck.w+.165f);
            // Standing collar and scarf close higher than a crossed robe.
            float depth=look.parts[7]==1?.090f:look.parts[7]==2||look.parts[7]==5?.125f:.175f;
            return front+new Vector2(.045f,depth);
        }
        public static Vector4 CollarBinding(Appearance look,int anchor)
        {
            var source=CollarSource(look,anchor);var rect=OutfitRect(look);var target=CollarTarget(look,anchor);
            return new Vector4(target.x,target.y,target.x-rect.x-source.x*rect.width,target.y-rect.y-source.y*rect.height);
        }
        public static Vector2 OutfitSamplingPoint(Appearance look,Vector2 p)
        {
            Vector2 delta=Vector2.zero;float total=0;
            for(int i=0;i<8;i++)
            {
                var b=OutfitBinding(look,i);
                var d=p-new Vector2(b.x,b.y);d.y*=1.5f;float weight=1/(d.sqrMagnitude+.0000001f);
                delta+=new Vector2(b.z,b.w)*weight;total+=weight;
            }
            var rect=OutfitRect(look);var source=p-delta/total;
            return new Vector2((source.x-rect.x)/rect.width,(source.y-rect.y)/rect.height);
        }
        // Shoulder constraints keep a collar correction local. The old chest
        // anchors were too low: moving a collar also dragged the entire upper
        // torso, producing a raised near shoulder and a narrow far shoulder.
        public static Vector4 OutfitBinding(Appearance look,int anchor)
        {
            if(anchor<3)return CollarBinding(look,anchor);
            var rect=OutfitRect(look);
            if(anchor<5)
            {
                bool near=anchor==4;
                float row=look.parts[7]<4?0:.075f;
                var source=new Vector2(near?.93f:.075f,(near?.30f:.40f)-row-(look.sex==1?.055f:0));
                var original=new Vector2(rect.x+source.x*rect.width,rect.y+source.y*rect.height);
                var offset=new Vector2(0,near?.034f:-.012f);
                var target=original+offset;
                return new Vector4(target.x,target.y,offset.x,offset.y);
            }
            return anchor==5?new Vector4(-.12f,1.12f,0,0):anchor==6?new Vector4(1.17f,1.12f,0,0):new Vector4(.50f,1.35f,0,0);
        }
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
            {new Vector2(.535f,.365f),new Vector2(.430f,.535f),new Vector2(.790f,.730f),new Vector2(.215f,.655f)},
            {new Vector2(.508f,.365f),new Vector2(.449f,.526f),new Vector2(.742f,.719f),new Vector2(.184f,.646f)},
            {new Vector2(.469f,.344f),new Vector2(.441f,.508f),new Vector2(.750f,.721f),new Vector2(.121f,.633f)},
            {new Vector2(.520f,.385f),new Vector2(.383f,.552f),new Vector2(.687f,.732f),new Vector2(.113f,.656f)},
            {new Vector2(.550f,.281f),new Vector2(.441f,.500f),new Vector2(.785f,.667f),new Vector2(.234f,.604f)},
            {new Vector2(.520f,.310f),new Vector2(.449f,.503f),new Vector2(.746f,.664f),new Vector2(.191f,.607f)},
            {new Vector2(.508f,.307f),new Vector2(.438f,.484f),new Vector2(.738f,.667f),new Vector2(.129f,.599f)},
            {new Vector2(.516f,.320f),new Vector2(.434f,.513f),new Vector2(.680f,.677f),new Vector2(.145f,.615f)},
            {new Vector2(.550f,.302f),new Vector2(.418f,.456f),new Vector2(.785f,.651f),new Vector2(.227f,.563f)},
            {new Vector2(.516f,.292f),new Vector2(.434f,.464f),new Vector2(.746f,.667f),new Vector2(.191f,.578f)},
            {new Vector2(.523f,.289f),new Vector2(.430f,.458f),new Vector2(.730f,.661f),new Vector2(.141f,.573f)},
            {new Vector2(.516f,.263f),new Vector2(.422f,.448f),new Vector2(.691f,.630f),new Vector2(.141f,.568f)},
            {new Vector2(.547f,.266f),new Vector2(.438f,.393f),new Vector2(.785f,.575f),new Vector2(.230f,.505f)},
            {new Vector2(.516f,.221f),new Vector2(.438f,.393f),new Vector2(.738f,.589f),new Vector2(.184f,.505f)},
            {new Vector2(.512f,.230f),new Vector2(.430f,.404f),new Vector2(.734f,.594f),new Vector2(.148f,.510f)},
            {new Vector2(.516f,.227f),new Vector2(.438f,.406f),new Vector2(.710f,.585f),new Vector2(.145f,.505f)}};
        public static Vector2 HairSource(Appearance look,int anchor){return HairSources[look.sex*HairCount+look.parts[1],anchor];}
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
            var p=HairTarget(look,0);int hair=look.parts[1];
            // Compact the ornament above the registered crown, never the
            // skull or face. Keep headwear inside both portrait frames.
            float scale=look.sex==1?(hair==0?.74f:hair==4?.60f:1):(hair==0?.80f:hair==3?.85f:hair==5?.86f:1);
            return new Vector4(p.x,p.y,scale,0);
        }
        public static float FarScalpPad(Appearance look)
        {
            int hair=look.parts[1];
            // The new tied silhouettes narrow at the far upper temple. Fill
            // only that skull arc; measured hairline, eye and ear stay fixed.
            return hair==0||hair==1||hair==4||hair==5?.055f:.016f;
        }
        static float Smooth(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3-2*t);}
        public static Vector2 HairSamplingPoint(Appearance look,Vector2 p)
        {
            // The opening and the visible ear use one deformation; changing
            // ear size must not leave a strip of hair over the new silhouette.
            p=EarSamplingPoint(look,p);
            var crown=CrownAdjustment(look);var center=new Vector2(crown.x,crown.y);
            var f=Landmarks(look);
            float scalp=(1-Smooth(f.eyeFar.x+.055f,f.eyeNear.x-.015f,p.x))*(1-Smooth(f.browFar.y-.055f,f.browFar.y-.010f,p.y))*Smooth(crown.y-.035f,crown.y+.035f,p.y);
            p.x+=scalp*FarScalpPad(look);
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
        // Lip opening and underlying chin are measured independently in each
        // new painting. A common source pivot stretched a short moustache into
        // a handlebar and made the long beard float below a narrow face.
        static readonly Vector4[] BeardSources={
            new Vector4(.483f,.523f,.489f,.657f),
            new Vector4(.439f,.503f,.483f,.674f),
            new Vector4(.466f,.572f,.486f,.704f),
            new Vector4(.438f,.562f,.481f,.731f),
            new Vector4(.477f,.495f,.484f,.600f),
            new Vector4(.493f,.464f,.500f,.643f),
            new Vector4(.508f,.501f,.528f,.633f)};
        public static Vector2 BeardSource(Appearance look,int anchor)
        {
            var source=BeardSources[Mathf.Clamp(look.parts[6]-1,0,BeardSources.Length-1)];var canvas=BeardCanvas(look);
            var p=anchor==0?new Vector2(source.x,source.y):new Vector2(source.z,source.w);
            return new Vector2((p.x-canvas.x)/canvas.width,(p.y-canvas.y)/canvas.height);
        }
        public static Vector4 BeardMouth(Appearance look)
        {var f=Landmarks(look);var source=BeardSource(look,0);return new Vector4(f.mouth.x,f.mouth.y,source.x,source.y);}
        public static Vector4 BeardChin(Appearance look)
        {var f=Landmarks(look);var source=BeardSource(look,1);return new Vector4(f.chin.x,f.chin.y,source.x,source.y);}
        public static Vector4 BeardScales(Appearance look)
        {
            var f=Landmarks(look);var canvas=BeardCanvas(look);float width=look.parts[6]==3?.78f:1;
            float lower=look.parts[6]==2?.45f:look.parts[6]==6?.44f:.35f;
            return new Vector4((f.mouth.x-f.lipFar.x)/.170f*canvas.width*width,(f.lipNear.x-f.mouth.x)/.180f*canvas.width*width,.28f*canvas.height,lower*canvas.height);
        }
        public static float BeardJawWidth(Appearance look)
        {
            var f=Landmarks(look);
            return (look.parts[6]==4?.64f:.52f)*(look.parts[6]==3?.78f:1)*Mathf.Clamp((f.earNear.x-f.chin.x)/.301f,.90f,1.08f)*BeardCanvas(look).width;
        }
        public static Vector2 BeardSamplingPoint(Appearance look,Vector2 p)
        {
            p=FeatureSamplingPoint(look,p);
            var m=BeardMouth(look);var c=BeardChin(look);var scale=BeardScales(look);
            float t=Mathf.Clamp01((p.y-m.y)/(c.y-m.y));float center=Mathf.Lerp(m.x,c.x,t),sourceCenter=Mathf.Lerp(m.z,c.z,t);
            float sy=p.y<m.y?m.w+(p.y-m.y)/scale.z:p.y>c.y?c.w+(p.y-c.y)/scale.w:Mathf.Lerp(m.w,c.w,t);
            float sxScale=Mathf.Lerp(p.x<center?scale.x:scale.y,BeardJawWidth(look),t);
            return new Vector2(sourceCenter+(p.x-center)/sxScale,sy);
        }
    }
}
