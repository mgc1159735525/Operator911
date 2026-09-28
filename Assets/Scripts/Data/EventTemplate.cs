using UnityEngine;

namespace HeroDispatch
{
    // 事件模板（独立文件：类名必须与文件名一致）
    [CreateAssetMenu(fileName = "NewEvent", menuName = "HeroDispatch/EventTemplate")]
    public class EventTemplate : ScriptableObject
    {
        public EventType eventType;
        public string eventName;
        public string icon;
        [TextArea(2, 3)]
        public string description;
        [TextArea(1, 2)]
        public string fogDescription;

        [Header("需求能力（如 strength/perception/flight 等）")]
        public string[] needs;

        [Header("基准评级（D/C/B/A/S 生成时按权重随机）")]
        public string baseGrade;

        [Header("参数（由评级倍率进一步放大）")]
        public int baseReward = 500;
        public int baseReputation = 5;
        public int baseInjuryRisk = 10;
        public int baseCollateral = 10;

        public string GetNeedText()
        {
            var map = new System.Collections.Generic.Dictionary<string, string> {
                { "fire_resist", "🔥火焰抗性" }, { "ice", "❄️冰霜能力" }, { "strength", "💪力量" },
                { "speed", "⚡速度" }, { "intelligence", "🧠智力" }, { "perception", "👁️感知" },
                { "endurance", "🏋️耐力" }, { "flight", "🦅飞行" }, { "healing", "💚治愈" }, { "electronic", "⚡电磁" }
            };
            var parts = new System.Collections.Generic.List<string>();
            foreach (var n in needs)
                parts.Add(map.ContainsKey(n) ? map[n] : n);
            return string.Join(" / ", parts);
        }
    }
}
