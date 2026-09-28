using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public class ConfigTableWindow : EditorWindow
    {
        string source = "outputs/minglu-v03/明路地下_游戏配置.xlsx";
        string report = "编辑Excel并保存，先预览校验，再导入。正在运行的对局不受改表影响，新开局才加载新配置。";
        Vector2 scroll;
        GameConfig pending;
        [MenuItem("Minglu/配置表工具")]
        public static void Open() { GetWindow<ConfigTableWindow>("明路·导表工具"); }
        void OnGUI()
        {
            EditorGUILayout.LabelField("Excel / CSV → 游戏配置", EditorStyles.boldLabel);
            string next = EditorGUILayout.TextField("工作簿或CSV目录", source);
            if (next != source) { source = next; pending = null; }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择 XLSX")) { var p = EditorUtility.OpenFilePanel("选择配置工作簿", "", "xlsx"); if (p.Length > 0) { source = p; pending = null; } }
            if (GUILayout.Button("选择 CSV 目录")) { var p = EditorUtility.OpenFolderPanel("选择CSV配置目录", "", ""); if (p.Length > 0) { source = p; pending = null; } }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("1. 预览与校验")) Run(() => { pending = ConfigTableTools.Read(source); report = ConfigTableTools.Summary(pending) + "\n校验通过，尚未修改运行配置。"; });
            GUI.enabled = pending != null && !EditorApplication.isPlaying;
            if (GUILayout.Button("2. 重新读取并导入")) Run(() => { pending = ConfigTableTools.Read(source); ConfigTableTools.Publish(pending); report = ConfigTableTools.Summary(pending) + "\n导入成功。新开局生效，旧存档使用自己的配置快照。"; });
            GUI.enabled = !EditorApplication.isPlaying;
            if (GUILayout.Button("从当前配置导出 CSV")) Run(() => { var path = EditorUtility.OpenFolderPanel("导出CSV", "", ""); if (path.Length > 0) { ConfigTableTools.Export(ConfigStore.Load(), path); report = "已导出至 " + path; } });
            GUI.enabled = true;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        void Run(Action action) { try { action(); } catch (Exception e) { pending = null; report = "导入未执行：\n" + e.Message; Debug.LogWarning(report); } }
    }

    public static class ConfigTableTools
    {
        public const string Output = "Assets/Minglu/Resources/Minglu/Config/game_config.json";
        static readonly string[] Sheets = { "Settings", "Agents", "Districts", "Actions", "Events", "Assets", "Missions" };
        static readonly string[] Fields = { "settings", "agents", "districts", "actions", "events", "assets", "missions" };
        public static GameConfig Read(string path)
        {
            var tables = Directory.Exists(path) ? Sheets.Where(s => s != "Missions" || File.Exists(Path.Combine(path, s + ".csv"))).ToDictionary(s => s, s => ParseCsv(File.ReadAllText(Path.Combine(path, s + ".csv")))) : ReadXlsx(path);
            var c = new GameConfig();
            for (int i = 0; i < Sheets.Length; i++)
            {
                if (Sheets[i] == "Missions" && !tables.ContainsKey(Sheets[i])) continue;
                if (!tables.ContainsKey(Sheets[i])) throw new InvalidDataException("缺少工作表 " + Sheets[i]);
                var field = typeof(GameConfig).GetField(Fields[i]);
                var list = (System.Collections.IList)field.GetValue(c);
                var rowType = field.FieldType.GetGenericArguments()[0];
                var rows = tables[Sheets[i]];
                if (rows.Count == 0) throw new InvalidDataException(Sheets[i] + " 缺少字段行。");
                var headers = rows[0].Select(x => x.Trim().TrimStart('\uFEFF')).ToArray();
                var properties = rowType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (headers.Distinct().Count() != headers.Length || headers.Any(h => !properties.Any(p => p.Name == h)) || properties.Any(p => !headers.Contains(p.Name)))
                    throw new InvalidDataException(Sheets[i] + " 字段不匹配，应为：" + string.Join(",", properties.Select(x => x.Name)));
                for (int r = 1; r < rows.Count; r++)
                {
                    var cells = rows[r];
                    if (cells.All(string.IsNullOrWhiteSpace) || (cells.Count > 0 && cells[0].StartsWith("#"))) continue;
                    if (cells.Count > headers.Length && cells.Skip(headers.Length).Any(x => !string.IsNullOrEmpty(x))) throw new InvalidDataException(Sheets[i] + " 第" + (r + 1) + "行有多余列。");
                    var record = Activator.CreateInstance(rowType);
                    for (int col = 0; col < headers.Length; col++)
                    {
                        string value = col < cells.Count ? cells[col] : "";
                        var p = rowType.GetField(headers[col]);
                        try
                        {
                            if (p.FieldType == typeof(string)) p.SetValue(record, value);
                            else if (p.FieldType == typeof(int)) p.SetValue(record, int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
                            else p.SetValue(record, float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
                        }
                        catch { throw new InvalidDataException(Sheets[i] + " 第" + (r + 1) + "行 " + headers[col] + " 不是有效" + p.FieldType.Name + "：" + value); }
                    }
                    list.Add(record);
                }
            }
            ConfigStore.Validate(c);
            foreach (var asset in c.assets)
                if (Resources.Load<Texture2D>(asset.resource) == null) throw new InvalidDataException("Assets " + asset.id + " 找不到纹理：" + asset.resource);
            return c;
        }

        public static void Publish(GameConfig config)
        {
            ConfigStore.Validate(config);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            string temp = Output + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(config, true), new UTF8Encoding(false));
            if (File.Exists(Output)) File.Replace(temp, Output, Output + ".bak"); else File.Move(temp, Output);
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceUpdate);
            Debug.Log("MINGLU_CONFIG_IMPORTED " + Summary(config));
        }

        public static string Summary(GameConfig c) { return "角色 " + c.agents.Count + " | 区域 " + c.districts.Count + " | 行动 " + c.actions.Count + " | 事件 " + c.events.Count + " | 任务 " + c.missions.Count + " | 资源 " + c.assets.Count + "\n" + c.Value("turns") + " 回合 / " + c.Value("goal") + " 目标 / " + c.Value("ap") + " AP"; }

        public static void Export(GameConfig c, string directory)
        {
            Directory.CreateDirectory(directory);
            for (int i = 0; i < Sheets.Length; i++)
            {
                var list = (System.Collections.IList)typeof(GameConfig).GetField(Fields[i]).GetValue(c);
                var type = typeof(GameConfig).GetField(Fields[i]).FieldType.GetGenericArguments()[0];
                var props = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var lines = new List<string> { string.Join(",", props.Select(p => p.Name)) };
                foreach (var record in list) lines.Add(string.Join(",", props.Select(p => CsvCell(Convert.ToString(p.GetValue(record), CultureInfo.InvariantCulture)))));
                File.WriteAllText(Path.Combine(directory, Sheets[i] + ".csv"), string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(true));
            }
        }
        static string CsvCell(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }

        public static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); bool quoted = false;
            text = text.TrimStart('\uFEFF');
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '"')
                {
                    if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (!quoted && (ch == ',' || ch == '\n' || ch == '\r'))
                {
                    row.Add(cell.ToString()); cell.Clear();
                    if (ch != ',') { rows.Add(row); row = new List<string>(); if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; }
                }
                else cell.Append(ch);
            }
            if (quoted) throw new InvalidDataException("CSV引号未闭合。");
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }

        static Dictionary<string, List<List<string>>> ReadXlsx(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                var shared = new List<string>();
                if (zip.GetEntry("xl/sharedStrings.xml") != null) shared = Xml(zip, "xl/sharedStrings.xml").Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToList();
                var relations = Xml(zip, "xl/_rels/workbook.xml.rels").Root.Elements().ToDictionary(x => (string)x.Attribute("Id"), x => (string)x.Attribute("Target"));
                var output = new Dictionary<string, List<List<string>>>();
                foreach (var sheet in Xml(zip, "xl/workbook.xml").Descendants(ns + "sheet"))
                {
                    string name = (string)sheet.Attribute("name"); if (!Sheets.Contains(name)) continue;
                    string target = relations[(string)sheet.Attribute(rel + "id")];
                    string entry = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                    var rows = new List<List<string>>();
                    foreach (var row in Xml(zip, entry).Descendants(ns + "row"))
                    {
                        var cells = new List<string>();
                        foreach (var cell in row.Elements(ns + "c"))
                        {
                            if (cell.Element(ns + "f") != null) throw new InvalidDataException(name + "!" + cell.Attribute("r") + " 含公式。配置表只接受确定值，请粘贴为数值。");
                            string address = (string)cell.Attribute("r"); int col = 0;
                            foreach (char ch in address) { if (!char.IsLetter(ch)) break; col = col * 26 + char.ToUpperInvariant(ch) - 'A' + 1; }
                            if (col < 1 || col > 100) throw new InvalidDataException("配置表列数超出限制。");
                            while (cells.Count < col) cells.Add("");
                            string value = (string)cell.Element(ns + "v") ?? "";
                            string type = (string)cell.Attribute("t");
                            if (type == "s") value = shared[int.Parse(value, CultureInfo.InvariantCulture)];
                            else if (type == "inlineStr") value = string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));
                            else if (type == "e") throw new InvalidDataException(name + " 单元格含Excel错误。");
                            cells[col - 1] = value;
                        }
                        if (rows.Count == 0) while (cells.Count > 0 && string.IsNullOrEmpty(cells[cells.Count - 1])) cells.RemoveAt(cells.Count - 1);
                        rows.Add(cells);
                    }
                    output.Add(name, rows);
                }
                return output;
            }
        }
        static XDocument Xml(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path); if (entry == null) throw new InvalidDataException("工作簿缺少 " + path);
            using (var stream = entry.Open()) return XDocument.Load(stream);
        }

        public static void BatchImport() { Publish(Read("outputs/minglu-v03/明路地下_游戏配置.xlsx")); }
    }
}
