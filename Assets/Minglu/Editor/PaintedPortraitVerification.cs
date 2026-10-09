using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class PaintedPortraitVerification
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Painted portrait: "+message);}
        static void CheckCell(Rect cell)
        {Check(cell.width>0&&cell.height>0&&cell.xMin>=0&&cell.yMin>=0&&cell.xMax<=1&&cell.yMax<=1,"atlas cell outside texture");}
        static void CheckAtlas(string name,int columns,int rows,float aspect=2f/3)
        {
            var texture=Resources.Load<Texture2D>("Minglu/Art/Painted/"+name);
            Check(texture!=null,"missing "+name);
            Check(Mathf.Abs((float)texture.width/columns/((float)texture.height/rows)-aspect)<.012f,"unexpected source aspect in "+name);
            var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            Check(importer!=null&&!importer.mipmapEnabled&&importer.npotScale==TextureImporterNPOTScale.None,"atlas resampling enabled: "+name);
        }
        static Texture2D ReadAtlas(string name)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            Check(texture.LoadImage(File.ReadAllBytes("Assets/Minglu/Resources/Minglu/Art/Painted/"+name+".png")),"cannot inspect "+name);
            return texture;
        }
        static float Alpha(Texture2D texture,Rect cell,Vector2 point)
        {
            if(point.x<0||point.x>1||point.y<0||point.y>1)return 0;
            return texture.GetPixelBilinear(cell.x+point.x*cell.width,cell.y+(1-point.y)*cell.height).a;
        }
        static float VisibleHairAlpha(Texture2D hair,Texture2D body,Appearance look,Vector2 point)
        {
            var sample=PaintedPortraitLayout.HairSamplingPoint(look,point);
            float source=Alpha(hair,PaintedPortraitLayout.Hair(look),sample);
            // Match the final surface, not just the texture registration. The
            // old scalp tests could pass while the shader left a brow patch.
            float outline=Alpha(body,PaintedPortraitLayout.Base(look),point+PaintedPortraitLayout.HairOutlineOffset(look,point.y));
            return source*PaintedPortraitLayout.HairEarAlpha(look,point)*PaintedPortraitLayout.HairFarSideAlpha(look,point,outline,PaintedPortraitLayout.HairHeadwearAlpha(look,sample))*HairRootFeather(hair,body,look,point);
        }
        static float HairRootFeather(Texture2D hair,Texture2D body,Appearance look,Vector2 p)
        {
            var cell=PaintedPortraitLayout.Hair(look);var q=PaintedPortraitLayout.HairSamplingPoint(look,p);
            var dx=new Vector2(2.4f/hair.width/cell.width,0);var dy=new Vector2(0,2.4f/hair.height/cell.height);
            float coverage=(Alpha(hair,cell,q+dx)+Alpha(hair,cell,q-dx)+Alpha(hair,cell,q+dy)+Alpha(hair,cell,q-dy))*.25f;
            float line=PaintedPortraitLayout.HairTarget(look,1).y;
            float skin=Alpha(body,PaintedPortraitLayout.Base(look),p)*Smooth(line-.025f,line+.025f,p.y)*(1-PaintedPortraitLayout.HairHeadwearAlpha(look,q));
            return Mathf.Lerp(1,Mathf.Lerp(.35f,1,Smooth(.1f,.98f,coverage)),skin);
        }
        static float Smooth(float from,float to,float value)
        {float t=Mathf.Clamp01((value-from)/(to-from));return t*t*(3-2*t);}
        static float LegacyFarHairAlpha(Texture2D body,Appearance look,Vector2 point)
        {
            // Negative control: the released far-lock mask waited until the
            // brow and depended on overlap with the body. Use actual PNG alpha
            // so empty test regions cannot claim to reproduce that defect.
            var f=PaintedPortraitLayout.Landmarks(look);var cell=PaintedPortraitLayout.Base(look);
            float silhouette=Mathf.Max(Alpha(body,cell,point),Alpha(body,cell,point+new Vector2(.09f,0)));
            silhouette=Mathf.Max(silhouette,Smooth(f.mouth.y-.020f,f.chin.y-.005f,point.y));
            float boundary=Mathf.Lerp(f.eyeFar.x+.015f,f.chin.x+.100f,Smooth(f.mouth.y-.050f,f.chin.y+.020f,point.y));
            return 1-silhouette*(1-Smooth(boundary,boundary+.045f,point.x))*Smooth(f.browFar.y-.045f,f.browFar.y+.005f,point.y);
        }
        static int SourceSignature(Texture2D texture,Rect cell,bool empty,string label)
        {
            int visible=0,hash=17;
            for(int y=0;y<64;y++)for(int x=0;x<48;x++)
            {
                var p=texture.GetPixelBilinear(cell.x+(x+.5f)/48*cell.width,cell.y+(y+.5f)/64*cell.height);
                if(p.a>.15f)visible++;
                unchecked{hash=hash*31+Mathf.RoundToInt(p.a*255);}
            }
            Check(empty?visible==0:visible>30,"grooming source is unexpectedly blank or populated: "+label);
            // Check the selected source rectangle itself, including authored
            // per-style crops. Transparent gutters prevent the renderer from
            // sampling a neighbouring hairstyle or cutting off a beard tip.
            float dx=.6f/texture.width/cell.width,dy=.6f/texture.height/cell.height;
            for(int i=0;i<=128;i++)
            {
                float t=(float)i/128;
                Check(Alpha(texture,cell,new Vector2(dx,t))<.15f&&Alpha(texture,cell,new Vector2(1-dx,t))<.15f&&
                      Alpha(texture,cell,new Vector2(t,dy))<.15f&&Alpha(texture,cell,new Vector2(t,1-dy))<.15f,
                      "grooming silhouette touches its source crop or includes a neighbouring fragment: "+label);
            }
            return hash;
        }
        static void VerifyGroomingSources()
        {
            var hair=ReadAtlas("hair-styles");var beards=ReadAtlas("beard-styles");var fullBeard=ReadAtlas("beard-full-contour");
            try
            {
                var hairSignatures=new HashSet<int>();var beardSignatures=new HashSet<int>();
                for(int sex=0;sex<2;sex++)for(int style=0;style<PaintedPortraitLayout.HairCount;style++)
                {
                    var look=AppearanceRules.Randomize(17,sex);look.parts[1]=style;
                    Check(hairSignatures.Add(SourceSignature(hair,PaintedPortraitLayout.Hair(look),false,"hair "+sex+"/"+style)),"duplicate hairstyle source");
                }
                for(int style=0;style<PaintedPortraitLayout.BeardCount;style++)
                {
                    var look=AppearanceRules.Randomize(17,0);look.parts[6]=style;
                    Check(beardSignatures.Add(SourceSignature(BeardContour.Enabled(look)?fullBeard:beards,PaintedPortraitLayout.Beard(look),style==0,"beard "+style)),"duplicate beard source");
                }
                var whiteShadow=PaintedPortraitLayout.HairShadow(3);var whiteLight=PaintedPortraitLayout.HairLight(3);
                Check(whiteShadow.grayscale<.40f&&whiteLight.grayscale>.95f,"silver pigment loses dark roots or white highlights");
                for(int color=0;color<4;color++)Check(PaintedPortraitLayout.HairLight(color).grayscale>PaintedPortraitLayout.HairShadow(color).grayscale+.20f,"hair pigment loses strand contrast");
                Debug.Log("MINGLU_GROOMING_SOURCES_SUCCESS: 16 distinct nonempty hair sources, 7 distinct beards and one empty source, transparent source gutters, white hair/beard pigment with strand contrast");
            }
            finally{UnityEngine.Object.DestroyImmediate(hair);UnityEngine.Object.DestroyImmediate(beards);UnityEngine.Object.DestroyImmediate(fullBeard);}
        }
        static void VerifyScalpAndHeadwear()
        {
            var hair=ReadAtlas("hair-styles");var body=ReadAtlas("portrait-bases");int scalpProbes=0,frameEdges=0;
            var errors=new List<string>();
            try
            {
                for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)for(int style=0;style<PaintedPortraitLayout.HairCount;style++)
                {
                    var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});look.parts[0]=face;look.parts[1]=style;
                    var f=PaintedPortraitLayout.Landmarks(look);var bodyCell=PaintedPortraitLayout.Base(look);var hairCell=PaintedPortraitLayout.Hair(look);
                    // The exposed brown crescent sat on the far cranial arc,
                    // above the forehead, where every authored style has hair.
                    // Find that arc from this actual face's alpha silhouette;
                    // do not mistake the legal central forehead for bald scalp.
                    bool reportedArc=false;
                    foreach(float offset in new[]{.080f,.105f,.130f})
                    {
                        float y=f.skull.y+offset;float edge=-1;
                        for(int x=0;x<=160;x++)
                        {
                            // At the top of a narrow elderly skull its outer
                            // arc can lie right of the far-eye centre. Search
                            // as far as the skull centre, still taking only
                            // the very first opaque pixel from the left.
                            float px=.10f+x*(f.skull.x-.10f)/160;
                            if(Alpha(body,bodyCell,new Vector2(px,y))>.95f){edge=px;break;}
                        }
                        if(edge<0){if(!reportedArc)errors.Add("cannot locate far skull arc: "+sex+"/"+face);reportedArc=true;continue;}
                        var p=new Vector2(edge+.006f,y);float coverage=VisibleHairAlpha(hair,body,look,p);
                        if(coverage<=.65f&&!reportedArc){errors.Add("exposed far-side scalp crescent: "+sex+"/"+face+"/"+style+" at "+p+" alpha "+coverage.ToString("F3"));reportedArc=true;}
                        scalpProbes++;
                    }
                    foreach(var crop in new[]{PaintedPortraitLayout.FullCrop,PaintedPortraitLayout.DetailCrop})
                    {
                        // Sample inside the frame edge: source cell gutters
                        // alone cannot detect a bun cut off by the UI mask.
                        bool reportedEdge=false;
                        foreach(float inset in new[]{.004f,.010f})for(int x=0;x<=160;x++)
                        {
                            var p=new Vector2(.10f+x/160f,crop.yMin+inset);
                            float alpha=VisibleHairAlpha(hair,body,look,p);
                            if(alpha>=.10f&&!reportedEdge){errors.Add("headwear touches portrait top mask: "+sex+"/"+face+"/"+style+" crop "+crop.yMin+" at "+p);reportedEdge=true;}
                        }
                        frameEdges++;
                    }
                }
                Check(errors.Count==0,"headwear silhouette regressions (one per arc/frame):\n"+string.Join("\n",errors.ToArray()));
                Debug.Log("MINGLU_GROOMING_SILHOUETTE_SUCCESS: "+scalpProbes+" actual far-skull probes covered after ear/temple masks, "+frameEdges+" full/detail frame edges clear of hair; portrait sheets still reviewed visually");
            }
            finally{UnityEngine.Object.DestroyImmediate(hair);UnityEngine.Object.DestroyImmediate(body);}
        }
        static void VerifyFarTempleSurface()
        {
            var hair=ReadAtlas("hair-styles");var body=ReadAtlas("portrait-bases");
            int combinations=0,browSamples=0,outlineSamples=0,protectedSamples=0,sourceWitnesses=0,legacyWitnesses=0;
            int clothExclusions=0,capSamples=0,capRiskWitnesses=0,capHairWitnesses=0,capStrandSamples=0;
            float maxBrow=0,maxOutside=0,maxProtectedChange=0,maxCapChange=0;
            var errors=new List<string>();
            try
            {
                for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)for(int style=0;style<PaintedPortraitLayout.HairCount;style++)
                {
                    var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});look.parts[0]=face;look.parts[1]=style;
                    var f=PaintedPortraitLayout.Landmarks(look);var trim=PaintedPortraitLayout.HairFarTrim(look);
                    var hairCell=PaintedPortraitLayout.Hair(look);var bodyCell=PaintedPortraitLayout.Base(look);
                    string label=sex+"/"+face+"/"+style;
                    Check(trim.x<trim.y&&trim.z<trim.w&&trim.w<f.browFar.y,"unordered temple fade or lock surviving to the eyebrow: "+label);
                    bool reportedBrow=false,reportedOutside=false,reportedProtected=false;
                    // This forbidden region is tied to anatomy, not the new
                    // trim constants: the far eyebrow/eye must be visible and
                    // there must be no independent hair patch beside them.
                    for(int y=0;y<=10;y++)for(int x=0;x<=18;x++)
                    {
                        var p=new Vector2(f.eyeFar.x-.140f+x*.008f,f.browFar.y-.012f+y*.006f);
                        var sample=PaintedPortraitLayout.HairSamplingPoint(look,p);
                        // A cloth cap owns its silhouette. Exclude only source
                        // pixels classified as cloth, never the whole hairstyle;
                        // the strands below that same cap must still be removed.
                        if(PaintedPortraitLayout.HairHeadwearAlpha(look,sample)>.001f){clothExclusions++;continue;}
                        float source=Alpha(hair,hairCell,sample)*PaintedPortraitLayout.HairEarAlpha(look,p);
                        float visible=VisibleHairAlpha(hair,body,look,p);
                        maxBrow=Mathf.Max(maxBrow,visible);browSamples++;
                        if(source>.05f)sourceWitnesses++;
                        if(sex==0&&style==3&&source>.05f)capHairWitnesses++;
                        if(source*LegacyFarHairAlpha(body,look,p)>.05f)legacyWitnesses++;
                        if(visible>.01f&&!reportedBrow){errors.Add("hair still covers far brow/eye: "+label+" at "+p+" alpha "+visible.ToString("F3"));reportedBrow=true;}
                    }
                    // Find the outside of the actual face painting. A common
                    // hard-coded left X would miss older/narrower faces. Do not
                    // test only points on skin, which hid the original defect.
                    for(int y=0;y<=10;y++)
                    {
                        float py=f.browFar.y-.050f+y*.007f,edge=-1;
                        for(int x=0;x<=160;x++)
                        {
                            float px=.10f+x*(f.skull.x-.10f)/160;
                            if(Alpha(body,bodyCell,new Vector2(px,py))>.95f){edge=px;break;}
                        }
                        Check(edge>=0,"cannot locate lower temple outline: "+label+" at y "+py);
                        for(int x=0;x<=20;x++)
                        {
                            var p=new Vector2(edge-.012f-x*.006f,py);
                            if(p.x>f.eyeFar.x+.045f||Alpha(body,bodyCell,p+new Vector2(.010f,0))>.01f)continue;
                            var sample=PaintedPortraitLayout.HairSamplingPoint(look,p);
                            if(PaintedPortraitLayout.HairHeadwearAlpha(look,sample)>.001f){clothExclusions++;continue;}
                            float source=Alpha(hair,hairCell,sample)*PaintedPortraitLayout.HairEarAlpha(look,p);
                            float visible=VisibleHairAlpha(hair,body,look,p);
                            maxOutside=Mathf.Max(maxOutside,visible);outlineSamples++;
                            if(source>.05f)sourceWitnesses++;
                            if(sex==0&&style==3&&source>.05f)capHairWitnesses++;
                            if(source*LegacyFarHairAlpha(body,look,p)>.05f)legacyWitnesses++;
                            if(visible>.02f&&!reportedOutside){errors.Add("detached hair remains outside lower temple: "+label+" at "+p+" alpha "+visible.ToString("F3"));reportedOutside=true;}
                        }
                    }
                    // Protect nonempty source strands above the skull and on
                    // the near/right side. A mask that deletes all hair, uses
                    // the wrong side, or shaves the crown must not pass.
                    for(int region=0;region<2;region++)
                    {
                        int opaque=0;
                        for(int y=0;y<=20;y++)for(int x=0;x<=16;x++)
                        {
                            var p=region==0
                                ?new Vector2(f.skull.x-.12f+x*.015f,f.skull.y-.05f+y*.006f)
                                :new Vector2(f.earNear.x-.11f+x*.017f,Mathf.Lerp(f.skull.y+.055f,f.chin.y+.180f,y/20f));
                            float source=Alpha(hair,hairCell,PaintedPortraitLayout.HairSamplingPoint(look,p))*PaintedPortraitLayout.HairEarAlpha(look,p);
                            if(source<.5f)continue;
                            // Deliberate skin-facing root feather is separate
                            // from accidental loss caused by the far trim.
                            float change=Mathf.Abs(VisibleHairAlpha(hair,body,look,p)-source*HairRootFeather(hair,body,look,p));
                            maxProtectedChange=Mathf.Max(maxProtectedChange,change);opaque++;protectedSamples++;
                            if(change>.001f&&!reportedProtected){errors.Add("far trim removes "+(region==0?"crown":"near/back hair")+": "+label+" at "+p+" loss "+change.ToString("F3"));reportedProtected=true;}
                        }
                        Check(opaque>=10,"protection region lacks real opaque "+(region==0?"crown":"near-side")+" source samples: "+label+" count "+opaque);
                    }
                    if(sex==0&&style==3)
                    {
                        // The source cap has no pure strands in some far-brow
                        // windows. Check actual source hair below/behind the
                        // cloth instead of requiring an invented forbidden lock.
                        int strands=0;
                        for(int sy=40;sy<100;sy++)for(int sx=0;sx<100;sx++)
                        {
                            var sample=new Vector2((sx+.5f)/100,(sy+.5f)/100);
                            if(Alpha(hair,hairCell,sample)>.5f&&PaintedPortraitLayout.HairHeadwearAlpha(look,sample)<.001f)strands++;
                        }
                        Check(strands>=20,"headwear protection incorrectly labels the entire hair sheet as cloth: "+label);
                        capStrandSamples+=strands;
                        int rows=0,retained=0,vulnerable=0;bool reportedCap=false;
                        // Locate the cap's real left contour, including the
                        // upper part outside the skull. Interior crown probes
                        // alone cannot detect a cap shaved down to a bald head.
                        for(int y=0;y<=90;y++)
                        {
                            float py=Mathf.Lerp(f.skull.y-.160f,f.hairline.y+.015f,y/90f),left=-1;
                            for(int x=0;x<=200;x++)
                            {
                                var p=new Vector2(Mathf.Lerp(f.eyeFar.x-.220f,f.skull.x-.045f,x/200f),py);
                                var sample=PaintedPortraitLayout.HairSamplingPoint(look,p);
                                if(Alpha(hair,hairCell,sample)>=.15f&&PaintedPortraitLayout.HairHeadwearAlpha(look,sample)>.99999f){left=p.x;break;}
                            }
                            if(left<0)continue;
                            rows++;
                            for(int inset=0;inset<4;inset++)
                            {
                                var p=new Vector2(left+inset*.003f,py);var sample=PaintedPortraitLayout.HairSamplingPoint(look,p);
                                if(PaintedPortraitLayout.HairHeadwearAlpha(look,sample)<.99999f)continue;
                                float source=Alpha(hair,hairCell,sample)*PaintedPortraitLayout.HairEarAlpha(look,p);
                                if(source<.15f)continue;
                                float change=Mathf.Abs(VisibleHairAlpha(hair,body,look,p)-source);
                                maxCapChange=Mathf.Max(maxCapChange,change);retained++;capSamples++;
                                if(change>.001f&&!reportedCap){errors.Add("left/upper cloth cap contour is shaved: "+label+" at "+p+" loss "+change.ToString("F4"));reportedCap=true;}
                                float outline=Alpha(body,bodyCell,p+PaintedPortraitLayout.HairOutlineOffset(look,p.y));
                                float withoutClothProtection=source*PaintedPortraitLayout.HairFarSideAlpha(look,p,outline,0);
                                if(source-withoutClothProtection>.03f){vulnerable++;capRiskWitnesses++;}
                            }
                        }
                        Check(rows>=12&&retained>=30,"cloth cap contour checks lack real source samples: "+label+" rows/samples "+rows+"/"+retained);
                        Check(vulnerable>0,"cap regression has no real contour pixels at risk without material protection: "+label);
                    }
                    combinations++;
                }
                Check(outlineSamples>combinations*50,"outside-face scan was vacuous");
                Check(sourceWitnesses>0&&legacyWitnesses>0,"negative control failed to expose the previous visible far-side hair defect in actual PNG samples");
                Check(capRiskWitnesses>0&&capStrandSamples>0,"cloth/hair material separation was vacuous");
                Check(errors.Count==0,"far-temple final alpha regressions (one per region/combination):\n"+string.Join("\n",errors.ToArray()));
                Debug.Log("MINGLU_GROOMING_FAR_TEMPLE_SUCCESS: "+combinations+" face/hair pairs; "+browSamples+" brow/eye, "+outlineSamples+" outside-face and "+protectedSamples+" opaque crown/near-side samples; "+legacyWitnesses+" legacy-mask residual witnesses from "+sourceWitnesses+" populated forbidden samples; "+capSamples+" cloth-cap contour samples including "+capRiskWitnesses+" at-risk pixels, "+clothExclusions+" cloth exclusions and "+capHairWitnesses+" forbidden hair samples under that cap; max forbidden alpha "+maxBrow.ToString("F4")+"/"+maxOutside.ToString("F4")+", max protected loss "+maxProtectedChange.ToString("F4")+"/"+maxCapChange.ToString("F4")+". Source-surface checks, not a replacement for render review.");
            }
            finally{UnityEngine.Object.DestroyImmediate(hair);UnityEngine.Object.DestroyImmediate(body);}
        }
        static void VerifyFeatureFitting()
        {
            int surfaces=0,earPairs=0,beardPairs=0;
            float minJacobian=float.MaxValue;
            var categories=new[]{2,3,4,5,13};
            for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)
            {
                var look=AppearanceRules.AsRealistic(new Appearance{enabled=true,sex=sex});look.parts[0]=face;
                var f=PaintedPortraitLayout.Landmarks(look);
                // Every option is exercised once in isolation and in eight
                // mixed combinations. Check areas between anchors too: an
                // anchor alone cannot reveal a fold in a cheek or eyelid.
                for(int category=-1;category<categories.Length;category++)for(int option=0;option<8;option++)
                {
                    for(int c=2;c<=5;c++)look.parts[c]=category<0?(option+c*3)%8:0;look.ears=category<0?(option+5)%8:0;
                    if(category>=0){int c=categories[category];if(c==13)look.ears=option;else look.parts[c]=option;}
                    for(int x=0;x<=44;x++)for(int y=0;y<=34;y++)
                    {
                        var p=new Vector2(.19f+x*.015f,.23f+y*.012f);
                        var q=PaintedPortraitLayout.FeatureSamplingPoint(look,p);
                        var dx=(PaintedPortraitLayout.FeatureSamplingPoint(look,p+new Vector2(.0005f,0))-q)/.0005f;
                        var dy=(PaintedPortraitLayout.FeatureSamplingPoint(look,p+new Vector2(0,.0005f))-q)/.0005f;
                        float jacobian=dx.x*dy.y-dx.y*dy.x;
                        minJacobian=Mathf.Min(minJacobian,jacobian);
                        Check(jacobian>.15f,"facial edit folds or pinches the source: "+sex+"/"+face+"/"+category+"/"+option);
                        Check(q.x>0&&q.x<1&&q.y>0&&q.y<1,"facial edit samples a neighbouring atlas cell");
                    }
                    foreach(var point in new[]{f.skull,f.hairline,f.chin,PaintedPortraitLayout.CollarTarget(look,2)})
                        Check(Vector2.Distance(point,PaintedPortraitLayout.FeatureSamplingPoint(look,point))<.00001f,"feature edit displaces the head outline, chin or collar");
                    surfaces++;
                }
                for(int c=2;c<=5;c++)look.parts[c]=0;
                for(int ear=0;ear<8;ear++)for(int hair=0;hair<PaintedPortraitLayout.HairCount;hair++)
                {
                    look.ears=ear;look.parts[1]=hair;
                    var bounds=PaintedPortraitLayout.HairCanvas(look);
                    for(int anchor=0;anchor<4;anchor++)
                    {
                        var source=PaintedPortraitLayout.HairSource(look,anchor);
                        var expected=new Vector2((source.x-bounds.x)/bounds.width,(source.y-bounds.y)/bounds.height);
                        Check(Vector2.Distance(PaintedPortraitLayout.HairSamplingPoint(look,PaintedPortraitLayout.HairTarget(look,anchor)),expected)<.001f,"edited ear moves the registered hairline");
                    }
                    var center=new Vector2(f.earNear.x,f.earNear.y);
                    Check(PaintedPortraitLayout.HairEarAlpha(look,center)==0,"hair covers the edited ear opening");
                    // The opening must actually respond to ear size, not just
                    // cut the same fixed ellipse for all eight choices.
                    var edge=center+new Vector2(f.earNear.z*.48f,0);
                    float edited=PaintedPortraitLayout.HairEarAlpha(look,edge);
                    look.ears=0;float native=PaintedPortraitLayout.HairEarAlpha(look,edge);look.ears=ear;
                    if(ear==1)Check(edited>native,"narrow ear retains an oversized hair opening");
                    if(ear==2)Check(edited<native,"wide ear remains covered by hair");
                    earPairs++;
                }
                look.ears=0;
                if(sex==0)for(int beard=1;beard<PaintedPortraitLayout.BeardCount;beard++)for(int mouth=0;mouth<8;mouth++)for(int nose=0;nose<8;nose++)
                {
                    look.parts[6]=beard;look.parts[5]=mouth;look.parts[4]=nose;
                    var center=new Vector2(f.mouth.x,f.mouth.y);
                    Check(Vector2.Distance(PaintedPortraitLayout.BeardSamplingPoint(look,center),PaintedPortraitLayout.BeardSource(look,0))<.0001f,"edited mouth loses beard anchor");
                    if(!BeardContour.Enabled(look))Check(Vector2.Distance(PaintedPortraitLayout.BeardSamplingPoint(look,f.chin),PaintedPortraitLayout.BeardSource(look,1))<.0001f,"edited mouth displaces beard from the chin");
                    Check(PaintedPortraitLayout.BeardLipAlpha(look,center)==0,"beard paints over the lip opening");
                    float previous=-10;
                    for(int y=0;y<=60;y++)
                    {
                        var q=PaintedPortraitLayout.BeardSamplingPoint(look,center+new Vector2(0,-.055f+y*.004f));
                        // The short full beard is a bounded facial mesh. Its
                        // transparent exterior is not a fold or a long tip.
                        // Triangle continuity/actual roots are tested separately.
                        if(BeardContour.Enabled(look)&&q.x<0)continue;
                        Check(q.y>previous,"nose or mouth edit folds the beard");previous=q.y;
                    }
                    beardPairs++;
                }
            }
            Debug.Log("MINGLU_PORTRAIT_FEATURE_FIT_SUCCESS: "+surfaces+" face/feature surfaces, min Jacobian "+minJacobian.ToString("F3")+", "+earPairs+" hair/edited-ear pairs, "+beardPairs+" beard/mouth/nose pairs; framing and collar anchors preserved");
        }
        static void VerifyRegistration()
        {
            var hair=ReadAtlas("hair-styles");var body=ReadAtlas("portrait-bases");var outfit=ReadAtlas("outfit-overlays");
            try
            {
                for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)
                {
                    var look=AppearanceRules.Randomize(17,sex);look.parts[0]=face;var feature=PaintedPortraitLayout.Landmarks(look);
                    var mouth=new Vector2(feature.mouth.x,feature.mouth.y);
                    Check(Alpha(body,PaintedPortraitLayout.Base(look),mouth)>.90f,"mouth registration falls outside actual painted face");
                    Check(Alpha(body,PaintedPortraitLayout.Base(look),feature.chin)>.90f,"chin registration falls outside actual painted face");
                    Check(Alpha(body,PaintedPortraitLayout.Base(look),feature.chin+new Vector2(.060f,.085f))>.90f,"continuous painted neck missing under the garment opening");
                    Check(feature.chin.x>mouth.x+.02f&&feature.chin.y>mouth.y+.05f,"three-quarter mouth/chin separation lost");
                    for(int clothing=0;clothing<8;clothing++)
                    {
                        look.parts[7]=clothing;
                        var garment=PaintedPortraitLayout.OutfitRect(look);
                        Check(PaintedPortraitLayout.FullCrop.xMin<garment.xMin&&PaintedPortraitLayout.FullCrop.xMax>garment.xMax,"full portrait crops the shoulder silhouette");
                        for(int anchor=3;anchor<5;anchor++)
                        {
                            var binding=PaintedPortraitLayout.OutfitBinding(look,anchor);
                            var target=new Vector2(binding.x,binding.y);
                            var expected=new Vector2((binding.x-binding.z-garment.x)/garment.width,(binding.y-binding.w-garment.y)/garment.height);
                            Check(Vector2.Distance(PaintedPortraitLayout.OutfitSamplingPoint(look,target),expected)<.001f,"collar movement displaces a shoulder");
                        }
                        for(int anchor=0;anchor<3;anchor++)
                            Check(Vector2.Distance(PaintedPortraitLayout.OutfitSamplingPoint(look,PaintedPortraitLayout.CollarTarget(look,anchor)),PaintedPortraitLayout.CollarSource(look,anchor))<.001f,"garment collar misses its anatomical binding");
                        // A valid set of anchor points can still fold the fabric
                        // between them. Check the local sampling orientation.
                        for(int x=0;x<=20;x++)for(int y=0;y<=16;y++)
                        {
                            var p=new Vector2(-.15f+x*.067f,.52f+y*.05f);var q=PaintedPortraitLayout.OutfitSamplingPoint(look,p);
                            var dx=PaintedPortraitLayout.OutfitSamplingPoint(look,p+new Vector2(.001f,0))-q;
                            var dy=PaintedPortraitLayout.OutfitSamplingPoint(look,p+new Vector2(0,.001f))-q;
                            Check(dx.x*dy.y-dx.y*dy.x>0,"collar registration folds the garment");
                        }
                    }
                    for(int h=0;h<PaintedPortraitLayout.HairCount;h++)
                    {
                        look.parts[1]=h;var bounds=PaintedPortraitLayout.HairCanvas(look);
                        for(int anchor=0;anchor<4;anchor++)
                        {
                            var source=PaintedPortraitLayout.HairSource(look,anchor);
                            var expected=new Vector2((source.x-bounds.x)/bounds.width,(source.y-bounds.y)/bounds.height);
                            var actual=PaintedPortraitLayout.HairSamplingPoint(look,PaintedPortraitLayout.HairTarget(look,anchor));
                            Check(Vector2.Distance(actual,expected)<.001f,"hair anatomical anchor misses source registration");
                        }
                        var opening=PaintedPortraitLayout.HairSamplingPoint(look,new Vector2(feature.earNear.x,feature.earNear.y));
                        Check(Alpha(hair,PaintedPortraitLayout.Hair(look),opening)<.20f,"registered ear opening contains opaque hair pixels");
                    }
                    if(sex==0)for(int beard=1;beard<PaintedPortraitLayout.BeardCount;beard++)
                    {
                        look.parts[6]=beard;
                        var atMouth=PaintedPortraitLayout.BeardSamplingPoint(look,mouth);
                        var atChin=PaintedPortraitLayout.BeardSamplingPoint(look,feature.chin);
                        Check(Vector2.Distance(atMouth,PaintedPortraitLayout.BeardSource(look,0))<.0001f,"beard mouth anchor drift");
                        if(!BeardContour.Enabled(look))Check(Vector2.Distance(atChin,PaintedPortraitLayout.BeardSource(look,1))<.0001f,"beard chin anchor drift");
                        float last=-10;
                        for(int sample=0;sample<=40;sample++)
                        {
                            float y=mouth.y-.06f+sample*.008f;var q=PaintedPortraitLayout.BeardSamplingPoint(look,new Vector2(mouth.x,y));
                            if(BeardContour.Enabled(look)&&q.x<0)continue;
                            Check(q.y>last,"beard folds between lips and jaw");last=q.y;
                        }
                    }
                }
                Check(PaintedPortraitLayout.FullCrop.xMin<=-.112f&&PaintedPortraitLayout.FullCrop.xMax>=1.158f,"full framing crops authored shoulders");
                for(int sex=0;sex<2;sex++)foreach(int clothing in new[]{1,5})
                {
                    var look=AppearanceRules.Randomize(17,sex);look.parts[7]=clothing;var cell=PaintedPortraitLayout.Outfit(look);
                    var sliver=new Vector2(.0015f,.65f);var gap=new Vector2(.014f,.65f);var shoulder=new Vector2(.04f,.65f);
                    Check(Alpha(outfit,cell,sliver)>.10f,"expected detached atlas sliver moved; recheck the source crop");
                    Check(Alpha(outfit,cell,gap)<.10f,"garment trim no longer falls in the transparent gap");
                    Check(Alpha(outfit,cell,shoulder)>.90f,"expected garment shoulder moved");
                    Check(PaintedPortraitLayout.OutfitEdgeAlpha(look,sliver)==0&&PaintedPortraitLayout.OutfitEdgeAlpha(look,shoulder)==1,"garment edge trim removes a shoulder or retains a sliver");
                }
                Debug.Log("MINGLU_PORTRAIT_REGISTRATION_SUCCESS: 16 continuous necks, 384 collar bindings without fabric folds, 512 hair bindings, 128 actual transparent ear openings, 56 separate lip/jaw bindings; visual sheets still required");
            }
            finally{UnityEngine.Object.DestroyImmediate(hair);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(outfit);}
        }
        public static void Verify()
        {
            CheckAtlas("portrait-bases",4,4);CheckAtlas("hair-styles",4,4);CheckAtlas("outfit-overlays",4,4,1);CheckAtlas("beard-styles",4,2);
            CheckAtlas("beard-full-contour",1,1,1);
            var shader=Resources.Load<Shader>("Minglu/Art/PaintedPortrait");
            Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"shader compilation");
            VerifyGroomingSources();
            VerifyScalpAndHeadwear();
            VerifyFarTempleSurface();
            VerifyRegistration();
            VerifyFeatureFitting();
            int combinations=0;
            for(int sex=0;sex<2;sex++)for(int face=0;face<8;face++)for(int hair=0;hair<PaintedPortraitLayout.HairCount;hair++)for(int outfit=0;outfit<8;outfit++)
            {
                var look=AppearanceRules.Randomize(17,sex);look.parts[0]=face;look.parts[1]=hair;look.parts[7]=outfit;
                for(int beard=0;beard<(sex==0?PaintedPortraitLayout.BeardCount:1);beard++)
                {
                    look.parts[6]=beard;AppearanceRules.Validate(look);
                    CheckCell(PaintedPortraitLayout.Base(look));CheckCell(PaintedPortraitLayout.Hair(look));CheckCell(PaintedPortraitLayout.Outfit(look));CheckCell(PaintedPortraitLayout.Beard(look));
                    var hairRect=PaintedPortraitLayout.HairRect(look);var hairUV=PaintedPortraitLayout.Hair(look);
                    Check(Mathf.Abs(hairRect.width/hairRect.height-hairUV.width/hairUV.height)<.001f,"hair registration stretches source pixels");
                    var outfitRect=PaintedPortraitLayout.OutfitRect(look);
                    Check(Mathf.Abs(outfitRect.width/outfitRect.height*2f/3-1)<.001f,"square clothes stretched");
                    combinations++;
                }
                var feature=PaintedPortraitLayout.Landmarks(look);
                Check(feature.eyeFar.x<feature.eyeNear.x&&feature.eyeFar.z<feature.eyeNear.z,"3/4 eye registration lost");
            }
            for(int seed=0;seed<200;seed++)
            {
                var look=AppearanceRules.Randomize(seed);string original=JsonUtility.ToJson(look);
                var next=AppearanceRules.AsRealistic(look);
                Check(original==JsonUtility.ToJson(look)&&original==JsonUtility.ToJson(next),"version 15 choices mutated by renderer migration");
                var restored=JsonUtility.FromJson<Appearance>(original);Check(original==JsonUtility.ToJson(restored),"save parts roundtrip");
            }
            foreach(var size in new[]{new Vector2(328,470),new Vector2(66,94),new Vector2(64,91),new Vector2(230,360)})
            {
                foreach(bool detail in new[]{false,true})
                {
                    var placement=PaintedPortraitLayout.Placement(size.x,size.y,detail);
                    Check(Mathf.Abs(placement.width/placement.height-2f/3)<.0001f,"portrait distorted by UI frame");
                }
            }
            Debug.Log("MINGLU_PAINTED_PORTRAIT_SUCCESS: 4 atlases, shader, "+combinations+" valid layer combinations, 200 unchanged saved looks, consistent full/detail proportions");
        }
    }
}
