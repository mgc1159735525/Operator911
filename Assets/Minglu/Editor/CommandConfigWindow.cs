using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public class CommandConfigWindow:EditorWindow
    {
        const string PathName="Assets/Minglu/Resources/Minglu/Config/command_config.json";
        string body,notice="编辑模板与组织参数，发布前检查。修改只影响新局。";Vector2 scroll;
        [MenuItem("Minglu/组织首脑配置与任务模板")]
        static void Open(){GetWindow<CommandConfigWindow>("组织首脑配置");}
        void OnEnable(){body=File.ReadAllText(PathName);}
        void Run(Action action){try{action();}catch(Exception e){notice=e.Message;}}
        void Check(){CommandRules.ValidateConfig(JsonUtility.FromJson<CommandConfig>(body));}
        void OnGUI()
        {
            EditorGUILayout.HelpBox(notice,MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("导入JSON"))Run(()=>{string f=EditorUtility.OpenFilePanel("导入组织配置","","json");if(f!=""){body=File.ReadAllText(f);Check();notice="已载入并校验；尚未发布。";}});
            if(GUILayout.Button("校验"))Run(()=>{Check();notice="校验通过。";});
            if(GUILayout.Button("备份并发布"))Run(()=>{Check();File.Copy(PathName,PathName+".bak",true);File.WriteAllText(PathName,body);AssetDatabase.Refresh();notice="已发布，新局生效。";});
            if(GUILayout.Button("导出JSON"))Run(()=>{Check();string f=EditorUtility.SaveFilePanel("导出配置","","command_config","json");if(f!="")File.WriteAllText(f,body);});
            EditorGUILayout.EndHorizontal();
            scroll=EditorGUILayout.BeginScrollView(scroll);body=EditorGUILayout.TextArea(body,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
    }
}
