using System;
using UnityEngine;
namespace Minglu
{
    [Serializable] public class Appearance
    {
        public bool enabled;
        // ears and parts[1] remain in the save bridge only. Version 20 heads
        // include their own hair, face outline, ears and connected neck.
        public int style, sex, ears, portraitVersion;
        public bool featureEditing, groomingEditing;
        public int[] parts=new int[13];
    }
    public static class AppearanceRules
    {
        public const int CurrentPortraitVersion=20,HeadCount=12;
        public static readonly string[] Labels={"头部","随头部","眼睛","眉形","鼻形","嘴形","胡须","服装","饰物","背景","肤色","衣色","须发色"};
        public static readonly string[] MaleHeadNames={"青年束发","青年披发","青年巾帽","壮年短束","壮年发髻","壮年半束","中年儒士","中年行旅","中年高髻","长者束发","长者披发","长者巾帽"};
        public static readonly string[] FemaleHeadNames={"青年素髻","青年披发","青年侧髻","壮年编髻","壮年盘髻","壮年半束","成熟低髻","中年半束","中年高髻","长者束发","长者披发","长者素簪"};
        // Kept for source compatibility, not exposed as an independent part.
        public static readonly string[] MaleHairNames={"随头部"};
        public static readonly string[] FemaleHairNames={"随头部"};
        public static readonly string[] BeardNames={"无","短须","长须","八字须","络腮短须","山羊须","三绺须","短髭"};
        public static readonly string[][] Names={MaleHeadNames,MaleHairNames,AppearancePartCatalog.Eyes,AppearancePartCatalog.Brows,AppearancePartCatalog.Noses,AppearancePartCatalog.Mouths,BeardNames,
            new[]{"交领袍","儒士衫","披风","布衣马甲","锦纹长袍","围巾衫","工坊便服","挎包装"},new[]{"无","圆镜","耳坠","刀疤"},new[]{"墨竹","烟蓝","赭石","纸白"},new[]{"瓷白","暖玉","小麦","深棕"},new[]{"青黛","松绿","朱砂","沙金","墨灰","绛紫"},new[]{"乌黑","栗棕","霜灰","银白"}};
        static readonly int[] LegacyCounts={8,8,8,8,8,8,8,8,4,4,4,6,4};
        // Rows are age bands; columns are the old eight hairstyle IDs.
        // Select the closest complete head without retaining old hair sprites.
        static readonly int[,] MaleLegacyVariant={
            {0,0,1,2,0,0,1,0},{1,0,2,0,1,1,2,0},{2,0,1,0,2,0,1,0},{0,0,1,2,0,0,1,0}};
        static readonly int[,] FemaleLegacyVariant={
            {0,0,2,1,0,0,1,0},{1,1,1,2,1,0,2,1},{2,0,2,1,2,2,1,0},{0,0,2,1,0,0,1,2}};
        public static string[] HeadNames(int sex)
        {
            if(sex<0||sex>1)throw new ArgumentOutOfRangeException(nameof(sex));
            return sex==0?MaleHeadNames:FemaleHeadNames;
        }
        public static Appearance Copy(Appearance a){return a==null?new Appearance():JsonUtility.FromJson<Appearance>(JsonUtility.ToJson(a));}
        public static int LegacyHead(int sex,int face,int hair)
        {
            if(sex<0||sex>1||face<0||face>=8||hair<0||hair>=8)throw new ArgumentOutOfRangeException("legacyHead");
            int age=face%4;return age*3+(sex==0?MaleLegacyVariant[age,hair]:FemaleLegacyVariant[age,hair]);
        }
        public static Appearance AsRealistic(Appearance a)
        {
            Validate(a);var result=Copy(a);
            if(result.portraitVersion<CurrentPortraitVersion)
            {
                result.parts[0]=LegacyHead(result.sex,result.parts[0],result.parts[1]);
                result.parts[1]=0;result.ears=0;
                // The new IDs select authored sprites. Preserve valid old
                // feature choices and manual palettes; reading an elder must
                // not silently recolour an existing saved appearance.
                if(result.sex==1)result.parts[6]=0;
                result.portraitVersion=CurrentPortraitVersion;
            }
            result.featureEditing=true;result.groomingEditing=true;result.style=1;
            Validate(result);return result;
        }
        public static void Validate(Appearance a)
        {
            if(a==null)return;
            if(a.portraitVersion<0||a.portraitVersion>CurrentPortraitVersion)throw new ArgumentException("外观数据版本无效。");
            if(a.style<0||a.style>1||a.sex<0||a.sex>1)throw new ArgumentException("外观风格或性别无效。");
            if(a.parts==null||a.parts.Length!=Names.Length)throw new ArgumentException("外观部件数量无效。");
            bool legacy=a.portraitVersion<CurrentPortraitVersion;
            if(a.ears<0||a.ears>=(legacy?8:1))throw new ArgumentException("耳部索引无效。");
            for(int i=0;i<a.parts.Length;i++)
                if(a.parts[i]<0||a.parts[i]>=(legacy?LegacyCounts[i]:Names[i].Length))throw new ArgumentException("外观部件索引无效："+Labels[i]);
            if(!legacy&&a.sex==1&&a.parts[6]!=0)throw new ArgumentException("女性头部不能使用胡须部件。");
        }
        public static int AgeBand(int face)
        {
            if(face<0||face>=HeadCount)throw new ArgumentOutOfRangeException(nameof(face));
            return face/3;
        }
        public static bool IsElderFace(int face){return face>=9&&face<HeadCount;}
        public static void SelectFace(Appearance a,int face,bool keepHairColor=false)
        {
            if(a==null)throw new ArgumentNullException(nameof(a));
            AgeBand(face);Validate(a);
            if(a.portraitVersion!=CurrentPortraitVersion)throw new ArgumentException("请先将外观升级为当前头部目录。");
            a.parts[0]=face;
            if(IsElderFace(face)&&!keepHairColor)a.parts[12]=3;
        }
        public static void ApplyAgeHairColor(Appearance a)
        {
            if(a==null)throw new ArgumentNullException(nameof(a));
            Validate(a);
            if(a.portraitVersion!=CurrentPortraitVersion)throw new ArgumentException("请先将外观升级为当前头部目录。");
            a.parts[12]=IsElderFace(a.parts[0])?3:0;
        }
        public static Appearance Randomize(int seed,int sex=-1,int face=-1)
        {
            if(sex < -1||sex>1)throw new ArgumentOutOfRangeException(nameof(sex));
            if(face < -1||face>=HeadCount)throw new ArgumentOutOfRangeException(nameof(face));
            var random=new System.Random(seed);
            var a=new Appearance{enabled=true,style=1,portraitVersion=CurrentPortraitVersion,featureEditing=true,groomingEditing=true};
            for(int i=0;i<a.parts.Length;i++)a.parts[i]=random.Next(Names[i].Length);
            if(face>=0)a.parts[0]=face;
            a.sex=sex<0?random.Next(2):sex;a.parts[1]=0;a.ears=0;
            a.parts[12]=IsElderFace(a.parts[0])?(random.Next(100)<85?3:2):random.Next(2);
            if(a.sex==1)a.parts[6]=0;Validate(a);return a;
        }
        public static Appearance ForRecruit(uint seed,int id){return Randomize(unchecked((int)(seed*747796405u+(uint)id*2891336453u+277803737u)),id%2);}
        public static Appearance EffectiveMemberLook(Agent member,uint seed,int knownSex=-1)
        {
            if(member==null)throw new ArgumentNullException(nameof(member));
            Validate(member.appearance);
            if(member.appearance!=null&&member.appearance.enabled)return AsRealistic(member.appearance);
            // Old null entries may have been materialized by Unity into an
            // untouched version-0 object. Its default zero is not evidence
            // that the member was male; preserve a real historical sex hint.
            int sex=!RunLifecycle.IsEmptyLookSlot(member.appearance)?member.appearance.sex:knownSex;
            return Randomize(unchecked((int)(seed*747796405u+(uint)member.id*2891336453u+277803737u)),sex);
        }
    }
}
