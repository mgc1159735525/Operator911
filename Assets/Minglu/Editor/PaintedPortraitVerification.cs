using System;
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
        public static void Verify()
        {
            CheckAtlas("portrait-bases",4,4);CheckAtlas("hair-overlays",4,2);CheckAtlas("outfit-overlays",4,4,1);CheckAtlas("beard-overlays",3,1);
            var shader=Resources.Load<Shader>("Minglu/Art/PaintedPortrait");
            Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"shader compilation");
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
