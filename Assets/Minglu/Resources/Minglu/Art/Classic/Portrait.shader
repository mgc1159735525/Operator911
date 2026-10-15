Shader "Minglu/ClassicPortrait"
{
 Properties {
  [PerRendererData] _MainTex("Atlas",2D)="white"{} _Color("Color",Color)=(1,1,1,1)
  _BaseTex("Head skin",2D)="white"{} _BaseUV("Head UV",Vector)=(0,0,1,1) _FeatureUV("Feature UV",Vector)=(0,0,1,1) _FeatureTarget("Feature position",Vector)=(0,0,1,1)
  _FeatureRotation("Feature pivot and rotation",Vector)=(0,0,1,0)
  _Skin("Skin",Color)=(1,1,1,1) _Cloth("Cloth",Color)=(1,1,1,1) _FeatureSkin("Source skin reference",Color)=(.82,.66,.58,1)
  _Mode("Mode",Float)=0 _HairIndex("Hair",Float)=0 _SourceWhite("White source",Float)=0 _Cap("Cap",Float)=0 _HeadTopTrim("Head top gutter",Float)=0 _HeadLeftTrim("Head left gutter",Float)=0
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
   sampler2D _MainTex,_BaseTex;fixed4 _Color,_Skin,_Cloth,_FeatureSkin;float4 _ClipRect,_BaseUV,_FeatureUV,_FeatureTarget,_FeatureRotation,_MainTex_TexelSize;float _Mode,_HairIndex,_SourceWhite,_Cap,_HeadTopTrim,_HeadLeftTrim;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float3 Hair(float l,float whiteSource){float t=saturate(whiteSource>.5?(l-.15)/.75:(l-.025)*2.6);if(_HairIndex<.5)return lerp(float3(.045,.039,.032),float3(.31,.28,.23),t);if(_HairIndex<1.5)return lerp(float3(.08,.047,.029),float3(.44,.31,.19),t);if(_HairIndex<2.5)return lerp(float3(.18,.19,.18),float3(.7,.7,.65),t);return lerp(float3(.39,.41,.39),float3(.97,.96,.89),t);}
   // Brow and beard originals are mid-tone brown, unlike scalp or white hair.
   // Keep their painted strand contrast instead of crushing it into one fill.
   float3 FacialHair(float l,float whiteSource){float t=saturate(whiteSource>.5?(l-.36)/.50:(l-.18)/.42);t=smoothstep(0,1,t);if(_HairIndex<.5)return lerp(float3(.05,.044,.036),float3(.28,.24,.185),t);if(_HairIndex<1.5)return lerp(float3(.10,.06,.034),float3(.43,.29,.17),t);float silver=saturate(whiteSource>.5?(l-.30)/.65:(l-.12)/.78);if(_HairIndex<2.5)return lerp(float3(.22,.215,.195),float3(.68,.66,.61),silver);return lerp(float3(.46,.445,.405),float3(.94,.92,.86),silver);}
   float Feather(float2 p,float2 centre,float2 radius){return 1-smoothstep(.60,1.12,length((p-centre)/radius));}
   float HeadSkinWeight(float3 colour){float light=dot(colour,float3(.299,.587,.114));return smoothstep(.035,.085,colour.r-colour.g)*smoothstep(.09,.16,colour.r-colour.b)*smoothstep(.16,.31,light);}
   float SkinEdge(float2 uv){
     // Feather the authored alpha contour, including opaque skin cut-outs.
     // Work in source texels so editor zoom and calibration scaling agree.
     float2 d=_MainTex_TexelSize.xy*3;
     float edge=tex2D(_MainTex,uv).a;
     edge=min(edge,tex2D(_MainTex,uv+float2(d.x,0)).a);edge=min(edge,tex2D(_MainTex,uv-float2(d.x,0)).a);
     edge=min(edge,tex2D(_MainTex,uv+float2(0,d.y)).a);edge=min(edge,tex2D(_MainTex,uv-float2(0,d.y)).a);
     return smoothstep(.08,.92,edge);
   }
   fixed4 frag(v2f i):SV_Target {
    float2 placementUV=i.uv;
    if(_Mode>4.5&&_Mode<5.5){
      // Keep the root near the inner eye corners while the lower nose
      // projects towards the head's turn. Moving a whole frontal crop
      // cannot establish this relationship.
      float2 shape=(i.uv-_FeatureUV.xy)/_FeatureUV.zw;
      float turn=smoothstep(.10,.80,1-shape.y);
      i.uv.x+=lerp(-.01,.055,turn)*_FeatureUV.z;
    }
    fixed4 c=tex2D(_MainTex,i.uv);float l=dot(c.rgb,float3(.299,.587,.114));
    c.a=saturate((c.a-.055)/.945);
    if(_Mode<.5||_Mode>7.5){
      // Warm white hair is not skin. Require both red/green and red/blue
      // separation, otherwise palette changes leave noisy undyed strands.
      float skin=HeadSkinWeight(c.rgb);
      float2 cell=saturate((i.uv-_BaseUV.xy)/_BaseUV.zw);cell.y=1-cell.y;
      c.a*=smoothstep(.018,.042,cell.x)*(1-smoothstep(.975,.993,cell.x));
      if(_Mode<.5){
        if(_HeadTopTrim>0)c.a*=smoothstep(_HeadTopTrim-.0014,_HeadTopTrim+.0014,cell.y);
        if(_HeadLeftTrim>0)c.a*=smoothstep(_HeadLeftTrim-.0055,_HeadLeftTrim+.0055,cell.x);
        float neckBand=smoothstep(.28,.34,cell.x)*(1-smoothstep(.78,.82,cell.x));
        float neckSkin=smoothstep(.015,.05,c.r-c.g)*smoothstep(.06,.12,c.r-c.b);
        float neckEdge=max(smoothstep(.65,.73,cell.x),1-smoothstep(.32,.40,cell.x));
        c.a*=1-neckBand*neckSkin*smoothstep(.87-.04*neckEdge,.96-.04*neckEdge,cell.y);
      }
      float hairArea=max(1-smoothstep(.34,.41,cell.y),max(1-smoothstep(.23,.27,cell.x),smoothstep(.63,.68,cell.x)));
      float cap=(_Cap>.5&&cell.y<.49)?1:0;float3 hair=Hair(l,_SourceWhite);
      c.rgb=lerp(c.rgb*lerp(float3(1,1,1),_Skin.rgb,skin),hair,hairArea*(1-skin)*(1-cap));
    }
    // Eye art has its own transparent 4 x 4 atlas, with no painted brows.
    // Its authored alpha preserves the lids; the retired 8 x 8 eyebrow
    // suppression mask would slice this new sheet at the wrong positions.
    else if(_Mode<1.5){float skin=smoothstep(.015,.07,c.r-c.g);c.rgb=lerp(c.rgb,c.rgb*_Skin.rgb,skin);}
    else if(_Mode<2.5){float2 p=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));float mask=max(Feather(p,float2(.24,.47),float2(.26,.12)),Feather(p,float2(.75,.46),float2(.26,.12)));float elder=step(.87,frac(i.uv.x));c.a*=mask*(1-smoothstep(.50+elder*.20,.77+elder*.20,l))*.90;c.rgb=FacialHair(l,elder);}
    else if(_Mode<3.5){float3 neutral=lerp(float3(l,l,l),c.rgb,.28);c.rgb=neutral*_Cloth.rgb*1.46;}
    else if(_Mode>4.5&&_Mode<6.5){
      float2 p=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));
      // A single broad ellipse leaves a hard strip down the bridge yet
      // erases its root. Feather the narrow bridge separately from the
      // wider nasal wings, so the root can meet the inner eye sockets.
      float bridge=Feather(p,float2(.50,.36),float2(.25,.43));
      float wings=Feather(p,float2(.50,.76),float2(.39,.25));
      float mask=_Mode<5.5?max(bridge,wings)*smoothstep(.025,.23,p.y):Feather(p,float2(.50,.51),float2(.46,.24));
      if(_Mode<5.5){
        // Female nose artwork ends higher inside its atlas cell. Exclude
        // the skin pad below each columella; it otherwise forms a pale arc.
        float female=step(.5,1-i.uv.y);
        mask*=1-smoothstep(lerp(.84,.77,female),lerp(.94,.88,female),p.y);
      }
      c.a*=mask;float skin=smoothstep(.015,.07,c.r-c.g);c.rgb=lerp(c.rgb,c.rgb*_Skin.rgb,skin);
    }
    else if(_Mode>6.5){c.rgb=FacialHair(l,0);c.a*=lerp(.82,1,smoothstep(.16,.40,l));}
    if((_Mode>.5&&_Mode<1.5)||(_Mode>4.5&&_Mode<6.5)){
      // Skin is sampled at the displayed point, not the deformed source.
      float2 local=(placementUV-_FeatureUV.xy)/_FeatureUV.zw;local.y=1-local.y;
      float2 head=_FeatureTarget.xy+local*_FeatureTarget.zw;
      float2 relative=head-_FeatureRotation.xy;
      head=_FeatureRotation.xy+float2(relative.x*_FeatureRotation.z-relative.y*_FeatureRotation.w,relative.x*_FeatureRotation.w+relative.y*_FeatureRotation.z);
      float3 underlying=tex2D(_BaseTex,_BaseUV.xy+float2(head.x,1-head.y)*_BaseUV.zw).rgb;
      float3 source=tex2D(_MainTex,i.uv).rgb;
      float warm=source.r-source.g;
      float skin=smoothstep(.008,.035,warm)*smoothstep(.18,.34,l);
      if(_Mode<1.5){
        // Each eye is a separate half crop. Protect the sclera, iris and
        // eyelid opening by anatomy, not warmth (painted whites are warm).
        float2 eye=(i.uv-_FeatureUV.xy)/_FeatureUV.zw;eye.y=1-eye.y;
        float core=1-smoothstep(.76,1.15,length((eye-float2(.40,.46))/float2(.39,.235)));
        skin*=1-core;
      }
      else if(_Mode<5.5)skin=smoothstep(.10,.28,l);
      if(_Mode>5.5){
        // Warm surrounding skin is not lip pigment. Restrict protection
        // to the painted lips, otherwise elder mouth pads stay pale.
        float2 mouth=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));
        float lips=1-smoothstep(.76,1.12,length((mouth-float2(.50,.51))/float2(.43,.12)));
        skin=smoothstep(.16,.32,l)*(1-lips);
      }
      float edge=SkinEdge(i.uv);
      c.a*=lerp(1,edge,skin);
      // A fixed RGB gain leaves each sprite's warm/bright skin pad visible.
      // Use the displayed face's chroma and the SAME palette mask as the
      // head. Keep bounded neutral relief, fading it at the cut-out edge.
      // Dark iris/lash/nostril pixels and red lip pigment retain source art.
      float3 face=underlying*lerp(float3(1,1,1),_Skin.rgb,HeadSkinWeight(underlying));
      float reference=dot(_FeatureSkin.rgb,float3(.299,.587,.114));
      float relief=clamp((l-reference)/max(reference,.1),-.55,.30);
      relief*=(_Mode<1.5?.30:(_Mode<5.5?1:.45))*edge*smoothstep(.05,.75,c.a);
      if(_Mode>4.5){
        float2 feature=float2(frac(i.uv.x*8),1-frac(i.uv.y*8));
        float noseRelief=max(Feather(feature,float2(.50,.43),float2(.15,.38)),Feather(feature,float2(.50,.69),float2(.29,.21)));
        float mouthRelief=Feather(feature,float2(.50,.51),float2(.32,.13));
        relief*=_Mode<5.5?noseRelief:mouthRelief;
      }
      float3 matched=face*(1+relief);
      c.rgb=lerp(c.rgb,matched,skin);
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
