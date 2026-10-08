using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
            public string file, comparison;
            public List<GroomingFitCase> cases=new List<GroomingFitCase>();
        }

        [Serializable] sealed class GroomingFitManifest
        {
            public string gameVersion, capturedUtc;
            public string result="Rendered comparisons only; visual review is required. No aesthetic pass is inferred from resource or state checks.";
            public string indices="All IDs, face/hair/beard/feature indices in this manifest are zero-based.";
            public string fixturePolicy="Hair pages change only hair within each face. Beard pages change only beard, including a clean-shaven baseline. Male matrices use eyes2/brows1/nose5/mouth0/ears2; female matrices use native features. Elder faces3/7 use silver. Matrix clothing is0; approximate user male clothing is7, female clothing is5.";
            public int screenWidth, screenHeight, hairPairs, beardedPairs, cleanShavenBaselines, approximateUserCases, renderCount;
            public bool profileUnchanged, runUnchanged, editorDraftUnchanged, isolatedFilesUnchanged;
            public List<GroomingFitPage> pages=new List<GroomingFitPage>();
        }

        Appearance GroomingFitLook(int sex,int face,int hair,int beard=0)
        {
            var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
            look.parts[0]=face;look.parts[1]=hair;look.parts[6]=sex==0?beard:0;
            look.parts[7]=0;look.parts[9]=3;look.parts[10]=0;look.parts[11]=0;
            if(sex==0)
            {
                // Assign after normalization so portrait migration cannot reset
                // the specific facial edits from the reported male example.
                look.parts[2]=2;look.parts[3]=1;look.parts[4]=5;look.parts[5]=0;look.ears=2;
            }
            AppearanceRules.ApplyAgeHairColor(look);
            AppearanceRules.Validate(look);
            return look;
        }

        RectTransform GroomingFitSheet(string title,string subtitle)
        {
            Draw();
            var sheet=Box("GroomingFitProof",root,0,0,1600,900,Paper);
            Label(sheet,title,20,15,1560,34,25,PaperInk);
            Label(sheet,subtitle,20,54,1560,30,17,PaperMuted);
            return sheet;
        }

        void GroomingFitRender(Transform parent,GroomingFitPage page,string id,Appearance look,float x,float y,float w,float h,bool headOnly)
        {
            string before=JsonUtility.ToJson(look);
            int previous=parent.GetComponentsInChildren<PaintedPortraitGraphic>().Length;
            // This is the game's actual UI renderer and shader, not a separate
            // image compositor. Use its detail framing to expose the seams.
            DrawRealisticLook(parent,look,x,y,w,h,Color.white,headOnly);
            var layers=parent.GetComponentsInChildren<PaintedPortraitGraphic>();
            int expected=look.sex==0&&look.parts[6]>0?5:4;
            if(layers.Length-previous!=expected)throw new Exception("Grooming fit missing portrait layers: "+id);
            for(int i=previous;i<layers.Length;i++)
                if(layers[i].texture==null||layers[i].material==null||layers[i].material.shader==null||!layers[i].material.shader.isSupported)
                    throw new Exception("Grooming fit resource/shader unavailable: "+id);
            if(JsonUtility.ToJson(look)!=before)throw new Exception("Portrait drawing mutated the fixture: "+id);
            page.cases.Add(new GroomingFitCase{id=id,look=AppearanceRules.Copy(look),uiRect=new[]{x,y,w,h},headOnly=headOnly});
        }

        IEnumerator GroomingFitCapture(string path,string suffix,GroomingFitPage page,GroomingFitManifest manifest)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            string output=Path.ChangeExtension(path,suffix+".png");
            Capture(output);
            if(!File.Exists(output)||new FileInfo(output).Length==0)throw new Exception("Grooming fit capture missing: "+output);
            page.file=Path.GetFileName(output);
            manifest.pages.Add(page);manifest.renderCount+=page.cases.Count;
        }

        static string GroomingFitSnapshot(object value){return value==null?"null":JsonUtility.ToJson(value);}

        IEnumerator GroomingFitSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave)||ProfilePath!=smokeSave+".profile"||!atTitle)
                throw new Exception("Grooming fit smoke requires --minglu-capture and its isolated title profile");
            var profileBefore=GroomingFitSnapshot(profile);
            var runBefore=GroomingFitSnapshot(State);
            var draftBefore=GroomingFitSnapshot(lookDraft);
            // No profile, preset or run writes are needed for this comparison.
            // Preserve even pre-existing isolated files exactly, rather than
            // deleting them and hiding an accidental save by the renderer.
            string[] isolatedFiles={SavePath,SavePath+".bak",SavePath+".tmp",ProfilePath,ProfilePath+".bak",ProfilePath+".tmp"};
            var filesBefore=new Dictionary<string,string>();
            foreach(string file in isolatedFiles)filesBefore[file]=File.Exists(file)?Convert.ToBase64String(File.ReadAllBytes(file)):null;
            var manifest=new GroomingFitManifest{gameVersion=Application.version,capturedUtc=DateTime.UtcNow.ToString("o"),screenWidth=Screen.width,screenHeight=Screen.height};

            // Produce the two reported combinations first so a reviewer can
            // inspect their portrait and head detail before the larger matrix.
            for(int sex=0;sex<2;sex++)
            {
                var look=GroomingFitLook(sex,sex==0?0:3,sex==0?2:1,sex==0?4:0);
                look.parts[7]=sex==0?7:5;
                string title=(sex==0?"男性":"女性")+" · 用户截图近似组合 / 原图比例与头部细节";
                var sheet=GroomingFitSheet(title,"截图未包含的选项采用固定基线；这里只复现绘制，不应用、保存或替换玩家外观。所有下列索引从 0 开始。");
                var page=new GroomingFitPage{comparison="Approximate user example, same look rendered full portrait and head detail; screenshot identification is not exact."};
                GroomingFitRender(sheet,page,"user-s"+sex+"-portrait",look,20,100,460,730,false);
                GroomingFitRender(sheet,page,"user-s"+sex+"-detail",look,502,100,680,730,true);
                string hair=(sex==0?AppearanceRules.MaleHairNames:AppearanceRules.FemaleHairNames)[look.parts[1]];
                string description="性别 "+sex+" / 脸型 "+look.parts[0]+"\n\n发型 "+look.parts[1]+" · "+hair+
                    "\n胡须 "+look.parts[6]+" · "+AppearanceRules.BeardNames[look.parts[6]]+
                    "\n服装 "+look.parts[7]+" · "+AppearanceRules.Names[7][look.parts[7]]+
                    "\n须发色 "+look.parts[12]+" · "+AppearanceRules.Names[12][look.parts[12]]+
                    "\n\n眼睛 "+look.parts[2]+" / 眉形 "+look.parts[3]+"\n鼻形 "+look.parts[4]+" / 嘴形 "+look.parts[5]+"\n耳朵 "+look.ears+
                    "\n\n检查重点\n发际线与额头\n鬓角与耳朵\n胡须与嘴角、下颌\n颈部与衣领\n\n需逐图人工判断协调程度。";
                Label(sheet,description,1210,110,370,692,21,PaperInk);
                Label(sheet,"完整立绘",20,846,460,28,18,PaperInk);
                Label(sheet,"同一组合 · 游戏原生头部裁切",502,846,680,28,18,PaperInk);
                yield return GroomingFitCapture(path,"fit-user-"+(sex==0?"male":"female"),page,manifest);
                manifest.approximateUserCases++;
            }

            // Four tall detail panels per page keep the hairline and ear edges
            // readable. Each face uses two pages covering all eight hairstyles.
            for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)for(int half=0;half<2;half++)
            {
                string fixedParts=sex==0?"固定五官：眼2 / 眉1 / 鼻5 / 嘴0 / 耳2":"固定原生五官";
                var sheet=GroomingFitSheet((sex==0?"男性":"女性")+" · 脸型 "+face+" / 发型局部对照 "+(half+1)+" / 2",
                    "同脸、同衣、同色、同构图；本页只换发型。"+fixedParts+"。"+(AppearanceRules.IsElderFace(face)?"长者银白。":"乌黑。")+"索引从 0 开始。");
                var page=new GroomingFitPage{comparison="Only hair changes within this page and its paired page; facial edits, face, beard0, outfit0 and age palette remain fixed."};
                for(int col=0;col<4;col++)
                {
                    int hair=half*4+col;float x=20+col*394;
                    var look=GroomingFitLook(sex,face,hair);
                    GroomingFitRender(sheet,page,"hair-s"+sex+"-face"+face+"-hair"+hair,look,x,100,376,704,true);
                    string name=(sex==0?AppearanceRules.MaleHairNames:AppearanceRules.FemaleHairNames)[hair];
                    Label(sheet,"H"+hair+" · "+name+"\n"+AppearanceRules.Names[12][look.parts[12]]+" / 无须 / 交领袍",x,816,376,65,20,PaperInk);
                    manifest.hairPairs++;
                }
                yield return GroomingFitCapture(path,"fit-hair-s"+sex+"-face"+face+"-page"+(half+1),page,manifest);
            }

            // Include a clean-shaven baseline. Unlike the old proof, changing
            // beard must never also change the mouth or nose being compared.
            for(int face=0;face<8;face++)for(int half=0;half<2;half++)
            {
                var sheet=GroomingFitSheet("男性 · 脸型 "+face+" / 胡须局部对照 "+(half+1)+" / 2",
                    "只换胡须；固定短束、交领袍、眼2 / 眉1 / 鼻5 / 嘴0 / 耳2。"+(AppearanceRules.IsElderFace(face)?"长者白发白须。":"黑发黑须。")+"索引从 0 开始。");
                var page=new GroomingFitPage{comparison="Only beard changes within this page and its paired page. Hair1, eyes2/brows1/nose5/mouth0/ears2, outfit0 and age palette remain fixed; beard0 is the clean-shaven baseline."};
                for(int col=0;col<4;col++)
                {
                    int beard=half*4+col;float x=20+col*394;
                    var look=GroomingFitLook(0,face,1,beard);
                    GroomingFitRender(sheet,page,"beard-face"+face+"-beard"+beard,look,x,100,376,704,true);
                    Label(sheet,"B"+beard+" · "+AppearanceRules.BeardNames[beard]+"\n"+AppearanceRules.Names[12][look.parts[12]]+" / 鼻5 / 嘴0",x,816,376,65,20,PaperInk);
                    if(beard==0)manifest.cleanShavenBaselines++;else manifest.beardedPairs++;
                }
                yield return GroomingFitCapture(path,"fit-beard-face"+face+"-page"+(half+1),page,manifest);
            }

            manifest.profileUnchanged=GroomingFitSnapshot(profile)==profileBefore;
            manifest.runUnchanged=GroomingFitSnapshot(State)==runBefore;
            manifest.editorDraftUnchanged=GroomingFitSnapshot(lookDraft)==draftBefore;
            manifest.isolatedFilesUnchanged=true;
            foreach(string file in isolatedFiles)
                if((File.Exists(file)?Convert.ToBase64String(File.ReadAllBytes(file)):null)!=filesBefore[file])manifest.isolatedFilesUnchanged=false;
            File.WriteAllText(Path.ChangeExtension(path,"fit-manifest.json"),JsonUtility.ToJson(manifest,true));
            if(!manifest.profileUnchanged||!manifest.runUnchanged||!manifest.editorDraftUnchanged||!manifest.isolatedFilesUnchanged)
                throw new Exception("Grooming fit renderer changed profile/run/editor draft or wrote an isolated save; inspect fit-manifest.json");
            if(manifest.pages.Count!=50||manifest.hairPairs!=128||manifest.beardedPairs!=56||manifest.cleanShavenBaselines!=8||manifest.approximateUserCases!=2||manifest.renderCount!=196)
                throw new Exception("Grooming fit comparison coverage incomplete; inspect fit-manifest.json");
            Debug.Log("MINGLU_GROOMING_FIT_RENDER_SUCCESS: 50 actual Unity screenshots, 128 face/hair pairs, 56 face/beard pairs plus 8 clean-shaven baselines, 2 approximate user looks at portrait/detail sizes; profile/run/draft/files unchanged. This verifies rendering coverage only; visual review is required.");
        }
    }
}
