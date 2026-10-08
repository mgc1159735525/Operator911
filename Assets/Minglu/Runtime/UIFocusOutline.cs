using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Minglu
{
    public class UIFocusOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public void OnSelect(BaseEventData data)
        {
            var control=GetComponent<Selectable>();
            if(control==null||!control.IsActive()||!control.IsInteractable())return;
            var outline=GetComponent<Outline>();if(outline!=null)outline.enabled=true;
            RevealInScrollViews();
        }
        public void OnDeselect(BaseEventData data)
        {var outline=GetComponent<Outline>();if(outline!=null)outline.enabled=false;}
        static Bounds RectBounds(RectTransform space,RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var bounds=new Bounds(space.InverseTransformPoint(corners[0]),Vector3.zero);
            for(int i=1;i<4;i++)bounds.Encapsulate(space.InverseTransformPoint(corners[i]));
            return bounds;
        }
        void RevealInScrollViews()
        {
            var target=transform as RectTransform;if(target==null)return;
            Canvas.ForceUpdateCanvases();
            // Unity Selectable navigation can target clipped controls. Move the
            // actual owning viewport with the focus; a visible outline alone
            // must never allow Submit to choose an unseen list entry.
            foreach(var scroll in GetComponentsInParent<ScrollRect>())
            {
                if(!scroll.isActiveAndEnabled||scroll.viewport==null||scroll.content==null||!transform.IsChildOf(scroll.content))continue;
                var view=scroll.viewport.rect;var item=RectBounds(scroll.viewport,target);
                var content=RectBounds(scroll.viewport,scroll.content);bool moved=false;
                float vertical=content.size.y-view.height,horizontal=content.size.x-view.width;
                // Recenter a clipped selection where possible so its nearby
                // name and decision information also enter the reading area.
                if(scroll.vertical&&vertical>.5f&&(item.min.y<view.yMin+6||item.max.y>view.yMax-6))
                {scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-(view.center.y-item.center.y)/vertical);moved=true;}
                if(scroll.horizontal&&horizontal>.5f&&(item.min.x<view.xMin+6||item.max.x>view.xMax-6))
                {scroll.horizontalNormalizedPosition=Mathf.Clamp01(scroll.horizontalNormalizedPosition-(view.center.x-item.center.x)/horizontal);moved=true;}
                if(moved){scroll.StopMovement();Canvas.ForceUpdateCanvases();}
            }
        }
    }
}
