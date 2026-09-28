using System;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class PortraitRepairVerification
    {
        public static void BatchDeliver()
        {
            try
            {
                for(int s=0;s<200;s++){var a=AppearanceRules.Randomize(s);AppearanceRules.Validate(a);if(a.portraitVersion!=15)throw new Exception("Morph version");if(JsonUtility.ToJson(a)!=JsonUtility.ToJson(AppearanceRules.AsRealistic(a)))throw new Exception("Repeated migration changes a new look");}
                var old=new Appearance{enabled=true};old.parts[4]=3;
                var migrated=AppearanceRules.AsRealistic(old);
                if(old.parts[4]!=3||migrated.parts[4]!=0||migrated.portraitVersion!=15)throw new Exception("Legacy donor migration");
                for(int sex=0;sex<2;sex++)for(int f=0;f<8;f++)for(int hair=0;hair<4;hair++)
                {var a=AppearanceRules.Randomize(1,sex);a.parts[0]=f;var src=PortraitLayout.HairSource[sex,hair];var dst=PortraitLayout.Grooming(sex,hair,a);if(Mathf.Abs(src.width/src.height-dst.width/dst.height)>.001f)throw new Exception("Hair aspect stretched");}
                var shader=Resources.Load<Shader>("Minglu/Art/PortraitFeatures");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Morph shader compile");
                Debug.Log("MINGLU_PORTRAIT_RULES_SUCCESS: 200 looks, migration idempotence, 64 proportional hair registrations, shader");
                PrototypeTools.BatchBuild();EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
