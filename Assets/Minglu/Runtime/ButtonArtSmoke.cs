using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator ButtonArtSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Button art smoke requires an isolated save");
            string[] families={"primary","paper","dark"};
            string[] states={"normal","hover","pressed","selected","disabled"};
            string[] headings={"签发操作","档案操作","深色界面"};
            string[] captions={"签发指令","查看档案","返回目录"};
            string[] rows={"常态","悬停","按下","选中","不可用"};
            var sprites=new Sprite[3,5];
            for(int family=0;family<3;family++)for(int state=0;state<5;state++)
            {
                string resource="Minglu/UI/PrintButtons/button_"+families[family]+"_"+states[state];
                var sprite=Resources.Load<Sprite>(resource);sprites[family,state]=sprite;
                if(sprite==null)throw new Exception("Button asset missing: "+resource);
                if(sprite.border.x<=0||sprite.border.y<=0||sprite.border.z<=0||sprite.border.w<=0)
                    throw new Exception("Button asset has no usable nine-slice border: "+resource);
            }

            // The real input module must not inject the desktop cursor into a deterministic state sheet.
            // Raycasting and event dispatch below still use the actual EventSystem and UGUI handlers.
            var input=EventSystem.current.currentInputModule;
            bool inputWasEnabled=input!=null&&input.enabled;
            if(input!=null)input.enabled=false;
            EventSystem.current.SetSelectedGameObject(null);
            if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);}
            root=Rect("ButtonArtPreview",canvas.transform,0,0,1600,900);
            root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            Box("PreviewBackground",root,0,0,1600,900,Ink);
            Label(root,"明路 · 地下",52,32,650,48,34,Gold);
            Label(root,"按钮印样",52,84,650,30,19,Muted);
            Label(root,"三类按钮  /  五种状态",1100,55,448,30,18,Muted,TextAnchor.MiddleRight);
            Rule(root,52,133,1496,Line);

            var buttons=new Button[3,5];
            int[] clicks={0,0,0};
            for(int family=0;family<3;family++)
            {
                int capturedFamily=family;
                float x=230+family*438;
                var surface=Box("ButtonSpecimenSurface_"+family,root,x,156,402,654,family==1?Paper:Panel);
                SurfaceTheme(surface,family==1);
                Label(surface,headings[family],32,23,338,34,25,Gold);
                Rule(surface,32,75,338);
                for(int state=0;state<5;state++)
                {
                    string name=family==0?"CommandAuthorize":"ButtonSpecimen_"+families[family]+"_"+states[state];
                    var b=PrintButton(name,surface,captions[family],32,110+state*103,338,62,()=>clicks[capturedFamily]++,state!=4,state==3);
                    buttons[family,state]=b;
                    if(b.transition!=Selectable.Transition.SpriteSwap||b.image.type!=Image.Type.Sliced)
                        throw new Exception("Button did not use sliced artwork: "+name);
                    AssertButtonArt(b,sprites[family,state==3?3:state==4?4:0],"initial "+families[family]+" "+states[state]);
                    if(b.GetComponentsInChildren<Text>().Length!=1)throw new Exception("Button label must remain a separate Text component");
                }
            }
            for(int row=0;row<5;row++)Label(root,rows[row],52,275+row*103,150,42,21,Gold,TextAnchor.MiddleLeft);
            Rule(root,52,835,1496,Line);
            Label(root,"暖纸、墨色与铁锈红。文字独立排版，边缘随按钮尺寸保留。",52,850,1496,28,17,Muted);
            yield return null;
            Canvas.ForceUpdateCanvases();

            for(int family=0;family<3;family++)
            {
                var button=buttons[family,0];
                var pointer=ButtonArtPointer(button);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                AssertButtonArt(button,sprites[family,1],"hover "+families[family]);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                AssertButtonArt(button,sprites[family,2],"pressed "+families[family]);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                if(clicks[family]!=1)throw new Exception("One pointer click must invoke one callback: "+families[family]);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                EventSystem.current.SetSelectedGameObject(null);
                AssertButtonArt(button,sprites[family,0],"returned normal "+families[family]);

                // Actual keyboard/controller focus must select the focus artwork and clear it on blur.
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                AssertButtonArt(button,sprites[family,3],"focus selected "+families[family]);
                EventSystem.current.SetSelectedGameObject(null);
                AssertButtonArt(button,sprites[family,0],"focus cleared "+families[family]);

                // Persistent choice selection uses the normal sprite, surviving a hover round trip.
                var selected=buttons[family,3];var selectedPointer=ButtonArtPointer(selected);
                AssertButtonArt(selected,sprites[family,3],"persistent selected "+families[family]);
                ExecuteEvents.Execute(selected.gameObject,selectedPointer,ExecuteEvents.pointerEnterHandler);
                AssertButtonArt(selected,sprites[family,1],"persistent hover "+families[family]);
                ExecuteEvents.Execute(selected.gameObject,selectedPointer,ExecuteEvents.pointerExitHandler);
                AssertButtonArt(selected,sprites[family,3],"persistent restored "+families[family]);

                var disabled=buttons[family,4];var disabledPointer=ButtonArtPointer(disabled);
                if(disabled.IsInteractable())throw new Exception("Disabled button is interactable: "+families[family]);
                ExecuteEvents.Execute(disabled.gameObject,disabledPointer,ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(disabled.gameObject,disabledPointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(disabled.gameObject,disabledPointer,ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(disabled.gameObject,disabledPointer,ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(disabled.gameObject,disabledPointer,ExecuteEvents.pointerExitHandler);
                if(clicks[family]!=1)throw new Exception("Disabled button dispatched a callback: "+families[family]);
                AssertButtonArt(disabled,sprites[family,4],"disabled retained "+families[family]);

                // Show hover/press together without stealing focus from another specimen column.
                var hovered=buttons[family,1];var pressed=buttons[family,2];
                var navigation=pressed.navigation;navigation.mode=Navigation.Mode.None;pressed.navigation=navigation;
                ExecuteEvents.Execute(hovered.gameObject,ButtonArtPointer(hovered),ExecuteEvents.pointerEnterHandler);
                var pressPointer=ButtonArtPointer(pressed);
                ExecuteEvents.Execute(pressed.gameObject,pressPointer,ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(pressed.gameObject,pressPointer,ExecuteEvents.pointerDownHandler);
            }
            yield return null;yield return null;yield return new WaitForEndOfFrame();
            for(int family=0;family<3;family++)for(int state=0;state<5;state++)
                AssertButtonArt(buttons[family,state],sprites[family,state],"preview "+families[family]+" "+states[state]);
            Capture(path);
            if(input!=null)input.enabled=inputWasEnabled;
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})
                if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_BUTTON_ART_SUCCESS: 15 imported nine-slice sprites, real UGUI hover/press/focus/persistent-selection/disabled transitions, raycast hit and exactly one click callback per family, actual state-sheet capture");
        }

        PointerEventData ButtonArtPointer(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current)
            {
                position=RectTransformUtility.WorldToScreenPoint(viewCamera,rect.TransformPoint(rect.rect.center)),
                button=PointerEventData.InputButton.Left
            };
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)
                throw new Exception("Button-art specimen is not the frontmost raycast hit: "+button.name);
            return pointer;
        }

        void AssertButtonArt(Button button,Sprite expected,string stage)
        {
            if(button.image.overrideSprite!=expected)
                throw new Exception("Button sprite mismatch at "+stage+": expected "+expected.name+", got "+(button.image.overrideSprite==null?"null":button.image.overrideSprite.name));
        }
    }
}
