using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        Texture2D paintedBases,paintedHair,paintedOutfits,paintedBeards;
        void DrawPaintedLook(Transform parent,Appearance look,float x,float y,float w,float h,Color tint,bool headOnly=false)
        {
            if(paintedBases==null)paintedBases=Resources.Load<Texture2D>("Minglu/Art/Painted/portrait-bases");
            if(paintedHair==null)paintedHair=Resources.Load<Texture2D>("Minglu/Art/Painted/hair-styles");
            if(paintedOutfits==null)paintedOutfits=Resources.Load<Texture2D>("Minglu/Art/Painted/outfit-overlays");
            if(paintedBeards==null)paintedBeards=Resources.Load<Texture2D>("Minglu/Art/Painted/beard-styles");
            var bg=Hex(new[]{"DDD0AB","A9B4B4","B8A186","E8DDC5"}[look.parts[9]])*tint;
            var frame=Box("PaintedPortrait",parent,x,y,w,h,bg);frame.GetComponent<Image>().raycastTarget=false;frame.gameObject.AddComponent<RectMask2D>();
            if(paintedBases==null||paintedHair==null||paintedOutfits==null||paintedBeards==null||Resources.Load<Shader>("Minglu/Art/PaintedPortrait")==null)
            {
                Label(frame,"绘制立绘资源缺失",4,4,w-8,h-8,Mathf.Min(15,Mathf.RoundToInt(w/7)),Red,TextAnchor.MiddleCenter);return;
            }
            var placement=PaintedPortraitLayout.Placement(w,h,headOnly||w<115);
            PaintedLayer(frame,look,paintedHair,PaintedPortraitLayout.Hair(look),placement,PaintedPortraitLayer.Hair,tint,true);
            PaintedLayer(frame,look,paintedBases,PaintedPortraitLayout.Base(look),placement,PaintedPortraitLayer.Base,tint);
            PaintedLayer(frame,look,paintedOutfits,PaintedPortraitLayout.Outfit(look),placement,PaintedPortraitLayer.Outfit,tint);
            PaintedLayer(frame,look,paintedHair,PaintedPortraitLayout.Hair(look),placement,PaintedPortraitLayer.Hair,tint);
            if(look.sex==0&&look.parts[6]>0)PaintedLayer(frame,look,paintedBeards,PaintedPortraitLayout.Beard(look),placement,PaintedPortraitLayer.Beard,tint);
        }
        void PaintedLayer(Transform parent,Appearance look,Texture2D atlas,UnityEngine.Rect cell,UnityEngine.Rect placement,PaintedPortraitLayer mode,Color tint,bool backHair=false)
        {
            var registration=mode==PaintedPortraitLayer.Hair?PaintedPortraitLayout.HairRect(look):mode==PaintedPortraitLayer.Outfit?PaintedPortraitLayout.OutfitRect(look):mode==PaintedPortraitLayer.Beard?PaintedPortraitLayout.BeardRect(look):new UnityEngine.Rect(0,0,1,1);
            var draw=PaintedPortraitLayout.DrawRect(look,mode);
            var registered=PaintedPortraitLayout.Register(placement,draw);
            var layer=Rect("Painted"+mode+(backHair?"Back":""),parent,registered.x,registered.y,registered.width,registered.height).gameObject.AddComponent<PaintedPortraitGraphic>();
            layer.color=tint;layer.Configure(atlas,paintedBases,cell,registration,draw,look,mode,backHair);
        }
    }
}
