using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        Appearance GroomingProofLook(int sex,int face,int hair,int beard=0)
        {
            var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
            look.parts[0]=face;look.parts[1]=hair;look.parts[6]=sex==0?beard:0;
            look.parts[7]=0;look.parts[9]=3;look.parts[11]=0;
            AppearanceRules.ApplyAgeHairColor(look);
            return look;
        }
        IEnumerator GroomingSmoke(string path)
        {
            if(smokeSave==null)throw new Exception("Grooming smoke requires an isolated profile");
            yield return Click("HomeAppearance");
            if(LookOptions(1).Length!=8||LookOptions(6).Length!=8)throw new Exception("Expanded grooming catalog is not exposed in the editor");
            for(int sex=0;sex<2;sex++)
            {
                yield return Click(sex==0?"LookMale":"LookFemale");
                yield return CheckStudioCycle(1);
                if(sex==0)yield return CheckStudioCycle(6);
            }
            yield return Click("LookMale");
            yield return StudioSetOption(0,0);yield return StudioSetOption(12,0);
            yield return StudioSetOption(0,3);
            if(lookDraft.parts[12]!=3)throw new Exception("Elder selection did not default to silver hair and beard");
            yield return StudioSetOption(12,1);
            yield return Click("LookLock_12");
            yield return StudioSetOption(0,7);
            if(lookDraft.parts[12]!=1)throw new Exception("Elder selection overwrote locked hair colour");
            yield return StudioSetOption(12,1);yield return Click("LookLock_12");
            string beforeAge=JsonUtility.ToJson(lookDraft);
            yield return Click("LookAgeColor");
            if(lookDraft.parts[12]!=3)throw new Exception("Explicit age palette did not restore silver");
            yield return Click("LookUndo");
            if(JsonUtility.ToJson(lookDraft)!=beforeAge)throw new Exception("Age palette cannot be undone exactly");
            yield return Click("LookRedo");
            if(lookDraft.parts[12]!=3)throw new Exception("Age palette redo lost silver");
            yield return StudioSetOption(1,7);yield return StudioSetOption(6,7);
            CheckStudioPanel("AppearanceStudio");
            Capture(Path.ChangeExtension(path,"elder-editor.png"));
            string savedLook=JsonUtility.ToJson(lookDraft);
            yield return Click("LookPresets");yield return Click("LookSaveSlot");
            if(lookPopup=="overwrite")yield return Click("LookPopupConfirm");
            yield return Click("LookPopupBack");
            yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");
            if(JsonUtility.ToJson(lookDraft)!=savedLook)throw new Exception("Expanded hair/beard/silver choices changed through preset save/load");

            // One face per sheet keeps the silhouette comparison honest: all
            // eight hairstyles use exactly the same face, clothing and framing.
            for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)
            {
                Draw();var sheet=Box("GroomingHairProof",root,0,0,1600,900,Paper);
                Label(sheet,(sex==0?"男性":"女性")+" · 面容 "+(face+1)+" / 八款发型 · 同脸同衣同构图"+(AppearanceRules.IsElderFace(face)?" / 长者银白":""),24,12,1548,30,22,PaperInk);
                for(int hair=0;hair<8;hair++)
                {
                    var look=GroomingProofLook(sex,face,hair);
                    float x=24+hair%4*398,y=50+hair/4*420;
                    DrawLook(sheet,look,x,y,280,370);
                    DrawLook(sheet,look,x+288,y+80,72,106);
                    Label(sheet,"发型 "+(hair+1)+" / 8",x,y+376,280,28,18,PaperInk);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"hair-s"+sex+"-face"+face+".png"));
            }
            for(int face=0;face<8;face++)
            {
                Draw();var sheet=Box("GroomingBeardProof",root,0,0,1600,900,Paper);
                Label(sheet,"男性 · 面容 "+(face+1)+" / 无须与七款胡须 · 嘴角及下颌对照"+(AppearanceRules.IsElderFace(face)?" / 白发白须":""),24,12,1548,30,22,PaperInk);
                for(int beard=0;beard<8;beard++)
                {
                    var look=GroomingProofLook(0,face,1,beard);
                    // Every edited mouth and nose occurs on every face. These
                    // are local edits; beard registration must follow them.
                    look.parts[5]=beard;look.parts[4]=(beard+face)%8;
                    float x=24+beard%4*398,y=50+beard/4*420;
                    DrawRealisticLook(sheet,look,x,y,366,370,Color.white,true);
                    Label(sheet,AppearanceRules.Names[6][beard]+" · 嘴形 "+(beard+1),x,y+376,366,28,18,PaperInk);
                }
                yield return null;yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"beard-face"+face+".png"));
            }
            // Persist expanded indices through an actual new run and save/load,
            // rather than just serializing an isolated Appearance object.
            Draw();yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(JsonUtility.ToJson(State.chiefLook)!=savedLook)throw new Exception("New run changed expanded grooming choices");
            yield return SmokeSaveLoad();
            if(JsonUtility.ToJson(State.chiefLook)!=savedLook)throw new Exception("Run save/load changed expanded grooming choices");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_GROOMING_UI_SUCCESS: 128 face/hair pairs at portrait and roster size, 64 face/beard pairs with edited mouths/noses, all eight visible hair/beard choices, elder default/locked override/undo, preset and run save/load; contact sheets require visual review");
        }
    }
}
