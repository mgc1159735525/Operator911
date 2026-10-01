using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public enum PaintedPortraitLayer { Base, Hair, Outfit, Beard }

    public sealed class PaintedPortraitGraphic:RawImage
    {
        Material owned;
        public void Configure(Texture2D atlas,Texture2D body,Rect cell,Rect registration,Rect draw,Appearance look,PaintedPortraitLayer layer,bool backHair=false)
        {
            var shader=Resources.Load<Shader>("Minglu/Art/PaintedPortrait");
            if(shader==null)throw new System.InvalidOperationException("缺少绘制人物着色器。");
            texture=atlas;uvRect=cell;raycastTarget=false;
            owned=new Material(shader);
            owned.SetVector("_AtlasRect",new Vector4(cell.x,cell.y,cell.width,cell.height));
            owned.SetVector("_LayerRect",new Vector4(registration.x,registration.y,registration.width,registration.height));
            owned.SetVector("_DrawRect",new Vector4(draw.x,draw.y,draw.width,draw.height));
            for(int i=0;i<4;i++)owned.SetVector("_HairAnchor"+i,PaintedPortraitLayout.HairBinding(look,i));
            owned.SetVector("_CrownAdjust",PaintedPortraitLayout.CrownAdjustment(look));
            owned.SetVector("_BeardMouth",PaintedPortraitLayout.BeardMouth(look));
            owned.SetVector("_BeardChin",PaintedPortraitLayout.BeardChin(look));
            owned.SetVector("_BeardScales",PaintedPortraitLayout.BeardScales(look));
            var bodyCell=PaintedPortraitLayout.Base(look);
            owned.SetTexture("_BodyTex",body);owned.SetVector("_BodyRect",new Vector4(bodyCell.x,bodyCell.y,bodyCell.width,bodyCell.height));
            owned.SetFloat("_CapStrayCut",look.sex==0&&look.parts[1]==3?1:0);
            owned.SetFloat("_OutfitLeftTrim",look.parts[7]%4==1?1:0);
            owned.SetFloat("_LayerMode",(int)layer);owned.SetFloat("_BackHair",backHair?1:0);
            owned.SetVector("_FeatureCols",new Vector4(look.parts[2],look.parts[3],look.parts[4],look.parts[5]));
            owned.SetFloat("_EarOption",look.ears);
            var landmarks=PaintedPortraitLayout.Landmarks(look);
            owned.SetVector("_EyeFar",landmarks.eyeFar);owned.SetVector("_EyeNear",landmarks.eyeNear);
            owned.SetVector("_BrowFar",landmarks.browFar);owned.SetVector("_BrowNear",landmarks.browNear);
            owned.SetVector("_Nose",landmarks.nose);owned.SetVector("_Mouth",landmarks.mouth);
            owned.SetVector("_EarFar",landmarks.earFar);owned.SetVector("_EarNear",landmarks.earNear);
            owned.SetColor("_Complexion",new[]{Color.white,new Color(1,.95f,.87f),new Color(.88f,.77f,.64f),new Color(.73f,.60f,.49f)}[look.parts[10]]);
            var cloth=new[]{new Color(.43f,.50f,.59f),new Color(.40f,.54f,.44f),new Color(.69f,.40f,.31f),new Color(.71f,.58f,.35f),new Color(.46f,.47f,.48f),new Color(.54f,.40f,.59f)}[look.parts[11]];
            owned.SetColor("_ClothShadow",cloth*.30f);owned.SetColor("_ClothLight",Color.Lerp(cloth,Color.white,.25f));
            owned.SetColor("_HairShadow",new[]{new Color(.060f,.052f,.044f),new Color(.100f,.055f,.035f),new Color(.21f,.21f,.20f),new Color(.36f,.36f,.34f)}[look.parts[12]]);
            owned.SetColor("_HairLight",new[]{new Color(.42f,.37f,.31f),new Color(.54f,.37f,.23f),new Color(.74f,.74f,.70f),new Color(.95f,.94f,.89f)}[look.parts[12]]);
            material=owned;
        }
        protected override void OnDestroy(){base.OnDestroy();if(owned!=null)Destroy(owned);}
    }
}
