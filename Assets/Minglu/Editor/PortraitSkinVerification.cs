using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    // Capture the authoring preview's real shared compositor, never a mock-up.
    public static class PortraitSkinVerification
    {
        public static void Capture()
        {
            try
            {
                var args=Environment.GetCommandLineArgs();
                bool before=Array.IndexOf(args,"--skin-before")>=0;
                string prefix="outputs/qa-current/skin-"+(before?"before":"after");
                string table=File.ReadAllText(PortraitCalibrationWindow.ProjectCsv);
                var rows=PortraitCalibration.CommittedSnapshot();
                var files=new List<string>();
                using(var preview=new ClassicPortraitPreview())
                {
                    for(int sex=0;sex<2;sex++)for(int age=0;age<2;age++)for(int skin=0;skin<4;skin++)
                    {
                        var a=AppearanceRules.Randomize(430,sex,0);a.sex=sex;a.parts=new int[13];
                        a.parts[0]=age==0?0:9;a.parts[2]=age==0?0:7;a.parts[3]=age==0?0:7;
                        a.parts[4]=age==0?0:7;a.parts[5]=age==0?0:7;a.parts[10]=skin;a.parts[12]=age==0?0:3;
                        AppearanceRules.Validate(a);
                        string path=prefix+"-s"+sex+"-age"+age+"-tone"+skin+".png";
                        Save(preview.Render(a,rows,800,1200),path);files.Add(path);
                    }
                    // Explicit moved/scaled/rotated/spaced features must still sample
                    // the face at their displayed location, not at the source crop.
                    var changed=new List<PortraitCalibrationRow>(rows);
                    changed.Add(new PortraitCalibrationRow{sex=1,head=3,part=PortraitPart.Eyes,variant=3,dx=6,dy=-3,scaleX=1.12f,scaleY=.94f,spacing=7,rotation=5});
                    var varied=AppearanceRules.Randomize(430,1,0);varied.sex=1;varied.parts=new int[13];varied.parts[0]=3;varied.parts[2]=3;varied.parts[10]=2;
                    Save(preview.Render(varied,changed,800,1200),prefix+"-transformed.png");
                }
                if(File.ReadAllText(PortraitCalibrationWindow.ProjectCsv)!=table)throw new Exception("Skin proof changed the user's calibration table");
                Debug.Log("MINGLU_PORTRAIT_SKIN_PREVIEW_SUCCESS: 16 sex/age/palette actual previews plus transformed feature; user calibration preserved. Visual comparison required.");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Save(RenderTexture source,string path)
        {
            var previous=RenderTexture.active;var texture=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=source;texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0);texture.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
