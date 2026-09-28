#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch.EditorTools
{
    // 场景搭建 Part1：入口 + 管理器 + 顶栏 + 地图
    public static partial class SceneCreator
    {
        [MenuItem("HeroDispatch/搭建完整场景")]
        public static void CreateFullScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            var gm = CreateManagers();
            var canvas = CreateCanvas();
            CreateUI(canvas.transform);
            SaveScene();
        }

        [MenuItem("HeroDispatch/批处理编译验证")]
        public static void BuildAndQuit()
        {
            ImportTMPEssentials();
            CreateChineseFontAssets();
            DefaultDataInitializer.CreateAll();
            CreateFullScene();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            Debug.Log("BUILD_SUCCESS");
            EditorApplication.Exit(0);
        }

        // 自动导入 TMP Essential Resources（避免首次打开弹窗且所有文字空白）
        static void ImportTMPEssentials()
        {
            // 直接检查 TMP Settings 资产文件是否已存在（比 FindAssets 更可靠）
            if (System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
            string pkg = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
            string fullPath = System.IO.Path.GetFullPath(pkg);
            if (!System.IO.File.Exists(fullPath))
            {
                var caches = System.IO.Directory.GetDirectories("Library/PackageCache", "com.unity.textmeshpro*");
                if (caches.Length > 0)
                    fullPath = System.IO.Path.Combine(caches[0], "Package Resources", "TMP Essential Resources.unitypackage");
            }
            if (System.IO.File.Exists(fullPath))
            {
                AssetDatabase.ImportPackage(fullPath, false);
                AssetDatabase.Refresh();
                Debug.Log("[SceneCreator] TMP Essentials 已导入");
            }
            else
            {
                Debug.LogWarning("[SceneCreator] 未找到 TMP Essential Resources 包");
            }
        }

        static void SaveScene()
        {
            string scenePath = "Assets/Scenes/Main.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == scenePath);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[SceneCreator] 场景已保存: " + scenePath);
        }

        static void CreateCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.07f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 0, -10);
        }

        static GameManager CreateManagers()
        {
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.heroManager = Child<HeroManager>(gmGo, "HeroManager");
            gm.eventManager = Child<EventManager>(gmGo, "EventManager");
            gm.combatSystem = Child<CombatSystem>(gmGo, "CombatSystem");
            gm.saveManager = Child<SaveManager>(gmGo, "SaveManager");
            gm.audioManager = Child<AudioManager>(gmGo, "AudioManager");

            gm.heroManager.heroDataArray = LoadAll<HeroData>("Assets/ScriptableObjects/Heroes");
            gm.eventManager.allEventTemplates = LoadAll<EventTemplate>("Assets/ScriptableObjects/Events");
            gm.eventManager.allDistricts = LoadAll<DistrictData>("Assets/ScriptableObjects/Districts");
            // 关系由 HeroManager.InitRelationships 从 HeroTable 初始化
            return gm;
        }

        static T Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<T>();
        }

        static T[] LoadAll<T>(string folder) where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            var list = new System.Collections.Generic.List<T>();
            foreach (var g in guids)
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g));
                if (a != null) list.Add(a);
            }
            return list.ToArray();
        }

        static Transform CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            return canvasGo.transform;
        }

        // ==================== 通用小工具 ====================
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static RectTransform Panel(Transform parent, string name, Color bg, bool active = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            go.AddComponent<Image>().color = bg;
            go.SetActive(active);
            return rt;
        }

        static TMP_Text Txt(RectTransform parent, string name, string content, int size, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            return t;
        }

        static Button Btn(Transform parent, string name, string label, int size, Color bg, Color fg,
            Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            var t = new GameObject("label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(go.transform, false);
            t.text = label; t.fontSize = size; t.color = fg;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            Stretch((RectTransform)t.transform);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            return go.GetComponent<Button>();
        }

        // ==================== 顶栏 ====================
        static TopBarUI CreateTopBar(Transform parent)
        {
            var go = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 48);
            go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);

            var brand = Txt(rt, "Brand", "HERO DISPATCH", 16, Theme.Cyan, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(180, 26), TextAlignmentOptions.Left);
            ((RectTransform)brand.transform).pivot = new Vector2(0, 0.5f);

            var tb = go.AddComponent<TopBarUI>();
            tb.dayText = Txt(rt, "Day", "DAY 1", 14, Theme.Purple, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(90, 24), TextAlignmentOptions.Left);
            ((RectTransform)tb.dayText.transform).pivot = new Vector2(0, 0.5f);
            tb.timeText = Txt(rt, "Time", "06:00", 15, Theme.Orange, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(90, 26));
            tb.repText = Txt(rt, "Rep", "50/100", 14, Theme.Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180, 0), new Vector2(110, 24));
            tb.budgetText = Txt(rt, "Budget", "¥10,000", 14, Theme.Yellow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, 0), new Vector2(130, 24));

            tb.speedText = null;
            tb.speedButton = Btn(rt, "SpeedBtn", "1x", 13, Theme.BgElevated, Theme.TextSecondary, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(42, 28));
            tb.pauseButton = Btn(rt, "PauseBtn", "⏸", 13, Theme.BgElevated, Theme.TextSecondary, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(34, 28));
            return tb;
        }

        // ==================== 地图 ====================
        static MapPanelUI CreateMapPanel(Transform parent)
        {
            var go = new GameObject("MapPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 0); rt.offsetMax = new Vector2(-420, -48);
            go.GetComponent<Image>().color = Theme.BgCanvas;

            var mp = go.AddComponent<MapPanelUI>();
            mp.districtContainer = rt;
            mp.markerContainer = rt;

            var markerGo = new GameObject("EventMarkerPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
            var mrt = (RectTransform)markerGo.transform;
            mrt.sizeDelta = new Vector2(86, 46);
            markerGo.GetComponent<Image>().color = Theme.BgCard;
            var gt = Txt(mrt, "GradeText", "[B]", 10, Theme.Blue, new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -8), new Vector2(30, 12), TextAlignmentOptions.Left);
            ((RectTransform)gt.transform).pivot = new Vector2(0, 1);
            var tt = Txt(mrt, "TimeText", "60s", 10, Theme.Warning, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -8), new Vector2(34, 12), TextAlignmentOptions.Right);
            ((RectTransform)tt.transform).pivot = new Vector2(1, 1);
            var it = Txt(mrt, "TypeIcon", "🔥", 13, Theme.TextPrimary, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 3), new Vector2(22, 22));
            var nt = Txt(mrt, "TypeName", "仓库火灾", 10, Theme.TextPrimary, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(28, 3), new Vector2(-54, 14), TextAlignmentOptions.Left);
            ((RectTransform)nt.transform).pivot = new Vector2(0, 0.5f);

            markerGo.SetActive(false);
            markerGo.transform.SetParent(go.transform, false);
            mp.eventMarkerPrefab = markerGo;
            return mp;
        }
    }
}
#endif
