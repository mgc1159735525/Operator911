using System;
using UnityEngine;
namespace Minglu
{
    [Serializable] public class Appearance
    {
        public bool enabled;
        public int style, sex, ears, portraitVersion;
        public bool featureEditing, groomingEditing;
        public int[] parts=new int[13];
    }
    public static class AppearanceRules
    {
        public static readonly string[] Labels={"脸型","发式","眼睛","眉形","鼻形","嘴形","胡须","服装","饰物","背景","肤色","衣色","须发色"};
        public static readonly string[] MaleHairNames={"束髻","短束","披发","巾帽","低髻","文士髻","半束","束发"};
        public static readonly string[] FemaleHairNames={"高髻","低髻","侧髻","半束","双环髻","编髻","垂鬟","素簪髻"};
        public static readonly string[] BeardNames={"无","短须","长须","八字须","络腮短须","山羊须","三绺须","短髭"};
        public static readonly string[][] Names={
            new[]{"清秀","方正","圆润","修长","书卷","敦厚","历练","清癯"},MaleHairNames,AppearancePartCatalog.Eyes,AppearancePartCatalog.Brows,AppearancePartCatalog.Noses,AppearancePartCatalog.Mouths,BeardNames,new[]{"交领袍","立领衫","披肩","行装","马甲","围巾衫","商服","挎包装"},new[]{"无","圆镜","耳坠","刀疤"},new[]{"墨竹","烟蓝","赭石","纸白"},new[]{"瓷白","暖玉","小麦","深棕"},new[]{"青黛","松绿","朱砂","沙金","墨灰","绛紫"},new[]{"乌黑","栗棕","霜灰","银白"}};
        public static Appearance Copy(Appearance a){return a==null?new Appearance():JsonUtility.FromJson<Appearance>(JsonUtility.ToJson(a));}
        public static Appearance AsRealistic(Appearance a)
        {
            var result=Copy(a);
            if(result.style==0&&result.enabled){result.featureEditing=true;result.groomingEditing=true;result.ears=result.parts[0]%4;}
            if(result.portraitVersion<15){for(int i=2;i<=5;i++)result.parts[i]=0;result.ears=0;result.portraitVersion=15;}
            result.featureEditing=true;result.groomingEditing=true;result.style=1;return result;
        }
        public static void Validate(Appearance a)
        {
            if(a==null)return;
            if(a.ears<0||a.ears>=AppearancePartCatalog.Ears.Length)throw new ArgumentException("耳部索引无效。");
            if(a.style<0||a.style>1||a.sex<0||a.sex>1)throw new ArgumentException("外观风格或性别无效。");
            if(a.parts==null||a.parts.Length!=Names.Length)throw new ArgumentException("外观部件数量无效。");
            for(int i=0;i<a.parts.Length;i++)if(a.parts[i]<0||a.parts[i]>=Names[i].Length)throw new ArgumentException("外观部件索引无效："+Labels[i]);
        }
        public static bool IsElderFace(int face){return face==3||face==7;}
        public static void SelectFace(Appearance a,int face,bool keepHairColor=false)
        {
            a.parts[0]=face;
            // An explicit face selection may suggest a color; loading or drawing
            // an existing portrait never changes the user's saved palette.
            if(IsElderFace(face)&&!keepHairColor)a.parts[12]=3;
        }
        public static void ApplyAgeHairColor(Appearance a){a.parts[12]=IsElderFace(a.parts[0])?3:0;}
        public static Appearance Randomize(int seed,int sex=-1,int face=-1)
        {
            if(face < -1||face>=Names[0].Length)throw new ArgumentOutOfRangeException(nameof(face));
            var random=new System.Random(seed);var a=new Appearance{enabled=true,style=1,portraitVersion=15};
            for(int i=0;i<a.parts.Length;i++)a.parts[i]=random.Next(Names[i].Length);
            if(face>=0)a.parts[0]=face;
            a.featureEditing=true;a.ears=random.Next(AppearancePartCatalog.Ears.Length);
            a.parts[12]=IsElderFace(a.parts[0])?(random.Next(100)<85?3:2):random.Next(2);
            a.sex=sex<0?random.Next(2):sex;a.groomingEditing=true;if(a.sex==1)a.parts[6]=0;Validate(a);return a;
        }
        public static Appearance ForRecruit(uint seed,int id){return Randomize(unchecked((int)(seed*747796405u+(uint)id*2891336453u+277803737u)),id%2);}
    }
}

