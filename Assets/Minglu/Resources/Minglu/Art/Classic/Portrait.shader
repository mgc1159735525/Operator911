Shader "Minglu/ClassicPortrait"
{
 Properties {
  [PerRendererData] _MainTex("Atlas",2D)="white"{} _Color("Color",Color)=(1,1,1,1)
  _BaseTex("Head skin",2D)="white"{} _BaseUV("Head UV",Vector)=(0,0,1,1) _FeatureUV("Feature UV",Vector)=(0,0,1,1) _FeatureTarget("Feature position",Vector)=(0,0,1,1)
  _Skin("Skin",Color)=(1,1,1,1) _Cloth("Cloth",Color)=(1,1,1,1)
  _Mode("Mode",Float)=0 _HairIndex("Hair",Float)=0 _SourceWhite("White source",Float)=0 _Cap("Cap",Float)=0
  _StencilComp("Stencil comparison",Float)=8 _Stencil("Stencil",Float)=0 _StencilOp("Stencil operation",Float)=0 _StencilWriteMask("Stencil write mask",Float)=255 _StencilReadMask("Stencil read mask",Float)=255 _ColorMask("Color mask",Float)=15
 }
 SubShader {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
   sampler2D _MainTex,_BaseTex;fixed4 _Color,_Skin,_Cloth;float4 _ClipRect,_BaseUV,_FeatureUV,_FeatureTarget;float _Mode,_HairIndex,_SourceWhite,_Cap;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float3 Hair(float l,float whiteSource){float t=saturate(whiteSource>.5?(l-.15)/.75:(l-.025)*2.6);if(_HairIndex<.5)return lerp(float3(.045,.039,.032),float3(.31,.28,.23),t);if(_HairIndex<1.5)return lerp(float3(.08,.047,.029),float3(.44,.31,.19),t);if(_HairIndex<2.5)return lerp(float3(.18,.19,.18),float3(.7,.7,.65),t);return lerp(float3(.39,.41,.39),float3(.97,.96,.89),t);}
   // Brow and beard originals are mid-tone brown, unlike scalp or white hair.
   // Keep their painted strand contrast instead of crushing it into one fill.
   float3 FacialHair(float l,float whiteSource){float t=saturate(whiteSource>.5?(l-.36)/.50:(l-.18)/.42);t=smoothstep(0,1,t);if(_HairIndex<.5)return lerp(float3(.05,.044,.036),float3(.28,.24,.185),t);if(_HairIndex<1.5)return lerp(float3(.10,.06,.034),float3(.43,.29,.17),t);float silver=saturate(whiteSource>.5?(l-.30)/.65:(l-.12)/.78);if(_HairIndex<2.5)return lerp(float3(.20,.22,.20),float3(.66,.68,.63),silver);return lerp(float3(.40,.42,.38),float3(.94,.94,.87),silver);}
   float Feather(float2 p,float2 centre,float2 radius){return 1-smoothstep(.60,1.12,length((p-centre)/radius));}
   fixed4 frag(v2f i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);float l=dot(c.rgb,float3(.299,.587,.114));
    c.a=saturate((c.a-.055)/.945);
    if(_Mode<.5||_Mode>7.5){float skin=smoothstep(.016,.061,c.r-c.g)*smoothstep(.16,.31,l);float2 cell=saturate((i.uv-_BaseUV.xy)/_BaseUV.zw);cell.y=1-cell.y;c.a*=smoothstep(.018,.042,cell.x)*(1-smoothstep(.975,.993,cell.x));if(_Mode<.5){float neckBand=smoothstep(.28,.34,cell.x)*(1-smoothstep(.66,.72,cell.x));c.a*=1-neckBand*smoothstep(.87,.96,cell.y);}float hairArea=max(1-smoothstep(.34,.41,cell.y),max(1-smoothstep(.23,.27,cell.x),smoothstep(.68,.74,cell.x)));float cap=(_Cap>.5&&cell.y<.49)?1:0;float3 hair=Hair(l,_SourceWhite);c.rgb=lerp(c.rgb*lerp(float3(1,1,1),_Skin.rgb,skin),hair,hairArea*(1-skin)*(1-cap));}
    else if(_Mode<1.5){float2 p=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));float mask=max(Feather(p,float2(.24,.625),float2(.265,.13)),Feather(p,float2(.75,.605),float2(.265,.13)));c.a*=mask;float skin=smoothstep(.015,.07,c.r-c.g);c.rgb=lerp(c.rgb,c.rgb*_Skin.rgb,skin);}
    else if(_Mode<2.5){float2 p=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));float mask=max(Feather(p,float2(.24,.47),float2(.26,.10)),Feather(p,float2(.75,.46),float2(.26,.10)));float elder=step(.87,frac(i.uv.x));c.a*=mask*(1-smoothstep(.50+elder*.20,.77+elder*.20,l))*.90;c.rgb=FacialHair(l,elder);}
    else if(_Mode<3.5){float3 neutral=lerp(float3(l,l,l),c.rgb,.28);c.rgb=neutral*_Cloth.rgb*1.46;}
    else if(_Mode>4.5&&_Mode<6.5){float2 p=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));float mask=_Mode<5.5?Feather(p,float2(.50,.49),float2(.32,.61)):Feather(p,float2(.50,.51),float2(.46,.24));c.a*=mask;float skin=smoothstep(.015,.07,c.r-c.g);c.rgb=lerp(c.rgb,c.rgb*_Skin.rgb,skin);}
    else if(_Mode>6.5){c.rgb=FacialHair(l,0);c.a*=lerp(.82,1,smoothstep(.16,.40,l));}
    if((_Mode>.5&&_Mode<1.5)||(_Mode>4.5&&_Mode<6.5)){
      float2 local=(i.uv-_FeatureUV.xy)/_FeatureUV.zw;local.y=1-local.y;
      float2 head=_FeatureTarget.xy+local*_FeatureTarget.zw;
      float3 underlying=tex2D(_BaseTex,_BaseUV.xy+float2(head.x,1-head.y)*_BaseUV.zw).rgb*_Skin.rgb;
      float warm=tex2D(_MainTex,i.uv).r-tex2D(_MainTex,i.uv).g;
      float skin=smoothstep(.012,.045,warm)*smoothstep(.18,.40,l);
      if(_Mode>5.5)skin*=1-smoothstep(.13,.19,warm);
      // Match skin hue while retaining the source nose/eyelid modelling.
      // The old narrow luminance range erased the volume of the features.
      float delta=l-.67;float shadowContrast=(_Mode>4.5&&_Mode<5.5)?1.35:(_Mode<1.5?.70:.90);float contrast=delta<0?shadowContrast:.35;
      c.rgb=lerp(c.rgb,underlying*clamp(1+delta*contrast,.58,1.045),skin);
    }
    c*=i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    return c;
   }
  ENDCG}
 }
}
