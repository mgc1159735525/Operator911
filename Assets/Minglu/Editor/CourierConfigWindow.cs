using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public class CourierConfigWindow:EditorWindow
    {
        const string Output="Assets/Minglu/Resources/Minglu/Config/courier_config.json";
        string json,message="情报交通配置。发布只影响新局，旧局使用快照。";Vector2 scroll;
        [MenuItem("Minglu/情报交通配置")]
        public static void Open(){GetWindow<CourierConfigWindow>("情报交通");}
        void OnEnable(){json=File.ReadAllText(Output);}
        CourierConfig Validate()
        {
            var c=JsonUtility.FromJson<CourierConfig>(json);CourierRules.ValidateConfig(c,CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load()));return c;
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox(message,MessageType.Info);
            if(GUILayout.Button("校验联络、密件与交通参数"))Run(()=>{var c=Validate();message="通过："+c.sources.Count+"潜伏联络 / "+c.templates.Count+"密件模板。";});
            if(GUILayout.Button("导入 JSON 预览"))Run(()=>{var p=EditorUtility.OpenFilePanel("导入情报交通配置","","json");if(p!=""){json=File.ReadAllText(p);Validate();message="已导入预览，尚未发布。";}});
            if(GUILayout.Button("导出 JSON"))Run(()=>{var c=Validate();var p=EditorUtility.SaveFilePanel("导出情报交通配置","","courier_config","json");if(p!="")File.WriteAllText(p,JsonUtility.ToJson(c,true));});
            if(GUILayout.Button("校验并发布运行配置"))Run(()=>{var c=Validate();File.WriteAllText(Output+".tmp",JsonUtility.ToJson(c,true));File.Replace(Output+".tmp",Output,Output+".bak");AssetDatabase.ImportAsset(Output);message="发布成功，旧配置已备份。";});
            scroll=EditorGUILayout.BeginScrollView(scroll);json=EditorGUILayout.TextArea(json,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
        void Run(Action action){try{action();}catch(Exception e){message="未发布："+e.Message;}}
    }
}


