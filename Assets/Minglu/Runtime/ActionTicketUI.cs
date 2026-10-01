using System;
using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    public partial class CityGameUI
    {
        // Action and cost are separate text fields, so localised labels do not push the price around.
        Button ActionTicket(string name,Transform parent,string title,string cost,float x,float y,float w,float h,Action action,bool enabled=true)
        {
            var button=Button(name,parent,title,x,y,w,h,action,enabled);
            float costWidth=Mathf.Min(144,w*.34f);
            var titleLabel=button.GetComponentInChildren<Text>();
            titleLabel.alignment=TextAnchor.MiddleLeft;
            titleLabel.rectTransform.anchoredPosition=new Vector2(16,0);
            titleLabel.rectTransform.sizeDelta=new Vector2(w-costWidth-44,h);
            var line=Box("ActionCostDivider",button.transform,w-costWidth-20,10,1,h-20,PaperSurface(parent)?PaperLine:Line);
            line.GetComponent<Image>().raycastTarget=false;
            var amount=Label(button.transform,cost,w-costWidth-8,0,costWidth-8,h,16,enabled?TextColor:titleLabel.color,TextAnchor.MiddleRight);
            if(RedAction(name))
            {
                amount.color=titleLabel.color;
                var divider=titleLabel.color;divider.a=.35f;line.GetComponent<Image>().color=divider;
            }
            amount.name="ActionCost";return button;
        }
    }
}
