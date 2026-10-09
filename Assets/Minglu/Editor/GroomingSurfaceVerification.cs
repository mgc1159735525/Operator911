using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minglu.EditorTools
{
    // Geometry and source-pixel regressions, not a claim of visual approval.
    // The separate GroomingFitSmoke captures must still be inspected at both
    // portrait sizes: correct anchors alone cannot establish a natural face.
    public static class GroomingSurfaceVerification
    {
        static int checks;
        static void Require(bool condition,string message)
        {
            checks++;
            if(!condition)throw new Exception("Grooming surface: "+message);
        }
        static float Luma(Color c){return c.r*.299f+c.g*.587f+c.b*.114f;}
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        static Appearance Look(int sex,int face=0)
        {
            var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});
            for(int category=2;category<=5;category++)look.parts[category]=0;
            look.ears=0;look.parts[0]=face;return look;
        }
        static Texture2D ReadSource(string name)
        {
            string path=Path.Combine(Application.dataPath,"Minglu/Resources/Minglu/Art/Painted/"+name+".png");
            Require(File.Exists(path),"missing actual source PNG: "+path);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Require(texture.LoadImage(File.ReadAllBytes(path)),"cannot decode source PNG: "+path);
                return texture;
            }
            catch{UnityEngine.Object.DestroyImmediate(texture);throw;}
        }
        static Color Sample(Texture2D texture,Rect cell,Vector2 source)
        {
            if(source.x<0||source.x>1||source.y<0||source.y>1)return Color.clear;
            // Same top-left portrait convention and inset as the shader. Test
            // original PNGs, not importer-resized/cached asset copies.
            float u=Mathf.Clamp(cell.x+source.x*cell.width,cell.x+.75f/texture.width,cell.xMax-.75f/texture.width);
            float v=Mathf.Clamp(cell.y+(1-source.y)*cell.height,cell.y+.75f/texture.height,cell.yMax-.75f/texture.height);
            return texture.GetPixelBilinear(u,v);
        }
        static float Quantile(List<float> sorted,float percentile)
        {return sorted[Mathf.RoundToInt((sorted.Count-1)*percentile)];}

        static void VerifyPigment(Texture2D texture,Rect cell,string label,ref float minBlackContrast,ref float minWhiteContrast,Rect canvas=default(Rect),bool clothCap=false,bool beardPart=false,bool fullBeard=false)
        {
            var source=new List<float>();
            int width=Mathf.RoundToInt(cell.width*texture.width),height=Mathf.RoundToInt(cell.height*texture.height);
            for(int y=2;y<height-2;y+=3)for(int x=2;x<width-2;x+=3)
            {
                var local=new Vector2((x+.5f)/width,(y+.5f)/height);
                if(clothCap)
                {
                    // The male cloth cap is deliberately not recoloured. Only
                    // test the exposed hair below its preserved cloth region.
                    var original=new Vector2(canvas.x+local.x*canvas.width,canvas.y+local.y*canvas.height);
                    float edge=.552f+Mathf.Max(0,original.x-.383f)*.32f;
                    if(original.y<edge+.012f||(original.x>.72f&&original.y>.59f))continue;
                }
                var pixel=Sample(texture,cell,local);
                // Transparent matte RGB and gold/red accessories are not hair
                // pigment. The latter are preserved separately by the shader.
                if(pixel.a>.95f&&pixel.r-pixel.b<.10f)source.Add(Luma(pixel));
            }
            Require(source.Count>150,label+": insufficient opaque neutral source pixels");
            source.Sort();float low=Quantile(source,.1f),middle=Quantile(source,.5f),high=Quantile(source,.9f);
            float blackLow=Luma(beardPart?PaintedPortraitLayout.BeardPigment(0,low,fullBeard):PaintedPortraitLayout.HairPigment(0,low));
            float blackMid=Luma(beardPart?PaintedPortraitLayout.BeardPigment(0,middle,fullBeard):PaintedPortraitLayout.HairPigment(0,middle));
            float blackHigh=Luma(beardPart?PaintedPortraitLayout.BeardPigment(0,high,fullBeard):PaintedPortraitLayout.HairPigment(0,high));
            float whiteLow=Luma(beardPart?PaintedPortraitLayout.BeardPigment(3,low,fullBeard):PaintedPortraitLayout.HairPigment(3,low));
            float whiteMid=Luma(beardPart?PaintedPortraitLayout.BeardPigment(3,middle,fullBeard):PaintedPortraitLayout.HairPigment(3,middle));
            float whiteHigh=Luma(beardPart?PaintedPortraitLayout.BeardPigment(3,high,fullBeard):PaintedPortraitLayout.HairPigment(3,high));
            float originalContrast=high-low,blackContrast=blackHigh-blackLow,whiteContrast=whiteHigh-whiteLow;
            // Relative retention prevents a flat source sheet from passing a
            // palette-only test, or a high white floor hiding painted roots.
            Require(originalContrast>.12f,label+": source has lost strand light/shadow variation");
            Require(blackContrast>.06f&&blackContrast/originalContrast>.30f,label+": black pigment collapses actual source texture");
            Require(whiteContrast>.15f&&whiteContrast/originalContrast>.65f,label+": white pigment flattens actual source into a cap");
            Require(blackLow<blackMid&&blackMid<blackHigh&&blackHigh<.40f,label+": black hair lost its dark/medium/light ordering");
            // Hair shaded by a cloth cap has a legitimately darker source P90
            // than an exposed crown. Enforce contrast and black/white separation
            // on the same source samples, not an invented universal highlight.
            Require(whiteLow<whiteMid&&whiteMid<whiteHigh&&whiteLow<.65f,label+": white hair lost shaded roots or light ordering");
            Require(whiteLow-blackLow>=.35f&&whiteMid-blackMid>=.35f&&whiteHigh-blackHigh>=.35f,label+": black and white pigment cannot be distinguished on the same source strands");
            minBlackContrast=Mathf.Min(minBlackContrast,blackContrast);minWhiteContrast=Mathf.Min(minWhiteContrast,whiteContrast);
            Debug.Log("GROOMING_SOURCE_VALUES "+label+" samples="+source.Count+" source(p10/p50/p90)="+low.ToString("F3")+"/"+middle.ToString("F3")+"/"+high.ToString("F3")+" pigment contrast black="+blackContrast.ToString("F3")+" white="+whiteContrast.ToString("F3"));
        }

        static void VerifyBeardSurface(Texture2D beard,Appearance look,ref int visibleSamples)
        {
            var f=PaintedPortraitLayout.Landmarks(look);var jaw=PaintedPortraitLayout.BeardJaw(look);
            var scale=PaintedPortraitLayout.BeardScales(look);var surface=PaintedPortraitLayout.BeardSurface(look);
            string label="face="+look.parts[0]+" beard="+look.parts[6];
            bool contour=BeardContour.Enabled(look);
            if(!contour)
            {
            Require(jaw.x<f.chin.x&&jaw.z>f.chin.x&&jaw.y<f.chin.y&&jaw.w<jaw.y,label+": authored near/far jaw has the wrong orientation");
            Require(surface.x>.15f&&surface.y>surface.x*1.2f&&surface.y<.80f,label+": beard lost separate near/far jaw widths");
            Require(surface.z>=0&&surface.w>surface.z&&Finite(surface.z)&&Finite(surface.w),label+": beard slopes cannot follow the three-quarter jaw");
            Require(scale.x>.15f&&scale.y>scale.x&&scale.x<.65f&&scale.y<.65f,label+": lip opening has reversed/excessive perspective width");
            }

            var cell=PaintedPortraitLayout.Beard(look);var draw=PaintedPortraitLayout.DrawRect(look,PaintedPortraitLayer.Beard);
            int visible=0;float minX=1,maxX=0,minY=1,maxY=0;
            // Sample the wider portrait, including outside the allocated mesh,
            // so a passing screenshot cannot merely be clipping a runaway part.
            for(int y=0;y<=105;y++)for(int x=0;x<=110;x++)
            {
                var p=new Vector2(.05f+x*.008f,.27f+y*.006f);
                var q=PaintedPortraitLayout.BeardSamplingPoint(look,p);
                float alpha=contour?ContourAlpha(beard,look,p):Sample(beard,cell,q).a*PaintedPortraitLayout.BeardRootAlpha(look,p)*PaintedPortraitLayout.BeardLipAlpha(look,p);
                if(alpha<=.20f)continue;
                visible++;minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);
                Require(draw.Contains(p),label+": visible beard falls outside its render mesh at "+p);
                Require(p.x>f.eyeFar.x-.08f&&p.x<f.earNear.x+.02f,label+": visible beard flies outside the face silhouette at "+p);
                Require(p.y>(contour?f.earNear.y-.025f:f.mouth.y-.045f)&&p.y<f.chin.y+(contour?.07f:.27f),label+": visible beard escapes cheek/tip bounds at "+p);
            }
            Require(visible>20,label+": beard disappeared or collapsed");
            Require(minX<f.mouth.x&&maxX>f.mouth.x,label+": actual beard texture no longer spans both sides of the mouth");
            Require(maxX-minX>.065f&&maxX-minX<.48f,label+": actual texture footprint has an implausible face width");
            if(contour)
            {
                Require(maxX-minX>.30f&&maxX>f.earNear.x-.10f,label+": full beard has collapsed back into a mouth-centred sticker");
                VerifyContourAttachment(beard,look);
            }
            visibleSamples+=visible;
            Debug.Log("GROOMING_BEARD_BOUNDS "+label+" visible="+visible+" x="+minX.ToString("F3")+".."+maxX.ToString("F3")+" y="+minY.ToString("F3")+".."+maxY.ToString("F3"));
        }
        static void VerifyBeardEdits(Appearance look,ref float minJacobian,ref int samples)
        {
            if(BeardContour.Enabled(look)){VerifyContourEdits(look,ref minJacobian,ref samples);return;}
            var f=PaintedPortraitLayout.Landmarks(look);var center=new Vector2(f.mouth.x,f.mouth.y);
            for(int mouth=0;mouth<8;mouth++)for(int nose=0;nose<8;nose++)
            {
                look.parts[5]=mouth;look.parts[4]=nose;
                string label="face="+look.parts[0]+" beard="+look.parts[6]+" mouth="+mouth+" nose="+nose;
                foreach(float offset in new[]{-.16f,-.08f,0,.08f,.16f})
                {
                    Require(PaintedPortraitLayout.BeardRootAlpha(look,center+new Vector2(offset,-.055f))<.001f,label+": beard remains opaque above cheek growth line");
                    Require(PaintedPortraitLayout.BeardRootAlpha(look,center+new Vector2(offset,.024f))>.999f,label+": root feather erases lower beard");
                    Require(PaintedPortraitLayout.BeardRootAlpha(look,f.chin+new Vector2(offset,.19f))>.999f,label+": root feather clips free/long ends");
                }
                Require(PaintedPortraitLayout.BeardLipAlpha(look,center)<.001f,label+": lip opening covered");
                // Centre-line monotonicity misses folds on the cheek. Check a
                // two-dimensional field through both jaw planes and free ends,
                // with all 64 interacting mouth/nose edits on every beard/face.
                const float epsilon=.0005f;
                for(int y=0;y<=20;y++)for(int x=0;x<=18;x++)
                {
                    var p=center+new Vector2(-.20f+x*.025f,-.055f+y*.015f);
                    var q=PaintedPortraitLayout.BeardSamplingPoint(look,p);
                    var dx=(PaintedPortraitLayout.BeardSamplingPoint(look,p+new Vector2(epsilon,0))-q)/epsilon;
                    var dy=(PaintedPortraitLayout.BeardSamplingPoint(look,p+new Vector2(0,epsilon))-q)/epsilon;
                    float determinant=dx.x*dy.y-dx.y*dy.x;
                    // Do not allocate millions of coordinate/error strings on
                    // a passing build; retain the exact failing point instead.
                    checks+=2;
                    if(!Finite(q.x)||!Finite(q.y)||Mathf.Abs(q.x)>=4||Mathf.Abs(q.y)>=4)
                        Require(false,label+": unbounded beard mapping at "+p);
                    if(!Finite(determinant)||determinant<=.02f)
                        Require(false,label+": folded/collapsed cheek mapping at "+p+", Jacobian="+determinant);
                    minJacobian=Mathf.Min(minJacobian,determinant);samples++;
                }
            }
            look.parts[5]=0;look.parts[4]=0;
        }

        static Vector2 Position(Vector4 v){return new Vector2(v.x,v.y);}
        static Vector2 Source(Vector4 v){return new Vector2(v.z,v.w);}
        static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        static float Smooth(float from,float to,float value){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,value));}
        static int[] ContourTriangles()
        {
            var result=new List<int>();
            for(int row=0;row<BeardContour.Rows-1;row++)for(int col=0;col<BeardContour.Columns-1;col++)
            {
                int a=row*BeardContour.Columns+col,b=a+1,c=a+BeardContour.Columns,d=c+1;
                result.AddRange(new[]{a,b,d,a,d,c});
            }
            return result.ToArray();
        }
        // Forward projection is a test-side reference in source space. Do not
        // infer attachment from an empty control vertex or the old lip scales.
        static Vector2 ProjectContourSource(Appearance look,Vector2 source)
        {
            var vertices=BeardContour.Vertices(look);var triangles=ContourTriangles();
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                var ab=Source(b)-Source(a);var ac=Source(c)-Source(a);var ap=source-Source(a);
                float determinant=Cross(ab,ac),u=Cross(ap,ac)/determinant,v=Cross(ab,ap)/determinant;
                if(u>=-.00001f&&v>=-.00001f&&u+v<=1.00001f)
                    return Position(a)+u*(Position(b)-Position(a))+v*(Position(c)-Position(a));
            }
            throw new Exception("Grooming surface: actual full-beard source point is outside the contour mesh: "+source);
        }
        static float ContourAlpha(Texture2D texture,Appearance look,Vector2 p)
        {
            var q=PaintedPortraitLayout.BeardSamplingPoint(look,p);var cell=PaintedPortraitLayout.Beard(look);
            if(q.x<0||q.x>1||q.y<0||q.y>1)return 0;
            const float dx=.020f,dy=.020f;
            float coverage=(Sample(texture,cell,q+new Vector2(dx,0)).a+Sample(texture,cell,q-new Vector2(dx,0)).a
                +Sample(texture,cell,q+new Vector2(0,dy)).a+Sample(texture,cell,q-new Vector2(0,dy)).a)*.25f;
            var pixel=Sample(texture,cell,q);float tone=Mathf.Clamp01((Luma(pixel)-.05f)*2.6f);
            return pixel.a*PaintedPortraitLayout.BeardRootAlpha(look,p)*PaintedPortraitLayout.BeardLipAlpha(look,p)
                *Mathf.Lerp(.18f,1,Smooth(.10f,.97f,coverage))*Mathf.Lerp(.68f,1,Smooth(.12f,.58f,tone));
        }
        static void VerifyContourAttachment(Texture2D texture,Appearance look)
        {
            var f=PaintedPortraitLayout.Landmarks(look);var cell=PaintedPortraitLayout.Beard(look);
            var ear=new Vector2(f.earNear.x,f.earNear.y);var mouth=new Vector2(f.mouth.x,f.mouth.y);
            string label="full-beard face="+look.parts[0];
            Require(cell==new Rect(0,0,1,1),label+": standalone contour source is being sampled as an atlas cell");
            // Measured alpha-bearing points in beard-full-contour.png. Its
            // canvas top and internal chin registration are not hair roots or
            // the free end. These would reject the old narrow atlas mapping.
            var nearSource=new Vector2(.830f,.300f);var farSource=new Vector2(.140f,.505f);var tipSource=new Vector2(.373f,.848f);
            var near=ProjectContourSource(look,nearSource);var far=ProjectContourSource(look,farSource);var tip=ProjectContourSource(look,tipSource);
            foreach(var q in new[]{nearSource,farSource,tipSource,new Vector2(.80f,.45f),new Vector2(.72f,.58f),new Vector2(.64f,.66f)})
            {
                Require(Sample(texture,cell,q).a>.20f,label+": measured attachment/cheek sample contains no actual source hair at "+q);
                var p=ProjectContourSource(look,q);
                Require(Vector2.Distance(PaintedPortraitLayout.BeardSamplingPoint(look,p),q)<.0001f,label+": source hair attachment is not registered at "+q);
                Require(ContourAlpha(texture,look,p)>.10f,label+": source hair is erased by the final render masks at "+q);
            }
            Require(near.x<ear.x&&near.x>f.eyeNear.x+.10f&&Vector2.Distance(near,ear)<.07f,label+": actual near root does not attach in front of the ear: "+near+" ear="+ear);
            Require(far.x<mouth.x&&far.x>f.eyeFar.x-.045f&&far.y<mouth.y+.01f,label+": far root floats below or outside the far cheek: "+far);
            Require(tip.y>=f.chin.y-.01f&&Vector2.Distance(tip,f.chin)<.07f,label+": short-beard free end is detached from the chin: "+tip);
            var jaw=PaintedPortraitLayout.BeardJaw(look);
            foreach(var q in new[]{new Vector2(.227f,.731f),new Vector2(.665f,.726f)})
            {
                Require(Sample(texture,cell,q).a>.20f,label+": source jaw-edge witness is empty at "+q);
                var p=ProjectContourSource(look,q);bool isFar=p.x<f.chin.x;
                var end=isFar?new Vector2(jaw.x,jaw.y):new Vector2(jaw.z,jaw.w);
                float t=Mathf.Clamp01((p.x-f.chin.x)/(end.x-f.chin.x));float jawY=Mathf.Lerp(f.chin.y,end.y,t);
                Require(Mathf.Abs(p.y-jawY)<.055f,label+": actual short-beard edge misses the mandibular contour at "+p);
            }
            foreach(var q in new[]{new Vector2(.280f,.602f),new Vector2(.350f,.602f),new Vector2(.430f,.610f)})
            {
                Require(Sample(texture,cell,q).a<.05f,label+": lip-hole witness is not transparent in the actual source");
                Require(ContourAlpha(texture,look,ProjectContourSource(look,q))<.05f,label+": lip hole is filled by the mapped source");
            }
            Require(Vector2.Distance(ProjectContourSource(look,new Vector2(.350f,.602f)),mouth)<.003f,label+": source lip opening is detached from the painted mouth");
            Require(ContourAlpha(texture,look,new Vector2(.02f,.02f))==0,label+": outside-mesh sampling leaks a clamped texture edge");
            Debug.Log("GROOMING_CONTOUR_ATTACHMENT "+label+" ear-root="+Vector2.Distance(near,ear).ToString("F4")+" chin-tip="+Vector2.Distance(tip,f.chin).ToString("F4")+" actual source lip/root/jaw/tip witnesses preserved");
        }
        // Invert only the shared feature deformation for interior probes. The
        // inverse beard map is still the code under test. This keeps a sample
        // inside its triangle even when an extreme mouth edit moves its edges.
        static Vector2 ScreenForFeature(Appearance look,Vector2 target)
        {
            Vector2 p=target;const float epsilon=.0001f;
            for(int attempt=0;attempt<12;attempt++)
            {
                var q=PaintedPortraitLayout.FeatureSamplingPoint(look,p);var error=q-target;
                if(error.sqrMagnitude<.000000000025f)return p;
                var dx=(PaintedPortraitLayout.FeatureSamplingPoint(look,p+new Vector2(epsilon,0))-q)/epsilon;
                var dy=(PaintedPortraitLayout.FeatureSamplingPoint(look,p+new Vector2(0,epsilon))-q)/epsilon;
                float determinant=Cross(dx,dy);
                Require(Finite(determinant)&&determinant>.02f,"feature map folds while locating a contour interior probe");
                p-=new Vector2(Cross(error,dy),Cross(dx,error))/determinant;
            }
            Require(Vector2.Distance(PaintedPortraitLayout.FeatureSamplingPoint(look,p),target)<.00002f,"could not place the contour probe inside its edited triangle");
            return p;
        }
        static void VerifyContourEdits(Appearance look,ref float minJacobian,ref int samples)
        {
            var vertices=BeardContour.Vertices(look);var triangles=ContourTriangles();var f=PaintedPortraitLayout.Landmarks(look);
            for(int i=0;i<vertices.Length;i++)Require(vertices[i]==BeardContour.Vertex(look,i),"cached contour sent to the GPU differs from the current face binding");
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                float sourceArea=Cross(Source(b)-Source(a),Source(c)-Source(a));float targetArea=Cross(Position(b)-Position(a),Position(c)-Position(a));
                Require(Finite(sourceArea)&&Finite(targetArea)&&sourceArea*targetArea>0&&Mathf.Abs(targetArea)>.00001f,"full-beard face="+look.parts[0]+" triangle="+(i/3)+" is flipped or collapsed");
                foreach(var edge in new[]{new[]{a,b},new[]{b,c},new[]{c,a}})
                    Require(Vector2.Distance(BeardContour.Sample(look,(Position(edge[0])+Position(edge[1]))*.5f),(Source(edge[0])+Source(edge[1]))*.5f)<.0001f,"contour shared edge samples discontinuously");
            }
            for(int mouth=0;mouth<8;mouth++)for(int nose=0;nose<8;nose++)
            {
                look.parts[5]=mouth;look.parts[4]=nose;
                string label="full-beard face="+look.parts[0]+" mouth="+mouth+" nose="+nose;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                    foreach(var weights in new[]{new Vector3(.5f,.25f,.25f),new Vector3(.25f,.5f,.25f),new Vector3(.25f,.25f,.5f)})
                    {
                        var target=Position(a)*weights.x+Position(b)*weights.y+Position(c)*weights.z;
                        var expected=Source(a)*weights.x+Source(b)*weights.y+Source(c)*weights.z;
                        var p=ScreenForFeature(look,target);var q=PaintedPortraitLayout.BeardSamplingPoint(look,p);
                        Require(Vector2.Distance(q,expected)<.0003f,label+": source triangle no longer follows the same feature edits as the face");
                        const float epsilon=.00005f;
                        var dx=(PaintedPortraitLayout.BeardSamplingPoint(look,p+new Vector2(epsilon,0))-q)/epsilon;
                        var dy=(PaintedPortraitLayout.BeardSamplingPoint(look,p+new Vector2(0,epsilon))-q)/epsilon;float determinant=Cross(dx,dy);
                        Require(Finite(determinant)&&determinant>.02f,label+": folded contour interior, triangle="+(i/3)+" Jacobian="+determinant);
                        minJacobian=Mathf.Min(minJacobian,determinant);samples++;
                    }
                }
                foreach(var target in new[]{new Vector2(f.mouth.x,f.mouth.y),new Vector2(f.mouth.x-.025f,f.mouth.y),new Vector2(f.mouth.x+.025f,f.mouth.y)})
                    Require(PaintedPortraitLayout.BeardLipAlpha(look,ScreenForFeature(look,target))<.001f,label+": lip opening fails to follow the edited face");
                var root=ScreenForFeature(look,ProjectContourSource(look,new Vector2(.830f,.300f)));
                Require(PaintedPortraitLayout.BeardRootAlpha(look,root)>.999f,label+": mouth-height mask cuts off the real temple root");
            }
            look.parts[5]=0;look.parts[4]=0;
        }
        public static void Verify()
        {
            checks=0;
            Require(QualitySettings.activeColorSpace==ColorSpace.Gamma,"source-value tests assume the shipped Gamma renderer; Linear needs a colour-space-aware reference");
            Texture2D hair=null,beard=null,fullBeard=null;float minBlack=1,minWhite=1,minJacobian=float.MaxValue;int samples=0,visibleSamples=0;
            try
            {
                hair=ReadSource("hair-styles");beard=ReadSource("beard-styles");fullBeard=ReadSource("beard-full-contour");
                for(int sex=0;sex<2;sex++)for(int style=0;style<PaintedPortraitLayout.HairCount;style++)
                {
                    var look=Look(sex);look.parts[1]=style;
                    VerifyPigment(hair,PaintedPortraitLayout.Hair(look),"hair "+sex+"/"+style,ref minBlack,ref minWhite,PaintedPortraitLayout.HairCanvas(look),sex==0&&style==3);
                }
                var openings=new HashSet<string>();
                for(int style=1;style<PaintedPortraitLayout.BeardCount;style++)
                {
                    var look=Look(0);look.parts[6]=style;var scale=PaintedPortraitLayout.BeardScales(look);
                    openings.Add(scale.x.ToString("F4")+"/"+scale.y.ToString("F4"));
                    VerifyPigment(BeardContour.Enabled(look)?fullBeard:beard,PaintedPortraitLayout.Beard(look),"beard "+style,ref minBlack,ref minWhite,beardPart:true,fullBeard:BeardContour.Enabled(look));
                }
                Require(openings.Count>=5,"different beard source lip openings have collapsed to a shared scale");
                for(int face=0;face<8;face++)for(int style=1;style<PaintedPortraitLayout.BeardCount;style++)
                {
                    var look=Look(0,face);look.parts[6]=style;string original=JsonUtility.ToJson(look);
                    VerifyBeardSurface(BeardContour.Enabled(look)?fullBeard:beard,look,ref visibleSamples);
                    Require(JsonUtility.ToJson(look)==original,"source and surface queries mutated appearance");
                    VerifyBeardEdits(look,ref minJacobian,ref samples);
                    Require(JsonUtility.ToJson(look)==original,"edit verification failed to restore its own local fixture");
                }
            }
            finally
            {
                if(hair!=null)UnityEngine.Object.DestroyImmediate(hair);
                if(beard!=null)UnityEngine.Object.DestroyImmediate(beard);
                if(fullBeard!=null)UnityEngine.Object.DestroyImmediate(fullBeard);
            }
            Debug.Log("MINGLU_GROOMING_SURFACE_VERIFY_SUCCESS: "+checks+" checks; 23 actual PNG parts, black/white minimum contrast "+minBlack.ToString("F3")+"/"+minWhite.ToString("F3")+"; 56 face/beard silhouettes, "+visibleSamples+" visible source samples; "+samples+" 2D samples across 3,584 face/beard/mouth/nose combinations, min Jacobian "+minJacobian.ToString("F3")+". Pixel/geometry regression only; render review still required.");
        }
    }
}
