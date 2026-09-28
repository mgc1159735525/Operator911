using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroDispatch
{
    public class CombatSystem : MonoBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        void Awake() { Instance = this; }

        static readonly Dictionary<string, float> GradeMult = new Dictionary<string, float>
        { { "D", 0.4f }, { "C", 0.7f }, { "B", 1f }, { "A", 1.5f }, { "S", 2.5f } };

        // ==================== 单英雄战力（v2 七步公式） ====================
        public float CalculateHeroPower(GameState g, Hero h, GameEvent ev, TacticType tactic)
        {
            float power = 0f;
            foreach (var need in ev.template.needs)
            {
                if (HeroManager.Instance.HeroMatchesNeed(h, need))
                    power += HeroManager.Instance.GetNeedStat(h, need) * 1.5f;
                else
                {
                    float avg = (h.GetStat("str") + h.GetStat("spd") + h.GetStat("int") + h.GetStat("end") + h.GetStat("per")) / 5f;
                    power += avg * 0.3f;
                }
            }

            power *= (1f - h.fatigue / 200f);                    // 疲劳
            power *= (0.5f + h.mental / 200f);                   // 心理
            power *= (1f + h.level * 0.05f);                      // 等级

            bool quirk = Random.value < 0.15f;                   // 怪癖 15%
            if (quirk) power *= 0.6f;

            h.lastQuirkRolled = quirk;
            return power;
        }

        // ==================== 结果计算（v2 公式） ====================
        public CombatResult CalculateOutcome(GameState g, GameEvent ev, List<Hero> heroes, TacticType tactic)
        {
            var hm = HeroManager.Instance;
            float teamPower = 0f;
            var quirkList = new List<string>();

            foreach (var h in heroes)
            {
                teamPower += CalculateHeroPower(g, h, ev, tactic);
                if (h.lastQuirkRolled) quirkList.Add(h.heroId);
            }

            // 团队协同
            float synergy = 1f;
            for (int i = 0; i < heroes.Count; i++)
                for (int j = i + 1; j < heroes.Count; j++)
                {
                    var rel = hm.GetRelation(g, heroes[i].heroId, heroes[j].heroId);
                    if (rel == RelationType.Partner) synergy += 0.15f;
                    else if (rel == RelationType.Rival) synergy -= 0.05f;
                    else if (rel == RelationType.Conflict) synergy -= 0.2f;
                }
            teamPower *= synergy;

            // 战术修正
            teamPower *= tactic switch
            {
                TacticType.Aggressive => 1.2f,
                TacticType.Cautious => 1.0f,
                TacticType.Support => 0.9f,
                TacticType.Recon => 0.85f,
                _ => 1f
            };

            float difficulty = GradeMult[ev.grade] * 25f;
            float ratio = teamPower / Mathf.Max(difficulty, 1f);

            // 评级
            string resultGrade;
            if (ratio >= 2.0f) resultGrade = "S";
            else if (ratio >= 1.5f) resultGrade = "A";
            else if (ratio >= 1.0f) resultGrade = "B";
            else if (ratio >= 0.6f) resultGrade = "C";
            else if (ratio >= 0.3f) resultGrade = "D";
            else resultGrade = "FAIL";

            // 附带损害
            float collateral = tactic == TacticType.Aggressive
                ? ev.riskCollateral * (1.5f - ratio * 0.3f) + Random.Range(0f, 15f)
                : ev.riskCollateral * (1.0f - ratio * 0.2f) + Random.Range(0f, 10f);
            collateral = Mathf.Max(0f, collateral);

            // 受伤判定
            var injuries = new List<string>();
            float injMul = resultGrade == "FAIL" ? 2f : resultGrade == "D" ? 1.3f : 0.5f;
            foreach (var h in heroes)
            {
                if (Random.value < ev.riskInjury * injMul / 100f)
                    injuries.Add(h.heroId);
            }

            return new CombatResult
            {
                eventId = ev.id,
                grade = resultGrade,
                powerRatio = ratio,
                teamPower = Mathf.RoundToInt(teamPower),
                difficulty = Mathf.RoundToInt(difficulty),
                collateral = Mathf.RoundToInt(collateral),
                injuredHeroes = injuries,
                quirkTriggered = quirkList,
                repGain = resultGrade == "FAIL" ? -ev.rewardRep / 2 : ev.rewardRep,
                budgetGain = resultGrade == "FAIL" ? -ev.rewardBudget / 3 : ev.rewardBudget,
                xpAwarded = GradeToXp(resultGrade)
            };
        }

        public static int GradeToXp(string g) => g switch
        {
            "S" => 30, "A" => 20, "B" => 15, "C" => 10, "D" => 5, _ => 2
        };

        // ==================== 执行战斗 ====================
        public void ExecuteCombat(GameState g, GameEvent ev)
        {
            var hm = HeroManager.Instance;
            var heroes = new List<Hero>();
            foreach (var id in ev.assignedHeroIds)
            {
                var h = hm.FindHero(id);
                if (h != null) heroes.Add(h);
            }
            if (heroes.Count == 0) return;

            var tactic = ev.tacticUsed switch
            {
                "Aggressive" => TacticType.Aggressive,
                "Support" => TacticType.Support,
                "Recon" => TacticType.Recon,
                _ => TacticType.Cautious
            };

            var result = CalculateOutcome(g, ev, heroes, tactic);

            // 英雄表现
            foreach (var h in heroes)
            {
                result.performances.Add(new HeroPerformance
                {
                    heroId = h.heroId,
                    codename = h.codename,
                    powerContributed = CalculateHeroPower(g, h, ev, tactic),
                    quirkTriggered = result.quirkTriggered.Contains(h.heroId) ? h.data.quirkName : "",
                    wasInjured = result.injuredHeroes.Contains(h.heroId),
                    xpGained = result.xpAwarded
                });
            }

            // 叙事
            result.battleNarrative = GenerateBattleNarrative(ev, heroes, result);
            result.resultNarrative = GenerateResultNarrative(result);
            result.mediaHeadline = GenerateMediaHeadline(ev, result);

            // 应用到状态
            hm.ApplyCombatResult(g, result);
            g.reputation = Mathf.Clamp(g.reputation + result.repGain, 0, 100);
            g.budget = Mathf.Max(0, g.budget + result.budgetGain);
            if (result.grade == "FAIL") { g.dayStats.failed++; g.totalFailed++; }
            else { g.dayStats.resolved++; g.totalResolved++; g.dayStats.xpGained += result.xpAwarded; }
            g.todaysHeadlines.Add(result.mediaHeadline);

            // 音效
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.PlayCombatSFX(ev.template.eventType);
                if (result.grade == "FAIL") audio.MissionFail();
                else audio.MissionSuccess();
                if (result.injuredHeroes.Count > 0) audio.HeroInjured();
                if (result.grade == "S" && ev.assignedHeroIds.Count > 0)
                    audio.PlayHeroVoice(ev.assignedHeroIds[0], VoiceType.Victory);
            }

            // 播放演出（演出结束后由 CombatSimUI 回调显示结算）
            var ui = UIManager.Instance;
            if (ui != null && ui.combatSim != null)
                ui.combatSim.Play(ev, result);
            else
                ui?.result?.Show(result);

            Debug.Log("[CombatSystem] " + ev.template.eventName + " → " + result.grade + " (ratio " + result.powerRatio.ToString("F2") + ")");
        }

        // ==================== 叙事生成（v2 风格） ====================
        string GenerateBattleNarrative(GameEvent ev, List<Hero> heroes, CombatResult r)
        {
            var names = heroes.Select(h => h.codename).ToList();
            var sb = new System.Text.StringBuilder();
            sb.Append(names[0]);
            for (int i = 1; i < names.Count; i++) sb.Append("与").Append(names[i]);
            sb.Append("抵达").Append(ev.districtName).Append("。");

            foreach (var p in r.performances)
                if (!string.IsNullOrEmpty(p.quirkTriggered))
                    sb.Append("⚠ ").Append(p.codename).Append("的「").Append(p.quirkTriggered).Append("」发作了！");

            if (r.grade == "S") sb.Append("英雄们配合得天衣无缝，迅速控制了局面！");
            else if (r.grade == "A") sb.Append("行动干净利落，完美解决了危机。");
            else if (r.grade == "B") sb.Append("经过一番努力，事件得到妥善处理。");
            else if (r.grade == "C") sb.Append("虽然有些波折，但总算控制住了局面。");
            else if (r.grade == "D") sb.Append("行动勉强完成，但代价不小。");
            else sb.Append("英雄们力不从心，未能阻止事态恶化……");
            return sb.ToString();
        }

        string GenerateResultNarrative(CombatResult r) => r.grade switch
        {
            "S" => "教科书级别的行动！市民纷纷点赞。",
            "A" => "出色的处理，市民感到安心。",
            "B" => "合格的表现，没有大问题。",
            "C" => "勉强及格，但有改进空间。",
            "D" => "表现不佳，市政厅表达了关切。",
            _ => "任务失败。媒体开始质疑团队的能力。"
        };

        string GenerateMediaHeadline(GameEvent ev, CombatResult r)
        {
            if (r.collateral > 40) return "《英雄大战后，谁来为废墟买单？》";
            return r.grade switch
            {
                "S" => "《太神了！英雄团队再创奇迹》",
                "A" => "《干得漂亮：城市英雄值得信赖》",
                "B" => "《英雄出动，危机解除》",
                "C" => "《虽有瑕疵，尚可接受》",
                "D" => "《英雄表现令人担忧》",
                _ => "《惨败！调度中心必须给出解释》"
            };
        }
    }

    // 怪癖标记已移至 Hero.lastQuirkRolled 字段
}
