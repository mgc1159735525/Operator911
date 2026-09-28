using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public class OrganizationConfigWindow:EditorWindow
    {
        const string Output="Assets/Minglu/Resources/Minglu/Config/organization_config.json";
        OrganizationConfig config;string message="修改数值后保存，新开局生效。旧局继续使用自己的配置快照。";Vector2 scroll;
        [MenuItem("Minglu/组织规则配置")]
        public static void Open(){GetWindow<OrganizationConfigWindow>("组织规则");}
        void OnEnable(){try{config=OrganizationRules.LoadConfig();}catch(Exception e){config=new OrganizationConfig();message=e.Message;}}
        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var field in typeof(OrganizationConfig).GetFields())field.SetValue(config,EditorGUILayout.IntField(field.Name,(int)field.GetValue(config)));
            if(GUILayout.Button("校验并保存运行配置"))Run(()=>{OrganizationRules.ValidateConfig(config);File.WriteAllText(Output,JsonUtility.ToJson(config,true));AssetDatabase.ImportAsset(Output);message="保存成功，新开局生效。";});
            if(GUILayout.Button("导入 JSON"))Run(()=>{string path=EditorUtility.OpenFilePanel("导入组织规则","","json");if(path!=""){var candidate=JsonUtility.FromJson<OrganizationConfig>(File.ReadAllText(path));OrganizationRules.ValidateConfig(candidate);config=candidate;message="导入已预览，点击保存后生效。";}});
            if(GUILayout.Button("导出 JSON"))Run(()=>{OrganizationRules.ValidateConfig(config);string path=EditorUtility.SaveFilePanel("导出组织规则","","organization_config","json");if(path!="")File.WriteAllText(path,JsonUtility.ToJson(config,true));});
            EditorGUILayout.HelpBox(message,MessageType.Info);EditorGUILayout.EndScrollView();
        }
        void Run(Action action){try{action();}catch(Exception e){message="未保存："+e.Message;}}
    }
}
