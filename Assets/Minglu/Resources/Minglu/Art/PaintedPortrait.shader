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
   float4 _ClipRect,_AtlasRect,_LayerRect,_BodyRect,_FeatureCols,_EyeFar,_EyeNear,_BrowFar,_BrowNear,_Nose,_Mouth,_EarFar,_EarNear;
   float _LayerMode,_EarOption,_BackHair,_CapStrayCut;
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
   fixed4 frag(output i):SV_Target
   {
    float2 local=float2((i.uv.x-_AtlasRect.x)/_AtlasRect.z,1-(i.uv.y-_AtlasRect.y)/_AtlasRect.w);
    float2 p=_LayerRect.xy+local*_LayerRect.zw;
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
    q=(q-_LayerRect.xy)/_LayerRect.zw;
    float2 uv=float2(_AtlasRect.x+q.x*_AtlasRect.z,_AtlasRect.y+(1-q.y)*_AtlasRect.w);
    // Bilinear sampling must never borrow pixels from the neighbouring person.
    uv=clamp(uv,_AtlasRect.xy+_MainTex_TexelSize.xy*.75,_AtlasRect.xy+_AtlasRect.zw-_MainTex_TexelSize.xy*.75);
    fixed4 c=tex2D(_MainTex,uv);
    if(_LayerMode<.5)
    {
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
      c.rgb=lerp(_ClothShadow.rgb,_ClothLight.rgb,saturate(luminance*1.45));
      // Clothing is composited around the original painted skin boundary. It
      // cannot grow a pointed collar through the chin or replace the neck.
      // The original pigment edge supplies the antialiased silhouette instead
      // of an invented straight cut or a new disconnected neck patch.
      float2 bodyP=clamp(p,.001,.999);
      float2 bodyUV=float2(_BodyRect.x+bodyP.x*_BodyRect.z,_BodyRect.y+(1-bodyP.y)*_BodyRect.w);
      fixed4 body=tex2D(_BodyTex,bodyUV);
      float neckPigment=smoothstep(.032,.085,body.r-body.b);
      float skinProtection=max(1-smoothstep(.49,.51,p.y),neckPigment)*(1-smoothstep(.775,.805,p.y));
      c.a*=1-body.a*skinProtection;
     }
     else c.rgb=lerp(_HairShadow.rgb,_HairLight.rgb,saturate(luminance*1.65));
    }
    if(_LayerMode>.5&&_LayerMode<1.5)
    {
     // Length behind the collar, upper hair in front of the skull. The two
     // passes are alpha-over compensated to avoid doubled dark edge strands.
     float front=1-smoothstep(.590,.700,p.y);
     c.a*=_BackHair>.5?(1-front)/max(.001,1-c.a*front):front;
     // A few pixels from the adjacent loose-hair cell overlap the cap cell's
     // otherwise empty lower-left margin. Exclude that margin, not the cap.
     float stray=(1-smoothstep(.060,.090,local.x))*smoothstep(.64,.69,local.y);
     c.a*=1-_CapStrayCut*stray;
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
