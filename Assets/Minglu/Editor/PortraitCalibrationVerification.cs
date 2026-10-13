using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class PortraitCalibrationVerification
    {
        static int checks;
        static void Require(bool value, string message)
        { checks++; if (!value) throw new Exception("Portrait calibration: " + message); }

        static PortraitCalibrationRow Row(int head, int variant, float dx)
        { return new PortraitCalibrationRow { sex = 0, head = head, variant = variant, part = PortraitPart.Eyes, dx = dx }; }

        static void Reject(string csv, string label)
        {
            string before = PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot());
            string previewBefore = PortraitCalibration.ToCsv(PortraitCalibration.Snapshot());
            int revision = PortraitCalibration.Revision;
            string error; List<PortraitCalibrationRow> rows;
            Require(!PortraitCalibration.TryParseCsv(csv, out rows, out error) && rows == null && !string.IsNullOrEmpty(error), label + " parses as valid");
            Require(!PortraitCalibration.TryApplyCsv(csv, out error), label + " replaces live data");
            Require(PortraitCalibration.Revision == revision, label + " changes the renderer revision");
            Require(before == PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot()), label + " partially changes committed rows");
            Require(previewBefore == PortraitCalibration.ToCsv(PortraitCalibration.Snapshot()), label + " changes an active preview");
        }

        [MenuItem("Minglu/验证/人物部件校准表")]
        public static void Verify()
        {
            checks = 0;
            string saved = PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot());
            var savedPreview = PortraitCalibration.HasPreview ? PortraitCalibration.Snapshot() : null;
            var culture = Thread.CurrentThread.CurrentCulture;
            var uiCulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                string error;
                Require(PortraitCalibration.TryApplyCsv(PortraitCalibration.CsvHeader + "\r\n", out error), "empty table must be identity");
                var identity = PortraitCalibration.Resolve(1, 11, PortraitPart.Nose, 7);
                Require(identity.dx == 0 && identity.dy == 0 && identity.scaleX == 1 && identity.scaleY == 1 && identity.spacing == 0 && identity.rotation == 0, "missing row is not identity");
                var rect = new Rect(100, 200, 40, 60);
                Require(identity.Apply(rect) == rect, "identity changes geometry");
                var transform = new PortraitTransform { dx = 10, dy = -5, scaleX = 2, scaleY = .5f };
                Require(transform.Apply(rect) == new Rect(90, 210, 80, 30), "centre-based geometry scales or translates incorrectly");

                var priority = new List<PortraitCalibrationRow> { Row(-1, -1, 10), Row(-1, 2, 20), Row(3, -1, 30), Row(3, 2, 40) };
                Require(PortraitCalibration.TryApplyCsv(PortraitCalibration.ToCsv(priority), out error), "four valid priority scopes rejected");
                Require(PortraitCalibration.Resolve(0, 3, PortraitPart.Eyes, 2).dx == 40, "exact row does not win");
                Require(PortraitCalibration.Resolve(0, 3, PortraitPart.Eyes, 1).dx == 30, "head wildcard variant does not win");
                Require(PortraitCalibration.Resolve(0, 2, PortraitPart.Eyes, 2).dx == 20, "global specific variant does not win");
                Require(PortraitCalibration.Resolve(0, 2, PortraitPart.Eyes, 1).dx == 10, "global wildcard does not win");
                Require(PortraitCalibration.Resolve(1, 3, PortraitPart.Eyes, 2).dx == 0, "male calibration leaks into female portraits");
                priority.RemoveAt(3);
                Require(PortraitCalibration.SetPreview(priority, out error) && PortraitCalibration.HasPreview, "preview rejected");
                Require(PortraitCalibration.Resolve(0, 3, PortraitPart.Eyes, 2).dx == 30, "head wildcard should win over global specific variant");
                priority[0].dx = 88;
                Require(PortraitCalibration.Resolve(0, 1, PortraitPart.Eyes, 0).dx == 10, "caller can mutate installed preview rows");
                var copy = PortraitCalibration.Snapshot(); copy[0].dx = 91;
                Require(PortraitCalibration.Resolve(0, 1, PortraitPart.Eyes, 0).dx == 10, "snapshot aliases the runtime table");
                PortraitCalibration.ClearPreview();
                Require(!PortraitCalibration.HasPreview && PortraitCalibration.Resolve(0, 3, PortraitPart.Eyes, 2).dx == 40, "clearing preview does not restore validated table");

                // Quoting, locale and BOM exercise files produced by spreadsheet tools.
                var boundary = new List<PortraitCalibrationRow> {
                    new PortraitCalibrationRow { sex = 0, head = -1, part = PortraitPart.Eyes, variant = 7, dx = -400, dy = 600, scaleX = .1f, scaleY = 3, spacing = -100, rotation = -180 },
                    new PortraitCalibrationRow { sex = 1, head = 11, part = PortraitPart.Brows, variant = 7, dx = 400, dy = -600, scaleX = 3, scaleY = .1f, spacing = 100, rotation = 180 },
                    new PortraitCalibrationRow { sex = 1, head = 0, part = PortraitPart.Head, variant = 0, dx = 1.25f, dy = -2.75f },
                    new PortraitCalibrationRow { sex = 0, head = 2, part = PortraitPart.AdornmentFar, variant = 2, rotation = 13.125f }
                };
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                string french = PortraitCalibration.ToCsv(boundary);
                Require(french.Contains("1.25") && !french.Contains("1,25"), "CSV decimals depend on editor culture");
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
                List<PortraitCalibrationRow> parsed;
                Require(PortraitCalibration.TryParseCsv("\ufeff" + french, out parsed, out error), "BOM/invariant CSV does not reimport");
                Require(PortraitCalibration.ToCsv(parsed) == french, "CSV roundtrip changes rows");
                Require(PortraitCalibration.CsvEncoding.GetPreamble().Length == 3, "export encoding lacks UTF8 BOM");
                string quoted = PortraitCalibration.CsvHeader + "\n\"0\",\"-1\",\"Eyes\",\"2\",\"1.5\",\"-2.5\",\"1\",\"1\",\"2\",\"0\"\n";
                Require(PortraitCalibration.TryParseCsv(quoted, out parsed, out error) && parsed[0].dx == 1.5f, "well-formed quoted fields rejected");
                Require(PortraitCalibration.TryApplyCsv(french, out error), "finite legal boundary table rejected");
                Require(PortraitCalibration.SetPreview(new[] { Row(-1, -1, 7) }, out error), "fixture preview rejected");
                string valid = "0,-1,Eyes,-1,2,0,1,1,0,0\r\n";
                string prefix = PortraitCalibration.CsvHeader + "\r\n";
                Reject(prefix + valid + valid, "duplicate key");
                Reject(prefix + valid + "1,0,Nose,0,NaN,0,1,1,0,0\r\n", "late invalid row/NaN");
                Require(!PortraitCalibration.TryParseCsv(prefix + valid + "\r\n" + "1,0,Nose,0,NaN,0,1,1,0,0\r\n", out parsed, out error) && error.StartsWith("第 4 行"), "validation error loses original CSV line after blank lines");
                Reject(prefix + "0,0,Eyes,0,0,Infinity,1,1,0,0", "infinity");
                Reject(prefix + "0,0,Eyes,0,0,0,0,1,0,0", "zero scale");
                Reject(prefix + "0,0,Nose,0,0,0,1,1,1,0", "spacing on a single feature");
                Reject(prefix + "0,12,Eyes,0,0,0,1,1,0,0", "unknown head");
                Reject(prefix + "0,0,Eyes,8,0,0,1,1,0,0", "unknown variant");
                Reject(prefix + "1,0,Beard,1,0,0,1,1,0,0", "female beard variant");
                Reject(prefix + "0,0,Head,1,0,0,1,1,0,0", "head must use the head key");
                Reject(prefix + "0,0,Eys,0,0,0,1,1,0,0", "unknown part");
                Reject(prefix + "0,0,2,0,0,0,1,1,0,0", "numeric enum bypass");
                Reject(prefix + "0,0,eyes,0,0,0,1,1,0,0", "wrong part case");
                Reject(prefix + "0,0,Eyes,0,0,0,1,1,0", "missing column");
                Reject(prefix + "0,0,Eyes,0,0,0,1,1,0,0,extra", "extra column");
                Reject(prefix + "0,0,\"Eyes,0,0,0,1,1,0,0", "unclosed quote");
                Reject(prefix + "0,0,\"Eyes\"x,0,0,0,1,1,0,0", "garbage after quote");
                Reject(prefix + "0,0,Ey\"es,0,0,0,1,1,0,0", "quote inside unquoted field");
                Reject("head,sex,part,variant,dx,dy,scaleX,scaleY,spacing,rotation\n" + valid, "misordered header");
                Reject("", "missing header");
                Reject(prefix + "0,0,Eyes,0,401,0,1,1,0,0", "position beyond bounds");
                Reject(prefix + "0,0,Eyes,0,0,0,1,1,101,0", "spacing beyond bounds");
                Reject(prefix + "0,0,Eyes,0,0,0,1,1,0,181", "rotation beyond bounds");
                int revisionBefore = PortraitCalibration.Revision;
                Require(!PortraitCalibration.SetPreview(new[] { Row(-1, -1, float.NaN) }, out error), "invalid preview accepted");
                Require(PortraitCalibration.Revision == revisionBefore && PortraitCalibration.Resolve(0, 2, PortraitPart.Eyes, 1).dx == 7, "invalid preview replaces valid preview");
                var look = AppearanceRules.Randomize(812, 0, 3);
                Require(PortraitCalibration.VariantFor(look, PortraitPart.Head) == 0 && PortraitCalibration.VariantFor(look, PortraitPart.Neck) == 0, "head/neck variant does not use head scope");
                Require(PortraitCalibration.VariantFor(look, PortraitPart.Nose) == look.parts[4] && PortraitCalibration.VariantFor(look, PortraitPart.AdornmentFar) == look.parts[8], "selected appearance maps to wrong row");
                Debug.Log("MINGLU_PORTRAIT_CALIBRATION_VERIFY_SUCCESS: " + checks + " identity/precedence/preview isolation/strict CSV/locale/bounds checks. No player save or project table was written.");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = uiCulture;
                string error;
                if (!PortraitCalibration.TryApplyCsv(saved, out error)) throw new Exception("Could not restore calibration verification state: " + error);
                if (savedPreview != null && !PortraitCalibration.SetPreview(savedPreview, out error)) throw new Exception("Could not restore calibration preview: " + error);
            }
        }
    }
}
