using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch
{
    // ==================== 地图面板 ====================
    public class MapPanelUI : MonoBehaviour
    {
        public RectTransform districtContainer;
        public RectTransform markerContainer;
        public GameObject eventMarkerPrefab;

        readonly Dictionary<int, GameObject> _markers = new Dictionary<int, GameObject>();
        bool _districtsBuilt;

        void Start() { }

        void EnsureDistricts()
        {
            if (_districtsBuilt || districtContainer == null) return;
            var em = EventManager.Instance;
            if (em == null || em.allDistricts == null || em.allDistricts.Length == 0) return;
            var parentRect = districtContainer.rect;
            if (parentRect.width < 50) return; // 布局未就绪

            _districtsBuilt = true;
            foreach (var d in em.allDistricts)
            {
                var go = new GameObject("dist-" + d.districtName, typeof(RectTransform));
                go.transform.SetParent(districtContainer, false);
                var img = go.AddComponent<Image>();
                img.color = new Color(d.districtColor.r, d.districtColor.g, d.districtColor.b, 0.12f);
                img.raycastTarget = false;
                var rt = (RectTransform)go.transform;
                var center = new Vector2((d.mapPos.x - 0.5f) * parentRect.width, (d.mapPos.y - 0.5f) * parentRect.height);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = center;
                rt.sizeDelta = new Vector2(d.mapSize.x * parentRect.width, d.mapSize.y * parentRect.height);

                var nameGo = new GameObject("name", typeof(RectTransform));
                nameGo.transform.SetParent(go.transform, false);
                var nameT = nameGo.AddComponent<TextMeshProUGUI>();
                nameT.text = d.emoji + " " + d.districtName;
                nameT.fontSize = 13;
                nameT.color = d.districtColor;
                nameT.alignment = TextAlignmentOptions.Center;
                nameT.raycastTarget = false;
                var nrt = (RectTransform)nameGo.transform;
                nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
                nrt.offsetMin = new Vector2(0, 6); nrt.offsetMax = new Vector2(0, -6);

                var safeGo = new GameObject("safety", typeof(RectTransform));
                safeGo.transform.SetParent(go.transform, false);
                var safeT = safeGo.AddComponent<TextMeshProUGUI>();
                safeT.fontSize = 10;
                safeT.raycastTarget = false;
                var srt = (RectTransform)safeGo.transform;
                srt.anchorMin = new Vector2(0.5f, 0); srt.anchorMax = new Vector2(0.5f, 0);
                srt.pivot = new Vector2(0.5f, 0);
                srt.anchoredPosition = new Vector2(0, 2);
                srt.sizeDelta = new Vector2(80, 16);
                safeT.alignment = TextAlignmentOptions.Center;
                _districtSafetyTexts[d.districtId] = safeT;
            }
        }

        readonly Dictionary<string, TMP_Text> _districtSafetyTexts = new Dictionary<string, TMP_Text>();

        void Update() => EnsureDistricts();

        public void Refresh(GameState g)
        {
            // 更新区域安全度文本
            foreach (var kv in _districtSafetyTexts)
            {
                if (kv.Value == null) continue;
                if (g.districtSafety.TryGetValue(kv.Key, out float safety))
                {
                    kv.Value.text = "安全 " + safety.ToString("F0");
                    kv.Value.color = safety > 60 ? Theme.Success : safety > 40 ? Theme.Warning : Theme.Danger;
                }
            }

            if (markerContainer == null || eventMarkerPrefab == null) return;
            var parentRect = markerContainer.rect;

            foreach (var kv in _markers) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _markers.Clear();

            foreach (var ev in g.events)
            {
                var marker = Instantiate(eventMarkerPrefab, markerContainer);
                marker.SetActive(true); // 预制体为隐藏状态，克隆需显式激活
                var mrt = (RectTransform)marker.transform;
                var pos = EventPos(ev); // 已是归一化坐标
                mrt.anchoredPosition = new Vector2((pos.x - 0.5f) * parentRect.width, (0.5f - pos.y) * parentRect.height);

                var gradeT = marker.transform.Find("GradeText")?.GetComponent<TMP_Text>();
                if (gradeT != null)
                {
                    gradeT.text = ev.revealed ? "[" + ev.grade + "]" : "[?]";
                    gradeT.color = Theme.GradeColor(ev.revealed ? ev.grade : "D");
                }
                var typeName = marker.transform.Find("TypeName")?.GetComponent<TMP_Text>();
                if (typeName != null) typeName.text = ev.revealed ? ev.template.eventName : "迷雾中…";
                var timeT = marker.transform.Find("TimeText")?.GetComponent<TMP_Text>();
                if (timeT != null)
                {
                    timeT.text = Mathf.CeilToInt(ev.timeLeft) + "s";
                    timeT.color = ev.timeLeft < 15 ? Theme.Danger : Theme.Warning;
                }
                var icon = marker.transform.Find("TypeIcon")?.GetComponent<TMP_Text>();
                if (icon != null) icon.text = ev.revealed ? ev.template.icon : "🌫️";

                int eid = ev.id;
                var btn = marker.GetComponent<Button>() ?? marker.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    ev.revealed = true; // 点击揭示
                    AudioManager.Instance?.UIClick();
                    UIManager.Instance?.infoPanel?.ShowEvent(eid);
                });

                _markers[ev.id] = marker;
            }
        }

        // 事件位置：区域归一化坐标附近随机偏移（伪随机保持稳定，不再二次归一化）
        static Vector2 EventPos(GameEvent ev)
        {
            var em = EventManager.Instance;
            var d = em?.allDistricts?.FirstOrDefault(x => x.districtId == ev.district);
            if (d == null) return new Vector2(0.5f, 0.5f);
            var rnd = new System.Random(ev.id * 7919);
            return d.mapPos + new Vector2((float)(rnd.NextDouble() - 0.5) * 0.07f, (float)(rnd.NextDouble() - 0.5) * 0.05f);
        }
    }

    // ==================== 信息面板（事件详情+派遣） ====================
    public class InfoPanelUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public TMP_Text titleText;
        public TMP_Text descText;
        public TMP_Text needText;
        public TMP_Text timerText;
        public TMP_Text rewardText;
        public Transform heroListContainer;
        public GameObject heroSelectItemPrefab;
        public Toggle aggressiveToggle, cautiousToggle, supportToggle, reconToggle;
        public TMP_Text tacticDescText;
        public TMP_Text powerEstimateText;
        public Button dispatchButton;
        public Button closeButton;

        readonly List<string> _selectedHeroes = new List<string>();
        TacticType _tactic = TacticType.Cautious;
        int _eventId = -1;
        string _eventGrade = "C";

        void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (dispatchButton != null) dispatchButton.onClick.AddListener(Dispatch);
            BindToggle(aggressiveToggle, TacticType.Aggressive, "激进：战力×1.2，附带损害增加");
            BindToggle(cautiousToggle, TacticType.Cautious, "谨慎：战力×1.0，稳妥执行");
            BindToggle(supportToggle, TacticType.Support, "支援：战力×0.9，附带损害减少");
            BindToggle(reconToggle, TacticType.Recon, "侦查：战力×0.85，提前揭示信息");
            if (cautiousToggle != null) cautiousToggle.isOn = true;
        }

        void BindToggle(Toggle t, TacticType type, string desc)
        {
            if (t == null) return;
            t.onValueChanged.AddListener(v =>
            {
                if (v)
                {
                    _tactic = type;
                    if (tacticDescText != null) tacticDescText.text = desc;
                    AudioManager.Instance?.UITab();
                    UpdatePowerEstimate();
                }
            });
        }

        // 打开期间实时刷新倒计时
        float _timerRefresh;
        void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf || _eventId < 0) return;
            _timerRefresh += Time.deltaTime;
            if (_timerRefresh < 0.5f) return;
            _timerRefresh = 0f;
            var ev = GameManager.Instance?.state?.FindEvent(_eventId);
            if (ev == null) { Hide(); return; }
            if (timerText != null)
            {
                timerText.text = "⏱ " + Mathf.CeilToInt(ev.timeLeft) + "秒";
                timerText.color = ev.timeLeft < 15 ? Theme.Danger : Theme.Warning;
            }
        }

        public void ShowEvent(int eventId)
        {
            var gm = GameManager.Instance;
            var ev = gm?.state?.FindEvent(eventId);
            if (ev == null) return;
            _eventId = eventId;
            _eventGrade = ev.grade;
            _selectedHeroes.Clear();

            bool revealed = ev.revealed;
            if (titleText != null) titleText.text = (revealed ? ev.template.icon + " " : "🌫️ ") + (revealed ? ev.template.eventName : "未知事件") + "  [" + ev.grade + "]";
            if (descText != null) descText.text = revealed ? ev.template.description : ev.template.fogDescription;
            if (needText != null) needText.text = "需求: " + ev.template.GetNeedText();
            if (timerText != null) timerText.text = "⏱ " + Mathf.CeilToInt(ev.timeLeft) + "秒";
            if (rewardText != null) rewardText.text = "声望+" + ev.rewardRep + "  预算+¥" + ev.rewardBudget;

            RefreshHeroList();
            UpdatePowerEstimate();

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        void RefreshHeroList()
        {
            if (heroListContainer == null || heroSelectItemPrefab == null) return;
            foreach (Transform child in heroListContainer) Destroy(child.gameObject);

            var gm = GameManager.Instance;
            var hm = HeroManager.Instance;
            var ev = gm.state.FindEvent(_eventId);
            if (ev == null) return;

            // 推荐英雄优先排序
            var heroes = gm.state.heroes.OrderByDescending(h =>
            {
                float s = 0;
                foreach (var n in ev.template.needs)
                    if (hm.HeroMatchesNeed(h, n)) s += hm.GetNeedStat(h, n);
                return s;
            }).ToList();

            foreach (var h in heroes)
            {
                var item = Instantiate(heroSelectItemPrefab, heroListContainer);
                item.SetActive(true);
                var nameT = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
                if (nameT != null) nameT.text = h.emoji + " " + h.codename + (hm.HeroMatchesNeed(h, ev.template.needs[0]) ? " ★" : "");
                var statsT = item.transform.Find("StatsText")?.GetComponent<TMP_Text>();
                if (statsT != null)
                    statsT.text = "力" + h.GetStat("str").ToString("F0") + " 速" + h.GetStat("spd").ToString("F0") +
                        " 智" + h.GetStat("int").ToString("F0") + " 耐" + h.GetStat("end").ToString("F0") + " 感" + h.GetStat("per").ToString("F0") +
                        "  疲劳" + h.fatigue.ToString("F0");
                var stateT = item.transform.Find("StateText")?.GetComponent<TMP_Text>();
                if (stateT != null)
                {
                    stateT.text = h.IsAvailable ? "✓" : h.IsOnMission ? "任务" : h.status == HeroStatus.Resting ? "休息" : h.status == HeroStatus.Injured ? "受伤" : "—";
                    stateT.color = h.IsAvailable ? Theme.Success : Theme.TextDim;
                }

                var tog = item.GetComponent<Toggle>() ?? item.AddComponent<Toggle>();
                var id = h.heroId;
                tog.onValueChanged.RemoveAllListeners();
                tog.onValueChanged.AddListener(v =>
                {
                    if (v)
                    {
                        if (_selectedHeroes.Count >= 3) { tog.SetIsOnWithoutNotify(false); return; }
                        _selectedHeroes.Add(id);
                        AudioManager.Instance?.PlayHeroVoice(id, VoiceType.Selection, 0.8f);
                    }
                    else _selectedHeroes.Remove(id);
                    UpdatePowerEstimate();
                });
            }
        }

        void UpdatePowerEstimate()
        {
            if (powerEstimateText == null || _eventId < 0) return;
            var gm = GameManager.Instance;
            var cs = CombatSystem.Instance;
            var ev = gm.state.FindEvent(_eventId);
            if (ev == null || _selectedHeroes.Count == 0)
            {
                powerEstimateText.text = "战力预估: —（选择英雄）";
                return;
            }
            float total = 0;
            foreach (var id in _selectedHeroes)
            {
                var h = gm.state.FindHero(id);
                if (h != null) total += cs.CalculateHeroPower(gm.state, h, ev, _tactic);
            }
            float difficulty = ev.grade switch { "D" => 0.4f, "C" => 0.7f, "B" => 1f, "A" => 1.5f, _ => 2.5f } * 25f;
            float ratio = total / Mathf.Max(difficulty, 1f);
            string pred = ratio >= 2f ? "S" : ratio >= 1.5f ? "A" : ratio >= 1f ? "B" : ratio >= 0.6f ? "C" : ratio >= 0.3f ? "D" : "FAIL";
            powerEstimateText.text = "战力预估: " + total.ToString("F0") + " / " + difficulty.ToString("F0") + "  → 预计[" + pred + "]";
            powerEstimateText.color = Theme.GradeColor(pred);
        }

        void Dispatch()
        {
            if (_selectedHeroes.Count == 0) return;
            var gm = GameManager.Instance;
            var em = EventManager.Instance;
            em.DispatchToEvent(gm.state, _eventId, _selectedHeroes.ToList(), _tactic);
            Hide();
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
