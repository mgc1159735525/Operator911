using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        string briefingTitle,briefingBody,briefingReturnModal,briefingReturnFocus;
        int briefingReturnTab;

        // Explanations are read-only and return to the exact decision that opened them.
        void OpenBriefing(string title,string body)
        {
            if(modal=="commandBriefing")return;
            briefingTitle=title;briefingBody=body;briefingReturnModal=modal;briefingReturnTab=commandTab;
            var focus=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
            briefingReturnFocus=focus==null?null:focus.name;
            scrollPositions.Remove("BriefingBody");modal="commandBriefing";Draw();
        }
        void CloseBriefing()
        {
            modal=briefingReturnModal??"";commandTab=briefingReturnTab;Draw();
            var control=root.GetComponentsInChildren<Selectable>().FirstOrDefault(x=>x.name==briefingReturnFocus&&x.IsInteractable());
            if(control!=null&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(control.gameObject);
        }
        bool HandleBriefingKeyboard()
        {
            if(modal!="commandBriefing")return false;
            if(Input.GetKeyDown(KeyCode.Escape))HandleCommandModalEscape();
            // F5/F9 must not replace the decision being inspected behind this layer.
            return true;
        }
        void DrawCommandBriefing()
        {
            var p=CommandModal(briefingTitle,600);
            var content=Scroll(p,"BriefingBody",32,106,836,376,376);
            var body=Label(content,briefingBody,0,0,802,376,21,TextColor);
            float height=Mathf.Max(376,body.preferredHeight+16);
            body.rectTransform.sizeDelta=new Vector2(802,height);content.sizeDelta=new Vector2(content.sizeDelta.x,height);
            Rule(p,32,501,836);
            IconButton("BriefingClose",p,"close","返回",32,526,180,46,CloseBriefing);
            Label(p,"Esc 返回 · 查看不耗时",540,537,328,25,16,Muted,TextAnchor.MiddleRight);
        }
        Button IconButton(string name,Transform parent,string icon,string text,float x,float y,float w,float h,Action action,bool enabled=true,bool selected=false)
        {
            var button=Button(name,parent,text,x,y,w,h,action,enabled,selected);
            var label=button.GetComponentInChildren<Text>();
            label.rectTransform.anchoredPosition=new Vector2(42,0);label.rectTransform.sizeDelta=new Vector2(w-52,h);
            label.alignment=TextAnchor.MiddleLeft;
            NoirIcon(button.transform,icon,13,(h-20)/2,20,label.color);
            return button;
        }
        void CompactMetric(Transform parent,string icon,string title,string value,float x,float y,float w,Color color)
        {
            NoirIcon(parent,icon,x,y+2,18,Muted);
            Label(parent,title,x+27,y,w-27,23,16,Muted);
            Label(parent,value,x+27,y+29,w-27,29,22,color);
        }
    }
}
