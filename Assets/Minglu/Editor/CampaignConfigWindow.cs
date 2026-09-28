using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public class CampaignConfigWindow:EditorWindow
    {
        const string Output="Assets/Minglu/Resources/Minglu/Config/campaign_xinjing.json";
        string json,message="编辑或导入后先校验，再发布。已有存档保留整局配置快照。";Vector2 scroll;
        [MenuItem("Minglu/战役内容配置")]
        public static void Open(){GetWindow<CampaignConfigWindow>("战役内容");}
        void OnEnable(){json=File.ReadAllText(Output);}
        CampaignContent Validate(){var c=JsonUtility.FromJson<CampaignContent>(json);CampaignRules.BuildConfig(ConfigStore.Load(),c);return c;}
        void OnGUI()
        {
            EditorGUILayout.HelpBox(message,MessageType.Info);
            if(GUILayout.Button("校验阶段、任务依赖、角色与事件"))Run(()=>{var c=Validate();message="校验通过："+c.missions.Count+"任务 / "+c.events.Count+"事件 / 4阶段。";});
            if(GUILayout.Button("导入 JSON 预览"))Run(()=>{var path=EditorUtility.OpenFilePanel("导入战役","","json");if(path!=""){json=File.ReadAllText(path);Validate();message="已导入，尚未发布。";}});
            if(GUILayout.Button("导出已校验 JSON"))Run(()=>{var c=Validate();var path=EditorUtility.SaveFilePanel("导出战役","","campaign_xinjing","json");if(path!="")File.WriteAllText(path,JsonUtility.ToJson(c,true));});
            if(GUILayout.Button("校验并发布运行配置"))Run(()=>{var c=Validate();File.WriteAllText(Output+".tmp",JsonUtility.ToJson(c,true));File.Replace(Output+".tmp",Output,Output+".bak");AssetDatabase.ImportAsset(Output);message="发布成功，下一局生效。旧文件保留为 .bak。";});
            scroll=EditorGUILayout.BeginScrollView(scroll);json=EditorGUILayout.TextArea(json,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
        void Run(Action action){try{action();}catch(Exception e){message="未发布："+e.Message;}}
    }
}
