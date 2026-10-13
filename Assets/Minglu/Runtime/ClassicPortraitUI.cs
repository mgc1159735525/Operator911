using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        ClassicPortraitRenderer classicRenderer;
        void DrawClassicLook(Transform parent, Appearance value, float x, float y, float w, float h, Color tint, bool headOnly=false)
        {
            if(classicRenderer==null) classicRenderer=new ClassicPortraitRenderer();
            classicRenderer.Draw(parent,value,x,y,w,h,tint,headOnly);
        }
        void DisposeClassicPortraits(){if(classicRenderer!=null)classicRenderer.Dispose();classicRenderer=null;}
    }
}
