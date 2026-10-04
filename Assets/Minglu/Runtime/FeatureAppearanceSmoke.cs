using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        bool FacePixelsDiffer(string before,string after)
        {
            var a=new Texture2D(2,2);var b=new Texture2D(2,2);a.LoadImage(File.ReadAllBytes(before));b.LoadImage(File.ReadAllBytes(after));
            var preview=GameObject.Find("StudioPreview").GetComponent<RectTransform>();var corners=new Vector3[4];preview.GetWorldCorners(corners);
            var bottom=RectTransformUtility.WorldToScreenPoint(viewCamera,corners[0]);var top=RectTransformUtility.WorldToScreenPoint(viewCamera,corners[2]);
            int changed=0;
            for(int x=Mathf.Max(0,Mathf.CeilToInt(bottom.x));x<Mathf.Min(a.width,Mathf.FloorToInt(top.x));x++)for(int y=Mathf.Max(0,Mathf.CeilToInt(bottom.y));y<Mathf.Min(a.height,Mathf.FloorToInt(top.y));y++)
            {Color ca=a.GetPixel(x,y),cb=b.GetPixel(x,y);if(Mathf.Abs(ca.r-cb.r)+Mathf.Abs(ca.g-cb.g)+Mathf.Abs(ca.b-cb.b)>.025f)changed++;}
            Destroy(a);Destroy(b);return changed>8;
        }
        IEnumerator FeatureAppearanceSmoke(string path)
        {
            yield return Click("HomeAppearance");
            if(root.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="LookStyle"))throw new Exception("Classic mode is still exposed");
            foreach(int sex in new[]{0,1})
            {
                yield return Click(sex==0?"LookMale":"LookFemale");
                for(int option=0;option<8;option++)
                {
                    yield return StudioSetOption(0,option);
                    yield return StudioSetOption(7,option);
                    Capture(Path.ChangeExtension(path,"proportions-sex"+sex+"-"+option+".png"));
                }
            }
            foreach(int sex in new[]{0,1})
            {
                yield return Click(sex==0?"LookMale":"LookFemale");yield return StudioSetOption(0,0);
                foreach(int category in new[]{2,3,4,5,13})
                {
                    yield return StudioSetOption(category,0);
                    string before=Path.ChangeExtension(path,"sex"+sex+"-part"+category+"-before.png");Capture(before);
                    var original=AppearanceRules.Copy(lookDraft);
                    for(int option=1;option<LookOptions(category).Length;option++)
                    {
                        yield return StudioSetOption(category,option);
                        var expectedPart=AppearanceRules.Copy(original);if(category==13)expectedPart.ears=option;else expectedPart.parts[category]=option;
                        if(JsonUtility.ToJson(lookDraft)!=JsonUtility.ToJson(expectedPart))throw new Exception("Feature cycle changed another component: "+sex+" / "+category+" / "+option);
                    }
                    string after=Path.ChangeExtension(path,"sex"+sex+"-part"+category+"-after.png");Capture(after);
                    if(!FacePixelsDiffer(before,after))throw new Exception("Expanded feature did not change portrait pixels: "+sex+" / "+category);
                }
            }
            foreach(int sex in new[]{0,1})
            {
                yield return Click(sex==0?"LookMale":"LookFemale");
                int eyes=lookDraft.parts[2],nose=lookDraft.parts[4],mouth=lookDraft.parts[5];
                for(int option=0;option<LookOptions(1).Length;option++)
                {yield return StudioSetOption(1,option);Capture(Path.ChangeExtension(path,"hair-sex"+sex+"-"+option+".png"));}
                if(lookDraft.parts[2]!=eyes||lookDraft.parts[4]!=nose||lookDraft.parts[5]!=mouth)throw new Exception("Hair changed facial parts");
                if(sex==0)
                {
                    for(int option=0;option<LookOptions(6).Length;option++){yield return StudioSetOption(6,option);Capture(Path.ChangeExtension(path,"beard-"+option+".png"));}
                }
                yield return StudioSetOption(12,2);Capture(Path.ChangeExtension(path,"gray-hair-sex"+sex+".png"));
            }
            yield return Click("LookPresets");yield return Click("LookSaveSlot");string expected=JsonUtility.ToJson(lookDraft);yield return Click("LookPopupBack");yield return Click("LookRandom");yield return Click("LookPresets");yield return Click("LookLoadSlot");
            if(JsonUtility.ToJson(lookDraft)!=expected)throw new Exception("Feature preset mismatch");
            yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(JsonUtility.ToJson(State.chiefLook)!=expected)throw new Exception("Feature new run mismatch");
            uint rng=State.random;yield return Click("CommandOverview");yield return Click("CommandChiefAppearance");yield return StudioSetOption(4,(LookIndex(4)+1)%LookOptions(4).Length);yield return Click("LookCancel");yield return Click("LookPopupConfirm");
            yield return SmokeSaveLoad();
            if(JsonUtility.ToJson(State.chiefLook)!=expected||State.random!=rng)throw new Exception("Feature cancel/save mutated state");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_FEATURE_UI_SUCCESS: both sexes, every facial/ear option independently rendered through cycle controls, hairstyles, beards, hair tint, preview pixel changes, preset, new run, cancel, save/load");
        }
    }
}

