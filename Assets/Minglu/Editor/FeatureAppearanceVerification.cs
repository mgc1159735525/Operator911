using System;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class FeatureAppearanceVerification
    {
        public static void Verify()
        {
            var old=JsonUtility.FromJson<Appearance>("{\"enabled\":true,\"style\":1,\"sex\":1,\"parts\":[2,0,3,1,2,0,0,0,0,0,0,0,0]}");
            if(old.featureEditing)throw new Exception("Legacy appearance must retain base face");
            for(int i=0;i<200;i++){var a=AppearanceRules.Randomize(i);AppearanceRules.Validate(a);if(!a.featureEditing||!a.groomingEditing||a.ears<0||a.ears>3||(a.sex==1&&a.parts[6]!=0))throw new Exception("Generated features invalid");}
            var sample=AppearanceRules.Randomize(19);var clone=AppearanceRules.Copy(sample);
            if(JsonUtility.ToJson(sample)!=JsonUtility.ToJson(clone))throw new Exception("Feature fields not copied");
            sample.ears=5;bool rejected=false;try{AppearanceRules.Validate(sample);}catch(ArgumentException){rejected=true;}
            if(!rejected)throw new Exception("Invalid ears accepted");
            var shader=Resources.Load<Shader>("Minglu/Art/PortraitFeatures");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Feature shader failed compilation");
            if(Resources.Load<Texture2D>("Minglu/Art/bald-atlas-v11")==null||Resources.Load<Texture2D>("Minglu/Art/hair-atlas-v11")==null)throw new Exception("Missing grooming assets");
            Debug.Log("MINGLU_FEATURE_VERIFY_SUCCESS: legacy unchanged, 200 generated feature sets, copy, invalid ears, shader compilation");
        }
        public static void BatchDeliver(){try{Verify();RealisticAppearanceVerification.Verify();AppearanceVerification.Verify();PrototypeTools.BatchBuild();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
