using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator PortraitCatalogSmoke(string path)
        {
            if(smokeSave==null)throw new Exception("Catalog capture requires an isolated profile");
            yield return Click("HomeAppearance");
            int verified=0;
            for(int sex=0;sex<2;sex++)foreach(int feature in new[]{2,3,4,5,13})
            {
                var names=AppearancePartCatalog.Names(feature);var images=new List<Color32[]>();
                var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
                look.parts[0]=1;look.parts[1]=1;look.parts[9]=3;look.parts[11]=4;
                for(int option=0;option<names.Length;option++)
                {
                    if(feature==13)look.ears=option;else look.parts[feature]=option;
                    Draw();var proof=Box("CatalogPixelProof",root,0,0,1600,900,Paper);
                    // Identical scene and framing: labels cannot fake a visible feature change.
                    DrawRealisticLook(proof,look,500,20,560,840,Color.white,true);
                    yield return null;yield return new WaitForEndOfFrame();
                    string temp=Path.ChangeExtension(path,"pixel-temp.png");Capture(temp);
                    var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(temp));var pixels=texture.GetPixels32();Destroy(texture);File.Delete(temp);
                    foreach(var previous in images)
                    {
                        int changed=0;for(int i=0;i<pixels.Length;i++)if(Math.Abs(pixels[i].r-previous[i].r)+Math.Abs(pixels[i].g-previous[i].g)+Math.Abs(pixels[i].b-previous[i].b)>12)changed++;
                        if(changed<20)throw new Exception("Visually duplicate feature option: "+sex+" / "+feature+" / "+option);
                    }
                    images.Add(pixels);verified++;
                }
                Draw();var sheet=Box("CatalogContactSheet",root,0,0,1600,900,Paper);
                Label(sheet,(sex==0?"男性":"女性")+" / "+(feature==13?"耳朵":AppearanceRules.Labels[feature])+" / 同一面容 · 八种选择",24,10,1540,35,23,PaperInk);
                for(int option=0;option<names.Length;option++)
                {
                    if(feature==13)look.ears=option;else look.parts[feature]=option;
                    float x=24+option%4*398,y=52+option/4*420;
                    DrawRealisticLook(sheet,look,x,y,340,365,Color.white,true);
                    Label(sheet,(option+1)+"  "+names[option],x,y+373,340,30,18,PaperInk,TextAnchor.MiddleCenter);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"s"+sex+"-part"+feature+".png"));
            }
            Debug.Log("MINGLU_PORTRAIT_CATALOG_UI_SUCCESS: "+verified+" rendered variants, pairwise pixel distinction; 10 eight-option comparison sheets");
        }
    }
}
