using System;
using UnityEngine;

namespace Minglu
{
    // The base is a continuous 2:3 painting. Overlays carry registration in
    // that frame; square outfit cells retain square pixels when fitted to it.
    public static class PaintedPortraitLayout
    {
        public const float Width=400, Height=600;
        public static readonly Rect DetailCrop=new Rect(.06f,-.045f,.88f,.84f);
        public static readonly Rect FullCrop=new Rect(-.035f,-.090f,1.07f,1.10f);

        public static Rect Cell(int column,int row,int columns,int rows)
        {
            if(columns<1||rows<1||column<0||column>=columns||row<0||row>=rows)
                throw new ArgumentOutOfRangeException("Portrait atlas cell");
            return new Rect((float)column/columns,1f-(float)(row+1)/rows,1f/columns,1f/rows);
        }
        public static Rect Base(Appearance look){return Cell(look.parts[0]%4,look.sex*2+look.parts[0]/4,4,4);}
        public static Rect Hair(Appearance look)
        {
            var cell=Cell(look.parts[1],look.sex,4,2);var bounds=HairCanvas(look);
            return new Rect(cell.x+bounds.x*cell.width,cell.y+(1-bounds.yMax)*cell.height,cell.width*bounds.width,cell.height*bounds.height);
        }
        public static Rect Outfit(Appearance look){return Cell(look.parts[7]%4,look.sex*2+look.parts[7]/4,4,4);}
        public static Rect Beard(Appearance look){return Cell(Mathf.Clamp(look.parts[6]-1,0,2),0,3,1);}

        // Transparent atlas whitespace is not anatomy. Calibrate the hair's
        // scalp and ear opening with an isotropic transform in the base canvas.
        static Rect HairCanvas(Appearance look)
        {
            if(look.sex==0&&look.parts[1]==0)return new Rect(0,0,1,.92f);
            if(look.sex==0&&look.parts[1]==1)return new Rect(0,0,.94f,.92f);
            if(look.sex==1&&look.parts[1]==0)return new Rect(0,-.042f,1,1.042f);
            return new Rect(0,0,1,1);
        }
        public static Rect HairRect(Appearance look)
        {
            float[] maleScale={.780f,.780f,.780f,.804f};
            float[] femaleScale={.800f,.815f,.795f,.800f};
            float[] maleX={.165f,.165f,.165f,.162f};
            float[] femaleX={.142f,.142f,.151f,.146f};
            float[] maleY={-.052f,-.052f,-.052f,-.060f};
            float[] femaleY={-.020f,-.031f,-.017f,-.016f};
            int hair=look.parts[1];bool woman=look.sex==1;
            float authoredScale=(woman?femaleScale:maleScale)[hair];
            // The elderly bases are shifted left; the second row of male heads
            // is also broader. Fit around the near-ear anchor rather than
            // enlarging outward from the top-left corner of the UI frame.
            float[] maleFit={1,1.015f,1.015f,1.010f,1.033f,1.055f,1.074f,1.049f};
            float scale=authoredScale*(woman?1:maleFit[look.parts[0]]);
            float x=(woman?femaleX:maleX)[hair]+FaceX(look)-.75f*(scale-authoredScale);
            float y=(woman?femaleY:maleY)[hair]+FaceOffset(look)-(woman?.48f:.565f)*(scale-authoredScale);
            var bounds=HairCanvas(look);
            return new Rect(x+bounds.x*scale,y+bounds.y*scale,bounds.width*scale,bounds.height*scale);
        }
        public static Rect BeardRect(Appearance look)
        {
            float scale=look.parts[6]==3?.41f:.49f;
            var mouth=Landmarks(look).mouth;
            return new Rect(mouth.x-.400f*scale,mouth.y-.382f*scale,scale,scale);
        }
        public static Rect OutfitRect(Appearance look)
        {
            // Outfit cells are square; the base canvas is 2:3. This height keeps
            // the clothing pixels square while leaving the painted neck intact.
            float width=1.27f;
            // Source collar openings are narrower than the base shoulders.
            // Register the two collar tips, keeping garment scaling isotropic.
            return new Rect(-.112f,look.sex==0?.424f:.416f,width,width*2f/3);
        }
        public static Rect Register(Rect frame,Rect registration)
        {return new Rect(frame.x+frame.width*registration.x,frame.y+frame.height*registration.y,frame.width*registration.width,frame.height*registration.height);}

        public static Rect Placement(float width,float height,bool detail)
        {
            var crop=detail?DetailCrop:FullCrop;
            float scale=Mathf.Min(width/(Width*crop.width),height/(Height*crop.height));
            float x=(width-Width*crop.width*scale)/2-Width*crop.x*scale;
            float y=(height-Height*crop.height*scale)/2-Height*crop.y*scale;
            return new Rect(x,y,Width*scale,Height*scale);
        }

        // Coordinates use a top-left origin in one full portrait cell. The far
        // eye is smaller and higher: these are not mirrored frontal features.
        // Each face can be registered here without altering saved part indexes.
        public struct Features
        {
            public Vector4 eyeFar,eyeNear,browFar,browNear,nose,mouth,earFar,earNear;
        }
        static float FaceOffset(Appearance look)
        {
            // Measured against the generated 256 x 384 cells. Male faces in the
            // second row sit a few pixels higher; the female nose and chin are
            // shorter. Each row preserves the full original neck and shoulders.
            float[] maleY={0,0,.002f,.003f,-.011f,-.013f,-.010f,-.006f};
            float[] femaleY={0,0,.002f,.001f,.001f,.002f,.002f,.003f};
            return (look.sex==0?maleY:femaleY)[look.parts[0]];
        }
        static float FaceX(Appearance look)
        {
            float[] male={0,-.010f,-.029f,-.041f,-.004f,-.014f,-.030f,-.040f};
            float[] female={0,-.006f,-.030f,-.036f,-.004f,-.011f,-.028f,-.032f};
            return (look.sex==0?male:female)[look.parts[0]];
        }
        public static Features Landmarks(Appearance look)
        {
            float dy=FaceOffset(look),dx=FaceX(look);
            bool woman=look.sex==1;
            return new Features
            {
                eyeFar=new Vector4(.289f+dx*1.25f,(woman?.330f:.342f)+dy,.055f,.035f),
                eyeNear=new Vector4(.450f+dx,(woman?.315f:.335f)+dy,.077f,.037f),
                browFar=new Vector4(.278f+dx*1.25f,(woman?.297f:.308f)+dy,.061f,.029f),
                browNear=new Vector4(.447f+dx,(woman?.280f:.298f)+dy,.087f,.030f),
                nose=new Vector4(.298f+dx*1.15f,(woman?.413f:.433f)+dy,.073f,.063f),
                mouth=new Vector4(.378f+dx*.90f,(woman?.476f:.497f)+dy,.092f,.037f),
                earFar=new Vector4(.242f+dx*1.25f,(woman?.378f:.391f)+dy,.022f,.048f),
                earNear=new Vector4(.750f+dx,(woman?.365f:.385f)+dy,.063f,.070f)
            };
        }
    }
}
