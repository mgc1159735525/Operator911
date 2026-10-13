using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Minglu.EditorTools
{
    // A separate preview scene contains only a camera and the shared compositor.
    // No CityGameUI, game bootstrap, player profile or save file is instantiated.
    public sealed class ClassicPortraitPreview : IDisposable
    {
        Scene scene;
        Camera camera;
        GameObject canvasObject;
        RectTransform frame;
        RenderTexture target;
        ClassicPortraitRenderer renderer;
        public ClassicPortraitPreview()
        {
            scene=EditorSceneManager.NewPreviewScene();
            var cameraObject=new GameObject("Portrait calibration camera",typeof(Camera)){hideFlags=HideFlags.HideAndDontSave};
            SceneManager.MoveGameObjectToScene(cameraObject,scene);camera=cameraObject.GetComponent<Camera>();camera.scene=scene;
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=300;camera.nearClipPlane=.1f;camera.farClipPlane=2000;
            camera.transform.position=new Vector3(0,0,-1000);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.87f,.84f,.74f,1);
            canvasObject=new GameObject("Portrait calibration canvas",typeof(RectTransform),typeof(Canvas)){hideFlags=HideFlags.HideAndDontSave};
            SceneManager.MoveGameObjectToScene(canvasObject,scene);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rt=(RectTransform)canvasObject.transform;rt.sizeDelta=new Vector2(400,600);rt.pivot=new Vector2(.5f,.5f);rt.position=Vector3.zero;
        }
        T WithTable<T>(List<PortraitCalibrationRow> rows,Func<T> action)
        {
            bool hadPreview=PortraitCalibration.HasPreview;var previous=PortraitCalibration.Snapshot();string error;
            if(!PortraitCalibration.SetPreview(rows,out error))throw new ArgumentException(error);
            try{return action();}
            finally{if(hadPreview)PortraitCalibration.SetPreview(previous,out error);else PortraitCalibration.ClearPreview();}
        }
        public RenderTexture Render(Appearance look,List<PortraitCalibrationRow> rows,int width=800,int height=1200)
        {
            return WithTable(rows,()=>
            {
                if(frame!=null)UnityEngine.Object.DestroyImmediate(frame.gameObject);
                if(renderer!=null)renderer.Dispose();renderer=new ClassicPortraitRenderer();
                frame=renderer.Draw(canvasObject.transform,look,0,0,400,600,Color.white);
                width=Mathf.Clamp(width,200,1600);height=Mathf.Clamp(height,300,2400);
                if(target==null||target.width!=width||target.height!=height)
                {
                    if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                    target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Portrait calibration preview",hideFlags=HideFlags.HideAndDontSave,antiAliasing=1};target.Create();
                }
                camera.targetTexture=target;camera.aspect=400f/600f;camera.orthographicSize=300;
                Canvas.ForceUpdateCanvases();camera.Render();camera.targetTexture=null;return target;
            });
        }
        public Rect GetRect(Appearance look,PortraitPart part,List<PortraitCalibrationRow> rows)
        {return WithTable(rows,()=>ClassicPortraitRenderer.Bounds(AppearanceRules.AsRealistic(look),part));}
        public Rect GetBaseRect(Appearance look,PortraitPart part)
        {return ClassicPortraitRenderer.BaseRect(AppearanceRules.AsRealistic(look),part);}
        public Vector2 CanvasDeltaToPartDelta(Appearance look,PortraitPart part,Vector2 delta,List<PortraitCalibrationRow> rows)
        {return WithTable(rows,()=>ClassicPortraitRenderer.CanvasDeltaToPartDelta(AppearanceRules.AsRealistic(look),part,delta));}
        public void Dispose()
        {
            if(renderer!=null){renderer.Dispose();renderer=null;}
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
