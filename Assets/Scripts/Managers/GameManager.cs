using UnityEngine;

namespace HeroDispatch
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("子管理器（场景中的 GameObject）")]
        public HeroManager heroManager;
        public EventManager eventManager;
        public CombatSystem combatSystem;
        public SaveManager saveManager;
        public AudioManager audioManager;

        [Header("游戏状态")]
        public GameState state = new GameState();

        float _spawnAccumulator;
        float _uiRefreshTimer;

        void Awake() { Instance = this; }

        void Start()
        {
            if (heroManager == null) heroManager = FindObjectOfType<HeroManager>(true);
            if (eventManager == null) eventManager = FindObjectOfType<EventManager>(true);
            if (combatSystem == null) combatSystem = FindObjectOfType<CombatSystem>(true);
            if (saveManager == null) saveManager = FindObjectOfType<SaveManager>(true);
            if (audioManager == null) audioManager = FindObjectOfType<AudioManager>(true);

            StartNewGame();
        }

        public void StartNewGame()
        {
            state = new GameState();
            // v2: 6 区域初始安全度 80
            if (eventManager != null && eventManager.allDistricts != null)
                foreach (var d in eventManager.allDistricts)
                    state.districtSafety[d.districtId] = d.safety;

            heroManager?.InitHeroes(state);
            heroManager?.InitRelationships(state);
            eventManager?.GenerateEvents(state, 3);

            // 首次启动显示标题画面
            if (!_started)
            {
                _started = true;
                state.isPaused = true;
                UIManager.Instance?.RefreshAll();
                UIManager.Instance?.titleScreen?.Show();
            }
        }

        bool _started;

        public void BeginGame()
        {
            state.isPaused = false;
            UIManager.Instance?.titleScreen?.Hide();
            AudioManager.Instance?.PlaySFX("ui_menu_close");
        }

        public void RestartGame()
        {
            StartNewGame();
            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.HideAllOverlays();
                ui.gameOver?.Hide();
                ui.RefreshAll();
            }
            // 直接进入新一轮
            state.isPaused = false;
        }

        void Update()
        {
            if (state.gameOver || state.isPaused) return;

            float delta = Time.deltaTime * state.timeScale;
            state.timeInDay += delta;

            // 小时计算：6am 起一天 24 小时映射到 90 秒
            float progress = state.timeInDay / state.dayLength;
            state.hour = Mathf.FloorToInt(6 + progress * 24f) % 24;

            // 事件倒计时
            eventManager?.TickEvents(state, delta);

            // 英雄更新
            heroManager?.UpdateHeroes(delta);

            // 每 15 秒生成 1-2 个新事件（场上少于 5 个）
            _spawnAccumulator += delta;
            if (_spawnAccumulator >= 15f)
            {
                _spawnAccumulator = 0f;
                if (state.events.Count < 5)
                {
                    int n = Random.Range(1, 3);
                    eventManager?.GenerateEvents(state, n);
                    Debug.Log("[GameManager] 📡 新的紧急事件报告传入！");
                }
            }

            // 一天结束
            if (state.timeInDay >= state.dayLength)
                EndDay();

            // UI 节流刷新（每 0.3 秒，避免每帧销毁重建）
            _uiRefreshTimer += Time.unscaledDeltaTime;
            if (_uiRefreshTimer >= 0.3f)
            {
                _uiRefreshTimer = 0f;
                UIManager.Instance?.RefreshAll();
            }
        }

        // ==================== 日结算（v2 规则） ====================
        void EndDay()
        {
            state.timeInDay = 0;

            // 扣日薪 + 英雄恢复
            int totalSalary = 0;
            foreach (var h in state.heroes)
                totalSalary += h.data.salary;
            state.budget -= totalSalary;

            if (state.budget < 0)
            {
                state.reputation = Mathf.Max(0, state.reputation - 3);
                state.todaysHeadlines.Add("《预算赤字！调度中心资金告急》");
                Debug.Log("[GameManager] ⚠️ 预算赤字！声望-3");
            }

            heroManager?.DailyRecovery(state);

            Debug.Log("[GameManager] 💰 日薪支出 ¥" + totalSalary + "，当前预算 ¥" + state.budget);

            // 日报
            var ui = UIManager.Instance;
            ui?.dailyReport?.Show(state);
            audioManager?.DayTransition();

            // 推进天数
            state.day++;
            state.dayStats = new DayStats();
            state.todaysHeadlines.Clear();

            CheckGameOver();
        }

        public void CheckGameOver()
        {
            if (state.reputation <= 0)
                ShowGameOver(false, "声望降至0，你被解雇了。\n最后一天：第" + (state.day - 1) + "天");
            else if (state.reputation >= 100)
                ShowGameOver(true, "声望达到100！你成为城市传奇。\n用时：第" + (state.day - 1) + "天");
            else if (state.day > 30)
                ShowGameOver(true, "30天任期圆满结束。\n最终声望: " + state.reputation + "\n处理事件: " + state.totalResolved + " 成功 / " + state.totalFailed + " 失败");
        }

        void ShowGameOver(bool win, string reason)
        {
            state.gameOver = true;
            state.gameOverReason = reason;
            Debug.Log("[GameManager] " + (win ? "🏆 胜利: " : "💀 失败: ") + reason);
            string stats = "第 " + state.day + " 天 · 声望 " + state.reputation + "/100 · 预算 ¥" + state.budget +
                "\n事件处理 " + state.totalResolved + " 成功 / " + state.totalFailed + " 失败";
            UIManager.Instance?.gameOver?.Show(win, reason, stats);
        }

        // ==================== 控制 ====================
        public void TogglePause()
        {
            state.isPaused = !state.isPaused;
            Debug.Log("[GameManager] " + (state.isPaused ? "⏸ 暂停" : "▶ 继续"));
        }

        public void CycleSpeed()
        {
            state.timeScale = state.timeScale >= 3f ? 1f : state.timeScale + 1f;
            Debug.Log("[GameManager] ⏩ 速度 " + state.timeScale + "x");
        }

        public void OnCombatSettled(CombatResult result)
        {
            CheckGameOver();
        }

        // ==================== 存档 ====================
        public void SaveGame() => saveManager?.SaveGame(state);
        public void LoadGame()
        {
            if (saveManager != null && saveManager.HasSave())
                saveManager.LoadGame(state);
        }
    }
}
