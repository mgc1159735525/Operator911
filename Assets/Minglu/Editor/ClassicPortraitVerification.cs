using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class ClassicPortraitVerification
    {
        static int checks;
        static void Require(bool condition,string message){checks++;if(!condition)throw new Exception("Classic portrait: "+message);}
        static void VerifyCell(Texture2D source,Rect uv,string label,HashSet<ulong> variants)
        {
            Require(uv.width>0&&uv.height>0&&uv.xMin>=0&&uv.yMin>=0&&uv.xMax<=1.001f&&uv.yMax<=1.001f,"sprite crop lies outside its atlas: "+label);
            int x=Mathf.FloorToInt(uv.xMin*source.width),y=Mathf.FloorToInt(uv.yMin*source.height);
            int w=Mathf.Min(source.width-x,Mathf.CeilToInt(uv.width*source.width)),h=Mathf.Min(source.height-y,Mathf.CeilToInt(uv.height*source.height));
            var pixels=source.GetPixels(x,y,w,h);int visible=0;ulong hash=1469598103934665603UL;
            foreach(Color32 p in pixels)
            {
                if(p.a>32)visible++;
                unchecked{hash=(hash^p.r)*1099511628211UL;hash=(hash^p.g)*1099511628211UL;hash=(hash^p.b)*1099511628211UL;hash=(hash^p.a)*1099511628211UL;}
            }
            Require(visible>Mathf.Max(4,pixels.Length/1000),"catalog entry points at empty art: "+label);
            if(variants!=null)Require(variants.Add(hash),"catalog entries repeat exactly the same source pixels: "+label);
        }
        static void VerifyCells(Texture2D source,int index)
        {
            var variants=new HashSet<ulong>();
            if(index<=1)for(int head=0;head<12;head++)
            {
                var look=AppearanceRules.Randomize(430,index,head);
                VerifyCell(source,ClassicPortraitCatalog.Cell(head,4,3),"head "+index+"/"+head,variants);
                // The neck bridge is sampled from this very head. Similar
                // skin patches may legitimately repeat across head options.
                VerifyCell(source,ClassicPortraitCatalog.NeckUV(look),"neck "+index+"/"+head,null);
            }
            else if(index==2)for(int sex=0;sex<2;sex++)for(int category=2;category<=5;category++)
            {
                variants.Clear();var look=AppearanceRules.Randomize(430,sex,0);
                for(int option=0;option<8;option++){look.parts[category]=option;VerifyCell(source,ClassicPortraitCatalog.FeatureCell(look,category),"feature "+sex+"/"+category+"/"+option,variants);}
            }
            else if(index==3)for(int sex=0;sex<2;sex++)for(int outfit=0;outfit<8;outfit++)
            {
                var look=AppearanceRules.Randomize(430,sex,0);look.parts[7]=outfit;
                VerifyCell(source,ClassicPortraitCatalog.OutfitUV(look),"outfit "+sex+"/"+outfit,variants);
                var garment=ClassicPortraitCatalog.GarmentRect(look);
                Require(garment.width>0&&garment.height>0&&!float.IsNaN(garment.y)&&!float.IsInfinity(garment.height),"garment registration is invalid: "+sex+"/"+outfit);
            }
            else if(index==4)for(int detail=0;detail<10;detail++)VerifyCell(source,ClassicPortraitCatalog.DetailUV(detail),"beard/adornment "+detail,variants);
            else if(index==5)for(int background=0;background<4;background++)VerifyCell(source,ClassicPortraitCatalog.Cell(background,2,2),"backdrop "+background,variants);
            else if(index==6)for(int sex=0;sex<2;sex++)
            {
                variants.Clear();var look=AppearanceRules.Randomize(430,sex,0);
                for(int option=0;option<8;option++){look.parts[3]=option;VerifyCell(source,ClassicPortraitCatalog.FeatureCell(look,3),"painted brow "+sex+"/"+option,variants);}
            }
        }
        static void VerifyTexture(string resource,HashSet<ulong> signatures)
        {
            Require(!string.IsNullOrWhiteSpace(resource)&&resource.StartsWith("Minglu/Art/Classic/"),"catalog retains a legacy or invalid art path: "+resource);
            var imported=Resources.Load<Texture2D>(resource);Require(imported!=null,"runtime resource is missing: "+resource);
            Require(imported.width>=128&&imported.height>=128,"imported texture is too small: "+resource);
            string path=Path.Combine(Application.dataPath,"Minglu/Resources",resource+".png");Require(File.Exists(path),"source PNG is missing: "+path);
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Require(source.LoadImage(File.ReadAllBytes(path)),"source PNG cannot be decoded: "+resource);
                var pixels=source.GetPixels32();int clear=0,visible=0,nonOpaque=0;byte min=255,max=0;ulong signature=1469598103934665603UL;
                foreach(var p in pixels)
                {
                    if(p.a<255)nonOpaque++;if(p.a<=8)clear++;if(p.a>=32){visible++;byte l=(byte)((p.r*3+p.g*6+p.b)/10);min=Math.Min(min,l);max=Math.Max(max,l);}
                    unchecked{signature=(signature^p.r)*1099511628211UL;signature=(signature^p.g)*1099511628211UL;signature=(signature^p.b)*1099511628211UL;signature=(signature^p.a)*1099511628211UL;}
                }
                Require(visible>pixels.Length/1000,"atlas contains no usable painted content: "+resource);
                if(resource.EndsWith("/backdrops"))Require(nonOpaque==0,"painted background must be completely opaque: "+resource);
                else Require(clear>pixels.Length/1000,"atlas lacks transparent space for real part compositing: "+resource);
                Require(max-min>24,"atlas is effectively a flat placeholder: "+resource);
                Require(signatures.Add(signature),"different catalog resources contain identical art: "+resource);
                VerifyCells(source,Array.IndexOf(ClassicPortraitCatalog.ResourcePaths,resource));
            }
            finally{UnityEngine.Object.DestroyImmediate(source);}
        }
        static void VerifyRetirement()
        {
            foreach(string name in new[]{"PaintedPortraitUI","PaintedPortraitLayout","PaintedPortraitGraphic","BeardContour","FeaturePortrait","PortraitLayout","PortraitGraphic"})
                Require(!File.Exists(Path.Combine(Application.dataPath,"Minglu/Runtime",name+".cs")),"retired renderer remains: "+name);
            string art=Path.Combine(Application.dataPath,"Minglu/Resources/Minglu/Art");
            string retired=Path.Combine(art,"Painted");
            Require(!Directory.Exists(retired)||Directory.GetFiles(retired,"*",SearchOption.AllDirectories).Length==0,"old painted assets remain in Resources and would still ship");
            foreach(string name in new[]{"realistic-atlas-v10.png","bald-atlas-v11.png","hair-atlas-v11.png","hair-atlas-v153.png","neck-shoulders-v153.png","expansion-atlas-v12.png","portraits-v02.png","PaintedPortrait.shader","PortraitFeatures.shader"})
            {
                Require(!File.Exists(Path.Combine(art,name)),"retired portrait resource would still ship: "+name);
                Require(!File.Exists(Path.Combine(art,name+".meta")),"retired portrait resource metadata remains: "+name);
            }
            string bridge=File.ReadAllText(Path.Combine(Application.dataPath,"Minglu/Runtime/RealisticPortraitUI.cs"));
            Require(Regex.IsMatch(bridge,@"DrawClassic\w*\s*\("),"DrawRealisticLook does not route to the classic renderer");
            Require(!bridge.Contains("DrawPaintedLook"),"compatibility bridge still calls the old renderer");
            string entry=File.ReadAllText(Path.Combine(Application.dataPath,"Minglu/Runtime/AppearanceUI.cs"));
            Require(entry.Contains("DrawRealisticLook")&&entry.Contains("AsRealistic"),"DrawLook no longer uses the normalized common renderer entry");
            string city=File.ReadAllText(Path.Combine(Application.dataPath,"Minglu/Runtime/CityGameUI.cs"));
            Require(!city.Contains("raw.texture = portraits")&&!city.Contains("Asset(\"portraits\")"),"roster still has an old static portrait fallback");
        }
        // Independent measurements of the 2026-10-13 features.png source,
        // in top-left pixels of its 156.75 px cells, not target rectangles.
        // Pupil centres were measured from opaque dark pixels around each
        // iris; the female row is about 14 px higher than the male row.
        static readonly Vector4[] SourcePupils={
            new Vector4(40.7f,104,118.4f,101.9f),new Vector4(41.6f,102.9f,116.2f,101.6f),new Vector4(41,102.9f,119.5f,100.7f),new Vector4(43.6f,105.2f,117,102.5f),
            new Vector4(42.2f,107,117.2f,103.8f),new Vector4(42.2f,105.1f,117.8f,103.4f),new Vector4(40.4f,105.7f,118.1f,105.1f),new Vector4(40.9f,107.8f,115.5f,106.4f),
            new Vector4(42,89.3f,118.6f,88.4f),new Vector4(41.3f,88.7f,116.8f,87.4f),new Vector4(40.4f,89.7f,118.2f,87.7f),new Vector4(40.7f,90.3f,117.3f,88.1f),
            new Vector4(41.1f,91,118.5f,88.3f),new Vector4(41.8f,89.1f,117.8f,88.2f),new Vector4(40.4f,90.7f,117.8f,88.9f),new Vector4(41.5f,91.5f,116.4f,90.5f)
        };
        // Columella's lower dark-to-skin edge, not a nostril centroid or the
        // transparent bottom of the nose crop. Small noses differ by design.
        static readonly float[] SourceNoseBase={126,126,121,129,129,124,127,127,117,117,117,120,120,117,120,119};
        // Visible upper eyelid and lower brow envelopes measured inside the
        // painted eye/brow columns (3rd/97th percentile of opaque pixels).
        // Their transparent sprite margins are allowed to overlap.
        static readonly float[] SourceLidTop={92,92,97,92,92,92,92,93,79.8f,84,84,85,79,84,86,88};
        static readonly float[] SourceBrowBottom={83.2f,84.2f,84.2f,85.2f,85.2f,83.2f,84.2f,83.2f,76.2f,74.2f,75.2f,75.2f,75.2f,74.2f,72.2f,73.2f};
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        static Vector2 AtlasPoint(int sex,int part,int option,Vector2 point)
        {return new Vector2(option*156.75f+point.x,(sex*4+part-2)*156.75f+point.y);}
        static Vector2 ProjectSource(Rect sourceUV,Rect target,Vector2 atlasPoint)
        {
            float left=sourceUV.x*1254,top=(1-sourceUV.yMax)*1254;
            return new Vector2(target.x+(atlasPoint.x-left)/(sourceUV.width*1254)*target.width,target.y+(atlasPoint.y-top)/(sourceUV.height*1254)*target.height);
        }
        static Vector2 FeaturePoint(Appearance look,int part,Vector2 point)
        {return ProjectSource(ClassicPortraitCatalog.FeatureCell(look,part),ClassicPortraitCatalog.FeatureRect(look,part),AtlasPoint(look.sex,part,look.parts[part],point));}
        static void VisibleAnchor(Texture2D source,Vector2 point,string label,bool pupil=false)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt(point.x),0,source.width-1),y=Mathf.Clamp(source.height-1-Mathf.RoundToInt(point.y),0,source.height-1);
            var pixel=source.GetPixel(x,y);bool darkIris=!pupil;
            // A pupil's measured centre can land on its white catchlight.
            // Require adjacent opaque iris paint, not a dark centre pixel.
            if(pupil)for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
            {
                var neighbour=source.GetPixel(Mathf.Clamp(x+dx,0,source.width-1),Mathf.Clamp(y+dy,0,source.height-1));
                if(neighbour.a>.65f&&neighbour.grayscale<.50f)darkIris=true;
            }
            Require(pixel.a>.65f&&darkIris,"measured source anchor no longer lies on visible painted anatomy; remeasure the new art: "+label);
        }
        static bool PlausibleEyeNose(float distance,float pupilSpan)
        {return Finite(distance)&&Finite(pupilSpan)&&pupilSpan>0&&distance>=pupilSpan*.58f&&distance<=pupilSpan*1.12f;}
        static void VerifyAnatomicalRegistration()
        {
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Require(source.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Minglu/Resources/Minglu/Art/Classic/features.png"))),"cannot read anatomy measurement source");
                Require(source.width==1254&&source.height==1254,"anatomy atlas changed size; remeasure source anchors");
                for(int sex=0;sex<2;sex++)for(int option=0;option<8;option++)
                {
                    var p=SourcePupils[sex*8+option];
                    VisibleAnchor(source,AtlasPoint(sex,2,option,new Vector2(p.x,p.y)),"left pupil "+sex+"/"+option,true);
                    VisibleAnchor(source,AtlasPoint(sex,2,option,new Vector2(p.z,p.w)),"right pupil "+sex+"/"+option,true);
                    VisibleAnchor(source,AtlasPoint(sex,4,option,new Vector2(74,SourceNoseBase[sex*8+option])),"nose base "+sex+"/"+option);
                }
                float minRatio=float.PositiveInfinity,maxRatio=0,minBrowGap=float.PositiveInfinity;
                var headRect=ClassicPortraitCatalog.HeadRect;
                for(int sex=0;sex<2;sex++)for(int head=0;head<12;head++)for(int eye=0;eye<8;eye++)
                {
                    var look=AppearanceRules.Randomize(9500+head,sex,head);look.parts[2]=eye;
                    var p=SourcePupils[sex*8+eye];
                    var left=FeaturePoint(look,2,new Vector2(p.x,p.y));var right=FeaturePoint(look,2,new Vector2(p.z,p.w));
                    float eyeLine=(left.y+right.y)*.5f,pupilSpan=right.x-left.x;
                    string label="sex="+sex+" head="+head+" eyes="+eye;
                    Require(Finite(eyeLine)&&Finite(pupilSpan)&&pupilSpan>headRect.width*.12f&&pupilSpan<headRect.width*.27f,"invalid pupil projection: "+label);
                    Require(headRect.Contains(left)&&headRect.Contains(right),"pupil projection leaves its head: "+label);
                    for(int brow=0;brow<8;brow++)
                    {
                        look.parts[3]=brow;
                        float browBottom=FeaturePoint(look,3,new Vector2(74,SourceBrowBottom[sex*8+brow])).y;
                        float lidTop=FeaturePoint(look,2,new Vector2(74,SourceLidTop[sex*8+eye])).y;
                        float gap=lidTop-browBottom;minBrowGap=Mathf.Min(minBrowGap,gap);
                        Require(Finite(gap)&&gap>.35f,"painted brow intersects the upper eyelid: "+label+" brow="+brow+" gap="+gap.ToString("F2"));
                    }
                    for(int nose=0;nose<8;nose++)
                    {
                        look.parts[4]=nose;
                        var basePoint=FeaturePoint(look,4,new Vector2(74,SourceNoseBase[sex*8+nose]));
                        var root=FeaturePoint(look,4,new Vector2(78,40));
                        float distance=basePoint.y-eyeLine,ratio=distance/pupilSpan;
                        minRatio=Mathf.Min(minRatio,ratio);maxRatio=Mathf.Max(maxRatio,ratio);
                        string combination=label+" nose="+nose;
                        Require(headRect.Contains(basePoint)&&headRect.Contains(root),"nose projection leaves its head: "+combination);
                        Require(root.x>left.x&&root.x<right.x&&Mathf.Abs(root.y-eyeLine)<pupilSpan*.36f,"nose root is detached from the interocular region: "+combination);
                        Require(PlausibleEyeNose(distance,pupilSpan),"eye-to-nose distance is implausibly compressed/extended relative to pupil span: "+combination+" ratio="+ratio.ToString("F3"));
                        // All mouth choices share the same target anchor but
                        // their actual painted seam is measured independently.
                        for(int mouth=0;mouth<8;mouth++)
                        {
                            look.parts[5]=mouth;
                            float lipLine=FeaturePoint(look,5,new Vector2(75,sex==0?77:68)).y;
                            Require(Finite(lipLine)&&basePoint.y<lipLine-pupilSpan*.065f,"nose base reaches the painted lip line: "+combination+" mouth="+mouth);
                        }
                        for(int part=2;part<=5;part++)
                        {
                            var rect=ClassicPortraitCatalog.FeatureRect(look,part);var uv=ClassicPortraitCatalog.FeatureCell(look,part);
                            Require(Finite(rect.x)&&Finite(rect.y)&&Finite(rect.width)&&Finite(rect.height)&&rect.width>0&&rect.height>0&&rect.xMin>=headRect.xMin&&rect.xMax<=headRect.xMax&&rect.yMin>=headRect.yMin&&rect.yMax<=headRect.yMax,"feature rectangle is invalid or outside head: "+combination+" part="+part);
                            Require(uv.xMin>=0&&uv.yMin>=0&&uv.xMax<=1.0001f&&uv.yMax<=1.0001f&&uv.width>0&&uv.height>0,"anatomical source crop exceeds atlas: "+combination+" part="+part);
                        }
                    }
                }
                // Recorded pre-fix male H0 geometry. This independent failure
                // fixture ensures the guard detects the observed compressed
                // middle face instead of only accepting today's formula.
                var oldLeft=ProjectSource(new Rect(2/1254f,1-124/1254f,152/1254f,64/1254f),new Rect(97,157,136,61),new Vector2(40.7f,104));
                var oldRight=ProjectSource(new Rect(2/1254f,1-124/1254f,152/1254f,64/1254f),new Rect(97,157,136,61),new Vector2(118.4f,101.9f));
                var oldBase=ProjectSource(new Rect(9/1254f,1-461.5f/1254,140/1254f,148/1254f),new Rect(127,181,67,65.52f),new Vector2(74,313.5f+126));
                Require(!PlausibleEyeNose(oldBase.y-(oldLeft.y+oldRight.y)*.5f,oldRight.x-oldLeft.x),"regression guard no longer rejects the recorded compressed-face geometry");
                Debug.Log("MINGLU_CLASSIC_ANATOMY_VERIFY: 1536 sex/head/eye/nose combinations; measured source pixels, eye/nose ratio "+minRatio.ToString("F3")+".."+maxRatio.ToString("F3")+", minimum painted brow/lid gap "+minBrowGap.ToString("F2")+" px. Automatic structural checks do not establish artistic naturalness; shader shading/perspective still require rendered review.");
            }
            finally{UnityEngine.Object.DestroyImmediate(source);}
        }
        public static void Verify()
        {
            checks=0;
            Require(ClassicPortraitCatalog.HeadCount==12&&AppearanceRules.HeadCount==12,"complete-head catalogs disagree");
            Require(AppearanceRules.CurrentPortraitVersion>=20,"save bridge still identifies the deleted portrait schema");
            int[] counts={12,1,8,8,8,8,8,8,4,4,4,6,4};
            Require(AppearanceRules.Names.Length==counts.Length,"part count changed unexpectedly");
            for(int i=0;i<counts.Length;i++)Require(AppearanceRules.Names[i].Length==counts[i],"catalog count mismatch at part "+i);
            var paths=ClassicPortraitCatalog.ResourcePaths;
            Require(paths!=null&&paths.Length>=6&&paths.Distinct().Count()==paths.Length,"classic resource inventory is incomplete or duplicated");
            var shader=Resources.Load<Shader>("Minglu/Art/Classic/Portrait");Require(shader!=null&&!ShaderUtil.ShaderHasError(shader),"classic palette shader is missing or failed compilation");
            var signatures=new HashSet<ulong>();foreach(string resource in paths)VerifyTexture(resource,signatures);
            for(int sex=0;sex<2;sex++)for(int head=0;head<12;head++)
            {
                var look=AppearanceRules.Randomize(4300+head,sex,head);AppearanceRules.Validate(look);
                Require(look.parts[0]==head&&look.parts[1]==0&&look.ears==0,"new generation retained a removed head channel");
                Require(JsonUtility.ToJson(look)==JsonUtility.ToJson(AppearanceRules.AsRealistic(look)),"current complete head was migrated again");
            }
            VerifyRetirement();
            VerifyAnatomicalRegistration();
            Debug.Log("MINGLU_CLASSIC_PORTRAIT_VERIFY_SUCCESS: "+checks+" schema/source-alpha/resource/retirement checks; "+paths.Length+" new PNG atlases, 24 complete heads; common drawing bridge and static-avatar retirement verified. Runtime screenshots and artistic review remain separate.");
        }
    }
}
