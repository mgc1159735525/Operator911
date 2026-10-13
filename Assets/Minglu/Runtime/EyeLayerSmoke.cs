using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        [Serializable] sealed class EyeLayerCase
        {
            public string id,visibility,eyeTexture;
            public Appearance look;
        }
        [Serializable] sealed class EyeLayerPage
        {
            public string file,purpose;
            public List<EyeLayerCase> cases=new List<EyeLayerCase>();
        }
        [Serializable] sealed class EyeLayerManifest
        {
            public string capturedUtc,gameVersion;
            public string result="Actual shared Unity renderer; visual review must establish absence of a second eyebrow, not geometry tests alone.";
            public int screenWidth,screenHeight,renderCount;
            public bool runUnchanged,profileUnchanged,draftUnchanged,isolatedFilesUnchanged;
            public List<EyeLayerPage> pages=new List<EyeLayerPage>();
        }
        string EyeLayerSignature(Transform frame,string prefix)
        {
            return string.Join("|",frame.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith(prefix)).OrderBy(g=>g.name)
                .Select(g=>g.name+":"+g.texture.name+":"+g.uvRect+":"+g.rectTransform.rect+":"+g.rectTransform.anchoredPosition));
        }
        void EyeLayerMagnify(RectTransform frame,string prefix)
        {
            var graphics=frame.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith(prefix)).ToArray();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(frame,graphics[0].transform);
            foreach(var graphic in graphics.Skip(1))bounds.Encapsulate(RectTransformUtility.CalculateRelativeRectTransformBounds(frame,graphic.transform));
            var art=(RectTransform)frame.Find("ClassicCanvas");
            float factor=Mathf.Min((frame.rect.width-44)/bounds.size.x,(frame.rect.height-80)/bounds.size.y);
            Vector2 center=bounds.center;
            art.anchoredPosition=frame.rect.center+(art.anchoredPosition-center)*factor;
            art.localScale*=factor;
        }
        RectTransform EyeLayerRender(Transform parent,EyeLayerPage page,string id,Appearance look,float x,float y,float w,float h,string visibility)
        {
            string before=JsonUtility.ToJson(look);int prior=parent.childCount;
            DrawClassicLook(parent,look,x,y,w,h,Color.white,true);
            var frame=(RectTransform)parent.GetChild(prior);RequireClassicPortrait(frame,look);
            var eyes=frame.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith("ClassicEyes")).ToArray();
            var brows=frame.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith("ClassicBrows")).ToArray();
            if(eyes.Length!=2||brows.Length!=2)throw new Exception("Eye/brow left and right layers are not independently addressable");
            if(eyes.Any(g=>g.texture==null||g.texture.name!="eyes"))throw new Exception("Eye proof still uses the combined legacy feature sheet");
            if(brows.Any(g=>g.texture==null||g.texture.name!="brows"))throw new Exception("Eyebrows no longer use their independent sheet");
            if(visibility=="without-brows")foreach(var graphic in brows)graphic.enabled=false;
            else if(visibility=="eyes-only"||visibility=="brows-only")
            {
                string prefix=visibility=="eyes-only"?"ClassicEyes":"ClassicBrows";
                foreach(var graphic in frame.GetComponentsInChildren<Graphic>())
                    graphic.enabled=graphic.transform==frame||graphic.name.StartsWith(prefix);
                EyeLayerMagnify(frame,prefix);
            }
            if(JsonUtility.ToJson(look)!=before)throw new Exception("Eye-layer rendering modified an appearance fixture");
            page.cases.Add(new EyeLayerCase{id=id,visibility=visibility,eyeTexture=eyes[0].texture.name,look=AppearanceRules.Copy(look)});
            return frame;
        }
        IEnumerator EyeLayerCapture(string path,string suffix,EyeLayerPage page,EyeLayerManifest manifest)
        {
            yield return null;yield return new WaitForEndOfFrame();
            string target=Path.ChangeExtension(path,suffix+".png");Capture(target);
            if(!File.Exists(target)||new FileInfo(target).Length==0)throw new Exception("Eye-layer screenshot was not written");
            page.file=Path.GetFileName(target);manifest.pages.Add(page);manifest.renderCount+=page.cases.Count;
        }
        Appearance EyeProofLook(int sex,int age)
        {
            var look=ClassicProofLook(sex,age==0?0:9);
            look.parts[2]=age==0?0:7;look.parts[3]=age==0?0:7;
            if(age!=0){look.parts[4]=7;look.parts[5]=7;}
            look.parts[8]=0;AppearanceRules.ApplyAgeHairColor(look);AppearanceRules.Validate(look);return look;
        }
        IEnumerator EyeLayerSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave)||ProfilePath!=smokeSave+".profile"||!atTitle)
                throw new Exception("Eye-layer proof requires an isolated title-screen --minglu-capture session");
            string run=GroomingFitSnapshot(State),player=GroomingFitSnapshot(profile),draft=GroomingFitSnapshot(lookDraft);
            var files=new Dictionary<string,string>();
            foreach(string file in new[]{smokeSave,smokeSave+".bak",smokeSave+".tmp",ProfilePath,ProfilePath+".bak",ProfilePath+".tmp"})
                files[file]=File.Exists(file)?Convert.ToBase64String(File.ReadAllBytes(file)):null;
            var manifest=new EyeLayerManifest{capturedUtc=DateTime.UtcNow.ToString("o"),gameVersion=Application.version,screenWidth=Screen.width,screenHeight=Screen.height};
            for(int sex=0;sex<2;sex++)for(int age=0;age<2;age++)
            {
                var look=EyeProofLook(sex,age);
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" / "+(age==0?"青年":"长者白眉")+" · 眼眉分层核对","同一角色、同一组选择。只在这张验收画面临时隐藏绘制层，不修改外观、存档或校准表。");
                var page=new EyeLayerPage{purpose="Same look: composite, brows hidden, isolated eyes magnified, isolated brows magnified. Check eye layer for a second brow."};
                string[] modes={"composite","without-brows","eyes-only","brows-only"};
                string[] labels={"正常合成","隐藏眉毛层","仅眼睛层 · 放大","仅眉毛层 · 放大"};
                for(int col=0;col<4;col++)
                {
                    EyeLayerRender(sheet,page,"s"+sex+"-age"+age+"-"+modes[col],look,20+col*394,110,376,650,modes[col]);
                    Label(sheet,labels[col],20+col*394,785,376,34,21,PaperInk);
                }
                yield return EyeLayerCapture(path,"eye-layers-s"+sex+"-age"+age,page,manifest);

                for(int changing=2;changing<=3;changing++)
                {
                    bool varyEyes=changing==2;
                    sheet=GroomingFitSheet((sex==0?"男性":"女性")+" / "+(age==0?"青年":"长者白眉")+" · "+(varyEyes?"固定眉毛，只换眼睛":"固定眼睛，只换眉毛"),"固定头部、肤色与其他部件；8 款逐一对比。变化部件之外的眼/眉 UV、贴图和几何必须保持一致。");
                    page=new EyeLayerPage{purpose=varyEyes?"Eight eyes with a fixed independent eyebrow.":"Eight eyebrows with a fixed independent eye pair."};
                    string baseline=null;
                    for(int option=0;option<8;option++)
                    {
                        var choice=AppearanceRules.Copy(look);choice.parts[changing]=option;
                        float x=20+(option%4)*394,y=102+(option/4)*383;
                        var frame=EyeLayerRender(sheet,page,"s"+sex+"-age"+age+"-part"+changing+"-"+option,choice,x,y,376,337,"composite");
                        string signature=EyeLayerSignature(frame,varyEyes?"ClassicBrows":"ClassicEyes");
                        if(baseline==null)baseline=signature;else if(signature!=baseline)throw new Exception("Switching "+(varyEyes?"eyes":"brows")+" changed the independent counterpart geometry or UV");
                        Label(sheet,(varyEyes?"眼睛 ":"眉毛 ")+(option+1)+" / 8",x,y+340,376,32,18,PaperInk);
                    }
                    yield return EyeLayerCapture(path,"eye-switch-s"+sex+"-age"+age+"-part"+changing,page,manifest);
                }
            }
            for(int sex=0;sex<2;sex++)
            {
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" · 8 款独立眼睛层","真实游戏眼层及材质，隐藏所有其他五官与头部后放大。上眼睑与睫毛应保留，眼睛素材中不应出现眉毛。");
                var page=new EyeLayerPage{purpose="All eight authored eyes isolated and magnified through the actual game shader, with no eyebrow layer enabled."};
                for(int option=0;option<8;option++)
                {
                    var choice=EyeProofLook(sex,0);choice.parts[2]=option;
                    float x=20+(option%4)*394,y=102+(option/4)*383;
                    EyeLayerRender(sheet,page,"eye-only-s"+sex+"-"+option,choice,x,y,376,337,"eyes-only");
                    Label(sheet,"眼睛 "+(option+1)+" / 8 · 不绘制眉层",x,y+340,376,32,18,PaperInk);
                }
                yield return EyeLayerCapture(path,"eye-only-s"+sex,page,manifest);
            }
            Draw();yield return null;
            manifest.runUnchanged=GroomingFitSnapshot(State)==run;manifest.profileUnchanged=GroomingFitSnapshot(profile)==player;manifest.draftUnchanged=GroomingFitSnapshot(lookDraft)==draft;
            manifest.isolatedFilesUnchanged=files.All(pair=>(File.Exists(pair.Key)?Convert.ToBase64String(File.ReadAllBytes(pair.Key)):null)==pair.Value);
            File.WriteAllText(Path.ChangeExtension(path,"eye-manifest.json"),JsonUtility.ToJson(manifest,true));
            if(!manifest.runUnchanged||!manifest.profileUnchanged||!manifest.draftUnchanged||!manifest.isolatedFilesUnchanged)throw new Exception("Eye proof mutated state, draft or isolated files");
            if(manifest.pages.Count!=14||manifest.renderCount!=96)throw new Exception("Eye proof coverage is incomplete");
            Debug.Log("MINGLU_EYE_LAYER_UI_SUCCESS: "+Screen.width+"x"+Screen.height+"; 14 actual-render pages / 96 samples; independent eye atlas; four sex/age layer proofs; fixed-eye/fixed-brow eight-way cycles; all16 isolated eye pairs; state/profile/draft/files unchanged. Absence of a painted second eyebrow requires visual review.");
        }
    }
}
