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
        [Serializable] sealed class GroomingFitCase
        {
            public string id;
            public Appearance look;
            public float[] uiRect;
            public bool headOnly;
        }
        [Serializable] sealed class GroomingFitPage
        {
            public string file,comparison;
            public List<GroomingFitCase> cases=new List<GroomingFitCase>();
        }
        [Serializable] sealed class GroomingFitManifest
        {
            public string gameVersion,capturedUtc;
            public string renderer="ClassicPortrait";
            public string result="Actual Unity render coverage, not aesthetic acceptance; every page requires visual review.";
            public string indices="Zero based. Complete head includes face outline, hair, ears and neck. No independent hair or ear channel.";
            public int screenWidth,screenHeight,headPairs,beardedPairs,cleanShavenBaselines,outfitSamples,featureSamples,heroCases,renderCount;
            public bool profileUnchanged,runUnchanged,editorDraftUnchanged,isolatedFilesUnchanged;
            public List<GroomingFitPage> pages=new List<GroomingFitPage>();
        }
        Appearance ClassicProofLook(int sex,int head,int beard=0)
        {
            var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
            look.parts[0]=head;look.parts[6]=sex==0?beard:0;look.parts[7]=0;look.parts[9]=3;
            look.parts[10]=0;look.parts[11]=0;AppearanceRules.ApplyAgeHairColor(look);AppearanceRules.Validate(look);return look;
        }
        void RequireClassicPortrait(Transform frame,Appearance appearance)
        {
            if(frame==null||frame.name!="ClassicPortrait")throw new Exception("The actual classic portrait renderer was not used");
            var look=AppearanceRules.AsRealistic(appearance);
            var backdrop=frame.GetComponentsInChildren<RawImage>().FirstOrDefault(t=>t.name=="ClassicBackdrop");
            if(backdrop==null||backdrop.texture==null)throw new Exception("Classic painted backdrop is missing");
            var names=new List<string>{"ClassicNeck","ClassicHead","ClassicEyes","ClassicBrows","ClassicNose","ClassicMouth","ClassicOutfit"};
            if(look.sex==0&&look.parts[6]>0)names.Add("ClassicBeard");
            if(look.parts[8]>0)names.Add("ClassicAdornment");
            foreach(string name in names)
            {
                var layer=frame.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==name);
                var graphic=layer==null?null:layer.GetComponent<Graphic>();
                if(graphic==null||graphic.mainTexture==null||graphic.material==null||graphic.material.shader==null||!graphic.material.shader.isSupported)
                    throw new Exception("Classic portrait layer is absent or cannot render: "+name);
                if(graphic.material.shader.name!="Minglu/ClassicPortrait")throw new Exception("Classic portrait silently fell back to a different material: "+name);
            }
            if(frame.GetComponentsInChildren<Transform>().Any(t=>t.name=="PaintedPortrait"||t.name=="RealisticPortrait"))throw new Exception("Legacy portrait hierarchy remains active");
        }
        void RequireVisibleClassicPortraits()
        {
            var frames=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="ClassicPortrait").ToArray();
            if(frames.Length==0)throw new Exception("No classic portraits appear in this actual game view");
            foreach(var frame in frames)
            {
                var head=frame.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="ClassicHead");
                if(head==null||head.GetComponent<Graphic>()==null||head.GetComponent<Graphic>().mainTexture==null)throw new Exception("Game view contains a missing classic head");
            }
        }
        RectTransform GroomingFitSheet(string title,string subtitle)
        {
            Draw();var sheet=Box("GroomingFitProof",root,0,0,1600,900,Paper);
            Label(sheet,title,20,15,1560,34,25,PaperInk);Label(sheet,subtitle,20,54,1560,30,17,PaperMuted);return sheet;
        }
        void GroomingFitRender(Transform parent,GroomingFitPage page,string id,Appearance look,float x,float y,float w,float h,bool headOnly)
        {
            string before=JsonUtility.ToJson(look);int previous=parent.childCount;
            DrawRealisticLook(parent,look,x,y,w,h,Color.white,headOnly);
            var added=new List<Transform>();for(int i=previous;i<parent.childCount;i++)added.Add(parent.GetChild(i));
            var frame=added.FirstOrDefault(t=>t.name=="ClassicPortrait");RequireClassicPortrait(frame,look);
            if(JsonUtility.ToJson(look)!=before)throw new Exception("Drawing changed the appearance fixture: "+id);
            page.cases.Add(new GroomingFitCase{id=id,look=AppearanceRules.Copy(look),uiRect=new[]{x,y,w,h},headOnly=headOnly});
        }
        IEnumerator GroomingFitCapture(string path,string suffix,GroomingFitPage page,GroomingFitManifest manifest)
        {
            yield return null;yield return new WaitForEndOfFrame();string output=Path.ChangeExtension(path,suffix+".png");Capture(output);
            if(!File.Exists(output)||new FileInfo(output).Length==0)throw new Exception("Classic rendering capture missing: "+output);
            page.file=Path.GetFileName(output);manifest.pages.Add(page);manifest.renderCount+=page.cases.Count;
        }
        static string GroomingFitSnapshot(object value){return value==null?"null":JsonUtility.ToJson(value);}
        IEnumerator GroomingFitSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave)||ProfilePath!=smokeSave+".profile"||!atTitle)throw new Exception("Classic render coverage requires the isolated title-screen capture session");
            string profileBefore=GroomingFitSnapshot(profile),runBefore=GroomingFitSnapshot(State),draftBefore=GroomingFitSnapshot(lookDraft);
            var filesBefore=new Dictionary<string,string>();
            foreach(string file in new[]{smokeSave,smokeSave+".bak",smokeSave+".tmp",ProfilePath,ProfilePath+".bak",ProfilePath+".tmp"})filesBefore[file]=File.Exists(file)?Convert.ToBase64String(File.ReadAllBytes(file)):null;
            var manifest=new GroomingFitManifest{gameVersion=Application.version,capturedUtc=DateTime.UtcNow.ToString("o"),screenWidth=Screen.width,screenHeight=Screen.height};
            for(int sex=0;sex<2;sex++)
            {
                var look=ClassicProofLook(sex,sex==0?1:9,sex==0?4:0);look.parts[7]=sex==0?7:5;
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" · 全新绘卷 / 半身与面容", "独立绘制完整头部；不再组合旧发型与旧脸型。仅生成验收图片，不应用到任何角色。");
                var page=new GroomingFitPage{comparison="New complete-head examples, rendered through the game renderer at full portrait and detail size; not a reconstruction of the deleted art."};
                GroomingFitRender(sheet,page,"hero-s"+sex+"-portrait",look,20,100,460,730,false);GroomingFitRender(sheet,page,"hero-s"+sex+"-detail",look,502,100,680,730,true);
                Label(sheet,"性别 "+sex+" / 完整头部 "+look.parts[0]+"\n\n胡须 "+look.parts[6]+" / 衣服 "+look.parts[7]+"\n须发色 "+look.parts[12]+"\n\n实际游戏绘制\n头部、五官、衣饰\n\n需人工判断自然度。",1210,120,370,650,21,PaperInk);
                yield return GroomingFitCapture(path,"fit-user-"+(sex==0?"male":"female"),page,manifest);manifest.heroCases++;
            }
            for(int sex=0;sex<2;sex++)for(int batch=0;batch<3;batch++)
            {
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" · 完整头部 "+(batch*4)+"–"+(batch*4+3),"同衣服、固定五官；每款头部包含各自脸廓、发式、耳廓与颈部。旁图为名册尺寸。索引从 0 开始。");
                var page=new GroomingFitPage{comparison="Complete heads, each at portrait and roster size; clothing/features fixed. Elder heads9–11 use silver."};
                for(int col=0;col<4;col++)
                {
                    int head=batch*4+col;float x=20+col*394;var look=ClassicProofLook(sex,head);
                    GroomingFitRender(sheet,page,"head-s"+sex+"-h"+head,look,x,100,282,704,false);GroomingFitRender(sheet,page,"roster-s"+sex+"-h"+head,look,x+288,470,78,117,false);
                    Label(sheet,"H"+head+" · "+AppearanceRules.HeadNames(sex)[head],x,815,376,34,19,PaperInk);manifest.headPairs++;
                }
                yield return GroomingFitCapture(path,"fit-head-s"+sex+"-page"+(batch+1),page,manifest);
            }
            for(int head=0;head<12;head++)for(int batch=0;batch<2;batch++)
            {
                var sheet=GroomingFitSheet("男性 · 完整头部 "+head+" / 胡须对照 "+(batch+1)+" / 2","同头部、同五官、同衣、同色；本页只替换胡须。检查嘴角、颊面和下颌。索引从 0 开始。");
                var page=new GroomingFitPage{comparison="Only beard changes on each fixed complete head. B0 clean-shaven reference; older heads use silver."};
                for(int col=0;col<4;col++)
                {
                    int beard=batch*4+col;float x=20+col*394;var look=ClassicProofLook(0,head,beard);
                    GroomingFitRender(sheet,page,"beard-h"+head+"-b"+beard,look,x,100,376,704,true);
                    Label(sheet,"B"+beard+" · "+AppearanceRules.BeardNames[beard],x,814,376,34,19,PaperInk);
                    if(beard==0)manifest.cleanShavenBaselines++;else manifest.beardedPairs++;
                }
                yield return GroomingFitCapture(path,"fit-beard-head"+head+"-page"+(batch+1),page,manifest);
            }
            for(int sex=0;sex<2;sex++)for(int batch=0;batch<2;batch++)
            {
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" · 衣服衔接 "+(batch+1)+" / 2","固定完整头部；只换衣服。检查颈部和衣领、肩膀宽度；另含饰物抽样。");
                var page=new GroomingFitPage{comparison="Eight clothing samples per sex, head held fixed; four adornments repeat across clothing samples."};
                for(int col=0;col<4;col++)
                {
                    int outfit=batch*4+col;float x=20+col*394;var look=ClassicProofLook(sex,sex==0?4:7);look.parts[7]=outfit;look.parts[8]=outfit%4;
                    GroomingFitRender(sheet,page,"outfit-s"+sex+"-o"+outfit,look,x,100,376,704,false);
                    Label(sheet,"服装 "+outfit+" / 饰物 "+look.parts[8],x,814,376,34,19,PaperInk);manifest.outfitSamples++;
                }
                yield return GroomingFitCapture(path,"fit-outfit-s"+sex+"-page"+(batch+1),page,manifest);
            }
            for(int sex=0;sex<2;sex++)for(int category=2;category<=5;category++)for(int batch=0;batch<2;batch++)
            {
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" · "+AppearanceRules.Labels[category]+"部件 "+(batch+1)+" / 2","固定头部与其他五官；本页仅更换这一项实际部件。索引从 0 开始。");
                var page=new GroomingFitPage{comparison="Only one authored facial feature changes. Same head, other features, clothing and palette."};
                for(int col=0;col<4;col++)
                {
                    int option=batch*4+col;float x=20+col*394;var look=ClassicProofLook(sex,0);look.parts[category]=option;
                    GroomingFitRender(sheet,page,"feature-s"+sex+"-c"+category+"-o"+option,look,x,100,376,704,true);
                    Label(sheet,AppearanceRules.Labels[category]+" "+option+" · "+AppearanceRules.Names[category][option],x,814,376,34,19,PaperInk);manifest.featureSamples++;
                }
                yield return GroomingFitCapture(path,"fit-feature-s"+sex+"-c"+category+"-page"+(batch+1),page,manifest);
            }
            Draw();yield return null;
            manifest.profileUnchanged=GroomingFitSnapshot(profile)==profileBefore;manifest.runUnchanged=GroomingFitSnapshot(State)==runBefore;manifest.editorDraftUnchanged=GroomingFitSnapshot(lookDraft)==draftBefore;
            manifest.isolatedFilesUnchanged=filesBefore.All(pair=>(File.Exists(pair.Key)?Convert.ToBase64String(File.ReadAllBytes(pair.Key)):null)==pair.Value);
            File.WriteAllText(Path.ChangeExtension(path,"fit-manifest.json"),JsonUtility.ToJson(manifest,true));
            if(!manifest.profileUnchanged||!manifest.runUnchanged||!manifest.editorDraftUnchanged||!manifest.isolatedFilesUnchanged)throw new Exception("Classic render coverage mutated state or isolated files");
            if(manifest.pages.Count!=52||manifest.headPairs!=24||manifest.beardedPairs!=84||manifest.cleanShavenBaselines!=12||manifest.outfitSamples!=16||manifest.featureSamples!=64||manifest.heroCases!=2||manifest.renderCount!=228)throw new Exception("Classic render coverage incomplete; inspect fit-manifest.json");
            Debug.Log("MINGLU_GROOMING_FIT_RENDER_SUCCESS: ClassicPortrait only; 52 actual Unity screenshots, 24 complete heads at portrait/roster size, 84 head/beard pairs plus12 clean-shaven references, 16 outfits/adornments, 64 feature samples, 2 hero examples; profile/run/draft/files unchanged. Render coverage does not establish aesthetic acceptance.");
        }
    }
}
