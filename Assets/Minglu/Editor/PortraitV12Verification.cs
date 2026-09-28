using System;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class PortraitV12Verification
    {
        public static void Verify()
        {
            var legacy=new Appearance{enabled=true,style=0};var migrated=AppearanceRules.AsRealistic(legacy);
            if(migrated.style!=1||!migrated.featureEditing||legacy.style!=0)throw new Exception("Legacy conversion must be non-destructive");
            var asset=Resources.Load<Texture2D>("Minglu/Art/expansion-atlas-v12");if(asset==null)throw new Exception("Missing portrait expansion");
            bool newFace=false,newClothes=false;
            for(int seed=1;seed<=200;seed++){var a=AppearanceRules.Randomize(seed);AppearanceRules.Validate(a);newFace|=a.parts[0]>=4;newClothes|=a.parts[7]>=4;}
            if(!newFace||!newClothes)throw new Exception("New resources absent from random generation");
            Debug.Log("MINGLU_V12_VERIFY_SUCCESS: realistic-only legacy conversion, expansion loaded, 200 randomized appearances include new faces and outfits");
        }
        public static void BatchDeliver(){try{Verify();FeatureAppearanceVerification.Verify();AppearanceVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
