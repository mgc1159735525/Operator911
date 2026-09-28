using UnityEngine;
namespace Minglu
{
    // Source landmarks are in top-left atlas pixels; destination uses a 400 x 690 portrait.
    // Keep source rectangles and registration separate from UI size/cropping.
    public static class PortraitLayout
    {
        public static Rect Head(Appearance look)
        {
            float size=look.sex==0?240:232;
            return new Rect((400-size)/2,298-size,size,size);
        }
        public static Vector4 Landmarks(int sex,int face)
        {
            Vector4[] male={new Vector4(.503f,.512f,.453f,.733f),new Vector4(.501f,.510f,.450f,.737f),new Vector4(.506f,.511f,.453f,.737f),new Vector4(.506f,.512f,.453f,.739f),new Vector4(.490f,.516f,.453f,.735f),new Vector4(.492f,.507f,.443f,.737f),new Vector4(.493f,.515f,.448f,.737f),new Vector4(.499f,.513f,.450f,.739f)};
            Vector4[] female={new Vector4(.504f,.491f,.430f,.701f),new Vector4(.507f,.494f,.433f,.701f),new Vector4(.505f,.494f,.433f,.709f),new Vector4(.505f,.493f,.432f,.709f),new Vector4(.487f,.479f,.421f,.686f),new Vector4(.492f,.490f,.428f,.704f),new Vector4(.490f,.493f,.434f,.708f),new Vector4(.490f,.491f,.430f,.708f)};
            return (sex==0?male:female)[face];
        }
        public static readonly Rect[,] HairSource={
            {new Rect(0,0,384,512),new Rect(384,0,384,512),new Rect(768,0,384,512),new Rect(1152,0,384,512)},
            {new Rect(0,512,384,512),new Rect(384,512,384,512),new Rect(768,512,384,512),new Rect(1152,512,384,512)},
            {new Rect(93,832,229,168),new Rect(450,831,210,230),new Rect(804,810,214,276),new Rect(1157,876,203,61)}};
        public static readonly Vector2[,] HairAnchor={
            {new Vector2(195,230),new Vector2(578,219),new Vector2(968,199),new Vector2(1345,230)},
            {new Vector2(196,690),new Vector2(578,693),new Vector2(959,695),new Vector2(1340,662)},
            {new Vector2(201,920),new Vector2(554,916),new Vector2(905,868),new Vector2(1260,917)}};
        public static readonly float[,] HairScale={{.515f,.525f,.535f,.515f},{.515f,.52f,.515f,.53f},{.52f,.52f,.52f,.53f}};
        // Compress only the crown above the hairline, keeping temples and lower locks fixed.
        public static readonly float[,] CrownHeight={{.93f,.92f,.94f,.96f},{.93f,.94f,.95f,.94f}};
        public static readonly float[,] Hairline={{.335f,.325f,.33f,.345f},{.33f,.325f,.33f,.325f}};
        // Opaque temple width measured at 40% of each face crop, excluding ears.
        // Expansion faces are broader: fit the hair to the face, not to atlas padding.
        static readonly float[,] TempleWidth={{146,146,148,149,153,157,154,156},{147,147,148,149,158,156,157,158}};
        public static Rect Grooming(int row,int column,Appearance look)
        {
            var src=HairSource[row,column];var anchor=HairAnchor[row,column];var face=Landmarks(look.sex,look.parts[0]);
            var head=Head(look);
            float faceFit=row==2?TempleWidth[0,look.parts[0]]/147f:TempleWidth[look.sex,look.parts[0]]/147f;
            float factor=HairScale[row,column]*head.width/280f*faceFit;
            var target=new Vector2(head.x+head.width*face.x,head.y+head.height*(row==2?face.w:Hairline[look.sex,column]-.025f));
            return new Rect(target.x-(anchor.x-src.x)*factor,target.y-(anchor.y-src.y)*factor,src.width*factor,src.height*factor);
        }
        public static Vector2 NeckFit(Appearance look)
        {
            float[,] centers={{.502f,.5f,.5f,.498f,.497f,.503f,.495f,.498f},{.503f,.502f,.5f,.502f,.5f,.5f,.498f,.498f}};
            float[,] widths={{.347f,.363f,.376f,.379f,.344f,.395f,.36f,.373f},{.312f,.322f,.325f,.322f,.325f,.318f,.322f,.322f}};
            return new Vector2(centers[look.sex,look.parts[0]],widths[look.sex,look.parts[0]]);
        }
        public static Rect Neck(Appearance look)
        {
            var head=Head(look);var fit=NeckFit(look);var face=Landmarks(look.sex,look.parts[0]);
            float width=head.width*fit.y/.70f;
            return new Rect(head.x+head.width*fit.x-width/2,head.y+head.height*(face.w+.04f),width,136);
        }
        public static float GarmentTop(int sex,int outfit)
        {
            // Measured opaque centerline of each collar, relative to its source crop.
            // Attach that closure to the natural neck base rather than the image's top edge.
            float[] male={52,40,54,53,49,47,43,48};
            float[] female={51,30,38,50,45,48,40,53};
            float sourceScale=400f/(1254f*.248f);
            // Closed standing collars sit higher than open crossed collars; one common
            // center closure pulled the standing collar away from the neck.
            float[] maleClosure={311,298,309,311,311,300,311,310};
            float[] femaleClosure={307,294,305,307,307,297,307,306};
            float neckClosure=(sex==0?maleClosure:femaleClosure)[outfit];
            return neckClosure-(sex==0?male:female)[outfit]*sourceScale;        }
    }
}


