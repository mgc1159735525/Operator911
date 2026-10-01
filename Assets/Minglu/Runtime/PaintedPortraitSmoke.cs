using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator PaintedPortraitSmoke(string path)
        {
            yield return Click("HomeAppearance");
            for(int sex=0;sex<2;sex++)for(int hair=0;hair<4;hair++)
            {
                Draw();var sheet=Box("PaintedProof",root,0,0,1600,900,Paper);
                Label(sheet,"绘制立绘 / "+(sex==0?"男性":"女性")+" / 发型 "+(hair+1)+" / 面容与服装配准",25,12,1500,32,23,PaperInk);
                for(int face=0;face<8;face++)
                {
                    var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
                    look.parts[0]=face;look.parts[1]=hair;look.parts[7]=face;look.parts[9]=3;look.parts[11]=4;
                    DrawLook(sheet,look,25+face%4*398,53+face/4*420,267,385);
                    DrawLook(sheet,look,306+face%4*398,129+face/4*420,70,103);
                    Label(sheet,"面容 "+(face+1)+"\n服装 "+(face+1),300+face%4*398,256+face/4*420,88,54,16,PaperInk);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"s"+sex+"-h"+hair+".png"));
            }
            Draw();var beards=Box("BeardProof",root,0,0,1600,900,Paper);
            Label(beards,"胡须与五官 / 同一斜侧面，分别改变胡须及嘴形",24,12,1500,32,23,PaperInk);
            for(int row=0;row<2;row++)for(int beard=0;beard<4;beard++)
            {
                var look=AppearanceRules.AsRealistic(new Appearance{enabled=true});look.parts[0]=row==0?1:3;look.parts[1]=1;look.parts[6]=beard;look.parts[5]=row==0?0:3;look.parts[9]=3;look.parts[11]=4;
                DrawLook(beards,look,25+beard*398,53+row*420,370,395);
            }
            yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"beards.png"));
            // Every beard must follow both lip and chin on every authored face,
            // including the edited mouth; a two-face example misses the drift.
            for(int beard=1;beard<=3;beard++)
            {
                Draw();var proof=Box("BeardRegistrationProof",root,0,0,1600,900,Paper);
                Label(proof,"胡须配准 / 款式 "+beard+" / 八张面容 · 含嘴形变化",24,12,1500,32,23,PaperInk);
                for(int face=0;face<8;face++)
                {
                    var look=AppearanceRules.AsRealistic(new Appearance{enabled=true});
                    look.parts[0]=face;look.parts[1]=1;look.parts[6]=beard;look.parts[5]=face%4;look.parts[9]=3;look.parts[11]=4;
                    DrawRealisticLook(proof,look,25+face%4*398,53+face/4*420,366,385,Color.white,true);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"beard-fit-"+beard+".png"));
            }
            // Exercise every editable facial region through the real render path at a useful display size.
            foreach(int sex in new[]{0,1})foreach(int feature in new[]{2,3,4,5,13})
            {
                var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});look.parts[0]=1;look.parts[1]=1;look.parts[9]=3;
                var outputs=new string[2];
                for(int sample=0;sample<2;sample++)
                {
                    if(feature==13)look.ears=sample*3;else look.parts[feature]=sample*3;
                    Draw();var proof=Box("MorphProof",root,0,0,1600,900,Paper);DrawLook(proof,look,420,10,600,880);
                    yield return null;yield return new WaitForEndOfFrame();outputs[sample]=Path.ChangeExtension(path,"morph-"+sex+"-"+feature+"-"+sample+".png");Capture(outputs[sample]);
                }
                var before=new Texture2D(2,2);var after=new Texture2D(2,2);before.LoadImage(File.ReadAllBytes(outputs[0]));after.LoadImage(File.ReadAllBytes(outputs[1]));var a=before.GetPixels32();var b=after.GetPixels32();int changed=0;
                for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>12)changed++;
                Destroy(before);Destroy(after);if(changed<20)throw new Exception("Painted facial edit has no visible effect: "+sex+" / "+feature);
            }
            for(int sex=0;sex<2;sex++)
            {
                lookDraft=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
                lookDraft.parts[0]=1;lookDraft.parts[1]=sex==0?0:1;lookDraft.parts[7]=sex==0?6:0;
                lookDraft.parts[9]=3;lookDraft.parts[11]=sex==0?3:1;
                lookCategory=1;lookGroup=0;lookOptionPage=0;lookZoom=false;lookCompare=false;Draw();
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,sex==0?"editor-male.png":"editor-female.png"));
            }
            Debug.Log("MINGLU_PAINTED_PORTRAIT_UI_SUCCESS: 64 face/hair/outfit registrations, 8 beard variants, 24 enlarged beard/face registrations, 10 visible facial edits");
        }
    }
}
