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
            int changed=0;float scale=Screen.height/900f;
            for(int x=(int)(402*scale);x<(int)(603*scale);x++)for(int y=(int)(Screen.height-488*scale);y<(int)(Screen.height-309*scale);y++)
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
                    yield return Click("LookCategory_0");yield return Click("LookOption_"+option);
                    yield return Click("LookCategory_13");
                    yield return Click("LookCategory_7");yield return Click("LookOption_"+option);
                    Capture(Path.ChangeExtension(path,"proportions-sex"+sex+"-"+option+".png"));
                }
            }
            foreach(int sex in new[]{0,1})
            {
                yield return Click(sex==0?"LookMale":"LookFemale");yield return Click("LookCategory_0");yield return Click("LookOption_0");
                foreach(int category in new[]{2,3,4,5,13})
                {
                    yield return Click("LookCategory_"+category);yield return Click("LookOption_0");
                    string before=Path.ChangeExtension(path,"sex"+sex+"-part"+category+"-before.png");Capture(before);
                    for(int option=1;option<4;option++)yield return Click("LookOption_"+option);
                    string after=Path.ChangeExtension(path,"sex"+sex+"-part"+category+"-after.png");Capture(after);
                    if(!FacePixelsDiffer(before,after))throw new Exception("Feature did not change portrait pixels: "+sex+" / "+category);
                }
            }
            foreach(int sex in new[]{0,1})
            {
                yield return Click(sex==0?"LookMale":"LookFemale");
                int eyes=lookDraft.parts[2],nose=lookDraft.parts[4],mouth=lookDraft.parts[5];
                yield return Click("LookCategory_1");
                for(int option=0;option<4;option++)
                {yield return Click("LookOption_"+option);Capture(Path.ChangeExtension(path,"hair-sex"+sex+"-"+option+".png"));}
                if(lookDraft.parts[2]!=eyes||lookDraft.parts[4]!=nose||lookDraft.parts[5]!=mouth)throw new Exception("Hair changed facial parts");
                if(sex==0)
                {
                    yield return Click("LookCategory_6");
                    for(int option=0;option<4;option++){yield return Click("LookOption_"+option);Capture(Path.ChangeExtension(path,"beard-"+option+".png"));}
                }
                yield return Click("LookCategory_12");yield return Click("LookOption_2");Capture(Path.ChangeExtension(path,"gray-hair-sex"+sex+".png"));
            }
            yield return Click("LookSaveSlot");string expected=JsonUtility.ToJson(lookDraft);yield return Click("LookRandom");yield return Click("LookLoadSlot");
            if(JsonUtility.ToJson(lookDraft)!=expected)throw new Exception("Feature preset mismatch");
            yield return Click("LookApply");yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(JsonUtility.ToJson(State.chiefLook)!=expected)throw new Exception("Feature new run mismatch");
            uint rng=State.random;yield return Click("ChiefAppearance");yield return Click("LookCategory_4");yield return Click("LookOption_1");yield return Click("LookCancel");
            yield return Click("Save");yield return Click("Load");
            if(JsonUtility.ToJson(State.chiefLook)!=expected||State.random!=rng)throw new Exception("Feature cancel/save mutated state");
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_FEATURE_UI_SUCCESS: both sexes, five independently rendered features, 40 facial options, 8 hairstyles, 4 beard options, hair tint, portrait pixel changes, preset, new run, cancel, save/load");
        }
    }
}

