using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    [Serializable] public class SettingsRow
    {
        public string id; public int value; public string description;
    }
    [Serializable] public class AgentRow
    {
        public int id; public string name, role, bio; public int intel, stealth, handler, portrait;
    }
    [Serializable] public class DistrictRow
    {
        public int id; public string name, subtitle, description;
        public float x, y;
        public int risk, reconBonus, sabotageBonus, difficulty;
    }
    [Serializable] public class ActionRow
    {
        public string id, name, description;
        public int ap, intelCost, required, staminaCost, exposure, failExposure, intelReward, progressReward, pressure;
    }
    [Serializable] public class EventRow
    {
        public string id, title, body, optionA, optionB;
        public int turn, intelA, pressureA, progressA, intelB, pressureB, progressB;
    }
    [Serializable] public class AssetRow
    {
        public string id, resource, kind, description;
    }
    [Serializable] public class GameConfig
    {
        public string revision = "minglu-0.2";
        public List<SettingsRow> settings = new List<SettingsRow>();
        public List<AgentRow> agents = new List<AgentRow>();
        public List<DistrictRow> districts = new List<DistrictRow>();
        public List<ActionRow> actions = new List<ActionRow>();
        public List<EventRow> events = new List<EventRow>();
        public List<AssetRow> assets = new List<AssetRow>();
        public List<MissionRow> missions = new List<MissionRow>();
        public int Value(string id) { return settings.First(x => x.id == id).value; }
        public ActionRow Action(Duty duty) { return actions.First(x => x.id == duty.ToString()); }
        public DistrictRow District(int id) { return districts.First(x => x.id == id); }
        public AssetRow Asset(string id) { return assets.First(x => x.id == id); }
    }

    public static class ConfigStore
    {
        public static GameConfig Load()
        {
            var text = Resources.Load<TextAsset>("Minglu/Config/game_config");
            if (text == null) throw new InvalidOperationException("未找到配置。请使用 Minglu/配置表工具 导入工作簿。");
            var config = JsonUtility.FromJson<GameConfig>(text.text);
            Validate(config);
            return config;
        }
        public static void Validate(GameConfig c)
        {
            if (c == null || c.settings == null || c.agents == null || c.districts == null || c.actions == null || c.events == null || c.assets == null)
                throw new InvalidOperationException("配置缺少必要数据表。");
            Unique(c.settings.Select(x => x.id), "Settings"); Unique(c.agents.Select(x => x.id.ToString()), "Agents");
            Unique(c.districts.Select(x => x.id.ToString()), "Districts"); Unique(c.actions.Select(x => x.id), "Actions");
            Unique(c.events.Select(x => x.id), "Events"); Unique(c.assets.Select(x => x.id), "Assets");
            string[] keys = { "turns", "goal", "ap", "startingIntel", "startingPressure", "pressurePerTurn", "patrolThreshold", "patrolExposure", "captureThreshold", "leakTurn", "lostTurn", "leakExposure", "restStamina", "restExposure", "cutExposure", "rescuedStamina", "rescuedExposure" };
            foreach (string key in keys) if (!c.settings.Any(x => x.id == key)) throw new InvalidOperationException("Settings 缺少 " + key);
            if (c.settings.Count != keys.Length || c.settings.Any(x => x.value < 0 || x.value > 1000)) throw new InvalidOperationException("Settings 含未知参数或数值越界。");
            if (c.Value("turns") < 2 || c.Value("turns") > 40 || c.Value("ap") < 1 || c.Value("ap") > 8 || c.Value("goal") < 1 || c.Value("leakTurn") < 1 || c.Value("lostTurn") <= c.Value("leakTurn"))
                throw new InvalidOperationException("回合/AP/目标/审讯阈值不合法。");
            foreach (string key in keys.Where(x => x != "turns" && x != "goal" && x != "ap" && x != "startingIntel"))
                if (c.Value(key) > 100) throw new InvalidOperationException("Settings " + key + " 不能超过100。");
            if (c.Value("captureThreshold") < 1) throw new InvalidOperationException("被捕阈值必须为正数。");
            if (c.agents.Count < 2 || c.agents.Count > 8) throw new InvalidOperationException("当前界面支持2—8名成员。");
            foreach (var a in c.agents)
            {
                if (a.id < 0 || string.IsNullOrWhiteSpace(a.name) || a.intel < 1 || a.intel > 8 || a.stealth < 1 || a.stealth > 8 || a.portrait < 0 || a.portrait > 3) throw new InvalidOperationException("Agents " + a.id + " 字段越界。");
                var visited = new HashSet<int> { a.id }; int parent = a.handler;
                while (parent != -1)
                {
                    var p = c.agents.Find(x => x.id == parent);
                    if (p == null || !visited.Add(parent)) throw new InvalidOperationException("Agents " + a.id + " 上线不存在或形成环。");
                    parent = p.handler;
                }
            }
            if (c.districts.Count != 6 || !c.districts.Any(x => x.id == 0)) throw new InvalidOperationException("当前城市底图需要6个区域，且保留默认区域0。");
            foreach (var d in c.districts)
                if (d.id < 0 || string.IsNullOrWhiteSpace(d.name) || float.IsNaN(d.x) || float.IsNaN(d.y) || d.x < .08f || d.x > .92f || d.y < .08f || d.y > .92f || d.risk < 0 || d.risk > 30 || d.difficulty < 0 || d.difficulty > 2 || d.reconBonus < 0 || d.reconBonus > 3 || d.sabotageBonus < 0 || d.sabotageBonus > 8)
                    throw new InvalidOperationException("Districts " + d.id + " 地图位置或修正越界。");
            foreach (Duty duty in new[]{Duty.Rest,Duty.Recon,Duty.Sabotage,Duty.Rescue,Duty.CutContact}) if (!c.actions.Any(x => x.id == duty.ToString())) throw new InvalidOperationException("Actions 缺少 " + duty);
            if (c.actions.Count < 5 || c.actions.Count > 7 || c.actions.Any(a=>!Enum.IsDefined(typeof(Duty),a.id))) throw new InvalidOperationException("Actions 包含未知行为。");
            foreach (var a in c.actions)
                if (string.IsNullOrWhiteSpace(a.name) || a.ap < 0 || a.ap > c.Value("ap") || a.intelCost < 0 || a.intelCost > 10 || a.required < 0 || a.required > 8 || a.staminaCost < 0 || a.staminaCost > 100 || a.exposure < 0 || a.exposure > 100 || a.failExposure < 0 || a.failExposure > 100 || a.intelReward < 0 || a.intelReward > 10 || a.progressReward < 0 || a.progressReward > 100 || a.pressure < 0 || a.pressure > 100)
                    throw new InvalidOperationException("Actions " + a.id + " 数值越界。");
            if (c.Action(Duty.Rest).ap != 0 || c.Action(Duty.Rest).intelCost != 0 || c.Action(Duty.CutContact).intelCost != 0)
                throw new InvalidOperationException("休整必须免费，断线不消耗情报。");
            foreach (var a in new[] { c.Action(Duty.Rest), c.Action(Duty.CutContact) })
                if (a.required != 0 || a.staminaCost != 0 || a.exposure != 0 || a.failExposure != 0 || a.intelReward != 0 || a.progressReward != 0 || a.pressure != 0)
                    throw new InvalidOperationException(a.id + " 的判定和奖励字段必须为0；恢复效果由Settings控制。");
            foreach (var e in c.events)
                if (e.turn < 2 || e.turn > c.Value("turns") || string.IsNullOrWhiteSpace(e.title) || string.IsNullOrWhiteSpace(e.body) || string.IsNullOrWhiteSpace(e.optionA) || string.IsNullOrWhiteSpace(e.optionB) || new[] {e.intelA,e.intelB,e.pressureA,e.pressureB,e.progressA,e.progressB}.Any(x => Math.Abs((long)x) > 100))
                    throw new InvalidOperationException("Events " + e.id + " 事件字段不合法。");
            if (c.events.Select(x => x.turn).Distinct().Count() != c.events.Count) throw new InvalidOperationException("每回合最多一个剧情事件。");
            if (c.events.Any(e => (e.intelA < 0 || e.progressA < 0) && (e.intelB < 0 || e.progressB < 0))) throw new InvalidOperationException("每个事件至少保留一个无需消耗情报/成果的选项，避免资源不足时卡死。");
            DispatchRules.Validate(c);
            foreach (string id in new[] { "city", "portraits" })
                if (!c.assets.Any(x => x.id == id && !string.IsNullOrWhiteSpace(x.resource))) throw new InvalidOperationException("Assets 缺少 " + id);
            if (c.assets.Any(x => x.resource.Contains("..") || x.resource.Contains(":") || x.resource.StartsWith("/"))) throw new InvalidOperationException("资源必须使用Resources相对路径。");
        }
        static void Unique(IEnumerable<string> ids, string table)
        {
            var list = ids.ToList();
            if (list.Any(string.IsNullOrWhiteSpace) || list.Distinct().Count() != list.Count) throw new InvalidOperationException(table + " 主键为空或重复。");
        }
    }
}
