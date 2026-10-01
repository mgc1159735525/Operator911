using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator PortraitCombinationsSmoke(string path)
        {
            if(smokeSave==null)throw new Exception("Portrait smoke requires an isolated profile");
            yield return Click("HomeAppearance");
            // All 8 faces x 8 outfits per sex, including crossed age/neck shapes.
            // Capture both dossier and roster size; numeric anchors are not a visual verdict.
            for(int sex=0;sex<2;sex++)for(int offset=0;offset<8;offset++)
            {
                Draw();var sheet=Box("PortraitCrossProof",root,0,0,1600,900,Paper);
                Label(sheet,"随机组合验收 / "+(sex==0?"男性":"女性")+" / 衣服偏移 "+offset+" · 完整脸型×衣领覆盖",25,12,1500,32,22,PaperInk);
                for(int face=0;face<8;face++)
                {
                    var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
                    int outfit=(face+offset)%8;
                    look.parts[0]=face;look.parts[1]=(face+offset)%4;look.parts[7]=outfit;look.parts[6]=sex==0?(face+offset)%4:0;
                    look.parts[5]=face%4;look.ears=offset%4;look.parts[9]=3;look.parts[11]=offset%5;
                    DrawLook(sheet,look,25+face%4*398,53+face/4*420,267,385);
                    DrawLook(sheet,look,306+face%4*398,129+face/4*420,70,103);
                    Label(sheet,"脸 "+(face+1)+"\n衣 "+(outfit+1),306+face%4*398,256+face/4*420,86,55,17,PaperInk);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"cross-"+sex+"-"+offset+".png"));
            }
            Debug.Log("MINGLU_PORTRAIT_COMBINATIONS_UI_SUCCESS: 128 face/outfit pairs at portrait and roster size; visual review required");
        }
    }
}
