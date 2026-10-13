using UnityEngine;
namespace Minglu
{
    // Authored on one 400 x 600 canvas. Heads include their own hair and neck;
    // no independent hairstyle is stretched over a different face.
    public static class ClassicPortraitCatalog
    {
        public const int HeadCount=12;
        public static readonly string[] ResourcePaths={"Minglu/Art/Classic/heads-male","Minglu/Art/Classic/heads-female","Minglu/Art/Classic/features","Minglu/Art/Classic/outfits","Minglu/Art/Classic/details","Minglu/Art/Classic/backdrops","Minglu/Art/Classic/brows"};
        public static readonly Rect HeadRect=new Rect(40,12,320,320),OutfitRect=new Rect(0,185,400,400);
        // Frame references in each 362-pixel head cell: face centre x,
        // upper eye-socket y, lip y. A crop origin is NOT a pupil landmark;
        // FeatureRect registers the painted pupils and nose base separately.
        static readonly Vector3[] Male={new Vector3(173,191,266),new Vector3(173,190,266),new Vector3(173,196,269),new Vector3(173,191,266),new Vector3(173,191,268),new Vector3(174,192,268),new Vector3(173,197,269),new Vector3(174,192,268),new Vector3(173,194,270),new Vector3(175,194,270),new Vector3(175,194,270),new Vector3(173,197,269)};
        static readonly Vector3[] Female={new Vector3(171,191,265),new Vector3(171,191,265),new Vector3(171,191,265),new Vector3(171,191,265),new Vector3(172,191,266),new Vector3(172,191,266),new Vector3(172,194,268),new Vector3(172,194,268),new Vector3(172,194,268),new Vector3(174,196,271),new Vector3(174,196,271),new Vector3(174,196,271)};
        // Shift the whole facial assembly together in source-head pixels:
        // eyes, brows, nose, mouth, beards and face-attached adornments.
        // Eye and mouth lines share the same offset, preserving proportions.
        public static Vector3 Landmarks(Appearance look)
        {
            var anchors=(look.sex==0?Male:Female)[look.parts[0]];
            return anchors+new Vector3(-8,-6,-6);
        }
        public static Rect Cell(int index,int columns,int rows)
        {return new Rect((index%columns)/(float)columns,1-(index/columns+1)/(float)rows,1f/columns,1f/rows);}
        // Repainted lower atlas rows contain a few pixels from the preceding
        // neck. Keep the full head coordinate frame and exclude its gutter.
        static readonly float[] FemaleTopTrim={0,0,0,0,4,10,7,8,0,5,4,6};
        public static float HeadTopTrim(Appearance a)
        {return a.sex==1?FemaleTopTrim[a.parts[0]]/362f:0;}
        public static float HeadLeftTrim(Appearance a)
        {return (a.parts[0]==11?28:a.sex==1&&a.parts[0]==2?22:a.sex==1&&a.parts[0]==6?30:0)/362f;}
        public static Rect SourcePixels(float x,float y,float w,float h,float tw,float th)
        {return new Rect(x/tw,1-(y+h)/th,w/tw,h/th);}
        public static Rect FromHead(Rect r)
        {float s=HeadRect.width/362;return new Rect(HeadRect.x+r.x*s,HeadRect.y+r.y*s,r.width*s,r.height*s);}
        public static Rect FeatureRect(Appearance a,int part)
        {
            var p=Landmarks(a);
            switch(part){case 2:
                    // Male/female atlas rows have different vertical padding.
                    // Register the painted pupil line, not the crop centre;
                    // reusing the male offset makes female lids hit the brows.
                    const float eyeWidth=126,eyeHeight=52;
                    float pupilInCrop=a.sex==0?44:29, pupilY=p.y+(a.sex==0?8:3);
                    return FromHead(new Rect(p.x-eyeWidth*.5f,pupilY-pupilInCrop/64*eyeHeight,eyeWidth,eyeHeight));
                case 3:return FromHead(new Rect(p.x-69,p.y-17,138,19));
                // Measured columella baselines differ between the two rows
                // (male ~126 px, female ~118 px of the 148 px crop). Keep
                // each row's authored nose/lip spacing, excluding its padding.
                case 4:
                    float top=p.y-7, noseBase=p.z-(a.sex==0?18:22), sourceBase=a.sex==0?126:118;
                    return FromHead(new Rect(p.x-46,top,84,(noseBase-top)/(sourceBase/148)));
                default:
                    var lip=MouthAnchor(a);
                    return FromHead(new Rect(lip.x-40,lip.y-49*.44f,80,49));}
        }
        // The painted head turns slightly towards the viewer's left. The
        // protruding nose shifts further than the mouth. Beards use the same
        // actual lip anchor as the mouth, independently of either crop size.
        public static Vector2 MouthAnchor(Appearance a)
        {var p=Landmarks(a);return new Vector2(p.x-3,p.z);}
        public static Rect FeatureCell(Appearance a,int part)
        {
            int row=a.sex*4+part-2,col=a.parts[part];float cell=1254f/8;
            var crop=part==2?new Rect(2,60,152,64):part==3?new Rect(2,48,152,54):part==4?new Rect(9,0,140,148):new Rect(5,25,147,119);
            return SourcePixels(col*cell+crop.x,row*cell+crop.y,crop.width,crop.height,1254,1254);
        }
        // Measured bare-skin midtones in the 2026-10-13 feature atlas.
        // Six groups: male eyes/nose/mouth, female eyes/nose/mouth; eight each.
        // Transparent pixels and dark organs were excluded before sampling.
        static readonly Color32[] FeatureSkinReferences={
            new Color32(201,162,145,255),new Color32(198,158,141,255),new Color32(200,161,144,255),new Color32(195,156,139,255),new Color32(197,158,142,255),new Color32(202,163,147,255),new Color32(193,154,137,255),new Color32(194,157,139,255),
            new Color32(205,166,143,255),new Color32(204,164,142,255),new Color32(205,166,144,255),new Color32(204,165,143,255),new Color32(204,165,143,255),new Color32(206,168,146,255),new Color32(204,164,143,255),new Color32(202,163,141,255),
            new Color32(199,159,139,255),new Color32(197,157,139,255),new Color32(195,155,137,255),new Color32(195,155,137,255),new Color32(200,160,141,255),new Color32(198,158,140,255),new Color32(198,158,140,255),new Color32(196,157,136,255),
            new Color32(201,161,146,255),new Color32(201,161,145,255),new Color32(201,162,146,255),new Color32(199,159,143,255),new Color32(199,160,145,255),new Color32(201,163,148,255),new Color32(197,157,140,255),new Color32(198,159,141,255),
            new Color32(209,167,146,255),new Color32(205,164,142,255),new Color32(208,166,146,255),new Color32(206,164,144,255),new Color32(206,166,145,255),new Color32(206,166,145,255),new Color32(204,164,143,255),new Color32(201,160,139,255),
            new Color32(199,160,142,255),new Color32(197,156,138,255),new Color32(196,155,137,255),new Color32(197,156,138,255),new Color32(200,161,143,255),new Color32(197,157,139,255),new Color32(197,158,140,255),new Color32(194,155,135,255)
        };
        public static Color FeatureSkin(Appearance a,int part)
        {int group=part==2?0:part==4?1:2;return FeatureSkinReferences[a.sex*24+group*8+a.parts[part]];}
        // The source sheet has unequal top gutters. Register each garment at
        // its own collar, instead of using a nominal grid origin as anatomy.
        static readonly float[] OutfitTop={52,54,51,48,343,342,346,342,650,651,655,651,946,946,946,947};
        static readonly float[] OutfitBottom={308,308,308,308,620,620,620,620,923,923,923,923,1225,1225,1225,1225};
        public static Rect OutfitUV(Appearance a)
        {int i=a.sex*8+a.parts[7];return SourcePixels(i%4*313.5f+9,OutfitTop[i],295.5f,OutfitBottom[i]-OutfitTop[i],1254,1254);}
        public static Rect GarmentRect(Appearance a)
        {int i=a.sex*8+a.parts[7];return new Rect(10,267,380,(OutfitBottom[i]-OutfitTop[i])*380/295.5f);}
        public static Rect NeckUV(Appearance a)
        {int i=a.parts[0];return SourcePixels(i%4*362+117,i/4*362+302,130,48,1448,1086);}
        public static readonly Rect NeckRect=FromHead(new Rect(117,302,130,132));
        // Register at the painted lip opening, not at a changing sprite boundary.
        static readonly Rect[] DetailPixels={new Rect(40,115,270,215),new Rect(394,0,327,422),new Rect(734,108,370,182),new Rect(1120,85,328,256),new Rect(40,441,290,290),new Rect(388,421,310,343),new Rect(746,487,320,118),new Rect(1086,466,362,171),new Rect(59,786,242,202),new Rect(441,784,211,237)};
        static readonly Vector2[] BeardMouth={new Vector2(171,201),new Vector2(530,145),new Vector2(897,204),new Vector2(1257,209),new Vector2(173,548),new Vector2(532,518),new Vector2(894,563)};
        static readonly float[] BeardScale={.38f,.34f,.37f,.40f,.38f,.34f,.31f};
        public static Rect DetailUV(int index){var r=DetailPixels[index];return SourcePixels(r.x,r.y,r.width,r.height,1448,1086);}
        public static Rect BeardRect(Appearance a)
        {
            int i=a.parts[6]-1;var r=DetailPixels[i];var m=BeardMouth[i];var p=MouthAnchor(a);float s=BeardScale[i];
            float lift=i==2?8:i==6?9:0;
            return FromHead(new Rect(p.x+(r.x-m.x)*s,p.y+(r.y-m.y)*s-lift,r.width*s,r.height*s));
        }
        public static Rect AdornmentRect(Appearance a)
        {
            var p=Landmarks(a);
            if(a.parts[8]==1)return FromHead(new Rect(p.x-81,p.y-22,171,66));
            if(a.parts[8]==2)return FromHead(new Rect(p.x-91,p.y+25,194,60));
            return FromHead(new Rect(p.x+51,p.y+19,15,34));
        }
    }
}
