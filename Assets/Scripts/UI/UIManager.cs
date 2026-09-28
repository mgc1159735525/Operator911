using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch
{
    // ==================== 颜色常量（v2 主题） ====================
    public static class Theme
    {
        public static readonly Color BgCanvas = Hex("#080812");
        public static readonly Color BgSurface = Hex("#0F0F1E");
        public static readonly Color BgElevated = Hex("#161628");
        public static readonly Color BgCard = Hex("#1A1A30");
        public static readonly Color Purple = Hex("#8B5CF6");
        public static readonly Color Cyan = Hex("#21E6C1");
        public static readonly Color Pink = Hex("#FF3CAC");
        public static readonly Color Yellow = Hex("#FFE66D");
        public static readonly Color Orange = Hex("#FF8C42");
        public static readonly Color Blue = Hex("#4DA8FF");
        public static readonly Color Danger = Hex("#FF4D6A");
        public static readonly Color Success = Hex("#21E6C1");
        public static readonly Color Warning = Hex("#FFE66D");
        public static readonly Color TextPrimary = Hex("#F0F0F8");
        public static readonly Color TextSecondary = Hex("#9D9DB8");
        public static readonly Color TextDim = Hex("#6B6B88");
        public static readonly Color BorderSubtle = Hex("#2A2A44");

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
        }

        public static Color GradeColor(string g) => g switch
        {
            "S" => Yellow, "A" => Success, "B" => Blue, "C" => Warning, "D" => Orange, _ => Danger
        };
    }

    // ==================== UI 总协调器 ====================
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("顶栏")]
        public TopBarUI topBar;

        [Header("面板")]
        public MapPanelUI mapPanel;
        public HeroBarUI heroBar;
        public InfoPanelUI infoPanel;
        public HeroDetailUI heroDetail;
        public CombatSimUI combatSim;
        public ResultUI result;
        public DailyReportUI dailyReport;
        public PauseBannerUI pauseBanner;
        public TitleScreenUI titleScreen;
        public GameOverUI gameOver;

        void Awake() { Instance = this; }

        public void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.state == null) return;
            topBar?.Refresh(gm.state);
            mapPanel?.Refresh(gm.state);
            heroBar?.Refresh(gm.state);
        }

        // 隐藏所有弹窗（重开游戏时）
        public void HideAllOverlays()
        {
            infoPanel?.Hide();
            heroDetail?.Hide();
            combatSim?.Hide();
            result?.Hide();
            dailyReport?.Hide();
            pauseBanner?.SetPaused(false);
        }
    }

    // ==================== 标题画面 ====================
    public class TitleScreenUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public Button startButton;

        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }

    // ==================== 游戏结束画面 ====================
    public class GameOverUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public TMP_Text titleText;
        public TMP_Text reasonText;
        public TMP_Text statsText;
        public Button restartButton;

        public void Show(bool win, string reason, string stats)
        {
            if (titleText != null)
            {
                titleText.text = win ? "🏆 传奇谢幕" : "💀 任务终止";
                titleText.color = win ? Theme.Yellow : Theme.Danger;
            }
            if (reasonText != null) reasonText.text = reason;
            if (statsText != null) statsText.text = stats;
            if (panelRoot != null) panelRoot.SetActive(true);
            AudioManager.Instance?.PlaySFX(win ? "event_mission_success" : "event_mission_fail");
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }

    // ==================== 顶栏 ====================
    public class TopBarUI : MonoBehaviour
    {
        public TMP_Text dayText;
        public TMP_Text timeText;
        public TMP_Text budgetText;
        public TMP_Text repText;
        public Button pauseButton;
        public Button speedButton;
        public TMP_Text speedText;

        float _lastRefresh;

        void Start()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClick);
            if (speedButton != null) speedButton.onClick.AddListener(OnSpeedClick);
        }

        void OnPauseClick()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.TogglePause();
            if (UIManager.Instance?.pauseBanner != null)
                UIManager.Instance.pauseBanner.SetPaused(gm.state.isPaused);
        }

        void OnSpeedClick()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.CycleSpeed();
        }

        public void Refresh(GameState g)
        {
            if (dayText != null) dayText.text = "DAY " + g.day;
            if (timeText != null)
                timeText.text = string.Format("{0:00}:00", g.hour);
            if (budgetText != null)
            {
                budgetText.text = "¥" + g.budget.ToString("N0");
                budgetText.color = g.budget < 2000 ? Theme.Danger : Theme.Yellow;
            }
            if (repText != null)
            {
                repText.text = g.reputation + "/100";
                repText.color = g.reputation > 50 ? Theme.Cyan : g.reputation > 25 ? Theme.Warning : Theme.Danger;
            }
            if (speedText != null) speedText.text = GameManager.Instance.state.timeScale + "x";
        }

        void Update()
        {
            if (Time.unscaledTime - _lastRefresh > 0.5f)
            {
                _lastRefresh = Time.unscaledTime;
                var gm = GameManager.Instance;
                if (gm != null && gm.state != null) Refresh(gm.state);
            }
        }
    }

    // ==================== 暂停横幅 ====================
    public class PauseBannerUI : MonoBehaviour
    {
        public GameObject bannerRoot;
        public TMP_Text pauseText;

        public void SetPaused(bool paused)
        {
            if (bannerRoot != null) bannerRoot.SetActive(paused);
            if (pauseText != null) pauseText.text = "⏸ 已暂停";
        }
    }

    // ==================== 底部英雄栏 ====================
    public class HeroBarUI : MonoBehaviour
    {
        public ScrollRect heroScroll;
        public GameObject heroCardPrefab;

        public void Refresh(GameState g)
        {
            if (heroScroll == null || heroCardPrefab == null || heroScroll.content == null) return;
            var content = heroScroll.content;

            foreach (Transform child in content) Destroy(child.gameObject);

            foreach (var h in g.heroes)
            {
                var card = Instantiate(heroCardPrefab, content);
                card.SetActive(true); // 预制体隐藏，克隆需激活
                var crt = (RectTransform)card.transform;

                var portrait = crt.Find("Portrait")?.GetComponent<Image>();
                if (portrait != null)
                    portrait.sprite = h.status == HeroStatus.Injured && h.data.portraitInjured != null
                        ? h.data.portraitInjured : h.data.portraitNormal;
                var nameT = crt.Find("NameText")?.GetComponent<TMP_Text>();
                if (nameT != null) nameT.text = h.codename;
                var stateT = crt.Find("StateText")?.GetComponent<TMP_Text>();
                if (stateT != null)
                {
                    stateT.text = h.IsAvailable ? "✓" : h.IsOnMission ? "⚔" :
                        h.status == HeroStatus.Resting ? "💤" : h.status == HeroStatus.Injured ? "🏥" : "🔙";
                    stateT.color = h.IsAvailable ? Theme.Success : h.IsOnMission ? Theme.Orange :
                        h.status == HeroStatus.Resting ? Theme.Blue : Theme.Danger;
                }
                var fatigueT = crt.Find("FatigueText")?.GetComponent<TMP_Text>();
                if (fatigueT != null)
                {
                    fatigueT.text = "疲" + h.fatigue.ToString("F0");
                    fatigueT.color = h.fatigue > 70 ? Theme.Danger : h.fatigue > 40 ? Theme.Warning : Theme.TextDim;
                }

                var id = h.heroId;
                var btn = card.GetComponent<Button>() ?? card.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.UIClick();
                    UIManager.Instance?.heroDetail?.Show(id);
                });
            }
        }
    }
}
