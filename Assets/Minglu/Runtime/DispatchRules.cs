using System;
using System.Collections.Generic;
using System.Linq;

namespace Minglu
{
    [Serializable] public class MissionRow
    {
        public string id, title, body, duty, prerequisite;
        public int district, start, deadline, priority, intelReward, progressReward, failurePressure;
    }

    public static class DispatchRules
    {
        public static IEnumerable<MissionRow> Open(RunState s)
        {
            if (!s.dispatchEnabled || s.ended) return Enumerable.Empty<MissionRow>();
            return s.config.missions.Where(m => m.start <= s.turn && m.deadline >= s.turn &&
                CityContentRules.Available(s,m.id) && !s.completedMissions.Contains(m.id) && !s.expiredMissions.Contains(m.id) &&
                (string.IsNullOrEmpty(m.prerequisite) || s.completedMissions.Contains(m.prerequisite)))
                .OrderBy(m => m.deadline).ThenByDescending(m => m.priority);
        }
        public static string Assign(RunState s, int agent, string mission)
        {
            var m = Open(s).FirstOrDefault(x => x.id == mission);
            if (m == null) return "任务尚未开放或已结束。";
            if (s.agents.Any(a => a.id != agent && a.condition == Condition.Active && a.mission == mission)) return "此任务已有负责人；请先撤回原指令。";
            string blocked=CityContentRules.AssignmentError(s,agent,mission);if(blocked!="")return blocked;
            string error = PrototypeRules.Assign(s, agent, (Duty)Enum.Parse(typeof(Duty), m.duty), -1, m.district);
            if (error == "") s.agents.First(a => a.id == agent).mission = mission;
            return error;
        }
        public static string Verify(RunState s, string id)
        {
            if (s.ended || !string.IsNullOrEmpty(s.pendingEvent) || !Open(s).Any(m => m.id == id)) return "当前无法核实此任务。";
            if (s.verifiedMissions.Contains(id)) return "情报已核实，无需重复支出。";
            if (s.intelligence - PrototypeRules.ReservedIntel(s) < 1) return "未预留的情报不足1份。";
            s.intelligence--; s.verifiedMissions.Add(id);
            s.journal.Add("[" + s.turn + "] 核实线报：" + s.config.missions.First(m => m.id == id).title + "，情报-1；该任务永久增加1枚行动骰。");
            return "";
        }
        public static int Dice(RunState s, Agent a, Duty duty, string mission)
        {
            return PrototypeRules.Dice(a, duty) + (s.dispatchEnabled && !string.IsNullOrEmpty(mission) && s.verifiedMissions.Contains(mission) ? 1 : 0);
        }
        public static double Probability(int dice, int required)
        {
            if (required <= 0) return 1;
            if (required > dice) return 0;
            double p = 0;
            for (int k = required; k <= dice; k++) { double combinations = 1; for (int i = 1; i <= k; i++) combinations = combinations * (dice - i + 1) / i; p += combinations / Math.Pow(2, dice); }
            return p;
        }
        public static IEnumerable<Agent> Recommend(RunState s, MissionRow m)
        {
            var duty = (Duty)Enum.Parse(typeof(Duty), m.duty);
            int required = s.config.Action(duty).required + s.config.District(m.district).difficulty;
            return s.agents.Where(a => a.condition == Condition.Active && a.order == Duty.Rest && a.organizationDuty==OrganizationDuty.None && (!s.organizationEnabled||a.spirit>=15))
                .OrderByDescending(a => Probability(Dice(s, a, duty, m.id), required))
                .ThenBy(a => a.exposure).ThenByDescending(a => a.stamina).ThenBy(a => a.id);
        }
        public static void Complete(RunState s, Agent a)
        {
            var m = Open(s).FirstOrDefault(x => x.id == a.mission);
            if (m == null) return;
            CampaignRules.CompleteMission(s,m.id);
            CityContentRules.Complete(s,m.id);
            s.completedMissions.Add(m.id); s.intelligence += m.intelReward; s.progress += m.progressReward;
            s.journal.Add("[" + s.turn + "] 任务完成：" + m.title + "；额外情报+" + m.intelReward + "，成果+" + m.progressReward + "。后续线索将在下一轮派遣开放。");
        }
        public static void EndNight(RunState s)
        {
            if (!s.dispatchEnabled) return;
            var penalizedGroups=new HashSet<string>();
            foreach (var m in s.config.missions.Where(m => m.deadline == s.turn && !s.completedMissions.Contains(m.id) && !s.expiredMissions.Contains(m.id)))
            {
                s.expiredMissions.Add(m.id);
                // A follow-up that was never unlocked has no additional penalty.
                bool unlocked = CityContentRules.Available(s,m.id)&&(string.IsNullOrEmpty(m.prerequisite) || s.completedMissions.Contains(m.prerequisite));
                var cityCase=CityContentRules.Case(s,m.id);
                if(unlocked&&cityCase!=null&&!string.IsNullOrEmpty(cityCase.group))unlocked=penalizedGroups.Add(cityCase.group);
                int pressure = unlocked ? m.failurePressure : 0;
                s.pressure += pressure;
                s.journal.Add("[" + s.turn + "] " + (unlocked ? "任务逾期：" : "线索中断：") + m.title + "；敌方压力+" + pressure + "。");
            }
        }
        public static void Validate(GameConfig c)
        {
            if (c.missions == null) c.missions = new List<MissionRow>();
            if (c.missions.Any(m => m == null || string.IsNullOrWhiteSpace(m.id)) || c.missions.Select(m => m.id).Distinct().Count() != c.missions.Count)
                throw new InvalidOperationException("Missions 主键为空或重复。");
            foreach (var m in c.missions)
            {
                if (string.IsNullOrWhiteSpace(m.title) || string.IsNullOrWhiteSpace(m.body) || (!new[]{"Recon","Sabotage","Contact","Propaganda"}.Contains(m.duty)||!c.actions.Any(a=>a.id==m.duty)) ||
                    !c.districts.Any(d => d.id == m.district) || m.start < 1 || m.deadline < m.start || m.deadline > c.Value("turns") || m.priority < 1 || m.priority > 3 ||
                    m.intelReward < 0 || m.intelReward > 10 || m.progressReward < 0 || m.progressReward > 20 || m.failurePressure < 0 || m.failurePressure > 30)
                    throw new InvalidOperationException("Missions " + m.id + " 行动、区域、期限或奖励越界。");
                if (!string.IsNullOrEmpty(m.prerequisite) && !c.missions.Any(p => p.id == m.prerequisite && p.deadline < m.start))
                    throw new InvalidOperationException("Missions " + m.id + " 前置任务必须存在，且在本任务开始前截止。");
            }
        }
        public static void ValidateSave(RunState s)
        {
            if (s.completedMissions == null || s.expiredMissions == null || s.verifiedMissions == null) throw new InvalidOperationException("任务存档不完整。");
            foreach (var ids in new[] { s.completedMissions, s.expiredMissions, s.verifiedMissions })
                if (ids.Distinct().Count() != ids.Count || ids.Any(id => !s.config.missions.Any(m => m.id == id))) throw new InvalidOperationException("任务存档引用错误。");
            if (s.completedMissions.Intersect(s.expiredMissions).Any()) throw new InvalidOperationException("任务状态冲突。");
            var assigned = s.agents.Where(a => !string.IsNullOrEmpty(a.mission)).ToList();
            if (assigned.Select(a => a.mission).Distinct().Count() != assigned.Count || assigned.Any(a => a.condition != Condition.Active || !Open(s).Any(m => m.id == a.mission && m.duty == a.order.ToString() && m.district == a.district)))
                throw new InvalidOperationException("存档派遣任务不合法。");
        }
    }
}
