using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public class CommandCampaignConfigWindow:EditorWindow
    {
        const string ConfigPath="Assets/Minglu/Resources/Minglu/Config/command_campaign.json";
        string body,notice="三个剧本、每本三章、每章两条方法。新局快照配置，修改不影响进行中的战役。";
        Vector2 scroll;
        [MenuItem("Minglu/战役剧本配置与导入导出")]
        static void Open(){GetWindow<CommandCampaignConfigWindow>("战役剧本配置");}
        void OnEnable(){body=File.ReadAllText(ConfigPath);}
        void Check(){CommandRules.ValidateCommandCampaignConfig(JsonUtility.FromJson<CommandCampaignConfig>(body));}
        void Run(Action action){try{action();}catch(Exception e){notice=e.Message;}}
        void OnGUI()
        {
            EditorGUILayout.HelpBox(notice,MessageType.Info);EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("导入 JSON"))Run(()=>{string file=EditorUtility.OpenFilePanel("导入战役剧本","","json");if(file!=""){string before=body;body=File.ReadAllText(file);try{Check();}catch{body=before;throw;}notice="已导入并校验，尚未发布。";}});
            if(GUILayout.Button("校验"))Run(()=>{Check();notice="三剧本、十八方法及数值范围通过校验。";});
            if(GUILayout.Button("备份并发布"))Run(()=>{Check();File.Copy(ConfigPath,ConfigPath+".bak",true);File.WriteAllText(ConfigPath,body);AssetDatabase.Refresh();notice="已发布，仅新局生效。";});
            if(GUILayout.Button("导出 JSON"))Run(()=>{Check();string file=EditorUtility.SaveFilePanel("导出战役剧本","","command_campaign","json");if(file!="")File.WriteAllText(file,body);});
            EditorGUILayout.EndHorizontal();scroll=EditorGUILayout.BeginScrollView(scroll);body=EditorGUILayout.TextArea(body,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
    }
}
