using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator PortraitRepairSmoke(string path)
        {
            yield return Click("HomeAppearance");
            foreach(int sex in new[]{0,1})
            {
                yield return Click(sex==0?"LookMale":"LookFemale");
                foreach(int category in new[]{2,3,4,5,13})
                {
                    yield return StudioSetOption(category,0);
                    var before=Path.ChangeExtension(path,"s"+sex+"-c"+category+"-before.png");Capture(before);
                    yield return StudioSetOption(category,3);var after=Path.ChangeExtension(path,"s"+sex+"-c"+category+"-after.png");Capture(after);
                    if(!FacePixelsDiffer(before,after))throw new Exception("Local morph did not render: "+category);
                }
            }
            yield return Click("LookPresets");yield return Click("LookSaveSlot");string expected=JsonUtility.ToJson(lookDraft);yield return Click("LookPopupBack");
            yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");
            if(JsonUtility.ToJson(lookDraft)!=expected)throw new Exception("Morph preset mismatch");
            yield return Click("LookApply");yield return Click("HomeAppearance");
            if(JsonUtility.ToJson(lookDraft)!=expected)throw new Exception("Morph save mismatch");
            Capture(Path.ChangeExtension(path,"editor.png"));
            // Contact sheets rendered by the actual game renderer, not a separate approximation.
            for(int sex=0;sex<2;sex++)for(int hair=0;hair<4;hair++)
            {
                Draw();var sheet=Box("PortraitQA",root,0,0,1600,900,Ink);
                Label(sheet,"外观验收 / "+(sex==0?"男":"女")+" / 发型 "+hair+" / 8脸 × 8衣领抽样",24,8,1500,30,22,Gold);
                for(int i=0;i<8;i++)
                {
                    var a=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});a.parts[0]=i;a.parts[7]=i;a.parts[1]=hair;a.parts[12]=i%4==3?2:0;
                    int col=i%4,row=i/4;DrawLook(sheet,a,20+col*395,48+row*418,270,390);
                    DrawLook(sheet,a,300+col*395,92+row*418,66,94);
                    Label(sheet,"脸/衣 "+i,300+col*395,200+row*418,90,24,15,Gold);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"sheet-s"+sex+"-h"+hair+".png"));
            }
            for(int sex=0;sex<2;sex++)
            {
                Draw();var comparison=Box("ProportionQA",root,0,0,1600,900,Ink);
                Label(comparison,"比例对照 / 同一张脸 / 上排只换发型，下排只换衣服",24,8,1500,30,22,Gold);
                for(int i=0;i<8;i++)
                {
                    var a=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});a.parts[0]=1;
                    a.parts[1]=i<4?i:1;a.parts[7]=i<4?0:i-4;
                    DrawLook(comparison,a,40+i%4*390,48+i/4*418,260,390);
                    DrawLook(comparison,a,305+i%4*390,145+i/4*418,66,94);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"proportion-s"+sex+".png"));
            }
            Draw();var hairDetail=Box("HairDetailQA",root,0,0,1600,900,Ink);
            Label(hairDetail,"发型近景 / 上排男、下排女 / 同一面容",24,8,1500,30,22,Gold);
            for(int sex=0;sex<2;sex++)for(int hair=0;hair<4;hair++)
            {
                var cell=Box("HairDetailCell",hairDetail,10+hair*398,48+sex*422,388,408,Ink);
                cell.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                var a=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});a.parts[0]=1;a.parts[1]=hair;
                DrawLook(cell,a,-6,-12,400,690);
            }
            yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"hair-detail.png"));
            for(int beard=1;beard<4;beard++)
            {
                Draw();var beardSheet=Box("BeardQA",root,0,0,1600,900,Ink);
                Label(beardSheet,"胡须配准 / 款式 "+beard+" / 八种脸型与衣领",24,8,1500,30,22,Gold);
                for(int face=0;face<8;face++)
                {
                    var cell=Box("BeardCell",beardSheet,10+face%4*398,48+face/4*422,388,408,Ink);
                    cell.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                    var a=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=0});a.parts[0]=face;a.parts[1]=1;a.parts[6]=beard;a.parts[7]=face;
                    DrawLook(cell,a,-6,-12,400,690);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"beard-"+beard+".png"));
            }
            Draw();var variants=Box("PortraitQA",root,0,0,1600,900,Ink);
            for(int i=0;i<12;i++)
            {var a=AppearanceRules.Randomize(170+i,i/6);a.parts[6]=i%4;a.parts[10]=i%4;DrawLook(variants,a,20+i%6*263,20+i/6*440,245,405);}
            yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"random.png"));
            Debug.Log("MINGLU_PORTRAIT_REPAIR_SUCCESS: local morphs both sexes, preset/save, 64 full portraits + 64 roster crops + 12 random combinations rendered");
        }
    }
}
