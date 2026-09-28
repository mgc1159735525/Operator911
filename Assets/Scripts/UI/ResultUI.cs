using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch
{
    // ==================== 战斗演出面板 ====================
    public class CombatSimUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public TMP_Text titleText;
        public TMP_Text narrativeText;
        public TMP_Text powerText;

        CombatResult _pendingResult;

        public void Play(GameEvent ev, CombatResult result)
        {
            if (panelRoot == null) return;
            _pendingResult = result;
            if (titleText != null)
            {
                titleText.text = "⚔ " + (ev.revealed ? ev.template.eventName : "未知事件") + " — " + ev.districtName;
                titleText.color = Theme.GradeColor(result.grade);
            }
            if (powerText != null)
            {
                powerText.text = "我方战力 " + result.teamPower + " vs 难度 " + result.difficulty + "  →  [" + result.grade + "]";
                powerText.color = Theme.GradeColor(result.grade);
            }
            if (narrativeText != null) narrativeText.text = result.battleNarrative;
            panelRoot.SetActive(true);
            // 演出 2.2 秒后关闭并弹出结算
            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), 2.2f);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            // 演出结束 → 显示结算面板
            if (_pendingResult != null)
            {
                UIManager.Instance?.result?.Show(_pendingResult);
                _pendingResult = null;
            }
        }
    }

    // ==================== 结算面板 ====================
    public class ResultUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public TMP_Text gradeText;
        public Image gradeBackground;
        public TMP_Text powerRatioText;
        public TMP_Text battleNarrativeText;
        public TMP_Text resultNarrativeText;
        public TMP_Text mediaHeadlineText;
        public TMP_Text budgetChangeText;
        public TMP_Text repChangeText;
        public TMP_Text xpText;
        public TMP_Text collateralText;
        public Transform heroPerformanceContainer;
        public GameObject heroPerformancePrefab;
        public GameObject injuredPanel;
        public TMP_Text injuredHeroesText;
        public Button confirmButton;

        void Start()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Hide);
        }

        public void Show(CombatResult result)
        {
            if (panelRoot == null) return;
            if (gradeText != null)
            {
                gradeText.text = result.grade == "FAIL" ? "F" : result.grade;
                gradeText.color = Theme.GradeColor(result.grade);
            }
            if (powerRatioText != null) powerRatioText.text = "战力比 " + result.powerRatio.ToString("F2") + "（" + result.teamPower + " / " + result.difficulty + "）";
            if (battleNarrativeText != null) battleNarrativeText.text = result.battleNarrative;
            if (resultNarrativeText != null) resultNarrativeText.text = result.resultNarrative;
            if (mediaHeadlineText != null)
            {
                mediaHeadlineText.text = "📰 " + result.mediaHeadline;
                mediaHeadlineText.color = result.grade == "FAIL" ? Theme.Danger : Theme.Yellow;
            }
            if (budgetChangeText != null)
            {
                budgetChangeText.text = "预算 " + (result.budgetGain >= 0 ? "+" : "") + "¥" + result.budgetGain;
                budgetChangeText.color = result.budgetGain >= 0 ? Theme.Success : Theme.Danger;
            }
            if (repChangeText != null)
            {
                repChangeText.text = "声望 " + (result.repGain >= 0 ? "+" : "") + result.repGain;
                repChangeText.color = result.repGain >= 0 ? Theme.Success : Theme.Danger;
            }
            if (xpText != null) xpText.text = "XP +" + result.xpAwarded;
            if (collateralText != null)
            {
                collateralText.text = "附带损害: " + result.collateral;
                collateralText.color = result.collateral > 30 ? Theme.Danger : Theme.TextDim;
            }

            // 英雄表现
            if (heroPerformanceContainer != null)
            {
                foreach (Transform c in heroPerformanceContainer) Destroy(c.gameObject);
                if (heroPerformancePrefab != null)
                {
                    foreach (var p in result.performances)
                    {
                        var item = Instantiate(heroPerformancePrefab, heroPerformanceContainer);
                        item.SetActive(true);
                        var nameT = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
                        if (nameT != null) nameT.text = p.codename;
                        var powerT = item.transform.Find("PowerText")?.GetComponent<TMP_Text>();
                        if (powerT != null) powerT.text = "贡献 " + p.powerContributed.ToString("F0");
                        var quirkT = item.transform.Find("QuirkText")?.GetComponent<TMP_Text>();
                        if (quirkT != null)
                        {
                            quirkT.text = string.IsNullOrEmpty(p.quirkTriggered) ? "" : "⚠ " + p.quirkTriggered;
                            quirkT.color = Theme.Warning;
                        }
                        var injuryT = item.transform.Find("InjuryText")?.GetComponent<TMP_Text>();
                        if (injuryT != null)
                        {
                            injuryT.text = p.wasInjured ? "受伤!" : "";
                            injuryT.color = Theme.Danger;
                        }
                    }
                }
            }

            if (injuredPanel != null)
            {
                injuredPanel.SetActive(result.injuredHeroes.Count > 0);
                if (injuredHeroesText != null && result.injuredHeroes.Count > 0)
                    injuredHeroesText.text = "受伤: " + string.Join("、", result.injuredHeroes);
            }

            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            GameManager.Instance?.OnCombatSettled(null);
        }
    }

    // ==================== 日报面板 ====================
    public class DailyReportUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public TMP_Text titleText;
        public TMP_Text dateText;
        public TMP_Text eventsResolvedText;
        public TMP_Text eventsFailedText;
        public TMP_Text budgetText;
        public TMP_Text repText;
        public TMP_Text availableHeroesText;
        public Transform headlineContainer;
        public GameObject headlinePrefab;
        public Button nextDayButton;

        void Start()
        {
            if (nextDayButton != null) nextDayButton.onClick.AddListener(Hide);
        }

        public void Show(GameState g)
        {
            if (titleText != null) titleText.text = "📋 日报";
            if (dateText != null) dateText.text = "Day " + g.day + " 结束";
            if (eventsResolvedText != null) eventsResolvedText.text = "处理事件: " + g.dayStats.resolved;
            if (eventsFailedText != null) eventsFailedText.text = "失败/过期: " + g.dayStats.failed;
            if (budgetText != null) budgetText.text = "总预算: ¥" + g.budget.ToString("N0");
            if (repText != null) repText.text = "总声望: " + g.reputation + "/100";
            if (availableHeroesText != null)
                availableHeroesText.text = "英雄可用: " + g.heroes.Count(h => h.IsAvailable) + "/" + g.heroes.Count + "人";

            // 头条列表
            if (headlineContainer != null)
            {
                foreach (Transform c in headlineContainer) Destroy(c.gameObject);
                if (headlinePrefab != null)
                {
                    foreach (var headline in g.todaysHeadlines)
                    {
                        var item = Instantiate(headlinePrefab, headlineContainer);
                        item.SetActive(true);
                        var t = item.GetComponent<TMP_Text>();
                        if (t != null)
                        {
                            t.text = "📰 " + headline;
                            t.fontSize = 12;
                        }
                    }
                }
            }

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
