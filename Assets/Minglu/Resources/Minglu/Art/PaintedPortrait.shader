Shader "Minglu/PaintedPortrait"
{
 Properties
 {
  [PerRendererData] _MainTex("Portrait atlas",2D)="white"{}
  _BodyTex("Connected head and neck",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct input {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
   struct output {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
   sampler2D _MainTex,_BodyTex;float4 _MainTex_TexelSize;
   fixed4 _Color,_Complexion,_ClothShadow,_ClothLight,_HairShadow,_HairLight;
   float4 _ClipRect,_AtlasRect,_LayerRect,_DrawRect,_BodyRect,_EyeFar,_EyeNear,_BrowFar,_BrowNear,_Nose,_Mouth,_EarFar,_EarNear;
   float4 _EyeShape,_BrowShape,_NoseShape,_MouthShape,_EarShape;
   float4 _HairAnchor0,_HairAnchor1,_HairAnchor2,_HairAnchor3,_CrownAdjust,_BeardMouth,_BeardChin,_BeardScales;
   float4 _CollarAnchor0,_CollarAnchor1,_CollarAnchor2,_CollarAnchor3,_CollarAnchor4,_CollarAnchor5,_CollarAnchor6,_CollarAnchor7,_Jaw;
   float4 _HairCanvas,_BeardSurface,_HairFarTrim,_HairOutlineGuide;
   float _LayerMode,_BackHair,_CapCloth,_OutfitLeftTrim,_FarScalpPad;
   float _FullBeard;float4 _BeardContour[30];
   output vert(input v){output o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float2 morph(float2 p,float4 feature,float2 size,float tilt)
   {
    float2 delta=p-feature.xy;
    float weight=1-smoothstep(.12,1,length(delta/feature.zw));
    float2 transformed=delta/size;transformed.y+=delta.x*tilt;
    return (transformed-delta)*weight;
   }
   float2 earMorph(float2 p)
   {return morph(p,_EarFar,_EarShape.xy,_EarShape.z)+morph(p,_EarNear,_EarShape.xy,_EarShape.z);}
   float2 faceMorph(float2 p)
   {
    return morph(p,_EyeFar,_EyeShape.xy,_EyeShape.z*.70)+morph(p,_EyeNear,_EyeShape.xy,-_EyeShape.z)
     +morph(p,_BrowFar,_BrowShape.xy,_BrowShape.z*.70)+morph(p,_BrowNear,_BrowShape.xy,-_BrowShape.z)
     +morph(p,_Nose,_NoseShape.xy,_NoseShape.z)+morph(p,_Mouth,_MouthShape.xy,_MouthShape.z)+earMorph(p);
   }
   float3 hairAnchor(float2 p,float4 anchor)
   {
    float2 d=(p-anchor.xy)*float2(1,1.5);
    float r=dot(d,d);float weight=1/(r+.0000001);
    return float3(anchor.zw*weight,weight);
   }
   float2 hairSample(float2 p)
   {
    // The slightly more frontal male skull is wider at the far upper temple.
    // Fill only this arc on tied styles; all four registration anchors remain
    // outside the correction, including the hairline and the exposed ear.
    float scalp=(1-smoothstep(_EyeFar.x+.055,_HairAnchor1.x-.010,p.x))*(1-smoothstep(_CrownAdjust.y+.146,_BrowFar.y-.020,p.y))*smoothstep(_CrownAdjust.y-.035,_CrownAdjust.y+.035,p.y);
    p.x+=scalp*_FarScalpPad;
    float influence=1-smoothstep(_CrownAdjust.y-.02,_CrownAdjust.y+.03,p.y);
    p=lerp(p,_CrownAdjust.xy+(p-_CrownAdjust.xy)/_CrownAdjust.z,influence);
    float3 blend=hairAnchor(p,_HairAnchor0)+hairAnchor(p,_HairAnchor1)+hairAnchor(p,_HairAnchor2)+hairAnchor(p,_HairAnchor3);
    return (p-blend.xy/blend.z-_LayerRect.xy)/_LayerRect.zw;
   }
   float cross2(float2 a,float2 b){return a.x*b.y-a.y*b.x;}
   bool beardTriangle(float2 p,float4 a,float4 b,float4 c,out float2 source)
   {
    float2 ab=b.xy-a.xy,ac=c.xy-a.xy,ap=p-a.xy;
    float det=cross2(ab,ac),u=cross2(ap,ac)/det,v=cross2(ab,ap)/det;
    source=a.zw+u*(b.zw-a.zw)+v*(c.zw-a.zw);
    return u>=-.00001&&v>=-.00001&&u+v<=1.00001;
   }
   float2 beardSample(float2 p)
   {
    if(_FullBeard>.5)
    {
     float2 sample;
     [loop] for(int row=0;row<4;row++)[loop] for(int col=0;col<5;col++)
     {
      int a=row*6+col,b=a+1,c=a+6,d=c+1;
      if(beardTriangle(p,_BeardContour[a],_BeardContour[b],_BeardContour[d],sample))return sample;
      if(beardTriangle(p,_BeardContour[a],_BeardContour[d],_BeardContour[c],sample))return sample;
     }
     return float2(-1,-1);
    }
    float row=saturate((p.y-_BeardMouth.y)/(_BeardChin.y-_BeardMouth.y));
    float axis=lerp(_BeardMouth.x,_BeardChin.x,row),cheek=p.x-axis;
    p.y+=abs(cheek)*(cheek<0?_BeardSurface.z:_BeardSurface.w)*smoothstep(_BeardMouth.y-.012,_BeardChin.y,p.y);
    float t=saturate((p.y-_BeardMouth.y)/(_BeardChin.y-_BeardMouth.y));
    float center=lerp(_BeardMouth.x,_BeardChin.x,t),sourceCenter=lerp(_BeardMouth.z,_BeardChin.z,t);
    float sy=p.y<_BeardMouth.y?_BeardMouth.w+(p.y-_BeardMouth.y)/_BeardScales.z:p.y>_BeardChin.y?_BeardChin.w+(p.y-_BeardChin.y)/_BeardScales.w:lerp(_BeardMouth.w,_BeardChin.w,t);
    float sx=lerp(p.x<center?_BeardScales.x:_BeardScales.y,p.x<center?_BeardSurface.x:_BeardSurface.y,t);
    return float2(sourceCenter+(p.x-center)/sx,sy);
   }
   float2 outfitSample(float2 p)
   {
    float3 blend=hairAnchor(p,_CollarAnchor0)+hairAnchor(p,_CollarAnchor1)+hairAnchor(p,_CollarAnchor2);
    // Both acromion regions and the chest constrain the collar's influence.
    blend+=hairAnchor(p,_CollarAnchor3)+hairAnchor(p,_CollarAnchor4);
    blend+=hairAnchor(p,_CollarAnchor5)+hairAnchor(p,_CollarAnchor6)+hairAnchor(p,_CollarAnchor7);
    return (p-blend.xy/blend.z-_LayerRect.xy)/_LayerRect.zw;
   }
   fixed4 bodyAt(float2 p)
   {
    float inside=step(0,p.x)*step(p.x,1)*step(0,p.y)*step(p.y,1);
    p=clamp(p,.001,.999);
    fixed4 body=tex2D(_BodyTex,float2(_BodyRect.x+p.x*_BodyRect.z,_BodyRect.y+(1-p.y)*_BodyRect.w));
    body.a*=inside;return body;
   }
   fixed4 frag(output i):SV_Target
   {
    float2 local=float2((i.uv.x-_AtlasRect.x)/_AtlasRect.z,1-(i.uv.y-_AtlasRect.y)/_AtlasRect.w);
    float2 p=_DrawRect.xy+local*_DrawRect.zw;
    float2 q=p;
    // Face and beard share every local deformation, including the nose above
    // a moustache. The hair's ear opening follows the identical ear mapping.
    if(_LayerMode<.5||_LayerMode>2.5)q+=faceMorph(p);
    else if(_LayerMode<1.5)q+=earMorph(p);
    float2 featurePoint=q;
    q=_LayerMode>.5&&_LayerMode<1.5?hairSample(q):_LayerMode>2.5?beardSample(q):_LayerMode>1.5?outfitSample(q):(q-_LayerRect.xy)/_LayerRect.zw;
    float sourceInside=step(0,q.x)*step(q.x,1)*step(0,q.y)*step(q.y,1);
    float2 uv=float2(_AtlasRect.x+q.x*_AtlasRect.z,_AtlasRect.y+(1-q.y)*_AtlasRect.w);
    // Bilinear sampling must never borrow pixels from the neighbouring person.
    uv=clamp(uv,_AtlasRect.xy+_MainTex_TexelSize.xy*.75,_AtlasRect.xy+_AtlasRect.zw-_MainTex_TexelSize.xy*.75);
    fixed4 c=tex2D(_MainTex,uv);c.a*=sourceInside;
    float headwear=0;
    if(_LayerMode<.5)
    {
     // The source is a connected head/neck painting with an under-robe.
     // Do not render that obsolete garment behind the selected outfit: its
     // raised collar and shoulder tips otherwise protrude as a second contour.
     float jawY=lerp(_Jaw.y,_Jaw.w+.095,saturate((p.x-_Jaw.x)/max(.05,_Jaw.z-_Jaw.x)));
     float head=1-smoothstep(jawY+.010,jawY+.032,p.y);
     float neck=smoothstep(.055,.100,c.r-c.b)*(1-smoothstep(.72,.80,p.y));
     c.a*=max(head,neck);
     // The base painting includes its connected neck and an under-robe. Above
     // the collar the whole face receives the complexion; below it, warm skin
     // pigments distinguish the exposed neck from neutral grey cloth.
     float skin=max(1-smoothstep(.54,.58,p.y),smoothstep(.035,.11,c.r-c.b));
     float light=dot(c.rgb,float3(.299,.587,.114));
     float3 dyed=lerp(_ClothShadow.rgb,_ClothLight.rgb,saturate(light*1.45));
     c.rgb=lerp(dyed,c.rgb*_Complexion.rgb,skin);
    }
    else
    {
     float luminance=dot(c.rgb,float3(.299,.587,.114));
     if(_LayerMode>1.5&&_LayerMode<2.5)
     {
      // These two garments inherit a detached 2-3 pixel line from the
      // preceding atlas column. The fade ends inside the transparent gap,
      // before the authored shoulder, so it cannot narrow the body.
      c.a*=lerp(1,smoothstep(.0128,.0160,q.x),_OutfitLeftTrim);
      c.rgb=lerp(_ClothShadow.rgb,_ClothLight.rgb,saturate(luminance*1.45));
      // Protect the face and jaw, not the entire warm-coloured neck. A
      // standing collar/scarf must be allowed to cover the lower throat.
      // The continuous base stays opaque underneath every collar opening.
      fixed4 body=bodyAt(p);
      float jawY=lerp(_Jaw.y,_Jaw.w+.095,saturate((p.x-_Jaw.x)/max(.05,_Jaw.z-_Jaw.x)));
      float faceProtection=1-smoothstep(jawY+.010,jawY+.026,p.y);
      c.a*=1-body.a*faceProtection;
     }
     else
     {
      // The two current sheets share their luminance range. Preserve painted
      // roots and highlights; a high ivory floor flattened them into a cap.
      float tone=_LayerMode>2.5&&_FullBeard>.5?saturate((luminance-.05)*2.6):saturate(luminance*1.42);
      float3 dyed=lerp(_HairShadow.rgb,_HairLight.rgb,tone);
      if(_LayerMode>2.5)
      {
       // The beard receives the same facial light as the skin beneath it.
       // A palette tint alone made white hair into an evenly lit mesh.
       float faceLight=dot(bodyAt(featurePoint).rgb,float3(.299,.587,.114));
       dyed*=lerp(.73,1.02,smoothstep(.22,.68,faceLight));
       // Allow sparse dark roots to reveal the underlying painted volume.
       c.a*=lerp(.68,1,smoothstep(.12,.58,tone));
      }
      float preserve=0;
      if(_LayerMode<1.5)
      {
       float2 source=_HairCanvas.xy+q*_HairCanvas.zw;
       float capEdge=.552+max(0,source.x-.383)*.32;
       float cap=max(1-smoothstep(capEdge-.006,capEdge+.012,source.y),smoothstep(.72,.80,source.x)*smoothstep(.59,.67,source.y));
       headwear=_CapCloth*cap;
       // A white-haired elder keeps a cloth cap and coloured hair ornaments;
       // those materials are not strands and must not turn ivory with age.
       preserve=max(_CapCloth*cap,smoothstep(.10,.18,c.r-c.b));
      }
      c.rgb=lerp(dyed,c.rgb,preserve);
     }
    }
    if(_LayerMode>.5&&_LayerMode<1.5)
    {
     // Preserve the painted opening of the near ear. Ear edits and age no
     // longer leave an opaque lock across the concha after hair registration.
     float ear=1-smoothstep(.45,1,length((featurePoint-_EarNear.xy)/(_EarNear.zw*float2(.58,.65))));
     c.a*=1-ear;
     // Fit the lower far temple to this face's real outline. A small allowance
     // keeps a painted edge; the upper skull and near/back mass remain intact.
     float outline=(1-smoothstep(_CrownAdjust.x-.050,_CrownAdjust.x+.030,p.x))*smoothstep(_HairFarTrim.x,_HairFarTrim.y,p.y);
     float arc=smoothstep(_HairOutlineGuide.x,_HairOutlineGuide.y,p.y);
     float2 allowance=float2(lerp(_HairOutlineGuide.z,_HairOutlineGuide.w,arc),lerp(_HairOutlineGuide.z,0,arc));
     // Cloth headwear has its own silhouette and must not be shaved down to
     // the scalp like strands; retain the source cap and its hanging cloth.
     float outlineCoverage=smoothstep(.05,.95,bodyAt(p+allowance).a);
     c.a*=1-outline*(1-outlineCoverage)*(1-headwear);
     // Hide the far-side front lock before the eyebrow, including free wisps
     // outside the base silhouette. Both hair passes use the identical mask.
     float farBoundary=lerp(_EyeFar.x+.015,_Jaw.x+.10,smoothstep(_Mouth.y-.05,_Jaw.y+.020,p.y));
     float farSide=1-smoothstep(farBoundary,farBoundary+.045,p.x);
     float height=smoothstep(_HairFarTrim.z,_HairFarTrim.w,p.y);
     c.a*=1-farSide*height*(1-headwear);
     // Only the skin-facing roots receive the soft transition, not the
     // outer crown, cloth cap or free ends behind the shoulders.
     float2 rootTexel=_MainTex_TexelSize.xy*2.4;
     float rootCoverage=(tex2D(_MainTex,uv+float2(rootTexel.x,0)).a+tex2D(_MainTex,uv-float2(rootTexel.x,0)).a
                        +tex2D(_MainTex,uv+float2(0,rootTexel.y)).a+tex2D(_MainTex,uv-float2(0,rootTexel.y)).a)*.25;
     float skinRoot=bodyAt(p).a*smoothstep(_HairAnchor1.y-.025,_HairAnchor1.y+.025,p.y)*(1-headwear);
     c.a*=lerp(1,lerp(.35,1,smoothstep(.1,.98,rootCoverage)),skinRoot);
     // Split only the final clipped alpha. Applying the masks after splitting
     // darkens feathered edges where front and back layers overlap.
     float front=1-smoothstep(.590,.700,p.y);
     c.a*=_BackHair>.5?(1-front)/max(.001,1-c.a*front):front;
    }
    if(_LayerMode>2.5)
    {
     // The opening follows the same inverse lip edit as the base painting.
     // A beard may touch the lip edge but must not paint over the lip itself.
     float lip=1-smoothstep(.65,1,length((featurePoint-_Mouth.xy)/(_Mouth.zw*float2(.058/.089,.010/.033))));
     c.a*=1-lip;
     // Feather only the growth line into the skin. The former rigid cheek
     // spikes came from a frontal source; long free ends retain their alpha.
     float side=smoothstep(.055,.14,abs(featurePoint.x-_Mouth.x));
     float rootY=lerp(_Mouth.y-.026,_Mouth.y-.016,side);
     c.a*=lerp(smoothstep(rootY-.007,rootY+.009,featurePoint.y),1,_FullBeard);
     // The beard atlas has a dense, light rim. Soften that source boundary
     // in texels rather than screen pixels: large and small portraits must
     // share the same roots, and thin free strands must not be hard-clipped.
     float2 edgeTexel=_FullBeard>.5?_AtlasRect.zw*.020:_MainTex_TexelSize.xy*3;
     float coverage=(tex2D(_MainTex,uv+float2(edgeTexel.x,0)).a+tex2D(_MainTex,uv-float2(edgeTexel.x,0)).a
                    +tex2D(_MainTex,uv+float2(0,edgeTexel.y)).a+tex2D(_MainTex,uv-float2(0,edgeTexel.y)).a)*.25;
     c.a*=lerp(.18,1,smoothstep(.10,.97,coverage));
    }
    c*=i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
