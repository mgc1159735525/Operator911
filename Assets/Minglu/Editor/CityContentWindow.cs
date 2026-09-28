using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public class CityContentWindow:EditorWindow
    {
        const string Output="Assets/Minglu/Resources/Minglu/Config/city_content.json";
        string json,message="城市地点与案件配置。发布只影响新局，旧局使用快照。";Vector2 scroll;
        [MenuItem("Minglu/城市地点与案件配置")]
        public static void Open(){GetWindow<CityContentWindow>("城市与案件");}
        void OnEnable(){json=File.ReadAllText(Output);}
        CityContent Validate()
        {
            var c=JsonUtility.FromJson<CityContent>(json);var campaign=CampaignRules.Load();var game=CampaignRules.BuildConfig(ConfigStore.Load(),campaign);CityContentRules.Validate(c,game);
            foreach(var row in c.cases){campaign.missions.Add(row.mission);game.missions.Add(row.mission.task);}CampaignRules.Validate(campaign);ConfigStore.Validate(game);return c;
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox(message,MessageType.Info);
            if(GUILayout.Button("校验地点、线索、案件与前置"))Run(()=>{var c=Validate();message="通过："+c.sites.Count+"地点 / "+c.cases.Count+"案件方案。";});
            if(GUILayout.Button("导入 JSON 预览"))Run(()=>{var p=EditorUtility.OpenFilePanel("导入城市内容","","json");if(p!=""){json=File.ReadAllText(p);Validate();message="已导入预览，尚未发布。";}});
            if(GUILayout.Button("导出 JSON"))Run(()=>{var c=Validate();var p=EditorUtility.SaveFilePanel("导出城市内容","","city_content","json");if(p!="")File.WriteAllText(p,JsonUtility.ToJson(c,true));});
            if(GUILayout.Button("校验并发布运行配置"))Run(()=>{var c=Validate();File.WriteAllText(Output+".tmp",JsonUtility.ToJson(c,true));File.Replace(Output+".tmp",Output,Output+".bak");AssetDatabase.ImportAsset(Output);message="发布成功，旧配置已备份。";});
            scroll=EditorGUILayout.BeginScrollView(scroll);json=EditorGUILayout.TextArea(json,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
        void Run(Action action){try{action();}catch(Exception e){message="未发布："+e.Message;}}
    }
}
