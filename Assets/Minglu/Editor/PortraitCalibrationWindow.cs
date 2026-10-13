using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    // An authoring draft, deliberately unrelated to the player's appearance or save files.
    public sealed class PortraitCalibrationDraft : ScriptableObject
    {
        public List<PortraitCalibrationRow> rows = new List<PortraitCalibrationRow>();
        public Appearance look;
        public string basedOnCsv;
    }

    public sealed class PortraitCalibrationWindow : EditorWindow
    {
        public const string ProjectCsv = "Assets/Minglu/Resources/Minglu/Config/portrait-calibration.csv";
        const string DraftPath = "Library/Minglu/PortraitCalibrationDraft.json";
        static readonly PortraitPart[] Parts = { PortraitPart.Head, PortraitPart.Neck, PortraitPart.Eyes, PortraitPart.Brows, PortraitPart.Nose, PortraitPart.Mouth, PortraitPart.Beard, PortraitPart.Outfit, PortraitPart.Adornment, PortraitPart.AdornmentFar };
        static readonly string[] PartLabels = { "整体头部 / 五官随动", "颈部衔接", "眼睛", "眉毛", "鼻子", "嘴巴", "胡须", "衣服", "饰物 / 近侧耳坠", "远侧耳坠" };
        static readonly string[] ScopeLabels = { "当前头型 + 当前款式", "当前头型 + 所有款式", "所有头型 + 当前款式", "所有头型 + 所有款式" };
        [SerializeField] PortraitCalibrationDraft draft;
        // This is deliberately outside the Undo object. Undoing a pre-save edit
        // must not roll the known disk baseline backwards with the edited rows.
        [SerializeField] string savedBaseline;
        [SerializeField] bool projectInvalid;
        [SerializeField] int partIndex = 2, scopeIndex;
        [SerializeField] bool guides = true, faceZoom, sourceComparison, paletteExpanded;
        [SerializeField] float zoom = 1;
        Vector2 leftScroll, rightScroll;
        string message = "先选择部件，在预览中拖动位置；右下角方块调整大小。保存到项目后，游戏共用同一张校准表。";
        MessageType messageType = MessageType.Info;
        ClassicPortraitPreview preview;
        Texture previewTexture;
        bool renderDirty = true, disposed, writingDraft, disableDraftPersistence;
        int dragMode, dragUndoGroup;
        Vector2 dragStart;
        PortraitTransform dragValue;
        Rect dragBase;
        double lastBackup;
        bool backupPending;
        bool saveChangesFailed;
        Rect verificationCanvas;
        string isolatedProjectPath;
        string EffectiveProjectPath { get { return isolatedProjectPath ?? ProjectCsv; } }

        [MenuItem("Minglu/美术工具/人物部件校准器 %#p", false, 10)]
        [MenuItem("Tools/Minglu/人物部件校准器", false, 10)]
        public static void OpenMenu() { Open(); }

        public static PortraitCalibrationWindow Open()
        {
            var window = GetWindow<PortraitCalibrationWindow>("人物部件校准器");
            window.minSize = new Vector2(1100, 720);
            window.Show();
            return window;
        }

        void OnEnable()
        {
            disposed = false; minSize = new Vector2(1100, 720);
            if (draft == null)
            {
                draft = CreateInstance<PortraitCalibrationDraft>(); draft.hideFlags = HideFlags.HideAndDontSave;
                LoadProjectIntoDraft();
                TryRecoverDraft();
            }
            if (savedBaseline == null) savedBaseline = draft.basedOnCsv;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += UpdateBackup;
            EditorApplication.playModeStateChanged += OnPlayMode;
            saveChangesMessage = "人物校准尚未保存到项目。选择保存会更新共享校准表；未保存草稿也会保留在本项目的 Library 中。";
            SyncDirty(); renderDirty = true;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= UpdateBackup;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            if (!disableDraftPersistence) BackupDraft();
            if (preview != null) { preview.Dispose(); preview = null; }
            disposed = true;
            if (saveChangesFailed && !disableDraftPersistence)
            {
                string failure = message;
                string memoryDraft = JsonUtility.ToJson(draft), memoryBaseline = savedBaseline;
                bool invalid = projectInvalid;
                EditorApplication.delayCall += () =>
                {
                    if (this != null) return;
                    var reopened = Open();
                    // Even if both destination and Library writes failed, the
                    // latest edits survive this window's destruction in memory.
                    JsonUtility.FromJsonOverwrite(memoryDraft, reopened.draft);
                    reopened.savedBaseline = memoryBaseline; reopened.projectInvalid = invalid; reopened.Changed();
                    reopened.SetMessage("保存未完成，最新内存草稿已重新打开，可继续导出或重试。\n" + failure, MessageType.Error);
                };
            }
        }

        void OnPlayMode(PlayModeStateChange state)
        {
            if (preview != null) { preview.Dispose(); preview = null; }
            renderDirty = true; Repaint();
        }

        public override void SaveChanges()
        {
            saveChangesFailed = !SaveProject();
            if (!saveChangesFailed) base.SaveChanges();
            else { hasUnsavedChanges = true; BackupDraft(); }
        }

        public override void DiscardChanges()
        {
            LoadProjectIntoDraft(); BackupDraft(); base.DiscardChanges();
        }

        void LoadProjectIntoDraft()
        {
            try
            {
                string csv = ReadProjectCsv(); List<PortraitCalibrationRow> rows; string error;
                projectInvalid = !PortraitCalibration.TryParseCsv(csv, out rows, out error);
                if (projectInvalid) SetMessage("项目校准表无效，保留当前草稿。保存前请先修复或导入有效 CSV：" + error, MessageType.Error);
                else draft.rows = rows;
                savedBaseline = draft.basedOnCsv = csv;
            }
            catch (Exception e) { projectInvalid = true; SetMessage("无法读取项目表，当前草稿保留。请检查文件占用或权限后重试：" + e.Message, MessageType.Error); }
            if (draft.look == null) draft.look = DefaultLook();
        }

        static Appearance DefaultLook()
        {
            var a = AppearanceRules.Randomize(430, 0, 0);
            a.parts = new int[13]; a.portraitVersion = AppearanceRules.CurrentPortraitVersion; a.style = 1;
            a.enabled = a.featureEditing = a.groomingEditing = true;
            return a;
        }

        string ReadProjectCsv()
        { return File.Exists(EffectiveProjectPath) ? File.ReadAllText(EffectiveProjectPath, PortraitCalibration.CsvEncoding) : PortraitCalibration.ToCsv(new List<PortraitCalibrationRow>()); }

        void TryRecoverDraft()
        {
            if (!File.Exists(DraftPath)) return;
            var recovered = CreateInstance<PortraitCalibrationDraft>(); recovered.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(DraftPath, Encoding.UTF8), recovered);
                PortraitCalibration.ToCsv(recovered.rows); AppearanceRules.Validate(recovered.look);
                if (recovered.look == null || recovered.basedOnCsv == null) throw new InvalidDataException("草稿缺少预览或项目基线。");
                draft.rows = recovered.rows; draft.look = AppearanceRules.AsRealistic(recovered.look); draft.basedOnCsv = recovered.basedOnCsv;
                savedBaseline = recovered.basedOnCsv;
                SetMessage("已恢复本项目上次校准草稿。预览和表格均未读取玩家存档。", MessageType.Info);
            }
            catch (Exception e) { SetMessage("旧草稿读取失败，已保留原文件。当前载入项目表：" + e.Message, MessageType.Warning); }
            finally { DestroyImmediate(recovered); }
        }

        void OnUndoRedo() { if (draft != null) draft.basedOnCsv = savedBaseline; SyncDirty(); renderDirty = true; backupPending = true; Repaint(); }
        void UpdateBackup()
        {
            if (disposed || !backupPending || EditorApplication.timeSinceStartup - lastBackup < .6) return;
            BackupDraft();
        }

        bool BackupDraft()
        {
            if (writingDraft || draft == null || disableDraftPersistence) return false;
            writingDraft = true;
            try
            {
                draft.basedOnCsv = savedBaseline;
                AtomicWrite(DraftPath, JsonUtility.ToJson(draft, true), new UTF8Encoding(false));
                backupPending = false; lastBackup = EditorApplication.timeSinceStartup; return true;
            }
            catch (Exception e) { SetMessage("草稿自动保存失败，内存中的调整仍在。请重试或导出 CSV：" + e.Message, MessageType.Error); return false; }
            finally { writingDraft = false; }
        }

        static void AtomicWrite(string path, string text, Encoding encoding)
        {
            var full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full));
            var temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, text, encoding);
                if (File.Exists(full)) File.Replace(temp, full, null); else File.Move(temp, full);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        void SyncDirty()
        {
            if (draft == null) return;
            try { hasUnsavedChanges = PortraitCalibration.ToCsv(draft.rows) != Canonical(savedBaseline); }
            catch { hasUnsavedChanges = true; }
        }
        static string Canonical(string csv)
        {
            List<PortraitCalibrationRow> rows; string error;
            return PortraitCalibration.TryParseCsv(csv, out rows, out error) ? PortraitCalibration.ToCsv(rows) : csv;
        }
        void SetMessage(string text, MessageType type) { message = text; messageType = type; Repaint(); }
        PortraitPart SelectedPart { get { return Parts[Mathf.Clamp(partIndex, 0, Parts.Length - 1)]; } }
        ClassicPortraitPreview GetPreview() { return preview ?? (preview = new ClassicPortraitPreview()); }

        void OnGUI()
        {
            if (draft == null) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("人物部件校准", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(hasUnsavedChanges ? "● 草稿未应用" : "项目表已保存", EditorStyles.miniLabel);
                if (GUILayout.Button("教程", EditorStyles.toolbarButton, GUILayout.Width(58)))
                    EditorUtility.OpenWithDefaultApp(Path.GetFullPath("Docs/Minglu/人物部件校准器教程.html"));
            }
            EditorGUILayout.HelpBox(message, messageType);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorGUILayout.HelpBox("请先停止 Play 模式，再校准共享人物资源。草稿已保留；这里不会修改正在游玩的角色或进度。", MessageType.Warning); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLookColumn();
                DrawPreviewColumn();
                DrawTransformColumn();
            }
            DrawFooter();
        }

        void DrawLookColumn()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 74;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(235)))
            {
                leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
                GUILayout.Label("1 · 选择预览组合", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                int sex = EditorGUILayout.Popup("性别", draft.look.sex, new[] { "男性", "女性" });
                var look = AppearanceRules.Copy(draft.look);
                if (sex != look.sex) { look.sex = sex; if (sex == 1) look.parts[6] = 0; }
                look.parts[0] = IndexPopup("头部", look.parts[0], AppearanceRules.HeadNames(sex));
                foreach (int p in new[] { 2, 3, 4, 5, 6, 7, 8 })
                {
                    using (new EditorGUI.DisabledScope(p == 6 && sex == 1))
                        look.parts[p] = IndexPopup(AppearanceRules.Labels[p], look.parts[p], AppearanceRules.Names[p]);
                }
                paletteExpanded = EditorGUILayout.Foldout(paletteExpanded, "预览设色与背景", true);
                if (paletteExpanded) foreach (int p in new[] { 9, 10, 11, 12 }) look.parts[p] = IndexPopup(AppearanceRules.Labels[p], look.parts[p], AppearanceRules.Names[p]);
                if (EditorGUI.EndChangeCheck()) SetPreviewLook(look);
                EditorGUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("随机预览")) SetPreviewLook(AppearanceRules.Randomize(unchecked(Environment.TickCount), sex));
                    if (GUILayout.Button("复制组合 JSON")) { EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(draft.look, true); SetMessage("预览组合 JSON 已复制；未修改任何玩家档案。", MessageType.Info); }
                }
                if (GUILayout.Button("粘贴组合 JSON")) Run(() => { var a = JsonUtility.FromJson<Appearance>(EditorGUIUtility.systemCopyBuffer); if (a == null) throw new InvalidDataException("剪贴板没有有效外观。"); SetPreviewLook(AppearanceRules.AsRealistic(a)); });
                EditorGUILayout.Space(12);
                GUILayout.Label("2 · 选择调整部件", EditorStyles.boldLabel);
                for (int i = 0; i < Parts.Length; i++)
                {
                    bool chosen = i == partIndex;
                    if (GUILayout.Toggle(chosen, PartLabels[i], "Button", GUILayout.Height(25)) && !chosen) { partIndex = i; Repaint(); }
                }
                EditorGUILayout.HelpBox("头发、耳朵与脸型绘制在完整头部中，整体头部缩放会带动五官；衣服单独调整。\n索引从 0 开始，导出的 CSV 使用相同编号。", MessageType.None);
                EditorGUILayout.EndScrollView();
            }
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        static int IndexPopup(string label, int value, string[] names)
        { return EditorGUILayout.Popup(label, value, names.Select((name, index) => index + " · " + name).ToArray()); }

        void DrawPreviewColumn()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    bool nextFace = GUILayout.Toggle(faceZoom, "面部放大", EditorStyles.toolbarButton);
                    if (nextFace != faceZoom) { faceZoom = nextFace; }
                    guides = GUILayout.Toggle(guides, "参考线", EditorStyles.toolbarButton);
                    bool source = GUILayout.Toggle(sourceComparison, "对照默认", EditorStyles.toolbarButton);
                    if (source != sourceComparison) { sourceComparison = source; renderDirty = true; }
                }
                Rect viewport = GUILayoutUtility.GetRect(260, 260, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(viewport, new Color(.14f, .16f, .16f));
                if (Event.current.type == EventType.Repaint)
                {
                    try
                    {
                        if (preview == null) preview = new ClassicPortraitPreview();
                        if (renderDirty || previewTexture == null)
                        {
                            previewTexture = preview.Render(draft.look, sourceComparison ? new List<PortraitCalibrationRow>() : draft.rows, 800, 1200);
                            renderDirty = false;
                        }
                    }
                    catch (Exception e) { previewTexture = null; SetMessage("预览暂不可用，草稿仍保留：" + e.Message, MessageType.Error); }
                }
                Rect canvas = CanvasRect(viewport);
                verificationCanvas = canvas;
                GUI.BeginClip(viewport);
                Rect localCanvas = new Rect(canvas.position - viewport.position, canvas.size);
                if (previewTexture != null) GUI.DrawTexture(localCanvas, previewTexture, ScaleMode.StretchToFill, false);
                else GUI.Label(new Rect(10, 10, viewport.width - 20, 40), "正在绘制游戏同源预览…", EditorStyles.whiteLabel);
                GUI.EndClip();
                if (guides && !sourceComparison) DrawGuides(viewport, canvas);
                if (!sourceComparison) HandleCanvasInput(viewport, canvas);
                GUILayout.Label(sourceComparison ? "默认配准对照 · 松开对照后继续调整" : "拖动选中框移动 · 右下角方块缩放 · Shift 等比 · 方向键微调（Shift ×10）", EditorStyles.wordWrappedMiniLabel);
            }
        }

        Rect CanvasRect(Rect viewport)
        {
            float scale = Mathf.Min(viewport.width / (faceZoom ? 310 : 400), viewport.height / (faceZoom ? 330 : 600)) * zoom;
            Vector2 focus = faceZoom ? new Vector2(200, 195) : new Vector2(200, 300);
            return new Rect(viewport.center - focus * scale, new Vector2(400, 600) * scale);
        }

        Rect DisplayPartRect(Rect canvas)
        {
            Rect r = GetPreview().GetRect(draft.look, SelectedPart, draft.rows);
            float scale = canvas.width / 400;
            return new Rect(canvas.position + r.position * scale, r.size * scale);
        }

        void DrawGuides(Rect viewport, Rect canvas)
        {
            GUI.BeginClip(viewport);
            var offset = viewport.position;
            Handles.BeginGUI();
            Handles.color = new Color(.8f, .85f, .78f, .22f);
            for (int x = 0; x <= 400; x += 50) Handles.DrawLine(new Vector3(canvas.x + x * canvas.width / 400 - offset.x, canvas.y - offset.y), new Vector3(canvas.x + x * canvas.width / 400 - offset.x, canvas.yMax - offset.y));
            for (int y = 0; y <= 600; y += 50) Handles.DrawLine(new Vector3(canvas.x - offset.x, canvas.y + y * canvas.width / 400 - offset.y), new Vector3(canvas.xMax - offset.x, canvas.y + y * canvas.width / 400 - offset.y));
            Rect selected = DisplayPartRect(canvas); selected.position -= offset;
            var points = new[] { new Vector3(selected.xMin, selected.yMin), new Vector3(selected.xMax, selected.yMin), new Vector3(selected.xMax, selected.yMax), new Vector3(selected.xMin, selected.yMax), new Vector3(selected.xMin, selected.yMin) };
            Handles.color = new Color(1, .70f, .22f); Handles.DrawAAPolyLine(2, points);
            Handles.EndGUI();
            EditorGUI.DrawRect(new Rect(selected.xMax - 5, selected.yMax - 5, 10, 10), new Color(1, .70f, .22f));
            GUI.Label(new Rect(selected.x, selected.y - 21, Mathf.Max(180, selected.width), 21), PartLabels[partIndex], EditorStyles.whiteMiniLabel);
            GUI.EndClip();
        }

        void HandleCanvasInput(Rect viewport, Rect canvas)
        {
            var e = Event.current; Rect selected = DisplayPartRect(canvas);
            int id = GUIUtility.GetControlID("PortraitCalibrationDrag".GetHashCode(), FocusType.Keyboard, viewport);
            Rect handle = new Rect(selected.xMax - 9, selected.yMax - 9, 18, 18);
            if (e.type == EventType.MouseDown && e.button == 0 && viewport.Contains(e.mousePosition) && (selected.Contains(e.mousePosition) || handle.Contains(e.mousePosition)))
            {
                GUI.FocusControl(null); GUIUtility.hotControl = id; GUIUtility.keyboardControl = id;
                dragMode = handle.Contains(e.mousePosition) ? 2 : 1; dragStart = e.mousePosition; dragValue = CurrentTransform();
                dragBase = GetPreview().GetBaseRect(draft.look, SelectedPart);
                Undo.IncrementCurrentGroup(); dragUndoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("拖动人物部件");
                Undo.RegisterCompleteObjectUndo(draft, "拖动人物部件"); e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id && dragMode != 0)
            {
                Vector2 delta = (e.mousePosition - dragStart) / (canvas.width / 400);
                delta = GetPreview().CanvasDeltaToPartDelta(draft.look, SelectedPart, delta, draft.rows);
                var value = dragValue;
                if (dragMode == 1) { value.dx = Mathf.Clamp(value.dx + delta.x, -400, 400); value.dy = Mathf.Clamp(value.dy + delta.y, -600, 600); }
                else
                {
                    delta = (Vector2)(Quaternion.Euler(0, 0, -dragValue.rotation) * delta);
                    value.scaleX = Mathf.Clamp(value.scaleX + delta.x * 2 / Mathf.Max(1, dragBase.width), .1f, 3);
                    value.scaleY = Mathf.Clamp(value.scaleY + delta.y * 2 / Mathf.Max(1, dragBase.height), .1f, 3);
                    if (e.shift) value.scaleY = Mathf.Clamp(dragValue.scaleY * value.scaleX / dragValue.scaleX, .1f, 3);
                }
                SetCurrentParameters(value, false); e.Use();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
            { GUIUtility.hotControl = 0; dragMode = 0; Undo.CollapseUndoOperations(dragUndoGroup); BackupDraft(); e.Use(); }
            else if (e.type == EventType.KeyDown && GUIUtility.keyboardControl == id && !EditorGUIUtility.editingTextField)
            {
                Vector2 delta = e.keyCode == KeyCode.LeftArrow ? Vector2.left : e.keyCode == KeyCode.RightArrow ? Vector2.right : e.keyCode == KeyCode.UpArrow ? Vector2.down : e.keyCode == KeyCode.DownArrow ? Vector2.up : Vector2.zero;
                if (delta != Vector2.zero)
                {
                    delta = GetPreview().CanvasDeltaToPartDelta(draft.look, SelectedPart, delta * (e.shift ? 10 : 1), draft.rows);
                    var value = CurrentTransform(); value.dx = Mathf.Clamp(value.dx + delta.x, -400, 400); value.dy = Mathf.Clamp(value.dy + delta.y, -600, 600); SetCurrentParameters(value); e.Use();
                }
            }
        }

        void DrawTransformColumn()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 95;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(285)))
            {
                rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
                GUILayout.Label("3 · 调整 " + PartLabels[partIndex], EditorStyles.boldLabel);
                EditorGUILayout.Space(8);
                scopeIndex = EditorGUILayout.Popup("应用范围", scopeIndex, ScopeLabels);
                var key = CurrentKey();
                EditorGUILayout.LabelField((draft.look.sex == 0 ? "男性" : "女性") + " / 头型 " + KeyLabel(key.head) + " / 款式 " + KeyLabel(key.variant), EditorStyles.wordWrappedMiniLabel);
                var exact = draft.rows.FirstOrDefault(x => SameKey(x, key));
                var t = CurrentTransform();
                if (exact == null) EditorGUILayout.HelpBox("该范围未单独配置。首次调整会以当前实际值建立一行。", MessageType.None);
                EditorGUILayout.Space(7);
                EditorGUI.BeginChangeCheck();
                GUI.SetNextControlName("CalibrationX");
                t.dx = EditorGUILayout.Slider("水平偏移 X", t.dx, -400, 400);
                t.dy = EditorGUILayout.Slider("垂直偏移 Y", t.dy, -600, 600);
                t.scaleX = EditorGUILayout.Slider("宽度倍率", t.scaleX, .1f, 3);
                t.scaleY = EditorGUILayout.Slider("高度倍率", t.scaleY, .1f, 3);
                t.rotation = EditorGUILayout.Slider("顺时针角度", t.rotation, -180, 180);
                bool paired = SelectedPart == PortraitPart.Eyes || SelectedPart == PortraitPart.Brows;
                using (new EditorGUI.DisabledScope(!paired)) t.spacing = EditorGUILayout.Slider("双侧间距", t.spacing, -100, 100);
                if (EditorGUI.EndChangeCheck()) SetCurrentParameters(t);
                EditorGUILayout.HelpBox("X 向右为正，Y 向下为正。\n位置和间距以 400 × 600 立绘画布像素计；宽高 1 为原大小。\n眼 / 眉间距只移动两侧，不拉宽单只眼睛。", MessageType.None);
                Rect actual = GetPreview().GetRect(draft.look, SelectedPart, draft.rows);
                EditorGUILayout.LabelField("当前框尺寸", actual.width.ToString("0.0") + " × " + actual.height.ToString("0.0"));
                EditorGUILayout.Space(8);
                if (GUILayout.Button("本范围恢复默认参数", GUILayout.Height(29))) SetCurrentParameters(PortraitTransform.Identity);
                using (new EditorGUI.DisabledScope(exact == null))
                    if (GUILayout.Button("移除本范围配置 · 使用继承值")) RemoveCurrentRow();
                EditorGUILayout.Space(12);
                GUILayout.Label("范围与继承", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("精确头型 + 款式优先，然后是本头所有款、所有头的此款、该性别全部。范围之间覆盖，不叠加。\n修改大范围时，已有的更精确配置仍优先。", MessageType.None);
                var effective = Resolve(draft.rows, draft.look, SelectedPart);
                if (exact != null && !SameTransform(exact.Transform, effective))
                    EditorGUILayout.HelpBox("预览组合已有更精确的配置，本范围修改暂不会覆盖它。请改为“当前头型 + 当前款式”或移除那一行。", MessageType.Warning);
                if ((SelectedPart == PortraitPart.Beard && (draft.look.sex == 1 || draft.look.parts[6] == 0)) || (SelectedPart == PortraitPart.Adornment && draft.look.parts[8] == 0) || (SelectedPart == PortraitPart.AdornmentFar && draft.look.parts[8] != 2))
                    EditorGUILayout.HelpBox("该预览组合没有显示此部件。请在左侧先选择对应胡须或饰物。", MessageType.Warning);
                EditorGUILayout.EndScrollView();
            }
            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        static string KeyLabel(int v) { return v < 0 ? "全部 (-1)" : v.ToString(); }
        void DrawFooter()
        {
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("表内 " + draft.rows.Count + " 行\n保存不会修改玩家外观选择或进度。", EditorStyles.wordWrappedMiniLabel, GUILayout.Width(240));
                if (GUILayout.Button("撤销", GUILayout.Width(62), GUILayout.Height(32))) Undo.PerformUndo();
                if (GUILayout.Button("重做", GUILayout.Width(62), GUILayout.Height(32))) Undo.PerformRedo();
                if (GUILayout.Button("导入 CSV", GUILayout.Width(100), GUILayout.Height(32)))
                { string path = EditorUtility.OpenFilePanel("导入人物校准表", Path.GetDirectoryName(Path.GetFullPath(ProjectCsv)), "csv"); if (path.Length > 0) ImportCsv(path); }
                if (GUILayout.Button("导出 CSV", GUILayout.Width(100), GUILayout.Height(32)))
                { string path = EditorUtility.SaveFilePanel("导出人物校准表", Path.GetDirectoryName(Path.GetFullPath(ProjectCsv)), "portrait-calibration-backup", "csv"); if (path.Length > 0) ExportCsv(path); }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("保存到项目并生效", GUILayout.Width(180), GUILayout.Height(36))) SaveProject();
            }
        }

        PortraitCalibrationRow CurrentKey()
        {
            return new PortraitCalibrationRow { sex = draft.look.sex, head = scopeIndex >= 2 ? -1 : draft.look.parts[0], variant = scopeIndex == 1 || scopeIndex == 3 ? -1 : PortraitCalibration.VariantFor(draft.look, SelectedPart), part = SelectedPart };
        }
        static bool SameKey(PortraitCalibrationRow a, PortraitCalibrationRow b) { return a.sex == b.sex && a.head == b.head && a.part == b.part && a.variant == b.variant; }
        static bool SameTransform(PortraitTransform a, PortraitTransform b)
        { return a.dx == b.dx && a.dy == b.dy && a.scaleX == b.scaleX && a.scaleY == b.scaleY && a.spacing == b.spacing && a.rotation == b.rotation; }
        public PortraitTransform CurrentTransform()
        {
            var key = CurrentKey(); var exact = draft.rows.FirstOrDefault(x => SameKey(x, key));
            return exact == null ? Resolve(draft.rows, draft.look, SelectedPart) : exact.Transform;
        }
        static PortraitTransform Resolve(List<PortraitCalibrationRow> rows, Appearance look, PortraitPart part)
        {
            int variant = PortraitCalibration.VariantFor(look, part), head = look.parts[0];
            foreach (var pair in new[] { new[] { head, variant }, new[] { head, -1 }, new[] { -1, variant }, new[] { -1, -1 } })
            { var row = rows.FirstOrDefault(x => x.sex == look.sex && x.head == pair[0] && x.part == part && x.variant == pair[1]); if (row != null) return row.Transform; }
            return PortraitTransform.Identity;
        }

        public void SetCurrentParameters(PortraitTransform value, bool recordUndo = true)
        {
            var row = CurrentKey(); row.dx = value.dx; row.dy = value.dy; row.scaleX = value.scaleX; row.scaleY = value.scaleY; row.rotation = value.rotation;
            row.spacing = SelectedPart == PortraitPart.Eyes || SelectedPart == PortraitPart.Brows ? value.spacing : 0;
            var candidate = draft.rows.Where(x => !SameKey(x, row)).Select(x => x.Clone()).ToList(); candidate.Add(row);
            try { PortraitCalibration.ToCsv(candidate); }
            catch (Exception e) { SetMessage("调整未应用：" + e.Message, MessageType.Error); return; }
            if (recordUndo) Undo.RecordObject(draft, "调整人物部件");
            draft.rows = candidate; Changed();
        }
        public void RemoveCurrentRow()
        {
            var key = CurrentKey(); Undo.RecordObject(draft, "移除部件校准"); draft.rows.RemoveAll(x => SameKey(x, key)); Changed();
        }
        void Changed() { EditorUtility.SetDirty(draft); SyncDirty(); renderDirty = true; backupPending = true; Repaint(); }
        public void SetPreviewLook(Appearance look)
        { AppearanceRules.Validate(look); if (look == null) throw new ArgumentNullException("look"); Undo.RecordObject(draft, "切换校准预览"); draft.look = AppearanceRules.AsRealistic(look); Changed(); }
        public void SelectPart(PortraitPart part, int scope = 0)
        { partIndex = Array.IndexOf(Parts, part); if (partIndex < 0) partIndex = 0; scopeIndex = Mathf.Clamp(scope, 0, 3); Repaint(); }
        public List<PortraitCalibrationRow> DraftRows { get { return draft.rows.Select(x => x.Clone()).ToList(); } }
        public Appearance PreviewLook { get { return AppearanceRules.Copy(draft.look); } }

        public bool ImportCsv(string path)
        {
            try
            {
                List<PortraitCalibrationRow> rows; string error;
                string csv = File.ReadAllText(path, PortraitCalibration.CsvEncoding);
                if (!PortraitCalibration.TryParseCsv(csv, out rows, out error)) throw new InvalidDataException(error);
                Undo.RecordObject(draft, "导入人物校准表"); draft.rows = rows;
                if (Path.GetFullPath(path).Equals(Path.GetFullPath(EffectiveProjectPath), StringComparison.OrdinalIgnoreCase)) savedBaseline = draft.basedOnCsv = csv;
                projectInvalid = false;
                Changed(); BackupDraft();
                SetMessage("已导入 " + rows.Count + " 行至草稿，可撤销。请确认预览后再保存到项目。", MessageType.Info); return true;
            }
            catch (Exception e) { SetMessage("导入失败，原草稿未改变：" + e.Message, MessageType.Error); return false; }
        }
        public bool ExportCsv(string path)
        {
            try
            {
                if (Path.GetFullPath(path).Equals(Path.GetFullPath(EffectiveProjectPath), StringComparison.OrdinalIgnoreCase))
                { SetMessage("目标是项目生效表，请使用“保存到项目并生效”，避免绕过校验。", MessageType.Warning); return false; }
                AtomicWrite(path, PortraitCalibration.ToCsv(draft.rows), PortraitCalibration.CsvEncoding);
                SetMessage("CSV 已导出至 " + path + "。项目生效表未改动。", MessageType.Info); return true;
            }
            catch (Exception e) { SetMessage("导出失败，草稿仍保留，可重试：" + e.Message, MessageType.Error); return false; }
        }
        public bool SaveProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { SetMessage("请停止 Play 模式后保存。", MessageType.Warning); return false; }
            if (projectInvalid) { SetMessage("项目原表无效或未能读取，禁止以空表覆盖。请先修复原表，或明确导入有效 CSV 再保存。", MessageType.Error); return false; }
            try
            {
                string current = ReadProjectCsv();
                if (Canonical(current) != Canonical(savedBaseline))
                { SetMessage("项目表在打开窗口后被外部修改。请先导出当前草稿，再重新导入项目 CSV 后继续，避免覆盖他人修改。", MessageType.Error); return false; }
                string csv = PortraitCalibration.ToCsv(draft.rows); AtomicWrite(EffectiveProjectPath, csv, PortraitCalibration.CsvEncoding);
                if (isolatedProjectPath == null)
                {
                    AssetDatabase.ImportAsset(ProjectCsv, ImportAssetOptions.ForceUpdate);
                    string error; if (!PortraitCalibration.TryApplyCsv(csv, out error)) throw new InvalidDataException(error);
                }
                savedBaseline = draft.basedOnCsv = csv; saveChangesFailed = false; SyncDirty(); BackupDraft(); renderDirty = true;
                SetMessage("已保存到项目，编辑器和下次 Play 使用新校准。已安装游戏需重新构建安装后更新。", MessageType.Info); return true;
            }
            catch (Exception e) { SetMessage("保存失败，草稿和重试入口保留：" + e.Message, MessageType.Error); BackupDraft(); return false; }
        }
        void Run(Action action) { try { action(); } catch (Exception e) { SetMessage("操作未完成：" + e.Message, MessageType.Error); } }

        // Automation uses isolated draft state and paths. It never invokes SaveProject.
        public void UseIsolatedDraftForVerification(Appearance look, List<PortraitCalibrationRow> rows)
        {
            disableDraftPersistence = true;
            draft.look = AppearanceRules.AsRealistic(look); draft.rows = rows.Select(x => x.Clone()).ToList();
            savedBaseline = draft.basedOnCsv = PortraitCalibration.ToCsv(rows); projectInvalid = false;
            isolatedProjectPath = "outputs/qa-current/calibration-editor-fixtures/project-" + Guid.NewGuid().ToString("N") + ".csv";
            AtomicWrite(isolatedProjectPath, savedBaseline, PortraitCalibration.CsvEncoding); Changed();
        }
        public string IsolatedProjectPathForVerification { get { return isolatedProjectPath; } }
        public void BeginSimulatedDragForVerification()
        { Undo.IncrementCurrentGroup(); dragUndoGroup = Undo.GetCurrentGroup(); Undo.RegisterCompleteObjectUndo(draft, "校准拖动回归"); }
        public void EndSimulatedDragForVerification() { Undo.CollapseUndoOperations(dragUndoGroup); }
        public void FinishVerification() { hasUnsavedChanges = false; disableDraftPersistence = true; DestroyImmediate(this); }
        public bool HasPreviewForVerification { get { return previewTexture != null && verificationCanvas.width > 0; } }
        public Vector2 DragPointForVerification(bool resize)
        { var r = DisplayPartRect(verificationCanvas); return resize ? new Vector2(r.xMax, r.yMax) : r.center; }
        public void SetFaceZoomForVerification(bool value) { faceZoom = value; Repaint(); }
    }
}
