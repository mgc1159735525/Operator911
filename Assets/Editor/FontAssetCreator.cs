#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using System.Collections.Generic;
using System.IO;

namespace HeroDispatch.EditorTools
{
    // 中文字体资产生成：从系统 TTF 文件（黑体+Emoji）生成动态 SDF + 回退链
    // 注：Font.CreateDynamicFontFromOSFont 在 batch 模式下 FontEngine 加载失败会返回 null，
    //     因此改为直接复制 C:\Windows\Fonts 下的 TTF 进项目再导入，100% 可靠。
    public static partial class SceneCreator
    {
        const string FontDir = "Assets/Fonts";
        const string CjkPath = "Assets/TextMesh Pro/Resources/Fonts/CJK_SDF.asset";
        const string EmojiPath = "Assets/TextMesh Pro/Resources/Fonts/EmojiSDF.asset";
        const string LiberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("HeroDispatch/生成中文字体资产")]
        public static void CreateChineseFontAssets()
        {
            Directory.CreateDirectory("Assets/TextMesh Pro/Resources/Fonts");
            Directory.CreateDirectory(FontDir);

            // ---- 中文（黑体 TTF，完整 CJK 覆盖） ----
            var cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CjkPath);
            if (cjk == null)
            {
                cjk = CreateFromSystemTTF("simhei.ttf", "SimHei SDF", 1024);
                if (cjk == null)
                {
                    // 备选：微软雅黑（TTC 可能不支持，再退到宋体）
                    cjk = CreateFromSystemTTF("msyh.ttf", "YaHei SDF", 1024)
                       ?? CreateFromSystemTTF("simsun.ttf", "SimSun SDF", 1024);
                }
                if (cjk == null) { Debug.LogError("[FontAsset] 中文系统字体创建失败（simhei/msyh/simsun 均不可用）"); return; }
                AssetDatabase.CreateAsset(cjk, CjkPath);
                Debug.Log("[FontAsset] 已生成中文字体: " + CjkPath);
            }

            // ---- Emoji（Segoe UI Emoji TTF，单色轮廓渲染） ----
            var emoji = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EmojiPath);
            if (emoji == null)
            {
                emoji = CreateFromSystemTTF("seguiemj.ttf", "SegoeEmoji SDF", 512);
                if (emoji != null)
                {
                    AssetDatabase.CreateAsset(emoji, EmojiPath);
                    Debug.Log("[FontAsset] 已生成Emoji字体: " + EmojiPath);
                }
                else Debug.LogWarning("[FontAsset] seguiemj.ttf 不可用，Emoji 将显示为缺字");
            }

            // ---- 回退链：CJK 缺字 → Emoji ----
            if (emoji != null)
            {
                if (cjk.fallbackFontAssetTable == null) cjk.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!cjk.fallbackFontAssetTable.Contains(emoji)) cjk.fallbackFontAssetTable.Add(emoji);
                EditorUtility.SetDirty(cjk);
            }

            // ---- 场景文本当前使用的 LiberationSans SDF 挂载回退：Latin 用原字体，中文/Emoji 走回退 ----
            var lib = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
            if (lib != null)
            {
                if (lib.fallbackFontAssetTable == null) lib.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!lib.fallbackFontAssetTable.Contains(cjk)) lib.fallbackFontAssetTable.Add(cjk);
                if (emoji != null && !lib.fallbackFontAssetTable.Contains(emoji)) lib.fallbackFontAssetTable.Add(emoji);
                EditorUtility.SetDirty(lib);
            }

            // ---- TMP 全局回退（fallbackFontAssets 为只读属性，须用 SerializedObject 写入） ----
            if (TMP_Settings.instance != null)
            {
                var so = new SerializedObject(TMP_Settings.instance);
                var prop = so.FindProperty("m_fallbackFontAssets");
                if (prop != null)
                {
                    bool hasCjk = false, hasEmoji = false;
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var a = prop.GetArrayElementAtIndex(i).objectReferenceValue as TMP_FontAsset;
                        if (a == cjk) hasCjk = true;
                        if (a == emoji) hasEmoji = true;
                    }
                    if (!hasCjk)
                    {
                        prop.arraySize++;
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = cjk;
                    }
                    if (emoji != null && !hasEmoji)
                    {
                        prop.arraySize++;
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = emoji;
                    }
                    so.ApplyModifiedProperties();
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[FontAsset] 中文字体资产就绪（SimHei + Emoji 回退链）");
        }

        // 从 C:\Windows\Fonts 复制 TTF 进项目 → 导入为 Font → 生成动态 TMP_FontAsset
        static TMP_FontAsset CreateFromSystemTTF(string ttfName, string assetName, int atlasSize)
        {
            string dst = FontDir + "/" + ttfName;
            string src = "C:/Windows/Fonts/" + ttfName;

            if (!System.IO.File.Exists(dst))
            {
                if (!System.IO.File.Exists(src))
                {
                    Debug.LogWarning("[FontAsset] 系统字体不存在: " + src);
                    return null;
                }
                System.IO.File.Copy(src, dst);
            }

            AssetDatabase.ImportAsset(dst);
            var font = AssetDatabase.LoadAssetAtPath<Font>(dst);
            if (font == null)
            {
                Debug.LogWarning("[FontAsset] 无法导入为 Font 资产: " + dst);
                return null;
            }

            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, atlasSize, atlasSize);
            if (fa == null)
            {
                Debug.LogWarning("[FontAsset] TMP_FontAsset.CreateFontAsset 返回 null: " + ttfName);
                return null;
            }
            fa.name = assetName;
            return fa;
        }
    }
}
#endif
