using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Minglu
{
    public class UIFocusOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public void OnSelect(BaseEventData data){GetComponent<Outline>().enabled=true;}
        public void OnDeselect(BaseEventData data){GetComponent<Outline>().enabled=false;}
    }
}
