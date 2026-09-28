using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroDispatch
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        public EventTemplate[] allEventTemplates;
        public DistrictData[] allDistricts;

        void Awake() { Instance = this; }

        static readonly Dictionary<string, float> DifficultyMult = new Dictionary<string, float>
        { { "D", 0.4f }, { "C", 0.7f }, { "B", 1f }, { "A", 1.5f }, { "S", 2.5f } };
        static readonly Dictionary<string, float> RewardMult = new Dictionary<string, float>
        { { "D", 0.5f }, { "C", 1f }, { "B", 1.5f }, { "A", 2.5f }, { "S", 4f } };

        // ==================== 生成（v2 权重规则） ====================
        static readonly string[] Grades = { "D", "C", "B", "A", "S" };
        static readonly int[] GradeWeights = { 30, 35, 20, 12, 3 };

        public void GenerateEvents(GameState g, int count)
        {
            if (allEventTemplates == null || allEventTemplates.Length == 0)
            {
                Debug.LogError("[EventManager] allEventTemplates 未配置！");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var tpl = allEventTemplates[Random.Range(0, allEventTemplates.Length)];
                var district = allDistricts[Random.Range(0, allDistricts.Length)];
                string grade = WeightedRandom();

                float gm = RewardMult[grade];
                var ev = new GameEvent
                {
                    id = g.nextEventId++,
                    template = tpl,
                    grade = grade,
                    district = district.districtId,
                    districtName = district.districtName,
                    timeLeft = 40 + Random.Range(0, 31),
                    rewardRep = Mathf.FloorToInt(3 * gm + Random.Range(0f, 3f)),
                    rewardBudget = Mathf.FloorToInt(500 * gm + Random.Range(0f, 300f)),
                    riskInjury = Mathf.FloorToInt(5 + gm * 8 + Random.Range(0f, 10f)),
                    riskCollateral = Mathf.FloorToInt(gm * 10 + Random.Range(0f, 15f)),
                    revealed = false
                };
                ev.maxTime = ev.timeLeft;
                g.events.Add(ev);
            }
            AudioManager.Instance?.Notification();
        }

        static string WeightedRandom()
        {
            int total = GradeWeights.Sum();
            int r = Random.Range(0, total);
            for (int i = 0; i < Grades.Length; i++)
            {
                r -= GradeWeights[i];
                if (r <= 0) return Grades[i];
            }
            return "D";
        }

        // ==================== 每秒 tick ====================
        public void TickEvents(GameState g, float delta)
        {
            for (int i = g.events.Count - 1; i >= 0; i--)
            {
                var ev = g.events[i];
                if (ev.IsNew)
                {
                    ev.timeLeft -= delta;
                    if (ev.IsExpired) ExpireEvent(g, ev);
                }
            }
        }

        void ExpireEvent(GameState g, GameEvent ev)
        {
            g.events.Remove(ev);
            g.reputation = Mathf.Max(0, g.reputation - 5);
            if (g.districtSafety.ContainsKey(ev.district))
                g.districtSafety[ev.district] = Mathf.Max(0, g.districtSafety[ev.district] - 10);
            g.dayStats.failed++;
            g.totalFailed++;
            Debug.Log("[EventManager] ❌ 「" + ev.template.eventName + "」超时恶化！声望-5");
        }

        // ==================== 派遣 ====================
        public bool DispatchToEvent(GameState g, int eventId, List<string> heroIds, TacticType tactic)
        {
            var ev = g.FindEvent(eventId);
            if (ev == null || !ev.IsNew) return false;

            var hm = HeroManager.Instance;
            foreach (var id in heroIds)
            {
                var h = hm.FindHero(id);
                if (h == null || !h.IsAvailable)
                {
                    Debug.Log("[EventManager] " + id + " 不可用");
                    return false;
                }
            }

            if (heroIds.Count < 1 || heroIds.Count > 3) return false;

            ev.assignedHeroIds = heroIds.ToList();
            ev.tacticUsed = tactic.ToString();
            ev.revealed = true;

            foreach (var id in heroIds)
            {
                var h = hm.FindHero(id);
                h.status = HeroStatus.OnMission;
                h.currentEventId = eventId;
            }

            g.events.Remove(ev);
            AudioManager.Instance?.MissionStart();
            if (heroIds.Count > 0)
                AudioManager.Instance?.PlayHeroVoice(heroIds[0], VoiceType.BattleStart);

            Debug.Log("[EventManager] 🚀 派遣 " + string.Join("、", heroIds) + " → " + ev.template.eventName);

            // 延迟结算：模拟英雄在途+处理时间
            StartCoroutine(CombatAfterDelay(g, ev, 2.5f));
            return true;
        }

        System.Collections.IEnumerator CombatAfterDelay(GameState g, GameEvent ev, float delay)
        {
            yield return new WaitForSeconds(delay);
            // 若已读档重开（事件对象失效），跳过
            if (g == null || g.gameOver || ev == null) yield break;
            CombatSystem.Instance?.ExecuteCombat(g, ev);
        }
    }
}
