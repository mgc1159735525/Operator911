using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class PortraitCalibrationEditorVerification
    {
        static int checks, phase;
        static PortraitCalibrationWindow window;
        static double nextTick, started;
        static Vector2 mouseStart;
        static PortraitTransform moved, beforeResize;
        const string Output = "outputs/qa-current/calibration-editor";
        static void Require(bool value, string reason) { checks++; if (!value) throw new Exception("Portrait calibration editor: " + reason); }
        static bool Near(float a, float b) { return Mathf.Abs(a - b) < .02f; }
        static Appearance Look()
        { var a = AppearanceRules.Randomize(430, 0, 0); a.parts = new int[13]; return a; }

        public static int VerifyDraftOperations()
        {
            checks = 0;
            var w = ScriptableObject.CreateInstance<PortraitCalibrationWindow>();
            string committed = PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot());
            string project = File.Exists(PortraitCalibrationWindow.ProjectCsv) ? File.ReadAllText(PortraitCalibrationWindow.ProjectCsv) : null;
            try
            {
                w.UseIsolatedDraftForVerification(Look(), new List<PortraitCalibrationRow>()); w.SelectPart(PortraitPart.Eyes);
                var t = PortraitTransform.Identity; t.dx = 12; t.dy = -4; t.scaleX = 1.1f; t.scaleY = .92f; t.spacing = 7; t.rotation = 3;
                Undo.IncrementCurrentGroup(); w.SetCurrentParameters(t); Undo.FlushUndoRecordObjects();
                Require(w.DraftRows.Count == 1 && Near(w.CurrentTransform().dx, 12), "parameter editing did not create the exact scope row");
                Undo.PerformUndo(); Require(w.DraftRows.Count == 0, "numeric change could not be undone");
                Undo.PerformRedo(); Require(w.DraftRows.Count == 1 && Near(w.CurrentTransform().spacing, 7), "numeric change could not be redone");
                Require(w.SaveProject(), "isolated project save failed");
                Require(!w.hasUnsavedChanges, "save did not clear dirty state");
                Undo.PerformUndo(); Require(w.hasUnsavedChanges && w.DraftRows.Count == 0, "undo after save restored the disk baseline incorrectly");
                Require(w.SaveProject(), "save after undo was mistaken for an external conflict");
                Undo.IncrementCurrentGroup(); w.SetCurrentParameters(t); Undo.FlushUndoRecordObjects();
                string directory = Path.GetDirectoryName(w.IsolatedProjectPathForVerification);
                string exported = Path.Combine(directory, "export-" + Guid.NewGuid().ToString("N") + ".csv");
                Require(w.ExportCsv(exported), "CSV export failed");
                var bytes = File.ReadAllBytes(exported); Require(bytes.Length > 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191, "export lacks Excel-readable UTF-8 BOM");
                string previous = PortraitCalibration.ToCsv(w.DraftRows);
                string bad = Path.Combine(directory, "bad-" + Guid.NewGuid().ToString("N") + ".csv"); File.WriteAllText(bad, "bad,header\n");
                Require(!w.ImportCsv(bad) && previous == PortraitCalibration.ToCsv(w.DraftRows), "invalid import changed the active draft");
                string blocked = Path.Combine(directory, "blocked-" + Guid.NewGuid().ToString("N") + ".csv"); Directory.CreateDirectory(blocked);
                Require(!w.ExportCsv(blocked) && previous == PortraitCalibration.ToCsv(w.DraftRows), "failed export changed the draft");
                Undo.IncrementCurrentGroup(); Require(w.ImportCsv(exported), "valid import did not recover after an invalid import"); Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup(); w.SelectPart(PortraitPart.Eyes, 3); var broad = PortraitTransform.Identity; broad.dx = 50; w.SetCurrentParameters(broad); Undo.FlushUndoRecordObjects();
                Require(w.DraftRows.Any(x => x.head == -1 && x.variant == -1 && Near(x.dx, 50)), "all-head/all-variant scope not authored");
                w.SelectPart(PortraitPart.Eyes); Require(Near(w.CurrentTransform().dx, 12), "broad row overwrote exact row");
                Undo.IncrementCurrentGroup(); w.RemoveCurrentRow(); Undo.FlushUndoRecordObjects(); Require(Near(w.CurrentTransform().dx, 50), "remove failed to restore inherited value");
                Undo.PerformUndo(); Require(Near(w.CurrentTransform().dx, 12), "remove row could not be undone");
                string foreign = PortraitCalibration.CsvHeader + "\r\n0,-1,Head,-1,1,0,1,1,0,0\r\n";
                File.WriteAllText(w.IsolatedProjectPathForVerification, foreign);
                Require(!w.SaveProject() && File.ReadAllText(w.IsolatedProjectPathForVerification) == foreign, "external project edit was overwritten");
                Require(w.ImportCsv(w.IsolatedProjectPathForVerification) && w.SaveProject(), "explicit project import did not resolve stale baseline");
                Undo.IncrementCurrentGroup(); w.SelectPart(PortraitPart.Eyes); w.SetCurrentParameters(t); Undo.FlushUndoRecordObjects();
                string stored = File.ReadAllText(w.IsolatedProjectPathForVerification);
                var attributes = File.GetAttributes(w.IsolatedProjectPathForVerification);
                try
                {
                    File.SetAttributes(w.IsolatedProjectPathForVerification, attributes | FileAttributes.ReadOnly);
                    Require(!w.SaveProject() && w.hasUnsavedChanges, "read-only isolated target did not retain an unsaved retryable draft");
                    Require(File.ReadAllText(w.IsolatedProjectPathForVerification) == stored, "failed write damaged the isolated previous table");
                }
                finally { File.SetAttributes(w.IsolatedProjectPathForVerification, attributes); }
                Require(w.SaveProject() && !w.hasUnsavedChanges, "saving did not recover after target became writable");
                Require(PortraitCalibration.ToCsv(PortraitCalibration.CommittedSnapshot()) == committed, "isolated editor checks contaminated runtime table");
                Require((File.Exists(PortraitCalibrationWindow.ProjectCsv) ? File.ReadAllText(PortraitCalibrationWindow.ProjectCsv) : null) == project, "isolated editor checks changed the real project table");
                Debug.Log("MINGLU_PORTRAIT_CALIBRATION_EDITOR_RULES_SUCCESS checks=" + checks);
                return checks;
            }
            finally { w.FinishVerification(); }
        }

        // Launch with a normal (non-batch, graphics-enabled) Unity editor:
        // -executeMethod Minglu.EditorTools.PortraitCalibrationEditorVerification.OpenForVerification
        // --minglu-calibration-verify. The explicit flag requests automatic exit.
        public static void OpenForVerification()
        {
            BeginAfterStartup();
        }
        static void BeginAfterStartup()
        {
            try
            {
                VerifyDraftOperations();
                if (Application.isBatchMode) throw new InvalidOperationException("Actual editor interaction capture requires non-batch Unity.");
                if (Resources.FindObjectsOfTypeAll<PortraitCalibrationWindow>().Length == 0)
                {
                    Require(EditorApplication.ExecuteMenuItem("Minglu/美术工具/人物部件校准器"), "authoring menu item is not registered");
                    var opened = Resources.FindObjectsOfTypeAll<PortraitCalibrationWindow>();
                    Require(opened.Length == 1, "menu did not create the calibration window");
                    opened[0].FinishVerification();
                }
                window = ScriptableObject.CreateInstance<PortraitCalibrationWindow>();
                window.titleContent = new GUIContent("人物校准器 · 隔离验收");
                window.UseIsolatedDraftForVerification(Look(), new List<PortraitCalibrationRow>());
                window.SelectPart(PortraitPart.Eyes); window.SetFaceZoomForVerification(true);
                window.position = new Rect(24, 48, 1200, 760); window.ShowUtility(); window.Focus(); window.Repaint();
                phase = 0; started = EditorApplication.timeSinceStartup; nextTick = started + 6;
                EditorApplication.update += Tick;
            }
            catch (Exception e) { Finish(false, e); }
        }

        static void Mouse(EventType type, Vector2 point)
        { window.SendEvent(new Event { type = type, mousePosition = point, button = 0 }); window.Repaint(); }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextTick) return;
            nextTick = EditorApplication.timeSinceStartup + .3;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 35) throw new TimeoutException("Editor preview/interaction verification timed out.");
                if (!window.HasPreviewForVerification) { window.Repaint(); return; }
                switch (phase++)
                {
                    case 0:
                        Capture(Output + "-before.png"); mouseStart = window.DragPointForVerification(false); Mouse(EventType.MouseDown, mouseStart); break;
                    case 1: Mouse(EventType.MouseDrag, mouseStart + new Vector2(10, -3)); break;
                    case 2: Mouse(EventType.MouseDrag, mouseStart + new Vector2(20, -7)); break;
                    case 3:
                        Mouse(EventType.MouseUp, mouseStart + new Vector2(20, -7)); moved = window.CurrentTransform();
                        Require(moved.dx > 0 && moved.dy < 0, "actual cross-frame mouse drag did not change position");
                        Undo.PerformUndo(); Require(Near(window.CurrentTransform().dx, 0), "cross-frame drag was not one undo step");
                        Undo.PerformRedo(); Require(Near(window.CurrentTransform().dx, moved.dx), "drag redo failed"); break;
                    case 4:
                        window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow });
                        Require(Near(window.CurrentTransform().dx, moved.dx + 1), "focused preview arrow-key nudge failed");
                        beforeResize = window.CurrentTransform(); mouseStart = window.DragPointForVerification(true); Mouse(EventType.MouseDown, mouseStart); break;
                    case 5: Mouse(EventType.MouseDrag, mouseStart + new Vector2(14, 7)); break;
                    case 6:
                        Mouse(EventType.MouseUp, mouseStart + new Vector2(14, 7)); Require(window.CurrentTransform().scaleX > beforeResize.scaleX && window.CurrentTransform().scaleY > beforeResize.scaleY, "resize handle did not change both axes");
                        window.Repaint(); break;
                    case 7:
                        Capture(Output + "-adjusted.png");
                        var female = Look(); female.sex = 1; female.parts[0] = 9; female.parts[12] = 3;
                        window.SetPreviewLook(female); window.SelectPart(PortraitPart.Nose); window.SetFaceZoomForVerification(false); window.Repaint(); break;
                    case 8:
                        Capture(Output + "-female.png"); Finish(true, null); break;
                }
            }
            catch (Exception e) { Finish(false, e); }
        }

        static void Capture(string path)
        {
            var rect = window.position; float ppp = EditorGUIUtility.pixelsPerPoint;
            int width = Mathf.RoundToInt(rect.width * ppp), height = Mathf.RoundToInt(rect.height * ppp);
            // Capture this GUIView's own render buffer. Screen pixels can come
            // from a startup splash/another application when the editor is hidden.
            // Unity 2022.3 GUIView.bindings.cs exposes GrabPixels(RenderTexture, Rect).
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var parent = typeof(EditorWindow).GetField("m_Parent", flags).GetValue(window);
            var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView");
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create(); window.Focus(); viewType.GetMethod("RepaintImmediately", flags).Invoke(parent, null);
                viewType.GetMethod("GrabPixels", flags).Invoke(parent, new object[] { target, new Rect(0, 0, width, height) });
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); int sampled = 0, middle = 0, chromatic = 0;
                for (int i = 0; i < pixels.Length; i += 97)
                {
                    sampled++; var c = pixels[i]; int brightest = Math.Max(c.r, Math.Max(c.g, c.b)), darkest = Math.Min(c.r, Math.Min(c.g, c.b));
                    if (brightest > 12 && darkest < 242) middle++;
                    if (brightest - darkest > 18) chromatic++;
                }
                // GUIView's D3D back buffer is top-origin, unlike Camera.Render.
                // Normalize only this capture buffer for PNG's texture orientation.
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var row = new Color32[width];
                    for (int y = 0; y < height / 2; y++)
                    {
                        int low = y * width, high = (height - 1 - y) * width;
                        Array.Copy(pixels, low, row, 0, width); Array.Copy(pixels, high, pixels, low, width); Array.Copy(row, 0, pixels, high, width);
                    }
                    texture.SetPixels32(pixels); texture.Apply();
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, texture.EncodeToPNG());
                Require(middle > sampled / 5 && chromatic > sampled / 100, "GUIView capture is blank/monochrome rather than the calibration window");
                Debug.Log("MINGLU_CALIBRATION_CAPTURE path=" + path + " logical=" + rect.size + " scale=" + ppp + " framebuffer=" + width + "x" + height);
                using (var portrait = new ClassicPortraitPreview()) SaveTexture(portrait.Render(window.PreviewLook, window.DraftRows), path.Replace(".png", "-portrait.png"));
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
        static void SaveTexture(RenderTexture source, string path)
        {
            var previous = RenderTexture.active; var texture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            try { RenderTexture.active = source; texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
        }
        static void Finish(bool passed, Exception error)
        {
            EditorApplication.update -= Tick;
            if (window != null) { window.FinishVerification(); window = null; }
            if (passed) Debug.Log("MINGLU_PORTRAIT_CALIBRATION_EDITOR_UI_SUCCESS checks=" + checks + " actual-window=true drag/resize/keyboard/undo/redo=true");
            else Debug.LogException(error ?? new Exception("Editor verification failed."));
            Directory.CreateDirectory("outputs/qa-current");
            File.WriteAllText(Output + "-status.json", "{\"passed\":" + (passed ? "true" : "false") + ",\"checks\":" + checks + ",\"actualWindow\":" + (passed ? "true" : "false") + "}");
            if (Environment.GetCommandLineArgs().Contains("--minglu-calibration-verify")) EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
