using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Minglu
{
    public enum PortraitPart { Head, Neck, Eyes, Brows, Nose, Mouth, Beard, Outfit, Adornment, AdornmentFar }

    [Serializable]
    public struct PortraitTransform
    {
        public float dx, dy, scaleX, scaleY, spacing, rotation;
        public static PortraitTransform Identity { get { return new PortraitTransform { scaleX = 1, scaleY = 1 }; } }
        // Coordinates are top-left pixels of the shared 400 x 600 canvas.
        // Rotation and paired spacing are deliberately applied by the renderer.
        public Rect Apply(Rect source)
        {
            var center = source.center + new Vector2(dx, dy);
            var size = new Vector2(source.width * scaleX, source.height * scaleY);
            return new Rect(center - size * .5f, size);
        }
    }

    [Serializable]
    public sealed class PortraitCalibrationRow
    {
        public int sex, head = -1, variant = -1;
        public PortraitPart part;
        public float dx, dy, scaleX = 1, scaleY = 1, spacing, rotation;
        public PortraitTransform Transform { get { return new PortraitTransform { dx = dx, dy = dy, scaleX = scaleX, scaleY = scaleY, spacing = spacing, rotation = rotation }; } }
        public PortraitCalibrationRow Clone() { return (PortraitCalibrationRow)MemberwiseClone(); }
    }

    // Shared authoring/runtime table. This never reads or writes player saves.
    // A preview is an entire in-memory replacement table, not a saved profile.
    public static class PortraitCalibration
    {
        public const string ResourcePath = "Minglu/Config/portrait-calibration";
        public const string CsvHeader = "sex,head,part,variant,dx,dy,scaleX,scaleY,spacing,rotation";
        public const int MaxRows = 8192;
        public static readonly Encoding CsvEncoding = new UTF8Encoding(true, true);
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        static Dictionary<string, PortraitCalibrationRow> committed = new Dictionary<string, PortraitCalibrationRow>(StringComparer.Ordinal);
        static Dictionary<string, PortraitCalibrationRow> preview;
        static bool initialized;
        static int revision;
        public static int Revision { get { EnsureLoaded(); return revision; } }
        public static bool HasPreview { get { return preview != null; } }
        public static string LastError { get; private set; }

        static string Key(int sex, int head, PortraitPart part, int variant)
        { return sex.ToString(Invariant) + "/" + head.ToString(Invariant) + "/" + part + "/" + variant.ToString(Invariant); }

        static void EnsureLoaded()
        {
            if (initialized) return;
            string error;
            Reload(out error);
        }

        public static bool Reload(out string error)
        {
            initialized = true;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                // An intentionally absent table is the unmodified portrait.
                committed = new Dictionary<string, PortraitCalibrationRow>(StringComparer.Ordinal);
                preview = null; LastError = null; revision++; error = null;
                return true;
            }
            if (TryApplyCsv(asset.text, out error)) return true;
            LastError = error;
            Debug.LogError("MINGLU_PORTRAIT_CALIBRATION_INVALID: " + error + "; keeping the last validated table (identity on first load).");
            return false;
        }

        public static int VariantCount(PortraitPart part, int sex)
        {
            switch (part)
            {
                case PortraitPart.Head: case PortraitPart.Neck: return 1;
                case PortraitPart.Eyes: case PortraitPart.Brows: case PortraitPart.Nose: case PortraitPart.Mouth:
                case PortraitPart.Outfit: return 8;
                case PortraitPart.Beard: return sex == 0 ? 8 : 1;
                case PortraitPart.Adornment: case PortraitPart.AdornmentFar: return 4;
                default: return 0;
            }
        }

        public static int VariantFor(Appearance look, PortraitPart part)
        {
            switch (part)
            {
                case PortraitPart.Head: case PortraitPart.Neck: return 0;
                case PortraitPart.Eyes: return look.parts[2];
                case PortraitPart.Brows: return look.parts[3];
                case PortraitPart.Nose: return look.parts[4];
                case PortraitPart.Mouth: return look.parts[5];
                case PortraitPart.Beard: return look.parts[6];
                case PortraitPart.Outfit: return look.parts[7];
                case PortraitPart.Adornment: case PortraitPart.AdornmentFar: return look.parts[8];
                default: return -1;
            }
        }

        public static PortraitTransform Resolve(Appearance look, PortraitPart part)
        { return Resolve(look, part, VariantFor(look, part)); }

        public static PortraitTransform Resolve(Appearance look, PortraitPart part, int variant)
        { return Resolve(look.sex, look.parts[0], part, variant); }

        public static PortraitTransform Resolve(int sex, int head, PortraitPart part, int variant)
        {
            EnsureLoaded();
            var table = preview ?? committed;
            PortraitCalibrationRow row;
            // These are replacements, never cumulative corrections.
            if (table.TryGetValue(Key(sex, head, part, variant), out row) ||
                table.TryGetValue(Key(sex, head, part, -1), out row) ||
                table.TryGetValue(Key(sex, -1, part, variant), out row) ||
                table.TryGetValue(Key(sex, -1, part, -1), out row)) return row.Transform;
            return PortraitTransform.Identity;
        }

        public static List<PortraitCalibrationRow> Snapshot()
        { EnsureLoaded(); return CopyRows(preview ?? committed); }

        public static List<PortraitCalibrationRow> CommittedSnapshot()
        { EnsureLoaded(); return CopyRows(committed); }

        static List<PortraitCalibrationRow> CopyRows(Dictionary<string, PortraitCalibrationRow> table)
        {
            var rows = new List<PortraitCalibrationRow>(table.Count);
            foreach (var row in table.Values) rows.Add(row.Clone());
            rows.Sort(CompareRows);
            return rows;
        }

        static int CompareRows(PortraitCalibrationRow a, PortraitCalibrationRow b)
        {
            int result = a.sex.CompareTo(b.sex); if (result != 0) return result;
            result = a.head.CompareTo(b.head); if (result != 0) return result;
            result = a.part.CompareTo(b.part); return result != 0 ? result : a.variant.CompareTo(b.variant);
        }

        public static bool SetPreview(IEnumerable<PortraitCalibrationRow> rows, out string error)
        {
            EnsureLoaded();
            Dictionary<string, PortraitCalibrationRow> parsed;
            if (!ValidateRows(rows, out parsed, out error)) return false;
            preview = parsed; revision++; LastError = null;
            return true;
        }

        public static void ClearPreview()
        { if (preview == null) return; preview = null; revision++; }

        public static bool TryApplyCsv(string csv, out string error)
        {
            List<PortraitCalibrationRow> rows;
            if (!TryParseCsv(csv, out rows, out error)) { LastError = error; return false; }
            Dictionary<string, PortraitCalibrationRow> parsed;
            if (!ValidateRows(rows, out parsed, out error)) { LastError = error; return false; }
            committed = parsed; preview = null; initialized = true; LastError = null; revision++;
            return true;
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static bool Between(float value, float min, float max) { return Finite(value) && value >= min && value <= max; }

        static bool ValidateRows(IEnumerable<PortraitCalibrationRow> rows, out Dictionary<string, PortraitCalibrationRow> result, out string error, IList<int> sourceLines = null)
        {
            result = new Dictionary<string, PortraitCalibrationRow>(StringComparer.Ordinal); error = null;
            if (rows == null) { error = "校准表不能为空引用。"; return false; }
            int index = 0;
            foreach (var row in rows)
            {
                int line = sourceLines == null ? index + 2 : sourceLines[index]; index++;
                string problem = row == null ? "记录为空" :
                    row.sex < 0 || row.sex > 1 ? "sex 只能为 0 或 1" :
                    row.head < -1 || row.head >= ClassicPortraitCatalog.HeadCount ? "head 只能为 -1 或 0～11" :
                    !Enum.IsDefined(typeof(PortraitPart), row.part) ? "未知 part" :
                    row.variant < -1 || row.variant >= VariantCount(row.part, row.sex) ? "variant 超出该部件的款式范围" :
                    !Between(row.dx, -400, 400) || !Between(row.dy, -600, 600) ? "位置必须为有限数，dx 范围 -400～400，dy 范围 -600～600" :
                    !Between(row.scaleX, .1f, 3) || !Between(row.scaleY, .1f, 3) ? "尺寸倍率范围为 0.1～3" :
                    !Between(row.spacing, -100, 100) ? "间距范围为 -100～100" :
                    row.spacing != 0 && row.part != PortraitPart.Eyes && row.part != PortraitPart.Brows ? "只有 Eyes / Brows 可设置双侧间距" :
                    !Between(row.rotation, -180, 180) ? "旋转角度范围为 -180～180" : null;
                if (problem != null) { error = "第 " + line + " 行：" + problem + "。"; return false; }
                string key = Key(row.sex, row.head, row.part, row.variant);
                if (result.ContainsKey(key)) { error = "第 " + line + " 行：重复键 " + key + "。"; return false; }
                result.Add(key, row.Clone());
                if (result.Count > MaxRows) { error = "校准表超过 " + MaxRows + " 行上限。"; return false; }
            }
            return true;
        }

        public static string ToCsv(IEnumerable<PortraitCalibrationRow> rows)
        {
            Dictionary<string, PortraitCalibrationRow> validated; string error;
            if (!ValidateRows(rows, out validated, out error)) throw new ArgumentException(error, "rows");
            var text = new StringBuilder(CsvHeader + "\r\n");
            foreach (var row in CopyRows(validated))
                text.Append(row.sex.ToString(Invariant)).Append(',').Append(row.head.ToString(Invariant)).Append(',').Append(row.part).Append(',')
                    .Append(row.variant.ToString(Invariant)).Append(',').Append(row.dx.ToString("R", Invariant)).Append(',').Append(row.dy.ToString("R", Invariant)).Append(',')
                    .Append(row.scaleX.ToString("R", Invariant)).Append(',').Append(row.scaleY.ToString("R", Invariant)).Append(',')
                    .Append(row.spacing.ToString("R", Invariant)).Append(',').Append(row.rotation.ToString("R", Invariant)).Append("\r\n");
            return text.ToString();
        }

        public static bool TryParseCsv(string csv, out List<PortraitCalibrationRow> rows, out string error)
        {
            rows = null; error = null;
            if (csv == null) { error = "CSV 内容为空。"; return false; }
            if (csv.Length > 4 * 1024 * 1024) { error = "CSV 超过 4 MiB 上限。"; return false; }
            if (csv.Length > 0 && csv[0] == '\ufeff') csv = csv.Substring(1);
            List<List<string>> records; List<int> recordLines;
            if (!ParseRecords(csv, out records, out recordLines, out error)) return false;
            if (records.Count == 0 || string.Join(",", records[0]) != CsvHeader || records[0].Count != 10)
            { error = "CSV 表头必须为：" + CsvHeader; return false; }
            var parsed = new List<PortraitCalibrationRow>();
            var parsedLines = new List<int>();
            for (int i = 1; i < records.Count; i++)
            {
                var fields = records[i];
                if (fields.Count == 1 && fields[0].Length == 0) continue;
                if (fields.Count != 10) { error = "第 " + recordLines[i] + " 行应包含 10 列。"; return false; }
                var row = new PortraitCalibrationRow();
                if (!int.TryParse(fields[0], NumberStyles.Integer, Invariant, out row.sex) ||
                    !int.TryParse(fields[1], NumberStyles.Integer, Invariant, out row.head) ||
                    !Enum.TryParse(fields[2], false, out row.part) || row.part.ToString() != fields[2] ||
                    !int.TryParse(fields[3], NumberStyles.Integer, Invariant, out row.variant) ||
                    !float.TryParse(fields[4], NumberStyles.Float, Invariant, out row.dx) ||
                    !float.TryParse(fields[5], NumberStyles.Float, Invariant, out row.dy) ||
                    !float.TryParse(fields[6], NumberStyles.Float, Invariant, out row.scaleX) ||
                    !float.TryParse(fields[7], NumberStyles.Float, Invariant, out row.scaleY) ||
                    !float.TryParse(fields[8], NumberStyles.Float, Invariant, out row.spacing) ||
                    !float.TryParse(fields[9], NumberStyles.Float, Invariant, out row.rotation))
                { error = "第 " + recordLines[i] + " 行有未知部件或无效数值；小数请使用英文句点。"; return false; }
                parsed.Add(row); parsedLines.Add(recordLines[i]);
            }
            Dictionary<string, PortraitCalibrationRow> validated;
            if (!ValidateRows(parsed, out validated, out error, parsedLines)) return false;
            rows = CopyRows(validated); return true;
        }

        // RFC-style quoted fields, with strict closing quotes. No partial rows
        // escape to the live table if a later record contains a damaged quote.
        static bool ParseRecords(string csv, out List<List<string>> records, out List<int> recordLines, out string error)
        {
            records = new List<List<string>>(); recordLines = new List<int>(); error = null;
            var row = new List<string>(); var field = new StringBuilder();
            bool quoted = false, closed = false;
            int currentLine = 1, recordLine = 1;
            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                        else { quoted = false; closed = true; }
                    }
                    else
                    {
                        field.Append(c);
                        if (c == '\n' || c == '\r' && (i + 1 >= csv.Length || csv[i + 1] != '\n')) currentLine++;
                    }
                    continue;
                }
                if (c == '"')
                {
                    if (field.Length != 0 || closed) { error = "CSV 引号必须从字段起点开始。"; return false; }
                    quoted = true; continue;
                }
                if (c == ',' || c == '\r' || c == '\n')
                {
                    row.Add(field.ToString()); field.Length = 0; closed = false;
                    if (c != ',')
                    {
                        records.Add(row); recordLines.Add(recordLine); row = new List<string>();
                        if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                        currentLine++; recordLine = currentLine;
                    }
                    continue;
                }
                if (closed) { error = "CSV 结束引号后只能紧跟逗号或换行。"; return false; }
                field.Append(c);
            }
            if (quoted) { error = "CSV 存在未闭合的引号。"; return false; }
            if (field.Length > 0 || closed || row.Count > 0) { row.Add(field.ToString()); records.Add(row); recordLines.Add(recordLine); }
            return true;
        }
    }
}
