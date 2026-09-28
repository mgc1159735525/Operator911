#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch.EditorTools
{
    // 场景搭建 Part2：信息面板 + 英雄详情 + 战斗演出 + 结算 + 日报 + 暂停
    public static partial class SceneCreator
    {
        static void CreateUI(Transform canvas)
        {
            var uiGo = new GameObject("UIManager", typeof(RectTransform));
            uiGo.transform.SetParent(canvas, false);
            Stretch((RectTransform)uiGo.transform);
            var ui = uiGo.AddComponent<UIManager>();

            ui.topBar = CreateTopBar(uiGo.transform);
            ui.mapPanel = CreateMapPanel(uiGo.transform);
            ui.heroBar = CreateHeroBar(uiGo.transform);
            ui.infoPanel = CreateInfoPanel(uiGo.transform);
            ui.heroDetail = CreateHeroDetail(uiGo.transform);
            ui.combatSim = CreateCombatSim(uiGo.transform);
            ui.result = CreateResult(uiGo.transform);
            ui.dailyReport = CreateDailyReport(uiGo.transform);
            ui.pauseBanner = CreatePauseBanner(uiGo.transform);
            ui.titleScreen = CreateTitleScreen(uiGo.transform);
            ui.gameOver = CreateGameOver(uiGo.transform);
            Debug.Log("[SceneCreator] UI 全部创建完成");
        }

        // ==================== 信息面板 ====================
        static InfoPanelUI CreateInfoPanel(Transform parent)
        {
            var root = Panel(parent, "InfoPanel", new Color(0, 0, 0, 0.85f));
            var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(root, false);
            var brt = (RectTransform)boxGo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-10, 0);
            brt.sizeDelta = new Vector2(400, 620);
            boxGo.GetComponent<Image>().color = Theme.BgSurface;

            var ip = root.gameObject.AddComponent<InfoPanelUI>();
            ip.panelRoot = root.gameObject;

            ip.titleText = Txt(brt, "Title", "事件", 17, Theme.Yellow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -26), new Vector2(-76, 26));
            ip.descText = Txt(brt, "Desc", "描述", 12, Theme.TextSecondary, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -62), new Vector2(-40, 46), TextAlignmentOptions.TopLeft);
            ip.needText = Txt(brt, "Need", "需求", 12, Theme.TextSecondary, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -116), new Vector2(-40, 18), TextAlignmentOptions.Left);
            ip.rewardText = Txt(brt, "Reward", "奖励", 12, Theme.Yellow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -140), new Vector2(-40, 18), TextAlignmentOptions.Left);
            ip.timerText = Txt(brt, "Timer", "⏱ 60s", 15, Theme.Warning, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -168), new Vector2(-40, 22), TextAlignmentOptions.Left);

            // 英雄列表（滚动）
            var scrollGo = new GameObject("HeroList", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(brt, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = new Vector2(0, 0.30f); srt.anchorMax = new Vector2(1, 0.72f);
            srt.offsetMin = new Vector2(16, 4); srt.offsetMax = new Vector2(-16, -4);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            vpGo.transform.SetParent(srt, false);
            var vrt = (RectTransform)vpGo.transform;
            Stretch(vrt);
            vrt.GetComponent<Image>().color = Color.clear;
            var cGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cGo.transform.SetParent(vpGo.transform, false);
            var crt = (RectTransform)cGo.transform;
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            var vlg = cGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            cGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = crt; scroll.viewport = vrt; scroll.horizontal = false;
            ip.heroListContainer = crt;

            // 英雄选择项模板
            var itemGo = new GameObject("HeroSelectItemPrefab", typeof(RectTransform), typeof(Image), typeof(Toggle));
            var irt = (RectTransform)itemGo.transform;
            irt.sizeDelta = new Vector2(0, 50);
            itemGo.GetComponent<Image>().color = Theme.BgCard;
            var inT = Txt(irt, "NameText", "英雄", 13, Theme.TextPrimary, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(140, 18), TextAlignmentOptions.Left);
            ((RectTransform)inT.transform).pivot = new Vector2(0, 1);
            var isT = Txt(irt, "StatsText", "属性", 10, Theme.TextDim, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 5), new Vector2(250, 14), TextAlignmentOptions.Left);
            ((RectTransform)isT.transform).pivot = new Vector2(0, 0);
            var ist = Txt(irt, "StateText", "✓", 12, Theme.Success, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(28, 20));
            ((RectTransform)ist.transform).pivot = new Vector2(1, 0.5f);
            itemGo.SetActive(false);
            itemGo.transform.SetParent(brt, false);
            ip.heroSelectItemPrefab = itemGo;

            ip.aggressiveToggle = MakeToggle(brt, "AggressiveToggle", "激进", 0.00f);
            ip.cautiousToggle = MakeToggle(brt, "CautiousToggle", "谨慎", 0.26f);
            ip.supportToggle = MakeToggle(brt, "SupportToggle", "支援", 0.52f);
            ip.reconToggle = MakeToggle(brt, "ReconToggle", "侦查", 0.78f);
            ip.tacticDescText = Txt(brt, "TacticDesc", "谨慎：战力×1.0", 11, Theme.TextDim, new Vector2(0, 0.19f), new Vector2(1, 0.19f), new Vector2(0, 0), new Vector2(-32, 18));
            ip.powerEstimateText = Txt(brt, "PowerEstimate", "战力预估: —", 13, Theme.Yellow, new Vector2(0, 0.13f), new Vector2(1, 0.13f), new Vector2(0, 0), new Vector2(-32, 22));

            ip.dispatchButton = Btn(brt, "DispatchBtn", "🚀 派 遣", 15, Theme.Success, Color.black, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(170, 38));
            ip.closeButton = Btn(brt, "CloseBtn", "✕", 13, Color.clear, Theme.TextDim, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(28, 28));

            root.gameObject.SetActive(false);
            return ip;
        }

        static Toggle MakeToggle(RectTransform parent, string name, string label, float xAnchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Toggle>();
            go.GetComponent<Image>().color = Theme.BgCard;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(xAnchor + 0.125f, 0.24f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(84, 28);
            var lt = new GameObject("label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            lt.transform.SetParent(go.transform, false);
            lt.text = label; lt.fontSize = 12; lt.color = Theme.TextPrimary;
            lt.alignment = TextAlignmentOptions.Center;
            lt.raycastTarget = false;
            Stretch((RectTransform)lt.transform);
            return t;
        }

        // ==================== 英雄详情 ====================
        static HeroDetailUI CreateHeroDetail(Transform parent)
        {
            var root = Panel(parent, "HeroDetailPanel", new Color(0, 0, 0, 0.9f));
            var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(root, false);
            var brt = (RectTransform)boxGo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(560, 640);
            boxGo.GetComponent<Image>().color = Theme.BgSurface;

            var hd = root.gameObject.AddComponent<HeroDetailUI>();
            hd.panelRoot = root.gameObject;

            // 立绘
            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(brt, false);
            var prt = (RectTransform)portraitGo.transform;
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(0.38f, 1);
            prt.offsetMin = new Vector2(16, -296); prt.offsetMax = new Vector2(16, -16);
            var pImg = portraitGo.GetComponent<Image>();
            pImg.color = Theme.BgCard;
            pImg.preserveAspect = true;
            hd.portraitImage = pImg;
            hd.playVoiceButton = Btn(prt, "VoiceBtn", "🔊 台词", 12, new Color(0, 0, 0, 0.6f), Theme.Cyan, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(90, 26));

            // 右侧文字区（0.40-1.0）
            float R = 0.40f;
            hd.codenameText = Txt(brt, "Codename", "英雄", 22, Theme.Yellow, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(-48, 30));
            hd.displayNameText = Txt(brt, "DisplayName", "真名 · Lv.1", 13, Theme.TextDim, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -62), new Vector2(-48, 18));
            hd.powerText = Txt(brt, "Power", "超能力", 12, Theme.Cyan, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -84), new Vector2(-48, 16));
            hd.stateText = Txt(brt, "State", "✓ 待命", 14, Theme.Success, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(-48, 20));
            hd.fatigueText = Txt(brt, "Fatigue", "疲劳: 0", 12, Theme.Success, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -132), new Vector2(-48, 16));
            hd.mentalText = Txt(brt, "Mental", "心理: 85", 12, Theme.Success, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -152), new Vector2(-48, 16));
            hd.quirkNameText = Txt(brt, "QuirkName", "⚠ 怪癖", 13, Theme.Warning, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -176), new Vector2(-48, 18));
            hd.quirkDescText = Txt(brt, "QuirkDesc", "怪癖描述", 11, Theme.TextDim, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -196), new Vector2(-48, 28), TextAlignmentOptions.TopLeft);
            hd.missionsText = Txt(brt, "Missions", "任务: 0/0", 12, Theme.TextSecondary, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -230), new Vector2(-48, 16));
            hd.salaryText = Txt(brt, "Salary", "日薪: ¥0", 12, Theme.Yellow, new Vector2(R, 1), new Vector2(1, 1), new Vector2(0, -250), new Vector2(-48, 16));

            // 技能区（3 行，含图标）
            hd.skillIcons = new Image[3];
            hd.skillNameTexts = new TMP_Text[3];
            hd.skillDescTexts = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = -286 - i * 74;
                var icoGo = new GameObject("SkillIcon" + i, typeof(RectTransform), typeof(Image));
                icoGo.transform.SetParent(brt, false);
                var icoRt = (RectTransform)icoGo.transform;
                icoRt.anchorMin = icoRt.anchorMax = new Vector2(0.42f, 1);
                icoRt.pivot = new Vector2(0.5f, 1);
                icoRt.anchoredPosition = new Vector2(30, y);
                icoRt.sizeDelta = new Vector2(52, 52);
                icoGo.GetComponent<Image>().color = Theme.BgCard;
                hd.skillIcons[i] = icoGo.GetComponent<Image>();

                hd.skillNameTexts[i] = Txt(brt, "SkillName" + i, "[被动] 技能", 12, Theme.TextPrimary, new Vector2(0.55f, 1), new Vector2(1, 1), new Vector2(0, y - 6), new Vector2(-40, 16), TextAlignmentOptions.Left);
                hd.skillDescTexts[i] = Txt(brt, "SkillDesc" + i, "技能描述", 10, Theme.TextDim, new Vector2(0.55f, 1), new Vector2(1, 1), new Vector2(0, y - 24), new Vector2(-40, 44), TextAlignmentOptions.TopLeft);
            }

            // 关系条目模板 + 容器
            var relGo = new GameObject("RelationItemPrefab", typeof(RectTransform), typeof(Image));
            relGo.GetComponent<Image>().color = Theme.BgCard;
            ((RectTransform)relGo.transform).sizeDelta = new Vector2(0, 22);
            var rn = Txt((RectTransform)relGo.transform, "NameText", "英雄", 11, Theme.TextPrimary, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6, 0), new Vector2(0, 18), TextAlignmentOptions.Left);
            ((RectTransform)rn.transform).pivot = new Vector2(0, 0.5f);
            var rt2 = Txt((RectTransform)relGo.transform, "TypeText", "搭档", 10, Theme.Success, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(0, 18), TextAlignmentOptions.Right);
            ((RectTransform)rt2.transform).pivot = new Vector2(1, 0.5f);
            relGo.SetActive(false);
            relGo.transform.SetParent(brt, false);
            var relContainerGo = new GameObject("RelationList", typeof(RectTransform));
            relContainerGo.transform.SetParent(brt, false);
            var rcRt = (RectTransform)relContainerGo.transform;
            rcRt.anchorMin = new Vector2(0.4f, 0); rcRt.anchorMax = new Vector2(1, 0);
            rcRt.offsetMin = new Vector2(20, 56); rcRt.offsetMax = new Vector2(-30, 160);
            var rcVlg = relContainerGo.AddComponent<VerticalLayoutGroup>();
            rcVlg.spacing = 2; rcVlg.childForceExpandWidth = true; rcVlg.childForceExpandHeight = false;
            hd.relationContainer = relContainerGo.transform;
            // 模板移入容器备用
            relGo.transform.SetParent(relContainerGo.transform, false);

            hd.restButton = Btn(brt, "RestBtn", "💤 安排休息", 12, Theme.Blue, Color.white, new Vector2(0.2f, 0), new Vector2(0.2f, 0), new Vector2(0, 14), new Vector2(130, 32));
            hd.closeButton = Btn(brt, "CloseBtn", "✕", 13, Color.clear, Theme.TextDim, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(28, 28));

            root.gameObject.SetActive(false);
            return hd;
        }
    }
}
#endif
