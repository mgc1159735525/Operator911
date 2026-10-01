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
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct input {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
   struct output {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
   sampler2D _MainTex,_BodyTex;float4 _MainTex_TexelSize;
   fixed4 _Color,_Complexion,_ClothShadow,_ClothLight,_HairShadow,_HairLight;
   float4 _ClipRect,_AtlasRect,_LayerRect,_DrawRect,_BodyRect,_FeatureCols,_EyeFar,_EyeNear,_BrowFar,_BrowNear,_Nose,_Mouth,_EarFar,_EarNear;
   float4 _HairAnchor0,_HairAnchor1,_HairAnchor2,_HairAnchor3,_CrownAdjust,_BeardMouth,_BeardChin,_BeardScales;
   float4 _CollarAnchor0,_CollarAnchor1,_CollarAnchor2,_CollarAnchor3,_CollarAnchor4,_CollarAnchor5,_CollarAnchor6,_CollarAnchor7,_Jaw;
   float _LayerMode,_EarOption,_BackHair,_CapStrayCut,_OutfitLeftTrim,_FarScalpPad;
   output vert(input v){output o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float2 optionShape(float option,float2 one,float2 two,float2 three)
   {return option<.5?float2(1,1):option<1.5?one:option<2.5?two:three;}
   float2 morph(float2 p,float4 feature,float2 size,float tilt)
   {
    float2 delta=p-feature.xy;
    float weight=1-smoothstep(.12,1,length(delta/feature.zw));
    float2 transformed=delta/size;transformed.y+=delta.x*tilt;
    return (transformed-delta)*weight;
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
    float scalp=(1-smoothstep(_EyeFar.x+.055,_EyeNear.x-.015,p.x))*(1-smoothstep(_BrowFar.y-.055,_BrowFar.y-.010,p.y))*smoothstep(_CrownAdjust.y-.035,_CrownAdjust.y+.035,p.y);
    p.x+=scalp*_FarScalpPad;
    float influence=1-smoothstep(_CrownAdjust.y-.02,_CrownAdjust.y+.03,p.y);
    p=lerp(p,_CrownAdjust.xy+(p-_CrownAdjust.xy)/_CrownAdjust.z,influence);
    float3 blend=hairAnchor(p,_HairAnchor0)+hairAnchor(p,_HairAnchor1)+hairAnchor(p,_HairAnchor2)+hairAnchor(p,_HairAnchor3);
    return (p-blend.xy/blend.z-_LayerRect.xy)/_LayerRect.zw;
   }
   float2 beardSample(float2 p)
   {
    float t=saturate((p.y-_BeardMouth.y)/(_BeardChin.y-_BeardMouth.y));
    float center=lerp(_BeardMouth.x,_BeardChin.x,t),sourceCenter=lerp(_BeardMouth.z,_BeardChin.z,t);
    float sy=p.y<_BeardMouth.y?_BeardMouth.w+(p.y-_BeardMouth.y)/_BeardScales.z:p.y>_BeardChin.y?_BeardChin.w+(p.y-_BeardChin.y)/_BeardScales.w:lerp(_BeardMouth.w,_BeardChin.w,t);
    float sx=lerp(p.x<center?_BeardScales.x:_BeardScales.y,.57,t);
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
    if(_LayerMode<.5)
    {
     float2 eyes=optionShape(_FeatureCols.x,float2(1.09,.84),float2(.96,1.17),float2(1.12,1.05));
     q+=morph(p,_EyeFar,eyes,0)+morph(p,_EyeNear,eyes,0);
     float2 brows=optionShape(_FeatureCols.y,float2(1,.82),float2(1,1),float2(1.13,1));
     float tilt=_FeatureCols.y>1.5&&_FeatureCols.y<2.5?.11:0;
     q+=morph(p,_BrowFar,brows,tilt*.70)+morph(p,_BrowNear,brows,-tilt);
     q+=morph(p,_Nose,optionShape(_FeatureCols.z,float2(.83,1),float2(1.17,1),float2(.96,1.11)),0);
     q+=morph(p,_Mouth,optionShape(_FeatureCols.w,float2(.85,1),float2(1.13,1),float2(1,1.19)),0);
     float2 ears=optionShape(_EarOption,float2(.84,1),float2(1.15,1),float2(1,1.13));
     q+=morph(p,_EarFar,ears,0)+morph(p,_EarNear,ears,0);
    }
    else if(_LayerMode>2.5)
    {
     // Beard and lip share the same local deformation and full-canvas frame.
     q+=morph(p,_Mouth,optionShape(_FeatureCols.w,float2(.85,1),float2(1.13,1),float2(1,1.19)),0);
    }
    float2 featurePoint=q;
    q=_LayerMode>.5&&_LayerMode<1.5?hairSample(q):_LayerMode>2.5?beardSample(q):_LayerMode>1.5?outfitSample(q):(q-_LayerRect.xy)/_LayerRect.zw;
    float sourceInside=step(0,q.x)*step(q.x,1)*step(0,q.y)*step(q.y,1);
    float2 uv=float2(_AtlasRect.x+q.x*_AtlasRect.z,_AtlasRect.y+(1-q.y)*_AtlasRect.w);
    // Bilinear sampling must never borrow pixels from the neighbouring person.
    uv=clamp(uv,_AtlasRect.xy+_MainTex_TexelSize.xy*.75,_AtlasRect.xy+_AtlasRect.zw-_MainTex_TexelSize.xy*.75);
    fixed4 c=tex2D(_MainTex,uv);c.a*=sourceInside;
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
     else c.rgb=lerp(_HairShadow.rgb,_HairLight.rgb,saturate(luminance*1.42));
    }
    if(_LayerMode>.5&&_LayerMode<1.5)
    {
     // Length behind the collar, upper hair in front of the skull. The two
     // passes are alpha-over compensated to avoid doubled dark edge strands.
     float front=1-smoothstep(.590,.700,p.y);
     c.a*=_BackHair>.5?(1-front)/max(.001,1-c.a*front):front;
     // A few pixels from the adjacent loose-hair cell overlap the cap cell's
     // otherwise empty lower-left margin. Exclude that margin, not the cap.
     float stray=(1-smoothstep(.060,.090,q.x))*smoothstep(.64,.69,q.y);
     c.a*=1-_CapStrayCut*stray;
     // Preserve the painted opening of the near ear. Ear edits and age no
     // longer leave an opaque lock across the concha after hair registration.
     float ear=1-smoothstep(.45,1,length((p-_EarNear.xy)/(_EarNear.zw*float2(.58,.65))));
     c.a*=1-ear;
     // Follow the whole far-side front lock down to the garment. Ending this
     // cut at the jaw leaves a detached black tuft that looks like a beard.
     // The near-side/back mass remains beyond this face-derived boundary.
     float silhouette=max(bodyAt(p).a,bodyAt(p+float2(.09,0)).a);
     // Below the lips this is the same removed front lock even where it
     // hangs outside the body's alpha silhouette. Clear that tail completely
     // rather than leaving a detached strand floating beside the neck.
     silhouette=max(silhouette,smoothstep(_Mouth.y-.020,_Jaw.y-.005,p.y));
     float farBoundary=lerp(_EyeFar.x+.015,_Jaw.x+.10,smoothstep(_Mouth.y-.05,_Jaw.y+.020,p.y));
     float farSide=1-smoothstep(farBoundary,farBoundary+.045,p.x);
     float height=smoothstep(_BrowFar.y-.045,_BrowFar.y+.005,p.y);
     c.a*=1-silhouette*farSide*height;
    }
    if(_LayerMode>2.5)
    {
     // The opening follows the same inverse lip edit as the base painting.
     // A beard may touch the lip edge but must not paint over the lip itself.
     float lip=1-smoothstep(.65,1,length((featurePoint-_Mouth.xy)/float2(.058,.010)));
     c.a*=1-lip;
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
