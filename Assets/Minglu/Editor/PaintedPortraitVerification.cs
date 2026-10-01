using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class PaintedPortraitVerification
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Painted portrait: "+message);}
        static void CheckCell(Rect cell)
        {Check(cell.width>0&&cell.height>0&&cell.xMin>=0&&cell.yMin>=0&&cell.xMax<=1&&cell.yMax<=1,"atlas cell outside texture");}
        static void CheckAtlas(string name,int columns,int rows,float aspect=2f/3)
        {
            var texture=Resources.Load<Texture2D>("Minglu/Art/Painted/"+name);
            Check(texture!=null,"missing "+name);
            Check(Mathf.Abs((float)texture.width/columns/((float)texture.height/rows)-aspect)<.012f,"unexpected source aspect in "+name);
            var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            Check(importer!=null&&!importer.mipmapEnabled&&importer.npotScale==TextureImporterNPOTScale.None,"atlas resampling enabled: "+name);
        }
        static Texture2D ReadAtlas(string name)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            Check(texture.LoadImage(File.ReadAllBytes("Assets/Minglu/Resources/Minglu/Art/Painted/"+name+".png")),"cannot inspect "+name);
            return texture;
        }
        static float Alpha(Texture2D texture,Rect cell,Vector2 point)
        {
            if(point.x<0||point.x>1||point.y<0||point.y>1)return 0;
            return texture.GetPixelBilinear(cell.x+point.x*cell.width,cell.y+(1-point.y)*cell.height).a;
        }
        static void VerifyRegistration()
        {
            var hair=ReadAtlas("hair-overlays");var body=ReadAtlas("portrait-bases");var outfit=ReadAtlas("outfit-overlays");
            try
            {
                for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)
                {
                    var look=AppearanceRules.Randomize(17,sex);look.parts[0]=face;var feature=PaintedPortraitLayout.Landmarks(look);
                    var mouth=new Vector2(feature.mouth.x,feature.mouth.y);
                    Check(Alpha(body,PaintedPortraitLayout.Base(look),mouth)>.90f,"mouth registration falls outside actual painted face");
                    Check(Alpha(body,PaintedPortraitLayout.Base(look),feature.chin)>.90f,"chin registration falls outside actual painted face");
                    Check(Alpha(body,PaintedPortraitLayout.Base(look),feature.chin+new Vector2(.060f,.085f))>.90f,"continuous painted neck missing under the garment opening");
                    Check(feature.chin.x>mouth.x+.02f&&feature.chin.y>mouth.y+.05f,"three-quarter mouth/chin separation lost");
                    for(int clothing=0;clothing<8;clothing++)
                    {
                        look.parts[7]=clothing;
                        var garment=PaintedPortraitLayout.OutfitRect(look);
                        Check(PaintedPortraitLayout.FullCrop.xMin<garment.xMin&&PaintedPortraitLayout.FullCrop.xMax>garment.xMax,"full portrait crops the shoulder silhouette");
                        for(int anchor=3;anchor<5;anchor++)
                        {
                            var binding=PaintedPortraitLayout.OutfitBinding(look,anchor);
                            var target=new Vector2(binding.x,binding.y);
                            var expected=new Vector2((binding.x-binding.z-garment.x)/garment.width,(binding.y-binding.w-garment.y)/garment.height);
                            Check(Vector2.Distance(PaintedPortraitLayout.OutfitSamplingPoint(look,target),expected)<.001f,"collar movement displaces a shoulder");
                        }
                        for(int anchor=0;anchor<3;anchor++)
                            Check(Vector2.Distance(PaintedPortraitLayout.OutfitSamplingPoint(look,PaintedPortraitLayout.CollarTarget(look,anchor)),PaintedPortraitLayout.CollarSource(look,anchor))<.001f,"garment collar misses its anatomical binding");
                        // A valid set of anchor points can still fold the fabric
                        // between them. Check the local sampling orientation.
                        for(int x=0;x<=20;x++)for(int y=0;y<=16;y++)
                        {
                            var p=new Vector2(-.15f+x*.067f,.52f+y*.05f);var q=PaintedPortraitLayout.OutfitSamplingPoint(look,p);
                            var dx=PaintedPortraitLayout.OutfitSamplingPoint(look,p+new Vector2(.001f,0))-q;
                            var dy=PaintedPortraitLayout.OutfitSamplingPoint(look,p+new Vector2(0,.001f))-q;
                            Check(dx.x*dy.y-dx.y*dy.x>0,"collar registration folds the garment");
                        }
                    }
                    for(int h=0;h<4;h++)
                    {
                        look.parts[1]=h;var bounds=PaintedPortraitLayout.HairCanvas(look);
                        for(int anchor=0;anchor<4;anchor++)
                        {
                            var source=PaintedPortraitLayout.HairSource(look,anchor);
                            var expected=new Vector2((source.x-bounds.x)/bounds.width,(source.y-bounds.y)/bounds.height);
                            var actual=PaintedPortraitLayout.HairSamplingPoint(look,PaintedPortraitLayout.HairTarget(look,anchor));
                            Check(Vector2.Distance(actual,expected)<.001f,"hair anatomical anchor misses source registration");
                        }
                        var opening=PaintedPortraitLayout.HairSamplingPoint(look,new Vector2(feature.earNear.x,feature.earNear.y));
                        Check(Alpha(hair,PaintedPortraitLayout.Hair(look),opening)<.20f,"registered ear opening contains opaque hair pixels");
                    }
                    if(sex==0)for(int beard=1;beard<4;beard++)
                    {
                        look.parts[6]=beard;
                        var atMouth=PaintedPortraitLayout.BeardSamplingPoint(look,mouth);
                        var atChin=PaintedPortraitLayout.BeardSamplingPoint(look,feature.chin);
                        Check(Vector2.Distance(atMouth,new Vector2(.400f,.382f))<.0001f,"beard mouth anchor drift");
                        Check(Vector2.Distance(atChin,new Vector2(.394f,.510f))<.0001f,"beard chin anchor drift");
                        float last=-10;
                        for(int sample=0;sample<=40;sample++)
                        {
                            float y=mouth.y-.06f+sample*.008f;var q=PaintedPortraitLayout.BeardSamplingPoint(look,new Vector2(mouth.x,y));
                            Check(q.y>last,"beard folds between lips and jaw");last=q.y;
                        }
                    }
                }
                Check(PaintedPortraitLayout.FullCrop.xMin<=-.112f&&PaintedPortraitLayout.FullCrop.xMax>=1.158f,"full framing crops authored shoulders");
                for(int sex=0;sex<2;sex++)foreach(int clothing in new[]{1,5})
                {
                    var look=AppearanceRules.Randomize(17,sex);look.parts[7]=clothing;var cell=PaintedPortraitLayout.Outfit(look);
                    var sliver=new Vector2(.0015f,.65f);var gap=new Vector2(.014f,.65f);var shoulder=new Vector2(.04f,.65f);
                    Check(Alpha(outfit,cell,sliver)>.10f,"expected detached atlas sliver moved; recheck the source crop");
                    Check(Alpha(outfit,cell,gap)<.10f,"garment trim no longer falls in the transparent gap");
                    Check(Alpha(outfit,cell,shoulder)>.90f,"expected garment shoulder moved");
                    Check(PaintedPortraitLayout.OutfitEdgeAlpha(look,sliver)==0&&PaintedPortraitLayout.OutfitEdgeAlpha(look,shoulder)==1,"garment edge trim removes a shoulder or retains a sliver");
                }
                Debug.Log("MINGLU_PORTRAIT_REGISTRATION_SUCCESS: 16 continuous necks, 384 collar bindings without fabric folds, 256 hair bindings, 64 actual transparent ear openings, 24 separate lip/jaw bindings; visual sheets still required");
            }
            finally{UnityEngine.Object.DestroyImmediate(hair);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(outfit);}
        }
        public static void Verify()
        {
            CheckAtlas("portrait-bases",4,4);CheckAtlas("hair-overlays",4,2);CheckAtlas("outfit-overlays",4,4,1);CheckAtlas("beard-overlays",3,1);
            var shader=Resources.Load<Shader>("Minglu/Art/PaintedPortrait");
            Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"shader compilation");
            VerifyRegistration();
            int combinations=0;
            for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)for(int hair=0;hair<4;hair++)for(int outfit=0;outfit<8;outfit++)
            {
                var look=AppearanceRules.Randomize(17,sex);look.parts[0]=face;look.parts[1]=hair;look.parts[7]=outfit;
                for(int beard=0;beard<(sex==0?4:1);beard++)
                {
                    look.parts[6]=beard;AppearanceRules.Validate(look);
                    CheckCell(PaintedPortraitLayout.Base(look));CheckCell(PaintedPortraitLayout.Hair(look));CheckCell(PaintedPortraitLayout.Outfit(look));CheckCell(PaintedPortraitLayout.Beard(look));
                    var hairRect=PaintedPortraitLayout.HairRect(look);var hairUV=PaintedPortraitLayout.Hair(look);
                    Check(Mathf.Abs(hairRect.width/hairRect.height-hairUV.width/hairUV.height*2)<.001f,"hair registration stretches source pixels");
                    var outfitRect=PaintedPortraitLayout.OutfitRect(look);
                    Check(Mathf.Abs(outfitRect.width/outfitRect.height*2f/3-1)<.001f,"square clothes stretched");
                    combinations++;
                }
                var feature=PaintedPortraitLayout.Landmarks(look);
                Check(feature.eyeFar.x<feature.eyeNear.x&&feature.eyeFar.z<feature.eyeNear.z,"3/4 eye registration lost");
            }
            for(int seed=0;seed<200;seed++)
            {
                var look=AppearanceRules.Randomize(seed);string original=JsonUtility.ToJson(look);
                var next=AppearanceRules.AsRealistic(look);
                Check(original==JsonUtility.ToJson(look)&&original==JsonUtility.ToJson(next),"version 15 choices mutated by renderer migration");
                var restored=JsonUtility.FromJson<Appearance>(original);Check(original==JsonUtility.ToJson(restored),"save parts roundtrip");
            }
            foreach(var size in new[]{new Vector2(328,470),new Vector2(66,94),new Vector2(64,91),new Vector2(230,360)})
            {
                foreach(bool detail in new[]{false,true})
                {
                    var placement=PaintedPortraitLayout.Placement(size.x,size.y,detail);
                    Check(Mathf.Abs(placement.width/placement.height-2f/3)<.0001f,"portrait distorted by UI frame");
                }
            }
            Debug.Log("MINGLU_PAINTED_PORTRAIT_SUCCESS: 4 atlases, shader, "+combinations+" valid layer combinations, 200 unchanged saved looks, consistent full/detail proportions");
        }
    }
}
