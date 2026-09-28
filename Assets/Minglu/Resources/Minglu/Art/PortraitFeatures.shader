Shader "Minglu/PortraitFeatures"
{
 Properties
 {
  [PerRendererData] _MainTex("Atlas",2D)="white"{}
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
   sampler2D _MainTex,_HairMask;fixed4 _Color,_HairShadow,_HairLight;float4 _NeckFit,_Crown,_ClipRect,_HeadRect,_FeatureCols,_Landmarks,_FrontFade,_HairMap,_HairBounds,_Exclude0,_Exclude1,_Exclude2;float _EarCol,_Sex,_HairMode,_NeckMode;
   output vert(input v){output o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float2 crownUV(float2 uv)
   {
    float hairlineBand=_Crown.w>0?1-smoothstep(0,_Crown.w,abs(uv.y-_Crown.x)):0;
    if(_Crown.y>0 && uv.y>_Crown.x)uv.y=_Crown.x+(uv.y-_Crown.x)/_Crown.y;
    // Reveal a little more forehead without translating the crown or lower locks.
    uv.y-=_Crown.z*hairlineBand;
    return uv;
   }
   float2 atlas(float2 p,float col){return float2(col*.25+.001+p.x*_HeadRect.z,_HeadRect.y+(1-p.y)*_HeadRect.w);}
   float excluded(float2 uv,float4 bounds){return step(bounds.x,uv.x)*step(uv.x,bounds.z)*step(bounds.y,uv.y)*step(uv.y,bounds.w);}
   float2 morph(float2 p,float2 center,float2 radius,float2 size,float tilt)
   {
    float2 delta=p-center;
    float weight=1-smoothstep(.15,1,length(delta/radius));
    float2 transformed=delta/size;
    transformed.y+=delta.x*tilt;
    return (transformed-delta)*weight;
   }
   float2 shape(float option,float2 a,float2 b,float2 c)
   {return option<.5?float2(1,1):option<1.5?a:option<2.5?b:c;}   fixed4 frag(output i):SV_Target
   {
    float2 sampleUV=_HairMode>.5?crownUV(i.uv):i.uv;
    fixed4 c=tex2D(_MainTex,sampleUV);
    if(_HairMode>.5&&_Crown.y>0)c.a*=excluded(sampleUV,_HairBounds);
    if(_NeckMode>.5)
    {
     float2 n=float2((i.uv.x-_HeadRect.x)/_HeadRect.z,1-(i.uv.y-_HeadRect.y)/_HeadRect.w);
     // Anatomical sides, not the vertical edges of the atlas crop. Expansion stays under the collar.
     float halfWidth=lerp(.325,.48,smoothstep(.10,.65,n.y));
     c.a*=1-smoothstep(halfWidth-.022,halfWidth,abs(n.x-.5));
    }
    else if(_HairMode>.5){c.rgb=lerp(_HairShadow.rgb,_HairLight.rgb,saturate(dot(c.rgb,float3(.299,.587,.114))*2.2));c.a*=1-saturate(excluded(i.uv,_Exclude0)+excluded(i.uv,_Exclude1)+excluded(i.uv,_Exclude2));}
    else
    {
    float2 p=float2((i.uv.x-_HeadRect.x)/_HeadRect.z,1-(i.uv.y-_HeadRect.y)/_HeadRect.w);
    float x=_Landmarks.x,eyes=_Landmarks.y,brows=_Landmarks.z;
    float2 q=p;
    float2 eyeSize=shape(_FeatureCols.x,float2(1.10,.86),float2(.97,1.16),float2(1.13,1.05));
    q+=morph(p,float2(x-.109,eyes),float2(.112,.060),eyeSize,0);
    q+=morph(p,float2(x+.109,eyes),float2(.112,.060),eyeSize,0);
    float2 browSize=shape(_FeatureCols.y,float2(1,.84),float2(1,1),float2(1.13,1));
    float slope=_FeatureCols.y>1.5&&_FeatureCols.y<2.5?.12:0;
    q+=morph(p,float2(x-.113,brows),float2(.125,.045),browSize,slope);
    q+=morph(p,float2(x+.113,brows),float2(.125,.045),browSize,-slope);
    q+=morph(p,float2(x,lerp(.627,.602,_Sex)),float2(.120,.112),shape(_FeatureCols.z,float2(.84,1),float2(1.16,1),float2(.96,1.10)),0);
    q+=morph(p,float2(x,_Landmarks.w),float2(.142,.068),shape(_FeatureCols.w,float2(.87,1),float2(1.12,1),float2(1,1.18)),0);
    float2 earSize=shape(_EarCol,float2(.87,1),float2(1.12,1),float2(1,1.12));
    q+=morph(p,float2(x-.27,.61),float2(.085,.16),earSize,0);
    q+=morph(p,float2(x+.27,.61),float2(.085,.16),earSize,0);
    q=clamp(q,.002,.998);
    c=tex2D(_MainTex,float2(_HeadRect.x+q.x*_HeadRect.z,_HeadRect.y+(1-q.y)*_HeadRect.w));
    // Keep scalp inside the registered hair silhouette, without removing the forehead or ears.
    float2 hairUV=crownUV(_HairMap.xy+p*_HairMap.zw);
    float cap=tex2D(_HairMask,hairUV).a*excluded(hairUV,_HairBounds);
    float covered=max(1-smoothstep(.23,.26,p.y),smoothstep(.16,.23,abs(p.x-x))*(1-smoothstep(.40,.46,p.y)));
    c.a*=lerp(1,cap,covered);
    // Remove the old flared neck stump before blending onto the continuous neck layer.
    float neckSide=1-smoothstep(_NeckFit.y-.018,_NeckFit.y,abs(p.x-_NeckFit.x));
    c.a*=lerp(1,neckSide,smoothstep(.82,.87,p.y));
    c.a*=1-smoothstep(.87,.97,p.y);
    }
    if(_HairMode>.5&&_FrontFade.y>0)
    {
     float front=smoothstep(_FrontFade.x,_FrontFade.y,i.uv.y);
     // Back and front are complementary, with alpha-over compensation. Drawing
     // the complete rear sprite again darkens every transparent strand twice.
     c.a*=_FrontFade.z>.5?(1-front)/max(.001,1-c.a*front):front;
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



