#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch.EditorTools
{
    // 场景搭建 Part3：战斗演出 + 结算 + 日报 + 暂停 + 英雄栏
    public static partial class SceneCreator
    {
        // ==================== 战斗演出 ====================
        static CombatSimUI CreateCombatSim(Transform parent)
        {
            var root = Panel(parent, "CombatSimOverlay", new Color(0, 0, 0, 0.88f));
            var cs = root.gameObject.AddComponent<CombatSimUI>();
            cs.panelRoot = root.gameObject;
            cs.titleText = Txt(root, "Title", "⚔ 战斗", 22, Theme.Yellow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(700, 32));
            cs.powerText = Txt(root, "Power", "战力 100 vs 50", 17, Theme.Cyan, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -148), new Vector2(500, 26));
            cs.narrativeText = Txt(root, "Narrative", "战斗叙述……", 15, Theme.TextPrimary, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(760, 110), TextAlignmentOptions.TopLeft);
            root.gameObject.SetActive(false);
            return cs;
        }

        // ==================== 结算 ====================
        static ResultUI CreateResult(Transform parent)
        {
            var root = Panel(parent, "ResultOverlay", new Color(0, 0, 0, 0.92f));
            var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(root, false);
            var brt = (RectTransform)boxGo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(620, 660);
            boxGo.GetComponent<Image>().color = Theme.BgSurface;

            var r = root.gameObject.AddComponent<ResultUI>();
            r.panelRoot = root.gameObject;
            r.gradeText = Txt(brt, "Grade", "S", 44, Theme.Yellow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(120, 58));
            r.powerRatioText = Txt(brt, "PowerRatio", "战力比 1.00", 14, Theme.TextSecondary, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(240, 20));
            r.battleNarrativeText = Txt(brt, "BattleNarr", "战斗叙述", 13, Theme.TextPrimary, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -132), new Vector2(-60, 58), TextAlignmentOptions.TopLeft);
            r.resultNarrativeText = Txt(brt, "ResultNarr", "结果叙述", 13, Theme.TextDim, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -196), new Vector2(-60, 36), TextAlignmentOptions.TopLeft);
            r.mediaHeadlineText = Txt(brt, "Headline", "📰 头条", 14, Theme.Yellow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -238), new Vector2(-60, 22));
            r.budgetChangeText = Txt(brt, "Budget", "预算 +¥100", 13, Theme.Success, new Vector2(0, 1), new Vector2(0.33f, 1), new Vector2(0, -266), new Vector2(150, 20));
            r.repChangeText = Txt(brt, "Rep", "声望 +5", 13, Theme.Success, new Vector2(0.33f, 1), new Vector2(0.66f, 1), new Vector2(0, -266), new Vector2(150, 20));
            r.xpText = Txt(brt, "XP", "XP +30", 13, Theme.Yellow, new Vector2(0.66f, 1), new Vector2(1, 1), new Vector2(0, -266), new Vector2(150, 20));
            r.collateralText = Txt(brt, "Collateral", "附带损害: 10", 12, Theme.TextDim, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -290), new Vector2(-60, 18));

            // 英雄表现容器 + 模板
            var pfGo = new GameObject("HeroPerformancePrefab", typeof(RectTransform), typeof(Image));
            pfGo.GetComponent<Image>().color = Theme.BgCard;
            ((RectTransform)pfGo.transform).sizeDelta = new Vector2(0, 28);
            var pn = Txt((RectTransform)pfGo.transform, "NameText", "英雄", 11, Theme.TextPrimary, new Vector2(0, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(6, 0), new Vector2(0, 18), TextAlignmentOptions.Left);
            ((RectTransform)pn.transform).pivot = new Vector2(0, 0.5f);
            var pp = Txt((RectTransform)pfGo.transform, "PowerText", "贡献 10", 10, Theme.TextDim, new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(0, 18));
            var pq = Txt((RectTransform)pfGo.transform, "QuirkText", "", 10, Theme.Warning, new Vector2(0.5f, 0.5f), new Vector2(0.78f, 0.5f), new Vector2(0, 0), new Vector2(0, 18));
            var pi = Txt((RectTransform)pfGo.transform, "InjuryText", "", 10, Theme.Danger, new Vector2(0.78f, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(0, 18), TextAlignmentOptions.Right);
            ((RectTransform)pi.transform).pivot = new Vector2(1, 0.5f);
            pfGo.SetActive(false);
            pfGo.transform.SetParent(brt, false);

            var perfContainerGo = new GameObject("PerformanceList", typeof(RectTransform));
            perfContainerGo.transform.SetParent(brt, false);
            var pcRt = (RectTransform)perfContainerGo.transform;
            pcRt.anchorMin = new Vector2(0, 0.16f); pcRt.anchorMax = new Vector2(1, 0.42f);
            pcRt.offsetMin = new Vector2(30, 4); pcRt.offsetMax = new Vector2(-30, -4);
            var pcVlg = perfContainerGo.AddComponent<VerticalLayoutGroup>();
            pcVlg.spacing = 3; pcVlg.childForceExpandWidth = true; pcVlg.childForceExpandHeight = false;
            r.heroPerformanceContainer = perfContainerGo.transform;
            pfGo.transform.SetParent(perfContainerGo.transform, false);
            r.heroPerformancePrefab = pfGo;

            var injGo = new GameObject("InjuredPanel", typeof(RectTransform), typeof(Image));
            injGo.transform.SetParent(brt, false);
            var ijrt = (RectTransform)injGo.transform;
            ijrt.anchorMin = new Vector2(0, 0.11f); ijrt.anchorMax = new Vector2(1, 0.15f);
            ijrt.offsetMin = new Vector2(30, 0); ijrt.offsetMax = new Vector2(-30, 0);
            injGo.GetComponent<Image>().color = new Color(1f, 0.3f, 0.42f, 0.15f);
            r.injuredHeroesText = Txt(ijrt, "InjuredText", "受伤: 无", 12, Theme.Danger, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            Stretch((RectTransform)r.injuredHeroesText.transform);
            r.injuredPanel = injGo;

            r.confirmButton = Btn(brt, "ConfirmBtn", "确 认", 15, Theme.Yellow, Color.black, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(160, 38));
            root.gameObject.SetActive(false);
            return r;
        }

        // ==================== 日报 ====================
        static DailyReportUI CreateDailyReport(Transform parent)
        {
            var root = Panel(parent, "DailyReportOverlay", new Color(0, 0, 0, 0.94f));
            var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(root, false);
            var brt = (RectTransform)boxGo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(580, 600);
            boxGo.GetComponent<Image>().color = Theme.BgSurface;

            var dr = root.gameObject.AddComponent<DailyReportUI>();
            dr.panelRoot = root.gameObject;
            dr.titleText = Txt(brt, "Title", "📋 日报", 22, Theme.Yellow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(300, 30));
            dr.dateText = Txt(brt, "Date", "Day 1 结束", 15, Theme.TextSecondary, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(300, 22));

            var statsT = Txt(brt, "Stats1", "处理事件: 0    失败: 0", 13, Theme.TextPrimary, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(30, -96), new Vector2(0, 20), TextAlignmentOptions.Left);
            ((RectTransform)statsT.transform).pivot = new Vector2(0, 0.5f);
            dr.eventsResolvedText = statsT;
            var statsT2 = Txt(brt, "Stats2", "英雄可用: 20/20", 13, Theme.Success, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(-30, -96), new Vector2(0, 20), TextAlignmentOptions.Right);
            ((RectTransform)statsT2.transform).pivot = new Vector2(1, 0.5f);
            dr.availableHeroesText = statsT2;
            dr.budgetText = Txt(brt, "Budget", "总预算: ¥10,000", 14, Theme.Yellow, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(30, -122), new Vector2(0, 20), TextAlignmentOptions.Left);
            ((RectTransform)dr.budgetText.transform).pivot = new Vector2(0, 0.5f);
            dr.repText = Txt(brt, "Rep", "总声望: 50/100", 14, Theme.Cyan, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(-30, -122), new Vector2(0, 20), TextAlignmentOptions.Right);
            ((RectTransform)dr.repText.transform).pivot = new Vector2(1, 0.5f);

            // eventsResolvedText 单独字段赋值
            dr.eventsResolvedText = statsT;
            // eventsFailedText 复用（避免新 UI 引用缺失，把失败并入第一行）
            dr.eventsFailedText = statsT;
            // 头条容器 + 模板
            var hlGo = new GameObject("HeadlinePrefab", typeof(RectTransform));
            var ht = hlGo.AddComponent<TextMeshProUGUI>();
            ht.fontSize = 12; ht.color = Theme.TextSecondary;
            ((RectTransform)hlGo.transform).sizeDelta = new Vector2(0, 18);
            hlGo.SetActive(false);
            var hcGo = new GameObject("Headlines", typeof(RectTransform));
            hcGo.transform.SetParent(brt, false);
            var hcRt = (RectTransform)hcGo.transform;
            hcRt.anchorMin = new Vector2(0, 0.22f); hcRt.anchorMax = new Vector2(1, 0.9f);
            hcRt.offsetMin = new Vector2(30, 8); hcRt.offsetMax = new Vector2(-30, -8);
            var hcVlg = hcGo.AddComponent<VerticalLayoutGroup>();
            hcVlg.spacing = 4; hcVlg.childForceExpandWidth = true; hcVlg.childForceExpandHeight = false;
            hlGo.transform.SetParent(hcGo.transform, false);
            dr.headlineContainer = hcGo.transform;
            dr.headlinePrefab = hlGo;

            dr.nextDayButton = Btn(brt, "NextDayBtn", "🌅 下 一 天", 15, Theme.Cyan, Color.black, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(190, 38));
            root.gameObject.SetActive(false);
            return dr;
        }

        // ==================== 暂停横幅 ====================
        static PauseBannerUI CreatePauseBanner(Transform parent)
        {
            var root = Panel(parent, "PauseBanner", new Color(0, 0, 0, 0.55f));
            var pb = root.gameObject.AddComponent<PauseBannerUI>();
            pb.bannerRoot = root.gameObject;
            pb.pauseText = Txt(root, "PauseText", "⏸ 已暂停", 30, Theme.Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(420, 44));
            root.gameObject.SetActive(false);
            return pb;
        }

        // ==================== 标题画面 ====================
        static TitleScreenUI CreateTitleScreen(Transform parent)
        {
            var root = Panel(parent, "TitleScreen", Theme.BgCanvas);

            var t1 = Txt(root, "Title", "HERO DISPATCH", 64, Theme.Cyan, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1000, 80));
            var t2 = Txt(root, "SubTitle", "超 级 英 雄 调 度 中 心", 24, Theme.TextSecondary, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(800, 34));
            var t3 = Txt(root, "Hint", "指挥20位英雄 · 调度紧急事件 · 守护这座赛博都市30天", 15, Theme.TextDim, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(900, 22));

            var ts = root.gameObject.AddComponent<TitleScreenUI>();
            ts.panelRoot = root.gameObject;
            ts.startButton = Btn(root, "StartBtn", "▶ 开始执勤", 20, Theme.Success, Color.black, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(240, 52));
            ts.startButton.onClick.AddListener(() => GameManager.Instance.BeginGame());
            root.gameObject.SetActive(false);
            return ts;
        }

        // ==================== 游戏结束画面 ====================
        static GameOverUI CreateGameOver(Transform parent)
        {
            var root = Panel(parent, "GameOverOverlay", new Color(0.02f, 0.02f, 0.05f, 0.97f));
            var go = root.gameObject.AddComponent<GameOverUI>();
            go.panelRoot = root.gameObject;
            go.titleText = Txt(root, "Title", "🏆 传奇谢幕", 48, Theme.Yellow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(800, 60));
            go.reasonText = Txt(root, "Reason", "结束原因", 18, Theme.TextPrimary, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(800, 60));
            go.statsText = Txt(root, "Stats", "统计数据", 15, Theme.TextSecondary, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(700, 60));
            go.restartButton = Btn(root, "RestartBtn", "↻ 重新开始", 18, Theme.Cyan, Color.black, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(220, 48));
            go.restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            root.gameObject.SetActive(false);
            return go;
        }

        // ==================== 底部英雄栏 ====================
        static HeroBarUI CreateHeroBar(Transform parent)
        {
            var go = new GameObject("HeroBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 110);
            go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.16f, 0.95f);

            var hb = go.AddComponent<HeroBarUI>();

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(rt, false);
            var srt = (RectTransform)scrollGo.transform;
            Stretch(srt);
            srt.offsetMin = new Vector2(8, 8); srt.offsetMax = new Vector2(-8, -8);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Unrestricted;

            var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            vpGo.transform.SetParent(srt, false);
            var vrt = (RectTransform)vpGo.transform;
            Stretch(vrt);
            vrt.GetComponent<Image>().color = Color.clear;

            var cGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
            cGo.transform.SetParent(vpGo.transform, false);
            var crt = (RectTransform)cGo.transform;
            crt.anchorMin = new Vector2(0, 0.5f); crt.anchorMax = new Vector2(0, 0.5f);
            crt.pivot = new Vector2(0, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            var grid = cGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(76, 92);
            grid.spacing = new Vector2(6, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            scroll.content = crt;
            scroll.viewport = vrt;

            // 英雄卡片模板
            var cardGo = new GameObject("HeroCardPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
            var crt2 = (RectTransform)cardGo.transform;
            crt2.sizeDelta = new Vector2(76, 92);
            cardGo.GetComponent<Image>().color = Theme.BgCard;

            var pGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            pGo.transform.SetParent(cardGo.transform, false);
            var prt = (RectTransform)pGo.transform;
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(0.5f, 1);
            prt.anchoredPosition = new Vector2(0, -4);
            prt.sizeDelta = new Vector2(0, 60);
            pGo.GetComponent<Image>().color = Theme.BgElevated;
            pGo.GetComponent<Image>().preserveAspect = true;

            var nT = Txt(crt2, "NameText", "英雄", 11, Theme.TextPrimary, new Vector2(0, 0.2f), new Vector2(1, 0.34f), new Vector2(0, 0), new Vector2(0, 0));
            var sT = Txt(crt2, "StateText", "✓", 11, Theme.Success, new Vector2(0, 0.06f), new Vector2(0.5f, 0.18f), new Vector2(4, 0), new Vector2(0, 0), TextAlignmentOptions.Left);
            var fT = Txt(crt2, "FatigueText", "疲0", 9, Theme.TextDim, new Vector2(0.5f, 0.06f), new Vector2(1, 0.18f), new Vector2(-2, 0), new Vector2(30, 12), TextAlignmentOptions.Right);

            cardGo.SetActive(false);
            cardGo.transform.SetParent(crt, false);

            hb.heroScroll = scroll;
            hb.heroCardPrefab = cardGo;
            return hb;
        }
    }
}
#endif
