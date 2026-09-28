using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroDispatch
{
    [System.Serializable]
    public class HeroRelation
    {
        public string heroIdA;
        public string heroIdB;
        public RelationType relationType;
        public string desc;
    }

    public class HeroManager : MonoBehaviour
    {
        public static HeroManager Instance { get; private set; }

        public HeroData[] heroDataArray;
        public List<HeroRelation> relationConfigs = new List<HeroRelation>();

        public List<Hero> Heroes { get; private set; } = new List<Hero>();

        void Awake() { Instance = this; }

        public void InitHeroes(GameState g)
        {
            Heroes.Clear();
            g.heroes.Clear();

            if (heroDataArray == null || heroDataArray.Length == 0)
            {
                Debug.LogError("[HeroManager] heroDataArray 未配置！请先运行 HeroDispatch → 创建所有默认数据 并重新搭建场景");
                return;
            }

            foreach (var d in heroDataArray)
            {
                if (d == null) continue;
                var h = new Hero { data = d, level = d.level };
                Heroes.Add(h);
                g.heroes.Add(h);
            }
            Debug.Log("[HeroManager] 已初始化 " + Heroes.Count + " 名英雄");
        }

        public Hero FindHero(string id) => Heroes.Find(h => h.heroId == id);

        // ==================== 需求匹配（v2 规则） ====================
        static readonly Dictionary<string, string> NeedStatMap = new Dictionary<string, string>
        {
            { "strength", "str" }, { "speed", "spd" }, { "intelligence", "int" },
            { "endurance", "end" }, { "perception", "per" },
            { "fire_resist", "end" }, { "flight", "spd" }, { "healing", "per" },
            { "electronic", "int" }, { "ice", "int" }
        };

        // 能力特长表（HeroTable.PowerMap）
        static readonly Dictionary<string, string[]> PowerMap = BuildPowerMap();
        static Dictionary<string, string[]> BuildPowerMap()
        {
            var m = new Dictionary<string, string[]>();
            foreach (var (id, powers) in HeroTable.PowerMap) m[id] = powers;
            return m;
        }

        public bool HeroMatchesNeed(Hero h, string need)
        {
            if (PowerMap.TryGetValue(h.heroId, out var powers) && powers.Contains(need)) return true;
            if (NeedStatMap.TryGetValue(need, out var stat)) return h.GetStat(stat) >= 7;
            return false;
        }

        public float GetNeedStat(Hero h, string need)
        {
            if (NeedStatMap.TryGetValue(need, out var stat)) return h.GetStat(stat);
            return 5f;
        }

        // ==================== 关系 ====================
        public void InitRelationships(GameState g)
        {
            g.relationships.Clear();
            relationConfigs.Clear();
            foreach (var (a, b, rel, desc) in HeroTable.Relations)
            {
                g.relationships[a + "|" + b] = rel;
                g.relationships[b + "|" + a] = rel;
                relationConfigs.Add(new HeroRelation { heroIdA = a, heroIdB = b, relationType = rel, desc = desc });
            }
        }

        public RelationType GetRelation(GameState g, string a, string b)
        {
            var key = a + "|" + b;
            return g.relationships.TryGetValue(key, out var r) ? r : RelationType.Neutral;
        }

        // ==================== 日恢复（v2 规则） ====================
        public void DailyRecovery(GameState g)
        {
            foreach (var h in g.heroes)
            {
                if (h.status == HeroStatus.Resting)
                {
                    h.fatigue = Mathf.Max(0, h.fatigue - 30f);
                    if (h.fatigue <= 20) h.status = HeroStatus.Ready;
                }
                else if (h.status == HeroStatus.Injured)
                {
                    h.fatigue = Mathf.Max(0, h.fatigue - 15f);
                    h.mental = Mathf.Min(100, h.mental + 5);
                    if (Random.value < 0.4f)
                    {
                        h.status = HeroStatus.Resting;
                        Debug.Log("[HeroManager] 💊 " + h.codename + " 伤势好转，进入恢复期");
                    }
                }
                else
                {
                    h.fatigue = Mathf.Max(0, h.fatigue - 10f);
                }
                h.consecutiveMissions = 0;
            }
        }

        public void RestHero(GameState g, string heroId)
        {
            var h = FindHero(heroId);
            if (h == null || h.status == HeroStatus.OnMission) return;
            h.status = HeroStatus.Resting;
            h.restTimer = 0;
            Debug.Log("[HeroManager] " + h.codename + " 开始休息");
        }

        public void UpdateHeroes(float delta)
        {
            foreach (var h in Heroes)
            {
                if (h.status == HeroStatus.Returning)
                {
                    h.returnTimer -= delta;
                    if (h.returnTimer <= 0)
                    {
                        h.status = HeroStatus.Ready;
                        h.currentEventId = -1;
                    }
                }
                if (h.IsOnMission)
                    h.fatigue = Mathf.Min(100, h.fatigue + 0.8f * delta);
            }
        }

        public void ApplyCombatResult(GameState g, CombatResult result)
        {
            foreach (var p in result.performances)
            {
                var h = FindHero(p.heroId);
                if (h == null) continue;
                h.AddXp(p.xpGained);
                h.consecutiveMissions++;
                h.fatigue = Mathf.Min(100, h.fatigue + 15f);
                if (result.grade == "FAIL")
                {
                    h.missionsFailed++;
                    h.mental = Mathf.Max(0, h.mental - 10f);
                }
                else
                {
                    h.missionsCompleted++;
                    h.mental = Mathf.Min(100, h.mental + 5f);
                }
                if (p.wasInjured)
                {
                    h.hp = Mathf.Max(5, h.hp - 40f);
                    h.status = h.hp < 35 ? HeroStatus.Injured : HeroStatus.Returning;
                    h.returnTimer = 12f;
                }
                else
                {
                    h.status = HeroStatus.Returning;
                    h.returnTimer = 10f;
                }
            }
        }
    }
}
