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
            Debug.Log("MINGLU_CLASSIC_PORTRAIT_VERIFY_SUCCESS: "+checks+" schema/source-alpha/resource/retirement checks; "+paths.Length+" new PNG atlases, 24 complete heads; common drawing bridge and static-avatar retirement verified. Runtime screenshots and artistic review remain separate.");
        }
    }
}
