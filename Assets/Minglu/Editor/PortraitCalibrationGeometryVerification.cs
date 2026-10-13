using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Minglu.EditorTools
{
    public static class PortraitCalibrationGeometryVerification
    {
        static int checks;
        static void Require(bool result, string message)
        { checks++; if (!result) throw new Exception("Portrait calibration geometry: " + message); }
        static bool Near(float a, float b) { return Mathf.Abs(a - b) < .012f; }
        static bool Near(Vector2 a, Vector2 b) { return Near(a.x, b.x) && Near(a.y, b.y); }
        static bool Near(Rect a, Rect b) { return Near(a.position, b.position) && Near(a.size, b.size); }
        static readonly PortraitPart[] Parts = (PortraitPart[])Enum.GetValues(typeof(PortraitPart));

        static PortraitCalibrationRow Row(int sex, PortraitPart part, float dx = 0, float dy = 0, float sx = 1, float sy = 1, float spacing = 0, float rotation = 0)
        { return new PortraitCalibrationRow { sex = sex, head = -1, variant = -1, part = part, dx = dx, dy = dy, scaleX = sx, scaleY = sy, spacing = spacing, rotation = rotation }; }

        static void Preview(List<PortraitCalibrationRow> rows)
        { string error; Require(PortraitCalibration.SetPreview(rows, out error), "fixture table is invalid: " + error); }

        static RawImage[] Layers(RectTransform frame, PortraitPart part)
        {
            string primary = "Classic" + part;
            return frame.GetComponentsInChildren<RawImage>(true).Where(x => x.name == primary || ((part == PortraitPart.Eyes || part == PortraitPart.Brows) && x.name == primary + "Right")).ToArray();
        }

        static Rect ActualBounds(RectTransform frame, IEnumerable<RawImage> layers)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue); var max = -min; int count = 0;
            foreach (var layer in layers)
            {
                var corners = new Vector3[4]; layer.rectTransform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = frame.InverseTransformPoint(corner); var point = new Vector2(local.x, -local.y);
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point); count++;
                }
            }
            return count == 0 ? new Rect() : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Dictionary<PortraitPart, Rect> DrawAndMeasure(Appearance look, List<PortraitCalibrationRow> rows, bool inspectBounds)
        {
            Preview(rows);
            var root = new GameObject("Isolated calibration geometry", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
            var renderer = new ClassicPortraitRenderer();
            try
            {
                var frame = renderer.Draw(root.transform, look, 0, 0, 400, 600, Color.white);
                Canvas.ForceUpdateCanvases();
                var result = new Dictionary<PortraitPart, Rect>();
                foreach (var part in Parts)
                {
                    var layers = Layers(frame, part); var actual = ActualBounds(frame, layers); result[part] = actual;
                    if (!inspectBounds) continue;
                    var expected = ClassicPortraitRenderer.Bounds(look, part);
                    Require(Near(actual, expected), "selection bounds differ from actual RawImage world corners: sex=" + look.sex + ", head=" + look.parts[0] + ", part=" + part + ", expected=" + expected + ", actual=" + actual);
                    if (part == PortraitPart.Eyes || part == PortraitPart.Brows)
                    {
                        Require(layers.Length == 2, "paired feature is not split into two independently placed sprites: " + part);
                        Require(Near(layers[0].rectTransform.rect.width, layers[1].rectTransform.rect.width), "left/right sprite widths differ: " + part);
                        Require(Near(layers[0].uvRect.width, layers[1].uvRect.width) && Near(layers[0].uvRect.xMax, layers[1].uvRect.xMin), "split source atlas contains a gap or overlap: " + part);
                    }
                }
                return result;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); renderer.Dispose(); }
        }

        static List<PortraitCalibrationRow> Stress(int sex)
        {
            var rows = new List<PortraitCalibrationRow> {
                Row(sex, PortraitPart.Head, 12, -8, 1.2f, .86f, 0, 13),
                Row(sex, PortraitPart.Neck, 4, 3, .95f, 1.1f, 0, -7),
                Row(sex, PortraitPart.Eyes, 3, -2, 1.1f, .94f, 18, -5),
                Row(sex, PortraitPart.Brows, -2, 3, .9f, 1.1f, -35, 8),
                Row(sex, PortraitPart.Nose, 5, 7, 1.16f, .92f, 0, 17),
                Row(sex, PortraitPart.Mouth, -2, 2, .85f, 1.1f, 0, -12),
                Row(sex, PortraitPart.Outfit, -3, 9, .95f, 1.08f, 0, -4),
                Row(sex, PortraitPart.Adornment, -3, 6, 1.05f, .9f, 0, 12),
                Row(sex, PortraitPart.AdornmentFar, 4, -6, .85f, 1.1f, 0, -8)
            };
            if (sex == 0) rows.Add(Row(sex, PortraitPart.Beard, 4, 3, 1.1f, .91f, 0, 7));
            return rows;
        }

        static void VerifySpacing(Appearance look)
        {
            foreach (var part in new[] { PortraitPart.Eyes, PortraitPart.Brows })
            {
                var root = new GameObject("Isolated paired spacing", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                var renderer = new ClassicPortraitRenderer();
                try
                {
                    float referenceWidth = 0, referenceGap = 0;
                    foreach (float spacing in new[] { 0f, 20f, -20f, -100f, 100f })
                    {
                        Preview(new List<PortraitCalibrationRow> { Row(look.sex, part, spacing: spacing) });
                        var frame = renderer.Draw(root.transform, look, 0, 0, 400, 600, Color.white); Canvas.ForceUpdateCanvases();
                        var layers = Layers(frame, part); Require(layers.Length == 2, "spacing fixture lacks two sprites");
                        Rect left = ActualBounds(frame, new[] { layers[0] }), right = ActualBounds(frame, new[] { layers[1] });
                        float gap = right.xMin - left.xMax;
                        if (spacing == 0) { referenceWidth = left.width; referenceGap = gap; }
                        Require(Near(left.width, referenceWidth) && Near(right.width, referenceWidth), "spacing stretches a single eye or brow");
                        Require(Near(gap - referenceGap, spacing), "requested paired spacing does not change the actual sprite separation");
                        Require(Near(ActualBounds(frame, layers), ClassicPortraitRenderer.Bounds(look, part)), "negative/crossed pair bounds are wrong");
                        UnityEngine.Object.DestroyImmediate(frame.gameObject);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(root); renderer.Dispose(); }
            }
        }

        public static void Verify()
        {
            checks = 0;
            string saved = PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot());
            var savedPreview = PortraitCalibration.HasPreview ? PortraitCalibration.Snapshot() : null;
            try
            {
                var empty = new List<PortraitCalibrationRow>();
                for (int sex = 0; sex < 2; sex++) for (int head = 0; head < ClassicPortraitCatalog.HeadCount; head++)
                {
                    var look = AppearanceRules.Randomize(884 + head, sex, head); look.parts[6] = sex == 0 ? 4 : 0; look.parts[8] = 2;
                    DrawAndMeasure(look, empty, true); DrawAndMeasure(look, Stress(sex), true);
                }
                var focus = AppearanceRules.Randomize(883, 0, 1); focus.parts[6] = 4; focus.parts[8] = 2;
                var baseline = DrawAndMeasure(focus, empty, false);
                var shifted = DrawAndMeasure(focus, new List<PortraitCalibrationRow> { Row(0, PortraitPart.Head, 24, -16) }, false);
                foreach (var part in Parts)
                {
                    var before = baseline[part]; var after = shifted[part];
                    if (part == PortraitPart.Outfit) Require(Near(before, after), "moving head drags the outfit");
                    else Require(Near(after.center - before.center, new Vector2(24, -16)) && Near(before.size, after.size), "head translation does not carry " + part + " intact");
                }
                var scaled = DrawAndMeasure(focus, new List<PortraitCalibrationRow> { Row(0, PortraitPart.Head, 0, 0, 1.25f, .8f) }, false);
                foreach (var part in new[] { PortraitPart.Eyes, PortraitPart.Nose, PortraitPart.Mouth, PortraitPart.Neck, PortraitPart.Beard })
                {
                    var before = baseline[part]; var after = scaled[part]; var pivot = ClassicPortraitCatalog.HeadRect.center;
                    Require(Near(after.center, pivot + Vector2.Scale(before.center - pivot, new Vector2(1.25f, .8f))), "head scaling loses feature attachment: " + part);
                    Require(Near(after.size, Vector2.Scale(before.size, new Vector2(1.25f, .8f))), "head scaling changes proportions inconsistently: " + part);
                }
                Require(Near(baseline[PortraitPart.Outfit], scaled[PortraitPart.Outfit]), "head scale changes independent outfit");
                VerifySpacing(focus);

                // The editor preview temporarily installs a table only while drawing.
                // It restores an existing caller preview, and never applies it to Resources.
                var existingPreview = new List<PortraitCalibrationRow> { Row(0, PortraitPart.Nose, 7, -4) };
                Preview(existingPreview);
                string beforeCommitted = PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot());
                string beforePreview = PortraitCalibration.ToCsv(PortraitCalibration.Snapshot());
                using (var preview = new ClassicPortraitPreview())
                {
                    Rect first = preview.GetRect(focus, PortraitPart.Nose, empty);
                    Rect second = preview.GetRect(focus, PortraitPart.Nose, Stress(0));
                    Require(!Near(first, second), "different editor drafts yield identical bounds");
                    Require(PortraitCalibration.HasPreview && PortraitCalibration.ToCsv(PortraitCalibration.Snapshot()) == beforePreview, "editor GetRect leaks its draft over caller preview");
                    bool caught = false;
                    try { preview.GetRect(focus, PortraitPart.Nose, new List<PortraitCalibrationRow> { Row(0, PortraitPart.Nose, float.NaN) }); }
                    catch (ArgumentException) { caught = true; }
                    Require(caught && PortraitCalibration.ToCsv(PortraitCalibration.Snapshot()) == beforePreview, "invalid preview damages current calibration");
                    if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                    {
                        var texture = preview.Render(focus, Stress(0), 400, 600);
                        Require(texture != null && texture.IsCreated() && texture.width == 400 && texture.height == 600, "editor preview cannot render a real target");
                        Require(PortraitCalibration.ToCsv(PortraitCalibration.Snapshot()) == beforePreview, "editor Render leaks its temporary table");
                    }
                    PortraitCalibration.ClearPreview();
                    preview.GetRect(focus, PortraitPart.Eyes, Stress(0));
                    Require(!PortraitCalibration.HasPreview, "preview creates a lasting global override where none existed");
                }
                Require(PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot()) == beforeCommitted, "editor preview mutates committed data");
                Debug.Log("MINGLU_PORTRAIT_CALIBRATION_GEOMETRY_VERIFY_SUCCESS: " + checks + " actual UGUI world-corner/pair-spacing/head-attachment/editor-preview-isolation checks; 24 heads with identity and nonidentity tables. Pixel appearance is reviewed separately.");
            }
            finally
            {
                string error;
                if (!PortraitCalibration.TryApplyCsv(saved, out error)) throw new Exception("Cannot restore geometry test table: " + error);
                if (savedPreview != null && !PortraitCalibration.SetPreview(savedPreview, out error)) throw new Exception("Cannot restore geometry test preview: " + error);
            }
        }
    }
}
